using FirebirdApi.Services;
using System.Text.Json;

namespace FirebirdApi.Examples
{
    /// <summary>
    /// Exemplos de como usar a nova estrutura de metadados estruturada para comandos Firebird
    /// </summary>
    public class StructuredMetadataExample
    {
        /// <summary>
        /// Exemplo 1: Criar metadados para uma consulta simples
        /// </summary>
        public static string CreateSimpleQueryMetadata(string databaseId, string sql)
        {
            var metadata = new FirebirdCommandMetadata
            {
                DatabaseId = databaseId,
                Sql = sql,
                Type = "SELECT",
                Timeout = 30,
                Validate = true
            };

            return metadata.ToJson();
        }

        /// <summary>
        /// Exemplo 2: Criar metadados para inserção de dados
        /// </summary>
        public static string CreateInsertMetadata(string databaseId, string sql)
        {
            var metadata = new FirebirdCommandMetadata
            {
                DatabaseId = databaseId,
                Sql = sql,
                Type = "INSERT",
                Timeout = 30,
                Validate = true
            };

            return metadata.ToJson();
        }

        /// <summary>
        /// Exemplo 3: Criar metadados para listar tabelas
        /// </summary>
        public static string CreateListTablesMetadata(string databaseId)
        {
            var metadata = new FirebirdCommandMetadata
            {
                DatabaseId = databaseId
            };

            return metadata.ToJson();
        }

        /// <summary>
        /// Exemplo 4: Criar metadados para listar colunas de uma tabela
        /// </summary>
        public static string CreateListColumnsMetadata(string databaseId, string tableName)
        {
            var metadata = new FirebirdCommandMetadata
            {
                DatabaseId = databaseId,
                TableName = tableName
            };

            return metadata.ToJson();
        }

        /// <summary>
        /// Exemplo 5: Criar metadados com parâmetros personalizados
        /// </summary>
        public static string CreateCustomMetadata(string databaseId, string sql, int timeout, bool validate)
        {
            var metadata = new FirebirdCommandMetadata
            {
                DatabaseId = databaseId,
                Sql = sql,
                Type = "SELECT",
                Timeout = timeout,
                Validate = validate,
                AdditionalParameters = new Dictionary<string, object>
                {
                    { "maxResults", 100 },
                    { "includeInactive", false },
                    { "sortBy", "name" }
                }
            };

            return metadata.ToJson();
        }

        /// <summary>
        /// Exemplo 6: Parsear metadados de JSON string
        /// </summary>
        public static FirebirdCommandMetadata ParseMetadataFromJson(string jsonMetadata)
        {
            return FirebirdCommandMetadata.FromJson(jsonMetadata);
        }

        /// <summary>
        /// Exemplo 7: Criar comando gRPC completo
        /// </summary>
        public static object CreateGrpcCommand(string commandText, string commandId, string databaseId, string sql)
        {
            var metadata = new FirebirdCommandMetadata
            {
                DatabaseId = databaseId,
                Sql = sql,
                Type = "SELECT",
                Timeout = 30,
                Validate = true
            };

            return new
            {
                command_text = commandText,
                command_id = commandId,
                metadata = metadata.ToJson()
            };
        }

        /// <summary>
        /// Exemplo 8: Demonstração completa de uso
        /// </summary>
        public static void DemonstrateUsage()
        {
            Console.WriteLine("=== Exemplos de Uso da Nova Estrutura de Metadados ===\n");

            // Exemplo 1: Consulta simples
            var queryMetadata = CreateSimpleQueryMetadata("db-001", "SELECT COUNT(*) FROM USUARIOS");
            Console.WriteLine("1. Consulta Simples:");
            Console.WriteLine($"   Metadata: {queryMetadata}\n");

            // Exemplo 2: Inserção
            var insertMetadata = CreateInsertMetadata("db-001", "INSERT INTO USUARIOS (NOME, EMAIL) VALUES ('João', 'joao@email.com')");
            Console.WriteLine("2. Inserção de Dados:");
            Console.WriteLine($"   Metadata: {insertMetadata}\n");

            // Exemplo 3: Listar tabelas
            var tablesMetadata = CreateListTablesMetadata("db-001");
            Console.WriteLine("3. Listar Tabelas:");
            Console.WriteLine($"   Metadata: {tablesMetadata}\n");

            // Exemplo 4: Listar colunas
            var columnsMetadata = CreateListColumnsMetadata("db-001", "USUARIOS");
            Console.WriteLine("4. Listar Colunas:");
            Console.WriteLine($"   Metadata: {columnsMetadata}\n");

            // Exemplo 5: Parâmetros personalizados
            var customMetadata = CreateCustomMetadata("db-001", "SELECT * FROM USUARIOS WHERE ATIVO = 1", 60, true);
            Console.WriteLine("5. Parâmetros Personalizados:");
            Console.WriteLine($"   Metadata: {customMetadata}\n");

            // Exemplo 6: Parsear de volta
            var parsedMetadata = ParseMetadataFromJson(queryMetadata);
            Console.WriteLine("6. Parsear de Volta:");
            Console.WriteLine($"   DatabaseId: {parsedMetadata.DatabaseId}");
            Console.WriteLine($"   Sql: {parsedMetadata.Sql}");
            Console.WriteLine($"   Type: {parsedMetadata.Type}");
            Console.WriteLine($"   Timeout: {parsedMetadata.Timeout}");
            Console.WriteLine($"   Validate: {parsedMetadata.Validate}\n");

            // Exemplo 7: Comando gRPC completo
            var grpcCommand = CreateGrpcCommand("FIREBIRD_QUERY", "cmd-001", "db-001", "SELECT * FROM USUARIOS");
            Console.WriteLine("7. Comando gRPC Completo:");
            Console.WriteLine($"   {JsonSerializer.Serialize(grpcCommand, new JsonSerializerOptions { WriteIndented = true })}\n");
        }
    }
}
