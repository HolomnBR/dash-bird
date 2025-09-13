using Microsoft.EntityFrameworkCore;
using FirebirdApi.Data;
using FirebirdApi.Models;

namespace FirebirdApi.Services
{
    public class LocalNodeService : ILocalNodeService
    {
        private readonly LocalDbContext _context;
        private readonly ISystemInfoService _systemInfoService;
        private readonly ILogger<LocalNodeService> _logger;

        public LocalNodeService(LocalDbContext context, ISystemInfoService systemInfoService, ILogger<LocalNodeService> logger)
        {
            _context = context;
            _systemInfoService = systemInfoService;
            _logger = logger;
        }

        public async Task<LocalNode> SaveOrUpdateLocalNodeAsync(LocalNode node)
        {
            try
            {
                // REGRA DE UNICIDADE: Buscar por MachineId primeiro, depois por Id
                // Isso garante que sempre exista apenas um nó por máquina
                var existingNode = await _context.LocalNodes
                    .FirstOrDefaultAsync(n => n.MachineId == node.MachineId);

                if (existingNode != null)
                {
                    // Atualizar nó existente com base no MachineId
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

                    _logger.LogInformation("Nó local atualizado por MachineId: {NodeId} - {MachineName} (MachineId: {MachineId})", 
                        existingNode.Id, existingNode.MachineName, existingNode.MachineId);
                    return existingNode;
                }
                else
                {
                    // Verificar se existe algum nó com o mesmo MachineName mas MachineId diferente
                    // Se existir, remover o antigo para evitar duplicação
                    var duplicateNode = await _context.LocalNodes
                        .FirstOrDefaultAsync(n => n.MachineName == node.MachineName && n.MachineId != node.MachineId);
                    
                    if (duplicateNode != null)
                    {
                        _logger.LogWarning("Removendo nó duplicado com mesmo MachineName mas MachineId diferente: {OldNodeId} (MachineId: {OldMachineId}) -> {NewNodeId} (MachineId: {NewMachineId})", 
                            duplicateNode.Id, duplicateNode.MachineId, node.Id, node.MachineId);
                        
                        _context.LocalNodes.Remove(duplicateNode);
                    }

                    // Criar novo nó
                    node.CreatedAt = DateTime.UtcNow;
                    node.UpdatedAt = DateTime.UtcNow;
                    node.LastSeen = DateTime.UtcNow;

                    _context.LocalNodes.Add(node);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Nó local criado: {NodeId} - {MachineName} (MachineId: {MachineId})", 
                        node.Id, node.MachineName, node.MachineId);
                    return node;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar/atualizar nó local: {NodeId} (MachineId: {MachineId})", node.Id, node.MachineId);
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
                // REGRA DE UNICIDADE: Buscar primeiro por MachineId se possível
                // Se não encontrar, buscar por MachineName
                var node = await _context.LocalNodes
                    .FirstOrDefaultAsync(n => n.MachineName == machineName && n.IsActive);

                if (node != null)
                {
                    _logger.LogInformation("Nó encontrado por MachineName: {MachineName} (MachineId: {MachineId})", machineName, node.MachineId);
                }
                else
                {
                    _logger.LogWarning("Nó não encontrado por MachineName: {MachineName}", machineName);
                }

                return node;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter nó local por nome da máquina: {MachineName}", machineName);
                return null;
            }
        }

