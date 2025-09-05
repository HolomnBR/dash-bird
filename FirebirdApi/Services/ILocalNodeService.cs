using FirebirdApi.Models;

namespace FirebirdApi.Services
{
    public interface ILocalNodeService
    {
        /// <summary>
        /// Salvar ou atualizar nó local no SQLite
        /// </summary>
        Task<LocalNode> SaveOrUpdateLocalNodeAsync(LocalNode node);

        /// <summary>
        /// Obter nó local por ID
        /// </summary>
        Task<LocalNode?> GetLocalNodeByIdAsync(string nodeId);

        /// <summary>
        /// Obter nó local por nome da máquina
        /// </summary>
        Task<LocalNode?> GetLocalNodeByMachineNameAsync(string machineName);

        /// <summary>
        /// Obter todos os nós locais ativos
        /// </summary>
        Task<List<LocalNode>> GetAllLocalNodesAsync();

        /// <summary>
        /// Atualizar informações do sistema do nó local
        /// </summary>
        Task<LocalNode?> UpdateSystemInfoAsync(string nodeId, string operatingSystem, string systemVersion, string architecture);

        /// <summary>
        /// Atualizar status de conexão do nó
        /// </summary>
        Task<bool> UpdateNodeConnectionStatusAsync(string nodeId, bool isActive, DateTime lastSeen);

        /// <summary>
        /// Vincular nó a um usuário
        /// </summary>
        Task<bool> BindNodeToUserAsync(string nodeId, string userId);

        /// <summary>
        /// Desvincular nó de um usuário
        /// </summary>
        Task<bool> UnbindNodeFromUserAsync(string nodeId);

        /// <summary>
        /// Verificar se existe nó local para a máquina atual
        /// </summary>
        Task<bool> HasLocalNodeAsync(string machineName);

        /// <summary>
        /// Criar nó local com informações do sistema
        /// </summary>
        Task<LocalNode> CreateLocalNodeWithSystemInfoAsync(string machineName, string operatingSystem, string systemVersion, string architecture, string? ipAddress = null, int port = 8000);
    }
}
