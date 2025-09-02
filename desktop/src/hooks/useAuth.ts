import { useState, useEffect, useCallback } from 'react'

interface User {
  id: string
  name: string
  email: string
  createdAt: string
  lastLoginAt?: string
  isActive: boolean
}

interface AuthState {
  isAuthenticated: boolean
  user: User | null
  token: string | null
  loading: boolean
}

export const useAuth = () => {
  const [authState, setAuthState] = useState<AuthState>({
    isAuthenticated: false,
    user: null,
    token: null,
    loading: true
  })

  // Carregar autenticação armazenada ao inicializar
  useEffect(() => {
    const loadStoredAuth = async () => {
      try {
        const token = localStorage.getItem('auth_token')
        const userData = localStorage.getItem('user_data')

        if (token && userData) {
          // Validar token
          const validation = await window.auth.validateToken(token)
          if (validation.success && validation.valid) {
            setAuthState({
              isAuthenticated: true,
              user: JSON.parse(userData),
              token,
              loading: false
            })
          } else {
            // Token inválido, limpar todos os dados
            localStorage.removeItem('auth_token')
            localStorage.removeItem('user_data')
            localStorage.removeItem('connected_nodes')
            localStorage.removeItem('anonymous_token')
            setAuthState({
              isAuthenticated: false,
              user: null,
              token: null,
              loading: false
            })
          }
        } else {
          setAuthState(prev => ({ ...prev, loading: false }))
        }
      } catch (error) {
        console.error('Erro ao carregar autenticação:', error)
        // Limpar todos os dados em caso de erro
        localStorage.removeItem('auth_token')
        localStorage.removeItem('user_data')
        localStorage.removeItem('connected_nodes')
        localStorage.removeItem('anonymous_token')
        setAuthState({
          isAuthenticated: false,
          user: null,
          token: null,
          loading: false
        })
      }
    }

    loadStoredAuth()
  }, [])

  const login = useCallback(async (email: string, password: string) => {
    try {
      const result = await window.auth.login({ email, password })
      
      if (result.success) {
        const { token, user } = result.data as { token: string; user: User }
        
        // Armazenar no localStorage
        localStorage.setItem('auth_token', token)
        localStorage.setItem('user_data', JSON.stringify(user))
        
        setAuthState({
          isAuthenticated: true,
          user,
          token,
          loading: false
        })
        
        return { success: true }
      } else {
        return { success: false, error: result.error }
      }
    } catch (error) {
      console.error('Erro no login:', error)
      return { success: false, error: 'Erro de conexão' }
    }
  }, [])

  const register = useCallback(async (name: string, email: string, password: string) => {
    try {
      const result = await window.auth.register({ name, email, password })
      
      if (result.success) {
        const { token, user } = result.data as { token: string; user: User }
        
        // Armazenar no localStorage
        localStorage.setItem('auth_token', token)
        localStorage.setItem('user_data', JSON.stringify(user))
        
        setAuthState({
          isAuthenticated: true,
          user,
          token,
          loading: false
        })
        
        return { success: true }
      } else {
        return { success: false, error: result.error }
      }
    } catch (error) {
      console.error('Erro no registro:', error)
      return { success: false, error: 'Erro de conexão' }
    }
  }, [])

  const logout = useCallback(async () => {
    try {
      // Notificar a API para fazer logout e unbind dos nós
      if (authState.token) {
        await window.auth.logout(authState.token)
      }
    } catch (error) {
      console.error('Erro no logout:', error)
    } finally {
      // Limpar todos os dados locais relacionados à autenticação
      localStorage.removeItem('auth_token')
      localStorage.removeItem('user_data')
      localStorage.removeItem('connected_nodes') // Limpar nós conectados
      localStorage.removeItem('anonymous_token') // Limpar token anônimo se existir
      
      // Atualizar estado imediatamente
      setAuthState({
        isAuthenticated: false,
        user: null,
        token: null,
        loading: false
      })
    }
  }, [authState.token])

  const bindAnonymousNode = useCallback(async (anonymousToken: string) => {
    if (!authState.token) {
      return { success: false, error: 'Usuário não autenticado' }
    }

    try {
      const result = await window.auth.bindNode({
        token: authState.token,
        anonymousToken
      })
      
      return result
    } catch (error) {
      console.error('Erro ao vincular nó anônimo:', error)
      return { success: false, error: 'Erro de conexão' }
    }
  }, [authState.token])

  const refreshProfile = useCallback(async () => {
    if (!authState.token) return

    try {
      const result = await window.auth.getProfile(authState.token)
      
      if (result.success) {
        const user = result.data as User
        localStorage.setItem('user_data', JSON.stringify(user))
        setAuthState(prev => ({ ...prev, user }))
      }
    } catch (error) {
      console.error('Erro ao atualizar perfil:', error)
    }
  }, [authState.token])

  const getUserNodes = useCallback(async (token: string) => {
    try {
      const result = await window.auth.getUserNodes(token)
      return result
    } catch (error) {
      console.error('Erro ao obter nós do usuário:', error)
      return { success: false, error: 'Erro de conexão' }
    }
  }, [])

  const getAvailableNodes = useCallback(async () => {
    try {
      const result = await window.auth.getAvailableNodes()
      return result
    } catch (error) {
      console.error('Erro ao obter nós disponíveis:', error)
      return { success: false, error: 'Erro de conexão' }
    }
  }, [])

  const checkNodeConnection = useCallback(async (data: { token: string; machineId: string }) => {
    try {
      const result = await window.auth.checkNodeConnection(data)
      return result
    } catch (error) {
      console.error('Erro ao verificar conexão do nó:', error)
      return { success: false, error: 'Erro de conexão' }
    }
  }, [])

  const bindCurrentNode = useCallback(async (data: { token: string; machineId: string }) => {
    try {
      const result = await window.auth.bindCurrentNode(data)
      return result
    } catch (error) {
      console.error('Erro ao conectar nó atual:', error)
      return { success: false, error: 'Erro de conexão' }
    }
  }, [])

  const unbindNode = useCallback(async (data: { token: string; nodeId: string }) => {
    try {
      const result = await window.auth.unbindNode(data)
      return result
    } catch (error) {
      console.error('Erro ao desconectar nó:', error)
      return { success: false, error: 'Erro de conexão' }
    }
  }, [])

  return {
    ...authState,
    login,
    register,
    logout,
    bindAnonymousNode,
    refreshProfile,
    getUserNodes,
    getAvailableNodes,
    checkNodeConnection,
    bindCurrentNode,
    unbindNode
  }
}