        public async Task<LocalNode?> GetLocalNodeByMachineIdAsync(string machineId)
        {
            try
            {
                return await _context.LocalNodes
                    .FirstOrDefaultAsync(n => n.MachineId == machineId && n.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter nó local por MachineId: {MachineId}", machineId);
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

        public async Task<LocalNode> CreateLocalNodeWithSystemInfoAsync(string machineName, string operatingSystem, string systemVersion, string architecture, string? ipAddress = null, int port = 8000, string? machineId = null)
        {
            try
            {
                // Se MachineId não foi fornecido, gerar um baseado no nome da máquina
                if (string.IsNullOrWhiteSpace(machineId))
                {
                    machineId = GenerateMachineIdFromName(machineName);
                    _logger.LogWarning("MachineId não fornecido para {MachineName}, gerando automaticamente: {MachineId}", machineName, machineId);
                }

                // Verificar se já existe nó com este MachineId
                var existingNode = await _context.LocalNodes
                    .FirstOrDefaultAsync(n => n.MachineId == machineId);

                if (existingNode != null)
                {
                    _logger.LogInformation("Nó já existe para MachineId {MachineId}, atualizando informações do sistema", machineId);
                    
                    // Atualizar nó existente
                    existingNode.MachineName = machineName;
                    existingNode.OperatingSystem = operatingSystem;
                    existingNode.SystemVersion = systemVersion;
                    existingNode.Architecture = architecture;
                    existingNode.IpAddress = ipAddress ?? existingNode.IpAddress;
                    existingNode.Port = port > 0 ? port : existingNode.Port;
                    existingNode.IsActive = true;
                    existingNode.UpdatedAt = DateTime.UtcNow;
                    existingNode.LastSeen = DateTime.UtcNow;

                    _context.LocalNodes.Update(existingNode);
                    await _context.SaveChangesAsync();

                    return existingNode;
                }

                var nodeId = Guid.NewGuid().ToString();
                var node = new LocalNode
                {
                    Id = nodeId,
                    MachineId = machineId, // IMPORTANTE: Definir MachineId para garantir unicidade
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

                _logger.LogInformation("Nó local criado com informações do sistema: {NodeId} - {MachineName} (MachineId: {MachineId})", nodeId, machineName, machineId);
                return node;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar nó local com informações do sistema: {MachineName}", machineName);
                throw;
            }
        }

        /// <summary>
        /// Garante que existe um nó local para a máquina atual
        /// </summary>
        public async Task<LocalNode> EnsureLocalNodeExistsAsync(string machineName, string? machineId = null)
        {
            try
            {
                // Se MachineId foi fornecido, buscar por ele primeiro
                if (!string.IsNullOrWhiteSpace(machineId))
                {
                    var existingNode = await GetLocalNodeByMachineIdAsync(machineId);
                    if (existingNode != null)
                    {
                        _logger.LogInformation("Nó local já existe para MachineId: {MachineId}", machineId);
                        return existingNode;
                    }
                }

                // Buscar por nome da máquina
                var nodeByName = await GetLocalNodeByMachineNameAsync(machineName);
                if (nodeByName != null)
                {
                    _logger.LogInformation("Nó local encontrado por nome: {MachineName}", machineName);
                    return nodeByName;
                }

                // Se não existe, criar um novo nó
                _logger.LogInformation("Nó local não encontrado, criando novo para: {MachineName}", machineName);
                
                var operatingSystem = _systemInfoService.GetOperatingSystem();
                var systemVersion = _systemInfoService.GetSystemVersion();
                var architecture = _systemInfoService.GetArchitecture();

                return await CreateLocalNodeWithSystemInfoAsync(
                    machineName, 
                    operatingSystem, 
                    systemVersion, 
                    architecture, 
                    machineId: machineId
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao garantir existência do nó local: {MachineName}", machineName);
                throw;
            }
        }

        public async Task<List<LocalNode>> GetUserNodesAsync(string userId)
        {
            try
            {
                _logger.LogInformation("Buscando nós locais do usuário: {UserId}", userId);

                var userNodes = await _context.LocalNodes
                    .Where(n => n.UserId == userId && n.IsActive)
                    .OrderBy(n => n.MachineName)
                    .ToListAsync();

                _logger.LogInformation("Encontrados {Count} nós locais para o usuário {UserId}", userNodes.Count, userId);
                return userNodes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar nós locais do usuário: {UserId}", userId);
                return new List<LocalNode>();
            }
        }

        /// <summary>
        /// Gera um MachineId único baseado no nome da máquina
        /// </summary>
        private string GenerateMachineIdFromName(string machineName)
        {
            // Usar hash do nome da máquina + timestamp para garantir unicidade
            var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(machineName + Environment.MachineName));
            var machineId = Convert.ToHexString(hash)[..16]; // Primeiros 16 caracteres
            return machineId.ToLower();
        }
    }
}
