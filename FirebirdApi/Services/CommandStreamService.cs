using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using FirebirdApi.Protos;
using System.Text.Json;
using System.Threading.Channels;

namespace FirebirdApi.Services
{
    public class CommandStreamService : ICommandStreamService, IHostedService
    {
        private readonly ILogger<CommandStreamService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IMachineIdService _machineIdService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ITokenStorageService _tokenStorageService;
        private readonly SemaphoreSlim _reconnectSemaphore = new(1, 1);
        
        private GrpcChannel? _channel;
        private CommandService.CommandServiceClient? _client;
        private AsyncDuplexStreamingCall<ClientToServerMessage, ServerToClientMessage>? _stream;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _streamingTask;
        
        private readonly Channel<CommandReceivedEventArgs> _commandChannel;
        private readonly ChannelWriter<CommandReceivedEventArgs> _commandWriter;
        private readonly ChannelReader<CommandReceivedEventArgs> _commandReader;

        public bool IsConnected { get; private set; }
        public string? CurrentConnectionId { get; private set; }
        public string? CurrentMachineId { get; private set; }
        public string? CurrentUserId { get; private set; }
        public DateTime? LastConnectedAt { get; private set; }
        public DateTime? LastSeenAt { get; private set; }
        public ChannelReader<CommandReceivedEventArgs> CommandReceived => _commandReader;
        
        // Configurações de reconexão
        private readonly int _maxReconnectAttempts = 5;
        private readonly int _reconnectDelaySeconds = 5;
        private int _reconnectAttempts = 0;
        private bool _shouldReconnect = true;

        public CommandStreamService(
            ILogger<CommandStreamService> logger,
            IConfiguration configuration,
            IMachineIdService machineIdService,
            IServiceProvider serviceProvider,
            ITokenStorageService tokenStorageService)
        {
            _logger = logger;
            _configuration = configuration;
            _machineIdService = machineIdService;
            _serviceProvider = serviceProvider;
            _tokenStorageService = tokenStorageService;
            
            // Criar channel para comunicação com o host
            _commandChannel = Channel.CreateUnbounded<CommandReceivedEventArgs>();
            _commandWriter = _commandChannel.Writer;
            _commandReader = _commandChannel.Reader;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("CommandStreamService inicializado - aguardando comando para iniciar streaming...");
            
            // Não iniciar automaticamente - aguardar comando explícito
            // O streaming será iniciado via endpoint /api/Streaming/start-streaming
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Parando CommandStreamService...");
            _shouldReconnect = false; // Impedir reconexões automáticas
            await StopStreamingAsync();
        }

        public async Task StartStreamingAsync(string connectionId, string machineId, string? authToken = null, string? nodeId = null, string? name = null, string? machineName = null)
        {
            try
            {
                if (IsConnected)
                {
                    _logger.LogWarning("Streaming já está ativo. Parando conexão anterior...");
                    await StopStreamingAsync();
                }

                var cloudServerUrl = _configuration["CloudServer:Url"] ?? "https://localhost:7001";
                _logger.LogInformation("Conectando ao servidor cloud: {CloudServerUrl}", cloudServerUrl);

                // Configurar HttpClientHandler para lidar com certificados SSL
                var httpHandler = new HttpClientHandler();
                if (cloudServerUrl.StartsWith("https://localhost") || cloudServerUrl.StartsWith("https://127.0.0.1"))
                {
                    httpHandler.ServerCertificateCustomValidationCallback = 
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                }

                var httpClient = new HttpClient(httpHandler);
                httpClient.Timeout = Timeout.InfiniteTimeSpan;

                var channelOptions = new GrpcChannelOptions
                {
                    HttpClient = httpClient,
                    DisposeHttpClient = true
                };

                _channel = GrpcChannel.ForAddress(cloudServerUrl, channelOptions);
                _client = new CommandService.CommandServiceClient(_channel);

                // Configurar headers de autorização se disponível
                var headers = new Metadata();
                if (!string.IsNullOrEmpty(authToken))
                {
                    headers.Add("authorization", $"Bearer {authToken}");
                }

                _cancellationTokenSource = new CancellationTokenSource();
                _stream = _client.CommandStream(headers: headers, cancellationToken: _cancellationTokenSource.Token);

                CurrentConnectionId = connectionId;
                CurrentMachineId = machineId;
                CurrentUserId = authToken; // Armazenar token para referência
                IsConnected = true;
                LastConnectedAt = DateTime.UtcNow;
                LastSeenAt = DateTime.UtcNow;

                // Enviar primeira mensagem com connection_id, node_id, name e machineName
                var firstMessage = new ClientToServerMessage
                {
                    ConnectionId = connectionId,
                    NodeId = nodeId ?? string.Empty,
                    MachineId = machineId,
                    Name = name ?? string.Empty,
                    MachineName = machineName ?? string.Empty
                };
                
                await _stream.RequestStream.WriteAsync(firstMessage);
                
                _logger.LogInformation("📤 Primeira mensagem gRPC enviada: ConnectionId={ConnectionId}, NodeId={NodeId}, MachineId={MachineId}, Name={Name}, MachineName={MachineName}", 
                    connectionId, nodeId ?? "n/a", machineId, name ?? "n/a", machineName ?? "n/a");

                _logger.LogInformation("✅ Conexão gRPC estabelecida com ID: {ConnectionId}, MachineId: {MachineId}, UserId: {UserId}", 
                    connectionId, machineId, authToken != null ? "AUTHENTICATED" : "ANONYMOUS");

                // Iniciar task para escutar comandos
                _streamingTask = Task.Run(async () => await ListenForCommandsAsync(_cancellationTokenSource.Token));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao iniciar streaming");
                IsConnected = false;
                CurrentConnectionId = null;
                throw;
            }
        }

