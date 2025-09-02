using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using FirebirdApi.Services;
using FirebirdApi.Models;

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

        public SyncController(HttpClient httpClient, IConfiguration configuration, ILogger<SyncController> logger, IDatabaseConfigService configService, IMachineIdService machineIdService)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _configService = configService;
            _machineIdService = machineIdService;
        }

        /// <summary>
        /// Registrar nó desktop no servidor cloud (método legado)
        /// </summary>
        [HttpPost("register-node")]
        public async Task<IActionResult> RegisterNode([FromBody] DesktopNodeRegistration request)
        {
            try
            {
                _logger.LogInformation("Registrando nó desktop no servidor cloud: {MachineId}", request.MachineId);

                // Armazenar o MachineId recebido do Electron
                _machineIdService.SetMachineId(request.MachineId);
                _logger.LogInformation("MachineId armazenado localmente: {MachineId}", request.MachineId);

                var cloudServerUrl = _configuration["CloudServer:Url"] ?? "https://localhost:7001";
                var response = await _httpClient.PostAsJsonAsync(
                    $"{cloudServerUrl}/api/DesktopNode/register", 
                    request
                );

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<object>();
                    _logger.LogInformation("Nó registrado com sucesso no servidor cloud");
                    return Ok(new { success = true, data = result });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Falha ao registrar nó no servidor cloud: {StatusCode} - {Error}", 
                        response.StatusCode, errorContent);
                    return StatusCode((int)response.StatusCode, new { 
                        success = false, 
                        error = $"Erro ao registrar no servidor cloud: {response.StatusCode}" 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro interno ao registrar nó no servidor cloud");
                return StatusCode(500, new { 
                    success = false, 
                    error = $"Erro interno: {ex.Message}" 
                });
            }
        }

        /// <summary>
        /// Registrar nó anônimo no servidor cloud
        /// </summary>
        [HttpPost("register-anonymous-node")]
        public async Task<IActionResult> RegisterAnonymousNode([FromBody] AnonymousNodeRegisterRequest request)
        {
            try
            {
                _logger.LogInformation("Registrando nó anônimo no servidor cloud: {MachineId}", request.MachineId);

                // Armazenar o MachineId recebido do Electron
                _machineIdService.SetMachineId(request.MachineId);
                _logger.LogInformation("MachineId armazenado localmente: {MachineId}", request.MachineId);

                var cloudServerUrl = _configuration["CloudServer:Url"] ?? "https://localhost:7001";
                var response = await _httpClient.PostAsJsonAsync(
                    $"{cloudServerUrl}/api/AnonymousNode/register", 
                    request
                );

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<AnonymousNodeResponse>();
                    _logger.LogInformation("Nó anônimo registrado com sucesso no servidor cloud");
                    return Ok(new { success = true, data = result });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Falha ao registrar nó anônimo no servidor cloud: {StatusCode} - {Error}", 
                        response.StatusCode, errorContent);
                    return StatusCode((int)response.StatusCode, new { 
                        success = false, 
                        error = $"Erro ao registrar nó anônimo no servidor cloud: {response.StatusCode}" 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro interno ao registrar nó anônimo no servidor cloud");
                return StatusCode(500, new { 
                    success = false, 
                    error = $"Erro interno: {ex.Message}" 
                });
            }
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
        /// Sincronizar todas as databases locais com o servidor cloud
        /// </summary>
        [HttpPost("sync-databases")]
        public async Task<IActionResult> SyncDatabasesToCloud()
        {
            try
            {
                _logger.LogInformation("Iniciando sincronização de databases com o servidor cloud");

                var cloudServerUrl = _configuration["CloudServer:Url"] ?? "https://localhost:7001";
                var machineId = GetMachineId();

                // Obter todas as databases locais
                var localDatabases = await _configService.GetAllDatabasesAsync();
                var syncResults = new List<object>();

                foreach (var database in localDatabases)
                {
                    try
                    {
                        var request = new
                        {
                            DesktopNodeId = machineId,
                            Name = database.Name,
                            Server = database.Server,
                            Database = database.Database,
                            User = database.Username,
                            Password = database.Password,
                            Port = database.Port,
                            Charset = database.Charset
                        };

                        var response = await _httpClient.PostAsJsonAsync(
                            $"{cloudServerUrl}/api/DatabaseConfig/register-database", 
                            request
                        );

                        if (response.IsSuccessStatusCode)
                        {
                            syncResults.Add(new { 
                                databaseId = database.Id, 
                                name = database.Name, 
                                status = "success" 
                            });
                            _logger.LogInformation("Database sincronizada: {DatabaseName}", database.Name);
                        }
                        else
                        {
                            var errorContent = await response.Content.ReadAsStringAsync();
                            syncResults.Add(new { 
                                databaseId = database.Id, 
                                name = database.Name, 
                                status = "error", 
                                error = errorContent 
                            });
                            _logger.LogWarning("Falha ao sincronizar database: {DatabaseName} - {Error}", 
                                database.Name, errorContent);
                        }
                    }
                    catch (Exception ex)
                    {
                        syncResults.Add(new { 
                            databaseId = database.Id, 
                            name = database.Name, 
                            status = "error", 
                            error = ex.Message 
                        });
                        _logger.LogError(ex, "Erro ao sincronizar database: {DatabaseName}", database.Name);
                    }
                }

                return Ok(new { 
                    success = true, 
                    data = syncResults, 
                    message = $"Sincronização concluída. {syncResults.Count(r => r.GetType().GetProperty("status")?.GetValue(r)?.ToString() == "success")} databases sincronizadas com sucesso." 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro interno ao sincronizar databases");
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
        /// Obtém o MachineId correto para comunicação com o cloud
        /// </summary>
        private string GetMachineId()
        {
            return _machineIdService.GetMachineId();
        }
    }

    // DTOs para as requisições
    public class DesktopNodeRegistration
    {
        public string Name { get; set; } = string.Empty;
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
}
