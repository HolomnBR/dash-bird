using System.Text.Json;
using FirebirdApi.Data;
using FirebirdApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FirebirdApi.Services
{
    public class TokenStorageService : ITokenStorageService
    {
        private readonly ILogger<TokenStorageService> _logger;
        private readonly IConfiguration _configuration;
        private readonly LocalDbContext _context;
        private readonly string _tokenFilePath;
        private string? _cachedToken;

        public TokenStorageService(ILogger<TokenStorageService> logger, IConfiguration configuration, LocalDbContext context)
        {
            _logger = logger;
            _configuration = configuration;
            _context = context;
            
            // Usar diretório de dados da aplicação
            var dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DashBird");
            Directory.CreateDirectory(dataDir);
            _tokenFilePath = Path.Combine(dataDir, "auth_token.json");
        }

        public async Task<string?> GetStoredTokenAsync()
        {
            try
            {
                // Retornar token em cache se disponível
                if (!string.IsNullOrEmpty(_cachedToken))
                {
                    return _cachedToken;
                }

                // Primeiro tentar SQLite
                var sqliteToken = await _context.AuthTokens
                    .Where(t => t.IsActive && t.ExpiresAt > DateTime.UtcNow)
                    .OrderByDescending(t => t.StoredAt)
                    .FirstOrDefaultAsync();

                if (sqliteToken != null)
                {
                    _cachedToken = sqliteToken.Token;
                    _logger.LogInformation("Token carregado do SQLite");
                    return _cachedToken;
                }

                // Fallback para arquivo JSON (migração)
                if (File.Exists(_tokenFilePath))
                {
                    var json = await File.ReadAllTextAsync(_tokenFilePath);
                    var tokenData = JsonSerializer.Deserialize<TokenData>(json);
                    
                    if (tokenData != null && !string.IsNullOrEmpty(tokenData.Token))
                    {
                        // Verificar se o token não expirou
                        if (tokenData.ExpiresAt > DateTime.UtcNow)
                        {
                            _cachedToken = tokenData.Token;
                            _logger.LogInformation("Token carregado do arquivo JSON (migração)");
                            
                            // Migrar para SQLite
                            await StoreTokenAsync(tokenData.Token);
                            
                            return _cachedToken;
                        }
                        else
                        {
                            _logger.LogInformation("Token expirado, removendo do armazenamento");
                            await ClearTokenAsync();
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter token armazenado");
                return null;
            }
        }

        public async Task StoreTokenAsync(string token)
        {
            try
            {
                // Decodificar JWT para obter expiração e informações do usuário
                var expiresAt = GetTokenExpiration(token);
                var (userId, userEmail) = GetTokenUserInfo(token);

                // Desativar tokens anteriores
                await _context.AuthTokens
                    .Where(t => t.IsActive)
                    .ExecuteUpdateAsync(t => t.SetProperty(x => x.IsActive, false));

                // Criar novo token no SQLite
                var authToken = new AuthToken
                {
                    Token = token,
                    UserId = userId,
                    UserEmail = userEmail,
                    StoredAt = DateTime.UtcNow,
                    ExpiresAt = expiresAt,
                    IsActive = true
                };

                _context.AuthTokens.Add(authToken);
                await _context.SaveChangesAsync();
                
                _cachedToken = token;
                _logger.LogInformation("Token armazenado no SQLite até {ExpiresAt}", expiresAt);

                // Manter compatibilidade com arquivo JSON (backup)
                var tokenData = new TokenData
                {
                    Token = token,
                    StoredAt = DateTime.UtcNow,
                    ExpiresAt = expiresAt
                };

                var json = JsonSerializer.Serialize(tokenData, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_tokenFilePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao armazenar token");
            }
        }

        public async Task ClearTokenAsync()
        {
            try
            {
                _cachedToken = null;
                
                // Desativar tokens no SQLite
                await _context.AuthTokens
                    .Where(t => t.IsActive)
                    .ExecuteUpdateAsync(t => t.SetProperty(x => x.IsActive, false));

                await _context.SaveChangesAsync();
                
                // Remover arquivo JSON
                if (File.Exists(_tokenFilePath))
                {
                    File.Delete(_tokenFilePath);
                }
                
                _logger.LogInformation("Token removido do armazenamento local");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao limpar token");
            }
        }

        public async Task<bool> HasValidTokenAsync()
        {
            var token = await GetStoredTokenAsync();
            return !string.IsNullOrEmpty(token);
        }

        private DateTime GetTokenExpiration(string token)
        {
            try
            {
                var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                
                // JWT exp é em Unix timestamp
                var expClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "exp");
                if (expClaim != null && long.TryParse(expClaim.Value, out var exp))
                {
                    return DateTimeOffset.FromUnixTimeSeconds(exp).DateTime;
                }
                
                // Se não conseguir obter expiração, assumir 24 horas
                return DateTime.UtcNow.AddHours(24);
            }
            catch
            {
                // Se não conseguir decodificar, assumir 24 horas
                return DateTime.UtcNow.AddHours(24);
            }
        }

        private (string userId, string? userEmail) GetTokenUserInfo(string token)
        {
            try
            {
                var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                
                var userId = jwtToken.Claims.FirstOrDefault(x => x.Type == "sub" || x.Type == "id")?.Value ?? "unknown";
                var userEmail = jwtToken.Claims.FirstOrDefault(x => x.Type == "email")?.Value;
                
                return (userId, userEmail);
            }
            catch
            {
                return ("unknown", null);
            }
        }

        private class TokenData
        {
            public string Token { get; set; } = string.Empty;
            public DateTime StoredAt { get; set; }
            public DateTime ExpiresAt { get; set; }
        }
    }
}
