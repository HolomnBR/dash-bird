// import { api } from '../api/http'

export interface LocalUserRegisterRequest {
  name: string
  email: string
  password: string
}

export interface LocalUserLoginRequest {
  email: string
  password: string
}

export interface LocalUserResponse {
  id: string
  name: string
  email: string
  createdAt: string
  lastLoginAt?: string
  isActive: boolean
}

export interface LocalAuthResponse {
  token: string
  user: LocalUserResponse
  expiresAt: string
}

export interface AnonymousNodeRegisterRequest {
  name: string
  machineId: string
  ipAddress: string
  port: number
  databasePath?: string
  version?: string
  operatingSystem?: string
}

export interface AnonymousNodeResponse {
  id: string
  name: string
  machineId: string
  ipAddress: string
  port: number
  databasePath?: string
  version?: string
  operatingSystem?: string
  isAnonymous: boolean
  anonymousToken: string
  anonymousExpiresAt: string
  createdAt: string
  lastSeen: string
}

export interface BindNodeToUserRequest {
  anonymousToken: string
}

export interface BindNodeToUserResponse {
  nodeId: string
  userId: string
  message: string
  success: boolean
}

export class AuthService {
  private readonly localApiUrl: string
  private authToken: string | null = null
  private user: LocalUserResponse | null = null

  constructor(localApiUrl: string = 'http://localhost:8000') {
    this.localApiUrl = localApiUrl
    this.loadStoredAuth()
  }

  /**
   * Registra um novo usuário
   */
  async register(request: LocalUserRegisterRequest): Promise<LocalAuthResponse> {
    try {
      const response = await fetch(`${this.localApiUrl}/api/Auth/register`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request)
      })

      if (!response.ok) {
        const errorData = await response.json()
        throw new Error(errorData.message || 'Erro ao registrar usuário')
      }

