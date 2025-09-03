using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using FirebirdApi.Services;
using FirebirdApi.Models;
using FirebirdApi.Protos;
using Swashbuckle.AspNetCore.Annotations;

namespace FirebirdApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SyncController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SyncController> _logger;
        private readonly IDatabaseConfigService _configService;
        private readonly IMachineIdService _machineIdService;
        private readonly ICommandStreamService _commandStreamService;
        private readonly ISystemInfoService _systemInfoService;

        public SyncController(HttpClient httpClient, IConfiguration configuration, ILogger<SyncController> logger, IDatabaseConfigService configService, IMachineIdService machineIdService, ICommandStreamService commandStreamService, ISystemInfoService systemInfoService)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _configService = configService;
            _machineIdService = machineIdService;
            _commandStreamService = commandStreamService;
            _systemInfoService = systemInfoService;
        }

        /// <summary>
        /// Registrar nó desktop no servidor cloud via streaming gRPC (MÉTODO UNIFICADO)
        /// </summary>
        [HttpPost("register-node")]
        public async Task<IActionResult> RegisterNode([FromBody] DesktopNodeRegistration request)
        {
            try
            {
                _logger.LogInformation("🔄 Iniciando registro unificado de nó desktop: {MachineId}", request.MachineId);

                // Armazenar o MachineId recebido do Electron
                _machineIdService.SetMachineId(request.MachineId);
                _logger.LogInformation("✅ MachineId armazenado localmente: {MachineId}", request.MachineId);

                // Obter token de autenticação se disponível
                var authToken = GetAuthTokenFromRequest();
                var isAnonymous = string.IsNullOrEmpty(authToken);

                _logger.LogInformation("📝 Registrando nó como {NodeType}: {MachineId}", 
                    isAnonymous ? "ANÔNIMO (userId=null)" : "AUTENTICADO (userId=preenchido)", request.MachineId);

                // Gerar connectionId único baseado no nodeId
                var connectionId = request.NodeId ?? Guid.NewGuid().ToString();
                
                // Obter informações do sistema
                var operatingSystem = _systemInfoService.GetOperatingSystem();
                var systemVersion = _systemInfoService.GetSystemVersion();
                
                // Atualizar request com informações do sistema se não foram fornecidas
                if (string.IsNullOrEmpty(request.OperatingSystem))
                {
                    request.OperatingSystem = operatingSystem;
                }
                if (string.IsNullOrEmpty(request.Version))
                {
                    request.Version = systemVersion;
                }
                
                // 1. Iniciar streaming gRPC (que já registra o nó automaticamente)
                await _commandStreamService.StartStreamingAsync(connectionId, request.MachineId, authToken, connectionId, request.Name, request.MachineName, request.Version, request.OperatingSystem);

                if (_commandStreamService.IsConnected)
                {
                    _logger.LogInformation("✅ Nó registrado e streaming iniciado com sucesso - isAnonymous: {IsAnonymous}", isAnonymous);
                    
                    // 2. Sincronizar databases automaticamente após registro do nó
                    await SyncAllDatabasesToCloudAsync();
                    
                    return Ok(new { 
                        success = true, 
                        data = new {
                            nodeId = connectionId,
                            connectionId = connectionId,
                            machineId = request.MachineId,
                            isAnonymous = isAnonymous,
                            isConnected = true,
                            message = $"Nó registrado e streaming iniciado via gRPC (isAnonymous: {isAnonymous})",
                            timestamp = DateTime.UtcNow
                        }
                    });
                }
                else
                {
                    _logger.LogWarning("❌ Falha ao registrar nó e iniciar streaming");
                    return BadRequest(new { 
                        success = false, 
                        error = "Falha ao conectar com o servidor cloud",
                        machineId = request.MachineId,
                        timestamp = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro interno ao registrar nó via streaming gRPC");
                return StatusCode(500, new { 
                    success = false, 
                    error = $"Erro interno: {ex.Message}",
                    machineId = request.MachineId,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Registrar nó desktop no servidor cloud via streaming gRPC (MÉTODO LEGADO - DEPRECATED)
        /// </summary>
        [HttpPost("register-node-legacy")]
        [Obsolete("Use /register-node que agora é unificado")]
        public async Task<IActionResult> RegisterNodeLegacy([FromBody] DesktopNodeRegistration request)
        {
            return await RegisterNode(request);
        }



        /// <summary>
        /// Iniciar sincronização inicial com o servidor cloud
        /// </summary>
        [HttpPost("start-initial-sync")]
        public async Task<IActionResult> StartInitialSync([FromBody] InitialSyncRequest request)
        {
            try
            {
                _logger.LogInformation("Iniciando sincronização inicial para nó: {DesktopNodeId}", request.DesktopNodeId);

                var cloudServerUrl = _configuration["CloudServer:Url"] ?? "https://localhost:7001";
                var response = await _httpClient.PostAsJsonAsync(
                    $"{cloudServerUrl}/api/Sync/initial-sync/start", 
                    request
                );

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<object>();
                    _logger.LogInformation("Sincronização inicial iniciada com sucesso");
                    return Ok(new { success = true, data = result });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Falha ao iniciar sincronização inicial: {StatusCode} - {Error}", 
                        response.StatusCode, errorContent);
                    return StatusCode((int)response.StatusCode, new { 
                        success = false, 
                        error = $"Erro ao iniciar sincronização: {response.StatusCode}" 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro interno ao iniciar sincronização inicial");
                return StatusCode(500, new { 
                    success = false, 
                    error = $"Erro interno: {ex.Message}" 
                });
            }
        }

        /// <summary>
        /// Enviar lote de dados para sincronização
        /// </summary>
        [HttpPost("send-sync-batch")]
        public async Task<IActionResult> SendSyncBatch([FromBody] SyncDataBatchRequest request)
        {
            try
            {
                _logger.LogInformation("Enviando lote de sincronização: {ItemsCount} itens", request.DataBatch.Count);

                var cloudServerUrl = _configuration["CloudServer:Url"] ?? "https://localhost:7001";
                var response = await _httpClient.PostAsJsonAsync(
                    $"{cloudServerUrl}/api/Sync/initial-sync/batch", 
                    request
                );

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<object>();
                    _logger.LogInformation("Lote de sincronização enviado com sucesso");
                    return Ok(new { success = true, data = result });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Falha ao enviar lote de sincronização: {StatusCode} - {Error}", 
                        response.StatusCode, errorContent);
                    return StatusCode((int)response.StatusCode, new { 
                        success = false, 
                        error = $"Erro ao enviar lote: {response.StatusCode}" 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro interno ao enviar lote de sincronização");
                return StatusCode(500, new { 
                    success = false, 
                    error = $"Erro interno: {ex.Message}" 
                });
            }
        }

        /// <summary>
        /// Completar sincronização inicial
        /// </summary>
        [HttpPost("complete-initial-sync")]
        public async Task<IActionResult> CompleteInitialSync([FromBody] SyncCompleteRequest request)
        {
            try
            {
                _logger.LogInformation("Completando sincronização inicial: {OperationId}", request.OperationId);

                var cloudServerUrl = _configuration["CloudServer:Url"] ?? "https://localhost:7001";
                var response = await _httpClient.PostAsJsonAsync(
                    $"{cloudServerUrl}/api/Sync/initial-sync/complete", 
                    request
                );

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<object>();
                    _logger.LogInformation("Sincronização inicial completada com sucesso");
                    return Ok(new { success = true, data = result });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Falha ao completar sincronização inicial: {StatusCode} - {Error}", 
                        response.StatusCode, errorContent);
                    return StatusCode((int)response.StatusCode, new { 
                        success = false, 
                        error = $"Erro ao completar sincronização: {response.StatusCode}" 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro interno ao completar sincronização inicial");
                return StatusCode(500, new { 
                    success = false, 
                    error = $"Erro interno: {ex.Message}" 
                });
            }
        }

        /// <summary>
        /// Atualizar tamanhos de todas as bases de dados locais
        /// </summary>
        [HttpPost("update-database-sizes")]
        public async Task<IActionResult> UpdateDatabaseSizes()
        {
            try
            {
                _logger.LogInformation("Atualizando tamanhos de todas as bases de dados");

                var success = await _configService.UpdateAllDatabaseFileSizesAsync();
                
                if (success)
                {
                    var databases = await _configService.GetAllDatabasesAsync();
                    var results = databases.Select(db => new
                    {
                        id = db.Id,
                        name = db.Name,
                        fileSizeBytes = db.FileSizeBytes,
                        lastSizeCheck = db.LastSizeCheck,
                        fileSizeFormatted = db.FileSizeBytes.HasValue ? FormatFileSize(db.FileSizeBytes.Value) : "N/A"
                    }).ToList();

                    return Ok(new
                    {
                        success = true,
                        data = results,
                        message = $"Tamanhos atualizados para {results.Count} bases de dados"
                    });
                }
                else
                {
                    return StatusCode(500, new
                    {
                        success = false,
                        error = "Erro ao atualizar tamanhos das bases de dados"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro interno ao atualizar tamanhos das bases");
                return StatusCode(500, new
                {
                    success = false,
                    error = $"Erro interno: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Sincronizar uma base específica quando criada (cenário 1)
        /// </summary>
        [HttpPost("sync-database-created/{databaseId}")]
        public async Task<IActionResult> SyncDatabaseCreated(string databaseId)
        {
            try
            {
                _logger.LogInformation("Sincronizando base criada: {DatabaseId}", databaseId);

                var database = await _configService.GetDatabaseByIdAsync(databaseId);
                if (database == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        error = $"Base de dados com ID '{databaseId}' não encontrada"
                    });
                }

                // Atualizar tamanho do arquivo
                await _configService.UpdateAllDatabaseFileSizesAsync();

                var cloudServerUrl = _configuration["CloudServer:Url"] ?? "https://localhost:7001";
                var machineId = GetMachineId();

                var request = new
                {
                    DesktopNodeId = machineId,
                    Name = database.Name,
                    Server = database.Server,
                    Database = database.Database,
                    User = database.Username,
                    Password = database.Password,
                    Port = database.Port,
                    Charset = database.Charset,
                    FileSizeBytes = database.FileSizeBytes,
                    LastSizeCheck = database.LastSizeCheck,
                    OperatingSystem = _systemInfoService.GetOperatingSystem(),
                    SystemVersion = _systemInfoService.GetSystemVersion(),
                    SyncReason = "DATABASE_CREATED"
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"{cloudServerUrl}/api/DatabaseConfig/register-database",
                    request
                );

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Base criada sincronizada com sucesso: {DatabaseName}", database.Name);
                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            databaseId = database.Id,
                            name = database.Name,
                            fileSizeBytes = database.FileSizeBytes,
                            fileSizeFormatted = database.FileSizeBytes.HasValue ? FormatFileSize(database.FileSizeBytes.Value) : "N/A",
                            syncReason = "DATABASE_CREATED"
                        },
                        message = "Base criada sincronizada com o servidor cloud"
                    });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Falha ao sincronizar base criada: {DatabaseName} - {Error}",
                        database.Name, errorContent);
                    return StatusCode((int)response.StatusCode, new
                    {
                        success = false,
                        error = $"Erro ao sincronizar base criada: {response.StatusCode}",
                        details = errorContent
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro interno ao sincronizar base criada: {DatabaseId}", databaseId);
                return StatusCode(500, new
                {
                    success = false,
                    error = $"Erro interno: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Sincronizar base quando solicitada pelo servidor (cenário 2)
        /// </summary>
        [HttpPost("sync-database-requested/{databaseId}")]
        public async Task<IActionResult> SyncDatabaseRequested(string databaseId)
        {
            try
            {
                _logger.LogInformation("Sincronizando base solicitada pelo servidor: {DatabaseId}", databaseId);

                var database = await _configService.GetDatabaseByIdAsync(databaseId);
                if (database == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        error = $"Base de dados com ID '{databaseId}' não encontrada"
                    });
                }

                // Atualizar tamanho do arquivo
                await _configService.UpdateAllDatabaseFileSizesAsync();

                var cloudServerUrl = _configuration["CloudServer:Url"] ?? "https://localhost:7001";
                var machineId = GetMachineId();

                var request = new
                {
                    DesktopNodeId = machineId,
                    Name = database.Name,
                    Server = database.Server,
                    Database = database.Database,
                    User = database.Username,
                    Password = database.Password,
                    Port = database.Port,
                    Charset = database.Charset,
                    FileSizeBytes = database.FileSizeBytes,
                    LastSizeCheck = database.LastSizeCheck,
                    OperatingSystem = _systemInfoService.GetOperatingSystem(),
                    SystemVersion = _systemInfoService.GetSystemVersion(),
                    SyncReason = "SERVER_REQUESTED"
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"{cloudServerUrl}/api/DatabaseConfig/register-database",
                    request
                );

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Base solicitada sincronizada com sucesso: {DatabaseName}", database.Name);
                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            databaseId = database.Id,
                            name = database.Name,
                            fileSizeBytes = database.FileSizeBytes,
                            fileSizeFormatted = database.FileSizeBytes.HasValue ? FormatFileSize(database.FileSizeBytes.Value) : "N/A",
                            syncReason = "SERVER_REQUESTED"
                        },
                        message = "Base solicitada sincronizada com o servidor cloud"
                    });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Falha ao sincronizar base solicitada: {DatabaseName} - {Error}",
                        database.Name, errorContent);
                    return StatusCode((int)response.StatusCode, new
                    {
                        success = false,
                        error = $"Erro ao sincronizar base solicitada: {response.StatusCode}",
                        details = errorContent
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro interno ao sincronizar base solicitada: {DatabaseId}", databaseId);
                return StatusCode(500, new
                {
                    success = false,
                    error = $"Erro interno: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Sincronizar base após backup (cenário 3)
        /// </summary>
        [HttpPost("sync-database-after-backup/{databaseId}")]
        public async Task<IActionResult> SyncDatabaseAfterBackup(string databaseId)
        {
            try
            {
                _logger.LogInformation("Sincronizando base após backup: {DatabaseId}", databaseId);

                var database = await _configService.GetDatabaseByIdAsync(databaseId);
                if (database == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        error = $"Base de dados com ID '{databaseId}' não encontrada"
                    });
                }

                // Atualizar tamanho do arquivo (pode ter mudado após backup)
                await _configService.UpdateAllDatabaseFileSizesAsync();

                var cloudServerUrl = _configuration["CloudServer:Url"] ?? "https://localhost:7001";
                var machineId = GetMachineId();

                var request = new
                {
                    DesktopNodeId = machineId,
                    Name = database.Name,
                    Server = database.Server,
                    Database = database.Database,
                    User = database.Username,
                    Password = database.Password,
                    Port = database.Port,
                    Charset = database.Charset,
                    FileSizeBytes = database.FileSizeBytes,
                    LastSizeCheck = database.LastSizeCheck,
                    OperatingSystem = _systemInfoService.GetOperatingSystem(),
                    SystemVersion = _systemInfoService.GetSystemVersion(),
                    SyncReason = "AFTER_BACKUP"
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"{cloudServerUrl}/api/DatabaseConfig/register-database",
                    request
                );

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Base após backup sincronizada com sucesso: {DatabaseName}", database.Name);
                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            databaseId = database.Id,
                            name = database.Name,
                            fileSizeBytes = database.FileSizeBytes,
                            fileSizeFormatted = database.FileSizeBytes.HasValue ? FormatFileSize(database.FileSizeBytes.Value) : "N/A",
                            syncReason = "AFTER_BACKUP"
                        },
                        message = "Base após backup sincronizada com o servidor cloud"
                    });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Falha ao sincronizar base após backup: {DatabaseName} - {Error}",
                        database.Name, errorContent);
                    return StatusCode((int)response.StatusCode, new
                    {
                        success = false,
                        error = $"Erro ao sincronizar base após backup: {response.StatusCode}",
                        details = errorContent
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro interno ao sincronizar base após backup: {DatabaseId}", databaseId);
                return StatusCode(500, new
                {
                    success = false,
                    error = $"Erro interno: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Sincronizar todas as databases locais com o servidor cloud
        /// </summary>
        [HttpPost("sync-databases")]
        public async Task<IActionResult> SyncDatabasesToCloud()
        {
            try
            {
                _logger.LogInformation("🔄 Iniciando sincronização manual de databases com o servidor cloud");
                await SyncAllDatabasesToCloudAsync();
                
                return Ok(new { 
                    success = true, 
                    message = "Sincronização manual de databases concluída com sucesso" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro interno ao sincronizar databases");
                return StatusCode(500, new { 
                    success = false, 
                    error = $"Erro interno: {ex.Message}" 
                });
            }
        }

        /// <summary>
        /// Sincronizar databases via gRPC (método preferido)
        /// </summary>
        [HttpPost("sync-databases-grpc")]
        [SwaggerOperation(
            Summary = "Sincronizar databases via gRPC",
            Description = "Envia todas as databases locais para o servidor cloud via streaming gRPC (método preferido)."
        )]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SyncDatabasesViaGrpc()
        {
            try
            {
                if (!_commandStreamService.IsConnected)
                {
                    return BadRequest(new { 
                        success = false, 
                        error = "Streaming gRPC não está conectado. Inicie o streaming primeiro." 
                    });
                }

                _logger.LogInformation("🔄 Iniciando sincronização de databases via gRPC");

                var databases = await _configService.GetAllDatabasesAsync();
                
                if (!databases.Any())
                {
                    return Ok(new { 
                        success = true, 
                        message = "Nenhuma database local encontrada para sincronizar.",
                        databasesCount = 0
                    });
                }

                // Atualizar tamanhos dos arquivos antes de enviar
                await _configService.UpdateAllDatabaseFileSizesAsync();

                // Vincular databases ao nó atual se necessário
                var machineId = GetMachineId();
                await VinculateDatabasesToNodeAsync(machineId);

                // Enviar via gRPC
                await _commandStreamService.SendDatabaseSyncAsync("FULL_SYNC", "MANUAL_SYNC_VIA_GRPC", databases);

                _logger.LogInformation("✅ Sincronização de databases via gRPC concluída: {Count} databases", databases.Count);

                return Ok(new { 
                    success = true, 
                    message = $"Sincronização via gRPC concluída com sucesso. {databases.Count} databases enviadas.",
                    databasesCount = databases.Count,
                    method = "gRPC",
                    databases = databases.Select(db => new {
                        id = db.Id,
                        name = db.Name,
                        server = db.Server,
                        database = db.Database,
                        fileSizeBytes = db.FileSizeBytes,
                        fileSizeFormatted = db.FileSizeBytes.HasValue ? FormatFileSize(db.FileSizeBytes.Value) : "N/A",
                        desktopNodeId = db.DesktopNodeId
                    }).ToList(),
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao sincronizar databases via gRPC");
                return StatusCode(500, new { 
                    success = false, 
                    error = $"Erro interno: {ex.Message}",
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Verificar status das databases locais e se estão sincronizadas
        /// </summary>
        [HttpGet("database-status")]
        public async Task<IActionResult> GetDatabaseStatus()
        {
            try
            {
                var localDatabases = await _configService.GetAllDatabasesAsync();
                var machineId = GetMachineId();
                
                var status = new
                {
                    machineId = machineId,
                    localDatabasesCount = localDatabases.Count,
                    localDatabases = localDatabases.Select(db => new
                    {
                        id = db.Id,
                        name = db.Name,
                        server = db.Server,
                        database = db.Database,
                        fileSizeBytes = db.FileSizeBytes,
                        fileSizeFormatted = db.FileSizeBytes.HasValue ? FormatFileSize(db.FileSizeBytes.Value) : "N/A",
                        lastSizeCheck = db.LastSizeCheck,
                        createdAt = db.CreatedAt,
                        isActive = db.IsActive
                    }).ToList(),
                    timestamp = DateTime.UtcNow
                };

                return Ok(new { 
                    success = true, 
                    data = status,
                    message = $"Status das databases locais: {localDatabases.Count} databases encontradas"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao obter status das databases");
                return StatusCode(500, new { 
                    success = false, 
                    error = $"Erro interno: {ex.Message}" 
                });
            }
        }

        /// <summary>
        /// Buscar dados não sincronizados do servidor cloud
        /// </summary>
        [HttpGet("pull-data/{desktopNodeId}/{databaseId}")]
        public async Task<IActionResult> PullDataFromServer(string desktopNodeId, string databaseId, [FromQuery] DateTime? lastSyncTimestamp)
        {
            try
            {
                _logger.LogInformation("Buscando dados do servidor cloud para nó: {DesktopNodeId}", desktopNodeId);

                var cloudServerUrl = _configuration["CloudServer:Url"] ?? "https://localhost:7001";
                var url = $"{cloudServerUrl}/api/Sync/pull-data/{desktopNodeId}/{databaseId}";
                
                if (lastSyncTimestamp.HasValue)
                {
                    url += $"?lastSyncTimestamp={lastSyncTimestamp.Value:yyyy-MM-ddTHH:mm:ss.fffZ}";
                }

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<object>();
                    _logger.LogInformation("Dados obtidos do servidor cloud com sucesso");
                    return Ok(new { success = true, data = result });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Falha ao buscar dados do servidor cloud: {StatusCode} - {Error}", 
                        response.StatusCode, errorContent);
                    return StatusCode((int)response.StatusCode, new { 
                        success = false, 
                        error = $"Erro ao buscar dados: {response.StatusCode}" 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro interno ao buscar dados do servidor cloud");
                return StatusCode(500, new { 
                    success = false, 
                    error = $"Erro interno: {ex.Message}" 
                });
            }
        }

        /// <summary>
        /// Sincronizar dados via streaming gRPC (método preferido)
        /// </summary>
        [HttpPost("sync-via-streaming")]
        public async Task<IActionResult> SyncViaStreaming([FromBody] StreamingSyncRequest request)
        {
            try
            {
                if (!_commandStreamService.IsConnected)
                {
                    return BadRequest(new { 
                        success = false, 
                        error = "Streaming gRPC não está conectado. Inicie o streaming primeiro." 
                    });
                }

                _logger.LogInformation("Enviando dados de sincronização via streaming gRPC: {DataType}", request.DataType);

                // Criar comando de sincronização
                var syncCommand = new
                {
                    type = "SYNC_DATA",
                    dataType = request.DataType,
                    data = request.Data,
                    timestamp = DateTime.UtcNow,
                    machineId = GetMachineId()
                };

                // Enviar via streaming gRPC
                await _commandStreamService.SendResponseAsync(Guid.NewGuid().ToString(), syncCommand);

                _logger.LogInformation("Dados de sincronização enviados via streaming gRPC com sucesso");
                return Ok(new { 
                    success = true, 
                    message = "Dados enviados via streaming gRPC",
                    dataType = request.DataType,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar dados via streaming gRPC");
                return StatusCode(500, new { 
                    success = false, 
                    error = $"Erro interno: {ex.Message}" 
                });
            }
        }

        /// <summary>
        /// Enviar comando personalizado via streaming gRPC
        /// </summary>
        [HttpPost("send-command")]
        public async Task<IActionResult> SendCommand([FromBody] StreamingCommandRequest request)
        {
            try
            {
                if (!_commandStreamService.IsConnected)
                {
                    return BadRequest(new { 
                        success = false, 
                        error = "Streaming gRPC não está conectado. Inicie o streaming primeiro." 
                    });
                }

                _logger.LogInformation("Enviando comando via streaming gRPC: {CommandType}", request.CommandType);

                // Enviar comando via streaming gRPC
                await _commandStreamService.SendResponseAsync(request.CommandId ?? Guid.NewGuid().ToString(), request.CommandData);

                _logger.LogInformation("Comando enviado via streaming gRPC com sucesso");
                return Ok(new { 
                    success = true, 
                    message = "Comando enviado via streaming gRPC",
                    commandId = request.CommandId,
                    commandType = request.CommandType,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar comando via streaming gRPC");
                return StatusCode(500, new { 
                    success = false, 
                    error = $"Erro interno: {ex.Message}" 
                });
            }
        }

        /// <summary>
        /// Obter status detalhado da conexão de streaming
        /// </summary>
        [HttpGet("streaming-status")]
        public IActionResult GetStreamingStatus()
        {
            var status = _commandStreamService.GetConnectionStatus();
            return Ok(new
            {
                success = true,
                data = status,
                message = "Status da conexão gRPC obtido com sucesso"
            });
        }

        /// <summary>
        /// Verificar status de sincronização de databases
        /// </summary>
        [HttpGet("sync-status")]
        public async Task<IActionResult> GetSyncStatus()
        {
            try
            {
                var machineId = GetMachineId();
                var localDatabases = await _configService.GetAllDatabasesAsync();
                var cloudServerUrl = _configuration["CloudServer:Url"] ?? "https://localhost:7001";
                
                var status = new
                {
                    machineId = machineId,
                    localDatabasesCount = localDatabases.Count,
                    localDatabases = localDatabases.Select(db => new
                    {
                        id = db.Id,
                        name = db.Name,
                        server = db.Server,
                        database = db.Database,
                        fileSizeBytes = db.FileSizeBytes,
                        fileSizeFormatted = db.FileSizeBytes.HasValue ? FormatFileSize(db.FileSizeBytes.Value) : "N/A",
                        lastSizeCheck = db.LastSizeCheck,
                        createdAt = db.CreatedAt,
                        isActive = db.IsActive,
                        desktopNodeId = db.DesktopNodeId
                    }).ToList(),
                    grpcStreaming = new
                    {
                        isConnected = _commandStreamService.IsConnected,
                        connectionId = _commandStreamService.CurrentConnectionId,
                        lastSeenAt = _commandStreamService.LastSeenAt
                    },
                    cloudServer = new
                    {
                        url = cloudServerUrl,
                        isReachable = false // Será preenchido abaixo
                    },
                    timestamp = DateTime.UtcNow
                };

                // Verificar se o servidor cloud está acessível
                try
                {
                    var healthResponse = await _httpClient.GetAsync($"{cloudServerUrl}/health", 
                        new CancellationTokenSource(5000).Token);
                    status = status with { cloudServer = status.cloudServer with { isReachable = healthResponse.IsSuccessStatusCode } };
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("⚠️ Servidor cloud não está acessível: {Error}", ex.Message);
                }

                return Ok(new
                {
                    success = true,
                    data = status,
                    message = $"Status de sincronização: {localDatabases.Count} databases locais, gRPC: {(status.grpcStreaming.isConnected ? "Conectado" : "Desconectado")}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao verificar status de sincronização");
                return StatusCode(500, new
                {
                    success = false,
                    error = $"Erro interno: {ex.Message}",
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Forçar sincronização completa das databases (endpoint de teste)
        /// </summary>
        [HttpPost("force-sync-databases")]
        public async Task<IActionResult> ForceSyncDatabases()
        {
            try
            {
                _logger.LogInformation("🔄 Forçando sincronização completa das databases...");

                var machineId = GetMachineId();
                var localDatabases = await _configService.GetAllDatabasesAsync();

                if (!localDatabases.Any())
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Nenhuma database local encontrada para sincronizar",
                        databasesCount = 0,
                        timestamp = DateTime.UtcNow
                    });
                }

                // 1. Atualizar tamanhos dos arquivos
                await _configService.UpdateAllDatabaseFileSizesAsync();

                // 2. Vincular databases ao nó atual
                await VinculateDatabasesToNodeAsync(machineId);

                // 3. Tentar sincronização via gRPC primeiro
                if (_commandStreamService.IsConnected)
                {
                    try
                    {
                        _logger.LogInformation("📡 Tentando sincronização via gRPC...");
                        await _commandStreamService.SendDatabaseSyncAsync("FORCE_SYNC", "MANUAL_FORCE_SYNC", localDatabases);
                        
                        return Ok(new
                        {
                            success = true,
                            message = $"Sincronização forçada via gRPC concluída com sucesso. {localDatabases.Count} databases enviadas.",
                            databasesCount = localDatabases.Count,
                            method = "gRPC",
                            databases = localDatabases.Select(db => new
                            {
                                id = db.Id,
                                name = db.Name,
                                server = db.Server,
                                database = db.Database,
                                fileSizeBytes = db.FileSizeBytes,
                                fileSizeFormatted = db.FileSizeBytes.HasValue ? FormatFileSize(db.FileSizeBytes.Value) : "N/A",
                                desktopNodeId = db.DesktopNodeId
                            }).ToList(),
                            timestamp = DateTime.UtcNow
                        });
                    }
                    catch (Exception grpcEx)
                    {
                        _logger.LogError(grpcEx, "❌ Falha na sincronização via gRPC: {Error}", grpcEx.Message);
                        _logger.LogInformation("🔄 Tentando sincronização via HTTP como fallback...");
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ Streaming gRPC não está conectado, usando HTTP como fallback");
                }

                // 4. Fallback para HTTP
                var successCount = 0;
                var errorCount = 0;
                var errors = new List<string>();

                foreach (var database in localDatabases)
                {
                    try
                    {
                        await SendDatabaseToCloudAsync(database);
                        successCount++;
                        _logger.LogInformation("✅ Database sincronizada via HTTP: {DatabaseName}", database.Name);
                    }
                    catch (Exception dbEx)
                    {
                        errorCount++;
                        var errorMsg = $"Erro ao sincronizar {database.Name}: {dbEx.Message}";
                        errors.Add(errorMsg);
                        _logger.LogError(dbEx, "❌ {Error}", errorMsg);
                    }
                }

                return Ok(new
                {
                    success = errorCount == 0,
                    message = $"Sincronização forçada via HTTP concluída. {successCount} sucessos, {errorCount} erros.",
                    databasesCount = localDatabases.Count,
                    successCount = successCount,
                    errorCount = errorCount,
                    method = "HTTP_FALLBACK",
                    errors = errors,
                    databases = localDatabases.Select(db => new
                    {
                        id = db.Id,
                        name = db.Name,
                        server = db.Server,
                        database = db.Database,
                        fileSizeBytes = db.FileSizeBytes,
                        fileSizeFormatted = db.FileSizeBytes.HasValue ? FormatFileSize(db.FileSizeBytes.Value) : "N/A",
                        desktopNodeId = db.DesktopNodeId
                    }).ToList(),
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro interno ao forçar sincronização das databases");
                return StatusCode(500, new
                {
                    success = false,
                    error = $"Erro interno: {ex.Message}",
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Verificar status de conexão com o servidor cloud
        /// </summary>
        [HttpGet("connection-health")]
        public async Task<IActionResult> CheckConnectionHealth()
        {
            try
            {
                var machineId = GetMachineId();
                var cloudServerUrl = _configuration["CloudServer:Url"] ?? "https://localhost:7001";
                
                // Verificar se o streaming gRPC está ativo
                var grpcStatus = _commandStreamService.GetConnectionStatus();
                
                // Verificar se o servidor cloud está respondendo
                bool cloudServerReachable = false;
                try
                {
                    var healthResponse = await _httpClient.GetAsync($"{cloudServerUrl}/health", 
                        new CancellationTokenSource(5000).Token);
                    cloudServerReachable = healthResponse.IsSuccessStatusCode;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("⚠️ Servidor cloud não está acessível: {Error}", ex.Message);
                }

                var healthStatus = new
                {
                    machineId = machineId,
                    timestamp = DateTime.UtcNow,
                    grpcStreaming = new
                    {
                        isConnected = _commandStreamService.IsConnected,
                        connectionId = _commandStreamService.CurrentConnectionId,
                        lastSeenAt = _commandStreamService.LastSeenAt,
                        status = _commandStreamService.IsConnected ? "ONLINE" : "OFFLINE"
                    },
                    cloudServer = new
                    {
                        url = cloudServerUrl,
                        isReachable = cloudServerReachable,
                        status = cloudServerReachable ? "ONLINE" : "OFFLINE"
                    },
                    overallStatus = _commandStreamService.IsConnected && cloudServerReachable ? "HEALTHY" : "DEGRADED"
                };

                return Ok(new
                {
                    success = true,
                    data = healthStatus,
                    message = $"Status de conexão: {healthStatus.overallStatus}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao verificar status de conexão");
                return StatusCode(500, new
                {
                    success = false,
                    error = $"Erro interno: {ex.Message}",
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Forçar reconexão do streaming gRPC
        /// </summary>
        [HttpPost("reconnect-streaming")]
        public async Task<IActionResult> ReconnectStreaming()
        {
            try
            {
                _logger.LogInformation("🔄 Forçando reconexão do streaming gRPC...");

                var machineId = GetMachineId();
                var authToken = GetAuthTokenFromRequest();
                var connectionId = _commandStreamService.CurrentConnectionId ?? Guid.NewGuid().ToString();

                // Parar streaming atual se estiver ativo
                if (_commandStreamService.IsConnected)
                {
                    await _commandStreamService.StopStreamingAsync();
                    await Task.Delay(2000); // Aguardar 2 segundos
                }

                // Reiniciar streaming
                await _commandStreamService.StartStreamingAsync(connectionId, machineId, authToken, connectionId, null, null, null, null);

                if (_commandStreamService.IsConnected)
                {
                    _logger.LogInformation("✅ Reconexão do streaming gRPC bem-sucedida");
                    return Ok(new
                    {
                        success = true,
                        message = "Reconexão do streaming gRPC bem-sucedida",
                        connectionId = connectionId,
                        machineId = machineId,
                        timestamp = DateTime.UtcNow
                    });
                }
                else
                {
                    _logger.LogWarning("⚠️ Falha na reconexão do streaming gRPC");
                    return BadRequest(new
                    {
                        success = false,
                        error = "Falha na reconexão do streaming gRPC",
                        connectionId = connectionId,
                        machineId = machineId,
                        timestamp = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao reconectar streaming gRPC");
                return StatusCode(500, new
                {
                    success = false,
                    error = $"Erro interno: {ex.Message}",
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Vincular nó anônimo a um usuário autenticado (MÉTODO MELHORADO)
        /// </summary>
        [HttpPost("link-node-to-user")]
        public async Task<IActionResult> LinkNodeToUser([FromBody] LinkNodeToUserRequest request)
        {
            try
            {
                _logger.LogInformation("🔗 Vinculando nó a usuário: {NodeId}", request.NodeId);

                // Verificar se há token de autenticação
                var authToken = GetAuthTokenFromRequest();
                if (string.IsNullOrEmpty(authToken))
                {
                    return BadRequest(new { 
                        success = false, 
                        error = "Token de autenticação é obrigatório para vincular nó",
                        timestamp = DateTime.UtcNow
                    });
                }

                var machineId = GetMachineId();

                // 1. PRIMEIRO: Tentar vincular via API REST (método direto)
                try
                {
                    var cloudServerUrl = _configuration["CloudServer:Url"] ?? "https://localhost:7001";
                    
                    // Configurar headers de autenticação
                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");
                    
                    var bindResponse = await _httpClient.PostAsJsonAsync(
                        $"{cloudServerUrl}/api/User/bind-current-node",
                        new { machineId = machineId }
                    );

                    if (bindResponse.IsSuccessStatusCode)
                    {
                        var bindResult = await bindResponse.Content.ReadFromJsonAsync<object>();
                        _logger.LogInformation("✅ Nó vinculado com sucesso via API REST: {MachineId}", machineId);
                        
                        // 2. SEGUNDO: Notificar via streaming gRPC se conectado
                        if (_commandStreamService.IsConnected)
                        {
                            var linkCommand = new
                            {
                                type = "NODE_LINKED_TO_USER",
                                nodeId = request.NodeId,
                                machineId = machineId,
                                userId = "AUTHENTICATED",
                                timestamp = DateTime.UtcNow
                            };

                            await _commandStreamService.SendResponseAsync(Guid.NewGuid().ToString(), linkCommand);
                            _logger.LogInformation("📡 Notificação de vinculação enviada via streaming gRPC");
                        }

                        return Ok(new { 
                            success = true, 
                            message = "Nó vinculado ao usuário com sucesso",
                            nodeId = request.NodeId,
                            machineId = machineId,
                            method = "API_REST",
                            timestamp = DateTime.UtcNow
                        });
                    }
                    else
                    {
                        var errorContent = await bindResponse.Content.ReadAsStringAsync();
                        _logger.LogWarning("⚠️ Falha ao vincular via API REST: {StatusCode} - {Error}", 
                            bindResponse.StatusCode, errorContent);
                    }
                }
                catch (Exception apiEx)
                {
                    _logger.LogWarning("⚠️ Erro na vinculação via API REST, tentando via streaming: {Error}", apiEx.Message);
                }

                // 3. FALLBACK: Se API REST falhar, usar streaming gRPC
                if (_commandStreamService.IsConnected)
                {
                    var linkCommand = new
                    {
                        type = "LINK_NODE_TO_USER",
                        nodeId = request.NodeId,
                        machineId = machineId,
                        authToken = authToken,
                        timestamp = DateTime.UtcNow
                    };

                    await _commandStreamService.SendResponseAsync(Guid.NewGuid().ToString(), linkCommand);

                    _logger.LogInformation("📡 Comando de vinculação enviado via streaming gRPC (fallback)");
                    return Ok(new { 
                        success = true, 
                        message = "Comando de vinculação enviado via streaming gRPC (fallback)",
                        nodeId = request.NodeId,
                        machineId = machineId,
                        method = "STREAMING_GRPC",
                        timestamp = DateTime.UtcNow
                    });
                }
                else
                {
                    return BadRequest(new { 
                        success = false, 
                        error = "Streaming gRPC não está conectado e API REST falhou",
                        nodeId = request.NodeId,
                        machineId = machineId,
                        timestamp = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao vincular nó ao usuário");
                return StatusCode(500, new { 
                    success = false, 
                    error = $"Erro interno: {ex.Message}",
                    nodeId = request.NodeId,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Vincula todas as databases locais ao nó especificado
        /// </summary>
        private async Task VinculateDatabasesToNodeAsync(string machineId)
        {
            try
            {
                var localDatabases = await _configService.GetAllDatabasesAsync();
                
                foreach (var database in localDatabases)
                {
                    // Atualizar apenas se não estiver vinculada a nenhum nó
                    if (string.IsNullOrEmpty(database.DesktopNodeId))
                    {
                        database.DesktopNodeId = machineId;
                        await _configService.UpdateDatabaseAsync(database);
                        _logger.LogInformation("🔗 Database '{Name}' vinculada ao nó {MachineId}", database.Name, machineId);
                    }
                }
                
                _logger.LogInformation("✅ Todas as databases foram vinculadas ao nó {MachineId}", machineId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao vincular databases ao nó {MachineId}", machineId);
                throw;
            }
        }

        /// <summary>
        /// Sincroniza todas as databases locais com o servidor cloud (método interno)
        /// </summary>
        private async Task SyncAllDatabasesToCloudAsync()
        {
            try
            {
                var cloudServerUrl = _configuration["CloudServer:Url"] ?? "https://localhost:7001";
                var machineId = GetMachineId();

                // Verificar se o machineId é válido
                if (string.IsNullOrEmpty(machineId))
                {
                    _logger.LogWarning("⚠️ MachineId não está disponível para sincronização");
                    return;
                }

                // Obter todas as databases locais
                var localDatabases = await _configService.GetAllDatabasesAsync();
                
                if (!localDatabases.Any())
                {
                    _logger.LogInformation("ℹ️ Nenhuma database local encontrada para sincronizar");
                    return;
                }

                _logger.LogInformation("Sincronizando {Count} databases locais com o servidor cloud", localDatabases.Count);

                // Primeiro, tentar sincronização via gRPC se estiver conectado
                if (_commandStreamService.IsConnected)
                {
                    try
                    {
                        _logger.LogInformation("Sincronizando databases via gRPC (método preferido)");
                        await _commandStreamService.SendDatabaseSyncAsync("FULL_SYNC", "AUTO_SYNC_ON_NODE_REGISTRATION", localDatabases);
                        _logger.LogInformation("✅ Sincronização automática via gRPC concluída: {Count} databases", localDatabases.Count);
                        return; // Sucesso via gRPC, não precisa tentar HTTP
                    }
                    catch (Exception grpcEx)
                    {
                        _logger.LogError(grpcEx, "❌ Falha na sincronização via gRPC: {Error}", grpcEx.Message);
                        _logger.LogInformation("🔄 Tentando sincronização via HTTP como fallback...");
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ Streaming gRPC não está conectado, usando HTTP como fallback");
                }

                // Fallback para HTTP se gRPC não estiver disponível
                _logger.LogInformation("Sincronizando databases via HTTP (fallback)");

                // Atualizar tamanhos dos arquivos uma vez antes de enviar
                await _configService.UpdateAllDatabaseFileSizesAsync();

                foreach (var database in localDatabases)
                {
                    try
                    {
                        await SendDatabaseToCloudAsync(database);
                        _logger.LogInformation("✅ Database sincronizada via HTTP: {DatabaseName}", database.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Erro ao sincronizar database via HTTP: {DatabaseName}", database.Name);
                    }
                }

                _logger.LogInformation("Sincronização automática de databases via HTTP concluída");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro na sincronização automática de databases");
                throw;
            }
        }

        /// <summary>
        /// Obtém o MachineId correto para comunicação com o cloud
        /// </summary>
        private string GetMachineId()
        {
            return _machineIdService.GetMachineId();
        }

        /// <summary>
        /// Obtém o token de autenticação do cabeçalho da requisição
        /// </summary>
        private string? GetAuthTokenFromRequest()
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader != null && authHeader.StartsWith("Bearer "))
            {
                return authHeader.Substring("Bearer ".Length).Trim();
            }
            return null;
        }



        /// <summary>
        /// Envia uma database local para o servidor cloud
        /// </summary>
        private async Task SendDatabaseToCloudAsync(Models.DatabaseConfig databaseConfig)
        {
            try
            {
                var cloudServerUrl = _configuration["CloudServer:Url"] ?? "https://localhost:7001";
                var machineId = GetMachineId(); // Obter o MachineId correto

                // Primeiro, tentar enviar via gRPC se estiver conectado
                if (_commandStreamService.IsConnected)
                {
                    try
                    {
                        _logger.LogInformation("Enviando database via gRPC: {DatabaseName}", databaseConfig.Name);
                        await _commandStreamService.SendDatabaseSyncAsync("SINGLE_DATABASE", "DATABASE_CREATED", new List<Models.DatabaseConfig> { databaseConfig });
                        _logger.LogInformation("✅ Database enviada via gRPC com sucesso: {DatabaseName}", databaseConfig.Name);
                        return; // Sucesso via gRPC, não precisa tentar HTTP
                    }
                    catch (Exception grpcEx)
                    {
                        _logger.LogWarning("⚠️ Falha ao enviar via gRPC, tentando HTTP: {Error}", grpcEx.Message);
                    }
                }

                // Fallback para HTTP se gRPC não estiver disponível
                _logger.LogInformation("Enviando database via HTTP: {DatabaseName} -> {CloudUrl}", databaseConfig.Name, cloudServerUrl);



                // Obter informações do sistema
                var operatingSystem = _systemInfoService.GetOperatingSystem();
                var systemVersion = _systemInfoService.GetSystemVersion();

                var request = new
                {
                    DesktopNodeId = machineId,
                    Name = databaseConfig.Name,
                    Server = databaseConfig.Server,
                    Database = databaseConfig.Database,
                    User = databaseConfig.Username,
                    Password = databaseConfig.Password,
                    Port = databaseConfig.Port,
                    Charset = databaseConfig.Charset,
                    FileSizeBytes = databaseConfig.FileSizeBytes,
                    LastSizeCheck = databaseConfig.LastSizeCheck,
                    OperatingSystem = operatingSystem,
                    SystemVersion = systemVersion,
                    SyncReason = "DATABASE_CREATED"
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"{cloudServerUrl}/api/DatabaseConfig/register-database", 
                    request
                );

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<object>();
                    _logger.LogInformation("✅ Database enviada para o cloud via HTTP com sucesso: {DatabaseName} (Tamanho: {FileSize})", 
                        databaseConfig.Name, 
                        databaseConfig.FileSizeBytes.HasValue ? FormatFileSize(databaseConfig.FileSizeBytes.Value) : "N/A");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("⚠️ Falha ao enviar database para o cloud via HTTP: {StatusCode} - {Error}", 
                        response.StatusCode, errorContent);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning("⚠️ Erro de conexão ao enviar database para o cloud: {DatabaseName} - {Error}", 
                    databaseConfig.Name, ex.Message);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning("⚠️ Timeout ao enviar database para o cloud: {DatabaseName} - {Error}", 
                    databaseConfig.Name, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro inesperado ao enviar database para o cloud: {DatabaseName}", databaseConfig.Name);
            }
        }

        /// <summary>
        /// Formata o tamanho do arquivo em formato legível
        /// </summary>
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }

    // DTOs para as requisições
    public class DesktopNodeRegistration
    {
        public string? NodeId { get; set; } // ID único do nó (opcional, será gerado se não fornecido)
        public string Name { get; set; } = string.Empty;
        public string? MachineName { get; set; } // Nome da máquina (ex: DESKTOP-78ROEFC)
        public string MachineId { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; } = 8000;
        public string? DatabasePath { get; set; }
        public string? Version { get; set; }
        public string? OperatingSystem { get; set; }
    }

    public class InitialSyncRequest
    {
        public string DesktopNodeId { get; set; } = string.Empty;
        public string DatabaseId { get; set; } = string.Empty;
    }

    public class SyncDataBatchRequest
    {
        public string DesktopNodeId { get; set; } = string.Empty;
        public string DatabaseId { get; set; } = string.Empty;
        public List<SyncDataItem> DataBatch { get; set; } = new List<SyncDataItem>();
    }

    public class SyncDataItem
    {
        public string TableName { get; set; } = string.Empty;
        public object Data { get; set; } = new object();
    }

    public class SyncCompleteRequest
    {
        public string OperationId { get; set; } = string.Empty;
        public bool Success { get; set; } = true;
        public string? ErrorMessage { get; set; }
    }

    public class LinkNodeToUserRequest
    {
        public string NodeId { get; set; } = string.Empty;
    }



    public class AnonymousNodeResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string MachineId { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; }
        public string? DatabasePath { get; set; }
        public string? Version { get; set; }
        public string? OperatingSystem { get; set; }
        public bool IsAnonymous { get; set; }
        public string AnonymousToken { get; set; } = string.Empty;
        public DateTime AnonymousExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastSeen { get; set; }
    }

    public class StreamingSyncRequest
    {
        public string DataType { get; set; } = string.Empty;
        public object Data { get; set; } = new object();
    }

    public class StreamingCommandRequest
    {
        public string? CommandId { get; set; }
        public string CommandType { get; set; } = string.Empty;
        public object CommandData { get; set; } = new object();
    }
}
