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
} catch (error) {
  console.error('Erro ao configurar paths:', error)
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

// Função para registrar nó anônimo via API local (que por sua vez registra no servidor cloud)
async function registerAnonymousNodeInCloud(nodeConfig: NodeConfig) {
  try {
    const localApiUrl = 'http://localhost:5000' // API local
    const registrationData = {
      name: `${nodeConfig.machineName}${nodeConfig.alias ? ` (${nodeConfig.alias})` : ''}`,
      machineId: nodeConfig.machineId,
      ipAddress: '127.0.0.1', // IP local
      port: 5000, // Porta da API local
      databasePath: null, // Será preenchido quando configurar DB
      version: '1.0.0',
      operatingSystem: process.platform
    }

    console.log('🌐 Registrando nó anônimo via API local:', registrationData.name)
    
    const response = await fetch(`${localApiUrl}/api/AnonymousNode/register`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(registrationData)
    })
    
    if (response.ok) {
      const result = await response.json()
      console.log('✅ Nó anônimo registrado via API local com sucesso:', result)
      return result
    } else {
      const errorText = await response.text()
      console.warn('⚠️ Falha ao registrar nó anônimo via API local:', response.status, errorText)
      return null
    }
  } catch (error) {
    console.error('❌ Erro ao registrar nó anônimo via API local:', error)
    return null
  }
}

// Função para registrar nó anônimo apenas no startup da aplicação
let nodeRegistrationAttempted = false
async function registerNodeOnStartup() {
  if (nodeRegistrationAttempted) {
    console.log('🔄 Registro de nó já foi tentado, pulando...')
    return
  }
  
  nodeRegistrationAttempted = true
  
  try {
    console.log('🚀 Iniciando registro de nó anônimo no startup...')
    const cfg = getOrCreateNodeConfig()
    await registerAnonymousNodeInCloud(cfg)
  } catch (error) {
    console.warn('⚠️ Falha ao registrar nó anônimo no startup:', error)
  }
}

// Função para registrar nó via API local (método legado) - removida pois não é mais usada

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

ipcMain.handle('api:checkStatus', async () => {
  try {
    const isRunning = await apiManager.checkApiStatus()
    const status = apiManager.getStatus()
    return { 
      success: true, 
      isRunning, 
      status: {
        ...status,
        isRunning
      }
    }
  } catch (error) {
    return { success: false, error: error instanceof Error ? error.message : String(error) }
  }
})

// Auth handlers
ipcMain.handle('auth:register', async (_event, userData: { name: string; email: string; password: string }) => {
  try {
    const localApiUrl = 'http://localhost:5000'
    const response = await fetch(`${localApiUrl}/api/Auth/register`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(userData)
    })

    if (response.ok) {
      const responseText = await response.text()
      if (!responseText) {
        return { success: false, error: 'Resposta vazia da API' }
      }
      
      try {
        const result = JSON.parse(responseText)
        return { success: true, data: result }
      } catch (parseError) {
        console.error('Erro ao fazer parse da resposta:', parseError)
        return { success: false, error: 'Resposta inválida da API' }
      }
    } else {
      const responseText = await response.text()
      let errorMessage = 'Erro ao registrar usuário'
      
      if (responseText) {
        try {
          const errorData = JSON.parse(responseText)
          errorMessage = errorData.message || errorMessage
        } catch {
          errorMessage = responseText || errorMessage
        }
      }
      
      return { success: false, error: errorMessage }
    }
  } catch (error) {
    console.error('Erro no registro:', error)
    return { success: false, error: error instanceof Error ? error.message : String(error) }
  }
})

ipcMain.handle('auth:login', async (_event, credentials: { email: string; password: string }) => {
  try {
    const localApiUrl = 'http://localhost:5000'
    const response = await fetch(`${localApiUrl}/api/Auth/login`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(credentials)
    })

    if (response.ok) {
      const responseText = await response.text()
      if (!responseText) {
        return { success: false, error: 'Resposta vazia da API' }
      }
      
      try {
        const result = JSON.parse(responseText)
        return { success: true, data: result }
      } catch (parseError) {
        console.error('Erro ao fazer parse da resposta:', parseError)
        return { success: false, error: 'Resposta inválida da API' }
      }
    } else {
      const responseText = await response.text()
      let errorMessage = 'Email ou senha inválidos'
      
      if (responseText) {
        try {
          const errorData = JSON.parse(responseText)
          errorMessage = errorData.message || errorMessage
        } catch {
          errorMessage = responseText || errorMessage
        }
      }
      
      return { success: false, error: errorMessage }
    }
  } catch (error) {
    console.error('Erro no login:', error)
    return { success: false, error: error instanceof Error ? error.message : String(error) }
  }
})

