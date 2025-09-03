using FirebirdApi.Models;
using System.Text.Json;

namespace FirebirdApi.Services
{
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
    }

    public class DatabaseConfigService : IDatabaseConfigService
    {
        private readonly string _baseConfigFilePath;
        private readonly string _configFilePath;
        private readonly List<DatabaseConfig> _databases;
        private ProjectConfig _projectConfig;

        public DatabaseConfigService()
        {
            _baseConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database-configs.json");
            _projectConfig = LoadProjectConfig();
            _databases = LoadDatabases();
        }

        private ProjectConfig LoadProjectConfig()
        {
            try
            {
                if (File.Exists(_baseConfigFilePath))
                {
                    var json = File.ReadAllText(_baseConfigFilePath);
                    var config = JsonSerializer.Deserialize<ProjectConfig>(json);
                    return config ?? new ProjectConfig();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao carregar configuração base: {ex.Message}");
            }
            return new ProjectConfig();
        }

        private List<DatabaseConfig> LoadDatabases()
        {
            var databases = new List<DatabaseConfig>();

            // Primeiro, carregar do arquivo base (config.json)
            if (_projectConfig.Databases.Any())
            {
                databases.AddRange(_projectConfig.Databases);
            }

            // Depois, carregar do arquivo dinâmico (database-configs.json)
            try
            {
                if (File.Exists(_configFilePath))
                {
                    var json = File.ReadAllText(_configFilePath);
                    var dynamicConfigs = JsonSerializer.Deserialize<List<DatabaseConfig>>(json);
                    if (dynamicConfigs != null)
                    {
                        // Adicionar apenas configurações que não existem no arquivo base
                        foreach (var config in dynamicConfigs)
                        {
                            if (!databases.Any(db => db.Id == config.Id))
                            {
                                databases.Add(config);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao carregar configurações dinâmicas: {ex.Message}");
            }

            return databases;
        }

        private async Task SaveDatabasesAsync()
        {
            try
            {
                // Salvar apenas as configurações dinâmicas (não as do arquivo base)
                var dynamicConfigs = _databases.Where(db => db.Id != "default").ToList();
                var json = JsonSerializer.Serialize(dynamicConfigs, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_configFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao salvar configurações: {ex.Message}");
                throw;
            }
        }

        public async Task<List<DatabaseConfig>> GetAllDatabasesAsync()
        {
            return await Task.FromResult(_databases.Where(db => db.IsActive).ToList());
        }

        public async Task<DatabaseConfig?> GetDatabaseByIdAsync(string id)
        {
            return await Task.FromResult(_databases.FirstOrDefault(db => db.Id == id && db.IsActive));
        }

        public async Task<DatabaseConfig?> GetDefaultDatabaseAsync()
        {
            var defaultId = _projectConfig.Settings.DefaultDatabaseId;
            return await GetDatabaseByIdAsync(defaultId);
        }

        public async Task<bool> SetDefaultDatabaseAsync(string id)
        {
            var database = await GetDatabaseByIdAsync(id);
            if (database != null)
            {
                _projectConfig.Settings.DefaultDatabaseId = id;
                await SaveProjectConfigAsync();
                return true;
            }
            return false;
        }

        public async Task<ProjectConfig> GetProjectConfigAsync()
        {
            return await Task.FromResult(_projectConfig);
        }

        public async Task<DatabaseConfig> AddDatabaseAsync(DatabaseConfig config)
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

            // Verificar se já existe uma configuração com o mesmo ID
            var existingConfig = _databases.FirstOrDefault(db => db.Id == config.Id);
            if (existingConfig != null)
            {
                // Atualizar a configuração existente
                var index = _databases.IndexOf(existingConfig);
                _databases[index] = config;
            }
            else
            {
                _databases.Add(config);
            }

            await SaveDatabasesAsync();
            return config;
        }

        public async Task<bool> RemoveDatabaseAsync(string id)
        {
            // Não permitir remover a base padrão
            if (id == "default")
            {
                return false;
            }

            var database = _databases.FirstOrDefault(db => db.Id == id);
            if (database != null)
            {
                database.IsActive = false;
                await SaveDatabasesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> UpdateDatabaseAsync(DatabaseConfig config)
        {
            var existingDatabase = _databases.FirstOrDefault(db => db.Id == config.Id);
            if (existingDatabase != null)
            {
                var index = _databases.IndexOf(existingDatabase);
                
                // Atualizar tamanho do arquivo se o caminho da base mudou
                if (existingDatabase.Database != config.Database)
                {
                    await UpdateDatabaseFileSizeAsync(config);
                }
                
                _databases[index] = config;
                await SaveDatabasesAsync();
                return true;
            }
            return false;
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

        private async Task SaveProjectConfigAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_projectConfig, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_baseConfigFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao salvar configuração do projeto: {ex.Message}");
            }
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
                var databases = await GetAllDatabasesAsync();
                bool hasChanges = false;

                foreach (var database in databases)
                {
                    var oldSize = database.FileSizeBytes;
                    await UpdateDatabaseFileSizeAsync(database);
                    
                    if (oldSize != database.FileSizeBytes)
                    {
                        hasChanges = true;
                    }
                }

                if (hasChanges)
                {
                    await SaveDatabasesAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao atualizar tamanhos de todas as bases: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Atualiza o tamanho do arquivo da base de dados
        /// </summary>
        private async Task UpdateDatabaseFileSizeAsync(DatabaseConfig config)
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
        }

        public async Task<DatabaseSnapshot> GenerateDatabaseSnapshotAsync(string id)
        {
            var database = await GetDatabaseByIdAsync(id);
            if (database == null)
                throw new InvalidOperationException($"Base de dados com ID '{id}' não encontrada");

            try
            {
                using var connection = new FirebirdSql.Data.FirebirdClient.FbConnection(BuildConnectionString(database));
                await connection.OpenAsync();

                var snapshot = new DatabaseSnapshot
                {
                    DatabaseId = database.Id,
                    DatabaseName = database.Name,
                    GeneratedAt = DateTime.UtcNow,
                    Tables = new List<TableFullInfo>()
                };

                // 1. Listar todas as tabelas
                var tables = await GetTablesAsync(id);
                
                foreach (var table in tables)
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

                    try
                    {
                        // 2. Recuperar schema da tabela
                        var tableSchemas = await GetTableSchemaAsync(id, new[] { table.TableName });
                        if (tableSchemas.Any())
                        {
                            tableFullInfo.Columns = tableSchemas.First().Columns;
                        }

                        // 3. Contar quantidade de registros
                        var countQuery = $"SELECT COUNT(*) FROM \"{table.TableName}\"";
                        using var countCmd = new FirebirdSql.Data.FirebirdClient.FbCommand(countQuery, connection);
                        var recordCount = await countCmd.ExecuteScalarAsync();
                        if (recordCount != null && recordCount != DBNull.Value)
                        {
                            tableFullInfo.RecordCount = Convert.ToInt64(recordCount);
                        }

                        // 4. Pegar o lastId (procurar por colunas de chave primária ou colunas ID)
                        var pkColumns = await LoadPrimaryKeyColumnsAsync(connection, table.TableName);
                        if (pkColumns.Any())
                        {
                            // Se tem chave primária, usar a primeira coluna
                            var pkColumn = pkColumns.First();
                            var lastIdQuery = $"SELECT MAX(\"{pkColumn}\") FROM \"{table.TableName}\"";
                            using var lastIdCmd = new FirebirdSql.Data.FirebirdClient.FbCommand(lastIdQuery, connection);
                            var lastId = await lastIdCmd.ExecuteScalarAsync();
                            if (lastId != null && lastId != DBNull.Value)
                            {
                                tableFullInfo.LastId = Convert.ToInt64(lastId);
                            }
                        }
                        else
                        {
                            // Se não tem chave primária, procurar por colunas que parecem ser ID
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
                        // Log do erro mas continua com as outras tabelas
                        Console.WriteLine($"Erro ao processar tabela {table.TableName}: {ex.Message}");
                    }

                    snapshot.Tables.Add(tableFullInfo);
                }

                // 5. Salvar em JSON local
                var snapshotDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "snapshots");
                Directory.CreateDirectory(snapshotDir);
                
                var fileName = $"snapshot_{database.Id}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
                var filePath = Path.Combine(snapshotDir, fileName);
                
                var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, json);

                return snapshot;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Erro ao gerar snapshot da base de dados: {ex.Message}", ex);
            }
        }
    }
}
