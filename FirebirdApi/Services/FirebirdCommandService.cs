using FirebirdSql.Data.FirebirdClient;
using FirebirdApi.Models;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text.RegularExpressions;

namespace FirebirdApi.Services
{
    /// <summary>
    /// Serviço para execução de comandos Firebird via gRPC
    /// </summary>
    public class FirebirdCommandService : IFirebirdCommandService
    {
        private readonly IFirebirdService _firebirdService;
        private readonly IDatabaseConfigService _databaseConfigService;
        private readonly ILogger<FirebirdCommandService> _logger;

        public FirebirdCommandService(
            IFirebirdService firebirdService,
            IDatabaseConfigService databaseConfigService,
            ILogger<FirebirdCommandService> logger)
        {
            _firebirdService = firebirdService;
            _databaseConfigService = databaseConfigService;
            _logger = logger;
        }

        public async Task<FirebirdCommandResult> ExecuteCommandAsync(FirebirdCommand command, string? databaseId = null)
        {
            var startTime = DateTime.UtcNow;
            var commandId = Guid.NewGuid().ToString();

            try
            {
                _logger.LogInformation("Executando comando Firebird: {CommandType} - {CommandId}", command.Type, commandId);

                // Validar comando se necessário
                if (command.ValidateSyntax)
                {
                    ValidateCommand(command);
                }

                // Determinar tipo de execução baseado no comando
                var result = command.Type switch
                {
                    FirebirdCommandType.Select => await ExecuteSelectCommandAsync(command, databaseId),
                    FirebirdCommandType.Insert or FirebirdCommandType.Update or FirebirdCommandType.Delete => 
                        await ExecuteNonQueryCommandAsync(command, databaseId),
                    FirebirdCommandType.Create or FirebirdCommandType.Alter or FirebirdCommandType.Drop => 
                        await ExecuteNonQueryCommandAsync(command, databaseId),
                    _ => await ExecuteGenericCommandAsync(command, databaseId)
                };

                result.CommandId = commandId;
                result.ExecutionTime = DateTime.UtcNow - startTime;
                result.Success = true;

                _logger.LogInformation("Comando executado com sucesso: {CommandId} em {ExecutionTime}ms", 
                    commandId, result.ExecutionTime.TotalMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar comando Firebird: {CommandId}", commandId);

                return new FirebirdCommandResult
                {
                    CommandId = commandId,
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime,
                    ExecutedAt = DateTime.UtcNow
                };
            }
        }

        public async Task<FirebirdQueryResult> ExecuteQueryAsync(string query, string? databaseId = null)
        {
            var startTime = DateTime.UtcNow;
            var commandId = Guid.NewGuid().ToString();

            try
            {
                _logger.LogInformation("Executando query SELECT: {CommandId}", commandId);

                var dataTable = await _firebirdService.ExecuteQueryAsync(query, databaseId);
                var result = ConvertDataTableToQueryResult(dataTable);

                result.CommandId = commandId;
                result.Success = true;
                result.ExecutionTime = DateTime.UtcNow - startTime;

                _logger.LogInformation("Query executada com sucesso: {CommandId} - {RowCount} linhas em {ExecutionTime}ms", 
                    commandId, result.RowCount, result.ExecutionTime.TotalMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar query: {CommandId}", commandId);

                return new FirebirdQueryResult
                {
                    CommandId = commandId,
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime,
                    ExecutedAt = DateTime.UtcNow
                };
            }
        }

        public async Task<FirebirdCommandResult> ExecuteNonQueryAsync(string command, string? databaseId = null)
        {
            var startTime = DateTime.UtcNow;
            var commandId = Guid.NewGuid().ToString();

            try
            {
                _logger.LogInformation("Executando comando NonQuery: {CommandId}", commandId);

                var rowsAffected = await _firebirdService.ExecuteNonQueryAsync(command, databaseId);

                var result = new FirebirdCommandResult
                {
                    CommandId = commandId,
                    Success = true,
                    RowsAffected = rowsAffected,
                    ExecutionTime = DateTime.UtcNow - startTime,
                    ExecutedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Comando NonQuery executado com sucesso: {CommandId} - {RowsAffected} linhas afetadas em {ExecutionTime}ms", 
                    commandId, rowsAffected, result.ExecutionTime.TotalMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar comando NonQuery: {CommandId}", commandId);

                return new FirebirdCommandResult
                {
                    CommandId = commandId,
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime,
                    ExecutedAt = DateTime.UtcNow
                };
            }
        }

        public async Task<FirebirdScalarResult> ExecuteScalarAsync(string command, string? databaseId = null)
        {
            var startTime = DateTime.UtcNow;
            var commandId = Guid.NewGuid().ToString();

            try
            {
                _logger.LogInformation("Executando comando Scalar: {CommandId}", commandId);

                var value = await _firebirdService.ExecuteScalarAsync(command, databaseId);

                var result = new FirebirdScalarResult
                {
                    CommandId = commandId,
                    Success = true,
                    Value = value,
                    ExecutionTime = DateTime.UtcNow - startTime,
                    ExecutedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Comando Scalar executado com sucesso: {CommandId} em {ExecutionTime}ms", 
                    commandId, result.ExecutionTime.TotalMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar comando Scalar: {CommandId}", commandId);

                return new FirebirdScalarResult
                {
                    CommandId = commandId,
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTime = DateTime.UtcNow - startTime,
                    ExecutedAt = DateTime.UtcNow
                };
            }
        }

        public async Task<FirebirdQueryResult> ListTablesAsync(string? databaseId = null)
        {
            var query = @"
                SELECT 
                    RDB$RELATION_NAME as TABLE_NAME,
                    RDB$DESCRIPTION as DESCRIPTION,
                    CASE 
                        WHEN RDB$VIEW_BLR IS NULL THEN 'TABLE'
                        ELSE 'VIEW'
                    END as OBJECT_TYPE
                FROM RDB$RELATIONS 
                WHERE RDB$SYSTEM_FLAG IS NULL OR RDB$SYSTEM_FLAG = 0
                ORDER BY RDB$RELATION_NAME";

            return await ExecuteQueryAsync(query, databaseId);
        }

        public async Task<FirebirdQueryResult> ListTableColumnsAsync(string tableName, string? databaseId = null)
        {
            var query = @"
                SELECT 
                    RF.RDB$FIELD_NAME as COLUMN_NAME,
                    F.RDB$FIELD_TYPE as FIELD_TYPE,
                    F.RDB$FIELD_LENGTH as FIELD_LENGTH,
                    F.RDB$FIELD_SCALE as FIELD_SCALE,
                    F.RDB$FIELD_PRECISION as FIELD_PRECISION,
                    RF.RDB$NULL_FLAG as IS_NULLABLE,
                    RF.RDB$DEFAULT_VALUE as DEFAULT_VALUE,
                    RF.RDB$DESCRIPTION as DESCRIPTION
                FROM RDB$RELATION_FIELDS RF
                JOIN RDB$FIELDS F ON RF.RDB$FIELD_SOURCE = F.RDB$FIELD_NAME
                WHERE RF.RDB$RELATION_NAME = @tableName
                ORDER BY RF.RDB$FIELD_POSITION";

            // Para comandos com parâmetros, precisamos usar uma abordagem diferente
            // Por enquanto, vamos usar string replacement (em produção, usar parâmetros preparados)
            query = query.Replace("@tableName", $"'{tableName.ToUpper()}'");

            return await ExecuteQueryAsync(query, databaseId);
        }

        public async Task<FirebirdDatabaseInfo> GetDatabaseInfoAsync(string? databaseId = null)
        {
            try
            {
                var config = await GetDatabaseConfigAsync(databaseId);
                var isConnected = await _firebirdService.TestConnectionAsync(databaseId);

                var info = new FirebirdDatabaseInfo
                {
                    DatabaseName = config.Name,
                    IsConnected = isConnected,
                    ConnectionString = BuildConnectionString(config)
                };

                if (isConnected)
                {
                    try
                    {
                        // Obter versão do servidor
                        var versionQuery = "SELECT RDB$GET_CONTEXT('SYSTEM', 'ENGINE_VERSION') FROM RDB$DATABASE";
                        var versionResult = await _firebirdService.ExecuteScalarAsync(versionQuery, databaseId);
                        info.ServerVersion = versionResult?.ToString() ?? "Unknown";

                        // Contar tabelas
                        var tableCountQuery = "SELECT COUNT(*) FROM RDB$RELATIONS WHERE RDB$SYSTEM_FLAG IS NULL OR RDB$SYSTEM_FLAG = 0";
                        var tableCountResult = await _firebirdService.ExecuteScalarAsync(tableCountQuery, databaseId);
                        info.TableCount = Convert.ToInt32(tableCountResult);

                        // Obter tamanho do arquivo (se disponível)
                        if (config.FileSizeBytes.HasValue)
                        {
                            info.DatabaseSize = config.FileSizeBytes.Value;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erro ao obter informações detalhadas da base de dados");
                    }
                }

                return info;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter informações da base de dados");
                return new FirebirdDatabaseInfo
                {
                    DatabaseName = "Unknown",
                    IsConnected = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<FirebirdConnectionStatus> TestConnectionAsync(string? databaseId = null)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                var isConnected = await _firebirdService.TestConnectionAsync(databaseId);
                var responseTime = DateTime.UtcNow - startTime;

                var status = new FirebirdConnectionStatus
                {
                    IsConnected = isConnected,
                    ResponseTime = responseTime,
                    TestedAt = DateTime.UtcNow
                };

                if (isConnected)
                {
                    try
                    {
                        var versionQuery = "SELECT RDB$GET_CONTEXT('SYSTEM', 'ENGINE_VERSION') FROM RDB$DATABASE";
                        var version = await _firebirdService.ExecuteScalarAsync(versionQuery, databaseId);
                        status.ServerVersion = version?.ToString();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erro ao obter versão do servidor");
                    }
                }

                return status;
            }
            catch (Exception ex)
            {
                return new FirebirdConnectionStatus
                {
                    IsConnected = false,
                    ErrorMessage = ex.Message,
                    ResponseTime = DateTime.UtcNow - startTime,
                    TestedAt = DateTime.UtcNow
                };
            }
        }

        #region Private Methods

        private async Task<FirebirdQueryResult> ExecuteSelectCommandAsync(FirebirdCommand command, string? databaseId)
        {
            var dataTable = await _firebirdService.ExecuteQueryAsync(command.CommandText, databaseId);
            return ConvertDataTableToQueryResult(dataTable);
        }

        private async Task<FirebirdCommandResult> ExecuteNonQueryCommandAsync(FirebirdCommand command, string? databaseId)
        {
            var rowsAffected = await _firebirdService.ExecuteNonQueryAsync(command.CommandText, databaseId);
            return new FirebirdCommandResult
            {
                Success = true,
                RowsAffected = rowsAffected
            };
        }

        private async Task<FirebirdCommandResult> ExecuteGenericCommandAsync(FirebirdCommand command, string? databaseId)
        {
            // Para comandos genéricos, tentar executar como NonQuery primeiro
            try
            {
                var rowsAffected = await _firebirdService.ExecuteNonQueryAsync(command.CommandText, databaseId);
                return new FirebirdCommandResult
                {
                    Success = true,
                    RowsAffected = rowsAffected
                };
            }
            catch
            {
                // Se falhar como NonQuery, tentar como Scalar
                var value = await _firebirdService.ExecuteScalarAsync(command.CommandText, databaseId);
                return new FirebirdCommandResult
                {
                    Success = true,
                    Data = value
                };
            }
        }

        private FirebirdQueryResult ConvertDataTableToQueryResult(DataTable dataTable)
        {
            var result = new FirebirdQueryResult
            {
                Success = true,
                ExecutedAt = DateTime.UtcNow
            };

            // Converter colunas
            result.ColumnNames = dataTable.Columns.Cast<DataColumn>()
                .Select(c => c.ColumnName)
                .ToList();

            // Converter linhas
            foreach (DataRow row in dataTable.Rows)
            {
                var rowDict = new Dictionary<string, object>();
                foreach (DataColumn column in dataTable.Columns)
                {
                    rowDict[column.ColumnName] = row[column] == DBNull.Value ? null : row[column];
                }
                result.Rows.Add(rowDict);
            }

            return result;
        }

        private void ValidateCommand(FirebirdCommand command)
        {
            if (string.IsNullOrWhiteSpace(command.CommandText))
            {
                throw new ArgumentException("CommandText não pode ser vazio");
            }

            // Validações básicas de segurança
            var dangerousPatterns = new[]
            {
                @"\bDROP\s+DATABASE\b",
                @"\bSHUTDOWN\b",
                @"\bCREATE\s+DATABASE\b",
                @"\bALTER\s+DATABASE\b"
            };

            foreach (var pattern in dangerousPatterns)
            {
                if (Regex.IsMatch(command.CommandText, pattern, RegexOptions.IgnoreCase))
                {
                    throw new InvalidOperationException($"Comando perigoso detectado: {command.CommandText}");
                }
            }
        }

        private async Task<Models.DatabaseConfig> GetDatabaseConfigAsync(string? databaseId)
        {
            if (string.IsNullOrEmpty(databaseId))
            {
                return await _databaseConfigService.GetDefaultDatabaseAsync() 
                    ?? throw new InvalidOperationException("Nenhuma base de dados padrão configurada");
            }
            else
            {
                return await _databaseConfigService.GetDatabaseByIdAsync(databaseId)
                    ?? throw new InvalidOperationException($"Base de dados com ID '{databaseId}' não encontrada");
            }
        }

        private string BuildConnectionString(Models.DatabaseConfig config)
        {
            return $"Server={config.Server};" +
                   $"Port={config.Port};" +
                   $"Database={config.Database};" +
                   $"User={config.Username};" +
                   $"Password={config.Password};" +
                   $"Charset={config.Charset};" +
                   "Dialect=3;";
        }

        #endregion
    }
}
