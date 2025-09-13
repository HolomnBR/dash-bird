using Microsoft.EntityFrameworkCore;
using FirebirdApi.Data;
using FirebirdApi.Models;
using System.Text.Json;

namespace FirebirdApi.Services
{
    public interface ISqliteStorageService
    {
        // Token operations
        Task<string?> GetStoredTokenAsync();
        Task StoreTokenAsync(string token, string userId, string? userEmail = null);
        Task ClearTokenAsync();
        Task<bool> HasValidTokenAsync();

        // Database config operations
        Task<List<DatabaseConfig>> GetAllDatabasesAsync();
        Task<DatabaseConfig?> GetDatabaseByIdAsync(string id);
        Task<DatabaseConfig> AddDatabaseAsync(DatabaseConfig config);
        Task<bool> RemoveDatabaseAsync(string id);
        Task<bool> UpdateDatabaseAsync(DatabaseConfig config);
        Task<DatabaseConfig?> GetDefaultDatabaseAsync();
        Task<bool> SetDefaultDatabaseAsync(string id);
        Task<bool> UpdateAllDatabaseFileSizesAsync();

        // Snapshot operations
        Task<DatabaseSnapshot> GenerateDatabaseSnapshotAsync(string id);
        Task<string> StartSnapshotGenerationAsync(string id);
        Task<DatabaseSnapshotSqlite?> GetSnapshotProgressAsync(string snapshotId);
        Task<bool> UpdateSnapshotProgressAsync(string snapshotId, int processedTables, string? currentTable, string status, string? errorMessage = null);
        Task<List<DatabaseSnapshot>> GetSnapshotsAsync(string? databaseId = null);
        Task<DatabaseSnapshot?> GetSnapshotByIdAsync(string snapshotId);
        Task<bool> DeleteSnapshotAsync(string snapshotId);
        Task<DatabaseSnapshot?> GetCachedSnapshotAsync(string databaseId);
        Task<bool> CacheSnapshotAsync(string databaseId, DatabaseSnapshot snapshot);

        // Project settings
        Task<ProjectConfig> GetProjectConfigAsync();
        Task<bool> UpdateProjectConfigAsync(ProjectConfig config);
    }

    public class SqliteStorageService : ISqliteStorageService
    {
        private readonly LocalDbContext _context;
        private readonly ILogger<SqliteStorageService> _logger;
        private readonly IDatabaseConfigService _databaseConfigService;
        private readonly ISnapshotService _snapshotService;

        public SqliteStorageService(
            LocalDbContext context, 
            ILogger<SqliteStorageService> logger,
            IDatabaseConfigService databaseConfigService,
            ISnapshotService snapshotService)
        {
            _context = context;
            _logger = logger;
            _databaseConfigService = databaseConfigService;
            _snapshotService = snapshotService;
        }

        #region Token Operations

        public async Task<string?> GetStoredTokenAsync()
        {
            try
            {
                var token = await _context.AuthTokens
                    .Where(t => t.IsActive && t.ExpiresAt > DateTime.UtcNow)
                    .OrderByDescending(t => t.StoredAt)
                    .FirstOrDefaultAsync();

                if (token != null)
                {
                    _logger.LogInformation("Token carregado do SQLite");
                    return token.Token;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter token armazenado");
                return null;
            }
        }

        public async Task StoreTokenAsync(string token, string userId, string? userEmail = null)
        {
            try
            {
                // Desativar tokens anteriores
                await _context.AuthTokens
                    .Where(t => t.IsActive)
                    .ExecuteUpdateAsync(t => t.SetProperty(x => x.IsActive, false));

                // Decodificar JWT para obter expiração
                var expiresAt = GetTokenExpiration(token);

                var authToken = new AuthToken
                {
                    Token = token,
                    UserId = userId,
                    UserEmail = userEmail,
                    StoredAt = DateTime.UtcNow,
                    ExpiresAt = expiresAt,
                    IsActive = true
                };

                _context.AuthTokens.Add(authToken);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Token armazenado no SQLite até {ExpiresAt}", expiresAt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao armazenar token");
            }
        }

