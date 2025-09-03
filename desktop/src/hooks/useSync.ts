import { useState, useEffect, useCallback } from 'react'
import { syncService } from '../services/SyncService'
import type { SyncStatus } from '../services/SyncService'
import { localDataService } from '../services/LocalDataService'

export interface UseSyncOptions {
  databaseId: string
  autoStart?: boolean
  syncInterval?: number // em milissegundos
}

export interface UseSyncReturn {
  isInitialSyncCompleted: boolean
  isSyncing: boolean
  syncStatus: SyncStatus | null
  lastSyncTime: string | null
  error: string | null
  startInitialSync: () => Promise<void>
  startPeriodicSync: () => void
  stopPeriodicSync: () => void
  performManualSync: () => Promise<void>
  registerNode: () => Promise<void>
}

export function useSync({ 
  databaseId, 
  autoStart = true, 
  // syncInterval = 3600000 // 1 hora por padrão
}: UseSyncOptions): UseSyncReturn {
  const [isInitialSyncCompleted, setIsInitialSyncCompleted] = useState(false)
  const [isSyncing, setIsSyncing] = useState(false)
  const [syncStatus, setSyncStatus] = useState<SyncStatus | null>(null)
  const [lastSyncTime, setLastSyncTime] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  // Registrar nó desktop no servidor
  const registerNode = useCallback(async () => {
    try {
      setError(null)
      await syncService.registerDesktopNode()
      console.log('Nó desktop registrado com sucesso')
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Erro desconhecido'
      setError(`Falha ao registrar nó: ${errorMessage}`)
      console.error('Erro ao registrar nó desktop:', err)
    }
  }, [])

  // Executar sincronização inicial
  const startInitialSync = useCallback(async () => {
    if (isSyncing) {
      console.log('Sincronização já em andamento')
      return
    }

    try {
      setIsSyncing(true)
      setError(null)
      console.log('Iniciando sincronização inicial...')

      // Gerar snapshot completo da base local
      const snapshot = await localDataService.generateFullSnapshot(databaseId)
      
      // Converter snapshot para formato de sincronização
      const tableData = snapshot.tables.map(table => ({
        tableName: table.name,
        data: table.data || []
      }))

      // Enviar dados para o servidor
      const operationId = await syncService.performInitialSync(databaseId, tableData)
      
      setIsInitialSyncCompleted(true)
      setLastSyncTime(new Date().toISOString())
      localDataService.setLastSyncTimestamp(new Date().toISOString())
      
      console.log('Sincronização inicial concluída:', operationId)
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Erro desconhecido'
      setError(`Falha na sincronização inicial: ${errorMessage}`)
      console.error('Erro na sincronização inicial:', err)
    } finally {
      setIsSyncing(false)
    }
  }, [databaseId, isSyncing])

  // Executar sincronização manual
  const performManualSync = useCallback(async () => {
    if (isSyncing) {
      console.log('Sincronização já em andamento')
      return
    }

    try {
      setIsSyncing(true)
      setError(null)
      console.log('Executando sincronização manual...')

      // Detectar mudanças locais
      const changedTables = await localDataService.detectChanges(databaseId)
      
      if (changedTables.length > 0) {
        // Enviar mudanças para o servidor
        await syncService.performIncrementalSync(databaseId, changedTables)
        console.log(`Enviadas mudanças de ${changedTables.length} tabelas`)
      }

      // Obter dados não sincronizados do servidor
      const unsyncedData = await syncService.getUnsyncedData(databaseId)
      if (unsyncedData.length > 0) {
        // Aplicar dados do servidor na base local
        await localDataService.applyServerData(databaseId, unsyncedData)
        console.log(`Aplicados ${unsyncedData.length} conjuntos de dados do servidor`)
      }

      setLastSyncTime(new Date().toISOString())
      localDataService.setLastSyncTimestamp(new Date().toISOString())
      
      console.log('Sincronização manual concluída')
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Erro desconhecido'
      setError(`Falha na sincronização manual: ${errorMessage}`)
      console.error('Erro na sincronização manual:', err)
    } finally {
      setIsSyncing(false)
    }
  }, [databaseId, isSyncing])

  // Iniciar sincronização periódica
  const startPeriodicSync = useCallback(() => {
    console.log('Iniciando sincronização periódica...')
    
    const getTableDataCallback = async () => {
      const changedTables = await localDataService.detectChanges(databaseId)
      return changedTables
    }

    syncService.startPeriodicSync(databaseId, getTableDataCallback)
  }, [databaseId])

  // Parar sincronização periódica
  const stopPeriodicSync = useCallback(() => {
    console.log('Parando sincronização periódica...')
    syncService.stopPeriodicSync()
  }, [])

  // Carregar status de sincronização
  const loadSyncStatus = useCallback(async () => {
    try {
      const status = await syncService.getSyncStatus(databaseId)
      setSyncStatus(status)
      
      if (status) {
        setIsInitialSyncCompleted(status.syncStatus === 'COMPLETED')
        setLastSyncTime(status.lastIncrementalSync)
      }
    } catch (err) {
      console.error('Erro ao carregar status de sincronização:', err)
    }
  }, [databaseId])

  // Efeito para inicialização
  useEffect(() => {
    const initialize = async () => {
      try {
        // Carregar status de sincronização
        await loadSyncStatus()
        
        // Se autoStart estiver habilitado e sincronização inicial não foi concluída
        if (autoStart && !isInitialSyncCompleted) {
          await startInitialSync()
        }
        
        // Se sincronização inicial foi concluída, iniciar sincronização periódica
        if (isInitialSyncCompleted) {
          startPeriodicSync()
        }
      } catch (err) {
        console.error('Erro na inicialização da sincronização:', err)
      }
    }

    initialize()

    // Cleanup
    return () => {
      stopPeriodicSync()
    }
  }, [databaseId, autoStart, loadSyncStatus, startInitialSync, isInitialSyncCompleted, startPeriodicSync, stopPeriodicSync])

  // Efeito para iniciar sincronização periódica quando inicial for concluída
  useEffect(() => {
    if (isInitialSyncCompleted && autoStart) {
      startPeriodicSync()
    }
  }, [isInitialSyncCompleted, autoStart, startPeriodicSync])

  return {
    isInitialSyncCompleted,
    isSyncing,
    syncStatus,
    lastSyncTime,
    error,
    startInitialSync,
    startPeriodicSync,
    stopPeriodicSync,
    performManualSync,
    registerNode
  }
}
