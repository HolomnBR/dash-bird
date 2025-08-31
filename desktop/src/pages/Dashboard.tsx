import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import { Settings, Home } from 'lucide-react'
import { Header } from '../components/molecules/Header'
import { DashboardCard } from '../components/molecules/DashboardCard'

import { api } from '../api/http'
import type { QueryRequest } from '../api/types'
import './Dashboard.css'

interface DashboardData {
  productionEfficiency: any[]
  timeManagement: any[]
  inventoryControl: any[]
  marginAnalysis: any[]
}

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

export function Dashboard() {
  const [dashboardData, setDashboardData] = useState<DashboardData>({
    productionEfficiency: [],
    timeManagement: [],
    inventoryControl: [],
    marginAnalysis: []
  })
  const [loading, setLoading] = useState(false)
  const [selectedDatabase, setSelectedDatabase] = useState<string>('')
  const [availableDatabases, setAvailableDatabases] = useState<DatabaseConfig[]>([])

  // Queries SQL para as perguntas estratégicas
  const strategicQueries = {
    // 14.1 - PRODUÇÃO E EFICIÊNCIA
    productionEfficiency: `
      SELECT FIRST 5
        ID_GRADE_PRODUTO as Id
      FROM GRADE_PRODUTO 
      ORDER BY ID_GRADE_PRODUTO DESC
    `,
    
    // 14.2 - GESTÃO DE TEMPO E PRODUTIVIDADE
    timeManagement: `
      SELECT FIRST 5
        ID_RECEITA as IdReceita
      FROM RECEITA 
      ORDER BY ID_RECEITA DESC
    `,
    
    // 14.3 - CONTROLE DE ESTOQUE E PERDAS
    inventoryControl: `
      SELECT FIRST 5
        ID_MOV_ESTOQUE as IdMovimento
      FROM MOV_ESTOQUE 
      ORDER BY ID_MOV_ESTOQUE DESC
    `,
    
    // 14.4 - MARGEM E RENTABILIDADE
    marginAnalysis: `
      SELECT FIRST 5
        ID_GRADE_PRODUTO as Id
      FROM GRADE_PRODUTO 
      ORDER BY ID_GRADE_PRODUTO DESC
    `
  }

  const loadAvailableDatabases = async () => {
    try {
      const response = await api.request<any>('GET', '/api/DatabaseConfig/databases')
      if (response.success && response.data) {
        const databases = response.data.filter((db: DatabaseConfig) => db.isActive)
        setAvailableDatabases(databases)
        
        // Se não há base selecionada, seleciona a primeira disponível
        if (!selectedDatabase && databases.length > 0) {
          setSelectedDatabase(databases[0].id)
        }
      }
    } catch (error) {
      console.error('Erro ao carregar bases de dados:', error)
    }
  }

  const executeQuery = async (query: string, queryName: keyof DashboardData) => {
    if (!selectedDatabase) {
      console.error('Nenhuma base de dados selecionada')
      return
    }

    try {
      setLoading(true)
      const response = await api.request<any>('POST', '/api/Firebird/execute-query', {
        query: query
      } as QueryRequest, { databaseId: selectedDatabase })
      
      if (response.success && response.data) {
        setDashboardData(prev => ({
          ...prev,
          [queryName]: response.data
        }))
      }
    } catch (error) {
      console.error(`Erro ao executar query ${queryName}:`, error)
    } finally {
      setLoading(false)
    }
  }

  const loadDashboardData = async () => {
    setLoading(true)
    try {
      // Executa todas as queries em paralelo
      await Promise.all([
        executeQuery(strategicQueries.productionEfficiency, 'productionEfficiency'),
        executeQuery(strategicQueries.timeManagement, 'timeManagement'),
        executeQuery(strategicQueries.inventoryControl, 'inventoryControl'),
        executeQuery(strategicQueries.marginAnalysis, 'marginAnalysis')
      ])
    } catch (error) {
      console.error('Erro ao carregar dados do dashboard:', error)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadAvailableDatabases()
  }, [])

  useEffect(() => {
    if (selectedDatabase) {
      loadDashboardData()
    }
  }, [selectedDatabase])

    return (
    <div className="dashboard">
      <Header 
        title="Dashboard Estratégico" 
        subtitle="Análise de dados para tomada de decisão estratégica"
        leftSlot={
          <Link 
            to="/" 
            className="header-home-button"
            title="Página Inicial"
            aria-label="Página Inicial"
          >
            <Home size={18} />
          </Link>
        }
      />
      
      <div className="dashboard-container">
        <div className="dashboard-header">
          <h1>Dashboard Estratégico - Perguntas do Dono</h1>
          
          <div className="dashboard-controls">
            <div className="database-selector">
              <label htmlFor="database-select">Base de Dados:</label>
              <select 
                id="database-select"
                value={selectedDatabase}
                onChange={(e) => setSelectedDatabase(e.target.value)}
                disabled={availableDatabases.length === 0}
              >
                {availableDatabases.length === 0 ? (
                  <option value="">Carregando bases...</option>
                ) : (
                  availableDatabases.map((db) => (
                    <option key={db.id} value={db.id}>
                      {db.name} ({db.server}:{db.port})
                    </option>
                  ))
                )}
              </select>
              {availableDatabases.length > 0 && (
                <small className="database-info">
                  {availableDatabases.find(db => db.id === selectedDatabase)?.database || ''}
                </small>
              )}
            </div>
            
            <Link 
              to="/settings" 
              className="settings-button"
              title="Configurações"
              aria-label="Configurações"
            >
              <Settings size={20} />
              <span>Configurações</span>
            </Link>
          </div>
        </div>

        {loading && (
          <div className="loading-overlay">
            <div className="loading-spinner"></div>
            <p>Carregando dados estratégicos...</p>
          </div>
        )}

        <div className="dashboard-grid">
          {/* 14.1 - PRODUÇÃO E EFICIÊNCIA */}
          <DashboardCard
            title="📊 Produção e Eficiência"
            subtitle="Quais são meus produtos mais rentáveis?"
            description="Identifique produtos com maior margem e otimize sua produção"
            data={dashboardData.productionEfficiency}
            columns={['Id']}
            onRefresh={() => executeQuery(strategicQueries.productionEfficiency, 'productionEfficiency')}
          />

          {/* 14.2 - GESTÃO DE TEMPO E PRODUTIVIDADE */}
          <DashboardCard
            title="🕒 Gestão de Tempo e Produtividade"
            subtitle="Quanto tempo leva para produzir cada produto?"
            description="Otimize tempos de produção e identifique gargalos"
            data={dashboardData.timeManagement}
            columns={['IdReceita']}
            onRefresh={() => executeQuery(strategicQueries.timeManagement, 'timeManagement')}
          />

          {/* 14.3 - CONTROLE DE ESTOQUE E PERDAS */}
          <DashboardCard
            title="📈 Controle de Estoque e Perdas"
            subtitle="Quais ingredientes estão vencendo ou em excesso?"
            description="Minimize desperdícios e otimize suas compras"
            data={dashboardData.inventoryControl}
            columns={['IdMovimento']}
            onRefresh={() => executeQuery(strategicQueries.inventoryControl, 'inventoryControl')}
          />

          {/* 14.4 - MARGEM E RENTABILIDADE */}
          <DashboardCard
            title="💰 Margem e Rentabilidade"
            subtitle="Qual é a margem real de cada produto?"
            description="Ajuste preços e foque nos produtos mais lucrativos"
            data={dashboardData.marginAnalysis}
            columns={['Id']}
            onRefresh={() => executeQuery(strategicQueries.marginAnalysis, 'marginAnalysis')}
          />
        </div>

        
      </div>
    </div>
  )
}
