using Microsoft.AspNetCore.Mvc;
using FirebirdApi.Services;
using FirebirdApi.Models;

namespace FirebirdApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SnapshotController : ControllerBase
    {
        private readonly ISqliteStorageService _sqliteStorageService;
        private readonly IDatabaseConfigService _databaseConfigService;
        private readonly ILogger<SnapshotController> _logger;
        private static readonly Dictionary<string, Task<DatabaseSnapshot>> _generationTasks = new();

        public SnapshotController(
            ISqliteStorageService sqliteStorageService, 
            IDatabaseConfigService databaseConfigService,
            ILogger<SnapshotController> logger)
        {
            _sqliteStorageService = sqliteStorageService;
            _databaseConfigService = databaseConfigService;
            _logger = logger;
        }

        /// <summary>
        /// Obter snapshots (versão simplificada - apenas metadados)
        /// </summary>
        [HttpGet("get-snapshots")]
        public async Task<IActionResult> GetSnapshots([FromQuery] string? databaseId = null, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Iniciando obtenção de snapshots para database {DatabaseId}", databaseId ?? "todas");
                
                // Usar o token de cancelamento do request
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(10)); // Timeout de apenas 10 segundos
                
                var snapshots = await _sqliteStorageService.GetSnapshotsAsync(databaseId);
                
                _logger.LogInformation("Snapshots obtidos com sucesso: {Count} snapshots", snapshots.Count);
                return Ok(new { 
                    snapshots, 
                    count = snapshots.Count,
                    databaseId = databaseId,
                    timestamp = DateTime.UtcNow,
                    message = "Snapshots obtidos (versão simplificada - use get-snapshot/{id} para dados completos)"
                });
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Operação de obter snapshots foi cancelada para database {DatabaseId}", databaseId ?? "todas");
                return StatusCode(408, new { error = "Operação cancelada por timeout", databaseId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter snapshots para database {DatabaseId}", databaseId ?? "todas");
                return StatusCode(500, new { error = "Erro interno do servidor", databaseId });
            }
        }

        /// <summary>
        /// Obter snapshot por ID
        /// </summary>
        [HttpGet("get-snapshot/{snapshotId}")]
        public async Task<IActionResult> GetSnapshot(string snapshotId)
        {
            try
            {
                var snapshot = await _sqliteStorageService.GetSnapshotByIdAsync(snapshotId);
                
                if (snapshot == null)
                {
                    return NotFound(new { error = "Snapshot não encontrado" });
                }

                return Ok(new { snapshot });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter snapshot");
                return StatusCode(500, new { error = "Erro interno do servidor" });
            }
        }


        /// <summary>
        /// Deletar snapshot
        /// </summary>
        [HttpDelete("delete-snapshot/{snapshotId}")]
        public async Task<IActionResult> DeleteSnapshot(string snapshotId)
        {
            try
            {
                var success = await _sqliteStorageService.DeleteSnapshotAsync(snapshotId);
                
                if (!success)
                {
                    return NotFound(new { error = "Snapshot não encontrado" });
                }

                return Ok(new { success = true, message = "Snapshot deletado com sucesso" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao deletar snapshot");
                return StatusCode(500, new { error = "Erro interno do servidor" });
            }
        }



        /// <summary>
        /// Gerar snapshot de uma base de dados (assíncrono em background)
        /// </summary>
        [HttpPost("generate/{databaseId}")]
        public async Task<IActionResult> GenerateSnapshot(string databaseId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(databaseId))
                {
                    return BadRequest(new { success = false, message = "ID da base é obrigatório" });
                }

                // Verificar se a base existe
                var database = await _databaseConfigService.GetDatabaseByIdAsync(databaseId);
                if (database == null)
                {
                    return NotFound(new { success = false, message = "Base de dados não encontrada" });
                }

                // Verificar se já existe uma geração em andamento
                if (_generationTasks.ContainsKey(databaseId))
                {
                    return Ok(new { 
                        success = true, 
                        message = "Geração de snapshot já está em andamento para esta base",
                        databaseId = databaseId,
                        databaseName = database.Name
                    });
                }

                // Iniciar geração em background
                var generationTask = Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogInformation("Iniciando geração de snapshot em background para base: {DatabaseName}", database.Name);
                        var snapshot = await _databaseConfigService.GenerateDatabaseSnapshotAsync(databaseId);
                        _logger.LogInformation("Snapshot gerado com sucesso para base: {DatabaseName}", database.Name);
                        return snapshot;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao gerar snapshot em background para base: {DatabaseName}", database.Name);
                        throw;
                    }
                    finally
                    {
                        // Remover da lista de tarefas em andamento
                        _generationTasks.Remove(databaseId);
                    }
                });

                _generationTasks[databaseId] = generationTask;

                return Ok(new { 
                    success = true, 
                    message = "Geração de snapshot iniciada em background",
                    databaseId = databaseId,
                    databaseName = database.Name
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao iniciar geração de snapshot");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Verificar status da geração de snapshot
        /// </summary>
        [HttpGet("generation-status/{databaseId}")]
        public IActionResult GetGenerationStatus(string databaseId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(databaseId))
                {
                    return BadRequest(new { success = false, message = "ID da base é obrigatório" });
                }

                var isGenerating = _generationTasks.ContainsKey(databaseId);
                var task = isGenerating ? _generationTasks[databaseId] : null;
                var isCompleted = task?.IsCompleted ?? false;
                var isFaulted = task?.IsFaulted ?? false;

                return Ok(new { 
                    success = true,
                    databaseId = databaseId,
                    isGenerating = isGenerating && !isCompleted,
                    isCompleted = isCompleted,
                    isFaulted = isFaulted,
                    hasError = isFaulted ? task?.Exception?.GetBaseException()?.Message : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar status da geração");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}
