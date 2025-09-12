using Microsoft.AspNetCore.Mvc;
using FirebirdApi.Services;
using FirebirdApi.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace FirebirdApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocalNodeController : ControllerBase
    {
        private readonly ILocalNodeService _localNodeService;
        private readonly ISystemInfoService _systemInfoService;
        private readonly LocalNodeManagerService _nodeManagerService;
        private readonly ILogger<LocalNodeController> _logger;

        public LocalNodeController(
            ILocalNodeService localNodeService,
            ISystemInfoService systemInfoService,
            LocalNodeManagerService nodeManagerService,
            ILogger<LocalNodeController> logger)
        {
            _localNodeService = localNodeService;
            _systemInfoService = systemInfoService;
            _nodeManagerService = nodeManagerService;
            _logger = logger;
        }

        /// <summary>
        /// Salvar ou atualizar nó local com informações do sistema
        /// </summary>
        [HttpPost("save-local-node")]
        [SwaggerOperation(
            Summary = "Salvar nó local",
            Description = "Salva ou atualiza as informações do nó local no SQLite com dados do sistema. Garante unicidade por MachineId."
        )]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SaveLocalNode([FromBody] SaveLocalNodeRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.MachineName))
                {
                    return BadRequest(new { success = false, message = "Nome da máquina é obrigatório" });
                }

                if (string.IsNullOrWhiteSpace(request.MachineId))
                {
                    return BadRequest(new { success = false, message = "MachineId é obrigatório para garantir unicidade" });
                }

                // Obter informações do sistema se não fornecidas
                var operatingSystem = !string.IsNullOrWhiteSpace(request.OperatingSystem) 
                    ? request.OperatingSystem 
                    : _systemInfoService.GetOperatingSystem();
                
                var systemVersion = !string.IsNullOrWhiteSpace(request.SystemVersion) 
                    ? request.SystemVersion 
                    : _systemInfoService.GetSystemVersion();
                
                var architecture = !string.IsNullOrWhiteSpace(request.Architecture) 
                    ? request.Architecture 
                    : _systemInfoService.GetArchitecture();

                _logger.LogInformation("📥 Dados recebidos - MachineName: {MachineName}, MachineId: {MachineId}, OperatingSystem: {OperatingSystem}, SystemVersion: {SystemVersion}, Architecture: {Architecture}", 
                    request.MachineName, request.MachineId, operatingSystem, systemVersion, architecture);

                // REGRA DE UNICIDADE: Verificar se já existe nó para este MachineId
                var existingNode = await _localNodeService.GetLocalNodeByMachineIdAsync(request.MachineId);
                
                LocalNode node;
                bool isUpdate = existingNode != null;
                
                if (existingNode != null)
                {
                    // Nó já existe - atualizar propriedades do sistema
                    _logger.LogInformation("Nó existente encontrado para MachineId {MachineId}, atualizando propriedades do sistema", request.MachineId);
                    
                    existingNode.MachineName = request.MachineName; // Atualizar nome da máquina se mudou
                    existingNode.OperatingSystem = operatingSystem;
                    existingNode.SystemVersion = systemVersion;
                    existingNode.Architecture = architecture;
                    existingNode.IpAddress = request.IpAddress ?? existingNode.IpAddress;
                    existingNode.Port = request.Port > 0 ? request.Port : existingNode.Port;
                    existingNode.IsActive = true;
                    existingNode.LastSeen = DateTime.UtcNow;
                    existingNode.UpdatedAt = DateTime.UtcNow;

                    node = await _localNodeService.SaveOrUpdateLocalNodeAsync(existingNode);
                }
                else
                {
                    // Nó não existe - criar novo nó
                    _logger.LogInformation("Criando novo nó para MachineId {MachineId} (MachineName: {MachineName})", request.MachineId, request.MachineName);
                    
                    node = new LocalNode
                    {
                        Id = Guid.NewGuid().ToString(),
                        MachineId = request.MachineId, // IMPORTANTE: Usar MachineId como chave única
                        MachineName = request.MachineName,
                        OperatingSystem = operatingSystem,
                        SystemVersion = systemVersion,
                        Architecture = architecture,
                        IpAddress = request.IpAddress,
                        Port = request.Port > 0 ? request.Port : 8000,
                        IsActive = true,
                        IsAnonymous = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        LastSeen = DateTime.UtcNow
                    };

                    node = await _localNodeService.SaveOrUpdateLocalNodeAsync(node);
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = node.Id,
                        machineId = node.MachineId,
                        machineName = node.MachineName,
                        operatingSystem = node.OperatingSystem,
                        systemVersion = node.SystemVersion,
                        architecture = node.Architecture,
                        ipAddress = node.IpAddress,
                        port = node.Port,
                        isActive = node.IsActive,
                        isAnonymous = node.IsAnonymous,
                        createdAt = node.CreatedAt,
                        updatedAt = node.UpdatedAt,
                        lastSeen = node.LastSeen
                    },
                    message = isUpdate ? "Nó local atualizado com sucesso" : "Nó local criado com sucesso"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar nó local: {MachineName} (MachineId: {MachineId})", request.MachineName, request.MachineId);
                return StatusCode(500, new
                {
                    success = false,
                    error = $"Erro interno: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Obter nó local por ID
        /// </summary>
        [HttpGet("{nodeId}")]
        [SwaggerOperation(
            Summary = "Obter nó local por ID",
            Description = "Retorna as informações do nó local pelo ID."
        )]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLocalNode(string nodeId)
        {
            try
            {
                var node = await _localNodeService.GetLocalNodeByIdAsync(nodeId);
                
                if (node == null)
                {
                    return NotFound(new { success = false, message = "Nó local não encontrado" });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = node.Id,
                        machineName = node.MachineName,
                        operatingSystem = node.OperatingSystem,
                        systemVersion = node.SystemVersion,
                        architecture = node.Architecture,
                        ipAddress = node.IpAddress,
                        port = node.Port,
                        isActive = node.IsActive,
                        isAnonymous = node.IsAnonymous,
                        userId = node.UserId,
                        createdAt = node.CreatedAt,
                        updatedAt = node.UpdatedAt,
                        lastSeen = node.LastSeen
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter nó local: {NodeId}", nodeId);
                return StatusCode(500, new
                {
                    success = false,
                    error = $"Erro interno: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Obter nó local por nome da máquina
        /// </summary>
        [HttpGet("by-machine/{machineName}")]
        [SwaggerOperation(
            Summary = "Obter nó local por nome da máquina",
            Description = "Retorna as informações do nó local pelo nome da máquina."
        )]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLocalNodeByMachine(string machineName)
        {
            try
            {
                var node = await _localNodeService.GetLocalNodeByMachineNameAsync(machineName);
                
                if (node == null)
                {
                    return NotFound(new { success = false, message = "Nó local não encontrado" });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = node.Id,
                        machineName = node.MachineName,
                        operatingSystem = node.OperatingSystem,
                        systemVersion = node.SystemVersion,
                        architecture = node.Architecture,
                        ipAddress = node.IpAddress,
                        port = node.Port,
                        isActive = node.IsActive,
                        isAnonymous = node.IsAnonymous,
                        userId = node.UserId,
                        createdAt = node.CreatedAt,
                        updatedAt = node.UpdatedAt,
                        lastSeen = node.LastSeen
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter nó local por máquina: {MachineName}", machineName);
                return StatusCode(500, new
                {
                    success = false,
                    error = $"Erro interno: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Obter todos os nós locais
        /// </summary>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Listar todos os nós locais",
            Description = "Retorna todos os nós locais ativos."
        )]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllLocalNodes()
        {
            try
            {
                var nodes = await _localNodeService.GetAllLocalNodesAsync();

                return Ok(new
                {
                    success = true,
                    data = nodes.Select(node => new
                    {
                        id = node.Id,
                        machineName = node.MachineName,
                        operatingSystem = node.OperatingSystem,
                        systemVersion = node.SystemVersion,
                        architecture = node.Architecture,
                        ipAddress = node.IpAddress,
                        port = node.Port,
                        isActive = node.IsActive,
                        isAnonymous = node.IsAnonymous,
                        userId = node.UserId,
                        createdAt = node.CreatedAt,
                        updatedAt = node.UpdatedAt,
                        lastSeen = node.LastSeen
                    }).ToList(),
                    count = nodes.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter todos os nós locais");
                return StatusCode(500, new
                {
                    success = false,
                    error = $"Erro interno: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Atualizar informações do sistema do nó local
        /// </summary>
        [HttpPut("{nodeId}/system-info")]
        [SwaggerOperation(
            Summary = "Atualizar informações do sistema",
            Description = "Atualiza as informações do sistema operacional, versão e arquitetura do nó local."
        )]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateSystemInfo(string nodeId, [FromBody] UpdateSystemInfoRequest request)
        {
            try
            {
                var node = await _localNodeService.UpdateSystemInfoAsync(
                    nodeId, 
                    request.OperatingSystem, 
                    request.SystemVersion, 
                    request.Architecture
                );

                if (node == null)
                {
                    return NotFound(new { success = false, message = "Nó local não encontrado" });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = node.Id,
                        machineName = node.MachineName,
                        operatingSystem = node.OperatingSystem,
                        systemVersion = node.SystemVersion,
                        architecture = node.Architecture,
                        updatedAt = node.UpdatedAt
                    },
                    message = "Informações do sistema atualizadas com sucesso"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar informações do sistema: {NodeId}", nodeId);
                return StatusCode(500, new
                {
                    success = false,
                    error = $"Erro interno: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Vincular nó a um usuário
        /// </summary>
        [HttpPost("{nodeId}/bind-user")]
        [SwaggerOperation(
            Summary = "Vincular nó ao usuário",
            Description = "Vincula o nó local a um usuário autenticado."
        )]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> BindNodeToUser(string nodeId, [FromBody] BindLocalNodeToUserRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.UserId))
                {
                    return BadRequest(new { success = false, message = "ID do usuário é obrigatório" });
                }

                var success = await _localNodeService.BindNodeToUserAsync(nodeId, request.UserId);

                if (!success)
                {
                    return NotFound(new { success = false, message = "Nó local não encontrado" });
                }

                return Ok(new
                {
                    success = true,
                    message = "Nó vinculado ao usuário com sucesso",
                    nodeId = nodeId,
                    userId = request.UserId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao vincular nó ao usuário: {NodeId}", nodeId);
                return StatusCode(500, new
                {
                    success = false,
                    error = $"Erro interno: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Desvincular nó de um usuário
        /// </summary>
        [HttpPost("{nodeId}/unbind-user")]
        [SwaggerOperation(
            Summary = "Desvincular nó do usuário",
            Description = "Desvincula o nó local de um usuário, tornando-o anônimo."
        )]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UnbindNodeFromUser(string nodeId)
        {
            try
            {
                var success = await _localNodeService.UnbindNodeFromUserAsync(nodeId);

                if (!success)
                {
                    return NotFound(new { success = false, message = "Nó local não encontrado" });
                }

                return Ok(new
                {
                    success = true,
                    message = "Nó desvinculado do usuário com sucesso",
                    nodeId = nodeId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao desvincular nó do usuário: {NodeId}", nodeId);
                return StatusCode(500, new
                {
                    success = false,
                    error = $"Erro interno: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Verificar se existe nó local para a máquina atual
        /// </summary>
        [HttpGet("exists/{machineName}")]
        [SwaggerOperation(
            Summary = "Verificar se existe nó local",
            Description = "Verifica se já existe um nó local registrado para o nome da máquina especificado."
        )]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> HasLocalNode(string machineName)
        {
            try
            {
                var exists = await _localNodeService.HasLocalNodeAsync(machineName);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        machineName = machineName,
                        exists = exists
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar existência do nó local: {MachineName}", machineName);
                return StatusCode(500, new
                {
                    success = false,
                    error = $"Erro interno: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Obter nó local por MachineId (chave única)
        /// </summary>
        [HttpGet("by-machine-id/{machineId}")]
        [SwaggerOperation(
            Summary = "Obter nó local por MachineId",
            Description = "Retorna as informações do nó local pelo MachineId (chave única)."
        )]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLocalNodeByMachineId(string machineId)
        {
            try
            {
                var node = await _localNodeService.GetLocalNodeByMachineIdAsync(machineId);
                
                if (node == null)
                {
                    return NotFound(new { success = false, message = "Nó local não encontrado para este MachineId" });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = node.Id,
                        machineId = node.MachineId,
                        machineName = node.MachineName,
                        operatingSystem = node.OperatingSystem,
                        systemVersion = node.SystemVersion,
                        architecture = node.Architecture,
                        ipAddress = node.IpAddress,
                        port = node.Port,
                        isActive = node.IsActive,
                        isAnonymous = node.IsAnonymous,
                        userId = node.UserId,
                        createdAt = node.CreatedAt,
                        updatedAt = node.UpdatedAt,
                        lastSeen = node.LastSeen
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter nó local por MachineId: {MachineId}", machineId);
                return StatusCode(500, new
                {
                    success = false,
                    error = $"Erro interno: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Garantir que existe nó local para a máquina atual (thread-safe)
        /// </summary>
        [HttpPost("ensure-exists")]
        [SwaggerOperation(
            Summary = "Garantir existência do nó local",
            Description = "Garante que existe um nó local para a máquina atual. Se não existir, cria um novo. Thread-safe e evita chamadas duplicadas."
        )]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EnsureLocalNodeExists([FromBody] EnsureLocalNodeRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.MachineName))
                {
                    return BadRequest(new { success = false, message = "Nome da máquina é obrigatório" });
                }

                _logger.LogInformation("🔍 Garantindo existência do nó local para: {MachineName} (MachineId: {MachineId})", 
                    request.MachineName, request.MachineId ?? "não fornecido");

                // Usar o gerenciador thread-safe
                var node = await _nodeManagerService.EnsureLocalNodeExistsAsync(request.MachineName, request.MachineId);

                _logger.LogInformation("✅ Nó local garantido com sucesso: {NodeId} (MachineId: {MachineId})", 
                    node.Id, node.MachineId);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = node.Id,
                        machineId = node.MachineId,
                        machineName = node.MachineName,
                        operatingSystem = node.OperatingSystem,
                        systemVersion = node.SystemVersion,
                        architecture = node.Architecture,
                        ipAddress = node.IpAddress,
                        port = node.Port,
                        isActive = node.IsActive,
                        isAnonymous = node.IsAnonymous,
                        userId = node.UserId,
                        createdAt = node.CreatedAt,
                        updatedAt = node.UpdatedAt,
                        lastSeen = node.LastSeen
                    },
                    message = "Nó local garantido com sucesso"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao garantir existência do nó local: {MachineName}", request.MachineName);
                return StatusCode(500, new
                {
                    success = false,
                    error = $"Erro interno: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Obter estatísticas do gerenciador de nós locais
        /// </summary>
        [HttpGet("manager-stats")]
        [SwaggerOperation(
            Summary = "Estatísticas do gerenciador de nós",
            Description = "Retorna estatísticas do cache e operações pendentes do gerenciador de nós locais."
        )]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public IActionResult GetManagerStats()
        {
            try
            {
                var (cachedNodes, pendingOperations) = _nodeManagerService.GetCacheStats();
                
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        cachedNodes,
                        pendingOperations,
                        timestamp = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter estatísticas do gerenciador");
                return StatusCode(500, new
                {
                    success = false,
                    error = $"Erro interno: {ex.Message}"
                });
            }
        }
    }

    // DTOs para as requisições
    public class SaveLocalNodeRequest
    {
        public string MachineName { get; set; } = string.Empty;
        public string MachineId { get; set; } = string.Empty; // OBRIGATÓRIO para garantir unicidade
        public string? OperatingSystem { get; set; }
        public string? SystemVersion { get; set; }
        public string? Architecture { get; set; }
        public string? IpAddress { get; set; }
        public int Port { get; set; } = 8000;
    }

    public class UpdateSystemInfoRequest
    {
        public string OperatingSystem { get; set; } = string.Empty;
        public string SystemVersion { get; set; } = string.Empty;
        public string Architecture { get; set; } = string.Empty;
    }

    public class BindLocalNodeToUserRequest
    {
        public string UserId { get; set; } = string.Empty;
    }

    public class EnsureLocalNodeRequest
    {
        public string MachineName { get; set; } = string.Empty;
        public string? MachineId { get; set; }
    }
}
