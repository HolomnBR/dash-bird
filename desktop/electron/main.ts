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

// Controle para evitar registros duplicados do node
let nodeRegistrationAttempted = false

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
} catch {
  // Ignore errors during path setup
}

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

// Função para obter usuário logado via IPC
async function getLoggedUser(): Promise<{ id: string; name: string; email: string } | null> {
  try {
    if (!mainWindow) return null
    
    // Enviar mensagem para o renderer obter dados do usuário
    const userData = await mainWindow.webContents.executeJavaScript(`
      (() => {
        try {
          const token = localStorage.getItem('auth_token')
          const userData = localStorage.getItem('user_data')
          
          if (token && userData) {
            const user = JSON.parse(userData)
            return user
          }
          return null
        } catch (error) {
          console.error('Erro ao obter usuário logado:', error)
          return null
        }
      })()
    `)
    
    return userData
  } catch (error) {
    console.error('❌ Erro ao obter usuário logado via IPC:', error)
    return null
  }
}

// Função unificada para registrar o node e iniciar streaming (MÉTODO OTIMIZADO)
async function registerNodeAndStartStreaming(nodeConfig: NodeConfig, userId?: string): Promise<void> {
  // Evitar registros duplicados
  if (nodeRegistrationAttempted) {
    console.log('🔄 Registro do node já foi tentado nesta sessão, pulando...')
    return
  }
  
  nodeRegistrationAttempted = true
  
  try {
    const apiUrl = 'http://localhost:8000'
    
    // Obter token de autenticação se disponível
    let authToken: string | undefined
    if (userId) {
      try {
        const user = await getLoggedUser()
        if (user) {
          // Obter o token real do localStorage
          const token = await mainWindow?.webContents.executeJavaScript(`
            localStorage.getItem('auth_token')
          `)
          authToken = token || undefined
        }
      } catch (error) {
        console.warn('⚠️ Não foi possível obter token de autenticação:', error)
      }
    }
    
    // NOVO: Registro unificado - uma única chamada que faz tudo
    const nodeRegistrationData = {
      nodeId: nodeConfig.nodeId, // Usar o nodeId do Electron como connectionId
      name: nodeConfig.alias || `Desktop Node (${nodeConfig.machineName})` || `Node-${nodeConfig.nodeId.slice(0, 8)}`,
      machineName: nodeConfig.machineName, // Incluir machineName explicitamente
      machineId: nodeConfig.machineId,
      ipAddress: '::1',
      port: 5000,
      databasePath: null,
      version: '2.1.3',
      operatingSystem: process.platform === 'win32' ? 'Windows 11 Pro' : 
                      process.platform === 'darwin' ? 'macOS' : 
                      process.platform === 'linux' ? 'Linux' : 'Unknown'
    }
    
    console.log('🔄 Registrando node unificado (registro + streaming):', nodeRegistrationData)
    
    const controller = new AbortController()
    const timeoutId = setTimeout(() => controller.abort(), 15000) // 15 segundos timeout
    
    const response = await fetch(`${apiUrl}/api/Sync/register-node`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(authToken && { 'Authorization': `Bearer ${authToken}` })
      },
      body: JSON.stringify(nodeRegistrationData),
      signal: controller.signal
    })
    
    clearTimeout(timeoutId)
    
    if (response.ok) {
      const result = await response.json() as { success: boolean; data?: { isConnected?: boolean } }
      console.log('✅ Node registrado e streaming iniciado com sucesso:', result)
      
      // Verificar se o streaming está realmente conectado
      if (result.data?.isConnected) {
        console.log('🎉 Nó totalmente conectado e operacional!')
      } else {
        console.warn('⚠️ Nó registrado mas streaming pode não estar ativo')
      }
    } else {
      const errorText = await response.text()
      console.warn('⚠️ Falha ao registrar node unificado:', response.status, errorText)
    }
  } catch (error: unknown) {
    if (error instanceof Error) {
      if (error.name === 'AbortError') {
        console.warn('⚠️ Timeout ao registrar node unificado (15s)')
      } else if ('code' in error && (error as { code: string }).code === 'ECONNREFUSED') {
        console.warn('⚠️ API não está rodando em http://localhost:8000')
      } else {
        console.error('❌ Erro ao registrar node unificado:', error)
      }
    } else {
      console.error('❌ Erro desconhecido ao registrar node unificado:', error)
    }
    // Resetar flag em caso de erro para permitir nova tentativa
    nodeRegistrationAttempted = false
  }
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
  
  // Registrar o node e iniciar streaming DEPOIS de retornar a configuração
  setImmediate(async () => {
    try {
      const user = await getLoggedUser()
      await registerNodeAndStartStreaming(cfg, user?.id)
    } catch (error) {
      console.error('❌ Erro ao registrar node e iniciar streaming:', error)
    }
  })
  
  return cfg
})

ipcMain.handle('nodeConfig:setAlias', async (_event, alias: string) => {
  const cfg = getOrCreateNodeConfig()
  cfg.alias = alias ?? ''
  store.set('nodeConfig', cfg)
  return cfg
})