        public async Task StopStreamingAsync()
        {
            try
            {
                IsConnected = false;
                CurrentConnectionId = null;
                CurrentMachineId = null;
                CurrentUserId = null;

                if (_stream != null)
                {
                    await _stream.RequestStream.CompleteAsync();
                    _stream.Dispose();
                    _stream = null;
                }

                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                if (_streamingTask != null)
                {
                    try
                    {
                        await _streamingTask;
                    }
                    catch (OperationCanceledException)
                    {
                        // Esperado quando cancelamos
                    }
                    _streamingTask = null;
                }

                _channel?.Dispose();
                _channel = null;
                _client = null;

                _logger.LogInformation("Streaming parado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao parar streaming");
            }
        }

        public async Task SendResponseAsync(string commandId, object response)
        {
            if (!IsConnected || _stream == null)
            {
                _logger.LogWarning("Tentativa de enviar resposta sem conexão ativa");
                return;
            }

            try
            {
                var responseJson = JsonSerializer.Serialize(new
                {
                    commandId = commandId,
                    timestamp = DateTime.UtcNow,
                    data = response
                });

                await _stream.RequestStream.WriteAsync(new ClientToServerMessage
                {
                    JsonResponse = responseJson
                });

                // Atualizar timestamp de última atividade
                LastSeenAt = DateTime.UtcNow;

                _logger.LogInformation("📤 Resposta enviada para comando {CommandId}", commandId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar resposta para comando {CommandId}", commandId);
            }
        }

        /// <summary>
        /// Obtém status detalhado da conexão gRPC
        /// </summary>
        public object GetConnectionStatus()
        {
            return new
            {
                isConnected = IsConnected,
                connectionId = CurrentConnectionId,
                machineId = CurrentMachineId,
                userId = CurrentUserId != null ? "AUTHENTICATED" : "ANONYMOUS",
                lastConnectedAt = LastConnectedAt,
                lastSeenAt = LastSeenAt,
                reconnectAttempts = _reconnectAttempts,
                maxReconnectAttempts = _maxReconnectAttempts,
                shouldReconnect = _shouldReconnect,
                timestamp = DateTime.UtcNow
            };
        }

        private async Task ListenForCommandsAsync(CancellationToken cancellationToken)
        {
            try
            {
                await foreach (var serverMessage in _stream!.ResponseStream.ReadAllAsync(cancellationToken))
                {
                    // Atualizar timestamp de última atividade
                    LastSeenAt = DateTime.UtcNow;
                    
                    _logger.LogInformation("📨 Comando recebido: {CommandText} (ID: {CommandId})", 
                        serverMessage.CommandText, serverMessage.CommandId);

                    // Criar evento para notificar o host
                    var commandEvent = new CommandReceivedEventArgs
                    {
                        CommandId = serverMessage.CommandId,
                        CommandText = serverMessage.CommandText,
                        Metadata = serverMessage.Metadata,
                        ReceivedAt = DateTime.UtcNow
                    };

                    // Enviar para o channel
                    await _commandWriter.WriteAsync(commandEvent, cancellationToken);

                    // Processar comando automaticamente
                    await ProcessCommandAsync(commandEvent);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Escuta de comandos cancelada");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao escutar comandos");
                
                // Tentar reconectar se não foi cancelamento manual
                if (_shouldReconnect && !cancellationToken.IsCancellationRequested)
                {
                    await HandleDisconnectionAsync();
                }
            }
            finally
            {
                IsConnected = false;
                CurrentConnectionId = null;
            }
        }

        private async Task ProcessCommandAsync(CommandReceivedEventArgs commandEvent)
        {
            try
            {
                var command = commandEvent.CommandText.ToUpperInvariant();
                object response;

                switch (command)
                {
                    case "GET_SYSTEM_INFO":
                        response = await GetSystemInfoAsync();
                        break;
                    case "GET_DATABASE_STATUS":
                        response = await GetDatabaseStatusAsync();
                        break;
                    case "PING":
                        response = new { message = "pong", timestamp = DateTime.UtcNow };
                        break;
                    default:
                        response = new { error = "Comando não reconhecido", command = commandEvent.CommandText };
                        break;
                }

                await SendResponseAsync(commandEvent.CommandId, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar comando {CommandId}", commandEvent.CommandId);
                
                var errorResponse = new
                {
                    error = ex.Message,
                    command = commandEvent.CommandText,
                    timestamp = DateTime.UtcNow
                };
                
                await SendResponseAsync(commandEvent.CommandId, errorResponse);
            }
        }

        private async Task<object> GetSystemInfoAsync()
        {
            var machineId = _machineIdService.GetMachineId();
            
            return new
            {
                machineId = machineId,
                os = Environment.OSVersion.ToString(),
                version = Environment.Version.ToString(),
                processorCount = Environment.ProcessorCount,
                workingSet = Environment.WorkingSet,
                timestamp = DateTime.UtcNow
            };
        }

        private async Task<object> GetDatabaseStatusAsync()
        {
            try
            {
                // Usar o GrpcClientService para obter status do banco
                using var scope = _serviceProvider.CreateScope();
                var grpcClientService = scope.ServiceProvider.GetRequiredService<GrpcClientService>();
                
                var isConnected = await grpcClientService.TestConnectionAsync();
                
                return new
                {
                    connected = isConnected,
                    timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    connected = false,
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                };
            }
        }

        private async Task<string?> GetCurrentUserTokenAsync()
        {
            try
            {
                // Primeiro, tentar obter token do contexto HTTP atual (se disponível)
                using var scope = _serviceProvider.CreateScope();
                var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
                
                var httpContext = httpContextAccessor.HttpContext;
                if (httpContext != null)
                {
                    var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
                    if (authHeader != null && authHeader.StartsWith("Bearer "))
                    {
                        var token = authHeader.Substring("Bearer ".Length).Trim();
                        // Armazenar o token para uso futuro
                        await _tokenStorageService.StoreTokenAsync(token);
                        return token;
                    }
                }

                // Se não encontrou no contexto HTTP, tentar obter do armazenamento local
                _logger.LogInformation("Token não encontrado no contexto HTTP, tentando obter de armazenamento local...");
                var storedToken = await _tokenStorageService.GetStoredTokenAsync();
                
                if (!string.IsNullOrEmpty(storedToken))
                {
                    _logger.LogInformation("Token encontrado no armazenamento local");
                    return storedToken;
                }
                
                _logger.LogInformation("Nenhum token encontrado, conectando como anônimo");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter token do usuário atual");
                return null;
            }
        }

        private async Task HandleDisconnectionAsync()
        {
            await _reconnectSemaphore.WaitAsync();
            try
            {
                if (_reconnectAttempts >= _maxReconnectAttempts)
                {
                    _logger.LogError("Máximo de tentativas de reconexão atingido ({MaxAttempts}). Parando tentativas automáticas.", 
                        _maxReconnectAttempts);
                    return;
                }

                _reconnectAttempts++;
                _logger.LogWarning("Conexão perdida. Tentativa de reconexão {Attempt}/{MaxAttempts} em {Delay}s...", 
                    _reconnectAttempts, _maxReconnectAttempts, _reconnectDelaySeconds);

                // Aguardar antes de tentar reconectar
                await Task.Delay(TimeSpan.FromSeconds(_reconnectDelaySeconds));

                try
                {
                    // Limpar recursos da conexão anterior
                    await StopStreamingAsync();

                    // Tentar reconectar
                    var machineId = _machineIdService.GetMachineId();
                    var connectionId = Guid.NewGuid().ToString();
                    var authToken = await GetCurrentUserTokenAsync();

                    await StartStreamingAsync(connectionId, machineId, authToken, null, null, null);
                    
                    _logger.LogInformation("Reconexão bem-sucedida na tentativa {Attempt}", _reconnectAttempts);
                    _reconnectAttempts = 0; // Reset contador em caso de sucesso
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha na tentativa de reconexão {Attempt}/{MaxAttempts}", 
                        _reconnectAttempts, _maxReconnectAttempts);
                    
                    // Tentar novamente se ainda não atingiu o limite
                    if (_reconnectAttempts < _maxReconnectAttempts)
                    {
                        await HandleDisconnectionAsync();
                    }
                }
            }
            finally
            {
                _reconnectSemaphore.Release();
            }
        }

        public async Task TryReconnectWithStoredTokenAsync()
        {
            await _reconnectSemaphore.WaitAsync();
            try
            {
                if (IsConnected)
                {
                    _logger.LogInformation("Já conectado, não é necessário reconectar");
                    return;
                }

                var hasToken = await _tokenStorageService.HasValidTokenAsync();
                if (!hasToken)
                {
                    _logger.LogInformation("Nenhum token válido armazenado, não é possível reconectar");
                    return;
                }

                _logger.LogInformation("Tentando reconectar com token armazenado...");
                
                var machineId = _machineIdService.GetMachineId();
                var connectionId = Guid.NewGuid().ToString();
                var authToken = await GetCurrentUserTokenAsync();

                await StartStreamingAsync(connectionId, machineId, authToken, null, null, null);
                
                _logger.LogInformation("Reconexão com token armazenado bem-sucedida");
                _reconnectAttempts = 0; // Reset contador
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao tentar reconectar com token armazenado");
            }
            finally
            {
                _reconnectSemaphore.Release();
            }
        }
    }
}
