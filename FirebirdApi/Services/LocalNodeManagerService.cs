using FirebirdApi.Models;
using System.Collections.Concurrent;

namespace FirebirdApi.Services
{
    /// <summary>
    /// Serviço singleton para gerenciar nós locais de forma thread-safe
    /// Evita chamadas duplicadas e garante que apenas um nó seja criado por máquina
    /// </summary>
    public class LocalNodeManagerService
    {
        private readonly ILocalNodeService _localNodeService;
        private readonly ISystemInfoService _systemInfoService;
        private readonly ILogger<LocalNodeManagerService> _logger;
        
        // Cache thread-safe para evitar chamadas duplicadas
        private readonly ConcurrentDictionary<string, Task<LocalNode>> _pendingOperations = new();
        private readonly ConcurrentDictionary<string, LocalNode> _nodeCache = new();

        public LocalNodeManagerService(ILocalNodeService localNodeService, ISystemInfoService systemInfoService, ILogger<LocalNodeManagerService> logger)
        {
            _localNodeService = localNodeService;
            _systemInfoService = systemInfoService;
            _logger = logger;
        }

        /// <summary>
        /// Garante que existe um nó local para a máquina, evitando chamadas duplicadas
        /// </summary>
        public async Task<LocalNode> EnsureLocalNodeExistsAsync(string machineName, string? machineId = null)
        {
            // Criar chave única para a operação
            var operationKey = machineId ?? machineName;
            
            _logger.LogInformation("🔍 Iniciando operação para garantir nó local: {MachineName} (Key: {OperationKey})", 
                machineName, operationKey);

            // Verificar se já existe uma operação em andamento para esta máquina
            if (_pendingOperations.TryGetValue(operationKey, out var pendingTask))
            {
                _logger.LogInformation("⏳ Operação já em andamento para {OperationKey}, aguardando resultado...", operationKey);
                return await pendingTask;
            }

            // Verificar cache primeiro
            if (_nodeCache.TryGetValue(operationKey, out var cachedNode))
            {
                _logger.LogInformation("✅ Nó encontrado no cache: {OperationKey}", operationKey);
                return cachedNode;
            }

            // Criar nova operação
            var operation = CreateNodeOperationAsync(machineName, machineId, operationKey);
            
            // Registrar operação em andamento
            _pendingOperations.TryAdd(operationKey, operation);

            try
            {
                var result = await operation;
                
                // Adicionar ao cache se bem-sucedido
                if (result != null)
                {
                    _nodeCache.TryAdd(operationKey, result);
                }
                
                return result;
            }
            finally
            {
                // Remover operação pendente
                _pendingOperations.TryRemove(operationKey, out _);
            }
        }

        private async Task<LocalNode> CreateNodeOperationAsync(string machineName, string? machineId, string operationKey)
        {
            try
            {
                _logger.LogInformation("🚀 Executando operação para criar/obter nó: {MachineName} (Key: {OperationKey})", 
                    machineName, operationKey);

                // Se MachineId foi fornecido, buscar por ele primeiro
                if (!string.IsNullOrWhiteSpace(machineId))
                {
                    var existingNode = await _localNodeService.GetLocalNodeByMachineIdAsync(machineId);
                    if (existingNode != null)
                    {
                        _logger.LogInformation("✅ Nó encontrado por MachineId: {MachineId}", machineId);
                        return existingNode;
                    }
                }

                // Buscar por nome da máquina
                var nodeByName = await _localNodeService.GetLocalNodeByMachineNameAsync(machineName);
                if (nodeByName != null)
                {
                    _logger.LogInformation("✅ Nó encontrado por nome: {MachineName}", machineName);
                    return nodeByName;
                }

                // Se não existe, criar novo nó
                _logger.LogInformation("🆕 Criando novo nó para: {MachineName} (Key: {OperationKey})", machineName, operationKey);
                
                var operatingSystem = _systemInfoService.GetOperatingSystem();
                var systemVersion = _systemInfoService.GetSystemVersion();
                var architecture = _systemInfoService.GetArchitecture();

                var newNode = await _localNodeService.CreateLocalNodeWithSystemInfoAsync(
                    machineName, 
                    operatingSystem, 
                    systemVersion, 
                    architecture, 
                    machineId: machineId
                );

                _logger.LogInformation("✅ Novo nó criado com sucesso: {NodeId} (Key: {OperationKey})", 
                    newNode.Id, operationKey);

                return newNode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao criar/obter nó: {MachineName} (Key: {OperationKey})", 
                    machineName, operationKey);
                throw;
            }
        }

        /// <summary>
        /// Limpar cache (útil para testes ou reset)
        /// </summary>
        public void ClearCache()
        {
            _nodeCache.Clear();
            _logger.LogInformation("🧹 Cache de nós locais limpo");
        }

        /// <summary>
        /// Obter estatísticas do cache
        /// </summary>
        public (int CachedNodes, int PendingOperations) GetCacheStats()
        {
            return (_nodeCache.Count, _pendingOperations.Count);
        }
    }
}
