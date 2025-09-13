import { useState, useEffect, useCallback } from 'react'
import { localStorageService } from '../services/LocalStorageService'

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
        // Obter token do SQLite via API
        const token = await localStorageService.getToken();
        const userData = localStorage.getItem('user_data');

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
            await localStorageService.clearToken();
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
        await localStorageService.clearToken();
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
        
        // Armazenar no SQLite via API e localStorage como backup
        await localStorageService.saveToken(token, user.id, user.email);
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
        
        // Armazenar no SQLite via API e localStorage como backup
        await localStorageService.saveToken(token, user.id, user.email);
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
      await localStorageService.clearToken();
      
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
      console.log('🔍 Verificando conexão do nó na API LOCAL...')
      
      // Chamar a API LOCAL
      const result = await window.auth.checkNodeConnection(data)
      console.log('📡 Resultado da API LOCAL:', result)
      
      return result
    } catch (error) {
      console.error('Erro ao verificar conexão do nó:', error)
      return { success: false, error: 'Erro ao verificar conexão do nó' }
    }
  }, [])

  const bindCurrentNode = useCallback(async (data: { token: string; machineId: string }) => {
    try {
      console.log('🚀 Iniciando bindCurrentNode com:', { token: data.token?.substring(0, 20) + '...', machineId: data.machineId })
      
      // Chamar a API local que vai comunicar com o servidor cloud
      const response = await fetch('http://localhost:8000/api/Auth/bind-current-node', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${data.token}`
        },
        body: JSON.stringify({
          machineId: data.machineId
        })
      })
      
      if (!response.ok) {
        // Se a resposta não é OK, tentar extrair a mensagem de erro específica
        const result = await response.json()
        console.log('🔍 Resposta de erro da API bind-current-node:', result)
        
        // Extrair a mensagem de erro de forma mais robusta
        let errorMessage = `Erro HTTP ${response.status}`
        
        if (result.message) {
          errorMessage = result.message
        } else if (result.error) {
          errorMessage = result.error
        } else if (typeof result === 'string') {
          errorMessage = result
        }
        
        console.error('❌ Erro da API:', errorMessage)
        console.error('❌ Resultado completo da API:', result)
        
        return { 
          success: false, 
          error: errorMessage,
          data: result
        }
      }
      
      const result = await response.json()
      console.log('🔍 Resposta da API bind-current-node:', result)
      
      return {
        success: result.success || false,
        data: result,
        error: result.success ? undefined : (result.message || `Erro da API: ${JSON.stringify(result)}`)
      }
    } catch (error) {
      console.error('Erro ao conectar nó atual:', error)
      // Verificar se é um erro de rede ou de parsing JSON
      if (error instanceof TypeError && error.message.includes('fetch')) {
        return { success: false, error: 'Erro de conexão com a API local - verifique se a API está rodando' }
      }
      return { success: false, error: 'Erro de conexão com a API local' }
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
