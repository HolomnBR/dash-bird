import { useState, useEffect } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { Header } from '../components/molecules/Header'
import { QueryExecutor } from '../components/molecules/QueryExecutor'
import { ArrowLeft } from 'lucide-react'
import { api } from '../api/http'

interface DatabaseConfig {
  id: string
  name: string
  server: string
  database: string
  username: string
  port: number
  charset: string
  createdAt: string
  isActive: boolean
}

export function QueryTool() {
  const location = useLocation()
  const params = new URLSearchParams(location.search)
  const databaseId = params.get('databaseId') || ''
  
  const [selectedDatabase, setSelectedDatabase] = useState<string>(databaseId)
  const [availableDatabases, setAvailableDatabases] = useState<DatabaseConfig[]>([])
  const [currentDatabaseInfo, setCurrentDatabaseInfo] = useState<DatabaseConfig | null>(null)

  useEffect(() => {
    if (databaseId) {
      loadAvailableDatabases()
    }
  }, [databaseId])

  useEffect(() => {
    if (availableDatabases.length > 0 && selectedDatabase) {
      const dbInfo = availableDatabases.find((db: DatabaseConfig) => db.id === selectedDatabase)
      setCurrentDatabaseInfo(dbInfo || null)
    }
  }, [availableDatabases, selectedDatabase])

  const loadAvailableDatabases = async () => {
    try {
      const response = await api.request<any>('GET', '/api/DatabaseConfig/databases')
      if (response.success && response.data) {
        const databases = response.data.filter((db: DatabaseConfig) => db.isActive)
        setAvailableDatabases(databases)
        
        // Se temos um databaseId na URL, usamos ele
        if (databaseId && databases.find((db: DatabaseConfig) => db.id === databaseId)) {
          setSelectedDatabase(databaseId)
        } else if (databases.length > 0) {
          // Fallback para primeira base disponível
          setSelectedDatabase(databases[0].id)
        }
      }
    } catch (error) {
      console.error('Erro ao carregar bases de dados:', error)
    }
  }

  const headerTitle = currentDatabaseInfo 
    ? `Query Tool - ${currentDatabaseInfo.name}`
    : 'Query Tool'

  const headerSubtitle = currentDatabaseInfo 
    ? `${currentDatabaseInfo.database} (${currentDatabaseInfo.server}:${currentDatabaseInfo.port})`
    : 'Selecionando base de dados...'

  return (
    <main className="mx-auto max-w-5xl">
      <Header
        leftSlot={
          <Link to={`/databases/${selectedDatabase}`} aria-label="Voltar" title="Voltar">
            <ArrowLeft size={18} className="opacity-80 hover:opacity-100" />
          </Link>
        }
        title={headerTitle}
        subtitle={headerSubtitle}
      />
      <div className="p-6">
        <QueryExecutor
          onExecuteQuery={() => {}} // Função vazia já que o QueryExecutor executa internamente
          selectedDatabase={selectedDatabase}
        />
      </div>
    </main>
  )
}