ipcMain.handle('auth:logout', async (_event, token: string) => {
  try {
    const localApiUrl = 'http://localhost:5000'
    console.log('Iniciando logout para token:', token.substring(0, 20) + '...')
    
    // 1. Primeiro, obter todos os nós do usuário
    console.log('Buscando nós do usuário...')
    const nodesResponse = await fetch(`${localApiUrl}/api/Auth/nodes`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    })

    let unboundNodesCount = 0
    let unboundNodeIds: string[] = []

    console.log('Status da resposta de nós:', nodesResponse.status)
    
    if (nodesResponse.ok) {
      const nodesData = await nodesResponse.json()
      const userNodes = nodesData || []
      
      console.log('Dados dos nós recebidos:', nodesData)
      console.log(`Encontrados ${userNodes.length} nós para desvincular`)
      
      // 2. Desvincular cada nó individualmente
      for (const node of userNodes) {
        try {
          const unbindResponse = await fetch(`${localApiUrl}/api/Auth/unbind-node`, {
            method: 'POST',
            headers: {
              'Authorization': `Bearer ${token}`,
              'Content-Type': 'application/json'
            },
            body: JSON.stringify({ nodeId: node.id })
          })

          if (unbindResponse.ok) {
            unboundNodesCount++
            unboundNodeIds.push(node.id)
            console.log(`Nó desvinculado: ${node.name} (${node.id})`)
          } else {
            console.warn(`Falha ao desvincular nó: ${node.name} (${node.id})`)
          }
        } catch (unbindError) {
          console.error(`Erro ao desvincular nó ${node.id}:`, unbindError)
        }
      }
    } else {
      const errorText = await nodesResponse.text()
      console.warn('Não foi possível obter lista de nós do usuário. Status:', nodesResponse.status, 'Erro:', errorText)
    }

    // 3. Agora fazer o logout
    console.log('Fazendo logout...')
    const logoutResponse = await fetch(`${localApiUrl}/api/Auth/logout`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    })

    console.log('Status da resposta de logout:', logoutResponse.status)
    
    if (logoutResponse.ok) {
      let result = {}
      try {
        const responseText = await logoutResponse.text()
        if (responseText) {
          result = JSON.parse(responseText)
        }
      } catch (parseError) {
        console.warn('Resposta de logout não é JSON válido, usando resposta vazia')
      }
      
      console.log('Logout realizado com sucesso:', result)
      return { 
        success: true, 
        data: {
          ...result,
          unboundNodesCount,
          unboundNodeIds
        }
      }
    } else {
      let errorData = { message: 'Erro no logout' }
      try {
        const responseText = await logoutResponse.text()
        if (responseText) {
          errorData = JSON.parse(responseText)
        }
      } catch (parseError) {
        console.warn('Resposta de erro não é JSON válido')
      }
      
      console.error('Erro no logout:', errorData)
      return { success: false, error: errorData.message || 'Erro no logout' }
    }
  } catch (error) {
    console.error('Erro no logout:', error)
    return { success: false, error: error instanceof Error ? error.message : String(error) }
  }
})

ipcMain.handle('auth:getProfile', async (_event, token: string) => {
  try {
    const localApiUrl = 'http://localhost:5000'
    const response = await fetch(`${localApiUrl}/api/Auth/profile`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`,
      }
    })

    if (response.ok) {
      const responseText = await response.text()
      if (!responseText) {
        return { success: false, error: 'Resposta vazia da API' }
      }
      
      try {
        const result = JSON.parse(responseText)
        return { success: true, data: result }
      } catch (parseError) {
        console.error('Erro ao fazer parse da resposta:', parseError)
        return { success: false, error: 'Resposta inválida da API' }
      }
    } else {
      return { success: false, error: 'Erro ao obter perfil do usuário' }
    }
  } catch (error) {
    return { success: false, error: error instanceof Error ? error.message : String(error) }
  }
})

ipcMain.handle('auth:validateToken', async (_event, token: string) => {
  try {
    const localApiUrl = 'http://localhost:5000'
    const response = await fetch(`${localApiUrl}/api/Auth/validate-token`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
      }
    })

    if (response.ok) {
      const responseText = await response.text()
      if (!responseText) {
        return { success: false, valid: false }
      }
      
      try {
        const result = JSON.parse(responseText)
        return { success: true, valid: result.valid }
      } catch (parseError) {
        console.error('Erro ao fazer parse da resposta:', parseError)
        return { success: false, valid: false }
      }
    } else {
      return { success: false, valid: false }
    }
  } catch {
    return { success: false, valid: false }
  }
})

ipcMain.handle('auth:bindNode', async (_event, data: { token: string; anonymousToken: string }) => {
  try {
    const localApiUrl = 'http://localhost:5000'
    const response = await fetch(`${localApiUrl}/api/Auth/bind-node`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${data.token}`,
      },
      body: JSON.stringify({ anonymousToken: data.anonymousToken })
    })

    if (response.ok) {
      const responseText = await response.text()
      if (!responseText) {
        return { success: false, error: 'Resposta vazia da API' }
      }
      
      try {
        const result = JSON.parse(responseText)
        return { success: true, data: result }
      } catch (parseError) {
        console.error('Erro ao fazer parse da resposta:', parseError)
        return { success: false, error: 'Resposta inválida da API' }
      }
    } else {
      const responseText = await response.text()
      let errorMessage = 'Erro ao vincular nó ao usuário'
      
      if (responseText) {
        try {
          const errorData = JSON.parse(responseText)
          errorMessage = errorData.message || errorMessage
        } catch {
          errorMessage = responseText || errorMessage
        }
      }
      
      return { success: false, error: errorMessage }
    }
  } catch (error) {
    return { success: false, error: error instanceof Error ? error.message : String(error) }
  }
})

