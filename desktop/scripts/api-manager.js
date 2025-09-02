import { spawn } from 'child_process';
import fs from 'fs';
import path from 'path';
import http from 'http';
import { dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));

class ApiManager {
  constructor() {
    this.apiProcess = null;
    this.apiPort = 8000; // Porta padrão da API
    this.isRunning = false;
    this.apiExePath = null; // Será inicializado pelo método init()
  }

  init() {
    // Em desenvolvimento, usa o caminho local
    // Em produção, usa o caminho dos recursos do Electron
    if (process.env.NODE_ENV === 'development' || !process.resourcesPath) {
      this.apiExePath = path.join(__dirname, '../api-dist/FirebirdApi.exe');
    } else {
      this.apiExePath = path.join(process.resourcesPath, 'api-dist/FirebirdApi.exe');
    }
    
    console.log('🔧 Caminho do executável da API definido como:', this.apiExePath);
  }



  // Verificar se a API está rodando
  async checkApiStatus() {
    return new Promise((resolve) => {
      const req = http.request({
        hostname: 'localhost',
        port: this.apiPort,
        path: '/health', // Endpoint de health check
        method: 'GET',
        timeout: 2000
      }, (res) => {
        resolve(res.statusCode === 200);
      });

      req.on('error', () => {
        resolve(false);
      });

      req.on('timeout', () => {
        req.destroy();
        resolve(false);
      });

      req.end();
    });
  }

  // Iniciar a API
  async startApi() {
    if (this.apiProcess) {
      console.log('API já está rodando');
      return;
    }

    if (!this.apiExePath || !fs.existsSync(this.apiExePath)) {
      throw new Error(`Executável da API não encontrado. Caminho: ${this.apiExePath}`);
    }

    console.log('🚀 Iniciando API a partir do diretório do usuário...');

    this.apiProcess = spawn(this.apiExePath, [], {
      cwd: path.dirname(this.apiExePath),
      stdio: ['pipe', 'pipe', 'pipe'],
      detached: false
    });

    // Logs da API
    this.apiProcess.stdout.on('data', (data) => {
      console.log(`[API] ${data.toString().trim()}`);
    });

    this.apiProcess.stderr.on('data', (data) => {
      console.error(`[API ERROR] ${data.toString().trim()}`);
    });

    // Quando a API terminar
    this.apiProcess.on('close', (code) => {
      console.log(`API encerrada com código: ${code}`);
      this.apiProcess = null;
      this.isRunning = false;
    });

    // Aguardar a API estar pronta
    await this.waitForApiReady();
    this.isRunning = true;
    console.log('✅ API iniciada com sucesso!');
  }

  // Aguardar a API estar pronta
  async waitForApiReady(maxAttempts = 30) {
    for (let i = 0; i < maxAttempts; i++) {
      if (await this.checkApiStatus()) {
        return true;
      }
      await new Promise(resolve => setTimeout(resolve, 1000));
    }
    throw new Error('API não iniciou dentro do tempo esperado');
  }

  // Parar a API
  stopApi() {
    if (this.apiProcess) {
      console.log('🛑 Parando API...');
      this.apiProcess.kill();
      this.apiProcess = null;
      this.isRunning = false;
    }
  }

  // Verificar e iniciar a API se necessário
  async ensureApiRunning() {
    try {
      const isRunning = await this.checkApiStatus();
      
      if (!isRunning) {
        console.log('API não está rodando. Iniciando...');
        await this.startApi();
      } else {
        console.log('API já está rodando');
      }
      
      return true;
    } catch (error) {
      console.error('Erro ao verificar/iniciar API:', error.message);
      return false;
    }
  }

  // Obter status da API
  getStatus() {
    return {
      isRunning: this.isRunning,
      port: this.apiPort,
      processId: this.apiProcess?.pid
    };
  }
}

export default ApiManager;
