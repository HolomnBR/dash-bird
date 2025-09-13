using FirebirdApi.Models;
using FirebirdApi.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace FirebirdApi.Services
{
    /// <summary>
    /// Classe para armazenar informações em cache sobre contagens de tabelas
    /// </summary>
    public class CachedTableInfo
    {
        public long RecordCount { get; set; }
        public long? LastId { get; set; }
        public DateTime CachedAt { get; set; }
    }

    public interface IDatabaseConfigService
    {
        Task<List<DatabaseConfig>> GetAllDatabasesAsync();
        Task<DatabaseConfig?> GetDatabaseByIdAsync(string id);
        Task<DatabaseConfig> AddDatabaseAsync(DatabaseConfig config);
        Task<bool> RemoveDatabaseAsync(string id);
        Task<bool> UpdateDatabaseAsync(DatabaseConfig config);
        Task<bool> TestConnectionAsync(string id);
        Task<List<TableInfo>> GetTablesAsync(string id);
        Task<List<TableSchema>> GetTableSchemaAsync(string id, IEnumerable<string> tableNames);
        Task<DatabaseSnapshot> GenerateDatabaseSnapshotAsync(string id);
        Task<DatabaseConfig?> GetDefaultDatabaseAsync();
        Task<bool> SetDefaultDatabaseAsync(string id);
        Task<ProjectConfig> GetProjectConfigAsync();
        Task<bool> UpdateAllDatabaseFileSizesAsync();
        void ClearRecordCountCache();
        void ClearRecordCountCacheForDatabase(string databaseId);
    }

    public class DatabaseConfigService : IDatabaseConfigService
    {
        private readonly LocalDbContext _context;
        private readonly ILogger<DatabaseConfigService> _logger;
        private readonly MemoryCache _recordCountCache;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5); // Cache por 5 minutos

        public DatabaseConfigService(LocalDbContext context, ILogger<DatabaseConfigService> logger)
        {
            _context = context;
            _logger = logger;
            _recordCountCache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 1000, // Máximo 1000 entradas no cache
                CompactionPercentage = 0.25 // Remove 25% quando atinge o limite
            });
        }



        public async Task<List<DatabaseConfig>> GetAllDatabasesAsync()
        {
            try
            {
                var sqliteConfigs = await _context.DatabaseConfigs
                    .Where(db => db.IsActive)
                    .OrderBy(db => db.Name)
                    .ToListAsync();

                return sqliteConfigs.Select(MapToDatabaseConfig).ToList();
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
                var sqliteConfig = await _context.DatabaseConfigs
                    .FirstOrDefaultAsync(db => db.Id == id && db.IsActive);

                return sqliteConfig != null ? MapToDatabaseConfig(sqliteConfig) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter configuração de banco por ID");
                return null;
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

                var sqliteDatabase = await _context.DatabaseConfigs
                    .FirstOrDefaultAsync(db => db.Id == id);

                if (sqliteDatabase != null)
                {
                    sqliteDatabase.IsActive = false;
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
                var existingSqliteDatabase = await _context.DatabaseConfigs
                    .FirstOrDefaultAsync(db => db.Id == config.Id);

                if (existingSqliteDatabase != null)
                {
                    // Atualizar tamanho do arquivo se o caminho da base mudou
                    if (existingSqliteDatabase.Database != config.Database)
                    {
                        await UpdateDatabaseFileSizeAsync(config);
                    }

                    var sqliteConfig = MapToDatabaseConfigSqlite(config);
                    _context.Entry(existingSqliteDatabase).CurrentValues.SetValues(sqliteConfig);
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

        public async Task<bool> TestConnectionAsync(string id)
        {
            var database = await GetDatabaseByIdAsync(id);
            if (database == null)
                return false;

            try
            {
                using var connection = new FirebirdSql.Data.FirebirdClient.FbConnection(BuildConnectionString(database));
                await connection.OpenAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<TableInfo>> GetTablesAsync(string id)
        {
            var database = await GetDatabaseByIdAsync(id);
            if (database == null)
                return new List<TableInfo>();

            try
            {
                using var connection = new FirebirdSql.Data.FirebirdClient.FbConnection(BuildConnectionString(database));
                await connection.OpenAsync();

                var tables = new List<TableInfo>();
                var query = @"
                    SELECT 
                        r.rdb$relation_name as TableName,
                        r.rdb$owner_name as Schema,
                        r.rdb$relation_type as TableType,
                        r.rdb$description as Description
                    FROM rdb$relations r
                    WHERE r.rdb$view_blr is null 
                    AND (r.rdb$system_flag is null or r.rdb$system_flag = 0)
                    ORDER BY r.rdb$relation_name";

                using var command = new FirebirdSql.Data.FirebirdClient.FbCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    tables.Add(new TableInfo
                    {
                        TableName = reader["TableName"]?.ToString()?.Trim() ?? "",
                        Schema = reader["Schema"]?.ToString()?.Trim() ?? "",
                        TableType = GetTableType(reader["TableType"]?.ToString() ?? ""),
                        Description = reader["Description"]?.ToString()?.Trim() ?? ""
                    });
                }

                return tables;
            }
            catch
            {
                return new List<TableInfo>();
            }
        }

		public async Task<List<TableSchema>> GetTableSchemaAsync(string id, IEnumerable<string> tableNames)
		{
			var database = await GetDatabaseByIdAsync(id);
			if (database == null)
				return new List<TableSchema>();

			var requestedTables = (tableNames ?? Array.Empty<string>())
				.Where(n => !string.IsNullOrWhiteSpace(n))
				.Select(n => n.Trim().ToUpperInvariant())
				.Distinct()
				.ToList();

			if (!requestedTables.Any())
				return new List<TableSchema>();

			var result = new List<TableSchema>();

			try
			{
				using var connection = new FirebirdSql.Data.FirebirdClient.FbConnection(BuildConnectionString(database));
				await connection.OpenAsync();

				foreach (var table in requestedTables)
				{
					var columns = await LoadColumnsAsync(connection, table);
					var pkColumns = await LoadPrimaryKeyColumnsAsync(connection, table);

					foreach (var column in columns)
					{
						column.IsPrimaryKey = pkColumns.Contains(column.ColumnName);
					}

					result.Add(new TableSchema
					{
						TableName = table,
						Columns = columns
					});
				}

				return result;
			}
			catch
			{
				return new List<TableSchema>();
			}
		}

		private async Task<List<ColumnSchema>> LoadColumnsAsync(FirebirdSql.Data.FirebirdClient.FbConnection connection, string tableName)
		{
			var columns = new List<ColumnSchema>();
			var query = @"
				SELECT
					rf.rdb$field_name AS ColumnName,
					f.rdb$field_type AS FieldType,
					f.rdb$field_sub_type AS FieldSubType,
					f.rdb$field_length AS FieldLength,
					f.rdb$field_precision AS FieldPrecision,
					f.rdb$field_scale AS FieldScale,
					rf.rdb$null_flag AS NullFlag,
					rf.rdb$default_source AS DefaultSource,
					rf.rdb$description AS Description
				FROM rdb$relation_fields rf
				JOIN rdb$fields f ON rf.rdb$field_source = f.rdb$field_name
				WHERE rf.rdb$relation_name = @table
				ORDER BY rf.rdb$field_position";

			using var cmd = new FirebirdSql.Data.FirebirdClient.FbCommand(query, connection);
			cmd.Parameters.Add("@table", FirebirdSql.Data.FirebirdClient.FbDbType.Char).Value = tableName;
			using var reader = await cmd.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				var fieldType = reader["FieldType"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["FieldType"]);
				var fieldSubType = reader["FieldSubType"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["FieldSubType"]);
				var length = reader["FieldLength"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["FieldLength"]);
				var precision = reader["FieldPrecision"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["FieldPrecision"]);
				var scale = reader["FieldScale"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["FieldScale"]);

				columns.Add(new ColumnSchema
				{
					ColumnName = reader["ColumnName"]?.ToString()?.Trim() ?? string.Empty,
					DataType = MapFieldType(fieldType, fieldSubType, precision, scale, length),
					Length = length,
					Precision = precision,
					Scale = scale,
					IsNullable = reader["NullFlag"] == DBNull.Value,
					Default = CleanDefaultSource(reader["DefaultSource"]?.ToString()),
					Description = reader["Description"]?.ToString()?.Trim() ?? string.Empty
				});
			}

			return columns;
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

		private string MapFieldType(int? fieldType, int? fieldSubType, int? precision, int? scale, int? length)
		{
			if (fieldType == null)
				return string.Empty;

			switch (fieldType.Value)
			{
				case 7: // SMALLINT
					return "SMALLINT";
				case 8: // INTEGER
					return "INTEGER";
				case 16: // BIGINT / NUMERIC/DECIMAL
					if (fieldSubType.HasValue && (fieldSubType.Value == 1 || fieldSubType.Value == 2))
					{
						var p = precision ?? 18;
						var s = scale.HasValue ? Math.Abs(scale.Value) : 0;
						var t = fieldSubType.Value == 1 ? "NUMERIC" : "DECIMAL";
						return $"{t}({p},{s})";
					}
					return "BIGINT";
				case 10: // FLOAT
					return "FLOAT";
				case 27: // DOUBLE
					return "DOUBLE PRECISION";
				case 14: // CHAR
					return length.HasValue ? $"CHAR({length.Value})" : "CHAR";
				case 37: // VARCHAR
					return length.HasValue ? $"VARCHAR({length.Value})" : "VARCHAR";
				case 261: // BLOB
					return "BLOB";
				case 12: // DATE
					return "DATE";
				case 13: // TIME
					return "TIME";
				case 35: // TIMESTAMP
					return "TIMESTAMP";
				default:
					return "UNKNOWN";
			}
		}

		private string CleanDefaultSource(string? defaultSource)
		{
			if (string.IsNullOrWhiteSpace(defaultSource)) return string.Empty;
			var text = defaultSource.Trim();
			if (text.StartsWith("DEFAULT", StringComparison.OrdinalIgnoreCase))
			{
				text = text.Substring(7).Trim();
			}
			return text;
		}


        private string GetTableType(string type)
        {
            return type switch
            {
                "0" => "Table",
                "1" => "View",
                _ => "Unknown"
            };
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

        /// <summary>
        /// Atualiza o tamanho do arquivo de todas as bases de dados
        /// </summary>
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
                    var config = MapToDatabaseConfig(database);
                    await UpdateDatabaseFileSizeAsync(config);
                    
                    if (oldSize != config.FileSizeBytes)
                    {
                        database.FileSizeBytes = config.FileSizeBytes;
                        database.LastSizeCheck = config.LastSizeCheck;
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

        /// <summary>
        /// Atualiza o tamanho do arquivo da base de dados
        /// </summary>
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
                        // Podemos tentar obter informações via query do Firebird, mas isso é complexo
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
                Console.WriteLine($"Erro ao obter tamanho do arquivo da base {config.Name}: {ex.Message}");
                config.FileSizeBytes = null;
                config.LastSizeCheck = DateTime.UtcNow;
            }
            
            return Task.CompletedTask;
        }

        public async Task<DatabaseSnapshot> GenerateDatabaseSnapshotAsync(string id)
        {
            var database = await GetDatabaseByIdAsync(id);
            if (database == null)
                throw new InvalidOperationException($"Base de dados com ID '{id}' não encontrada");

            try
            {
                _logger.LogInformation("Iniciando geração de snapshot para base: {DatabaseName}", database.Name);
                
                // FASE 1: Descoberta rápida do schema (tabelas e colunas)
                var schemaStartTime = DateTime.UtcNow;
                var tablesWithSchema = await DiscoverDatabaseSchemaAsync(database);
                var schemaDuration = DateTime.UtcNow - schemaStartTime;
                _logger.LogInformation("Fase 1 (Schema) concluída em {Duration}ms para {TableCount} tabelas", 
                    schemaDuration.TotalMilliseconds, tablesWithSchema.Count);

                // FASE 2: Contagem otimizada de registros (paralela)
                var countStartTime = DateTime.UtcNow;
                await PopulateRecordCountsAsync(database, tablesWithSchema);
                var countDuration = DateTime.UtcNow - countStartTime;
                _logger.LogInformation("Fase 2 (Contagem) concluída em {Duration}ms", countDuration.TotalMilliseconds);

                // Criar snapshot final
                var snapshot = new DatabaseSnapshot
                {
                    DatabaseId = database.Id,
                    DatabaseName = database.Name,
                    GeneratedAt = DateTime.UtcNow,
                    Tables = tablesWithSchema.Select(t => new TableFullInfo
                    {
                        TableName = t.TableName,
                        Schema = t.Schema,
                        TableType = t.TableType,
                        Description = t.Description,
                        Columns = t.Columns,
                        RecordCount = t.RecordCount,
                        LastId = t.LastId,
                        GeneratedAt = DateTime.UtcNow
                    }).ToList()
                };

                // Salvar no SQLite
                var snapshotId = await SaveSnapshotToDatabaseAsync(database, snapshot);
                
                _logger.LogInformation("Snapshot gerado com sucesso: {SnapshotId} (Total: {TotalDuration}ms)", 
                    snapshotId, (DateTime.UtcNow - schemaStartTime).TotalMilliseconds);
                
                return snapshot;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar snapshot da base de dados");
                throw new InvalidOperationException($"Erro ao gerar snapshot da base de dados: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// FASE 1: Descoberta rápida do schema (tabelas e colunas) usando uma única conexão
        /// </summary>
        private async Task<List<TableFullInfo>> DiscoverDatabaseSchemaAsync(DatabaseConfig database)
        {
            var tables = new List<TableFullInfo>();
            
            using var connection = new FirebirdSql.Data.FirebirdClient.FbConnection(BuildConnectionString(database));
            await connection.OpenAsync();

            // 1. Descobrir todas as tabelas
            var tableQuery = @"
                SELECT 
                    r.rdb$relation_name as TableName,
                    r.rdb$owner_name as Schema,
                    r.rdb$relation_type as TableType,
                    r.rdb$description as Description
                FROM rdb$relations r
                WHERE r.rdb$view_blr is null 
                AND (r.rdb$system_flag is null or r.rdb$system_flag = 0)
                ORDER BY r.rdb$relation_name";

            using var tableCmd = new FirebirdSql.Data.FirebirdClient.FbCommand(tableQuery, connection);
            using var tableReader = await tableCmd.ExecuteReaderAsync();

            var tableNames = new List<string>();
            while (await tableReader.ReadAsync())
            {
                var tableName = tableReader["TableName"]?.ToString()?.Trim() ?? "";
                if (!string.IsNullOrEmpty(tableName))
                {
                    tableNames.Add(tableName);
                    tables.Add(new TableFullInfo
                    {
                        TableName = tableName,
                        Schema = tableReader["Schema"]?.ToString()?.Trim() ?? "",
                        TableType = GetTableType(tableReader["TableType"]?.ToString() ?? ""),
                        Description = tableReader["Description"]?.ToString()?.Trim() ?? "",
                        Columns = new List<ColumnSchema>(),
                        RecordCount = 0,
                        LastId = null,
                        GeneratedAt = DateTime.UtcNow
                    });
                }
            }
            tableReader.Close();

            if (!tableNames.Any())
                return tables;

            // 2. Obter schema de todas as colunas de uma vez
            var columnQuery = @"
                SELECT
                    rf.rdb$relation_name AS TableName,
                    rf.rdb$field_name AS ColumnName,
                    f.rdb$field_type AS FieldType,
                    f.rdb$field_sub_type AS FieldSubType,
                    f.rdb$field_length AS FieldLength,
                    f.rdb$field_precision AS FieldPrecision,
                    f.rdb$field_scale AS FieldScale,
                    rf.rdb$null_flag AS NullFlag,
                    rf.rdb$default_source AS DefaultSource,
                    rf.rdb$description AS Description,
                    rf.rdb$field_position AS FieldPosition
                FROM rdb$relation_fields rf
                JOIN rdb$fields f ON rf.rdb$field_source = f.rdb$field_name
                WHERE rf.rdb$relation_name IN (" + string.Join(",", tableNames.Select(t => $"'{t}'")) + @")
                ORDER BY rf.rdb$relation_name, rf.rdb$field_position";

            using var columnCmd = new FirebirdSql.Data.FirebirdClient.FbCommand(columnQuery, connection);
            using var columnReader = await columnCmd.ExecuteReaderAsync();

            var tableColumns = new Dictionary<string, List<ColumnSchema>>();
            while (await columnReader.ReadAsync())
            {
                var tableName = columnReader["TableName"]?.ToString()?.Trim() ?? "";
                if (string.IsNullOrEmpty(tableName)) continue;

                if (!tableColumns.ContainsKey(tableName))
                    tableColumns[tableName] = new List<ColumnSchema>();

                var fieldType = columnReader["FieldType"] == DBNull.Value ? (int?)null : Convert.ToInt32(columnReader["FieldType"]);
                var fieldSubType = columnReader["FieldSubType"] == DBNull.Value ? (int?)null : Convert.ToInt32(columnReader["FieldSubType"]);
                var length = columnReader["FieldLength"] == DBNull.Value ? (int?)null : Convert.ToInt32(columnReader["FieldLength"]);
                var precision = columnReader["FieldPrecision"] == DBNull.Value ? (int?)null : Convert.ToInt32(columnReader["FieldPrecision"]);
                var scale = columnReader["FieldScale"] == DBNull.Value ? (int?)null : Convert.ToInt32(columnReader["FieldScale"]);

                tableColumns[tableName].Add(new ColumnSchema
                {
                    ColumnName = columnReader["ColumnName"]?.ToString()?.Trim() ?? string.Empty,
                    DataType = MapFieldType(fieldType, fieldSubType, precision, scale, length),
                    Length = length,
                    Precision = precision,
                    Scale = scale,
                    IsNullable = columnReader["NullFlag"] == DBNull.Value,
                    Default = CleanDefaultSource(columnReader["DefaultSource"]?.ToString()),
                    Description = columnReader["Description"]?.ToString()?.Trim() ?? string.Empty,
                    IsPrimaryKey = false // Será definido na próxima query
                });
            }
            columnReader.Close();

            // 3. Obter chaves primárias de todas as tabelas de uma vez
            var pkQuery = @"
                SELECT 
                    rc.rdb$relation_name AS TableName,
                    seg.rdb$field_name AS ColumnName,
                    seg.rdb$field_position AS FieldPosition
                FROM rdb$relation_constraints rc
                JOIN rdb$index_segments seg ON seg.rdb$index_name = rc.rdb$index_name
                WHERE rc.rdb$relation_name IN (" + string.Join(",", tableNames.Select(t => $"'{t}'")) + @")
                AND rc.rdb$constraint_type = 'PRIMARY KEY'
                ORDER BY rc.rdb$relation_name, seg.rdb$field_position";

            using var pkCmd = new FirebirdSql.Data.FirebirdClient.FbCommand(pkQuery, connection);
            using var pkReader = await pkCmd.ExecuteReaderAsync();

            var tablePrimaryKeys = new Dictionary<string, HashSet<string>>();
            while (await pkReader.ReadAsync())
            {
                var tableName = pkReader["TableName"]?.ToString()?.Trim() ?? "";
                var columnName = pkReader["ColumnName"]?.ToString()?.Trim() ?? "";
                
                if (!string.IsNullOrEmpty(tableName) && !string.IsNullOrEmpty(columnName))
                {
                    if (!tablePrimaryKeys.ContainsKey(tableName))
                        tablePrimaryKeys[tableName] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    
                    tablePrimaryKeys[tableName].Add(columnName);
                }
            }
            pkReader.Close();

            // 4. Associar colunas às tabelas e marcar chaves primárias
            foreach (var table in tables)
            {
                if (tableColumns.ContainsKey(table.TableName))
                {
                    table.Columns = tableColumns[table.TableName];
                    
                    // Marcar chaves primárias
                    if (tablePrimaryKeys.ContainsKey(table.TableName))
                    {
                        var pkColumns = tablePrimaryKeys[table.TableName];
                        foreach (var column in table.Columns)
                        {
                            column.IsPrimaryKey = pkColumns.Contains(column.ColumnName);
                        }
                    }
                }
            }

            return tables;
        }

        /// <summary>
        /// FASE 2: Contagem otimizada de registros usando processamento paralelo
        /// </summary>
        private async Task PopulateRecordCountsAsync(DatabaseConfig database, List<TableFullInfo> tables)
        {
            const int maxConcurrentConnections = 5; // Limitar conexões simultâneas
            var semaphore = new SemaphoreSlim(maxConcurrentConnections);
            
            var tasks = tables.Select(async table =>
            {
                await semaphore.WaitAsync();
                try
                {
                    await PopulateTableRecordCountAsync(database, table);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Conta registros de uma tabela específica de forma otimizada com cache
        /// </summary>
        private async Task PopulateTableRecordCountAsync(DatabaseConfig database, TableFullInfo table)
        {
            try
            {
                var cacheKey = $"{database.Id}:{table.TableName}";
                
                // 1. Verificar cache primeiro
                if (_recordCountCache.TryGetValue(cacheKey, out CachedTableInfo? cachedInfo) && 
                    cachedInfo != null && 
                    DateTime.UtcNow - cachedInfo.CachedAt < _cacheExpiration)
                {
                    table.RecordCount = cachedInfo.RecordCount;
                    table.LastId = cachedInfo.LastId;
                    _logger.LogDebug("Cache hit para tabela {TableName}: {RecordCount} registros", 
                        table.TableName, table.RecordCount);
                    return;
                }

                using var connection = new FirebirdSql.Data.FirebirdClient.FbConnection(BuildConnectionString(database));
                await connection.OpenAsync();

                // 2. Contar registros (otimizado)
                var countQuery = $"SELECT COUNT(*) FROM \"{table.TableName}\"";
                using var countCmd = new FirebirdSql.Data.FirebirdClient.FbCommand(countQuery, connection);
                var recordCount = await countCmd.ExecuteScalarAsync();
                if (recordCount != null && recordCount != DBNull.Value)
                {
                    table.RecordCount = Convert.ToInt64(recordCount);
                }

                // 3. Obter último ID apenas se a tabela tem registros e chave primária
                if (table.RecordCount > 0)
                {
                    var pkColumn = table.Columns.FirstOrDefault(c => c.IsPrimaryKey);
                    if (pkColumn == null)
                    {
                        // Procurar por colunas que parecem ser ID
                        pkColumn = table.Columns.FirstOrDefault(c => 
                            c.ColumnName.ToUpperInvariant().Contains("ID") || 
                            c.ColumnName.ToUpperInvariant().Contains("CODIGO") ||
                            c.ColumnName.ToUpperInvariant().Contains("COD"));
                    }

                    if (pkColumn != null)
                    {
                        var lastIdQuery = $"SELECT MAX(\"{pkColumn.ColumnName}\") FROM \"{table.TableName}\"";
                        using var lastIdCmd = new FirebirdSql.Data.FirebirdClient.FbCommand(lastIdQuery, connection);
                        var lastId = await lastIdCmd.ExecuteScalarAsync();
                        if (lastId != null && lastId != DBNull.Value)
                        {
                            // Verificar se a coluna é numérica antes de converter
                            if (IsNumericColumn(pkColumn.DataType))
                            {
                                try
                                {
                                    table.LastId = Convert.ToInt64(lastId);
                                }
                                catch (FormatException ex)
                                {
                                    _logger.LogWarning("Não foi possível converter o valor '{LastIdValue}' da coluna '{ColumnName}' (tipo: {DataType}) para Int64 na tabela '{TableName}': {ErrorMessage}", 
                                        lastId, pkColumn.ColumnName, pkColumn.DataType, table.TableName, ex.Message);
                                    // Não definir LastId para colunas não numéricas
                                    table.LastId = null;
                                }
                            }
                            else
                            {
                                _logger.LogDebug("Coluna '{ColumnName}' da tabela '{TableName}' não é numérica (tipo: {DataType}), pulando conversão para LastId", 
                                    pkColumn.ColumnName, table.TableName, pkColumn.DataType);
                                // Não definir LastId para colunas não numéricas
                                table.LastId = null;
                            }
                        }
                    }
                }

                // 4. Salvar no cache
                var newCachedInfo = new CachedTableInfo
                {
                    RecordCount = table.RecordCount,
                    LastId = table.LastId,
                    CachedAt = DateTime.UtcNow
                };
                
                _recordCountCache.Set(cacheKey, newCachedInfo, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _cacheExpiration,
                    Size = 1 // Cada entrada ocupa 1 unidade de tamanho
                });

                _logger.LogDebug("Cache miss para tabela {TableName}: {RecordCount} registros (salvo no cache)", 
                    table.TableName, table.RecordCount);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao contar registros da tabela {TableName}: {Message}", 
                    table.TableName, ex.Message);
                // Continua com valores padrão (RecordCount = 0, LastId = null)
            }
        }

        /// <summary>
        /// Limpa o cache de contagens de registros
        /// </summary>
        public void ClearRecordCountCache()
        {
            _recordCountCache.Clear();
            _logger.LogInformation("Cache de contagens de registros limpo");
        }

        /// <summary>
        /// Limpa o cache de uma base de dados específica
        /// </summary>
        public void ClearRecordCountCacheForDatabase(string databaseId)
        {
            var keysToRemove = new List<string>();
            
            // Como não temos acesso direto às chaves, vamos usar uma abordagem diferente
            // Em uma implementação real, você poderia manter um índice das chaves
            _recordCountCache.Clear(); // Por simplicidade, limpa tudo
            _logger.LogInformation("Cache de contagens limpo para base: {DatabaseId}", databaseId);
        }

        /// <summary>
        /// Salva o snapshot no banco de dados SQLite
        /// </summary>
        private async Task<string> SaveSnapshotToDatabaseAsync(DatabaseConfig database, DatabaseSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot), "Snapshot não pode ser nulo");
            }

            var snapshotId = Guid.NewGuid().ToString();
            var snapshotData = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
            
            if (string.IsNullOrWhiteSpace(snapshotData))
            {
                throw new InvalidOperationException("Falha ao serializar snapshot - resultado vazio");
            }

            var sqliteSnapshot = new DatabaseSnapshotSqlite
            {
                Id = snapshotId,
                DatabaseId = database.Id,
                DatabaseName = database.Name,
                GeneratedAt = DateTime.UtcNow,
                SnapshotData = snapshotData,
                IsActive = true
            };

            _context.DatabaseSnapshots.Add(sqliteSnapshot);

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

            // Manter compatibilidade com arquivo JSON
            var snapshotDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "snapshots");
            Directory.CreateDirectory(snapshotDir);
            
            var fileName = $"snapshot_{database.Id}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine(snapshotDir, fileName);
            
            var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);

            // Atualizar o caminho do arquivo no SQLite
            sqliteSnapshot.FilePath = filePath;
            await _context.SaveChangesAsync();

            return snapshotId;
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

        /// <summary>
        /// Verifica se o tipo de dados da coluna é numérico
        /// </summary>
        private static bool IsNumericColumn(string dataType)
        {
            if (string.IsNullOrEmpty(dataType))
                return false;

            var numericTypes = new[]
            {
                "INTEGER", "INT", "BIGINT", "SMALLINT", "TINYINT",
                "DECIMAL", "NUMERIC", "FLOAT", "DOUBLE", "REAL",
                "INT64", "INT32", "INT16", "INT8"
            };

            var upperDataType = dataType.ToUpperInvariant();
            return numericTypes.Any(type => upperDataType.Contains(type));
        }
    }
}

