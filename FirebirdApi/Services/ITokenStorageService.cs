namespace FirebirdApi.Services
{
    public interface ITokenStorageService
    {
        Task<string?> GetStoredTokenAsync();
        Task StoreTokenAsync(string token);
        Task ClearTokenAsync();
        Task<bool> HasValidTokenAsync();
    }
}
