import { spawn } from 'child_process';
import fs from 'fs';
import path from 'path';
import http from 'http';

class ApiManager {
  constructor(app) {
    this.app = app;
    this.apiProcess = null;
    this.apiPort = 5000; // Porta padrão da API
    this.isRunning = false;
    this.apiExePath = null; // Será inicializado pelo método init()
  }

  init() {
    // Determina o caminho de origem da API
    const isDev = !this.app.isPackaged;
    const sourceApiDistPath = isDev
      ? path.join(this.app.getAppPath(), 'api-dist') // Em dev, fica na raiz do projeto
      : path.join(process.resourcesPath, 'api-dist'); // Em produção, fica nos recursos do app

    // Determina o caminho de destino (gravável) na pasta de dados do usuário
    const userDataPath = this.app.getPath('userData');
    const destinationApiDistPath = path.join(userDataPath, 'api-dist');

    // O caminho final do executável que será usado
    this.apiExePath = path.join(destinationApiDistPath, 'FirebirdApi.exe');

    // Log dos caminhos para facilitar o debug
    console.log('💡 Ambiente:', isDev ? 'Desenvolvimento' : 'Produção');
    console.log('🔍 Caminho de origem da API:', sourceApiDistPath);
    console.log('🔍 Caminho de destino da API:', destinationApiDistPath);
    console.log('🔧 Caminho do executável da API definido como:', this.apiExePath);

    // Garante que a API esteja "instalada" no local gravável
    this.ensureApiIsInstalled(sourceApiDistPath, destinationApiDistPath);
  }

  // Copia a API para um local gravável se for a primeira execução ou uma atualização
  ensureApiIsInstalled(sourcePath, destinationPath) {
    if (!fs.existsSync(sourcePath)) {
      console.error(`❌ CRÍTICO: Diretório de origem da API não encontrado: ${sourcePath}`);
      return;
    }

    // Uma verificação simples: se o executável não existe no destino, copiamos tudo.
    // Para atualizações, uma lógica de comparação de versão seria ideal no futuro.
    if (!fs.existsSync(this.apiExePath)) {
      console.log('🔧 Primeira execução ou API desatualizada. Instalando em diretório de usuário...');
      if (fs.existsSync(destinationPath)) {
        fs.rmSync(destinationPath, { recursive: true, force: true });
      }
      fs.mkdirSync(destinationPath, { recursive: true });
      console.log(`📂 Copiando de "${sourcePath}" para "${destinationPath}"...`);
      fs.cpSync(sourcePath, destinationPath, { recursive: true });
      console.log('✅ API copiada com sucesso.');
    } else {
      console.log('✅ API já está instalada no diretório do usuário.');
    }
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
      throw new Error(`Executável da API não encontrado. Caminho: ${this.apiExePath}. O método init() foi chamado?`);
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
