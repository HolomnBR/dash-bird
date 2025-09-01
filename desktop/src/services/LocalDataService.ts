import { api } from '../api/http'
import { TableSyncData } from './SyncService'

export interface DatabaseSnapshot {
  id: string
  databaseId: string
  generatedAt: string
  tables: TableInfo[]
}

export interface TableInfo {
  name: string
  schema: any
  recordCount: number
  lastId?: number
  data?: any[]
}

export class LocalDataService {
  private lastSyncTimestamp: string | null = null
  private lastDataHash: Map<string, string> = new Map()

  /**
   * Gera snapshot completo da base de dados
   */
  async generateFullSnapshot(databaseId: string): Promise<DatabaseSnapshot> {
    try {
      console.log(`Gerando snapshot completo para database ${databaseId}`)

      // Obter lista de tabelas
      const tablesResponse = await api.request<{ success: boolean; data: string[] }>(
        'GET',
        `/api/DatabaseConfig/databases/${databaseId}/tables`
      )

      if (!tablesResponse.success) {
        throw new Error('Falha ao obter lista de tabelas')
      }

      const tableNames = tablesResponse.data
      const tables: TableInfo[] = []

      // Para cada tabela, obter dados
      for (const tableName of tableNames) {
        try {
          const tableInfo = await this.getTableData(databaseId, tableName)
          tables.push(tableInfo)
        } catch (error) {
          console.error(`Erro ao obter dados da tabela ${tableName}:`, error)
          // Continua com as outras tabelas mesmo se uma falhar
        }
      }

      const snapshot: DatabaseSnapshot = {
        id: this.generateId(),
        databaseId,
        generatedAt: new Date().toISOString(),
        tables
      }

      console.log(`Snapshot gerado com sucesso: ${tables.length} tabelas processadas`)
      return snapshot
    } catch (error) {
      console.error('Erro ao gerar snapshot completo:', error)
      throw error
    }
  }

  /**
   * Obtém dados de uma tabela específica
   */
  private async getTableData(databaseId: string, tableName: string): Promise<TableInfo> {
    try {
      // Obter schema da tabela
      const schemaResponse = await api.request<{ success: boolean; data: any[] }>(
        'POST',
        `/api/DatabaseConfig/databases/${databaseId}/table-schema`,
        { tableNames: [tableName] }
      )

      if (!schemaResponse.success) {
        throw new Error(`Falha ao obter schema da tabela ${tableName}`)
      }

      // Obter contagem de registros
      const countQuery = `SELECT COUNT(*) as RECORD_COUNT FROM ${tableName}`
      const countResponse = await api.request<{ success: boolean; data: any[] }>(
        'POST',
        '/api/Firebird/execute-query',
        { query: countQuery },
        { databaseId }
      )

      const recordCount = countResponse.success && countResponse.data.length > 0 
        ? parseInt(countResponse.data[0].RECORD_COUNT) 
        : 0

      // Obter último ID (se a tabela tiver campo ID)
      let lastId: number | undefined
      try {
        const lastIdQuery = `SELECT MAX(ID) as LAST_ID FROM ${tableName}`
        const lastIdResponse = await api.request<{ success: boolean; data: any[] }>(
          'POST',
          '/api/Firebird/execute-query',
          { query: lastIdQuery },
          { databaseId }
        )

        if (lastIdResponse.success && lastIdResponse.data.length > 0) {
          lastId = parseInt(lastIdResponse.data[0].LAST_ID) || undefined
        }
      } catch (error) {
        // Tabela pode não ter campo ID, isso é normal
        console.log(`Tabela ${tableName} não possui campo ID`)
      }

      // Obter dados da tabela (limitado para não sobrecarregar)
      const dataQuery = `SELECT * FROM ${tableName} ORDER BY ID DESC ROWS 1000`
      const dataResponse = await api.request<{ success: boolean; data: any[] }>(
        'POST',
        '/api/Firebird/execute-query',
        { query: dataQuery },
        { databaseId }
      )

      const data = dataResponse.success ? dataResponse.data : []

      return {
        name: tableName,
        schema: schemaResponse.data[0],
        recordCount,
        lastId,
        data
      }
    } catch (error) {
      console.error(`Erro ao obter dados da tabela ${tableName}:`, error)
      throw error
    }
  }

  /**
   * Detecta mudanças nos dados desde a última sincronização
   */
  async detectChanges(databaseId: string): Promise<TableSyncData[]> {
    try {
      console.log('Detectando mudanças nos dados locais...')

      // Obter lista de tabelas
      const tablesResponse = await api.request<{ success: boolean; data: string[] }>(
        'GET',
        `/api/DatabaseConfig/databases/${databaseId}/tables`
      )

      if (!tablesResponse.success) {
        throw new Error('Falha ao obter lista de tabelas')
      }

      const tableNames = tablesResponse.data
      const changedTables: TableSyncData[] = []

      for (const tableName of tableNames) {
        try {
          const hasChanges = await this.hasTableChanged(databaseId, tableName)
          if (hasChanges) {
            const tableData = await this.getTableData(databaseId, tableName)
            changedTables.push({
              tableName,
              data: tableData.data || []
            })
          }
        } catch (error) {
          console.error(`Erro ao verificar mudanças na tabela ${tableName}:`, error)
        }
      }

      console.log(`Detectadas mudanças em ${changedTables.length} tabelas`)
      return changedTables
    } catch (error) {
      console.error('Erro ao detectar mudanças:', error)
      return []
    }
  }

