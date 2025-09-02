import { contextBridge, ipcRenderer } from 'electron'

contextBridge.exposeInMainWorld('env', {
  isElectron: true,
})

contextBridge.exposeInMainWorld('system', {
  async getInfo() {
    const info = await ipcRenderer.invoke('system:getInfo')
    return info as { deviceName: string; machineId: string }
  },
  async getNodeConfig() {
    return (await ipcRenderer.invoke('nodeConfig:get')) as {
      nodeId: string
      machineId: string
      machineName: string
      alias?: string
      createdAt: string
    }
  },
  async setAlias(alias: string) {
    return (await ipcRenderer.invoke('nodeConfig:setAlias', alias)) as {
      nodeId: string
      machineId: string
      machineName: string
      alias?: string
      createdAt: string
    }
  },
  async openFile(filters?: Array<{ name: string; extensions: string[] }>): Promise<string[]> {
    const paths = await ipcRenderer.invoke('dialog:openFile', { filters })
    return paths as string[]
  },
})

contextBridge.exposeInMainWorld('auth', {
  async register(userData: { name: string; email: string; password: string }) {
    return await ipcRenderer.invoke('auth:register', userData)
  },
  async login(credentials: { email: string; password: string }) {
    return await ipcRenderer.invoke('auth:login', credentials)
  },
  async logout(token: string) {
    return await ipcRenderer.invoke('auth:logout', token)
  },
  async getProfile(token: string) {
    return await ipcRenderer.invoke('auth:getProfile', token)
  },
  async validateToken(token: string) {
    return await ipcRenderer.invoke('auth:validateToken', token)
  },
  async bindNode(data: { token: string; anonymousToken: string }) {
    return await ipcRenderer.invoke('auth:bindNode', data)
  },
  async getUserNodes(token: string) {
    return await ipcRenderer.invoke('auth:getUserNodes', token)
  },
  async getAvailableNodes() {
    return await ipcRenderer.invoke('auth:getAvailableNodes')
  },
  async checkNodeConnection(data: { token: string; machineId: string }) {
    return await ipcRenderer.invoke('auth:checkNodeConnection', data)
  },
  async bindCurrentNode(data: { token: string; machineId: string }) {
    return await ipcRenderer.invoke('auth:bindCurrentNode', data)
  },
  async unbindNode(data: { token: string; nodeId: string }) {
    return await ipcRenderer.invoke('auth:unbindNode', data)
  },
})

declare global {
  interface Window {
    env: { isElectron: boolean }
    system: {
      getInfo: () => Promise<{ deviceName: string; machineId: string }>
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
      register: (userData: { name: string; email: string; password: string }) => Promise<{ success: boolean; data?: any; error?: string }>
      login: (credentials: { email: string; password: string }) => Promise<{ success: boolean; data?: any; error?: string }>
      logout: (token: string) => Promise<{ success: boolean }>
      getProfile: (token: string) => Promise<{ success: boolean; data?: any; error?: string }>
      validateToken: (token: string) => Promise<{ success: boolean; valid?: boolean }>
      bindNode: (data: { token: string; anonymousToken: string }) => Promise<{ success: boolean; data?: any; error?: string }>
      getUserNodes: (token: string) => Promise<{ success: boolean; data?: any; error?: string }>
      getAvailableNodes: () => Promise<{ success: boolean; data?: any; error?: string }>
      checkNodeConnection: (data: { token: string; machineId: string }) => Promise<{ success: boolean; isConnected?: boolean; node?: any; wasUnbound?: boolean; error?: string }>
      bindCurrentNode: (data: { token: string; machineId: string }) => Promise<{ success: boolean; data?: any; error?: string }>
      unbindNode: (data: { token: string; nodeId: string }) => Promise<{ success: boolean; data?: any; error?: string }>
    }
  }
}

