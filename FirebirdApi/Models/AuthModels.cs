using System.ComponentModel.DataAnnotations;

namespace FirebirdApi.Models
{
    // DTOs para autenticação local
    public class LocalUserRegisterRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(255)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [MinLength(6)]
        [MaxLength(100)]
        public string Password { get; set; } = string.Empty;
    }
    
    public class LocalUserLoginRequest
    {
        [Required]
        [MaxLength(255)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string Password { get; set; } = string.Empty;
    }
    
    public class LocalUserResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; }
    }
    
    public class LocalAuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public LocalUserResponse User { get; set; } = new LocalUserResponse();
        public DateTime ExpiresAt { get; set; }
    }
    
    // DTOs para registro anônimo
    public class AnonymousNodeRegisterRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string MachineId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(45)]
        public string IpAddress { get; set; } = string.Empty;
        
        public int Port { get; set; } = 8000;
        
        [MaxLength(500)]
        public string? DatabasePath { get; set; }
        
        [MaxLength(50)]
        public string? Version { get; set; }
        
        [MaxLength(100)]
        public string? OperatingSystem { get; set; }
    }
    
    public class AnonymousNodeResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string MachineId { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; }
        public string? DatabasePath { get; set; }
        public string? Version { get; set; }
        public string? OperatingSystem { get; set; }
        public bool IsAnonymous { get; set; }
        public string AnonymousToken { get; set; } = string.Empty;
        public DateTime AnonymousExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastSeen { get; set; }
    }
    
    // DTOs para vinculação de nó anônimo ao usuário
    public class BindNodeToUserRequest
    {
        [Required]
        [MaxLength(100)]
        public string AnonymousToken { get; set; } = string.Empty;
    }
    
    public class BindCurrentNodeRequest
    {
        [Required]
        [MaxLength(100)]
        public string MachineId { get; set; } = string.Empty;
    }
    
    public class BindNodeToUserResponse
    {
        public string NodeId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; }
    }
    
    // DTOs para desvinculação de nó do usuário
    public class UnbindNodeFromUserRequest
    {
        [Required]
        [MaxLength(100)]
        public string NodeId { get; set; } = string.Empty;
    }
    
    public class UnbindNodeFromUserResponse
    {
        public string NodeId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; }
    }
}
