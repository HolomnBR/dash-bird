/// <reference types="vite/client" />

interface SystemInfo {
  deviceName: string
  machineId: string
}

declare interface Window {
  env: { isElectron: boolean }
  system: {
    getInfo: () => Promise<SystemInfo>
    getNodeConfig: () => Promise<{
      nodeId: string
      machineId: string
      machineName: string
      alias?: string
      createdAt: string
    }>
    setAlias: (alias: string) => Promise<{
      nodeId: string
      machineId: string
      machineName: string
      alias?: string
      createdAt: string
    }>
    openFile: (filters?: Array<{ name: string; extensions: string[] }>) => Promise<string[]>
  }
  auth: {
    register: (userData: { name: string; email: string; password: string }) => Promise<{ success: boolean; data?: unknown; error?: string }>
    login: (credentials: { email: string; password: string }) => Promise<{ success: boolean; data?: unknown; error?: string }>
    logout: (token: string) => Promise<{ success: boolean; data?: unknown; error?: string }>
    getProfile: (token: string) => Promise<{ success: boolean; data?: unknown; error?: string }>
    validateToken: (token: string) => Promise<{ success: boolean; valid?: boolean; error?: string }>
    bindNode: (data: { token: string; anonymousToken: string }) => Promise<{ success: boolean; data?: unknown; error?: string }>
    getUserNodes: (token: string) => Promise<{ success: boolean; data?: unknown; error?: string }>
    getAvailableNodes: () => Promise<{ success: boolean; data?: unknown; error?: string }>
    checkNodeConnection: (data: { token: string; machineId: string }) => Promise<{ success: boolean; isConnected?: boolean; node?: unknown; error?: string }>
    bindCurrentNode: (data: { token: string; machineId: string }) => Promise<{ success: boolean; data?: unknown; error?: string }>
    unbindNode: (data: { token: string; nodeId: string }) => Promise<{ success: boolean; data?: unknown; error?: string }>
  }
}
