using FirebirdApi.Models;

namespace FirebirdApi.Services
{
    public interface IAuthService
    {
        Task<LocalAuthResponse> RegisterAsync(LocalUserRegisterRequest request);
        Task<LocalAuthResponse> LoginAsync(LocalUserLoginRequest request);
        Task<LocalUserResponse?> GetUserByIdAsync(string userId);
        Task<LocalUserResponse?> GetUserByEmailAsync(string email);
        Task<bool> ValidateTokenAsync(string token);
        Task<string?> GetUserIdFromTokenAsync(string token);
        Task<AnonymousNodeResponse> RegisterAnonymousNodeAsync(AnonymousNodeRegisterRequest request);
        Task<AnonymousNodeResponse?> GetAnonymousNodeByTokenAsync(string anonymousToken);
        Task<BindNodeToUserResponse> BindNodeToUserAsync(string anonymousToken, string userId);
        Task<BindNodeToUserResponse> BindCurrentNodeToUserAsync(string machineId, string userId);
        Task<UnbindNodeFromUserResponse> UnbindNodeFromUserAsync(string nodeId, string userId);
        Task<List<object>> GetUserNodesAsync(string userId);
        Task<object> CheckNodeConnectionAsync(string token, string machineId);
        Task<List<object>> GetUserTokensAsync(string userId);
        Task<bool> ValidateAnonymousTokenAsync(string anonymousToken);
    }
}
