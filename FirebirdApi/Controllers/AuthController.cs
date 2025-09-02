using Microsoft.AspNetCore.Mvc;
using FirebirdApi.Models;
using FirebirdApi.Services;

namespace FirebirdApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Registra um novo usuário no sistema
        /// </summary>
        /// <param name="request">Dados do usuário para registro</param>
        /// <returns>Token de autenticação e dados do usuário</returns>
        [HttpPost("register")]
        [ProducesResponseType(typeof(LocalAuthResponse), 200)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        public async Task<IActionResult> Register([FromBody] LocalUserRegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.RegisterAsync(request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Tentativa de registro com email já existente: {Email}", request.Email);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro interno no registro de usuário");
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Autentica um usuário no sistema
        /// </summary>
        /// <param name="request">Credenciais de login</param>
        /// <returns>Token de autenticação e dados do usuário</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LocalAuthResponse), 200)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        public async Task<IActionResult> Login([FromBody] LocalUserLoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.LoginAsync(request);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Tentativa de login inválida: {Email}", request.Email);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro interno no login de usuário");
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Obtém informações do usuário autenticado
        /// </summary>
        /// <returns>Dados do usuário</returns>
        [HttpGet("profile")]
        [ProducesResponseType(typeof(LocalUserResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        [ProducesResponseType(typeof(ProblemDetails), 404)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var token = GetTokenFromHeader();
                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized(new { message = "Token de autenticação não fornecido" });
                }

                var userId = await _authService.GetUserIdFromTokenAsync(token);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var user = await _authService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "Usuário não encontrado" });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro interno ao obter perfil do usuário");
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Valida um token de autenticação
        /// </summary>
        /// <returns>Status da validação</returns>
        [HttpPost("validate-token")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        public async Task<IActionResult> ValidateToken()
        {
            try
            {
                var token = GetTokenFromHeader();
                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized(new { message = "Token de autenticação não fornecido", valid = false });
                }

                var isValid = await _authService.ValidateTokenAsync(token);
                if (!isValid)
                {
                    return Unauthorized(new { message = "Token inválido", valid = false });
                }

                return Ok(new { message = "Token válido", valid = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro interno na validação do token");
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Registra um novo nó anônimo no sistema
        /// </summary>
        /// <param name="request">Dados do nó para registro anônimo</param>
        /// <returns>Dados do nó anônimo criado com token de acesso</returns>
        [HttpPost("anonymous/register")]
        [ProducesResponseType(typeof(AnonymousNodeResponse), 200)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        public async Task<IActionResult> RegisterAnonymousNode([FromBody] AnonymousNodeRegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.RegisterAnonymousNodeAsync(request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Tentativa de registro de nó com MachineId já existente: {MachineId}", request.MachineId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro interno no registro de nó anônimo");
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Obtém informações de um nó anônimo pelo token
        /// </summary>
        /// <param name="token">Token anônimo do nó</param>
        /// <returns>Dados do nó anônimo</returns>
        [HttpGet("anonymous/info/{token}")]
        [ProducesResponseType(typeof(AnonymousNodeResponse), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 404)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        public async Task<IActionResult> GetAnonymousNodeInfo(string token)
        {
            try
            {
                var node = await _authService.GetAnonymousNodeByTokenAsync(token);
                if (node == null)
                {
                    return NotFound(new { message = "Nó anônimo não encontrado ou token expirado" });
                }

                return Ok(node);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro interno ao obter informações do nó anônimo");
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Valida um token anônimo
        /// </summary>
        /// <param name="token">Token anônimo para validação</param>
        /// <returns>Status da validação</returns>
        [HttpPost("anonymous/validate/{token}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        public async Task<IActionResult> ValidateAnonymousToken(string token)
        {
            try
            {
                var isValid = await _authService.ValidateAnonymousTokenAsync(token);
                return Ok(new { valid = isValid, message = isValid ? "Token válido" : "Token inválido ou expirado" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro interno na validação do token anônimo");
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Vincula um nó anônimo ao usuário autenticado
        /// </summary>
        /// <param name="request">Token anônimo do nó para vincular</param>
        /// <returns>Resultado da vinculação</returns>
        [HttpPost("bind-node")]
        [ProducesResponseType(typeof(BindNodeToUserResponse), 200)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        public async Task<IActionResult> BindNodeToUser([FromBody] BindNodeToUserRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var token = GetTokenFromHeader();
                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized(new { message = "Token de autenticação não fornecido" });
                }

                var userId = await _authService.GetUserIdFromTokenAsync(token);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var result = await _authService.BindNodeToUserAsync(request.AnonymousToken, userId);
                
                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro interno ao vincular nó ao usuário");
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Vincula o nó atual (por machineId) ao usuário autenticado
        /// </summary>
        /// <param name="request">MachineId do nó para vincular</param>
        /// <returns>Resultado da vinculação</returns>
        [HttpPost("bind-current-node")]
        [ProducesResponseType(typeof(BindNodeToUserResponse), 200)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        public async Task<IActionResult> BindCurrentNode([FromBody] BindCurrentNodeRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var token = GetTokenFromHeader();
                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized(new { message = "Token de autenticação não fornecido" });
                }

                var userId = await _authService.GetUserIdFromTokenAsync(token);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var result = await _authService.BindCurrentNodeToUserAsync(request.MachineId, userId);
                
                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro interno ao vincular nó atual ao usuário");
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Lista todos os nós conectados ao usuário autenticado
        /// </summary>
        /// <returns>Lista de nós do usuário</returns>
        [HttpGet("nodes")]
        [ProducesResponseType(typeof(List<object>), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        public async Task<IActionResult> GetUserNodes()
        {
            try
            {
                var token = GetTokenFromHeader();
                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized(new { message = "Token de autenticação não fornecido" });
                }

                var userId = await _authService.GetUserIdFromTokenAsync(token);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var result = await _authService.GetUserNodesAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro interno ao obter nós do usuário");
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Verifica se um nó específico está conectado à conta do usuário
        /// </summary>
        /// <param name="machineId">ID da máquina para verificar</param>
        /// <returns>Status de conexão do nó e lista de tokens do usuário</returns>
        [HttpGet("check-node-connection/{machineId}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        public async Task<IActionResult> CheckNodeConnection(string machineId)
        {
            try
            {
                var token = GetTokenFromHeader();
                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized(new { message = "Token de autenticação não fornecido" });
                }

                var userId = await _authService.GetUserIdFromTokenAsync(token);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                // Obter lista de tokens do usuário
                var userTokens = await _authService.GetUserTokensAsync(userId);

                // Verificar conexão do nó (tentar com servidor cloud, mas não falhar se não disponível)
                object connectionResult;
                try
                {
                    connectionResult = await _authService.CheckNodeConnectionAsync(token, machineId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Servidor cloud não disponível, usando verificação local: {Error}", ex.Message);
                    connectionResult = new
                    {
                        success = true,
                        isConnected = false,
                        wasUnbound = false,
                        node = (object?)null,
                        error = "Servidor cloud não disponível"
                    };
                }

                var result = new
                {
                    success = true,
                    isConnected = false,
                    wasUnbound = false,
                    node = (object?)null,
                    userTokens = userTokens,
                    connectionResult = connectionResult
                };
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar conexão do nó {MachineId}", machineId);
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Desvincula um nó do usuário autenticado
        /// </summary>
        /// <param name="request">ID do nó para desvincular</param>
        /// <returns>Resultado da desvinculação</returns>
        [HttpPost("unbind-node")]
        [ProducesResponseType(typeof(UnbindNodeFromUserResponse), 200)]
        [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        public async Task<IActionResult> UnbindNodeFromUser([FromBody] UnbindNodeFromUserRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var token = GetTokenFromHeader();
                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized(new { message = "Token de autenticação não fornecido" });
                }

                var userId = await _authService.GetUserIdFromTokenAsync(token);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                var result = await _authService.UnbindNodeFromUserAsync(request.NodeId, userId);
                
                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro interno ao desvincular nó do usuário");
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Faz logout do usuário
        /// </summary>
        /// <returns>Resultado do logout</returns>
        [HttpPost("logout")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(ProblemDetails), 401)]
        [ProducesResponseType(typeof(ProblemDetails), 500)]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var token = GetTokenFromHeader();
                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized(new { message = "Token de autenticação não fornecido" });
                }

                var userId = await _authService.GetUserIdFromTokenAsync(token);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                _logger.LogInformation("Logout realizado para usuário: {UserId}", userId);

                return Ok(new { 
                    message = "Logout realizado com sucesso"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro interno durante logout");
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        private string? GetTokenFromHeader()
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader != null && authHeader.StartsWith("Bearer "))
            {
                return authHeader.Substring("Bearer ".Length).Trim();
            }
            return null;
        }
    }
}