ipcMain.handle('auth:getUserNodes', async (_event, token: string) => {
  try {
    const localApiUrl = 'http://localhost:5000'
    const response = await fetch(`${localApiUrl}/api/User/nodes`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`,
      }
    })

    if (response.ok) {
      const responseText = await response.text()
      if (!responseText) {
        return { success: false, error: 'Resposta vazia da API' }
      }
      
      try {
        const result = JSON.parse(responseText)
        return { success: true, data: result }
      } catch (parseError) {
        console.error('Erro ao fazer parse da resposta:', parseError)
        return { success: false, error: 'Resposta inválida da API' }
      }
    } else {
      const responseText = await response.text()
      let errorMessage = 'Erro ao obter nós do usuário'
      
      if (responseText) {
        try {
          const errorData = JSON.parse(responseText)
          errorMessage = errorData.message || errorMessage
        } catch {
          errorMessage = responseText || errorMessage
        }
      }
      
      return { success: false, error: errorMessage }
    }
  } catch (error) {
    return { success: false, error: error instanceof Error ? error.message : String(error) }
  }
})

ipcMain.handle('auth:getAvailableNodes', async () => {
  try {
    const localApiUrl = 'http://localhost:5000'
    const response = await fetch(`${localApiUrl}/api/AnonymousNode/available`, {
      method: 'GET',
    })

    if (response.ok) {
      const responseText = await response.text()
      if (!responseText) {
        return { success: false, error: 'Resposta vazia da API' }
      }
      
      try {
        const result = JSON.parse(responseText)
        return { success: true, data: result }
      } catch (parseError) {
        console.error('Erro ao fazer parse da resposta:', parseError)
        return { success: false, error: 'Resposta inválida da API' }
      }
    } else {
      const responseText = await response.text()
      let errorMessage = 'Erro ao obter nós disponíveis'
      
      if (responseText) {
        try {
          const errorData = JSON.parse(responseText)
          errorMessage = errorData.message || errorMessage
        } catch {
          errorMessage = responseText || errorMessage
        }
      }
      
      return { success: false, error: errorMessage }
    }
  } catch (error) {
    return { success: false, error: error instanceof Error ? error.message : String(error) }
  }
})

ipcMain.handle('auth:checkNodeConnection', async (_event, data: { token: string; machineId: string }) => {
  try {
    console.log('🔍 Verificando conexão do nó:', { machineId: data.machineId, tokenLength: data.token?.length })
    
    const localApiUrl = 'http://localhost:5000'
    console.log('🌐 Fazendo requisição para:', `${localApiUrl}/api/User/nodes`)
    
    const response = await fetch(`${localApiUrl}/api/User/nodes`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${data.token}`,
      }
    })

    console.log('📡 Resposta da API (checkNodeConnection):', { status: response.status, statusText: response.statusText })

    if (response.ok) {
      const responseText = await response.text()
      console.log('✅ Resposta da API (checkNodeConnection):', responseText)
      
      if (!responseText) {
        console.warn('⚠️ Resposta vazia da API')
        return { success: false, error: 'Resposta vazia da API' }
      }
      
      try {
        const result = JSON.parse(responseText)
        console.log('📋 Nós do usuário:', result)
        
        // Verificar se algum nó do usuário tem o mesmo machineId
        const connectedNode = result.find((node: { machineId: string }) => node.machineId === data.machineId)
        console.log('🔍 Nó conectado encontrado:', connectedNode)
        
        return { 
          success: true, 
          isConnected: !!connectedNode,
          node: connectedNode || null
        }
      } catch (parseError) {
        console.error('❌ Erro ao fazer parse da resposta:', parseError)
        return { success: false, error: 'Resposta inválida da API' }
      }
    } else {
      const responseText = await response.text()
      console.error('❌ Erro na API (checkNodeConnection):', { status: response.status, statusText: response.statusText, responseText })
      
      let errorMessage = 'Erro ao verificar conexão do nó'
      
      if (responseText) {
        try {
          const errorData = JSON.parse(responseText)
          errorMessage = errorData.message || errorMessage
        } catch {
          errorMessage = responseText || errorMessage
        }
      }
      
      return { success: false, error: errorMessage }
    }
  } catch (error) {
    console.error('❌ Erro na verificação de conexão do nó:', error)
    return { success: false, error: error instanceof Error ? error.message : String(error) }
  }
})

