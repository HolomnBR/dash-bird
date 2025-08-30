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

        public DatabaseConfigController(IDatabaseConfigService configService)
        {
            _configService = configService;
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
