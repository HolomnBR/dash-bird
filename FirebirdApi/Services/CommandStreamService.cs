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
    public class CommandStreamService : ICommandStreamService, IDisposable
    {
        private readonly ILogger<CommandStreamService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IMachineIdService _machineIdService;
        private readonly IServiceProvider _serviceProvider;
        private readonly SemaphoreSlim _reconnectSemaphore = new(1, 1);
        private bool _disposed = false;
        
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
        public string? RegisteredNodeId { get; private set; }
        public string? NodeAccessToken { get; private set; }
        public bool IsNodeRegistered { get; private set; }
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
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _configuration = configuration;
            _machineIdService = machineIdService;
            _serviceProvider = serviceProvider;
            
            // Criar channel para comunicação com o host
            _commandChannel = Channel.CreateUnbounded<CommandReceivedEventArgs>();
            _commandWriter = _commandChannel.Writer;
            _commandReader = _commandChannel.Reader;
            
            _logger.LogInformation("🔧 CommandStreamService construído e inicializado");
        }


        public async Task StartStreamingAsync(string connectionId, string machineId, string? authToken = null, string? nodeId = null, string? name = null, string? machineName = null, string? version = null, string? operatingSystem = null)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(CommandStreamService));
            }

            var startTime = DateTime.UtcNow;
            
            try
            {
                _logger.LogInformation("🚀 Iniciando streaming gRPC: ConnectionId={ConnectionId}, MachineId={MachineId}", connectionId, machineId);
                _logger.LogInformation("📋 Parâmetros completos - AuthToken: {HasToken}, NodeId: {NodeId}, Name: {Name}, MachineName: {MachineName}, Version: {Version}, OperatingSystem: {OperatingSystem}", 
                    !string.IsNullOrEmpty(authToken) ? "SIM" : "NÃO", nodeId ?? "NULL", name ?? "NULL", machineName ?? "NULL", version ?? "NULL", operatingSystem ?? "NULL");
                
                if (IsConnected)
                {
                    _logger.LogWarning("⚠️ Streaming já está ativo. Parando conexão anterior...");
                    await StopStreamingAsync();
                    await Task.Delay(1000); // Aguardar um pouco antes de reconectar
                }

                var cloudServerUrl = _configuration["GrpcServer:Url"] ?? _configuration["CloudServer:Url"] ?? "https://localhost:7001";
                _logger.LogInformation("🌐 Conectando ao servidor cloud: {CloudServerUrl}", cloudServerUrl);

                // Configurar HttpClientHandler para lidar com certificados SSL
                var httpHandler = new HttpClientHandler();
                
                // Para desenvolvimento e produção, aceitar certificados SSL inválidos
                // Em produção real, configure certificados válidos
                httpHandler.ServerCertificateCustomValidationCallback = 
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                
                _logger.LogInformation("🔒 SSL certificate validation desabilitada para desenvolvimento/produção");

                var httpClient = new HttpClient(httpHandler);
                httpClient.Timeout = Timeout.InfiniteTimeSpan;

                var channelOptions = new GrpcChannelOptions
                {
                    HttpClient = httpClient,
                    DisposeHttpClient = true
                };

                _logger.LogInformation("🔧 Criando canal gRPC...");
                _channel = GrpcChannel.ForAddress(cloudServerUrl, channelOptions);
                _client = new CommandService.CommandServiceClient(_channel);
                _logger.LogInformation("✅ Canal gRPC criado com sucesso para: {CloudServerUrl}", cloudServerUrl);

                // Configurar headers de autorização se disponível
                var headers = new Metadata();
                if (!string.IsNullOrEmpty(authToken))
                {
                    headers.Add("authorization", $"Bearer {authToken}");
                    _logger.LogInformation("🔑 Token de autenticação configurado: {TokenLength} caracteres", authToken.Length);
                }
                else
                {
                    _logger.LogInformation("⚠️ Nenhum token de autenticação fornecido - conectando como anônimo");
                }

                _logger.LogInformation("📡 Estabelecendo stream gRPC...");
                _cancellationTokenSource = new CancellationTokenSource();
                
                try
                {
                    _stream = _client.CommandStream(headers: headers, cancellationToken: _cancellationTokenSource.Token);
                    _logger.LogInformation("✅ Stream gRPC estabelecido com sucesso");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ ERRO ao estabelecer stream gRPC: {Error}", ex.Message);
                    throw;
                }

                CurrentConnectionId = connectionId;
                CurrentMachineId = machineId;
                CurrentUserId = authToken; // Armazenar token para referência
                IsConnected = true;
                LastConnectedAt = DateTime.UtcNow;
                LastSeenAt = DateTime.UtcNow;


                _logger.LogInformation("✅ Stream gRPC estabelecido com sucesso");

                // Enviar primeira mensagem com connection_id, node_id, name, machineName e informações do sistema
                var firstMessage = new ClientToServerMessage
                {
                    ConnectionId = connectionId,
                    NodeId = nodeId ?? string.Empty,
                    MachineId = machineId,
                    Name = name ?? string.Empty,
                    MachineName = machineName ?? string.Empty,
                    Version = version ?? string.Empty,
                    OperatingSystem = operatingSystem ?? string.Empty,
                    UserId = authToken ?? string.Empty // Incluir userId (token de autenticação)
                };
                
                _logger.LogInformation("📤 Dados da primeira mensagem gRPC - ConnectionId: {ConnectionId}, NodeId: {NodeId}, MachineId: {MachineId}, Name: {Name}, MachineName: {MachineName}, Version: {Version}, OperatingSystem: {OperatingSystem}, UserId: {UserId}", 
                    firstMessage.ConnectionId, firstMessage.NodeId, firstMessage.MachineId, firstMessage.Name, firstMessage.MachineName, firstMessage.Version, firstMessage.OperatingSystem, firstMessage.UserId);
                
                _logger.LogInformation("📤 Enviando primeira mensagem gRPC...");
                
                try
                {
                    await _stream.RequestStream.WriteAsync(firstMessage);
                    _logger.LogInformation("✅ Primeira mensagem gRPC enviada com sucesso");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ ERRO ao enviar primeira mensagem gRPC: {Error}", ex.Message);
                    throw;
                }
                
                _logger.LogInformation("📤 Primeira mensagem gRPC enviada: ConnectionId={ConnectionId}, NodeId={NodeId}, MachineId={MachineId}, Name={Name}, MachineName={MachineName}, Version={Version}, OperatingSystem={OperatingSystem}, UserId={UserId}", 
                    connectionId, nodeId ?? "n/a", machineId, name ?? "n/a", machineName ?? "n/a", version ?? "n/a", operatingSystem ?? "n/a", authToken != null ? "AUTHENTICATED" : "ANONYMOUS");

                _logger.LogInformation("✅ Conexão gRPC estabelecida com ID: {ConnectionId}, MachineId: {MachineId}, UserId: {UserId}", 
                    connectionId, machineId, authToken != null ? "AUTHENTICATED" : "ANONYMOUS");

                // Iniciar task para escutar comandos
                _logger.LogInformation("👂 Iniciando task de escuta de comandos...");
                _streamingTask = Task.Run(async () => await ListenForCommandsAsync(_cancellationTokenSource.Token));
                
                var duration = DateTime.UtcNow - startTime;
                _logger.LogInformation("🎉 Streaming gRPC iniciado com sucesso em {Duration}ms", duration.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogError(ex, "❌ Erro ao iniciar streaming gRPC após {Duration}ms: {Error}", duration.TotalMilliseconds, ex.Message);
                
                // Limpar estado em caso de erro
                IsConnected = false;
                CurrentConnectionId = null;
                CurrentMachineId = null;
                CurrentUserId = null;
                
                // Log específico para diferentes tipos de erro
                if (ex.Message.Contains("Connection refused") || ex.Message.Contains("No connection could be made"))
                {
                    _logger.LogError("❌ Servidor cloud não está acessível ou não está rodando");
                }
                else if (ex.Message.Contains("SSL") || ex.Message.Contains("certificate"))
                {
                    _logger.LogError("❌ Erro de SSL/Certificado - Verifique a configuração de certificados");
                }
                else if (ex.Message.Contains("PermissionDenied") || ex.Message.Contains("403"))
                {
                    _logger.LogError("❌ Erro de permissão - Verifique se o token de autenticação é válido");
                }
                
                throw;
            }
        }

        public async Task StopStreamingAsync()
        {
            if (_disposed)
            {
                _logger.LogInformation("CommandStreamService já foi disposed, ignorando StopStreamingAsync");
                return; // Já foi disposed, não fazer nada
            }

            _logger.LogInformation("🔄 Iniciando parada do streaming...");
            _shouldReconnect = false; // Impedir reconexões automáticas

            try
            {
                // 1. Marcar como desconectado primeiro
                IsConnected = false;
                CurrentConnectionId = null;
                CurrentMachineId = null;
                CurrentUserId = null;

                // 2. Cancelar token de cancelamento
                _cancellationTokenSource?.Cancel();

                // 3. Parar o stream gRPC
                if (_stream != null)
                {
                    try
                    {
                        _logger.LogInformation("🔄 Completando RequestStream...");
                        await _stream.RequestStream.CompleteAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erro ao completar RequestStream: {Message}", ex.Message);
                    }
                    
                    try
                    {
                        _logger.LogInformation("🔄 Disposing stream...");
                        _stream.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erro ao dispose do stream: {Message}", ex.Message);
                    }
                    finally
                    {
                        _stream = null;
                    }
                }

                // 4. Aguardar task de streaming terminar
                if (_streamingTask != null)
                {
                    try
                    {
                        _logger.LogInformation("🔄 Aguardando task de streaming terminar...");
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                        await _streamingTask.WaitAsync(cts.Token);
                        _logger.LogInformation("✅ Task de streaming terminou");
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogWarning("⚠️ Timeout ao aguardar task de streaming terminar");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erro ao aguardar task de streaming: {Message}", ex.Message);
                    }
                    finally
                    {
                        _streamingTask = null;
                    }
                }

                // 5. Dispose do channel
                if (_channel != null)
                {
                    try
                    {
                        _logger.LogInformation("🔄 Disposing gRPC channel...");
                        _channel.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erro ao dispose do channel: {Message}", ex.Message);
                    }
                    finally
                    {
                        _channel = null;
                    }
                }

                // 6. Dispose do CancellationTokenSource
                if (_cancellationTokenSource != null)
                {
                    try
                    {
                        _cancellationTokenSource.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erro ao dispose do CancellationTokenSource: {Message}", ex.Message);
                    }
                    finally
                    {
                        _cancellationTokenSource = null;
                    }
                }

                _client = null;
                _logger.LogInformation("✅ Streaming parado com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao parar streaming: {Message}", ex.Message);
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
        /// Verifica se o nó está devidamente registrado no servidor
        /// </summary>
        public async Task<bool> IsNodeRegisteredAsync()
        {
            if (!IsConnected || _stream == null)
            {
                _logger.LogWarning("⚠️ Não é possível verificar registro: streaming não está conectado");
                return false;
            }

            try
            {
                _logger.LogInformation("🔍 Verificando se nó está registrado no servidor cloud via gRPC...");
                
                // Usar o serviço de registro gRPC para verificar se o nó está registrado
                var nodeId = RegisteredNodeId ?? CurrentConnectionId ?? string.Empty;
                
                using var scope = _serviceProvider.CreateScope();
                var nodeRegistrationService = scope.ServiceProvider.GetRequiredService<INodeRegistrationGrpcService>();
                var isRegistered = await nodeRegistrationService.IsNodeRegisteredAsync(nodeId);
                
                // Atualizar o status interno
                IsNodeRegistered = isRegistered;
                
                _logger.LogInformation("📋 Status de registro do nó {NodeId}: {IsRegistered}", 
                    nodeId, isRegistered ? "REGISTRADO" : "NÃO REGISTRADO");
                
                return isRegistered;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("⚠️ Erro ao verificar registro do nó via gRPC: {Error}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Envia dados de sincronização de databases via gRPC
        /// </summary>
        public async Task SendDatabaseSyncAsync(string syncType, string syncReason, List<Models.DatabaseConfig> databases)
        {
            if (!IsConnected || _stream == null)
            {
                _logger.LogWarning("Tentativa de enviar sincronização de databases sem conexão ativa");
                throw new InvalidOperationException("Streaming gRPC não está conectado");
            }

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var systemInfoService = scope.ServiceProvider.GetRequiredService<ISystemInfoService>();

                _logger.LogInformation("🔄 Preparando envio de {Count} databases via gRPC: {SyncType} - {SyncReason}", 
                    databases.Count, syncType, syncReason);

                // Converter para formato gRPC
                var databaseSyncData = new DatabaseSyncData
                {
                    SyncType = syncType,
                    SyncReason = syncReason,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                foreach (var db in databases)
                {
                    var grpcDatabase = new DatabaseConfig
                    {
                        Id = db.Id,
                        Name = db.Name,
                        Server = db.Server,
                        Database = db.Database,
                        Username = db.Username,
                        Password = db.Password,
                        Port = db.Port,
                        Charset = db.Charset,
                        FileSizeBytes = db.FileSizeBytes ?? 0,
                        LastSizeCheck = db.LastSizeCheck.HasValue ? ((DateTimeOffset)db.LastSizeCheck.Value).ToUnixTimeMilliseconds() : 0,
                        CreatedAt = ((DateTimeOffset)db.CreatedAt).ToUnixTimeMilliseconds(),
                        IsActive = db.IsActive,
                        DesktopNodeId = CurrentConnectionId ?? string.Empty // Vincular database ao nó atual usando o ID real do nó
                    };

                    databaseSyncData.Databases.Add(grpcDatabase);
                    
                    _logger.LogInformation("📋 Database preparada para envio: {Name} (ID: {Id}, Tamanho: {FileSize})", 
                        db.Name, db.Id, db.FileSizeBytes.HasValue ? FormatFileSize(db.FileSizeBytes.Value) : "N/A");
                }

                // Enviar dados de sincronização via gRPC
                var message = new ClientToServerMessage
                {
                    DatabaseSync = databaseSyncData,
                    NodeId = CurrentConnectionId ?? string.Empty,
                    MachineId = CurrentMachineId ?? string.Empty,
                    Name = string.Empty,
                    MachineName = string.Empty,
                    Version = systemInfoService.GetSystemVersion(),
                    OperatingSystem = systemInfoService.GetOperatingSystem(),
                    UserId = CurrentUserId ?? string.Empty
                };

                // Verificar se o stream está disponível antes de escrever
                if (_stream?.RequestStream != null)
                {
                    await _stream.RequestStream.WriteAsync(message);
                    _logger.LogInformation("📤 Mensagem gRPC enviada com sucesso para {Count} databases", databases.Count);
                }
                else
                {
                    _logger.LogError("❌ RequestStream não está disponível para envio de sincronização");
                    throw new InvalidOperationException("RequestStream não está disponível");
                }

                // Atualizar timestamp de última atividade
                LastSeenAt = DateTime.UtcNow;

                _logger.LogInformation("✅ Dados de sincronização de databases enviados via gRPC com sucesso: {Count} databases, tipo: {SyncType}", 
                    databases.Count, syncType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao enviar sincronização de databases via gRPC: {Error}", ex.Message);
                throw; // Re-throw para que o SyncController possa capturar e tentar HTTP
            }
        }

        /// <summary>
        /// Formata o tamanho do arquivo em formato legível
        /// </summary>
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
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
                registeredNodeId = RegisteredNodeId,
                nodeAccessToken = NodeAccessToken != null ? "AVAILABLE" : "NOT_AVAILABLE",
                isNodeRegistered = IsNodeRegistered,
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
                
                // Log específico para erros de autenticação
                if (ex.Message.Contains("PermissionDenied") || ex.Message.Contains("403"))
                {
                    _logger.LogError("❌ Erro de permissão (403) - Verifique se o token de autenticação é válido e se o servidor está acessível");
                    _logger.LogError("🔍 Detalhes do erro: {ErrorDetails}", ex.ToString());
                }
                else if (ex.Message.Contains("Unavailable") || ex.Message.Contains("Connection refused"))
                {
                    _logger.LogError("❌ Servidor não disponível - Verifique se o servidor cloud está rodando e acessível");
                }
                else if (ex.Message.Contains("SSL") || ex.Message.Contains("certificate"))
                {
                    _logger.LogError("❌ Erro de SSL/Certificado - Verifique a configuração de certificados");
                }
                
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
                    case "GET_DATABASES":
                        response = await GetDatabasesAsync();
                        break;
                    case "SYNC_DATABASES":
                        response = await HandleDatabaseSyncRequestAsync(commandEvent);
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

        private Task<object> GetSystemInfoAsync()
        {
            var machineId = _machineIdService.GetMachineId();
            
            return Task.FromResult<object>(new
            {
                machineId = machineId,
                os = Environment.OSVersion.ToString(),
                version = Environment.Version.ToString(),
                processorCount = Environment.ProcessorCount,
                workingSet = Environment.WorkingSet,
                timestamp = DateTime.UtcNow
            });
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

        private async Task<object> GetDatabasesAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var databaseConfigService = scope.ServiceProvider.GetRequiredService<IDatabaseConfigService>();
                
                var databases = await databaseConfigService.GetAllDatabasesAsync();
                
                return new
                {
                    databases = databases.Select(db => new
                    {
                        id = db.Id,
                        name = db.Name,
                        server = db.Server,
                        database = db.Database,
                        username = db.Username,
                        port = db.Port,
                        charset = db.Charset,
                        fileSizeBytes = db.FileSizeBytes,
                        lastSizeCheck = db.LastSizeCheck,
                        createdAt = db.CreatedAt,
                        isActive = db.IsActive
                    }).ToList(),
                    count = databases.Count,
                    timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    databases = new List<object>(),
                    count = 0,
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                };
            }
        }

        private async Task<object> HandleDatabaseSyncRequestAsync(CommandReceivedEventArgs commandEvent)
        {
            try
            {
                _logger.LogInformation("Processando solicitação de sincronização de databases: {CommandId}", commandEvent.CommandId);

                using var scope = _serviceProvider.CreateScope();
                var databaseConfigService = scope.ServiceProvider.GetRequiredService<IDatabaseConfigService>();
                var systemInfoService = scope.ServiceProvider.GetRequiredService<ISystemInfoService>();
                
                // Obter todas as databases locais
                var databases = await databaseConfigService.GetAllDatabasesAsync();
                
                // Atualizar tamanhos dos arquivos
                await databaseConfigService.UpdateAllDatabaseFileSizesAsync();
                
                // Converter para formato gRPC
                var databaseSyncData = new DatabaseSyncData
                {
                    SyncType = "FULL_SYNC",
                    SyncReason = "SERVER_REQUESTED",
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                foreach (var db in databases)
                {
                    databaseSyncData.Databases.Add(new DatabaseConfig
                    {
                        Id = db.Id,
                        Name = db.Name,
                        Server = db.Server,
                        Database = db.Database,
                        Username = db.Username,
                        Password = db.Password,
                        Port = db.Port,
                        Charset = db.Charset,
                        FileSizeBytes = db.FileSizeBytes ?? 0,
                        LastSizeCheck = db.LastSizeCheck.HasValue ? ((DateTimeOffset)db.LastSizeCheck.Value).ToUnixTimeMilliseconds() : 0,
                        CreatedAt = ((DateTimeOffset)db.CreatedAt).ToUnixTimeMilliseconds(),
                        IsActive = db.IsActive,
                        DesktopNodeId = CurrentConnectionId ?? string.Empty // Vincular database ao nó atual usando o ID real do nó
                    });
                }

                // Enviar dados de sincronização via gRPC
                if (_stream?.RequestStream != null)
                {
                    await _stream.RequestStream.WriteAsync(new ClientToServerMessage
                    {
                        DatabaseSync = databaseSyncData,
                        NodeId = CurrentConnectionId ?? string.Empty,
                        MachineId = CurrentMachineId ?? string.Empty,
                        Name = string.Empty,
                        MachineName = string.Empty,
                        Version = systemInfoService.GetSystemVersion(),
                        OperatingSystem = systemInfoService.GetOperatingSystem(),
                        UserId = CurrentUserId ?? string.Empty
                    });
                }
                else
                {
                    _logger.LogWarning("⚠️ RequestStream não está disponível para envio de sincronização (HandleDatabaseSyncRequestAsync)");
                    return new
                    {
                        success = false,
                        error = "RequestStream não disponível",
                        timestamp = DateTime.UtcNow
                    };
                }

                _logger.LogInformation("Dados de sincronização enviados via gRPC: {Count} databases", databases.Count);

                return new
                {
                    success = true,
                    message = "Dados de sincronização enviados via gRPC",
                    databasesCount = databases.Count,
                    timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar solicitação de sincronização de databases");
                return new
                {
                    success = false,
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
                var tokenStorageService = scope.ServiceProvider.GetRequiredService<ITokenStorageService>();
                
                var httpContext = httpContextAccessor.HttpContext;
                if (httpContext != null)
                {
                    var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
                    if (authHeader != null && authHeader.StartsWith("Bearer "))
                    {
                        var token = authHeader.Substring("Bearer ".Length).Trim();
                        // Armazenar o token para uso futuro
                        await tokenStorageService.StoreTokenAsync(token);
                        return token;
                    }
                }

                // Se não encontrou no contexto HTTP, tentar obter do armazenamento local
                _logger.LogInformation("Token não encontrado no contexto HTTP, tentando obter de armazenamento local...");
                var storedToken = await tokenStorageService.GetStoredTokenAsync();
                
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

                    await StartStreamingAsync(connectionId, machineId, authToken, null, null, null, null, null);
                    
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

                using var scope = _serviceProvider.CreateScope();
                var tokenStorageService = scope.ServiceProvider.GetRequiredService<ITokenStorageService>();
                
                var hasToken = await tokenStorageService.HasValidTokenAsync();
                if (!hasToken)
                {
                    _logger.LogInformation("Nenhum token válido armazenado, não é possível reconectar");
                    return;
                }

                _logger.LogInformation("Tentando reconectar com token armazenado...");
                
                var machineId = _machineIdService.GetMachineId();
                var connectionId = Guid.NewGuid().ToString();
                var authToken = await GetCurrentUserTokenAsync();

                await StartStreamingAsync(connectionId, machineId, authToken, null, null, null, null, null);
                
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
                    _logger.LogInformation("Dispose do CommandStreamService iniciado...");
                    
                    // Parar streaming se estiver ativo
                    if (IsConnected)
                    {
                        try
                        {
                            StopStreamingAsync().Wait(TimeSpan.FromSeconds(5));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Erro ao parar streaming durante dispose");
                        }
                    }

                    // Liberar recursos
                    _cancellationTokenSource?.Dispose();
                    _channel?.Dispose();
                    _reconnectSemaphore?.Dispose();
                    _commandChannel.Writer.Complete();
                    
                    _logger.LogInformation("CommandStreamService disposed com sucesso");
                }
                
                _disposed = true;
            }
        }
    }
}