      const result: LocalAuthResponse = await response.json()
      this.setAuth(result.token, result.user)
      return result
    } catch (error) {
      console.error('Erro ao registrar usuário:', error)
      throw error
    }
  }

  /**
   * Faz login do usuário
   */
  async login(request: LocalUserLoginRequest): Promise<LocalAuthResponse> {
    try {
      const response = await fetch(`${this.localApiUrl}/api/Auth/login`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request)
      })

      if (!response.ok) {
        const errorData = await response.json()
        throw new Error(errorData.message || 'Email ou senha inválidos')
      }

      const result: LocalAuthResponse = await response.json()
      this.setAuth(result.token, result.user)
      return result
    } catch (error) {
      console.error('Erro ao fazer login:', error)
      throw error
    }
  }

  /**
   * Faz logout do usuário
   */
  logout(): void {
    this.authToken = null
    this.user = null
    this.clearStoredAuth()
  }

  /**
   * Obtém o perfil do usuário autenticado
   */
  async getProfile(): Promise<LocalUserResponse | null> {
    try {
      if (!this.authToken) {
        return null
      }

      const response = await fetch(`${this.localApiUrl}/api/Auth/profile`, {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${this.authToken}`,
        }
      })

      if (!response.ok) {
        if (response.status === 401) {
          this.logout()
          return null
        }
        throw new Error('Erro ao obter perfil do usuário')
      }

      const result: LocalUserResponse = await response.json()
      this.user = result
      return result
    } catch (error) {
      console.error('Erro ao obter perfil:', error)
      return null
    }
  }

  /**
   * Valida o token de autenticação
   */
  async validateToken(): Promise<boolean> {
    try {
      if (!this.authToken) {
        return false
      }

      const response = await fetch(`${this.localApiUrl}/api/Auth/validate-token`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${this.authToken}`,
        }
      })

      if (!response.ok) {
        this.logout()
        return false
      }

      const result = await response.json()
      return result.valid === true
    } catch (error) {
      console.error('Erro ao validar token:', error)
      this.logout()
      return false
    }
  }

  /**
   * Registra um nó anônimo
   */
  async registerAnonymousNode(request: AnonymousNodeRegisterRequest): Promise<AnonymousNodeResponse> {
    try {
      const response = await fetch(`${this.localApiUrl}/api/Auth/anonymous/register`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request)
      })

      if (!response.ok) {
        const errorData = await response.json()
        throw new Error(errorData.message || 'Erro ao registrar nó anônimo')
      }

      const result: AnonymousNodeResponse = await response.json()
      return result
    } catch (error) {
      console.error('Erro ao registrar nó anônimo:', error)
      throw error
    }
  }

  /**
   * Obtém informações de um nó anônimo
   */
  async getAnonymousNodeInfo(token: string): Promise<AnonymousNodeResponse | null> {
    try {
      const response = await fetch(`${this.localApiUrl}/api/Auth/anonymous/info/${token}`, {
        method: 'GET'
      })

      if (!response.ok) {
        return null
      }

      const result: AnonymousNodeResponse = await response.json()
      return result
    } catch (error) {
      console.error('Erro ao obter informações do nó anônimo:', error)
      return null
    }
  }

  /**
   * Valida um token anônimo
   */
  async validateAnonymousToken(token: string): Promise<boolean> {
    try {
      const response = await fetch(`${this.localApiUrl}/api/Auth/anonymous/validate/${token}`, {
        method: 'POST'
      })

      if (!response.ok) {
        return false
      }

      const result = await response.json()
      return result.valid === true
    } catch (error) {
      console.error('Erro ao validar token anônimo:', error)
      return false
    }
  }

  /**
   * Vincula um nó anônimo ao usuário autenticado
   */
  async bindNodeToUser(request: BindNodeToUserRequest): Promise<BindNodeToUserResponse> {
    try {
      if (!this.authToken) {
        throw new Error('Usuário não autenticado')
      }

      const response = await fetch(`${this.localApiUrl}/api/Auth/bind-node`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${this.authToken}`,
        },
        body: JSON.stringify(request)
      })

      if (!response.ok) {
        const errorData = await response.json()
        throw new Error(errorData.message || 'Erro ao vincular nó ao usuário')
      }

      const result: BindNodeToUserResponse = await response.json()
      return result
    } catch (error) {
      console.error('Erro ao vincular nó ao usuário:', error)
      throw error
    }
  }

  /**
   * Verifica se o usuário está autenticado
   */
  isAuthenticated(): boolean {
    return this.authToken !== null && this.user !== null
  }

  /**
   * Obtém o token de autenticação
   */
  getToken(): string | null {
    return this.authToken
  }

  /**
   * Obtém os dados do usuário
   */
  getUser(): LocalUserResponse | null {
    return this.user
  }

  /**
   * Define a autenticação e armazena localmente
   */
  private setAuth(token: string, user: LocalUserResponse): void {
    this.authToken = token
    this.user = user
    this.storeAuth(token, user)
  }

  /**
   * Armazena a autenticação no localStorage
   */
  private storeAuth(token: string, user: LocalUserResponse): void {
    try {
      localStorage.setItem('auth_token', token)
      localStorage.setItem('user_data', JSON.stringify(user))
    } catch (error) {
      console.error('Erro ao armazenar autenticação:', error)
    }
  }

  /**
   * Carrega a autenticação armazenada
   */
  private loadStoredAuth(): void {
    try {
      const token = localStorage.getItem('auth_token')
      const userData = localStorage.getItem('user_data')

      if (token && userData) {
        this.authToken = token
        this.user = JSON.parse(userData)
      }
    } catch (error) {
      console.error('Erro ao carregar autenticação armazenada:', error)
      this.clearStoredAuth()
    }
  }

  /**
   * Limpa a autenticação armazenada
   */
  private clearStoredAuth(): void {
    try {
      localStorage.removeItem('auth_token')
      localStorage.removeItem('user_data')
    } catch (error) {
      console.error('Erro ao limpar autenticação armazenada:', error)
    }
  }
}

// Instância singleton
export const authService = new AuthService()
