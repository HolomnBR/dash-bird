using FirebirdApi.Models;

namespace FirebirdApi.Services
{
    /// <summary>
    /// Interface para execução de comandos Firebird via gRPC
    /// </summary>
    public interface IFirebirdCommandService
    {
        /// <summary>
        /// Executa um comando SQL no Firebird
        /// </summary>
        /// <param name="command">Comando a ser executado</param>
        /// <param name="databaseId">ID da base de dados (opcional, usa padrão se não especificado)</param>
        /// <returns>Resultado da execução</returns>
        Task<FirebirdCommandResult> ExecuteCommandAsync(FirebirdCommand command, string? databaseId = null);

        /// <summary>
        /// Executa uma query SELECT e retorna os dados
        /// </summary>
        /// <param name="query">Query SQL SELECT</param>
        /// <param name="databaseId">ID da base de dados</param>
        /// <returns>Dados retornados pela query</returns>
        Task<FirebirdQueryResult> ExecuteQueryAsync(string query, string? databaseId = null);

        /// <summary>
        /// Executa um comando que modifica dados (INSERT, UPDATE, DELETE)
        /// </summary>
        /// <param name="command">Comando SQL</param>
        /// <param name="databaseId">ID da base de dados</param>
        /// <returns>Número de linhas afetadas</returns>
        Task<FirebirdCommandResult> ExecuteNonQueryAsync(string command, string? databaseId = null);

        /// <summary>
        /// Executa um comando que retorna um valor único
        /// </summary>
        /// <param name="command">Comando SQL</param>
        /// <param name="databaseId">ID da base de dados</param>
        /// <returns>Valor retornado</returns>
        Task<FirebirdScalarResult> ExecuteScalarAsync(string command, string? databaseId = null);

        /// <summary>
        /// Lista todas as tabelas de uma base de dados
        /// </summary>
        /// <param name="databaseId">ID da base de dados</param>
        /// <returns>Lista de tabelas</returns>
        Task<FirebirdQueryResult> ListTablesAsync(string? databaseId = null);

        /// <summary>
        /// Lista as colunas de uma tabela específica
        /// </summary>
        /// <param name="tableName">Nome da tabela</param>
        /// <param name="databaseId">ID da base de dados</param>
        /// <returns>Lista de colunas</returns>
        Task<FirebirdQueryResult> ListTableColumnsAsync(string tableName, string? databaseId = null);

        /// <summary>
        /// Obtém informações sobre uma base de dados
        /// </summary>
        /// <param name="databaseId">ID da base de dados</param>
        /// <returns>Informações da base</returns>
        Task<FirebirdDatabaseInfo> GetDatabaseInfoAsync(string? databaseId = null);

        /// <summary>
        /// Testa a conexão com uma base de dados
        /// </summary>
        /// <param name="databaseId">ID da base de dados</param>
        /// <returns>Status da conexão</returns>
        Task<FirebirdConnectionStatus> TestConnectionAsync(string? databaseId = null);
    }

    /// <summary>
    /// Metadados estruturados para comandos Firebird
    /// </summary>
    public class FirebirdCommandMetadata
    {
        /// <summary>
        /// ID da base de dados onde executar o comando
        /// </summary>
        public string? DatabaseId { get; set; }

        /// <summary>
        /// SQL a ser executado
        /// </summary>
        public string? Sql { get; set; }

        /// <summary>
        /// Tipo do comando (SELECT, INSERT, UPDATE, etc.)
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Nome da tabela (para comandos específicos de tabela)
        /// </summary>
        public string? TableName { get; set; }

        /// <summary>
        /// Timeout em segundos (padrão: 30)
        /// </summary>
        public int? Timeout { get; set; } = 30;

        /// <summary>
        /// Se deve validar a sintaxe do comando (padrão: true)
        /// </summary>
        public bool? Validate { get; set; } = true;

        /// <summary>
        /// Parâmetros adicionais específicos do comando
        /// </summary>
        public Dictionary<string, object> AdditionalParameters { get; set; } = new();

