


using FirebirdApi.Protos;
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

        public GrpcClientService(ILogger<GrpcClientService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            
            // Configurar o canal gRPC - ajuste a URL conforme necessário
            var grpcServerUrl = _configuration["GrpcServer:Url"] ?? "https://localhost:7001";
            
            // Configurar HttpClientHandler para lidar com certificados SSL em desenvolvimento
            var httpHandler = new HttpClientHandler();
            
            // Em desenvolvimento, ignorar erros de certificado SSL
            if (grpcServerUrl.StartsWith("https://localhost") || grpcServerUrl.StartsWith("https://127.0.0.1"))
            {
                httpHandler.ServerCertificateCustomValidationCallback = 
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }
            
            var httpClient = new HttpClient(httpHandler);
            
            // Configurar timeout
            var timeout = _configuration.GetValue<int>("GrpcServer:Timeout", 30);
            httpClient.Timeout = TimeSpan.FromSeconds(timeout);
            
            var channelOptions = new GrpcChannelOptions
            {
                HttpClient = httpClient,
                DisposeHttpClient = true
            };
            
            var channel = GrpcChannel.ForAddress(grpcServerUrl, channelOptions);
            _grpcClient = new DashBirdService.DashBirdServiceClient(channel);
            
            _logger.LogInformation("Cliente gRPC configurado para: {GrpcServerUrl} com timeout de {Timeout}s", 
                grpcServerUrl, timeout);
        }

        public async Task<bool> TestConnectionAsync(string? databaseId = null)
        {
            try
            {
                var request = new TestConnectionRequest
                {
                    DatabaseId = databaseId ?? "default"
                };

                var response = await _grpcClient.TestConnectionAsync(request);
                _logger.LogInformation("gRPC TestConnection: {Success} - {Message}", response.Success, response.Message);
                
                return response.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao testar conexão via gRPC");
                throw;
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
