using Microsoft.EntityFrameworkCore;
using FirebirdApi.Data;
using FirebirdApi.Models;
using System.Text.Json;

namespace FirebirdApi.Services
{
    public interface ISnapshotService
    {
        Task<string> StartSnapshotGenerationAsync(string databaseId);
        Task<DatabaseSnapshotSqlite?> GetSnapshotProgressAsync(string snapshotId);
        Task<bool> UpdateSnapshotProgressAsync(string snapshotId, int processedTables, string? currentTable, string status, string? errorMessage = null);
        Task<DatabaseSnapshot?> CompleteSnapshotAsync(string snapshotId);
        Task<List<DatabaseSnapshotSqlite>> GetActiveSnapshotsAsync(string? databaseId = null);
        Task<bool> CancelSnapshotAsync(string snapshotId);
        Task<DatabaseSnapshot?> GetCachedSnapshotAsync(string databaseId);
        Task<bool> CacheSnapshotAsync(string databaseId, DatabaseSnapshot snapshot);
    }

    public class SnapshotService : ISnapshotService
    {
        private readonly LocalDbContext _context;
        private readonly ILogger<SnapshotService> _logger;
        private readonly IDatabaseConfigService _databaseConfigService;

        public SnapshotService(
            LocalDbContext context,
            ILogger<SnapshotService> logger,
            IDatabaseConfigService databaseConfigService)
        {
            _context = context;
            _logger = logger;
            _databaseConfigService = databaseConfigService;
        }

        public async Task<string> StartSnapshotGenerationAsync(string databaseId)
        {
            try
            {
                var database = await _databaseConfigService.GetDatabaseByIdAsync(databaseId);
                if (database == null)
                    throw new InvalidOperationException($"Base de dados com ID '{databaseId}' não encontrada");

                // Verificar se já existe um snapshot em progresso para esta base
                var existingSnapshot = await _context.DatabaseSnapshots
                    .FirstOrDefaultAsync(s => s.DatabaseId == databaseId && 
                                            (s.Status == "Starting" || s.Status == "InProgress"));

                if (existingSnapshot != null)
                {
                    _logger.LogWarning("Já existe um snapshot em progresso para a base {DatabaseId}: {SnapshotId}", 
                        databaseId, existingSnapshot.Id);
                    return existingSnapshot.Id;
                }

                var snapshotId = Guid.NewGuid().ToString();
                var snapshot = new DatabaseSnapshotSqlite
                {
                    Id = snapshotId,
                    DatabaseId = databaseId,
                    DatabaseName = database.Name,
                    Status = "Starting",
                    StartedAt = DateTime.UtcNow,
                    TotalTables = 0,
                    ProcessedTables = 0,
                    ProgressPercentage = 0,
                    IsActive = true
                };

                _context.DatabaseSnapshots.Add(snapshot);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Iniciada geração de snapshot {SnapshotId} para base {DatabaseId}", 
                    snapshotId, databaseId);

                return snapshotId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao iniciar geração de snapshot para base {DatabaseId}", databaseId);
                throw;
            }
        }