        public async Task ClearTokenAsync()
        {
            try
            {
                await _context.AuthTokens
                    .Where(t => t.IsActive)
                    .ExecuteUpdateAsync(t => t.SetProperty(x => x.IsActive, false));

                await _context.SaveChangesAsync();
                _logger.LogInformation("Token removido do SQLite");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao limpar token");
            }
        }

        public async Task<bool> HasValidTokenAsync()
        {
            var token = await GetStoredTokenAsync();
            return !string.IsNullOrEmpty(token);
        }

        #endregion

        #region Database Config Operations

        public async Task<List<DatabaseConfig>> GetAllDatabasesAsync()
        {
            try
            {
                var configs = await _context.DatabaseConfigs
                    .Where(db => db.IsActive)
                    .OrderBy(db => db.Name)
                    .ToListAsync();

                return configs.Select(MapToDatabaseConfig).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter configurações de banco");
                return new List<DatabaseConfig>();
            }
        }

        public async Task<DatabaseConfig?> GetDatabaseByIdAsync(string id)
        {
            try
            {
                var config = await _context.DatabaseConfigs
                    .FirstOrDefaultAsync(db => db.Id == id && db.IsActive);

                return config != null ? MapToDatabaseConfig(config) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter configuração de banco por ID");
                return null;
            }
        }

        public async Task<DatabaseConfig> AddDatabaseAsync(DatabaseConfig config)
        {
            try
            {
                // Gerar GUID único se não for a base padrão
                if (config.Id != "default")
                {
                    config.Id = Guid.NewGuid().ToString();
                }

                config.CreatedAt = DateTime.UtcNow;
                config.IsActive = true;

                // Capturar tamanho do arquivo da base
                await UpdateDatabaseFileSizeAsync(config);

                var sqliteConfig = MapToDatabaseConfigSqlite(config);

                // Verificar se já existe uma configuração com o mesmo ID
                var existingConfig = await _context.DatabaseConfigs
                    .FirstOrDefaultAsync(db => db.Id == config.Id);

                if (existingConfig != null)
                {
                    // Atualizar a configuração existente
                    _context.Entry(existingConfig).CurrentValues.SetValues(sqliteConfig);
                }
                else
                {
                    _context.DatabaseConfigs.Add(sqliteConfig);
                }

                await _context.SaveChangesAsync();
                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao adicionar configuração de banco");
                throw;
            }
        }