ipcMain.handle('auth:bindCurrentNode', async (_event, data: { token: string; machineId: string }) => {
  try {
    console.log('🔗 Tentando conectar nó:', { machineId: data.machineId, tokenLength: data.token?.length })
    
    // Verificar se a API está rodando primeiro
    const apiStatus = await apiManager.checkApiStatus()
    if (!apiStatus) {
      console.error('❌ API não está rodando')
      return { success: false, error: 'API não está rodando. Tente reiniciar a aplicação.' }
    }
    
    const localApiUrl = 'http://localhost:5000'
    console.log('🌐 Fazendo requisição para:', `${localApiUrl}/api/Auth/bind-current-node`)
    
    const response = await fetch(`${localApiUrl}/api/Auth/bind-current-node`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${data.token}`,
      },
      body: JSON.stringify({ machineId: data.machineId })
    })

    console.log('📡 Resposta da API:', { status: response.status, statusText: response.statusText })

    if (response.ok) {
      const responseText = await response.text()
      console.log('✅ Resposta da API (sucesso):', responseText)
      
      if (!responseText) {
        console.warn('⚠️ Resposta vazia da API')
        return { success: false, error: 'Resposta vazia da API' }
      }
      
      try {
        const result = JSON.parse(responseText)
        console.log('✅ Nó conectado com sucesso:', result)
        return { success: true, data: result }
      } catch (parseError) {
        console.error('❌ Erro ao fazer parse da resposta:', parseError)
        return { success: false, error: 'Resposta inválida da API' }
      }
    } else {
      const responseText = await response.text()
      console.error('❌ Erro na API:', { status: response.status, statusText: response.statusText, responseText })
      
      let errorMessage = 'Erro ao conectar nó'
      
      if (responseText) {
        try {
          const errorData = JSON.parse(responseText)
          errorMessage = errorData.message || errorMessage
          console.log('📝 Mensagem de erro da API:', errorMessage)
        } catch {
          errorMessage = responseText || errorMessage
          console.log('📝 Erro como texto:', errorMessage)
        }
      }
      
      return { success: false, error: errorMessage }
    }
  } catch (error) {
    console.error('❌ Erro na conexão do nó:', error)
    
    // Verificar se é erro de conexão
    if (error instanceof Error && error.message.includes('fetch')) {
      return { success: false, error: 'Erro de conexão com a API. Verifique se a API está rodando.' }
    }
    
    return { success: false, error: error instanceof Error ? error.message : String(error) }
  }
})

ipcMain.handle('auth:unbindNode', async (_event, data: { token: string; nodeId: string }) => {
  try {
    const localApiUrl = 'http://localhost:5000'
    const response = await fetch(`${localApiUrl}/api/User/unbind-node`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${data.token}`,
      },
      body: JSON.stringify({ nodeId: data.nodeId })
    })

    if (response.ok) {
      const responseText = await response.text()
      if (!responseText) {
        return { success: false, error: 'Resposta vazia da API' }
      }
      
      try {
        const result = JSON.parse(responseText)
        return { success: true, data: result }
      } catch (parseError) {
        console.error('Erro ao fazer parse da resposta:', parseError)
        return { success: false, error: 'Resposta inválida da API' }
      }
    } else {
      const responseText = await response.text()
      let errorMessage = 'Erro ao desconectar nó'
      
      if (responseText) {
        try {
          const errorData = JSON.parse(responseText)
          errorMessage = errorData.message || errorMessage
        } catch {
          errorMessage = responseText || errorMessage
        }
      }
      
      return { success: false, error: errorMessage }
    }
  } catch (error) {
    return { success: false, error: error instanceof Error ? error.message : String(error) }
  }
})

console.log('✅ Handlers IPC registrados com sucesso')

app.whenReady().then(async () => {
  createWindow()

  // Verificar e iniciar a API se necessário
  try {
    console.log('🔍 Verificando status da API...')
    await apiManager.ensureApiRunning()
    
    // Registrar nó anônimo apenas no startup
    await registerNodeOnStartup()
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


