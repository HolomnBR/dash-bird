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
  }
}