        public async Task<bool> RemoveDatabaseAsync(string id)
        {
            try
            {
                // Não permitir remover a base padrão
                if (id == "default")
                {
                    return false;
                }

                var database = await _context.DatabaseConfigs
                    .FirstOrDefaultAsync(db => db.Id == id);

                if (database != null)
                {
                    database.IsActive = false;
                    await _context.SaveChangesAsync();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao remover configuração de banco");
                return false;
            }
        }

        public async Task<bool> UpdateDatabaseAsync(DatabaseConfig config)
        {
            try
            {
                var existingDatabase = await _context.DatabaseConfigs
                    .FirstOrDefaultAsync(db => db.Id == config.Id);

                if (existingDatabase != null)
                {
                    // Atualizar tamanho do arquivo se o caminho da base mudou
                    if (existingDatabase.Database != config.Database)
                    {
                        await UpdateDatabaseFileSizeAsync(config);
                    }

                    var sqliteConfig = MapToDatabaseConfigSqlite(config);
                    _context.Entry(existingDatabase).CurrentValues.SetValues(sqliteConfig);
                    await _context.SaveChangesAsync();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar configuração de banco");
                return false;
            }
        }

        public async Task<DatabaseConfig?> GetDefaultDatabaseAsync()
        {
            try
            {
                var settings = await _context.ProjectSettingsSqlite.FirstOrDefaultAsync();
                if (settings?.DefaultDatabaseId != null)
                {
                    return await GetDatabaseByIdAsync(settings.DefaultDatabaseId);
                }

                // Se não há configuração padrão, retornar a primeira ativa
                var firstConfig = await _context.DatabaseConfigs
                    .Where(db => db.IsActive)
                    .OrderBy(db => db.CreatedAt)
                    .FirstOrDefaultAsync();

                return firstConfig != null ? MapToDatabaseConfig(firstConfig) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter banco padrão");
                return null;
            }
        }

        public async Task<bool> SetDefaultDatabaseAsync(string id)
        {
            try
            {
                var database = await GetDatabaseByIdAsync(id);
                if (database == null)
                {
                    return false;
                }

                var settings = await _context.ProjectSettingsSqlite.FirstOrDefaultAsync();
                if (settings == null)
                {
                    settings = new ProjectSettingsSqlite();
                    _context.ProjectSettingsSqlite.Add(settings);
                }

                settings.DefaultDatabaseId = id;
                settings.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao definir banco padrão");
                return false;
            }
        }

        public async Task<bool> UpdateAllDatabaseFileSizesAsync()
        {
            try
            {
                var databases = await _context.DatabaseConfigs
                    .Where(db => db.IsActive)
                    .ToListAsync();

                bool hasChanges = false;

                foreach (var database in databases)
                {
                    var oldSize = database.FileSizeBytes;
                    await UpdateDatabaseFileSizeAsync(MapToDatabaseConfig(database));
                    
                    if (oldSize != database.FileSizeBytes)
                    {
                        hasChanges = true;
                    }
                }

                if (hasChanges)
                {
                    await _context.SaveChangesAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar tamanhos de todas as bases");
                return false;
            }
        }

        #endregion

        #region Snapshot Operations

        public async Task<DatabaseSnapshot> GenerateDatabaseSnapshotAsync(string id)
        {
            var database = await GetDatabaseByIdAsync(id);
            if (database == null)
                throw new InvalidOperationException($"Base de dados com ID '{id}' não encontrada");

            try
            {
                // Verificar se existe um snapshot em cache válido
                var cachedSnapshot = await GetCachedSnapshotAsync(id);
                if (cachedSnapshot != null)
                {
                    _logger.LogInformation("Snapshot em cache encontrado para base {DatabaseId}", id);
                    return cachedSnapshot;
                }

                // Iniciar geração de snapshot com progresso
                var snapshotId = await StartSnapshotGenerationAsync(id);
                var snapshot = await GenerateSnapshotWithProgressAsync(id, snapshotId);

                // Cachear o snapshot para otimização
                await CacheSnapshotAsync(id, snapshot);

                return snapshot;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar snapshot da base de dados");
                throw;
            }
        }

        public async Task<string> StartSnapshotGenerationAsync(string id)
        {
            return await _snapshotService.StartSnapshotGenerationAsync(id);
        }

        public async Task<DatabaseSnapshotSqlite?> GetSnapshotProgressAsync(string snapshotId)
        {
            return await _snapshotService.GetSnapshotProgressAsync(snapshotId);
        }

        public async Task<bool> UpdateSnapshotProgressAsync(string snapshotId, int processedTables, string? currentTable, string status, string? errorMessage = null)
        {
            return await _snapshotService.UpdateSnapshotProgressAsync(snapshotId, processedTables, currentTable, status, errorMessage);
        }

        public async Task<DatabaseSnapshot?> GetCachedSnapshotAsync(string databaseId)
        {
            return await _snapshotService.GetCachedSnapshotAsync(databaseId);
        }

        public async Task<bool> CacheSnapshotAsync(string databaseId, DatabaseSnapshot snapshot)
        {
            return await _snapshotService.CacheSnapshotAsync(databaseId, snapshot);
        }

        private async Task<DatabaseSnapshot> GenerateSnapshotWithProgressAsync(string databaseId, string snapshotId)
        {
            var database = await GetDatabaseByIdAsync(databaseId);
            if (database == null)
                throw new InvalidOperationException($"Base de dados com ID '{databaseId}' não encontrada");

            try
            {
                // Log de início sem update no banco
                _logger.LogInformation("Iniciando geração de snapshot {SnapshotId} para database {DatabaseId}", snapshotId, databaseId);

                var snapshot = new DatabaseSnapshot
                {
                    DatabaseId = database.Id,
                    DatabaseName = database.Name,
                    GeneratedAt = DateTime.UtcNow,
                    Tables = new List<TableFullInfo>()
                };

                // Listar todas as tabelas
                var tables = await _databaseConfigService.GetTablesAsync(databaseId);
                var totalTables = tables.Count;

                // Atualizar total de tabelas
                var sqliteSnapshot = await _context.DatabaseSnapshots
                    .FirstOrDefaultAsync(s => s.Id == snapshotId);
                if (sqliteSnapshot != null)
                {
                    sqliteSnapshot.TotalTables = totalTables;
                    await _context.SaveChangesAsync();
                }

                int processedTables = 0;

                // Processar tabelas sequencialmente para evitar sobrecarga do banco
                foreach (var table in tables)
                {
                    try
                    {
                        var tableFullInfo = new TableFullInfo
                        {
                            TableName = table.TableName,
                            Schema = table.Schema,
                            TableType = table.TableType,
                            Description = table.Description,
                            Columns = new List<ColumnSchema>(),
                            RecordCount = 0,
                            LastId = null,
                            GeneratedAt = DateTime.UtcNow
                        };

                        // Recuperar schema da tabela
                        var tableSchemas = await _databaseConfigService.GetTableSchemaAsync(databaseId, new[] { table.TableName });
                        if (tableSchemas.Any())
                        {
                            tableFullInfo.Columns = tableSchemas.First().Columns;
                        }

                        // Contar quantidade de registros com timeout
                        using var connection = new FirebirdSql.Data.FirebirdClient.FbConnection(BuildConnectionString(database));
                        await connection.OpenAsync();
                        
                        var countQuery = $"SELECT COUNT(*) FROM \"{table.TableName}\"";
                        using var countCmd = new FirebirdSql.Data.FirebirdClient.FbCommand(countQuery, connection);
                        countCmd.CommandTimeout = 30; // Timeout de 30 segundos
                        
                        try
                        {
                            var recordCount = await countCmd.ExecuteScalarAsync();
                            if (recordCount != null && recordCount != DBNull.Value)
                            {
                                tableFullInfo.RecordCount = Convert.ToInt64(recordCount);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Erro ao contar registros da tabela {TableName}", table.TableName);
                            tableFullInfo.RecordCount = 0; // Definir como 0 se não conseguir contar
                        }

                        // Pegar o lastId (procurar por colunas de chave primária ou colunas ID) com timeout
                        try
                        {
                            var pkColumns = await LoadPrimaryKeyColumnsAsync(connection, table.TableName);
                            if (pkColumns.Any())
                            {
                                var pkColumn = pkColumns.First();
                                var lastIdQuery = $"SELECT MAX(\"{pkColumn}\") FROM \"{table.TableName}\"";
                                using var lastIdCmd = new FirebirdSql.Data.FirebirdClient.FbCommand(lastIdQuery, connection);
                                lastIdCmd.CommandTimeout = 15; // Timeout de 15 segundos
                                var lastId = await lastIdCmd.ExecuteScalarAsync();
                                if (lastId != null && lastId != DBNull.Value)
                                {
                                    tableFullInfo.LastId = Convert.ToInt64(lastId);
                                }
                            }
                            else
                            {
                                var idColumns = tableFullInfo.Columns
                                    .Where(c => c.ColumnName.ToUpperInvariant().Contains("ID") || 
                                              c.ColumnName.ToUpperInvariant().Contains("CODIGO") ||
                                              c.ColumnName.ToUpperInvariant().Contains("COD"))
                                    .ToList();
                                
                                if (idColumns.Any())
                                {
                                    var idColumn = idColumns.First();
                                    var lastIdQuery = $"SELECT MAX(\"{idColumn.ColumnName}\") FROM \"{table.TableName}\"";
                                    using var lastIdCmd = new FirebirdSql.Data.FirebirdClient.FbCommand(lastIdQuery, connection);
                                    lastIdCmd.CommandTimeout = 15; // Timeout de 15 segundos
                                    var lastId = await lastIdCmd.ExecuteScalarAsync();
                                    if (lastId != null && lastId != DBNull.Value)
                                    {
                                        tableFullInfo.LastId = Convert.ToInt64(lastId);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Erro ao obter LastId da tabela {TableName}", table.TableName);
                            tableFullInfo.LastId = null; // Definir como null se não conseguir obter
                        }

                        snapshot.Tables.Add(tableFullInfo);
                        processedTables++;

                        _logger.LogDebug("Processada tabela {TableName} ({ProcessedTables}/{TotalTables})", 
                            table.TableName, processedTables, totalTables);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erro ao processar tabela {TableName}", table.TableName);
                        // Continuar com as outras tabelas mesmo se uma falhar
                    }
                }

                // Salvar snapshot completo no SQLite
                var snapshotData = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
                
                var finalSnapshot = await _context.DatabaseSnapshots
                    .FirstOrDefaultAsync(s => s.Id == snapshotId);
                
                if (finalSnapshot != null)
                {
                    finalSnapshot.SnapshotData = snapshotData;
                    finalSnapshot.GeneratedAt = DateTime.UtcNow;
                    finalSnapshot.Status = "Completed";
                    finalSnapshot.CompletedAt = DateTime.UtcNow;
                    finalSnapshot.ProgressPercentage = 100;
                    finalSnapshot.ProcessedTables = processedTables;
                    finalSnapshot.CurrentTable = null;
                }

                // Salvar tabelas e colunas
                foreach (var table in snapshot.Tables)
                {
                    var snapshotTable = new SnapshotTable
                    {
                        SnapshotId = snapshotId,
                        TableName = table.TableName,
                        Schema = table.Schema,
                        TableType = table.TableType,
                        Description = table.Description,
                        RecordCount = table.RecordCount,
                        LastId = table.LastId,
                        GeneratedAt = table.GeneratedAt
                    };

                    _context.SnapshotTables.Add(snapshotTable);

                    foreach (var column in table.Columns)
                    {
                        var snapshotColumn = new SnapshotTableColumn
                        {
                            SnapshotId = snapshotId,
                            TableName = table.TableName,
                            ColumnName = column.ColumnName,
                            DataType = column.DataType,
                            Length = column.Length,
                            Precision = column.Precision,
                            Scale = column.Scale,
                            IsNullable = column.IsNullable,
                            DefaultValue = column.Default,
                            Description = column.Description,
                            IsPrimaryKey = column.IsPrimaryKey
                        };

                        _context.SnapshotTableColumns.Add(snapshotColumn);
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Snapshot gerado e salvo no SQLite: {SnapshotId} ({ProcessedTables}/{TotalTables} tabelas)", 
                    snapshotId, processedTables, totalTables);

                return snapshot;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar snapshot da base de dados {DatabaseId}", databaseId);
                throw;
            }
        }

        private string BuildConnectionString(DatabaseConfig config)
        {
            return $"Server={config.Server};" +
                   $"Port={config.Port};" +
                   $"Database={config.Database};" +
                   $"User={config.Username};" +
                   $"Password={config.Password};" +
                   $"Charset={config.Charset};" +
                   "Dialect=3;";
        }

        private async Task<HashSet<string>> LoadPrimaryKeyColumnsAsync(FirebirdSql.Data.FirebirdClient.FbConnection connection, string tableName)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var query = @"
                SELECT seg.rdb$field_name AS ColumnName
                FROM rdb$relation_constraints rc
                JOIN rdb$index_segments seg ON seg.rdb$index_name = rc.rdb$index_name
                WHERE rc.rdb$relation_name = @table AND rc.rdb$constraint_type = 'PRIMARY KEY'
                ORDER BY seg.rdb$field_position";

            using var cmd = new FirebirdSql.Data.FirebirdClient.FbCommand(query, connection);
            cmd.Parameters.Add("@table", FirebirdSql.Data.FirebirdClient.FbDbType.Char).Value = tableName;
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var columnName = reader["ColumnName"]?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(columnName))
                    set.Add(columnName);
            }
            return set;
        }

        public async Task<List<DatabaseSnapshot>> GetSnapshotsAsync(string? databaseId = null)
        {
            try
            {
                _logger.LogInformation("Iniciando GetSnapshotsAsync para database {DatabaseId}", databaseId ?? "todas");
                
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); // Timeout de apenas 5 segundos
                
                // Consulta MUITO simplificada - apenas dados básicos do SQLite
                var query = _context.DatabaseSnapshots
                    .Where(s => s.IsActive);

                if (!string.IsNullOrEmpty(databaseId))
                {
                    query = query.Where(s => s.DatabaseId == databaseId);
                }

                // Buscar apenas os snapshots mais recentes (últimos 5)
                var snapshots = await query
                    .OrderByDescending(s => s.GeneratedAt)
                    .Take(5) // Apenas 5 snapshots
                    .Select(s => new { 
                        s.Id, 
                        s.DatabaseId, 
                        s.DatabaseName, 
                        s.GeneratedAt,
                        s.Status
                    })
                    .ToListAsync(cts.Token);

                _logger.LogInformation("Encontrados {Count} snapshots no banco", snapshots.Count);

                // Retornar apenas os dados básicos do SQLite - SEM deserialização
                var result = new List<DatabaseSnapshot>();
                
                foreach (var snapshot in snapshots)
                {
                    var simpleSnapshot = new DatabaseSnapshot
                    {
                        Id = snapshot.Id,
                        DatabaseId = snapshot.DatabaseId,
                        DatabaseName = snapshot.DatabaseName,
                        GeneratedAt = snapshot.GeneratedAt,
                        Tables = new List<TableFullInfo>() // SEMPRE vazio - não carregar dados
                    };
                    
                    result.Add(simpleSnapshot);
                }

                _logger.LogInformation("Retornados {Count} snapshots básicos para database {DatabaseId}", result.Count, databaseId ?? "todas");
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Operação de obter snapshots foi cancelada por timeout");
                return new List<DatabaseSnapshot>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter snapshots para database {DatabaseId}", databaseId ?? "todas");
                return new List<DatabaseSnapshot>();
            }
        }

        public async Task<DatabaseSnapshot?> GetSnapshotByIdAsync(string snapshotId)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)); // Timeout de 10 segundos
                
                var snapshot = await _context.DatabaseSnapshots
                    .FirstOrDefaultAsync(s => s.Id == snapshotId && s.IsActive, cts.Token);

                if (snapshot != null)
                {
                    // Verificar se SnapshotData não é nulo ou vazio
                    if (string.IsNullOrWhiteSpace(snapshot.SnapshotData))
                    {
                        _logger.LogWarning("Snapshot {SnapshotId} tem SnapshotData vazio ou nulo", snapshot.Id);
                        return null;
                    }

                    // Deserialização com timeout e configurações otimizadas
                    try
                    {
                        var deserializedSnapshot = JsonSerializer.Deserialize<DatabaseSnapshot>(
                            snapshot.SnapshotData,
                            new JsonSerializerOptions 
                            { 
                                PropertyNameCaseInsensitive = true,
                                ReadCommentHandling = JsonCommentHandling.Skip,
                                MaxDepth = 32 // Limitar profundidade para evitar travamentos
                            });

                        // IMPORTANTE: Adicionar o ID do banco SQLite ao objeto deserializado
                        // porque snapshots antigos podem não ter o ID no JSON
                        if (deserializedSnapshot != null)
                        {
                            deserializedSnapshot.Id = snapshot.Id;
                        }

                        return deserializedSnapshot;
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "Erro na deserialização JSON do snapshot {SnapshotId}", snapshotId);
                        return null;
                    }
                }

                return null;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Operação de obter snapshot por ID foi cancelada por timeout: {SnapshotId}", snapshotId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter snapshot por ID: {SnapshotId}", snapshotId);
                return null;
            }
        }

        public async Task<bool> DeleteSnapshotAsync(string snapshotId)
        {
            try
            {
                var snapshot = await _context.DatabaseSnapshots
                    .FirstOrDefaultAsync(s => s.Id == snapshotId);

                if (snapshot != null)
                {
                    snapshot.IsActive = false;
                    await _context.SaveChangesAsync();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao deletar snapshot");
                return false;
            }
        }


        #endregion

        #region Project Settings

        public async Task<ProjectConfig> GetProjectConfigAsync()
        {
            try
            {
                var settings = await _context.ProjectSettingsSqlite.FirstOrDefaultAsync();
                if (settings == null)
                {
                    settings = new ProjectSettingsSqlite();
                    _context.ProjectSettingsSqlite.Add(settings);
                    await _context.SaveChangesAsync();
                }

                return new ProjectConfig
                {
                    Settings = new ProjectSettings
                    {
                        DefaultDatabaseId = settings.DefaultDatabaseId ?? "default"
                    },
                    Databases = await GetAllDatabasesAsync()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter configuração do projeto");
                return new ProjectConfig();
            }
        }

        public async Task<bool> UpdateProjectConfigAsync(ProjectConfig config)
        {
            try
            {
                var settings = await _context.ProjectSettingsSqlite.FirstOrDefaultAsync();
                if (settings == null)
                {
                    settings = new ProjectSettingsSqlite();
                    _context.ProjectSettingsSqlite.Add(settings);
                }

                settings.DefaultDatabaseId = config.Settings.DefaultDatabaseId;
                settings.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar configuração do projeto");
                return false;
            }
        }

        #endregion

        #region Helper Methods

        private DateTime GetTokenExpiration(string token)
        {
            try
            {
                var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                
                // JWT exp é em Unix timestamp
                var expClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "exp");
                if (expClaim != null && long.TryParse(expClaim.Value, out var exp))
                {
                    return DateTimeOffset.FromUnixTimeSeconds(exp).DateTime;
                }
                
                // Se não conseguir obter expiração, assumir 24 horas
                return DateTime.UtcNow.AddHours(24);
            }
            catch
            {
                // Se não conseguir decodificar, assumir 24 horas
                return DateTime.UtcNow.AddHours(24);
            }
        }

        private Task UpdateDatabaseFileSizeAsync(DatabaseConfig config)
        {
            try
            {
                // Verificar se o arquivo existe
                if (File.Exists(config.Database))
                {
                    var fileInfo = new FileInfo(config.Database);
                    config.FileSizeBytes = fileInfo.Length;
                    config.LastSizeCheck = DateTime.UtcNow;
                }
                else
                {
                    // Se for uma conexão remota (não local), tentar obter informações via conexão
                    if (config.Server != "localhost" && config.Server != "127.0.0.1")
                    {
                        // Para conexões remotas, não conseguimos obter o tamanho do arquivo diretamente
                        config.FileSizeBytes = null;
                        config.LastSizeCheck = DateTime.UtcNow;
                    }
                    else
                    {
                        config.FileSizeBytes = null;
                        config.LastSizeCheck = DateTime.UtcNow;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter tamanho do arquivo da base {DatabaseName}", config.Name);
                config.FileSizeBytes = null;
                config.LastSizeCheck = DateTime.UtcNow;
            }
            
            return Task.CompletedTask;
        }

        private DatabaseConfig MapToDatabaseConfig(DatabaseConfigSqlite sqliteConfig)
        {
            return new DatabaseConfig
            {
                Id = sqliteConfig.Id,
                Name = sqliteConfig.Name,
                Server = sqliteConfig.Server,
                Database = sqliteConfig.Database,
                Username = sqliteConfig.Username,
                Password = sqliteConfig.Password,
                Port = sqliteConfig.Port,
                Charset = sqliteConfig.Charset,
                CreatedAt = sqliteConfig.CreatedAt,
                IsActive = sqliteConfig.IsActive,
                FileSizeBytes = sqliteConfig.FileSizeBytes,
                LastSizeCheck = sqliteConfig.LastSizeCheck,
                DesktopNodeId = sqliteConfig.DesktopNodeId,
                UserId = sqliteConfig.UserId
            };
        }

        private DatabaseConfigSqlite MapToDatabaseConfigSqlite(DatabaseConfig config)
        {
            return new DatabaseConfigSqlite
            {
                Id = config.Id,
                Name = config.Name,
                Server = config.Server,
                Database = config.Database,
                Username = config.Username,
                Password = config.Password,
                Port = config.Port,
                Charset = config.Charset,
                CreatedAt = config.CreatedAt,
                IsActive = config.IsActive,
                FileSizeBytes = config.FileSizeBytes,
                LastSizeCheck = config.LastSizeCheck,
                DesktopNodeId = config.DesktopNodeId,
                UserId = config.UserId
            };
        }

        #endregion
    }
}
