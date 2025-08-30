using FirebirdSql.Data.FirebirdClient;
using FirebirdApi.Models;
using System.Data;

namespace FirebirdApi.Services
{
    public interface IFirebirdService
    {
        Task<DataTable> ExecuteQueryAsync(string query, string? databaseId = null);
        Task<int> ExecuteNonQueryAsync(string query, string? databaseId = null);
        Task<object> ExecuteScalarAsync(string query, string? databaseId = null);
        Task<bool> TestConnectionAsync(string? databaseId = null);
    }

    public class FirebirdService : IFirebirdService
    {
        private readonly IDatabaseConfigService _configService;

        public FirebirdService(IDatabaseConfigService configService)
        {
            _configService = configService;
        }

        private async Task<string> GetConnectionStringAsync(string? databaseId = null)
        {
            DatabaseConfig config;
            
            if (string.IsNullOrEmpty(databaseId))
            {
                // Usar base padrão se não especificada
                config = await _configService.GetDefaultDatabaseAsync() 
                    ?? throw new InvalidOperationException("Nenhuma base de dados padrão configurada");
            }
            else
            {
                // Usar base específica
                config = await _configService.GetDatabaseByIdAsync(databaseId)
                    ?? throw new InvalidOperationException($"Base de dados com ID '{databaseId}' não encontrada");
            }

            return BuildConnectionString(config);
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

        public async Task<DataTable> ExecuteQueryAsync(string query, string? databaseId = null)
        {
            var connectionString = await GetConnectionStringAsync(databaseId);
            using var connection = new FbConnection(connectionString);
            using var command = new FbCommand(query, connection);
            using var adapter = new FbDataAdapter(command);
            
            var dataTable = new DataTable();
            await Task.Run(() => adapter.Fill(dataTable));
            
            return dataTable;
        }

        public async Task<int> ExecuteNonQueryAsync(string query, string? databaseId = null)
        {
            var connectionString = await GetConnectionStringAsync(databaseId);
            using var connection = new FbConnection(connectionString);
            using var command = new FbCommand(query, connection);
            
            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync();
        }

        public async Task<object> ExecuteScalarAsync(string query, string? databaseId = null)
        {
            var connectionString = await GetConnectionStringAsync(databaseId);
            using var connection = new FbConnection(connectionString);
            using var command = new FbCommand(query, connection);
            
            await connection.OpenAsync();
            return await command.ExecuteScalarAsync();
        }

        public async Task<bool> TestConnectionAsync(string? databaseId = null)
        {
            try
            {
                var connectionString = await GetConnectionStringAsync(databaseId);
                using var connection = new FbConnection(connectionString);
                await connection.OpenAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
