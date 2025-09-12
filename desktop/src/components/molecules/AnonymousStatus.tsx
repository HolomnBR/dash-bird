import React, { useState, useEffect } from 'react'
import { useAuthContext } from '../../contexts/AuthContext'

interface AnonymousNodeInfo {
  id: string
  name: string
  machineId: string
  anonymousToken: string
  anonymousExpiresAt: string
  createdAt: string
}

export const AnonymousStatus: React.FC = () => {
  const { isAuthenticated, bindAnonymousNode } = useAuthContext()
  const [anonymousNode, setAnonymousNode] = useState<AnonymousNodeInfo | null>(null)
  const [loading, setLoading] = useState(true)
  const [showBindModal, setShowBindModal] = useState(false)

  useEffect(() => {
    loadAnonymousNodeInfo()
  }, [isAuthenticated])

  // Limpar estado quando usuário faz logout
  useEffect(() => {
    if (!isAuthenticated) {
      setAnonymousNode(null)
      setShowBindModal(false)
    }
  }, [isAuthenticated])

  const loadAnonymousNodeInfo = async () => {
    try {
      const storedToken = localStorage.getItem('anonymous_token')
      const storedNodeId = localStorage.getItem('anonymous_node_id')
      
      if (storedToken && storedNodeId && !isAuthenticated) {
        // Obter informações reais do nó anônimo via API
        try {
          const response = await fetch('http://localhost:8000/api/Auth/anonymous/info/' + storedToken)
          if (response.ok) {
            const nodeInfo = await response.json()
            setAnonymousNode({
              id: nodeInfo.id,
              name: nodeInfo.name,
              machineId: nodeInfo.machineId,
              anonymousToken: storedToken,
              anonymousExpiresAt: nodeInfo.anonymousExpiresAt,
              createdAt: nodeInfo.createdAt
            })
          } else {
            // Se não conseguir obter info da API, usar dados básicos
            setAnonymousNode({
              id: storedNodeId,
              name: 'Nó Anônimo',
              machineId: 'unknown',
              anonymousToken: storedToken,
              anonymousExpiresAt: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString(), // 30 dias
              createdAt: new Date().toISOString()
            })
          }
        } catch (apiError) {
          console.warn('Erro ao obter informações do nó anônimo via API, usando dados básicos:', apiError)
          // Fallback para dados básicos
          setAnonymousNode({
            id: storedNodeId || 'unknown',
            name: 'Nó Anônimo',
            machineId: 'unknown',
            anonymousToken: storedToken,
            anonymousExpiresAt: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString(), // 30 dias
            createdAt: new Date().toISOString()
          })
        }
      }
    } catch (error) {
      console.error('Erro ao carregar informações do nó anônimo:', error)
    } finally {
      setLoading(false)
    }
  }

  const handleBindToUser = async () => {
    if (!anonymousNode) return

    try {
      const result = await bindAnonymousNode(anonymousNode.anonymousToken)
      if (result.success) {
        setShowBindModal(false)
        setAnonymousNode(null)
        localStorage.removeItem('anonymous_token')
        // Mostrar mensagem de sucesso
        alert('Nó vinculado ao usuário com sucesso!')
      } else {
        alert(`Erro ao vincular nó: ${result.error}`)
      }
    } catch (error) {
      console.error('Erro ao vincular nó:', error)
      alert('Erro ao vincular nó ao usuário')
    }
  }

  if (loading) {
    return (
      <div className="flex items-center space-x-2 text-sm text-gray-500">
        <div className="w-2 h-2 bg-gray-300 rounded-full animate-pulse"></div>
        <span>Carregando...</span>
      </div>
    )
  }

  if (!anonymousNode) {
    return null
  }

  const isExpired = new Date(anonymousNode.anonymousExpiresAt) < new Date()
  const daysUntilExpiry = Math.ceil((new Date(anonymousNode.anonymousExpiresAt).getTime() - Date.now()) / (1000 * 60 * 60 * 24))

  return (
    <div className="flex items-center space-x-2">
      <div className={`w-2 h-2 rounded-full ${isExpired ? 'bg-red-500' : 'bg-green-500'}`}></div>
      
      <div className="text-sm">
        <div className="text-gray-700">
          {isAuthenticated ? 'Conectado como usuário' : 'Modo anônimo'}
        </div>
        {!isAuthenticated && (
          <div className="text-xs text-gray-500">
            {isExpired ? 'Token expirado' : `${daysUntilExpiry} dias restantes`}
          </div>
        )}
      </div>

      {isAuthenticated && anonymousNode && (
        <button
          onClick={() => setShowBindModal(true)}
          className="text-xs bg-blue-100 text-blue-700 px-2 py-1 rounded hover:bg-blue-200 transition-colors"
        >
          Vincular
        </button>
      )}

      {/* Modal de confirmação para vincular nó */}
      {showBindModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 w-full max-w-md mx-4">
            <h3 className="text-lg font-semibold mb-4">Vincular Nó Anônimo</h3>
            <p className="text-gray-600 mb-4">
              Deseja vincular este nó anônimo à sua conta? Isso permitirá sincronizar seus dados.
            </p>
            <div className="flex space-x-3">
              <button
                onClick={() => setShowBindModal(false)}
                className="flex-1 px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
              >
                Cancelar
              </button>
              <button
                onClick={handleBindToUser}
                className="flex-1 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
              >
                Vincular
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
