import { app, BrowserWindow, shell, ipcMain, dialog, globalShortcut } from 'electron'
import { existsSync, mkdirSync } from 'node:fs'
import Store from 'electron-store'
import { randomUUID } from 'node:crypto'
import { hostname } from 'os'
import { machineId } from 'node-machine-id'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'
import ApiManager from '../scripts/api-manager.js'

process.env.ELECTRON_DISABLE_SECURITY_WARNINGS = 'true'

const __dirname = dirname(fileURLToPath(import.meta.url))

let mainWindow: BrowserWindow | null = null
const apiManager = new ApiManager()
const argv = process.argv.slice(1)
const shouldOpenDevTools = argv.includes('--devtools') || argv.includes('--debug') || argv.includes('-d') || process.env.OPEN_DEVTOOLS === '1'
const shouldDisableGpu = argv.includes('--disable-gpu') || argv.includes('--safe-mode') || process.env.DISABLE_GPU === '1' || process.env.SAFE_MODE === '1'

// Configure paths and Chromium switches as early as possible (before app ready)
try {
  const userDataPath = join(app.getPath('appData'), 'Dash Bird')
  app.setPath('userData', userDataPath)
  if (!existsSync(userDataPath)) mkdirSync(userDataPath, { recursive: true })
  const diskCachePath = join(userDataPath, 'Cache')
  const gpuCachePath = join(userDataPath, 'GPUCache')
  if (!existsSync(diskCachePath)) mkdirSync(diskCachePath, { recursive: true })
  if (!existsSync(gpuCachePath)) mkdirSync(gpuCachePath, { recursive: true })
  app.commandLine.appendSwitch('user-data-dir', userDataPath)
  app.commandLine.appendSwitch('disk-cache-dir', diskCachePath)
  app.commandLine.appendSwitch('disable-http-cache', '1')
  app.commandLine.appendSwitch('disable-gpu-shader-disk-cache', '1')
  app.commandLine.appendSwitch('disable-gpu-program-cache', '1')
} catch {}

if (shouldDisableGpu) {
  app.disableHardwareAcceleration()
  app.commandLine.appendSwitch('disable-gpu')
  app.commandLine.appendSwitch('disable-gpu-compositing')
}

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 400,
    height: 700,
    autoHideMenuBar: true,
    title: 'Dash Bird - Sua IA conectada em seu FireBird',
    webPreferences: {
      preload: join(__dirname, 'preload.mjs'),
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: false,
    },
  })

  if (!app.isPackaged) {
    mainWindow.loadURL('http://localhost:5173')
    mainWindow.webContents.openDevTools({ mode: 'detach' })
  } else {
    mainWindow.loadFile(join(__dirname, '../dist/index.html'))
    if (shouldOpenDevTools) {
      // Open once the renderer is ready to avoid timing issues on some systems
      mainWindow.webContents.once('dom-ready', () => {
        mainWindow?.webContents.openDevTools({ mode: 'detach' })
      })
    }
  }

  mainWindow.webContents.setWindowOpenHandler(({ url }) => {
    shell.openExternal(url)
    return { action: 'deny' }
  })

  mainWindow.on('closed', () => {
    mainWindow = null
  })

  // Also try on first paint/ready-to-show for extra reliability
  if (shouldOpenDevTools) {
    mainWindow.once('ready-to-show', () => {
      if (!mainWindow?.webContents.isDevToolsOpened()) {
        mainWindow?.webContents.openDevTools({ mode: 'detach' })
      }
    })
  }

  // Fallback: if load fails (e.g., due to GPU/driver issues), try a soft reload once
  mainWindow.webContents.once('did-fail-load', () => {
    setTimeout(() => {
      if (!mainWindow) return
      if (app.isPackaged) mainWindow.loadFile(join(__dirname, '../dist/index.html'))
      else mainWindow.loadURL('http://localhost:5173')
    }, 300)
  })
}

