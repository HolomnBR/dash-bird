using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FirebirdApi.Services;

namespace FirebirdApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StreamingController : ControllerBase
    {
        private readonly ICommandStreamService _commandStreamService;
        private readonly ILogger<StreamingController> _logger;

        public StreamingController(ICommandStreamService commandStreamService, ILogger<StreamingController> logger)
        {
            _commandStreamService = commandStreamService;
            _logger = logger;
        }

        /// <summary>
        /// Obtém o status da conexão de streaming
        /// </summary>
        [HttpGet("status")]
        public ActionResult<object> GetStreamingStatus()
        {
            return Ok(new
            {
                isConnected = _commandStreamService.IsConnected,
                connectionId = _commandStreamService.CurrentConnectionId,
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Inicia a conexão de streaming
        /// </summary>
        [HttpPost("start")]
        public async Task<ActionResult> StartStreaming([FromBody] StartStreamingRequest request)
        {
            try
            {
                if (_commandStreamService.IsConnected)
                {
                    return BadRequest("Streaming já está ativo");
                }

                var connectionId = request.ConnectionId ?? Guid.NewGuid().ToString();
                var machineId = request.MachineId ?? Environment.MachineName;
                
                await _commandStreamService.StartStreamingAsync(connectionId, machineId, request.AuthToken, request.NodeId);
                
                _logger.LogInformation("🚀 Streaming iniciado via API: ConnectionId={ConnectionId}, MachineId={MachineId}, AuthToken={HasToken}", 
                    connectionId, machineId, !string.IsNullOrEmpty(request.AuthToken));
                
                return Ok(new
                {
                    message = "Streaming iniciado com sucesso",
                    connectionId = connectionId,
                    machineId = machineId,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao iniciar streaming");
                return StatusCode(500, "Erro interno do servidor");
            }
        }

        /// <summary>
        /// Para a conexão de streaming
        /// </summary>
        [HttpPost("stop")]
        public async Task<ActionResult> StopStreaming()
        {
            try
            {
                if (!_commandStreamService.IsConnected)
                {
                    return BadRequest("Streaming não está ativo");
                }

                await _commandStreamService.StopStreamingAsync();
                
                return Ok(new
                {
                    message = "Streaming parado com sucesso",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao parar streaming");
                return StatusCode(500, "Erro interno do servidor");
            }
        }

        /// <summary>
        /// Reinicia a conexão de streaming
        /// </summary>
        [HttpPost("restart")]
        public async Task<ActionResult> RestartStreaming([FromBody] StartStreamingRequest request)
        {
            try
            {
                if (_commandStreamService.IsConnected)
                {
                    await _commandStreamService.StopStreamingAsync();
                    await Task.Delay(1000); // Aguardar um pouco antes de reconectar
                }

                var connectionId = request.ConnectionId ?? Guid.NewGuid().ToString();
                var machineId = request.MachineId ?? Environment.MachineName;
                
                await _commandStreamService.StartStreamingAsync(connectionId, machineId, request.AuthToken, request.NodeId);
                
                return Ok(new
                {
                    message = "Streaming reiniciado com sucesso",
                    connectionId = connectionId,
                    machineId = machineId,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao reiniciar streaming");
                return StatusCode(500, "Erro interno do servidor");
            }
        }
    }

    public class StartStreamingRequest
    {
        public string? ConnectionId { get; set; }
        public string? MachineId { get; set; }
        public string? AuthToken { get; set; }
        public string? NodeId { get; set; }
    }
}
