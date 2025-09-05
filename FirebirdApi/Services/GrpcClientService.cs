


using DashBirdServer.Protos;
using Grpc.Net.Client;
using System.Data;

namespace FirebirdApi.Services
{
    public interface IGrpcClientService
    {
        Task<bool> TestConnectionAsync(string? databaseId = null);
        Task<DataTable> ExecuteQueryAsync(string query, string? databaseId = null);
        Task<int> ExecuteNonQueryAsync(string query, string? databaseId = null);
        Task<object?> ExecuteScalarAsync(string query, string? databaseId = null);
        Task<List<string>> GetTablesAsync(string? databaseId = null);
        Task<object> GetTableInfoAsync(string tableName, string? databaseId = null);
    }

    public class GrpcClientService : IGrpcClientService
    {
        private readonly DashBirdService.DashBirdServiceClient _grpcClient;
        private readonly ILogger<GrpcClientService> _logger;
        private readonly IConfiguration _configuration;

        public GrpcClientService(DashBirdService.DashBirdServiceClient grpcClient, ILogger<GrpcClientService> logger, IConfiguration configuration)
        {
            _grpcClient = grpcClient;
            _logger = logger;
            _configuration = configuration;
            
            var grpcServerUrl = _configuration["GrpcServer:Url"] ?? "http://localhost:7001";
            _logger.LogInformation("🔗 Cliente gRPC configurado e injetado via DI");
            _logger.LogInformation("🔗 Conectando ao servidor gRPC em: {GrpcServerUrl}", grpcServerUrl);
        }

        public async Task<bool> TestConnectionAsync(string? databaseId = null)
        {
            try
            {
                _logger.LogInformation("Iniciando teste de conexão gRPC para database: {DatabaseId}", databaseId ?? "default");
                
                var request = new TestConnectionRequest
                {
                    DatabaseId = databaseId ?? "default"
                };

                _logger.LogInformation("Enviando requisição gRPC para servidor...");
                
                // Usar timeout configurado ou padrão de 30 segundos
                var timeoutSeconds = _configuration.GetValue<int>("GrpcServer:Timeout", 30);
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                var response = await _grpcClient.TestConnectionAsync(request, cancellationToken: cts.Token);
                
                _logger.LogInformation("gRPC TestConnection: {Success} - {Message}", response.Success, response.Message);
                return response.Success;
            }
            catch (OperationCanceledException)
            {
                var timeoutSeconds = _configuration.GetValue<int>("GrpcServer:Timeout", 30);
                _logger.LogError("Timeout na conexão gRPC - servidor não respondeu em {TimeoutSeconds} segundos", timeoutSeconds);
                return false;
            }
            catch (Grpc.Core.RpcException grpcEx)
            {
                _logger.LogError(grpcEx, "Erro gRPC: {StatusCode} - {Detail}", grpcEx.StatusCode, grpcEx.Status.Detail);
                return false;
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "Erro de conexão HTTP: {Message}", httpEx.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao testar conexão via gRPC. Detalhes: {ErrorType} - {ErrorMessage}", 
                    ex.GetType().Name, ex.Message);
                return false;
            }
        }

        public async Task<DataTable> ExecuteQueryAsync(string query, string? databaseId = null)
        {
            try
            {
                var request = new ExecuteQueryRequest
                {
                    Query = query,
                    DatabaseId = databaseId ?? "default"
                };

                var response = await _grpcClient.ExecuteQueryAsync(request);
                
                if (!response.Success)
                {
                    throw new Exception(response.Message);
                }

                return ConvertGrpcResponseToDataTable(response.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar query via gRPC");
                throw;
            }
        }

        public async Task<int> ExecuteNonQueryAsync(string query, string? databaseId = null)
        {
            try
            {
                var request = new ExecuteNonQueryRequest
                {
                    Query = query,
                    DatabaseId = databaseId ?? "default"
                };

                var response = await _grpcClient.ExecuteNonQueryAsync(request);
                
                if (!response.Success)
                {
                    throw new Exception(response.Message);
                }

                return response.RowsAffected;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar non-query via gRPC");
                throw;
            }
        }

        public async Task<object?> ExecuteScalarAsync(string query, string? databaseId = null)
        {
            try
            {
                var request = new ExecuteScalarRequest
                {
                    Query = query,
                    DatabaseId = databaseId ?? "default"
                };

                var response = await _grpcClient.ExecuteScalarAsync(request);
                
                if (!response.Success)
                {
                    throw new Exception(response.Message);
                }

                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar scalar via gRPC");
                throw;
            }
        }

        public async Task<List<string>> GetTablesAsync(string? databaseId = null)
        {
            try
            {
                var request = new GetTablesRequest
                {
                    DatabaseId = databaseId ?? "default"
                };

                var response = await _grpcClient.GetTablesAsync(request);
                
                if (!response.Success)
                {
                    throw new Exception(response.Message);
                }

                return response.TableNames.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter tabelas via gRPC");
                throw;
            }
        }

        public async Task<object> GetTableInfoAsync(string tableName, string? databaseId = null)
        {
            try
            {
                var request = new GetTableInfoRequest
                {
                    TableName = tableName,
                    DatabaseId = databaseId ?? "default"
                };

                var response = await _grpcClient.GetTableInfoAsync(request);
                
                if (!response.Success)
                {
                    throw new Exception(response.Message);
                }

                return new
                {
                    TableName = response.TableInfo.TableName,
                    Schema = response.TableInfo.Schema,
                    Columns = response.TableInfo.Columns.Select(c => new
                    {
                        ColumnName = c.ColumnName,
                        DataType = c.DataType,
                        IsNullable = c.IsNullable,
                        MaxLength = c.MaxLength,
                        IsPrimaryKey = c.IsPrimaryKey
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter informações da tabela via gRPC");
                throw;
            }
        }

        private DataTable ConvertGrpcResponseToDataTable(Google.Protobuf.Collections.RepeatedField<QueryRow> grpcData)
        {
            var dataTable = new DataTable();
            
            if (grpcData.Count == 0)
                return dataTable;

            // Criar colunas baseado na primeira linha
            var firstRow = grpcData[0];
            foreach (var column in firstRow.Columns)
            {
                dataTable.Columns.Add(column.Key, typeof(string));
            }

            // Adicionar linhas
            foreach (var row in grpcData)
            {
                var dataRow = dataTable.NewRow();
                foreach (var column in row.Columns)
                {
                    dataRow[column.Key] = column.Value;
                }
                dataTable.Rows.Add(dataRow);
            }

            return dataTable;
        }
    }
}