  /**
   * Verifica se uma tabela específica mudou desde a última sincronização
   */
  private async hasTableChanged(databaseId: string, tableName: string): Promise<boolean> {
    try {
      // Obter contagem atual e último ID
      const countQuery = `SELECT COUNT(*) as RECORD_COUNT FROM ${tableName}`
      const countResponse = await api.request<{ success: boolean; data: any[] }>(
        'POST',
        '/api/Firebird/execute-query',
        { query: countQuery },
        { databaseId }
      )

      if (!countResponse.success) {
        return false
      }

      const currentRecordCount = parseInt(countResponse.data[0].RECORD_COUNT)

      // Obter último ID
      let lastId: number | undefined
      try {
        const lastIdQuery = `SELECT MAX(ID) as LAST_ID FROM ${tableName}`
        const lastIdResponse = await api.request<{ success: boolean; data: any[] }>(
          'POST',
          '/api/Firebird/execute-query',
          { query: lastIdQuery },
          { databaseId }
        )

        if (lastIdResponse.success && lastIdResponse.data.length > 0) {
          lastId = parseInt(lastIdResponse.data[0].LAST_ID) || undefined
        }
      } catch (error) {
        // Tabela pode não ter campo ID
      }

      // Criar hash dos dados atuais
      const currentHash = this.calculateHash({
        recordCount: currentRecordCount,
        lastId: lastId,
        tableName
      })

      // Verificar se mudou desde a última verificação
      const lastHash = this.lastDataHash.get(`${databaseId}:${tableName}`)
      const hasChanged = lastHash !== currentHash

      if (hasChanged) {
        this.lastDataHash.set(`${databaseId}:${tableName}`, currentHash)
        console.log(`Tabela ${tableName} mudou: ${currentRecordCount} registros, último ID: ${lastId}`)
      }

      return hasChanged
    } catch (error) {
      console.error(`Erro ao verificar mudanças na tabela ${tableName}:`, error)
      return false
    }
  }

  /**
   * Aplica dados recebidos do servidor na base local
   */
  async applyServerData(databaseId: string, tableData: TableSyncData[]): Promise<void> {
    try {
      console.log(`Aplicando ${tableData.length} conjuntos de dados do servidor...`)

      for (const table of tableData) {
        try {
          await this.applyTableData(databaseId, table.tableName, table.data)
        } catch (error) {
          console.error(`Erro ao aplicar dados da tabela ${table.tableName}:`, error)
        }
      }

      console.log('Dados do servidor aplicados com sucesso')
    } catch (error) {
      console.error('Erro ao aplicar dados do servidor:', error)
      throw error
    }
  }

  /**
   * Aplica dados de uma tabela específica
   */
  private async applyTableData(databaseId: string, tableName: string, data: any[]): Promise<void> {
    if (!data || data.length === 0) {
      return
    }

    try {
      // Aqui você implementaria a lógica para aplicar os dados
      // Por exemplo, INSERT/UPDATE dos registros
      // Por simplicidade, apenas logamos os dados
      console.log(`Aplicando ${data.length} registros na tabela ${tableName}`)
      
      // TODO: Implementar lógica de aplicação de dados
      // - Verificar se registro existe (por ID)
      // - Se existe, fazer UPDATE
      // - Se não existe, fazer INSERT
      // - Tratar conflitos de dados
      
    } catch (error) {
      console.error(`Erro ao aplicar dados da tabela ${tableName}:`, error)
      throw error
    }
  }

  /**
   * Calcula hash dos dados
   */
  private calculateHash(data: any): string {
    const json = JSON.stringify(data)
    // Implementação simples de hash (em produção, use uma biblioteca de hash)
    let hash = 0
    for (let i = 0; i < json.length; i++) {
      const char = json.charCodeAt(i)
      hash = ((hash << 5) - hash) + char
      hash = hash & hash // Convert to 32bit integer
    }
    return hash.toString()
  }

  /**
   * Gera ID único
   */
  private generateId(): string {
    return Date.now().toString(36) + Math.random().toString(36).substr(2)
  }

  /**
   * Define timestamp da última sincronização
   */
  setLastSyncTimestamp(timestamp: string): void {
    this.lastSyncTimestamp = timestamp
  }

  /**
   * Obtém timestamp da última sincronização
   */
  getLastSyncTimestamp(): string | null {
    return this.lastSyncTimestamp
  }
}

// Instância singleton
export const localDataService = new LocalDataService()
