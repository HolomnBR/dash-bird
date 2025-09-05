using Microsoft.EntityFrameworkCore;
using FirebirdApi.Data;
using FirebirdApi.Models;

namespace FirebirdApi.Services
{
    public class LocalNodeService : ILocalNodeService
    {
        private readonly LocalDbContext _context;
        private readonly ILogger<LocalNodeService> _logger;

        public LocalNodeService(LocalDbContext context, ILogger<LocalNodeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<LocalNode> SaveOrUpdateLocalNodeAsync(LocalNode node)
        {
            try
            {
                var existingNode = await _context.LocalNodes
                    .FirstOrDefaultAsync(n => n.Id == node.Id);

                if (existingNode != null)
                {
                    // Atualizar nó existente
                    existingNode.MachineName = node.MachineName;
                    existingNode.OperatingSystem = node.OperatingSystem;
                    existingNode.SystemVersion = node.SystemVersion;
                    existingNode.Architecture = node.Architecture;
                    existingNode.IpAddress = node.IpAddress;
                    existingNode.Port = node.Port;
                    existingNode.IsActive = node.IsActive;
                    existingNode.UpdatedAt = DateTime.UtcNow;
                    existingNode.LastSeen = DateTime.UtcNow;
                    existingNode.UserId = node.UserId;
                    existingNode.IsAnonymous = node.IsAnonymous;
                    existingNode.AnonymousToken = node.AnonymousToken;
                    existingNode.AnonymousExpiresAt = node.AnonymousExpiresAt;

                    _context.LocalNodes.Update(existingNode);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Nó local atualizado: {NodeId} - {MachineName}", existingNode.Id, existingNode.MachineName);
                    return existingNode;
                }
                else
                {
                    // Criar novo nó
                    node.CreatedAt = DateTime.UtcNow;
                    node.UpdatedAt = DateTime.UtcNow;
                    node.LastSeen = DateTime.UtcNow;

                    _context.LocalNodes.Add(node);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Nó local criado: {NodeId} - {MachineName}", node.Id, node.MachineName);
                    return node;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar/atualizar nó local: {NodeId}", node.Id);
                throw;
            }
        }

        public async Task<LocalNode?> GetLocalNodeByIdAsync(string nodeId)
        {
            try
            {
                return await _context.LocalNodes
                    .FirstOrDefaultAsync(n => n.Id == nodeId && n.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter nó local por ID: {NodeId}", nodeId);
                return null;
            }
        }

        public async Task<LocalNode?> GetLocalNodeByMachineNameAsync(string machineName)
        {
            try
            {
                return await _context.LocalNodes
                    .FirstOrDefaultAsync(n => n.MachineName == machineName && n.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter nó local por nome da máquina: {MachineName}", machineName);
                return null;
            }
        }

        public async Task<List<LocalNode>> GetAllLocalNodesAsync()
        {
            try
            {
                return await _context.LocalNodes
                    .Where(n => n.IsActive)
                    .OrderByDescending(n => n.LastSeen)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter todos os nós locais");
                return new List<LocalNode>();
            }
        }

        public async Task<LocalNode?> UpdateSystemInfoAsync(string nodeId, string operatingSystem, string systemVersion, string architecture)
        {
            try
            {
                var node = await _context.LocalNodes
                    .FirstOrDefaultAsync(n => n.Id == nodeId && n.IsActive);

                if (node != null)
                {
                    node.OperatingSystem = operatingSystem;
                    node.SystemVersion = systemVersion;
                    node.Architecture = architecture;
                    node.UpdatedAt = DateTime.UtcNow;
                    node.LastSeen = DateTime.UtcNow;

                    _context.LocalNodes.Update(node);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Informações do sistema atualizadas para nó: {NodeId}", nodeId);
                    return node;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar informações do sistema do nó: {NodeId}", nodeId);
                return null;
            }
        }

        public async Task<bool> UpdateNodeConnectionStatusAsync(string nodeId, bool isActive, DateTime lastSeen)
        {
            try
            {
                var node = await _context.LocalNodes
                    .FirstOrDefaultAsync(n => n.Id == nodeId);

                if (node != null)
                {
                    node.IsActive = isActive;
                    node.LastSeen = lastSeen;
                    node.UpdatedAt = DateTime.UtcNow;

                    _context.LocalNodes.Update(node);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Status de conexão atualizado para nó: {NodeId} - Ativo: {IsActive}", nodeId, isActive);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar status de conexão do nó: {NodeId}", nodeId);
                return false;
            }
        }

        public async Task<bool> BindNodeToUserAsync(string nodeId, string userId)
        {
            try
            {
                var node = await _context.LocalNodes
                    .FirstOrDefaultAsync(n => n.Id == nodeId && n.IsActive);

                if (node != null)
                {
                    node.UserId = userId;
                    node.IsAnonymous = false;
                    node.UpdatedAt = DateTime.UtcNow;
                    node.LastSeen = DateTime.UtcNow;

                    _context.LocalNodes.Update(node);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Nó vinculado ao usuário: {NodeId} -> {UserId}", nodeId, userId);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao vincular nó ao usuário: {NodeId} -> {UserId}", nodeId, userId);
                return false;
            }
        }

        public async Task<bool> UnbindNodeFromUserAsync(string nodeId)
        {
            try
            {
                var node = await _context.LocalNodes
                    .FirstOrDefaultAsync(n => n.Id == nodeId && n.IsActive);

                if (node != null)
                {
                    node.UserId = null;
                    node.IsAnonymous = true;
                    node.UpdatedAt = DateTime.UtcNow;
                    node.LastSeen = DateTime.UtcNow;

                    _context.LocalNodes.Update(node);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Nó desvinculado do usuário: {NodeId}", nodeId);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao desvincular nó do usuário: {NodeId}", nodeId);
                return false;
            }
        }

        public async Task<bool> HasLocalNodeAsync(string machineName)
        {
            try
            {
                return await _context.LocalNodes
                    .AnyAsync(n => n.MachineName == machineName && n.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar se existe nó local: {MachineName}", machineName);
                return false;
            }
        }

        public async Task<LocalNode> CreateLocalNodeWithSystemInfoAsync(string machineName, string operatingSystem, string systemVersion, string architecture, string? ipAddress = null, int port = 8000)
        {
            try
            {
                var nodeId = Guid.NewGuid().ToString();
                var node = new LocalNode
                {
                    Id = nodeId,
                    MachineName = machineName,
                    OperatingSystem = operatingSystem,
                    SystemVersion = systemVersion,
                    Architecture = architecture,
                    IpAddress = ipAddress,
                    Port = port,
                    IsActive = true,
                    IsAnonymous = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    LastSeen = DateTime.UtcNow
                };

                _context.LocalNodes.Add(node);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Nó local criado com informações do sistema: {NodeId} - {MachineName}", nodeId, machineName);
                return node;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar nó local com informações do sistema: {MachineName}", machineName);
                throw;
            }
        }
    }
}
