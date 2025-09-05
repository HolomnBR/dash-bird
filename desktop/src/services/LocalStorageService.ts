/**
 * Serviço para gerenciar armazenamento local usando SQLite via API
 */
export class LocalStorageService {
  private static instance: LocalStorageService;
  private baseUrl: string;

  private constructor() {
    this.baseUrl = 'http://localhost:8000/api';
  }

  public static getInstance(): LocalStorageService {
    if (!LocalStorageService.instance) {
      LocalStorageService.instance = new LocalStorageService();
    }
    return LocalStorageService.instance;
  }

  /**
   * Salvar token de autenticação
   */
  async saveToken(token: string, userId: string, userEmail?: string): Promise<boolean> {
    try {
      const response = await fetch(`${this.baseUrl}/Token/save-token`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          token,
          userId,
          userEmail
        })
      });

      if (response.ok) {
        // Também salvar no localStorage como backup
        localStorage.setItem('auth_token', token);
        localStorage.setItem('user_id', userId);
        if (userEmail) {
          localStorage.setItem('user_email', userEmail);
        }
        return true;
      }
      return false;
    } catch (error) {
      console.error('Erro ao salvar token:', error);
      return false;
    }
  }

  /**
   * Obter token de autenticação
   */
  async getToken(): Promise<string | null> {
    try {
      // Primeiro tentar API
      const response = await fetch(`${this.baseUrl}/Token/get-token`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        }
      });

      if (response.ok) {
        const data = await response.json();
        if (data.token) {
          // Atualizar localStorage como backup
          localStorage.setItem('auth_token', data.token);
          return data.token;
        }
      }

      // Fallback para localStorage
      return localStorage.getItem('auth_token');
    } catch (error) {
      console.error('Erro ao obter token:', error);
      // Fallback para localStorage
      return localStorage.getItem('auth_token');
    }
  }

  /**
   * Limpar token de autenticação
   */
  async clearToken(): Promise<boolean> {
    try {
      const response = await fetch(`${this.baseUrl}/Token/clear-token`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        }
      });

      // Limpar localStorage também
      localStorage.removeItem('auth_token');
      localStorage.removeItem('user_id');
      localStorage.removeItem('user_email');
      localStorage.removeItem('user_data');
      localStorage.removeItem('connected_nodes');
      localStorage.removeItem('anonymous_token');

      return response.ok;
    } catch (error) {
      console.error('Erro ao limpar token:', error);
      // Limpar localStorage mesmo se a API falhar
      localStorage.removeItem('auth_token');
      localStorage.removeItem('user_id');
      localStorage.removeItem('user_email');
      localStorage.removeItem('user_data');
      localStorage.removeItem('connected_nodes');
      localStorage.removeItem('anonymous_token');
      return false;
    }
  }

  /**
   * Salvar configuração de banco de dados
   */
  async saveDatabaseConfig(config: Record<string, unknown>): Promise<boolean> {
    try {
      const response = await fetch(`${this.baseUrl}/Database/save-config`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(config)
      });

      return response.ok;
    } catch (error) {
      console.error('Erro ao salvar configuração de banco:', error);
      return false;
    }
  }

  /**
   * Obter configurações de banco de dados
   */
  async getDatabaseConfigs(): Promise<Record<string, unknown>[]> {
    try {
      const response = await fetch(`${this.baseUrl}/Database/get-configs`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        }
      });

      if (response.ok) {
        const data = await response.json();
        return data.configs || [];
      }
      return [];
    } catch (error) {
      console.error('Erro ao obter configurações de banco:', error);
      return [];
    }
  }

  /**
   * Salvar snapshot de banco de dados
   */
  async saveSnapshot(snapshot: Record<string, unknown>): Promise<boolean> {
    try {
      const response = await fetch(`${this.baseUrl}/Database/save-snapshot`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(snapshot)
      });

      return response.ok;
    } catch (error) {
      console.error('Erro ao salvar snapshot:', error);
      return false;
    }
  }

  /**
   * Obter snapshots de banco de dados
   */
  async getSnapshots(databaseId?: string): Promise<Record<string, unknown>[]> {
    try {
      const url = databaseId 
        ? `${this.baseUrl}/Database/get-snapshots?databaseId=${databaseId}`
        : `${this.baseUrl}/Database/get-snapshots`;
        
      const response = await fetch(url, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        }
      });

      if (response.ok) {
        const data = await response.json();
        return data.snapshots || [];
      }
      return [];
    } catch (error) {
      console.error('Erro ao obter snapshots:', error);
      return [];
    }
  }

  /**
   * Verificar se há dados armazenados localmente
   */
  async hasStoredData(): Promise<boolean> {
    try {
      const token = await this.getToken();
      return !!token;
    } catch (error) {
      console.error('Erro ao verificar dados armazenados:', error);
      return false;
    }
  }

  /**
   * Salvar nó local com informações do sistema
   */
  async saveLocalNode(nodeData: {
    machineName: string;
    operatingSystem?: string;
    systemVersion?: string;
    architecture?: string;
    ipAddress?: string;
    port?: number;
  }): Promise<{ success: boolean; data?: Record<string, unknown>; message?: string }> {
    try {
      const response = await fetch(`${this.baseUrl}/LocalNode/save-local-node`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(nodeData)
      });

      if (response.ok) {
        const result = await response.json();
        return result;
      } else {
        const error = await response.json();
        return { success: false, message: error.message || 'Erro ao salvar nó local' };
      }
    } catch (error) {
      console.error('Erro ao salvar nó local:', error);
      return { success: false, message: 'Erro de conexão ao salvar nó local' };
    }
  }

  /**
   * Obter nó local por nome da máquina
   */
  async getLocalNodeByMachine(machineName: string): Promise<{ success: boolean; data?: Record<string, unknown>; message?: string }> {
    try {
      const response = await fetch(`${this.baseUrl}/LocalNode/by-machine/${encodeURIComponent(machineName)}`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        }
      });

      if (response.ok) {
        const result = await response.json();
        return result;
      } else {
        const error = await response.json();
        return { success: false, message: error.message || 'Nó local não encontrado' };
      }
    } catch (error) {
      console.error('Erro ao obter nó local:', error);
      return { success: false, message: 'Erro de conexão ao obter nó local' };
    }
  }

  /**
   * Obter todos os nós locais
   */
  async getAllLocalNodes(): Promise<{ success: boolean; data?: Record<string, unknown>[]; count?: number; message?: string }> {
    try {
      const response = await fetch(`${this.baseUrl}/LocalNode`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        }
      });

      if (response.ok) {
        const result = await response.json();
        return result;
      } else {
        const error = await response.json();
        return { success: false, message: error.message || 'Erro ao obter nós locais' };
      }
    } catch (error) {
      console.error('Erro ao obter nós locais:', error);
      return { success: false, message: 'Erro de conexão ao obter nós locais' };
    }
  }

  /**
   * Verificar se existe nó local para a máquina atual
   */
  async hasLocalNode(machineName: string): Promise<{ success: boolean; data?: { machineName: string; exists: boolean }; message?: string }> {
    try {
      const response = await fetch(`${this.baseUrl}/LocalNode/exists/${encodeURIComponent(machineName)}`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        }
      });

      if (response.ok) {
        const result = await response.json();
        return result;
      } else {
        const error = await response.json();
        return { success: false, message: error.message || 'Erro ao verificar existência do nó local' };
      }
    } catch (error) {
      console.error('Erro ao verificar existência do nó local:', error);
      return { success: false, message: 'Erro de conexão ao verificar existência do nó local' };
    }
  }

  /**
   * Obter informações do sistema e salvar nó local
   */
  async getSystemInfoAndSaveNode(): Promise<{ success: boolean; data?: Record<string, unknown>; message?: string }> {
    try {
      // Primeiro, obter informações do sistema
      const systemInfoResponse = await fetch(`${this.baseUrl}/DatabaseConfig/system-info`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        }
      });

      if (!systemInfoResponse.ok) {
        return { success: false, message: 'Erro ao obter informações do sistema' };
      }

      const systemInfo = await systemInfoResponse.json();
      
      if (!systemInfo.success || !systemInfo.data) {
        return { success: false, message: 'Dados do sistema não disponíveis' };
      }

      // Extrair informações do sistema
      const { operatingSystem, systemVersion, machineName, architecture } = systemInfo.data;

      // Verificar se já existe nó para esta máquina
      const hasNodeResult = await this.hasLocalNode(machineName);
      
      if (hasNodeResult.success && hasNodeResult.data?.exists) {
        // Nó já existe, obter dados atuais
        const existingNodeResult = await this.getLocalNodeByMachine(machineName);
        return existingNodeResult;
      } else {
        // Criar novo nó com informações do sistema
        const nodeData = {
          machineName,
          operatingSystem,
          systemVersion,
          architecture,
          port: 8000
        };

        const saveResult = await this.saveLocalNode(nodeData);
        return saveResult;
      }
    } catch (error) {
      console.error('Erro ao obter informações do sistema e salvar nó:', error);
      return { success: false, message: 'Erro ao processar informações do sistema' };
    }
  }

}

export const localStorageService = LocalStorageService.getInstance();
