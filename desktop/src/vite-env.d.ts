/// <reference types="vite/client" />

interface SystemInfo {
  deviceName: string
  machineId: string
}

declare interface Window {
  env: { isElectron: boolean }
  system: {
    getInfo: () => Promise<SystemInfo>
    openFile: (filters?: Array<{ name: string; extensions: string[] }>) => Promise<string[]>
  }
}
