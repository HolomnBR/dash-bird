using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using FirebirdApi.Protos;
using System.Text.Json;

namespace FirebirdApi.Services
{
    public interface INodeRegistrationGrpcService
    {
        Task<RegisterNodeResponse> RegisterNodeAsync(RegisterNodeRequest request);
        Task<RegisterAnonymousNodeResponse> RegisterAnonymousNodeAsync(RegisterAnonymousNodeRequest request);
        Task<bool> IsNodeRegisteredAsync(string nodeId);
        void Dispose();
    }

    public class NodeRegistrationGrpcService : INodeRegistrationGrpcService, IDisposable
    {
        private readonly ILogger<NodeRegistrationGrpcService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private GrpcChannel? _channel;
        private NodeRegistrationService.NodeRegistrationServiceClient? _client;
        private bool _disposed = false;

        public NodeRegistrationGrpcService(
            ILogger<NodeRegistrationGrpcService> logger,
            IConfiguration configuration,
            HttpClient httpClient)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task<RegisterNodeResponse> RegisterNodeAsync(RegisterNodeRequest request)
        {
            try
            {
                _logger.LogInformation("🔄 Registrando nó autenticado via gRPC: {MachineId}", request.MachineId);

                await EnsureClientConnectedAsync();

                // Configurar headers de autenticação
                var headers = new Metadata();
                if (!string.IsNullOrEmpty(request.AuthToken))
                {
                    headers.Add("authorization", $"Bearer {request.AuthToken}");
                    _logger.LogInformation("🔑 Token de autenticação configurado para registro: {TokenLength} caracteres", request.AuthToken.Length);
                }

                var callOptions = new CallOptions(headers: headers);
                var response = await _client!.RegisterNodeAsync(request, callOptions);
                
                _logger.LogInformation("✅ Nó autenticado registrado via gRPC: {NodeId}", response.NodeId);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao registrar nó autenticado via gRPC: {MachineId}", request.MachineId);
                return new RegisterNodeResponse
                {
                    Success = false,
                    Message = $"Erro interno: {ex.Message}"
                };
            }
        }

        public async Task<RegisterAnonymousNodeResponse> RegisterAnonymousNodeAsync(RegisterAnonymousNodeRequest request)
        {
            try
            {
                _logger.LogInformation("🔄 Registrando nó anônimo via gRPC: {MachineId}", request.MachineId);

                await EnsureClientConnectedAsync();

                var response = await _client!.RegisterAnonymousNodeAsync(request);
                
                _logger.LogInformation("✅ Nó anônimo registrado via gRPC: {NodeId}", response.NodeId);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao registrar nó anônimo via gRPC: {MachineId}", request.MachineId);
                return new RegisterAnonymousNodeResponse
                {
                    Success = false,
                    Message = $"Erro interno: {ex.Message}"
                };
            }
        }

        public async Task<bool> IsNodeRegisteredAsync(string nodeId)
        {
            try
            {
                _logger.LogInformation("🔍 Verificando se nó está registrado: {NodeId}", nodeId);

                await EnsureClientConnectedAsync();

                // Para verificar se o nó está registrado, podemos usar o método PullData
                // Se conseguir executar sem erro, o nó está registrado
                var request = new PullDataRequest
                {
                    NodeId = nodeId,
                    DatabaseId = "test", // ID de teste
                    LastSyncTimestamp = 0
                };

                // Configurar headers de autenticação (pode ser necessário para verificação)
                var headers = new Metadata();
                var callOptions = new CallOptions(headers: headers);

                var response = await _client!.PullDataAsync(request, callOptions);
                
                var isRegistered = response.Success;
                _logger.LogInformation("📋 Status de registro do nó {NodeId}: {IsRegistered}", nodeId, isRegistered ? "REGISTRADO" : "NÃO REGISTRADO");
                
                return isRegistered;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("⚠️ Erro ao verificar registro do nó {NodeId}: {Error}", nodeId, ex.Message);
                return false;
            }
        }

        private Task EnsureClientConnectedAsync()
        {
            if (_client != null && _channel != null)
            {
                return Task.CompletedTask; // Já conectado
            }

            var cloudServerUrl = _configuration["GrpcServer:Url"] ?? _configuration["CloudServer:Url"] ?? "https://localhost:7001";
            _logger.LogInformation("🌐 Conectando ao servidor cloud para registro: {CloudUrl}", cloudServerUrl);

            // Configurar HttpClientHandler para lidar com certificados SSL
            var httpHandler = new HttpClientHandler();
            httpHandler.ServerCertificateCustomValidationCallback = 
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            var httpClient = new HttpClient(httpHandler);
            httpClient.Timeout = Timeout.InfiniteTimeSpan;

            var channelOptions = new GrpcChannelOptions
            {
                HttpClient = httpClient,
                DisposeHttpClient = true
            };

            _channel = GrpcChannel.ForAddress(cloudServerUrl, channelOptions);
            _client = new NodeRegistrationService.NodeRegistrationServiceClient(_channel);

            _logger.LogInformation("✅ Cliente gRPC de registro conectado ao servidor cloud");
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _logger.LogInformation("🔄 Disposing NodeRegistrationGrpcService...");
                    
                    _channel?.Dispose();
                    _channel = null;
                    _client = null;
                    
                    _logger.LogInformation("✅ NodeRegistrationGrpcService disposed");
                }
                
                _disposed = true;
            }
        }
    }
}