// Register IPC handlers before app is ready
type NodeConfig = { nodeId: string; machineId: string; machineName: string; alias?: string; createdAt: string }
const store = new Store<{ nodeConfig?: NodeConfig }>()
function getOrCreateNodeConfig(): NodeConfig {
  let cfg = store.get('nodeConfig')
  let changed = false
  if (!cfg) {
    cfg = {
      nodeId: randomUUID(),
      machineId: 'unknown',
      machineName: 'unknown',
      alias: '',
      createdAt: new Date().toISOString(),
    }
    changed = true
  }
  return (changed ? (store.set('nodeConfig', cfg), cfg) : cfg) as NodeConfig
}

// Register all IPC handlers before creating window
console.log('🔧 Registrando handlers IPC...')

ipcMain.handle('system:getInfo', async () => {
  console.log('📡 Handler system:getInfo chamado')
  let id = 'unknown'
  try {
    id = await machineId(true)
  } catch (error) {
    console.error('❌ Erro ao obter machineId:', error)
  }
  const deviceName = hostname()
  const result = { deviceName, machineId: id }
  console.log('✅ Handler system:getInfo retornando:', result)
  return result
})

ipcMain.handle('nodeConfig:get', async () => {
  console.log('📡 Handler nodeConfig:get chamado')
  const info = await (async () => {
    let id = 'unknown'
    try { id = await machineId(true) } catch (error) {
      console.error('❌ Erro ao obter machineId no nodeConfig:', error)
    }
    return { machineId: id, deviceName: hostname() }
  })()
  const cfg = getOrCreateNodeConfig()
  let changed = false
  if (!cfg.machineId || cfg.machineId === 'unknown') { cfg.machineId = info.machineId; changed = true }
  if (!cfg.machineName || cfg.machineName === 'unknown') { cfg.machineName = info.deviceName; changed = true }
  if (changed) store.set('nodeConfig', cfg)
  console.log('✅ Handler nodeConfig:get retornando:', cfg)
  return cfg
})

ipcMain.handle('nodeConfig:setAlias', async (_event, alias: string) => {
  const cfg = getOrCreateNodeConfig()
  cfg.alias = alias ?? ''
  store.set('nodeConfig', cfg)
  return cfg
})

ipcMain.handle('dialog:openFile', async (_event, options?: { filters?: Array<{ name: string; extensions: string[] }> }) => {
  const result = await dialog.showOpenDialog(mainWindow!, {
    properties: ['openFile'],
    filters: options?.filters ?? [
      { name: 'Firebird Database', extensions: ['fdb'] },
      { name: 'All Files', extensions: ['*'] },
    ],
  })
  return result.canceled ? [] : result.filePaths
})

// API Management handlers
ipcMain.handle('api:getStatus', async () => {
  return apiManager.getStatus()
})

ipcMain.handle('api:start', async () => {
  try {
    await apiManager.startApi()
    return { success: true }
  } catch (error) {
    return { success: false, error: error instanceof Error ? error.message : String(error) }
  }
})

ipcMain.handle('api:stop', async () => {
  try {
    apiManager.stopApi()
    return { success: true }
  } catch (error) {
    return { success: false, error: error instanceof Error ? error.message : String(error) }
  }
})

ipcMain.handle('api:restart', async () => {
  try {
    apiManager.stopApi()
    await new Promise(resolve => setTimeout(resolve, 1000)) // Aguardar um pouco
    await apiManager.startApi()
    return { success: true }
  } catch (error) {
    return { success: false, error: error instanceof Error ? error.message : String(error) }
  }
})

// Auth handlers
ipcMain.handle('auth:register', async (_event, userData: { name: string; email: string; password: string }) => {
  try {
    // Aqui você implementaria a lógica de registro
    // Por enquanto, retorna sucesso simulado
    return { success: true, data: { message: 'Registro realizado com sucesso' } }
  } catch (error) {
    return { success: false, error: error instanceof Error ? error.message : String(error) }
  }
})

ipcMain.handle('auth:login', async (_event, credentials: { email: string; password: string }) => {
  try {
    // Aqui você implementaria a lógica de login
    // Por enquanto, retorna sucesso simulado
    return { success: true, data: { token: 'fake-token', user: { email: credentials.email, name: 'Usuário' } } }
  } catch (error) {
    return { success: false, error: error instanceof Error ? error.message : String(error) }
  }
})

