using FirebirdApi.Data;
using FirebirdApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FirebirdApi.Services
{
    public class TokenStorageService : ITokenStorageService
    {
        private readonly ILogger<TokenStorageService> _logger;
        private readonly LocalDbContext _context;
        private string? _cachedToken;

        public TokenStorageService(ILogger<TokenStorageService> logger, LocalDbContext context)
        {
            _logger = logger;
            _context = context;
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

                // Buscar token ativo no SQLite
                var token = await _context.AuthTokens
                    .Where(t => t.IsActive && t.ExpiresAt > DateTime.UtcNow)
                    .OrderByDescending(t => t.StoredAt)
                    .Select(t => t.Token)
                    .FirstOrDefaultAsync();

                if (token != null)
                {
                    _cachedToken = token;
                    _logger.LogInformation("Token carregado do SQLite");
                }

                return token;
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

                // Desativar tokens anteriores do mesmo usuário
                await _context.AuthTokens
                    .Where(t => t.UserId == userId && t.IsActive)
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
                _logger.LogInformation("Token armazenado no SQLite para usuário: {UserId}", userId);
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

    }
}
