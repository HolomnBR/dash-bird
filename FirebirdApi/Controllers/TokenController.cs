using Microsoft.AspNetCore.Mvc;
using FirebirdApi.Services;
using FirebirdApi.Models;

namespace FirebirdApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TokenController : ControllerBase
    {
        private readonly ITokenStorageService _tokenStorageService;
        private readonly ILogger<TokenController> _logger;

        public TokenController(ITokenStorageService tokenStorageService, ILogger<TokenController> logger)
        {
            _tokenStorageService = tokenStorageService;
            _logger = logger;
        }

        /// <summary>
        /// Obter token armazenado
        /// </summary>
        [HttpGet("get-token")]
        public async Task<IActionResult> GetToken()
        {
            try
            {
                var token = await _tokenStorageService.GetStoredTokenAsync();
                
                if (string.IsNullOrEmpty(token))
                {
                    return Ok(new { token = (string?)null });
                }

                return Ok(new { token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter token");
                return StatusCode(500, new { error = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Salvar token
        /// </summary>
        [HttpPost("save-token")]
        public async Task<IActionResult> SaveToken([FromBody] SaveTokenRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Token))
                {
                    return BadRequest(new { error = "Token é obrigatório" });
                }

                await _tokenStorageService.StoreTokenAsync(request.Token);
                
                _logger.LogInformation("Token salvo com sucesso para usuário {UserId}", request.UserId);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar token");
                return StatusCode(500, new { error = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Limpar token
        /// </summary>
        [HttpPost("clear-token")]
        public async Task<IActionResult> ClearToken()
        {
            try
            {
                await _tokenStorageService.ClearTokenAsync();
                
                _logger.LogInformation("Token removido com sucesso");
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao limpar token");
                return StatusCode(500, new { error = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Verificar se há token válido
        /// </summary>
        [HttpGet("has-valid-token")]
        public async Task<IActionResult> HasValidToken()
        {
            try
            {
                var hasToken = await _tokenStorageService.HasValidTokenAsync();
                return Ok(new { hasToken });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar token");
                return StatusCode(500, new { error = "Erro interno do servidor" });
            }
        }
    }

    public class SaveTokenRequest
    {
        public string Token { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string? UserEmail { get; set; }
    }
}
