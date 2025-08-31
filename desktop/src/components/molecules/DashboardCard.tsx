import { useState } from 'react'
import { RefreshCw, TrendingUp, TrendingDown, Minus } from 'lucide-react'
import './DashboardCard.css'

interface DashboardCardProps {
  title: string
  subtitle: string
  description: string
  data: any[]
  columns: string[]
  onRefresh: () => void
}

export function DashboardCard({ 
  title, 
  subtitle, 
  description, 
  data, 
  columns, 
  onRefresh 
}: DashboardCardProps) {
  const [isRefreshing, setIsRefreshing] = useState(false)

  const handleRefresh = async () => {
    setIsRefreshing(true)
    try {
      await onRefresh()
    } finally {
      setIsRefreshing(false)
    }
  }

  const formatValue = (value: any, column: string) => {
    if (value === null || value === undefined) return 'N/A'
    
    // Formatação específica para colunas monetárias
    if (column.toLowerCase().includes('preco') || 
        column.toLowerCase().includes('custo') || 
        column.toLowerCase().includes('margem')) {
      return new Intl.NumberFormat('pt-BR', {
        style: 'currency',
        currency: 'BRL'
      }).format(Number(value))
    }
    
    // Formatação para percentuais
    if (column.toLowerCase().includes('percentual')) {
      return `${Number(value).toFixed(2)}%`
    }
    
    // Formatação para números
    if (typeof value === 'number') {
      return new Intl.NumberFormat('pt-BR').format(value)
    }
    
    return String(value)
  }

  const getTrendIcon = (column: string, value: any) => {
    if (column.toLowerCase().includes('margem') || column.toLowerCase().includes('percentual')) {
      const numValue = Number(value)
      if (numValue > 0) return <TrendingUp className="trend-icon positive" />
      if (numValue < 0) return <TrendingDown className="trend-icon negative" />
      return <Minus className="trend-icon neutral" />
    }
    return null
  }

  return (
    <div className="dashboard-card">
      <div className="card-header">
        <div className="card-title-section">
          <h3 className="card-title">{title}</h3>
          <p className="card-subtitle">{subtitle}</p>
          <p className="card-description">{description}</p>
        </div>
        <button 
          className={`refresh-button ${isRefreshing ? 'refreshing' : ''}`}
          onClick={handleRefresh}
          disabled={isRefreshing}
          title="Atualizar dados"
        >
          <RefreshCw className={`refresh-icon ${isRefreshing ? 'spinning' : ''}`} />
        </button>
      </div>

      <div className="card-content">
        {data.length === 0 ? (
          <div className="no-data">
            <p>Nenhum dado disponível</p>
            <small>Clique em atualizar para carregar os dados</small>
          </div>
        ) : (
          <div className="data-table-container">
            <table className="data-table">
              <thead>
                <tr>
                  {columns.map((column, index) => (
                    <th key={index} className="table-header">
                      {column}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {data.slice(0, 10).map((row, rowIndex) => (
                  <tr key={rowIndex} className="table-row">
                    {columns.map((column, colIndex) => (
                      <td key={colIndex} className="table-cell">
                        <div className="cell-content">
                          {getTrendIcon(column, row[column])}
                          <span className="cell-value">
                            {formatValue(row[column], column)}
                          </span>
                        </div>
                      </td>
                    ))}
                  </tr>
                ))}
              </tbody>
            </table>
            
            {data.length > 10 && (
              <div className="table-footer">
                <small>Mostrando 10 de {data.length} registros</small>
              </div>
            )}
          </div>
        )}
      </div>

      <div className="card-footer">
        <div className="card-stats">
          <span className="stat-item">
            <strong>Total:</strong> {data.length} registros
          </span>
          {data.length > 0 && (
            <span className="stat-item">
              <strong>Última atualização:</strong> {new Date().toLocaleTimeString('pt-BR')}
            </span>
          )}
        </div>
      </div>
    </div>
  )
}
