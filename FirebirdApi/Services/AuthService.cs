using FirebirdApi.Models;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;

namespace FirebirdApi.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IMachineIdService _machineIdService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITokenStorageService _tokenStorageService;
        private readonly string _cloudServerUrl;

        public AuthService(
            IConfiguration configuration, 
            ILogger<AuthService> logger, 
            IHttpClientFactory httpClientFactory,
            IMachineIdService machineIdService,
            IHttpContextAccessor httpContextAccessor,
            ITokenStorageService tokenStorageService)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("CloudServer");
            _machineIdService = machineIdService;
            _httpContextAccessor = httpContextAccessor;
            _tokenStorageService = tokenStorageService;
            _cloudServerUrl = _configuration["CloudServer:Url"] ?? "https://localhost:7001";
        }

        public async Task<LocalAuthResponse> RegisterAsync(LocalUserRegisterRequest request)
        {
            try
            {
                _logger.LogInformation("Registrando usuário local: {Email}", request.Email);

                // Registrar no servidor cloud
                var cloudResponse = await _httpClient.PostAsJsonAsync(
                    $"{_cloudServerUrl}/api/User/register", 
                    new
                    {
                        Name = request.Name,
                        Email = request.Email,
                        Password = request.Password
                    }
                );

                if (!cloudResponse.IsSuccessStatusCode)
                {
                    var errorContent = await cloudResponse.Content.ReadAsStringAsync();
                    _logger.LogWarning("Falha ao registrar usuário no servidor cloud: {StatusCode} - {Error}", 
                        cloudResponse.StatusCode, errorContent);
                    throw new InvalidOperationException($"Erro ao registrar no servidor cloud: {cloudResponse.StatusCode}");
                }

                var cloudResult = await cloudResponse.Content.ReadFromJsonAsync<CloudAuthResponse>();
                if (cloudResult == null)
                {
                    throw new InvalidOperationException("Resposta inválida do servidor cloud");
                }

                _logger.LogInformation("Usuário registrado com sucesso no servidor cloud: {Email}", request.Email);

                // Retornar resposta local baseada na resposta do cloud
                return new LocalAuthResponse
                {
                    Token = cloudResult.Token,
                    User = new LocalUserResponse
                    {
                        Id = cloudResult.User.Id,
                        Name = cloudResult.User.Name,
                        Email = cloudResult.User.Email,
                        CreatedAt = cloudResult.User.CreatedAt,
                        LastLoginAt = cloudResult.User.LastLoginAt,
                        IsActive = cloudResult.User.IsActive
                    },
                    ExpiresAt = cloudResult.ExpiresAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar usuário: {Email}", request.Email);
                throw;
            }
        }

        public async Task<LocalAuthResponse> LoginAsync(LocalUserLoginRequest request)
        {
            try
            {
                _logger.LogInformation("Fazendo login do usuário: {Email}", request.Email);

                // Fazer login no servidor cloud
                var cloudResponse = await _httpClient.PostAsJsonAsync(
                    $"{_cloudServerUrl}/api/User/login", 
                    new
                    {
                        Email = request.Email,
                        Password = request.Password
                    }
                );

                if (!cloudResponse.IsSuccessStatusCode)
                {
                    var errorContent = await cloudResponse.Content.ReadAsStringAsync();
                    _logger.LogWarning("Falha ao fazer login no servidor cloud: {StatusCode} - {Error}", 
                        cloudResponse.StatusCode, errorContent);
                    throw new UnauthorizedAccessException("Email ou senha inválidos");
                }

                var cloudResult = await cloudResponse.Content.ReadFromJsonAsync<CloudAuthResponse>();
                if (cloudResult == null)
                {
                    throw new UnauthorizedAccessException("Resposta inválida do servidor cloud");
                }

                _logger.LogInformation("Usuário logado com sucesso: {Email}", request.Email);

                // Retornar resposta local baseada na resposta do cloud
                return new LocalAuthResponse
                {
                    Token = cloudResult.Token,
                    User = new LocalUserResponse
                    {
                        Id = cloudResult.User.Id,
                        Name = cloudResult.User.Name,
                        Email = cloudResult.User.Email,
                        CreatedAt = cloudResult.User.CreatedAt,
                        LastLoginAt = cloudResult.User.LastLoginAt,
                        IsActive = cloudResult.User.IsActive
                    },
                    ExpiresAt = cloudResult.ExpiresAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fazer login: {Email}", request.Email);
                throw;
            }
        }

        public async Task<LocalUserResponse?> GetUserByIdAsync(string userId)
        {
            try
            {
                // Buscar no servidor cloud
                var response = await _httpClient.GetAsync($"{_cloudServerUrl}/api/User/profile");
                
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var cloudUser = await response.Content.ReadFromJsonAsync<CloudUserResponse>();
                if (cloudUser == null || cloudUser.Id != userId)
                {
                    return null;
                }

                return new LocalUserResponse
                {
                    Id = cloudUser.Id,
                    Name = cloudUser.Name,
                    Email = cloudUser.Email,
                    CreatedAt = cloudUser.CreatedAt,
                    LastLoginAt = cloudUser.LastLoginAt,
                    IsActive = cloudUser.IsActive
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar usuário por ID: {UserId}", userId);
                return null;
            }
        }

        public async Task<LocalUserResponse?> GetUserByEmailAsync(string email)
        {
            try
            {
                // Buscar no servidor cloud
                var response = await _httpClient.GetAsync($"{_cloudServerUrl}/api/User/profile");
                
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var cloudUser = await response.Content.ReadFromJsonAsync<CloudUserResponse>();
                if (cloudUser == null || cloudUser.Email != email)
                {
                    return null;
                }

                return new LocalUserResponse
                {
                    Id = cloudUser.Id,
                    Name = cloudUser.Name,
                    Email = cloudUser.Email,
                    CreatedAt = cloudUser.CreatedAt,
                    LastLoginAt = cloudUser.LastLoginAt,
                    IsActive = cloudUser.IsActive
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar usuário por email: {Email}", email);
                return null;
            }
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                // Validar no servidor cloud
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_cloudServerUrl}/api/User/validate-token");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar token");
                return false;
            }
        }

        public Task<string?> GetUserIdFromTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);

                var userIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "userId");
                return Task.FromResult(userIdClaim?.Value);
            }
            catch
            {
                return Task.FromResult<string?>(null);
            }
        }

        public async Task<AnonymousNodeResponse> RegisterAnonymousNodeAsync(AnonymousNodeRegisterRequest request)
        {
            try
            {
                _logger.LogInformation("Registrando nó anônimo via HTTP: {MachineId}", request.MachineId);

                // Usar o endpoint HTTP correto para registro de nó desktop
                var desktopNodeRequest = new
                {
                    name = request.Name, // Alias do nó
                    machineName = Environment.MachineName, // Nome da máquina
                    machineId = request.MachineId,
                    ipAddress = request.IpAddress,
                    port = request.Port,
                    databasePath = request.DatabasePath ?? string.Empty,
                    version = request.Version ?? "1.0.0",
                    operatingSystem = request.OperatingSystem ?? string.Empty
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"{_cloudServerUrl}/api/DesktopNode/register", 
                    desktopNodeRequest
                );

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Falha ao registrar nó anônimo no servidor cloud: {StatusCode} - {Error}", 
                        response.StatusCode, errorContent);
                    throw new InvalidOperationException($"Erro ao registrar nó anônimo: {response.StatusCode}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Resposta do servidor: {Response}", responseContent);

                // Para registro anônimo, criar resposta baseada no que foi enviado
                var result = new AnonymousNodeResponse
                {
                    Id = Guid.NewGuid().ToString(), // Gerar ID temporário
                    AnonymousToken = Guid.NewGuid().ToString(), // Gerar token temporário
                    MachineId = request.MachineId,
                    Name = request.Name, // Alias do nó
                    IpAddress = request.IpAddress,
                    Port = request.Port,
                    DatabasePath = request.DatabasePath,
                    Version = request.Version,
                    OperatingSystem = request.OperatingSystem,
                    IsAnonymous = true,
                    AnonymousExpiresAt = DateTime.UtcNow.AddDays(30), // Token anônimo válido por 30 dias
                    CreatedAt = DateTime.UtcNow,
                    LastSeen = DateTime.UtcNow
                };

                _logger.LogInformation("Nó anônimo registrado com sucesso via HTTP: {MachineId} -> {NodeId}", request.MachineId, result.Id);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar nó anônimo via HTTP: {MachineId}", request.MachineId);
                throw;
            }
        }

        public async Task<AnonymousNodeResponse?> GetAnonymousNodeByTokenAsync(string anonymousToken)
        {
            try
            {
                _logger.LogInformation("Buscando informações do nó anônimo por token via gRPC: {TokenLength} caracteres", anonymousToken?.Length ?? 0);

                // Para buscar informações de nó anônimo por token, vamos usar HTTP por enquanto
                // pois o serviço gRPC não tem um método específico para isso
                var response = await _httpClient.GetAsync($"{_cloudServerUrl}/api/AnonymousNode/info/{anonymousToken}");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Nó anônimo não encontrado ou token inválido: {StatusCode}", response.StatusCode);
                    return null;
                }

                var result = await response.Content.ReadFromJsonAsync<AnonymousNodeResponse>();
                _logger.LogInformation("Informações do nó anônimo obtidas com sucesso: {NodeId}", result?.Id ?? "N/A");
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar nó anônimo por token: {Token}", anonymousToken);
                return null;
            }
        }

        public async Task<BindNodeToUserResponse> BindNodeToUserAsync(string anonymousToken, string userId)
        {
            try
            {
                _logger.LogInformation("Vinculando nó anônimo ao usuário: {Token} -> {UserId}", anonymousToken, userId);

                var token = await GetTokenFromContextAsync();
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogError("Token não encontrado para vincular nó");
                    return new BindNodeToUserResponse
                    {
                        Success = false,
                        Message = "Token de autenticação não encontrado"
                    };
                }

                var request = new HttpRequestMessage(HttpMethod.Post, $"{_cloudServerUrl}/api/User/bind-node");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                request.Content = JsonContent.Create(new { AnonymousToken = anonymousToken });

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Falha ao vincular nó ao usuário: {StatusCode} - {Error}", 
                        response.StatusCode, errorContent);
                    return new BindNodeToUserResponse
                    {
                        Success = false,
                        Message = $"Erro ao vincular nó: {response.StatusCode}"
                    };
                }

                var result = await response.Content.ReadFromJsonAsync<BindNodeToUserResponse>();
                if (result == null)
                {
                    return new BindNodeToUserResponse
                    {
                        Success = false,
                        Message = "Resposta inválida do servidor cloud"
                    };
                }

                _logger.LogInformation("Nó vinculado ao usuário com sucesso: {NodeId} -> {UserId}", result.NodeId, result.UserId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao vincular nó ao usuário: {Token} -> {UserId}", anonymousToken, userId);
                return new BindNodeToUserResponse
                {
                    Success = false,
                    Message = "Erro interno ao vincular nó ao usuário"
                };
            }
        }

        public async Task<BindNodeToUserResponse> BindCurrentNodeToUserAsync(string machineId, string userId)
        {
            try
            {
                _logger.LogInformation("Vinculando nó atual ao usuário: {MachineId} -> {UserId}", machineId, userId);

                var token = await GetTokenFromContextAsync();
                _logger.LogInformation("Token obtido: {TokenLength} caracteres", token?.Length ?? 0);

                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogError("Token não encontrado no contexto HTTP nem no armazenamento local");
                    return new BindNodeToUserResponse
                    {
                        Success = false,
                        Message = "Token de autenticação não encontrado"
                    };
                }

                var request = new HttpRequestMessage(HttpMethod.Post, $"{_cloudServerUrl}/api/User/bind-current-node");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                request.Content = JsonContent.Create(new { MachineId = machineId });

                _logger.LogInformation("Enviando requisição para: {Url}", $"{_cloudServerUrl}/api/User/bind-current-node");
                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Falha ao vincular nó atual ao usuário: {StatusCode} - {Error}", 
                        response.StatusCode, errorContent);
                    return new BindNodeToUserResponse
                    {
                        Success = false,
                        Message = $"Erro ao vincular nó: {response.StatusCode}"
                    };
                }

                var result = await response.Content.ReadFromJsonAsync<BindNodeToUserResponse>();
                if (result == null)
                {
                    return new BindNodeToUserResponse
                    {
                        Success = false,
                        Message = "Resposta inválida do servidor cloud"
                    };
                }

                _logger.LogInformation("Nó atual vinculado ao usuário com sucesso: {NodeId} -> {UserId}", result.NodeId, result.UserId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao vincular nó atual ao usuário: {MachineId} -> {UserId}", machineId, userId);
                return new BindNodeToUserResponse
                {
                    Success = false,
                    Message = "Erro interno ao vincular nó atual ao usuário"
                };
            }
        }

        public async Task<List<object>> GetUserNodesAsync(string userId)
        {
            try
            {
                _logger.LogInformation("Obtendo nós do usuário: {UserId}", userId);

                var token = await GetTokenFromContextAsync();
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogError("Token não encontrado no contexto HTTP nem no armazenamento local");
                    return new List<object>();
                }

                var request = new HttpRequestMessage(HttpMethod.Get, $"{_cloudServerUrl}/api/User/nodes");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Falha ao obter nós do usuário: {StatusCode} - {Error}", 
                        response.StatusCode, errorContent);
                    return new List<object>();
                }

                var result = await response.Content.ReadFromJsonAsync<List<object>>();
                return result ?? new List<object>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter nós do usuário: {UserId}", userId);
                return new List<object>();
            }
        }

        public async Task<UnbindNodeFromUserResponse> UnbindNodeFromUserAsync(string nodeId, string userId)
        {
            try
            {
                _logger.LogInformation("Desvinculando nó do usuário: {NodeId} <- {UserId}", nodeId, userId);

                var token = await GetTokenFromContextAsync();
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogError("Token não encontrado no contexto HTTP nem no armazenamento local");
                    return new UnbindNodeFromUserResponse
                    {
                        Success = false,
                        Message = "Token de autenticação não encontrado"
                    };
                }

                var request = new HttpRequestMessage(HttpMethod.Post, $"{_cloudServerUrl}/api/User/unbind-node");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                request.Content = JsonContent.Create(new { NodeId = nodeId });

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Falha ao desvincular nó do usuário: {StatusCode} - {Error}", 
                        response.StatusCode, errorContent);
                    
                    return new UnbindNodeFromUserResponse
                    {
                        Success = false,
                        Message = $"Erro ao desvincular nó: {response.StatusCode}"
                    };
                }

                var result = await response.Content.ReadFromJsonAsync<UnbindNodeFromUserResponse>();
                if (result == null)
                {
                    _logger.LogError("Resposta inválida do servidor cloud ao desvincular nó");
                    return new UnbindNodeFromUserResponse
                    {
                        Success = false,
                        Message = "Resposta inválida do servidor cloud"
                    };
                }

                _logger.LogInformation("Nó desvinculado do usuário com sucesso: {NodeId} <- {UserId}", result.NodeId, result.UserId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao desvincular nó do usuário: {NodeId} <- {UserId}", nodeId, userId);
                return new UnbindNodeFromUserResponse
                {
                    Success = false,
                    Message = "Erro interno ao desvincular nó do usuário"
                };
            }
        }

        public async Task<bool> ValidateAnonymousTokenAsync(string anonymousToken)
        {
            try
            {
                _logger.LogInformation("Validando token anônimo: {TokenLength} caracteres", anonymousToken?.Length ?? 0);
                
                // Usar HTTP para validação de token anônimo por enquanto
                // pois o serviço gRPC não tem um método específico para isso
                var response = await _httpClient.PostAsync($"{_cloudServerUrl}/api/AnonymousNode/validate/{anonymousToken}", null);
                
                var isValid = response.IsSuccessStatusCode;
                _logger.LogInformation("Validação de token anônimo: {IsValid}", isValid ? "VÁLIDO" : "INVÁLIDO");
                
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar token anônimo: {Token}", anonymousToken);
                return false;
            }
        }

        public async Task<object> CheckNodeConnectionAsync(string token, string machineId)
        {
            try
            {
                _logger.LogInformation("Verificando conexão do nó: {MachineId} no servidor cloud: {CloudUrl}", machineId, _cloudServerUrl);

                var request = new HttpRequestMessage(HttpMethod.Get, $"{_cloudServerUrl}/api/User/check-node-connection/{machineId}");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Falha ao verificar conexão do nó: {StatusCode} - {Error}", 
                        response.StatusCode, errorContent);
                    
                    return new
                    {
                        success = true,
                        isConnected = false,
                        wasUnbound = false,
                        node = (object?)null,
                        error = $"Servidor cloud retornou: {response.StatusCode} - {errorContent}"
                    };
                }

                var result = await response.Content.ReadFromJsonAsync<object>();
                _logger.LogInformation("Resposta do servidor cloud para nó {MachineId}: {Result}", machineId, result);
                
                return result ?? new
                {
                    success = true,
                    isConnected = false,
                    wasUnbound = false,
                    node = (object?)null,
                    error = "Resposta vazia do servidor cloud"
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning("Erro de conexão HTTP ao verificar nó {MachineId}: {Error}", machineId, ex.Message);
                return new
                {
                    success = true,
                    isConnected = false,
                    wasUnbound = false,
                    node = (object?)null,
                    error = $"Servidor cloud não disponível: {ex.Message}"
                };
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning("Timeout ao verificar conexão do nó {MachineId}: {Error}", machineId, ex.Message);
                return new
                {
                    success = true,
                    isConnected = false,
                    wasUnbound = false,
                    node = (object?)null,
                    error = "Timeout ao conectar com servidor cloud"
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Erro inesperado ao verificar conexão do nó {MachineId}: {Error}", machineId, ex.Message);
                return new
                {
                    success = true,
                    isConnected = false,
                    wasUnbound = false,
                    node = (object?)null,
                    error = $"Erro de conexão com servidor cloud: {ex.Message}"
                };
            }
        }

        public async Task<List<object>> GetUserTokensAsync(string userId)
        {
            try
            {
                _logger.LogInformation("Obtendo tokens do usuário: {UserId}", userId);

                var token = await GetTokenFromContextAsync();
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogError("Token não encontrado no contexto HTTP nem no armazenamento local");
                    return new List<object>();
                }

                var request = new HttpRequestMessage(HttpMethod.Get, $"{_cloudServerUrl}/api/User/tokens");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Falha ao obter tokens do usuário: {StatusCode} - {Error}", 
                        response.StatusCode, errorContent);
                    return new List<object>();
                }

                var result = await response.Content.ReadFromJsonAsync<List<object>>();
                return result ?? new List<object>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Erro ao obter tokens do usuário: {UserId} - {Error}", userId, ex.Message);
                return new List<object>();
            }
        }

        /// <summary>
        /// Obtém o token armazenado localmente para uso em operações internas
        /// </summary>
        public async Task<string?> GetStoredTokenAsync()
        {
            return await _tokenStorageService.GetStoredTokenAsync();
        }

        /// <summary>
        /// Verifica se há um token válido armazenado localmente
        /// </summary>
        public async Task<bool> HasValidStoredTokenAsync()
        {
            return await _tokenStorageService.HasValidTokenAsync();
        }

        private async Task<string?> GetTokenFromContextAsync()
        {
            try
            {
                // Primeiro tentar obter do contexto HTTP atual
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null)
                {
                    var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
                    if (authHeader != null && authHeader.StartsWith("Bearer "))
                    {
                        var token = authHeader.Substring("Bearer ".Length).Trim();
                        _logger.LogDebug("Token obtido do header HTTP");
                        return token;
                    }
                }

                // Se não encontrou no header, tentar obter do armazenamento local
                var storedToken = await _tokenStorageService.GetStoredTokenAsync();
                if (!string.IsNullOrEmpty(storedToken))
                {
                    _logger.LogDebug("Token obtido do armazenamento local");
                    return storedToken;
                }

                _logger.LogWarning("Token não encontrado nem no header nem no armazenamento local");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter token do contexto");
                return null;
            }
        }

        private string? GetTokenFromContext()
        {
            try
            {
                // Obter o token do contexto HTTP atual
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null)
                {
                    _logger.LogWarning("HttpContext não disponível para obter token");
                    return null;
                }

                var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader != null && authHeader.StartsWith("Bearer "))
                {
                    return authHeader.Substring("Bearer ".Length).Trim();
                }

                _logger.LogWarning("Token de autorização não encontrado no header");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter token do contexto");
                return null;
            }
        }


    }

    // DTOs para comunicação com o servidor cloud
    public class CloudAuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public CloudUserResponse User { get; set; } = new CloudUserResponse();
        public DateTime ExpiresAt { get; set; }
    }

    public class CloudUserResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; }
    }
}