ipcMain.handle('registerNode', async (_event, databasePath?: string) => {
  try {
    console.log('📡 Handler registerNode chamado com databasePath:', databasePath)
    const cfg = getOrCreateNodeConfig()
    const user = await getLoggedUser()
    await registerNodeAndStartStreaming(cfg, user?.id)
    
    // Retornar informações do nó registrado
    return {
      success: true,
      data: {
        id: cfg.nodeId,
        name: cfg.alias || `Desktop Node (${cfg.machineName})`,
        machineId: cfg.machineId,
        machineName: cfg.machineName,
        ipAddress: '::1',
        port: 5000,
        databasePath: databasePath || null,
        version: '2.1.3',
        operatingSystem: process.platform === 'win32' ? 'Windows' : 
                        process.platform === 'darwin' ? 'macOS' : 
                        process.platform === 'linux' ? 'Linux' : 'Unknown',
        lastSeen: new Date().toISOString(),
        createdAt: cfg.createdAt,
        isActive: true
      }
    }
  } catch (error) {
    console.error('❌ Erro no handler registerNode:', error)
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Erro desconhecido'
    }
  }
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
ipcMain.handle('auth:register', async (_event, _userData: { name: string; email: string; password: string }) => {
  try {
    // Fazer requisição para a API local que vai comunicar com o servidor cloud
    const response = await fetch('http://localhost:8000/api/Auth/register', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(_userData)
    })
    
    const result = await response.json()
    
    if (!response.ok) {
      const errorData = result as { message?: string }
      throw new Error(errorData.message || `HTTP error! status: ${response.status}`)
    }
    
    return { success: true, data: result }
  } catch (error) {
    return { success: false, error: error instanceof Error ? error.message : String(error) }
  }
})

ipcMain.handle('auth:login', async (_event, credentials: { email: string; password: string }) => {
  try {
    // Fazer requisição para a API local que vai comunicar com o servidor cloud
    const response = await fetch('http://localhost:8000/api/Auth/login', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(credentials)
    })
    
    const result = await response.json()
    
    if (!response.ok) {
      const errorData = result as { message?: string }
      throw new Error(errorData.message || `HTTP error! status: ${response.status}`)
    }
    
    return { success: true, data: result }
  } catch (error) {
    return { success: false, error: error instanceof Error ? error.message : String(error) }
  }
})

ipcMain.handle('auth:logout', async (_event, _token: string) => {
  try {
    // Fazer requisição para a API local
    const response = await fetch('http://localhost:8000/api/Auth/logout', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${_token}`
      }
    })
    
    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`)
    }
    
    const result = await response.json()
    return { success: true, data: result }
  } catch (error) {
    return { success: false, error: error instanceof Error ? error.message : String(error) }
  }
})

ipcMain.handle('auth:getProfile', async (_event, _token: string) => {
  try {
    // Fazer requisição para a API local
    const response = await fetch('http://localhost:8000/api/Auth/profile', {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${_token}`
      }
    })
    
    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`)
    }
    
    const result = await response.json()
    return { success: true, data: result }
  } catch (error) {
    return { success: false, error: error instanceof Error ? error.message : String(error) }
  }
})

ipcMain.handle('auth:validateToken', async (_event, _token: string) => {
  try {
    // Fazer requisição para a API local
    const response = await fetch('http://localhost:8000/api/Auth/validate-token', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${_token}`
      }
    })
    
    if (!response.ok) {
      return { success: true, valid: false }
    }
    
    const result = await response.json() as { valid?: boolean }
    return { success: true, valid: result.valid || false }
  } catch (error) {
    return { success: false, error: error instanceof Error ? error.message : String(error) }
  }
})

ipcMain.handle('auth:bindNode', async (_event, _data: { token: string; anonymousToken: string }) => {
  try {
    // Fazer requisição para a API local que vai comunicar com o servidor cloud
    const response = await fetch('http://localhost:8000/api/Auth/bind-node', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${_data.token}`
      },
      body: JSON.stringify({
        anonymousToken: _data.anonymousToken
      })
    })
    
    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`)
    }
    
    const result = await response.json()
    return { success: true, data: result }
  } catch (error) {
    return { success: false, error: error instanceof Error ? error.message : String(error) }
  }
})

ipcMain.handle('auth:getUserNodes', async (_event, _token: string) => {
  try {
    // Fazer requisição para a API local
    const response = await fetch('http://localhost:8000/api/Auth/nodes', {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${_token}`
      }
    })
    
    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`)
    }
    
    const result = await response.json()
    return { success: true, data: result }
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

ipcMain.handle('auth:checkNodeConnection', async (_event, _data: { token: string; machineId: string }) => {
  try {
    // Fazer requisição para a API local que vai verificar no servidor cloud
    const response = await fetch(`http://localhost:8000/api/Auth/check-node-connection/${_data.machineId}`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${_data.token}`
      }
    })
    
    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`)
    }
    
    const result = await response.json() as Record<string, unknown>
    return { success: true, ...result }
  } catch (error) {
    return { success: false, error: error instanceof Error ? error.message : String(error) }
  }
})

ipcMain.handle('auth:bindCurrentNode', async (_event, _data: { token: string; machineId: string }) => {
  try {
    // Fazer requisição para a API local que vai comunicar com o servidor cloud
    const response = await fetch('http://localhost:8000/api/Auth/bind-current-node', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${_data.token}`
      },
      body: JSON.stringify({
        machineId: _data.machineId
      })
    })
    
    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`)
    }
    
    const result = await response.json()
    return { success: true, data: result }
  } catch (error) {
    return { success: false, error: error instanceof Error ? error.message : String(error) }
  }
})

ipcMain.handle('auth:unbindNode', async (_event, _data: { token: string; nodeId: string }) => {
  try {
    // Fazer requisição para a API local que vai comunicar com o servidor cloud
    const response = await fetch('http://localhost:8000/api/Auth/unbind-node', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${_data.token}`
      },
      body: JSON.stringify({
        nodeId: _data.nodeId
      })
    })
    
    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`)
    }
    
    const result = await response.json()
    return { success: true, data: result }
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