        /// <summary>
        /// Converte os metadados para JSON string
        /// </summary>
        public string ToJson()
        {
            return System.Text.Json.JsonSerializer.Serialize(this, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
        }

        /// <summary>
        /// Cria metadados a partir de JSON string
        /// </summary>
        public static FirebirdCommandMetadata FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return new FirebirdCommandMetadata();

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<FirebirdCommandMetadata>(json, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                }) ?? new FirebirdCommandMetadata();
            }
            catch
            {
                return new FirebirdCommandMetadata();
            }
        }

        /// <summary>
        /// Cria metadados a partir de um dicionário
        /// </summary>
        public static FirebirdCommandMetadata FromDictionary(Dictionary<string, string> dictionary)
        {
            var metadata = new FirebirdCommandMetadata();

            if (dictionary.TryGetValue("databaseId", out var databaseId))
                metadata.DatabaseId = databaseId;

            if (dictionary.TryGetValue("sql", out var sql))
                metadata.Sql = sql;

            if (dictionary.TryGetValue("type", out var type))
                metadata.Type = type;

            if (dictionary.TryGetValue("tableName", out var tableName))
                metadata.TableName = tableName;

            if (dictionary.TryGetValue("timeout", out var timeoutStr) && int.TryParse(timeoutStr, out var timeout))
                metadata.Timeout = timeout;

            if (dictionary.TryGetValue("validate", out var validateStr) && bool.TryParse(validateStr, out var validate))
                metadata.Validate = validate;

            // Adicionar parâmetros adicionais
            foreach (var kvp in dictionary)
            {
                if (!IsStandardParameter(kvp.Key))
                {
                    metadata.AdditionalParameters[kvp.Key] = kvp.Value;
                }
            }

            return metadata;
        }

        private static bool IsStandardParameter(string key)
        {
            var standardParams = new[] { "databaseId", "sql", "type", "tableName", "timeout", "validate" };
            return standardParams.Contains(key, StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Representa um comando Firebird
    /// </summary>
    public class FirebirdCommand
    {
        public string CommandText { get; set; } = string.Empty;
        public FirebirdCommandType Type { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
        public int TimeoutSeconds { get; set; } = 30;
        public bool ValidateSyntax { get; set; } = true;
    }

    /// <summary>
    /// Tipos de comando Firebird
    /// </summary>
    public enum FirebirdCommandType
    {
        Select,
        Insert,
        Update,
        Delete,
        Create,
        Alter,
        Drop,
        Execute,
        Other
    }

    /// <summary>
    /// Resultado da execução de um comando
    /// </summary>
    public class FirebirdCommandResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int RowsAffected { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
        public string? CommandId { get; set; }
        public object? Data { get; set; }
    }

    /// <summary>
    /// Resultado de uma query SELECT
    /// </summary>
    public class FirebirdQueryResult : FirebirdCommandResult
    {
        public List<Dictionary<string, object>> Rows { get; set; } = new();
        public int RowCount => Rows.Count;
        public List<string> ColumnNames { get; set; } = new();
    }

    /// <summary>
    /// Resultado de um comando SCALAR
    /// </summary>
    public class FirebirdScalarResult : FirebirdCommandResult
    {
        public object? Value { get; set; }
    }

    /// <summary>
    /// Informações sobre uma base de dados
    /// </summary>
    public class FirebirdDatabaseInfo
    {
        public string DatabaseName { get; set; } = string.Empty;
        public string ServerVersion { get; set; } = string.Empty;
        public long DatabaseSize { get; set; }
        public int TableCount { get; set; }
        public DateTime LastBackup { get; set; }
        public bool IsConnected { get; set; }
        public string ConnectionString { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Status da conexão
    /// </summary>
    public class FirebirdConnectionStatus
    {
        public bool IsConnected { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime TestedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan ResponseTime { get; set; }
        public string? ServerVersion { get; set; }
    }
}
