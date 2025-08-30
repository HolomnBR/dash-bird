import { app, BrowserWindow, shell, ipcMain, dialog } from 'electron'
import { hostname } from 'os'
import { machineId } from 'node-machine-id'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

process.env.ELECTRON_DISABLE_SECURITY_WARNINGS = 'true'

const __dirname = dirname(fileURLToPath(import.meta.url))

let mainWindow: BrowserWindow | null = null

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
    },
  })

  if (!app.isPackaged) {
    mainWindow.loadURL('http://localhost:5173')
    mainWindow.webContents.openDevTools({ mode: 'detach' })
  } else {
    mainWindow.loadFile(join(__dirname, '../dist/index.html'))
  }

  mainWindow.webContents.setWindowOpenHandler(({ url }) => {
    shell.openExternal(url)
    return { action: 'deny' }
  })

  mainWindow.on('closed', () => {
    mainWindow = null
  })
}

app.whenReady().then(() => {
  createWindow()
  ipcMain.handle('system:getInfo', async () => {
    let id = 'unknown'
    try {
      id = await machineId(true)
    } catch {}
    const deviceName = hostname()
    return { deviceName, machineId: id }
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
  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow()
  })
})

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') app.quit()
})


