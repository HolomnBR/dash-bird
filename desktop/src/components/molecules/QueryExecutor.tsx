import React, { useState } from 'react'
import { Play, Save, FileText, AlertCircle } from 'lucide-react'
import { api } from '../../api/http'
import type { QueryRequest } from '../../api/types'
import './QueryExecutor.css'

interface QueryExecutorProps {
  onExecuteQuery: (query: string) => void
  selectedDatabase: string
}

export function QueryExecutor({ onExecuteQuery, selectedDatabase }: QueryExecutorProps) {
  const [query, setQuery] = useState('')
  const [isExecuting, setIsExecuting] = useState(false)
  const [result, setResult] = useState<Record<string, unknown>[]>([])
  const [error, setError] = useState<string | null>(null)
  const [savedQueries, setSavedQueries] = useState<string[]>([])
  const [showSavedQueries, setShowSavedQueries] = useState(false)

  const executeQuery = async () => {
    if (!query.trim()) {
      setError('Digite uma consulta SQL válida')
      return
    }

    if (!selectedDatabase) {
      setError('Selecione uma base de dados primeiro')
      return
    }

    setIsExecuting(true)
    setError(null)
    setResult([])

    try {
      const response = await api.request<any>('POST', '/api/Firebird/execute-query', {
        query: query.trim()
      } as QueryRequest, { databaseId: selectedDatabase })

      if (response.success && response.data) {
        setResult(response.data)
        onExecuteQuery(query.trim())
      } else {
        setError('Erro ao executar a consulta')
      }
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Erro desconhecido ao executar a consulta'
      setError(errorMessage)
    } finally {
      setIsExecuting(false)
    }
  }

  const saveQuery = () => {
    if (query.trim() && !savedQueries.includes(query.trim())) {
      setSavedQueries(prev => [...prev, query.trim()])
      localStorage.setItem('savedQueries', JSON.stringify([...savedQueries, query.trim()]))
    }
  }

  const loadSavedQueries = () => {
    const saved = localStorage.getItem('savedQueries')
    if (saved) {
      setSavedQueries(JSON.parse(saved))
    }
  }



  const deleteSavedQuery = (index: number) => {
    const newSaved = savedQueries.filter((_, i) => i !== index)
    setSavedQueries(newSaved)
    localStorage.setItem('savedQueries', JSON.stringify(newSaved))
  }

  React.useEffect(() => {
    loadSavedQueries()
  }, [])

  const formatValue = (value: unknown) => {
    if (value === null || value === undefined) return 'N/A'
    if (typeof value === 'number') {
      return new Intl.NumberFormat('pt-BR').format(value)
    }
    return String(value)
  }

  return (
    <div className="query-executor">
      <div className="query-header">
        <h4>🔍 Executor de Consultas SQL</h4>
        <p>Execute consultas personalizadas para análise avançada</p>
      </div>

      <div className="query-controls">
        <div className="query-input-section">
          <div className="query-input-header">
            <label htmlFor="sql-query">Consulta SQL:</label>
            <div className="query-actions">
              <button
                className="action-button save-button"
                onClick={saveQuery}
                title="Salvar consulta"
                disabled={!query.trim()}
              >
                <Save size={16} />
                Salvar
              </button>
              <button
                className="action-button saved-button"
                onClick={() => setShowSavedQueries(!showSavedQueries)}
                title="Consultas salvas"
              >
                <FileText size={16} />
                Salvas ({savedQueries.length})
              </button>
            </div>
          </div>
          
          <textarea
            id="sql-query"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Digite sua consulta SQL aqui... Ex: SELECT FIRST 10 * FROM PRODUTO WHERE IS_ATIVO = 1"
            className="query-textarea"
            rows={6}
          />
          
          <button
            className="execute-button"
            onClick={executeQuery}
            disabled={isExecuting || !query.trim()}
          >
            <Play size={18} />
            {isExecuting ? 'Executando...' : 'Executar Consulta'}
          </button>

          <div className="query-examples">
            <small>Exemplos de consultas:</small>
            <div className="example-queries">
              <button 
                className="example-query-btn"
                onClick={() => setQuery("SELECT FIRST 5 ID_GRADE_PRODUTO FROM GRADE_PRODUTO")}
              >
                Primeiros 5 produtos
              </button>
              <button 
                className="example-query-btn"
                onClick={() => setQuery("SELECT FIRST 5 ID_RECEITA FROM RECEITA")}
              >
                Primeiras 5 receitas
              </button>
              <button 
                className="example-query-btn"
                onClick={() => setQuery("SELECT FIRST 5 ID_MOV_ESTOQUE FROM MOV_ESTOQUE")}
              >
                Primeiros 5 movimentos
              </button>
            </div>
          </div>
        </div>

        {showSavedQueries && savedQueries.length > 0 && (
          <div className="saved-queries">
            <h5>📚 Consultas Salvas:</h5>
            <div className="saved-queries-list">
              {savedQueries.map((savedQuery, index) => (
                <div key={index} className="saved-query-item">
                  <div className="saved-query-text" onClick={() => {
                    setQuery(savedQuery)
                    setShowSavedQueries(false)
                  }}>
                    {savedQuery.length > 80 ? `${savedQuery.substring(0, 80)}...` : savedQuery}
                  </div>
                  <button
                    className="delete-saved-query"
                    onClick={() => deleteSavedQuery(index)}
                    title="Excluir consulta salva"
                  >
                    ×
                  </button>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>

      {error && (
        <div className="error-message">
          <AlertCircle size={20} />
          <span>{error}</span>
        </div>
      )}

      {result.length > 0 && (
        <div className="query-results">
          <h5>📊 Resultados da Consulta:</h5>
          <div className="results-table-container">
            <table className="results-table">
              <thead>
                <tr>
                  {Object.keys(result[0] || {}).map((column, index) => (
                    <th key={index} className="result-header">
                      {column}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {result.slice(0, 20).map((row, rowIndex) => (
                  <tr key={rowIndex} className="result-row">
                    {Object.values(row).map((value, colIndex) => (
                      <td key={colIndex} className="result-cell">
                        {formatValue(value)}
                      </td>
                    ))}
                  </tr>
                ))}
              </tbody>
            </table>
            
            {result.length > 20 && (
              <div className="results-footer">
                <small>Mostrando 20 de {result.length} registros</small>
              </div>
            )}
          </div>
        </div>
      )}

      <div className="query-tips">
        <h5>💡 Dicas para Consultas Estratégicas:</h5>
        <ul>
          <li><strong>Produtos mais rentáveis:</strong> Analise margens e custos</li>
          <li><strong>Controle de estoque:</strong> Monitore quantidades e status</li>
          <li><strong>Análise de vendas:</strong> Identifique tendências e sazonalidade</li>
          <li><strong>Gestão de tempo:</strong> Otimize processos de produção</li>
        </ul>
      </div>
    </div>
  )
}
