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
}
