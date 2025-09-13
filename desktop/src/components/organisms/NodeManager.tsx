import { useEffect, useState, useCallback } from 'react'
import { useAuthContext } from '../../contexts/AuthContext'
import { Button } from '../atoms/Button'
import { CheckCircle, AlertCircle, Settings } from 'lucide-react'
import { useNavigate } from 'react-router-dom'

interface ConnectedNode {
  machineId: string
  nodeId: string
}

interface NodeInfo {
  id?: string
  machineId?: string
}

interface ApiResult {
  success: boolean
  isRunning?: boolean
  status?: { isRunning: boolean; port: number; processId?: number }
  error?: string
}

interface WindowWithApi {
  api?: {
    checkStatus: () => Promise<ApiResult>
    restart: () => Promise<ApiResult>
  }
  system?: {
    getNodeConfig: () => Promise<{ nodeId: string; machineId: string; machineName: string; alias?: string; createdAt: string }>
  }
  auth?: {
    checkNodeConnection: (data: { token: string; machineId: string }) => Promise<{ success: boolean; isConnected?: boolean; node?: NodeInfo; wasUnbound?: boolean; error?: string }>
  }
}

export function NodeManager() {
  const { isAuthenticated, token, bindCurrentNode, unbindNode } = useAuthContext()
  const navigate = useNavigate()
  const [nodeConfig, setNodeConfig] = useState<{
    nodeId: string
    machineId: string
    machineName: string
    alias?: string
    createdAt: string
  } | null>(null)
  const [isNodeConnected, setIsNodeConnected] = useState(false)
  const [connectedNodeId, setConnectedNodeId] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [actionLoading, setActionLoading] = useState(false)
  const [checkingConnection, setCheckingConnection] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [apiStatus, setApiStatus] = useState<{ isRunning: boolean; status?: ApiResult['status'] } | null>(null)

  useEffect(() => {
    loadNodeConfig()
    checkApiStatus()
  }, [])

  // Verificar localStorage quando nodeConfig for carregado
  useEffect(() => {
    if (nodeConfig && isAuthenticated && token) {
      const connectedNodes: ConnectedNode[] = JSON.parse(localStorage.getItem('connected_nodes') || '[]')
      const connectedNode = connectedNodes.find((node: ConnectedNode) => node.machineId === nodeConfig.machineId)
      
      if (connectedNode) {
        console.log('✅ Nó encontrado no localStorage como conectado:', connectedNode)
        setIsNodeConnected(true)
        setConnectedNodeId(connectedNode.nodeId)
      }
    }
  }, [nodeConfig, isAuthenticated, token])



  const checkApiStatus = async () => {
    try {
      const windowWithApi = window as unknown as WindowWithApi
      if (windowWithApi.api) {
        const result = await windowWithApi.api.checkStatus()
        if (result.success) {
          setApiStatus({ isRunning: result.isRunning || false, status: result.status })
        }
      }
    } catch (error) {
      console.error('Erro ao verificar status da API:', error)
    }
  }

  const loadNodeConfig = async () => {
    try {
      setLoading(true)
      if (window.system) {
        const cfg = await window.system.getNodeConfig()
        setNodeConfig(cfg)
      }
    } catch (error) {
      console.error('Erro ao carregar configuração do nó:', error)
    } finally {
      setLoading(false)
    }
  }

  const checkNodeConnection = useCallback(async (forceServerCheck = false) => {
    if (!nodeConfig || !token) return

    try {
      // Se for verificação forçada (botão clicado), mostrar loading
      if (forceServerCheck) {
        setCheckingConnection(true)
        // Não limpar erro aqui - apenas quando for uma nova tentativa de conectar
      }

      // Se não for verificação forçada, verificar primeiro no localStorage
      if (!forceServerCheck) {
        const connectedNodes: ConnectedNode[] = JSON.parse(localStorage.getItem('connected_nodes') || '[]')
        const connectedNode = connectedNodes.find((node: ConnectedNode) => node.machineId === nodeConfig.machineId)
        
        if (connectedNode) {
          console.log('✅ Nó encontrado no localStorage como conectado:', connectedNode)
          setIsNodeConnected(true)
          setConnectedNodeId(connectedNode.nodeId)
          return
        }
      }

      // PASSO 1: Enviar para API Local (FirebirdAPI)
      console.log('🔍 Verificando conexão no servidor...', forceServerCheck ? '(verificação forçada)' : '(nó não encontrado no localStorage)')
      
      // PASSO 2: API Local recebe o comando e envia para o Cloud (Server)
      // PASSO 3: Cloud lista os nodes por user logado e retorna para API Local
      const result = await window.auth.checkNodeConnection({
        token,
        machineId: nodeConfig.machineId
      })
      
      if (result.success) {
        console.log('📡 Resposta completa do servidor:', result)
        console.log('📡 Resposta do servidor (resumo):', { 
          isConnected: result.isConnected, 
          wasUnbound: result.wasUnbound, 
          hasNode: !!result.node,
          nodeId: (result.node as { id?: string })?.id || 'N/A'
        })
        
        const wasConnected = isNodeConnected
        const isNowConnected = result.isConnected || false
        
        // PASSO 4: API Local check se aquele nó está na lista
        setIsNodeConnected(isNowConnected)
        setConnectedNodeId((result.node as { id?: string })?.id || null)
        
        // PASSO 5: Caso estiver OK não faça nada (já atualizado acima)
        // PASSO 6: Caso não estiver, remova do localStorage para ficar sincronizado com o server
        if (isNowConnected && result.node) {
          // Nó está conectado - salvar/atualizar no localStorage
          const connectedNodes: ConnectedNode[] = JSON.parse(localStorage.getItem('connected_nodes') || '[]')
          const nodeInfo = result.node as NodeInfo
          const existingIndex = connectedNodes.findIndex((node: ConnectedNode) => node.machineId === nodeConfig.machineId)
          
          if (existingIndex >= 0) {
            connectedNodes[existingIndex] = { machineId: nodeConfig.machineId, nodeId: nodeInfo.id || '' }
          } else {
            connectedNodes.push({ machineId: nodeConfig.machineId, nodeId: nodeInfo.id || '' })
          }
          
          localStorage.setItem('connected_nodes', JSON.stringify(connectedNodes))
          console.log('💾 Nó salvo no localStorage:', { machineId: nodeConfig.machineId, nodeId: nodeInfo.id })
        } else if (!isNowConnected && !result.node) {
          // Nó não está na lista do servidor - remover do localStorage para sincronizar
          // Mas apenas se não houver erro de conexão com o cloud
          const hasCloudError = result.error && (
            result.error.includes('cloud') || 
            result.error.includes('Servidor cloud') ||
            result.error.includes('cloud não disponível')
          )
          
          if (!hasCloudError) {
            console.log('🚫 Nó não está na lista do servidor, removendo do localStorage para sincronizar...')
            const connectedNodes: ConnectedNode[] = JSON.parse(localStorage.getItem('connected_nodes') || '[]')
            const filteredNodes = connectedNodes.filter((node: ConnectedNode) => node.machineId !== nodeConfig.machineId)
            localStorage.setItem('connected_nodes', JSON.stringify(filteredNodes))
            console.log('🧹 Nó removido do localStorage para sincronizar com servidor:', { machineId: nodeConfig.machineId })
          } else {
            console.log('⚠️ Erro de conexão com cloud, mantendo estado atual no localStorage. Erro:', result.error)
            // Se há erro de cloud, manter o estado atual e mostrar aviso
            if (forceServerCheck) {
              setError('Servidor cloud não disponível. Estado mantido localmente.')
            }
          }
        }
        
        // Se o nó foi desvinculado remotamente, limpar o localStorage
        if (result.wasUnbound || (wasConnected && !isNowConnected)) {
          console.log('🚫 Nó foi desvinculado remotamente, limpando localStorage...', { 
            wasUnbound: result.wasUnbound, 
            wasConnected, 
            isNowConnected 
          })
          const connectedNodes: ConnectedNode[] = JSON.parse(localStorage.getItem('connected_nodes') || '[]')
          const filteredNodes = connectedNodes.filter((node: ConnectedNode) => node.machineId !== nodeConfig.machineId)
          localStorage.setItem('connected_nodes', JSON.stringify(filteredNodes))
          console.log('🧹 Nó removido do localStorage:', { machineId: nodeConfig.machineId })
        }
      } else {
        console.error('❌ Erro na verificação de conexão:', result.error)
        if (forceServerCheck) {
          setError(result.error || 'Erro ao verificar conexão')
        }
      }
    } catch (error) {
      console.error('Erro ao verificar conexão do nó:', error)
      if (forceServerCheck) {
        setError('Erro de conexão com o servidor')
      }
    } finally {
      if (forceServerCheck) {
        setCheckingConnection(false)
      }
    }
  }, [nodeConfig, token, isNodeConnected])

  // Verificação automática de conexão após carregar dados do nó
  useEffect(() => {
    if (nodeConfig && !loading) {
      console.log('🔄 Verificação automática de conexão do nó ao iniciar o app...')
      // Executar verificação após um pequeno delay para garantir que tudo foi carregado
      const timer = setTimeout(async () => {
        if (isAuthenticated && token) {
          // Usuário autenticado - tentar registro normal
          try {
            console.log('🚀 Tentando registrar nó automaticamente (usuário autenticado)...')
            const registerResult = await window.system.registerNode()
            if (registerResult.success) {
              console.log('✅ Nó registrado automaticamente:', registerResult.data)
            } else {
              console.warn('⚠️ Falha ao registrar nó automaticamente:', registerResult.error)
            }
          } catch (error) {
            console.error('❌ Erro ao registrar nó automaticamente:', error)
          }
          
          // Depois verificar conexão
          checkNodeConnection(true) // Forçar verificação no servidor
        } else {
          // Usuário não autenticado - verificar se já existe nó anônimo
          console.log('👤 Usuário não autenticado, verificando nó anônimo...')
          const anonymousToken = localStorage.getItem('anonymous_token')
          if (anonymousToken) {
            console.log('✅ Nó anônimo já registrado, token encontrado')
          } else {
            console.log('⚠️ Nenhum nó anônimo encontrado - o registro automático deve ter falhado')
          }
        }
      }, 2000) // 2 segundos de delay para garantir que a API esteja pronta
      
      return () => clearTimeout(timer)
    }
  }, [nodeConfig, isAuthenticated, token, loading, checkNodeConnection])

  // Verificação inicial quando o componente é montado - REMOVIDO para evitar desvinculação automática
  // useEffect(() => {
  //   if (nodeConfig && isAuthenticated && token) {
  //     console.log('🔄 Verificação inicial de conexão do nó...')
  //     checkNodeConnection(true) // Forçar verificação no servidor
  //   }
  // }, [nodeConfig, isAuthenticated, token, checkNodeConnection])

  // Verificação periódica a cada 30 segundos - REMOVIDO para evitar desvinculação automática
  // useEffect(() => {
  //   if (!nodeConfig || !isAuthenticated || !token) return

  //   console.log('⏰ Iniciando verificação periódica de conexão...')
  //   const interval = setInterval(() => {
  //     console.log('🔄 Verificação periódica de conexão do nó...')
  //     checkNodeConnection(true) // Forçar verificação no servidor
  //   }, 30000) // 30 segundos

  //   return () => {
  //     console.log('⏹️ Parando verificação periódica de conexão...')
  //     clearInterval(interval)
  //   }
  // }, [nodeConfig, isAuthenticated, token, checkNodeConnection])

  // Verificação quando a janela ganha foco - REMOVIDO para evitar desvinculação automática
  // useEffect(() => {
  //   if (!nodeConfig || !isAuthenticated || !token) return

  //   const handleFocus = () => {
  //     console.log('👁️ Janela ganhou foco, verificando conexão do nó...')
  //     checkNodeConnection(true) // Forçar verificação no servidor
  //   }

  //   window.addEventListener('focus', handleFocus)
    
  //   return () => {
  //     window.removeEventListener('focus', handleFocus)
  //   }
  // }, [nodeConfig, isAuthenticated, token, checkNodeConnection])

  // Limpar estado do nó quando usuário faz logout
  useEffect(() => {
    if (!isAuthenticated) {
      setIsNodeConnected(false)
      setConnectedNodeId(null)
      // Limpar localStorage dos nós conectados
      localStorage.removeItem('connected_nodes')
    }
  }, [isAuthenticated])

  const handleConnectNode = async () => {
    console.log('🔗 handleConnectNode chamado', { nodeConfig, token, isAuthenticated })
    
    if (!nodeConfig || !token) {
      console.log('❌ Falta nodeConfig ou token:', { nodeConfig: !!nodeConfig, token: !!token })
      return
    }

    try {
      setActionLoading(true)
      setError(null)
      
      // PASSO 7: Se clicar em Conectar nó na conta -> Bind
      console.log('📡 PASSO 7: Conectando nó na conta (Bind)...', { token, machineId: nodeConfig.machineId })
      const result = await bindCurrentNode({
        token,
        machineId: nodeConfig.machineId
      })
      
      console.log('📡 Resultado do bindCurrentNode:', result)
      
      if (result.success) {
        // Salvar no localStorage que o nó está conectado
        const connectedNodes: ConnectedNode[] = JSON.parse(localStorage.getItem('connected_nodes') || '[]')
        const existingIndex = connectedNodes.findIndex((node: ConnectedNode) => node.machineId === nodeConfig.machineId)
        
        const nodeData = { 
          machineId: nodeConfig.machineId, 
          nodeId: result.data.nodeId || nodeConfig.nodeId 
        }
        
        if (existingIndex >= 0) {
          connectedNodes[existingIndex] = nodeData
        } else {
          connectedNodes.push(nodeData)
        }
        
        localStorage.setItem('connected_nodes', JSON.stringify(connectedNodes))
        console.log('💾 Nó conectado salvo no localStorage:', nodeData)
        
        // Atualizar estado local
        setIsNodeConnected(true)
        setConnectedNodeId(nodeData.nodeId)
      } else {
        // Exibir o erro específico retornado pela API
        const errorMessage = result.error || 'Erro ao conectar nó'
        console.error('❌ Erro específico da API:', errorMessage)
        console.error('❌ Resultado completo:', result)
        console.error('❌ Tipo do erro:', typeof errorMessage)
        console.error('❌ Conteúdo do erro:', JSON.stringify(result))
        setError(errorMessage)
      }
    } catch (error) {
      console.error('Erro ao conectar nó:', error)
      setError('Erro de conexão com o servidor')
    } finally {
      setActionLoading(false)
    }
  }

  const handleDisconnectNode = async () => {
    if (!connectedNodeId || !token) return

    try {
      setActionLoading(true)
      setError(null)
      
      // PASSO 8: Se clicar em desconectar conta -> unbind
      console.log('📡 PASSO 8: Desconectando nó da conta (Unbind)...', { token, nodeId: connectedNodeId })
      const result = await unbindNode({
        token,
        nodeId: connectedNodeId
      })
      
      if (result.success) {
        // Remover do localStorage
        const connectedNodes: ConnectedNode[] = JSON.parse(localStorage.getItem('connected_nodes') || '[]')
        const filteredNodes = connectedNodes.filter((node: ConnectedNode) => node.machineId !== nodeConfig?.machineId)
        localStorage.setItem('connected_nodes', JSON.stringify(filteredNodes))
        console.log('🗑️ Nó removido do localStorage:', nodeConfig?.machineId)
        
        // Atualizar estado local
        setIsNodeConnected(false)
        setConnectedNodeId(null)
      } else {
        setError(result.error || 'Erro ao desconectar nó')
      }
    } catch (error) {
      console.error('Erro ao desconectar nó:', error)
      setError('Erro de conexão com o servidor')
    } finally {
      setActionLoading(false)
    }
  }

  const getStatusIcon = () => {
    return isNodeConnected ? (
      <CheckCircle className="w-5 h-5 text-green-500" />
    ) : (
      <AlertCircle className="w-5 h-5 text-gray-400" />
    )
  }

  const getStatusText = () => {
    return isNodeConnected ? 'Conectado' : 'Desconectado'
  }

  const getStatusColor = () => {
    return isNodeConnected ? 'text-green-600 bg-green-50' : 'text-gray-600 bg-gray-50'
  }

  if (loading) {
    return (
      <div 
        className="bg-white rounded-lg border border-gray-200 p-6 cursor-pointer hover:shadow-md transition-shadow"
        onClick={() => navigate('/settings')}
      >
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-lg font-semibold text-gray-900">Status do Nó</h3>
          <button
            onClick={(e) => {
              e.stopPropagation()
              navigate('/settings')
            }}
            className="p-2 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded-lg transition-colors"
            title="Configurações"
          >
            <Settings size={20} />
          </button>
        </div>
        <p className="text-gray-500">Carregando informações...</p>
      </div>
    )
  }

  if (!nodeConfig) {
    return (
      <div 
        className="bg-white rounded-lg border border-gray-200 p-6 cursor-pointer hover:shadow-md transition-shadow"
        onClick={() => navigate('/settings')}
      >
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-lg font-semibold text-gray-900">Status do Nó</h3>
          <button
            onClick={(e) => {
              e.stopPropagation()
              navigate('/settings')
            }}
            className="p-2 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded-lg transition-colors"
            title="Configurações"
          >
            <Settings size={20} />
          </button>
        </div>
        <p className="text-gray-500">Erro ao carregar configuração do nó.</p>
      </div>
    )
  }

  return (
    <div 
      className="bg-white rounded-lg border border-gray-200 p-6 cursor-pointer hover:shadow-md transition-shadow"
      onClick={() => navigate('/settings')}
    >
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-lg font-semibold text-gray-900">Status do Nó</h3>
        <button
          onClick={(e) => {
            e.stopPropagation()
            navigate('/settings')
          }}
          className="p-2 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded-lg transition-colors"
          title="Configurações"
        >
          <Settings size={20} />
        </button>
      </div>
      
      <div className="space-y-4">
        {/* Status de Conexão */}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            {getStatusIcon()}
            <div>
              <h4 className="font-medium text-gray-900">
                {nodeConfig.machineName}{nodeConfig.alias ? ` (${nodeConfig.alias})` : ''}
              </h4>
              <p className="text-sm text-gray-500">
                {nodeConfig.machineId}
              </p>
            </div>
          </div>
          <span className={`px-2 py-1 rounded-full text-xs font-medium ${getStatusColor()}`}>
            {getStatusText()}
          </span>
        </div>

        {/* Informações do Nó */}
        <div className="border-t border-gray-200 pt-4">
          <div className="text-xs text-gray-500 mb-2">Node ID: {nodeConfig.nodeId}</div>
          <div className="text-xs text-gray-500 mb-2">
            Criado em: {new Date(nodeConfig.createdAt).toLocaleDateString('pt-BR')}
          </div>
          <div className="text-xs text-gray-500 mb-2">Machine ID: {nodeConfig.machineId}</div>
          
          {/* Status da API */}
          {apiStatus && (
            <div className="text-xs text-gray-500 mb-2">
              API Status: {apiStatus.isRunning ? (
                <span className="text-green-600">✅ Rodando</span>
              ) : (
                <span className="text-red-600">❌ Parada</span>
              )}
            </div>
          )}
        </div>

        {/* Botão de Verificação Manual */}
        <div className="flex justify-center">
          <button
            onClick={() => checkNodeConnection(true)}
            disabled={checkingConnection}
            className="bg-blue-100 text-blue-700 px-3 py-1 rounded text-sm hover:bg-blue-200 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
          >
            {checkingConnection ? (
              <>
                <div className="w-3 h-3 border-2 border-blue-700 border-t-transparent rounded-full animate-spin"></div>
                Verificando...
              </>
            ) : (
              <>
                🔄 Verificar Conexão
              </>
            )}
          </button>
        </div>

        {/* Mensagem de Status e Ações */}
        {isNodeConnected ? (
          <div className="space-y-3">
            <div className="bg-green-50 border border-green-200 text-green-700 px-3 py-2 rounded text-sm">
              Este nó está conectado à sua conta no cloud.
            </div>
            <Button
              onClick={handleDisconnectNode}
              disabled={actionLoading}
              className="w-full bg-red-600 hover:bg-red-700 text-white"
            >
              {actionLoading ? 'Desconectando...' : 'Desconectar'}
            </Button>
          </div>
        ) : isAuthenticated ? (
          <div className="space-y-3">
            <div className="bg-orange-50 border border-orange-200 text-orange-700 px-3 py-2 rounded text-sm">
              Este nó não está conectado à sua conta.
            </div>
            
            {/* Botão para verificar/reiniciar API se não estiver rodando */}
            {apiStatus && !apiStatus.isRunning && (
              <div className="space-y-2">
                <div className="bg-red-50 border border-red-200 text-red-700 px-3 py-2 rounded text-sm">
                  ⚠️ API não está rodando. Clique em "Reiniciar API" e tente novamente.
                </div>
                <Button
                  onClick={async () => {
                    try {
                      setActionLoading(true)
                      const windowWithApi = window as unknown as WindowWithApi
                      if (windowWithApi.api) {
                        const result = await windowWithApi.api.restart()
                        if (result.success) {
                          await checkApiStatus()
                          setError(null)
                        } else {
                          setError(result.error || 'Erro ao reiniciar API')
                        }
                      }
                    } catch {
                      setError('Erro ao reiniciar API')
                    } finally {
                      setActionLoading(false)
                    }
                  }}
                  disabled={actionLoading}
                  className="w-full bg-orange-600 hover:bg-orange-700 text-white"
                >
                  {actionLoading ? 'Reiniciando API...' : 'Reiniciar API'}
                </Button>
              </div>
            )}
            
            <Button
              onClick={() => {
                console.log('🖱️ Botão clicado!')
                handleConnectNode()
              }}
              disabled={actionLoading || (apiStatus ? !apiStatus.isRunning : false)}
              className="w-full bg-blue-600 hover:bg-blue-700 text-white disabled:bg-gray-400"
            >
              {actionLoading ? 'Conectando...' : 'Conectar este nó na conta'}
            </Button>
          </div>
        ) : (
          <div className="bg-gray-50 border border-gray-200 text-gray-600 px-3 py-2 rounded text-sm">
            Faça login para conectar este nó à sua conta no cloud.
          </div>
        )}

        {/* Mensagem de Erro */}
        {error && (
          <div className={`px-4 py-3 rounded-lg text-sm border-l-4 ${
            error.includes('já está vinculado') || error.includes('Nó já está vinculado') || error.includes('outro usuário')
              ? 'bg-orange-50 border-orange-400 text-orange-800'
              : error.includes('conexão') || error.includes('API')
              ? 'bg-red-50 border-red-400 text-red-800'
              : 'bg-red-50 border-red-400 text-red-800'
          }`}>
            <div className="flex items-start gap-3">
              <span className="text-xl flex-shrink-0 mt-0.5">
                {error.includes('já está vinculado') || error.includes('Nó já está vinculado') || error.includes('outro usuário') ? '⚠️' : '❌'}
              </span>
              <div className="flex-1">
                <div className="font-semibold mb-1">
                  {error.includes('já está vinculado') || error.includes('Nó já está vinculado') || error.includes('outro usuário')
                    ? 'Nó já vinculado a outra conta' 
                    : 'Erro de conexão'}
                </div>
                <div className="text-sm leading-relaxed">
                  {error.includes('já está vinculado') || error.includes('Nó já está vinculado') || error.includes('outro usuário')
                    ? 'Este nó já está conectado a outra conta de usuário. Para conectar este nó à sua conta, você precisará primeiro desconectá-lo da conta anterior.'
                    : error}
                </div>
                {(error.includes('já está vinculado') || error.includes('Nó já está vinculado') || error.includes('outro usuário')) && (
                  <div className="mt-3 p-2 bg-orange-100 rounded text-xs text-orange-700">
                    <strong>Solução:</strong> Entre em contato com o suporte técnico para obter ajuda na transferência do nó para sua conta.
                  </div>
                )}
                <button
                  onClick={() => setError(null)}
                  className="mt-2 text-xs text-gray-500 hover:text-gray-700 underline"
                >
                  Fechar
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
