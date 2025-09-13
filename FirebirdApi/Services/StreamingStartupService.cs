using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace FirebirdApi.Services
{
    /// <summary>
    /// Serviço que inicia o streaming automaticamente quando a aplicação inicia
    /// </summary>
    public class StreamingStartupService : BackgroundService
    {
        private readonly ILogger<StreamingStartupService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _startupDelay = TimeSpan.FromSeconds(10); // Aguardar 10 segundos após startup

        public StreamingStartupService(ILogger<StreamingStartupService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("🚀 StreamingStartupService iniciado - aguardando {Delay}s para iniciar streaming automático...", _startupDelay.TotalSeconds);
                
                // Aguardar um tempo para a aplicação estabilizar
                await Task.Delay(_startupDelay, stoppingToken);
                
                if (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("StreamingStartupService cancelado durante delay inicial");
                    return;
                }

                _logger.LogInformation("🔄 Iniciando streaming automático no startup da aplicação...");
                
                using var scope = _serviceProvider.CreateScope();
                await StartStreamingOnStartupAsync(scope.ServiceProvider);
                
                _logger.LogInformation("✅ StreamingStartupService concluído");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("StreamingStartupService cancelado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro no StreamingStartupService: {Error}", ex.Message);
            }
        }

        private async Task StartStreamingOnStartupAsync(IServiceProvider serviceProvider)
        {
            try
            {
                var commandStreamService = serviceProvider.GetRequiredService<ICommandStreamService>();
                var tokenStorageService = serviceProvider.GetRequiredService<ITokenStorageService>();
                var localNodeService = serviceProvider.GetRequiredService<ILocalNodeService>();
                var systemInfoService = serviceProvider.GetRequiredService<ISystemInfoService>();
                var machineIdService = serviceProvider.GetRequiredService<IMachineIdService>();

                // Verificar se já está conectado
                if (commandStreamService.IsConnected)
                {
                    _logger.LogInformation("Streaming já está conectado no startup, não é necessário iniciar");
                    return;
                }

                // Verificar se há token armazenado
                var hasToken = await tokenStorageService.HasValidTokenAsync();
                if (!hasToken)
                {
                    _logger.LogInformation("ℹ️ Nenhum token válido armazenado no startup, iniciando streaming em modo anônimo para sync de databases");
                }

                // Obter MachineId
                var machineId = machineIdService.GetMachineId();
                if (string.IsNullOrEmpty(machineId))
                {
                    _logger.LogWarning("⚠️ MachineId não disponível no startup para iniciar streaming");
                    return;
                }

                // Obter informações do nó local - tentar por MachineId primeiro, depois por nome da máquina
                var localNode = await localNodeService.GetLocalNodeByMachineIdAsync(machineId);
                if (localNode == null)
                {
                    _logger.LogInformation("🔍 Nó não encontrado por MachineId, buscando por nome da máquina: {MachineName}", Environment.MachineName);
                    localNode = await localNodeService.GetLocalNodeByMachineNameAsync(Environment.MachineName);
                }
                
                if (localNode == null)
                {
                    _logger.LogWarning("⚠️ Nenhum nó local encontrado nem por MachineId ({MachineId}) nem por nome ({MachineName})", 
                        machineId, Environment.MachineName);
                    return;
                }
                
                _logger.LogInformation("✅ Nó local encontrado: {NodeId} - {MachineName} (MachineId: {MachineId})", 
                    localNode.Id, localNode.MachineName, localNode.MachineId);

                // Obter token armazenado (pode ser null para modo anônimo)
                var authToken = await tokenStorageService.GetStoredTokenAsync();
                if (string.IsNullOrEmpty(authToken))
                {
                    _logger.LogInformation("ℹ️ Iniciando streaming em modo anônimo (sem token de autenticação)");
                }
                else
                {
                    _logger.LogInformation("🔑 Iniciando streaming com token de autenticação");
                }

                // Gerar connectionId único
                var connectionId = Guid.NewGuid().ToString();

                _logger.LogInformation("🚀 Iniciando streaming no startup: MachineId={MachineId}, ConnectionId={ConnectionId}, NodeId={NodeId}", 
                    machineId, connectionId, localNode.Id);

                // Iniciar streaming
                await commandStreamService.StartStreamingAsync(
                    connectionId: connectionId,
                    machineId: machineId,
                    authToken: authToken,
                    nodeId: localNode.Id,
                    name: localNode.MachineName,
                    machineName: localNode.MachineName,
                    version: systemInfoService.GetSystemVersion(),
                    operatingSystem: systemInfoService.GetOperatingSystem()
                );

                _logger.LogInformation("✅ Streaming iniciado com sucesso no startup da aplicação");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao iniciar streaming no startup: {Error}", ex.Message);
                throw;
            }
        }
    }
}
