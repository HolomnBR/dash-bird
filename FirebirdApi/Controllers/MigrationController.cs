using Microsoft.AspNetCore.Mvc;
using FirebirdApi.Services;
using FirebirdApi.Models;
using System.Text.Json;
using System.IO;

namespace FirebirdApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MigrationController : ControllerBase
    {
        private readonly ISqliteStorageService _sqliteStorageService;
        private readonly IDatabaseConfigService _databaseConfigService;
        private readonly ILogger<MigrationController> _logger;

        public MigrationController(
            ISqliteStorageService sqliteStorageService,
            IDatabaseConfigService databaseConfigService,
            ILogger<MigrationController> logger)
        {
            _sqliteStorageService = sqliteStorageService;
            _databaseConfigService = databaseConfigService;
            _logger = logger;
        }

        /// <summary>
        /// Migrar dados dos arquivos JSON para SQLite
        /// </summary>
        [HttpPost("migrate-to-sqlite")]
        public async Task<IActionResult> MigrateToSqlite()
        {
            try
            {
                var databasesMigrated = 0;
                var errors = new List<string>();

                // Migrar configurações de banco de dados
                var databases = await _databaseConfigService.GetAllDatabasesAsync();
                foreach (var database in databases)
                {
                    try
                    {
                        await _sqliteStorageService.AddDatabaseAsync(database);
                        databasesMigrated++;
                    }
                    catch (Exception ex)
                    {
                        var error = $"Erro ao migrar database {database.Name}: {ex.Message}";
                        errors.Add(error);
                        _logger.LogError(ex, "Erro ao migrar database {DatabaseName}", database.Name);
                    }
                }


                _logger.LogInformation("Migração concluída: {Databases} databases", databasesMigrated);

                return Ok(new
                {
                    success = true,
                    message = "Migração concluída com sucesso",
                    data = new { databasesMigrated, errors }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante migração para SQLite");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Erro interno durante migração",
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// Limpar arquivos JSON antigos após migração
        /// </summary>
        [HttpPost("cleanup-json-files")]
        public Task<IActionResult> CleanupJsonFiles()
        {
            try
            {
                var filesRemoved = 0;
                var cleanupErrors = new List<string>();

                // Remover arquivo de token JSON
                var dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DashBird");
                var tokenFilePath = Path.Combine(dataDir, "auth_token.json");
                if (System.IO.File.Exists(tokenFilePath))
                {
                    try
                    {
                        System.IO.File.Delete(tokenFilePath);
                        filesRemoved++;
                        _logger.LogInformation("Arquivo de token JSON removido: {FilePath}", tokenFilePath);
                    }
                    catch (Exception ex)
                    {
                        var error = $"Erro ao remover arquivo de token: {ex.Message}";
                        cleanupErrors.Add(error);
                        _logger.LogError(ex, "Erro ao remover arquivo de token");
                    }
                }

                // Remover arquivo de configurações de database JSON
                var configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database-configs.json");
                if (System.IO.File.Exists(configFilePath))
                {
                    try
                    {
                        System.IO.File.Delete(configFilePath);
                        filesRemoved++;
                        _logger.LogInformation("Arquivo de configurações JSON removido: {FilePath}", configFilePath);
                    }
                    catch (Exception ex)
                    {
                        var error = $"Erro ao remover arquivo de configurações: {ex.Message}";
                        cleanupErrors.Add(error);
                        _logger.LogError(ex, "Erro ao remover arquivo de configurações");
                    }
                }

                return Task.FromResult<IActionResult>(Ok(new
                {
                    success = true,
                    message = "Limpeza de arquivos JSON concluída",
                    data = new { filesRemoved, errors = cleanupErrors }
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante limpeza de arquivos JSON");
                return Task.FromResult<IActionResult>(StatusCode(500, new
                {
                    success = false,
                    error = "Erro interno durante limpeza",
                    details = ex.Message
                }));
            }
        }
    }
}
