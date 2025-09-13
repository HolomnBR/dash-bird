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
        private readonly ILogger<SnapshotController> _logger;

        public SnapshotController(ISqliteStorageService sqliteStorageService, ILogger<SnapshotController> logger)
        {
            _sqliteStorageService = sqliteStorageService;
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
        /// Salvar snapshot
        /// </summary>
        [HttpPost("save-snapshot")]
        public Task<IActionResult> SaveSnapshot([FromBody] DatabaseSnapshot snapshot)
        {
            try
            {
                if (snapshot == null)
                {
                    return Task.FromResult<IActionResult>(BadRequest(new { error = "Snapshot é obrigatório" }));
                }

                // O snapshot já é salvo automaticamente quando gerado
                // Este endpoint pode ser usado para atualizações futuras
                return Task.FromResult<IActionResult>(Ok(new { success = true, message = "Snapshot salvo com sucesso" }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar snapshot");
                return Task.FromResult<IActionResult>(StatusCode(500, new { error = "Erro interno do servidor" }));
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
        /// Iniciar geração de snapshot com progresso
        /// </summary>
        [HttpPost("start-generation/{databaseId}")]
        public async Task<IActionResult> StartSnapshotGeneration(string databaseId)
        {
            try
            {
                var snapshotId = await _sqliteStorageService.StartSnapshotGenerationAsync(databaseId);
                return Ok(new { snapshotId, message = "Geração de snapshot iniciada" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao iniciar geração de snapshot");
                return StatusCode(500, new { error = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Obter progresso de geração de snapshot
        /// </summary>
        [HttpGet("progress/{snapshotId}")]
        public async Task<IActionResult> GetSnapshotProgress(string snapshotId)
        {
            try
            {
                var progress = await _sqliteStorageService.GetSnapshotProgressAsync(snapshotId);
                
                if (progress == null)
                {
                    return NotFound(new { error = "Snapshot não encontrado" });
                }

                return Ok(new { 
                    snapshotId = progress.Id,
                    databaseId = progress.DatabaseId,
                    databaseName = progress.DatabaseName,
                    totalTables = progress.TotalTables,
                    processedTables = progress.ProcessedTables,
                    currentTable = progress.CurrentTable,
                    progressPercentage = progress.ProgressPercentage,
                    status = progress.Status,
                    startedAt = progress.StartedAt,
                    completedAt = progress.CompletedAt,
                    errorMessage = progress.ErrorMessage,
                    estimatedCompletion = progress.EstimatedCompletion
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter progresso do snapshot");
                return StatusCode(500, new { error = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Obter snapshot em cache (otimizado)
        /// </summary>
        [HttpGet("cached/{databaseId}")]
        public async Task<IActionResult> GetCachedSnapshot(string databaseId)
        {
            try
            {
                var snapshot = await _sqliteStorageService.GetCachedSnapshotAsync(databaseId);
                
                if (snapshot == null)
                {
                    return NotFound(new { error = "Snapshot em cache não encontrado" });
                }

                return Ok(new { snapshot });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter snapshot em cache");
                return StatusCode(500, new { error = "Erro interno do servidor" });
            }
        }
    }
}
