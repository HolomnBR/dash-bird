using Microsoft.AspNetCore.Mvc;
using FirebirdApi.Services;

namespace FirebirdApi.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly IGrpcClientService _grpcClientService;
    private readonly ILogger<HealthController> _logger;
    private readonly IConfiguration _configuration;

    public HealthController(IGrpcClientService grpcClientService, ILogger<HealthController> logger, IConfiguration configuration)
    {
        _grpcClientService = grpcClientService;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }

    [HttpGet("grpc")]
    public async Task<IActionResult> TestGrpcConnection()
    {
        try
        {
            var grpcServerUrl = _configuration["GrpcServer:Url"] ?? "http://localhost:7001";
            _logger.LogInformation("Testando conexão com servidor gRPC em: {GrpcServerUrl}", grpcServerUrl);
            
            // Teste direto de conectividade gRPC
            var isConnected = await _grpcClientService.TestConnectionAsync();
            
            return Ok(new
            {
                grpc_connection = isConnected ? "connected" : "failed",
                grpc_server_url = grpcServerUrl,
                timestamp = DateTime.UtcNow,
                message = isConnected ? "Conexão gRPC estabelecida com sucesso" : "Falha na conexão gRPC"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao testar conexão gRPC");
            
            return StatusCode(500, new
            {
                grpc_connection = "error",
                timestamp = DateTime.UtcNow,
                message = $"Erro na conexão gRPC: {ex.Message}",
                error = ex.ToString()
            });
        }
    }
}
