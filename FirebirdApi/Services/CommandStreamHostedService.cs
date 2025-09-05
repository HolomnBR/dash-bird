using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FirebirdApi.Services
{
    public class CommandStreamHostedService : IHostedService, IDisposable
    {
        private readonly ICommandStreamService _commandStreamService;
        private readonly ILogger<CommandStreamHostedService> _logger;

        public CommandStreamHostedService(ICommandStreamService commandStreamService, ILogger<CommandStreamHostedService> logger)
        {
            _commandStreamService = commandStreamService;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("CommandStreamHostedService iniciado");
            
            // O CommandStreamService não precisa ser iniciado automaticamente
            // Ele será iniciado via endpoint quando necessário
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("CommandStreamHostedService parando");
            
            if (_commandStreamService != null)
            {
                await _commandStreamService.StopStreamingAsync();
            }
        }

        public void Dispose()
        {
            _logger.LogInformation("CommandStreamHostedService disposed");
        }
    }
}
