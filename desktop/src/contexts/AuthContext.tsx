import React, { createContext, useContext } from 'react'
import type { ReactNode } from 'react'
import { useAuth } from '../hooks/useAuth'

interface AuthContextType {
  isAuthenticated: boolean
  user: any | null
  token: string | null
  loading: boolean
  login: (email: string, password: string) => Promise<{ success: boolean; error?: string }>
  register: (name: string, email: string, password: string) => Promise<{ success: boolean; error?: string }>
  logout: () => Promise<void>
  bindAnonymousNode: (anonymousToken: string) => Promise<{ success: boolean; error?: string }>
  refreshProfile: () => Promise<void>
  getUserNodes: (token: string) => Promise<{ success: boolean; data?: any; error?: string }>
  getAvailableNodes: () => Promise<{ success: boolean; data?: any; error?: string }>
  checkNodeConnection: (data: { token: string; machineId: string }) => Promise<{ success: boolean; isConnected?: boolean; node?: any; wasUnbound?: boolean; error?: string }>
  bindCurrentNode: (data: { token: string; machineId: string }) => Promise<{ success: boolean; data?: any; error?: string }>
  unbindNode: (data: { token: string; nodeId: string }) => Promise<{ success: boolean; data?: any; error?: string }>
}

const AuthContext = createContext<AuthContextType | undefined>(undefined)

interface AuthProviderProps {
  children: ReactNode
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const auth = useAuth()

  return (
    <AuthContext.Provider value={auth}>
      {children}
    </AuthContext.Provider>
  )
}

export const useAuthContext = (): AuthContextType => {
  const context = useContext(AuthContext)
  if (context === undefined) {
    throw new Error('useAuthContext must be used within an AuthProvider')
  }
  return context
}
