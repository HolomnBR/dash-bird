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
        private readonly ILogger<LocalNodeController> _logger;

        public LocalNodeController(
            ILocalNodeService localNodeService,
            ISystemInfoService systemInfoService,
            ILogger<LocalNodeController> logger)
        {
            _localNodeService = localNodeService;
            _systemInfoService = systemInfoService;
            _logger = logger;
        }

        /// <summary>
        /// Salvar ou atualizar nó local com informações do sistema
        /// </summary>
        [HttpPost("save-local-node")]
        [SwaggerOperation(
            Summary = "Salvar nó local",
            Description = "Salva ou atualiza as informações do nó local no SQLite com dados do sistema."
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

                _logger.LogInformation("📥 Dados recebidos - MachineName: {MachineName}, OperatingSystem: {OperatingSystem}, SystemVersion: {SystemVersion}, Architecture: {Architecture}", 
                    request.MachineName, operatingSystem, systemVersion, architecture);

                // Verificar se já existe nó para esta máquina
                var existingNode = await _localNodeService.GetLocalNodeByMachineNameAsync(request.MachineName);
                
                LocalNode node;
                if (existingNode != null)
                {
                    // Nó já existe - apenas atualizar propriedades do sistema
                    _logger.LogInformation("Nó existente encontrado para {MachineName}, atualizando propriedades do sistema", request.MachineName);
                    
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
                    _logger.LogInformation("Criando novo nó para {MachineName}", request.MachineName);
                    
                    node = new LocalNode
                    {
                        Id = Guid.NewGuid().ToString(),
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
                    message = existingNode != null ? "Nó local atualizado com sucesso" : "Nó local criado com sucesso"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar nó local: {MachineName}", request.MachineName);
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
    }

    // DTOs para as requisições
    public class SaveLocalNodeRequest
    {
        public string MachineName { get; set; } = string.Empty;
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
}