ipcMain.handle('auth:logout', async (_event, token: string) => {
  try {
    // Aqui você implementaria a lógica de logout
    return { success: true }
  } catch (error) {
    return { success: false, error: error instanceof Error ? error.message : String(error) }
  }
})

ipcMain.handle('auth:getProfile', async (_event, token: string) => {
  try {
    // Aqui você implementaria a lógica de obter perfil
    return { success: true, data: { email: 'user@example.com', name: 'Usuário' } }
  } catch (error) {
    return { success: false, error: error instanceof Error ? error.message : String(error) }
  }
})

ipcMain.handle('auth:validateToken', async (_event, token: string) => {
  try {
    // Aqui você implementaria a validação do token
    return { success: true, valid: true }
  } catch (error) {
    return { success: false, error: error instanceof Error ? error.message : String(error) }
  }
})

ipcMain.handle('auth:bindNode', async (_event, data: { token: string; anonymousToken: string }) => {
  try {
    // Aqui você implementaria a lógica de vincular nó
    return { success: true, data: { message: 'Nó vinculado com sucesso' } }
  } catch (error) {
    return { success: false, error: error instanceof Error ? error.message : String(error) }
  }
})

ipcMain.handle('auth:getUserNodes', async (_event, token: string) => {
  try {
    // Aqui você implementaria a lógica de obter nós do usuário
    return { success: true, data: [] }
  } catch (error) {
    return { success: false, error: error instanceof Error ? error.message : String(error) }
  }
})

ipcMain.handle('auth:getAvailableNodes', async () => {
  try {
    // Aqui você implementaria a lógica de obter nós disponíveis
    return { success: true, data: [] }
  } catch (error) {
    return { success: false, error: error instanceof Error ? error.message : String(error) }
  }
})

ipcMain.handle('auth:checkNodeConnection', async (_event, data: { token: string; machineId: string }) => {
  try {
    // Aqui você implementaria a verificação de conexão do nó
    return { success: true, isConnected: false, wasUnbound: false }
  } catch (error) {
    return { success: false, error: error instanceof Error ? error.message : String(error) }
  }
})

ipcMain.handle('auth:bindCurrentNode', async (_event, data: { token: string; machineId: string }) => {
  try {
    // Aqui você implementaria a lógica de vincular nó atual
    return { success: true, data: { message: 'Nó atual vinculado com sucesso' } }
  } catch (error) {
    return { success: false, error: error instanceof Error ? error.message : String(error) }
  }
})

ipcMain.handle('auth:unbindNode', async (_event, data: { token: string; nodeId: string }) => {
  try {
    // Aqui você implementaria a lógica de desvincular nó
    return { success: true, data: { message: 'Nó desvinculado com sucesso' } }
  } catch (error) {
    return { success: false, error: error instanceof Error ? error.message : String(error) }
  }
})

console.log('✅ Handlers IPC registrados com sucesso')

app.whenReady().then(async () => {
  createWindow()

  // Inicializar o ApiManager
  apiManager.init()

  // Verificar e iniciar a API se necessário
  try {
    console.log('🔍 Verificando status da API...')
    await apiManager.ensureApiRunning()
  } catch (error) {
    console.error('❌ Erro ao verificar/iniciar API:', error instanceof Error ? error.message : String(error))
  }

  // Register devtools shortcut in prod/dev
  globalShortcut.register('Control+Shift+I', () => {
    const win = BrowserWindow.getFocusedWindow() || mainWindow
    if (win) {
      if (win.webContents.isDevToolsOpened()) win.webContents.closeDevTools()
      else win.webContents.openDevTools({ mode: 'detach' })
    }
  })
  // F12 toggle as well
  globalShortcut.register('F12', () => {
    const win = BrowserWindow.getFocusedWindow() || mainWindow
    if (win) {
      if (win.webContents.isDevToolsOpened()) win.webContents.closeDevTools()
      else win.webContents.openDevTools({ mode: 'detach' })
    }
  })
  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow()
  })
})

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') app.quit()
})

// Cleanup da API quando o app for fechado
app.on('before-quit', () => {
  console.log('🛑 Encerrando aplicação...')
  apiManager.stopApi()
})


