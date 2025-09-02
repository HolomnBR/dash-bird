using Microsoft.AspNetCore.Mvc;
using FirebirdApi.Services;
using FirebirdApi.Models;
using System.IO;
using Swashbuckle.AspNetCore.Annotations;
// filters removed

namespace FirebirdApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DatabaseConfigController : ControllerBase
    {
        private readonly IDatabaseConfigService _configService;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseConfigController> _logger;
        private readonly IMachineIdService _machineIdService;

        public DatabaseConfigController(
            IDatabaseConfigService configService,
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<DatabaseConfigController> logger,
            IMachineIdService machineIdService)
        {
            _configService = configService;
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _machineIdService = machineIdService;
        }

        /// <summary>
        /// Lista todas as bases de dados configuradas
        /// </summary>
        /// <remarks>Retorna apenas bases ativas. Bases removidas (soft delete) não aparecem.</remarks>
        [HttpGet("databases")]
        [SwaggerOperation(
            Summary = "Listar bases de dados configuradas",
            Description = "Lista todas as bases de dados ativas presentes nas configurações (base e dinâmica)."
        )]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ListDatabases()
        {
            try
            {
                var databases = await _configService.GetAllDatabasesAsync();
                return Ok(new { success = true, data = databases });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Adiciona uma nova base de dados
        /// </summary>
        [HttpPost("databases")]
        [SwaggerOperation(
            Summary = "Adicionar nova base de dados",
            Description = "Adiciona uma nova configuração de base. Apenas o campo 'database' é obrigatório; demais campos possuem valores padrão.")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddDatabase([FromBody] AddDatabaseRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Database))
                {
                    return BadRequest(new { success = false, message = "Caminho do arquivo da base é obrigatório" });
                }

                // Criar objeto DatabaseConfig com valores padrão
                var config = new DatabaseConfig
                {
                    Database = request.Database,
                    Name = !string.IsNullOrWhiteSpace(request.Name) 
                        ? request.Name 
                        : Path.GetFileNameWithoutExtension(request.Database),
                    Server = !string.IsNullOrWhiteSpace(request.Server) 
                        ? request.Server 
                        : "localhost",
                    Username = !string.IsNullOrWhiteSpace(request.Username) 
                        ? request.Username 
                        : "SYSDBA",
                    Password = !string.IsNullOrWhiteSpace(request.Password) 
                        ? request.Password 
                        : "masterkey",
                    Port = request.Port ?? 3050,
                    Charset = !string.IsNullOrWhiteSpace(request.Charset) 
                        ? request.Charset 
                        : "UTF8",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                var addedDatabase = await _configService.AddDatabaseAsync(config);
                
                // Enviar automaticamente para o cloud se configurado
                await SendDatabaseToCloudAsync(addedDatabase);
                
                return Ok(new { success = true, data = addedDatabase, message = "Base de dados adicionada com sucesso" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Remove uma base de dados (apenas da configuração)
        /// </summary>
        [HttpDelete("databases/{id}")]
        [SwaggerOperation(Summary = "Remover base de dados", Description = "Soft delete: desativa a base de dados nas configurações.")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RemoveDatabase(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return BadRequest(new { success = false, message = "ID da base é obrigatório" });
                }

                var removed = await _configService.RemoveDatabaseAsync(id);
                if (removed)
                {
                    return Ok(new { success = true, message = "Base de dados removida com sucesso" });
                }
                else
                {
                    return NotFound(new { success = false, message = "Base de dados não encontrada ou não pode ser removida" });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Testa a conexão com uma base de dados específica
        /// </summary>
        [HttpGet("databases/{id}/test-connection")]
        [SwaggerOperation(Summary = "Testar conexão de uma base", Description = "Valida se a conexão abre com sucesso para o ID informado.")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> TestConnection(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return BadRequest(new { success = false, message = "ID da base é obrigatório" });
                }

                var isConnected = await _configService.TestConnectionAsync(id);
                var database = await _configService.GetDatabaseByIdAsync(id);
                
                if (database == null)
                {
                    return NotFound(new { success = false, message = "Base de dados não encontrada" });
                }

                var result = new DatabaseConnectionTest
                {
                    Success = isConnected,
                    Message = isConnected ? "Conexão bem-sucedida!" : "Falha na conexão",
                    DatabaseId = id,
                    TestedAt = DateTime.UtcNow
                };

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lista todas as tabelas de uma base de dados específica
        /// </summary>
        [HttpGet("databases/{id}/tables")]
        [SwaggerOperation(Summary = "Listar tabelas de uma base", Description = "Retorna metadados das tabelas (nome, schema, tipo e descrição).")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ListTables(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return BadRequest(new { success = false, message = "ID da base é obrigatório" });
                }

                var database = await _configService.GetDatabaseByIdAsync(id);
                if (database == null)
                {
                    return NotFound(new { success = false, message = "Base de dados não encontrada" });
                }

                var tables = await _configService.GetTablesAsync(id);
                return Ok(new { success = true, data = tables, database = database.Name });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

		/// <summary>
		/// Retorna o schema (colunas, tipos, PK) de uma ou mais tabelas
		/// </summary>
		[HttpPost("databases/{id}/table-schema")]
		[SwaggerOperation(Summary = "Obter schema de tabelas", Description = "Recebe uma lista de nomes de tabelas e retorna suas colunas e chaves primárias.")]
		[ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
		public async Task<IActionResult> GetTableSchema(string id, [FromBody] TableSchemaRequest request)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(id))
				{
					return BadRequest(new { success = false, message = "ID da base é obrigatório" });
				}

				var database = await _configService.GetDatabaseByIdAsync(id);
				if (database == null)
				{
					return NotFound(new { success = false, message = "Base de dados não encontrada" });
				}

				if (request == null || request.TableNames == null || request.TableNames.Count == 0)
				{
					return BadRequest(new { success = false, message = "Informe ao menos um nome de tabela" });
				}

				var schemas = await _configService.GetTableSchemaAsync(id, request.TableNames);
				return Ok(new { success = true, data = schemas, database = database.Name });
			}
			catch (Exception ex)
			{
				return BadRequest(new { success = false, message = ex.Message });
			}
		}

		/// <summary>
		/// Gera um snapshot completo da base de dados com todas as tabelas, schemas, contadores e lastIds
		/// </summary>
		[HttpPost("databases/{id}/generate-snapshot")]
		[SwaggerOperation(Summary = "Gerar snapshot da base", Description = "Gera um snapshot completo com todas as tabelas, schemas, contadores de registros e lastIds. Salva localmente em JSON.")]
		[ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
		public async Task<IActionResult> GenerateDatabaseSnapshot(string id)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(id))
				{
					return BadRequest(new { success = false, message = "ID da base é obrigatório" });
				}

				var database = await _configService.GetDatabaseByIdAsync(id);
				if (database == null)
				{
					return NotFound(new { success = false, message = "Base de dados não encontrada" });
				}

				var snapshot = await _configService.GenerateDatabaseSnapshotAsync(id);
				return Ok(new { 
					success = true, 
					data = snapshot, 
					message = $"Snapshot gerado com sucesso e salvo localmente. Total de tabelas processadas: {snapshot.Tables.Count}" 
				});
			}
			catch (Exception ex)
			{
				return BadRequest(new { success = false, message = ex.Message });
			}
		}

        /// <summary>
        /// Atualiza uma configuração de base de dados existente
        /// </summary>
        [HttpPut("databases/{id}")]
        [SwaggerOperation(Summary = "Atualizar configuração de base", Description = "Atualiza os campos de uma base já configurada pelo seu ID.")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateDatabase(string id, [FromBody] DatabaseConfig config)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return BadRequest(new { success = false, message = "ID da base é obrigatório" });
                }

                config.Id = id;
                var updated = await _configService.UpdateDatabaseAsync(config);
                
                if (updated)
                {
                    var updatedDatabase = await _configService.GetDatabaseByIdAsync(id);
                    return Ok(new { success = true, data = updatedDatabase, message = "Base de dados atualizada com sucesso" });
                }
                else
                {
                    return NotFound(new { success = false, message = "Base de dados não encontrada" });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtém uma configuração de base de dados específica
        /// </summary>
        [HttpGet("databases/{id}")]
        [SwaggerOperation(Summary = "Obter configuração de base", Description = "Busca a configuração completa pelo ID informado.")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetDatabase(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return BadRequest(new { success = false, message = "ID da base é obrigatório" });
                }

                var database = await _configService.GetDatabaseByIdAsync(id);
                if (database == null)
                {
                    return NotFound(new { success = false, message = "Base de dados não encontrada" });
                }

                return Ok(new { success = true, data = database });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtém a base de dados padrão
        /// </summary>
        [HttpGet("default-database")]
        [SwaggerOperation(Summary = "Obter base padrão", Description = "Retorna a configuração marcada como padrão no projeto.")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDefaultDatabase()
        {
            try
            {
                var database = await _configService.GetDefaultDatabaseAsync();
                if (database == null)
                {
                    return NotFound(new { success = false, message = "Nenhuma base de dados padrão configurada" });
                }

                return Ok(new { success = true, data = database });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Define uma base de dados como padrão
        /// </summary>
        [HttpPost("default-database")]
        [SwaggerOperation(Summary = "Definir base padrão", Description = "Define uma base existente como padrão para as operações.")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SetDefaultDatabase([FromBody] SetDefaultDatabaseRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.DatabaseId))
                {
                    return BadRequest(new { success = false, message = "ID da base é obrigatório" });
                }

                var set = await _configService.SetDefaultDatabaseAsync(request.DatabaseId);
                if (set)
                {
                    var database = await _configService.GetDatabaseByIdAsync(request.DatabaseId);
                    return Ok(new { success = true, data = database, message = "Base de dados definida como padrão com sucesso" });
                }
                else
                {
                    return NotFound(new { success = false, message = "Base de dados não encontrada" });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtém as configurações do projeto
        /// </summary>
        [HttpGet("project-config")]
        public async Task<IActionResult> GetProjectConfig()
        {
            try
            {
                var config = await _configService.GetProjectConfigAsync();
                return Ok(new { success = true, data = config });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Envia uma database local para o servidor cloud
        /// </summary>
        private async Task SendDatabaseToCloudAsync(DatabaseConfig databaseConfig)
        {
            try
            {
                var cloudServerUrl = _configuration["CloudServer:Url"] ?? "https://localhost:7001";
                var machineId = GetMachineId(); // Obter o MachineId correto

                // Primeiro, verificar se o servidor cloud está disponível
                try
                {
                    var healthCheck = await _httpClient.GetAsync($"{cloudServerUrl}/status");
                    if (!healthCheck.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("Servidor cloud não está disponível. Pulando envio da database: {DatabaseName}", databaseConfig.Name);
                        return;
                    }
                }
                catch
                {
                    _logger.LogWarning("Não foi possível conectar ao servidor cloud. Pulando envio da database: {DatabaseName}", databaseConfig.Name);
                    return;
                }

                var request = new
                {
                    DesktopNodeId = machineId,
                    Name = databaseConfig.Name,
                    Server = databaseConfig.Server,
                    Database = databaseConfig.Database,
                    User = databaseConfig.Username,
                    Password = databaseConfig.Password,
                    Port = databaseConfig.Port,
                    Charset = databaseConfig.Charset
                };

                _logger.LogInformation("Enviando database para o cloud: {DatabaseName} -> {CloudUrl}", databaseConfig.Name, cloudServerUrl);

                var response = await _httpClient.PostAsJsonAsync(
                    $"{cloudServerUrl}/api/DatabaseConfig/register-database", 
                    request
                );

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<object>();
                    _logger.LogInformation("Database enviada para o cloud com sucesso: {DatabaseName}", databaseConfig.Name);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Falha ao enviar database para o cloud: {StatusCode} - {Error}", 
                        response.StatusCode, errorContent);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning("Erro de conexão ao enviar database para o cloud: {DatabaseName} - {Error}", 
                    databaseConfig.Name, ex.Message);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning("Timeout ao enviar database para o cloud: {DatabaseName} - {Error}", 
                    databaseConfig.Name, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao enviar database para o cloud: {DatabaseName}", databaseConfig.Name);
            }
        }

        /// <summary>
        /// Testa a conectividade com o servidor cloud
        /// </summary>
        [HttpGet("test-cloud-connection")]
        [SwaggerOperation(
            Summary = "Testar conexão com servidor cloud",
            Description = "Verifica se o servidor cloud está disponível e acessível."
        )]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> TestCloudConnection()
        {
            try
            {
                var cloudServerUrl = _configuration["CloudServer:Url"] ?? "https://localhost:7001";
                
                var response = await _httpClient.GetAsync($"{cloudServerUrl}/status");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return Ok(new { 
                        success = true, 
                        message = "Conexão com servidor cloud estabelecida com sucesso",
                        cloudUrl = cloudServerUrl,
                        response = content
                    });
                }
                else
                {
                    return BadRequest(new { 
                        success = false, 
                        message = $"Servidor cloud retornou status: {response.StatusCode}",
                        cloudUrl = cloudServerUrl
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { 
                    success = false, 
                    message = $"Erro ao conectar com servidor cloud: {ex.Message}",
                    cloudUrl = _configuration["CloudServer:Url"] ?? "https://localhost:7001"
                });
            }
        }

        /// <summary>
        /// Sincroniza todas as databases locais com o servidor cloud
        /// </summary>
        [HttpPost("sync-all-databases")]
        [SwaggerOperation(
            Summary = "Sincronizar todas as databases com o cloud",
            Description = "Envia todas as databases locais para o servidor cloud."
        )]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SyncAllDatabases()
        {
            try
            {
                var databases = await _configService.GetAllDatabasesAsync();
                var results = new List<object>();

                foreach (var database in databases)
                {
                    try
                    {
                        await SendDatabaseToCloudAsync(database);
                        results.Add(new { 
                            databaseId = database.Id, 
                            name = database.Name, 
                            status = "success" 
                        });
                    }
                    catch (Exception ex)
                    {
                        results.Add(new { 
                            databaseId = database.Id, 
                            name = database.Name, 
                            status = "error", 
                            error = ex.Message 
                        });
                    }
                }

                return Ok(new { 
                    success = true, 
                    data = results, 
                    message = $"Sincronização concluída. {results.Count(r => r.GetType().GetProperty("status")?.GetValue(r)?.ToString() == "success")} databases processadas." 
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
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

    public class SetDefaultDatabaseRequest
    {
        /// <summary>ID da base a ser definida como padrão.</summary>
        public string DatabaseId { get; set; } = string.Empty;
    }

	public class TableSchemaRequest
	{
		/// <summary>Lista de nomes de tabelas. Ex.: ["USERS", "ORDERS"]</summary>
		public List<string> TableNames { get; set; } = new List<string>();
	}
}
