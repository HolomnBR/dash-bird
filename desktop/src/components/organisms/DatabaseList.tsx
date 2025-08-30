import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { DatabaseConfigApi } from '../../api'

type Db = {
  id?: string | null
  name?: string | null
  database?: string | null
  server?: string | null
  isActive?: boolean
}

export function DatabaseList() {
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [items, setItems] = useState<Db[]>([])

  useEffect(() => {
    let mounted = true
    DatabaseConfigApi.listDatabases()
      .then((data) => {
        if (!mounted) return
        const wrapped = data as unknown as { data?: unknown }
        const list = Array.isArray(data)
          ? data
          : Array.isArray(wrapped?.data)
            ? (wrapped.data as Db[])
            : []
        setItems(list)
      })
      .catch((err: unknown) => {
        if (!mounted) return
        setError(err instanceof Error ? err.message : String(err))
      })
      .finally(() => mounted && setLoading(false))
    return () => {
      mounted = false
    }
  }, [])

  if (loading) return <div className="text-sm opacity-70">Carregando bases...</div>
  if (error) return <div className="text-sm text-red-400">Erro: {error}</div>
  if (items.length === 0) return <div className="text-sm opacity-70">Nenhuma base configurada.</div>

  return (
    <div className="space-y-2">
      {items.map((db) => {
        const id = db.id ?? db.database ?? crypto.randomUUID()
        return (
          <Link key={id} to={`/databases/${db.id ?? ''}`} className="block rounded border border-gray-700/50 px-3 py-2 hover:border-gray-500">
            <div className="text-sm font-medium">{db.name || db.database || '(sem nome)'}</div>
            <div className="text-xs opacity-70">{db.server || 'localhost'}</div>
          </Link>
        )
      })}
    </div>
  )
}


