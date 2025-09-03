import { api } from '../api/http'
import { machineId } from 'node-machine-id'

export interface TableSyncData {
  tableName: string
  data: unknown[]
}

export interface SyncStatus {
  id: string
  desktopNodeId: string
  databaseId: string
  lastFullSync: string
  lastIncrementalSync: string
  lastSyncHash?: string
  totalTables: number
  syncedTables: number
  syncStatus: 'PENDING' | 'IN_PROGRESS' | 'COMPLETED' | 'ERROR'
  errorMessage?: string
  createdAt: string
  updatedAt: string
}

export interface DesktopNode {
  id: string
  name: string
  machineId: string
  ipAddress: string
  port: number
  databasePath?: string
  lastSeen: string
  createdAt: string
  isActive: boolean
  lastSyncTimestamp?: string
  version?: string
  operatingSystem?: string
}

export class SyncService {
  private machineId: string = ''
  private syncInterval?: ReturnType<typeof setInterval>
  private isInitialSyncCompleted = false

  constructor() {
    this.initializeMachineId()
  }

  private async initializeMachineId() {
    this.machineId = await machineId()
  }

  /**
   * Registra o nó desktop no servidor via Electron
   */
  async registerDesktopNode(databasePath?: string): Promise<DesktopNode> {
    try {
      console.log('🔄 Registrando nó desktop via Electron com machineId:', this.machineId)
      console.log('📁 DatabasePath fornecido:', databasePath)
      
      // Chamar o Electron para registrar o nó (que já tem as informações corretas)
      const result = await window.system.registerNode(databasePath)
      
      if (result.success && result.data) {
        console.log('Nó desktop registrado com sucesso via Electron:', result.data.name)
        return result.data
      } else {
        throw new Error(result.error || 'Falha ao registrar nó desktop via Electron')
      }
    } catch (error) {
      console.error('Erro ao registrar nó desktop via Electron:', error)
      throw error
    }
  }

  /**
   * Executa sincronização inicial enviando todos os dados em lotes
   */
  async performInitialSync(databaseId: string, tableData: TableSyncData[]): Promise<string> {
    try {
      console.log(`Iniciando sincronização inicial para database ${databaseId} com ${tableData.length} tabelas`)

      const response = await api.request<{ success: boolean; operationId: string }>(
        'POST',
        '/api/Sync/initial-sync',
        {
          desktopNodeId: this.machineId,
          databaseId,
          tableData
        }
      )

      if (response.success) {
        console.log('Sincronização inicial iniciada com sucesso:', response.operationId)
        this.isInitialSyncCompleted = true
        return response.operationId
      } else {
        throw new Error('Falha ao iniciar sincronização inicial')
      }
    } catch (error) {
      console.error('Erro na sincronização inicial:', error)
      throw error
    }
  }

  /**
   * Executa sincronização incremental
   */
  async performIncrementalSync(databaseId: string, tableData: TableSyncData[]): Promise<string> {
    try {
      console.log(`Executando sincronização incremental para database ${databaseId}`)

      const response = await api.request<{ success: boolean; operationId: string }>(
        'POST',
        '/api/Sync/incremental-sync',
        {
          desktopNodeId: this.machineId,
          databaseId,
          tableData
        }
      )

      if (response.success) {
        console.log('Sincronização incremental executada com sucesso:', response.operationId)
        return response.operationId
      } else {
        throw new Error('Falha ao executar sincronização incremental')
      }
    } catch (error) {
      console.error('Erro na sincronização incremental:', error)
      throw error
    }
  }

  /**
   * Obtém dados não sincronizados do servidor
   */
  async getUnsyncedData(databaseId: string): Promise<TableSyncData[]> {
    try {
      const response = await api.request<{ success: boolean; data: TableSyncData[] }>(
        'GET',
        `/api/Sync/unsynced-data/${this.machineId}/${databaseId}`
      )

      if (response.success) {
        console.log(`Encontrados ${response.data.length} conjuntos de dados não sincronizados`)
        return response.data
      } else {
        throw new Error('Falha ao obter dados não sincronizados')
      }
    } catch (error) {
      console.error('Erro ao obter dados não sincronizados:', error)
      return []
    }
  }

  /**
   * Obtém status de sincronização
   */
  async getSyncStatus(databaseId: string): Promise<SyncStatus | null> {
    try {
      const response = await api.request<{ success: boolean; data: SyncStatus }>(
        'GET',
        `/api/Sync/status/${this.machineId}/${databaseId}`
      )

      if (response.success) {
        return response.data
      } else {
        return null
      }
    } catch (error) {
      console.error('Erro ao obter status de sincronização:', error)
      return null
    }
  }

  /**
   * Inicia sincronização periódica (a cada hora)
   */
  startPeriodicSync(databaseId: string, getTableDataCallback: () => Promise<TableSyncData[]>): void {
    if (this.syncInterval) {
      clearInterval(this.syncInterval)
    }

    // Executar sincronização a cada hora (3600000 ms)
    this.syncInterval = setInterval(async () => {
      try {
        console.log('Executando sincronização periódica...')
        
        // Primeiro, obter dados não sincronizados do servidor
        const unsyncedData = await this.getUnsyncedData(databaseId)
        if (unsyncedData.length > 0) {
          console.log(`Recebendo ${unsyncedData.length} conjuntos de dados do servidor`)
          // Aqui você pode processar os dados recebidos do servidor
          // Por exemplo, aplicar as mudanças na base local
        }

        // Depois, enviar dados locais que mudaram
        const localTableData = await getTableDataCallback()
        if (localTableData.length > 0) {
          await this.performIncrementalSync(databaseId, localTableData)
        }

        console.log('Sincronização periódica concluída')
      } catch (error) {
        console.error('Erro na sincronização periódica:', error)
      }
    }, 3600000) // 1 hora

    console.log('Sincronização periódica iniciada (a cada hora)')
  }

  /**
   * Para a sincronização periódica
   */
  stopPeriodicSync(): void {
    if (this.syncInterval) {
      clearInterval(this.syncInterval)
      this.syncInterval = undefined
      console.log('Sincronização periódica parada')
    }
  }



  /**
   * Verifica se a sincronização inicial foi concluída
   */
  isInitialSyncDone(): boolean {
    return this.isInitialSyncCompleted
  }

  /**
   * Obtém o ID da máquina
   */
  getMachineId(): string {
    return this.machineId
  }
}

// Instância singleton
export const syncService = new SyncService()