        public async Task<DatabaseSnapshotSqlite?> GetSnapshotProgressAsync(string snapshotId)
        {
            try
            {
                return await _context.DatabaseSnapshots
                    .FirstOrDefaultAsync(s => s.Id == snapshotId && s.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter progresso do snapshot {SnapshotId}", snapshotId);
                return null;
            }
        }

        public async Task<bool> UpdateSnapshotProgressAsync(string snapshotId, int processedTables, string? currentTable, string status, string? errorMessage = null)
        {
            try
            {
                var snapshot = await _context.DatabaseSnapshots
                    .FirstOrDefaultAsync(s => s.Id == snapshotId && s.IsActive);

                if (snapshot == null)
                    return false;

                snapshot.ProcessedTables = processedTables;
                snapshot.CurrentTable = currentTable;
                snapshot.Status = status;
                snapshot.ErrorMessage = errorMessage;

                // Calcular porcentagem de progresso
                if (snapshot.TotalTables > 0)
                {
                    snapshot.ProgressPercentage = Math.Round((decimal)processedTables / snapshot.TotalTables * 100, 2);
                }

                // Estimar conclusão se em progresso
                if (status == "InProgress" && snapshot.ProcessedTables > 0)
                {
                    var elapsed = DateTime.UtcNow - snapshot.StartedAt;
                    var avgTimePerTable = elapsed.TotalMilliseconds / snapshot.ProcessedTables;
                    var remainingTables = snapshot.TotalTables - snapshot.ProcessedTables;
                    var estimatedRemaining = TimeSpan.FromMilliseconds(avgTimePerTable * remainingTables);
                    snapshot.EstimatedCompletion = DateTime.UtcNow.Add(estimatedRemaining);
                }

                // Marcar como concluído se processou todas as tabelas
                if (status == "InProgress" && snapshot.ProcessedTables >= snapshot.TotalTables)
                {
                    snapshot.Status = "Completed";
                    snapshot.CompletedAt = DateTime.UtcNow;
                    snapshot.ProgressPercentage = 100;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar progresso do snapshot {SnapshotId}", snapshotId);
                return false;
            }
        }

        public async Task<DatabaseSnapshot?> CompleteSnapshotAsync(string snapshotId)
        {
            try
            {
                var snapshot = await _context.DatabaseSnapshots
                    .FirstOrDefaultAsync(s => s.Id == snapshotId && s.IsActive);

                if (snapshot == null)
                    return null;

                // Deserializar dados do snapshot
                if (string.IsNullOrEmpty(snapshot.SnapshotData))
                {
                    _logger.LogError("Snapshot {SnapshotId} não possui dados serializados", snapshotId);
                    return null;
                }

                var databaseSnapshot = JsonSerializer.Deserialize<DatabaseSnapshot>(snapshot.SnapshotData);
                return databaseSnapshot;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao completar snapshot {SnapshotId}", snapshotId);
                return null;
            }
        }

        public async Task<List<DatabaseSnapshotSqlite>> GetActiveSnapshotsAsync(string? databaseId = null)
        {
            try
            {
                var query = _context.DatabaseSnapshots
                    .Where(s => s.IsActive);

                if (!string.IsNullOrEmpty(databaseId))
                {
                    query = query.Where(s => s.DatabaseId == databaseId);
                }

                return await query
                    .OrderByDescending(s => s.GeneratedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter snapshots ativos");
                return new List<DatabaseSnapshotSqlite>();
            }
        }

        public async Task<bool> CancelSnapshotAsync(string snapshotId)
        {
            try
            {
                var snapshot = await _context.DatabaseSnapshots
                    .FirstOrDefaultAsync(s => s.Id == snapshotId && s.IsActive);

                if (snapshot == null)
                    return false;

                snapshot.Status = "Cancelled";
                snapshot.CompletedAt = DateTime.UtcNow;
                snapshot.ErrorMessage = "Snapshot cancelado pelo usuário";

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao cancelar snapshot {SnapshotId}", snapshotId);
                return false;
            }
        }

        public async Task<DatabaseSnapshot?> GetCachedSnapshotAsync(string databaseId)
        {
            try
            {
                var cache = await _context.SnapshotCache
                    .FirstOrDefaultAsync(c => c.DatabaseId == databaseId && 
                                            c.IsActive && 
                                            c.ExpiresAt > DateTime.UtcNow);

                if (cache == null)
                    return null;

                return JsonSerializer.Deserialize<DatabaseSnapshot>(cache.CachedData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter snapshot em cache para base {DatabaseId}", databaseId);
                return null;
            }
        }

        public async Task<bool> CacheSnapshotAsync(string databaseId, DatabaseSnapshot snapshot)
        {
            try
            {
                // Remover cache existente
                var existingCache = await _context.SnapshotCache
                    .Where(c => c.DatabaseId == databaseId && c.IsActive)
                    .ToListAsync();

                foreach (var cache in existingCache)
                {
                    cache.IsActive = false;
                }

                // Criar novo cache
                var cacheKey = $"snapshot_{databaseId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
                var cacheData = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
                var expiresAt = DateTime.UtcNow.AddHours(24); // Cache por 24 horas

                var newCache = new SnapshotCache
                {
                    DatabaseId = databaseId,
                    CacheKey = cacheKey,
                    CachedData = cacheData,
                    CachedAt = DateTime.UtcNow,
                    ExpiresAt = expiresAt,
                    IsActive = true
                };

                _context.SnapshotCache.Add(newCache);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Snapshot em cache para base {DatabaseId} expira em {ExpiresAt}", 
                    databaseId, expiresAt);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao cachear snapshot para base {DatabaseId}", databaseId);
                return false;
            }
        }
    }
}
