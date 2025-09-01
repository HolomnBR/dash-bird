import React from 'react'
import { useSync } from '../../hooks/useSync'
import { Button } from '../atoms/Button'
import { 
  Cloud, 
  CloudOff, 
  RefreshCw, 
  CheckCircle, 
  AlertCircle, 
  Clock,
  Database,
  Wifi,
  WifiOff
} from 'lucide-react'

interface SyncStatusProps {
  databaseId: string
  className?: string
}

export function SyncStatus({ databaseId, className = '' }: SyncStatusProps) {
  const {
    isInitialSyncCompleted,
    isSyncing,
    syncStatus,
    lastSyncTime,
    error,
    startInitialSync,
    performManualSync,
    registerNode
  } = useSync({ databaseId })

  const getStatusIcon = () => {
    if (error) return <AlertCircle className="w-4 h-4 text-red-500" />
    if (isSyncing) return <RefreshCw className="w-4 h-4 text-blue-500 animate-spin" />
    if (isInitialSyncCompleted) return <CheckCircle className="w-4 h-4 text-green-500" />
    return <Clock className="w-4 h-4 text-yellow-500" />
  }

  const getStatusText = () => {
    if (error) return 'Erro na sincronização'
    if (isSyncing) return 'Sincronizando...'
    if (isInitialSyncCompleted) return 'Sincronizado'
    return 'Aguardando sincronização inicial'
  }

  const getStatusColor = () => {
    if (error) return 'text-red-600'
    if (isSyncing) return 'text-blue-600'
    if (isInitialSyncCompleted) return 'text-green-600'
    return 'text-yellow-600'
  }

  const formatLastSyncTime = (timestamp: string | null) => {
    if (!timestamp) return 'Nunca'
    
    const date = new Date(timestamp)
    const now = new Date()
    const diffMs = now.getTime() - date.getTime()
    const diffMins = Math.floor(diffMs / 60000)
    const diffHours = Math.floor(diffMins / 60)
    const diffDays = Math.floor(diffHours / 24)

    if (diffMins < 1) return 'Agora mesmo'
    if (diffMins < 60) return `${diffMins} min atrás`
    if (diffHours < 24) return `${diffHours}h atrás`
    return `${diffDays}d atrás`
  }

  return (
    <div className={`bg-white rounded-lg border border-gray-200 p-4 ${className}`}>
      <div className="flex items-center justify-between mb-3">
        <div className="flex items-center space-x-2">
          <Database className="w-5 h-5 text-gray-600" />
          <h3 className="text-lg font-semibold text-gray-800">Status de Sincronização</h3>
        </div>
        <div className="flex items-center space-x-2">
          {getStatusIcon()}
          <span className={`text-sm font-medium ${getStatusColor()}`}>
            {getStatusText()}
          </span>
        </div>
      </div>

      {error && (
        <div className="mb-3 p-3 bg-red-50 border border-red-200 rounded-md">
          <div className="flex items-center space-x-2">
            <AlertCircle className="w-4 h-4 text-red-500" />
            <span className="text-sm text-red-700">{error}</span>
          </div>
        </div>
      )}

      <div className="space-y-2 text-sm text-gray-600">
        <div className="flex items-center justify-between">
          <span className="flex items-center space-x-2">
            <Wifi className="w-4 h-4" />
            <span>Última sincronização:</span>
          </span>
          <span className="font-medium">
            {formatLastSyncTime(lastSyncTime)}
          </span>
        </div>

        {syncStatus && (
          <>
            <div className="flex items-center justify-between">
              <span>Tabelas sincronizadas:</span>
              <span className="font-medium">
                {syncStatus.syncedTables} / {syncStatus.totalTables}
              </span>
            </div>
            
            <div className="flex items-center justify-between">
              <span>Status do servidor:</span>
              <span className={`font-medium ${
                syncStatus.syncStatus === 'COMPLETED' ? 'text-green-600' :
                syncStatus.syncStatus === 'ERROR' ? 'text-red-600' :
                'text-yellow-600'
              }`}>
                {syncStatus.syncStatus}
              </span>
            </div>
          </>
        )}
      </div>

      <div className="mt-4 flex space-x-2">
        {!isInitialSyncCompleted && (
          <Button
            onClick={startInitialSync}
            disabled={isSyncing}
            className="flex items-center space-x-2"
          >
            <Cloud className="w-4 h-4" />
            <span>Iniciar Sincronização</span>
          </Button>
        )}

        <Button
          onClick={performManualSync}
          disabled={isSyncing}
          variant="outline"
          className="flex items-center space-x-2"
        >
          <RefreshCw className={`w-4 h-4 ${isSyncing ? 'animate-spin' : ''}`} />
          <span>Sincronizar Agora</span>
        </Button>

        <Button
          onClick={registerNode}
          variant="outline"
          className="flex items-center space-x-2"
        >
          <WifiOff className="w-4 h-4" />
          <span>Reconectar</span>
        </Button>
      </div>

      {isInitialSyncCompleted && (
        <div className="mt-3 p-2 bg-green-50 border border-green-200 rounded-md">
          <div className="flex items-center space-x-2">
            <CheckCircle className="w-4 h-4 text-green-500" />
            <span className="text-sm text-green-700">
              Sincronização automática ativa (a cada hora)
            </span>
          </div>
        </div>
      )}
    </div>
  )
}
