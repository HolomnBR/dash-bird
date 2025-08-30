import { useEffect, useState } from 'react'
import { DatabaseConfigApi } from '../../api'

type Props = { databaseId: string }

type TableMeta = {
  name?: string
  tableName?: string
  schema?: string | null
  type?: string | null
  tableType?: string | null
  description?: string | null
}

export function TableList({ databaseId }: Props) {
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [items, setItems] = useState<TableMeta[]>([])

  useEffect(() => {
    let mounted = true
    setLoading(true)
    DatabaseConfigApi.listTables(databaseId)
      .then((data) => {
        if (!mounted) return
        const wrapped = data as unknown as { data?: unknown }
        const list = Array.isArray(data)
          ? data
          : Array.isArray(wrapped?.data)
            ? (wrapped.data as TableMeta[])
            : []
        setItems(list)
      })
      .catch((err: unknown) => {
        if (!mounted) return
        setError(err instanceof Error ? err.message : String(err))
      })
      .finally(() => mounted && setLoading(false))
    return () => { mounted = false }
  }, [databaseId])

  if (loading) return <div className="text-sm opacity-70">Carregando tabelas...</div>
  if (error) return <div className="text-sm text-red-400">Erro: {error}</div>
  if (items.length === 0) return <div className="text-sm opacity-70">Nenhuma tabela encontrada.</div>

  return (
    <div className="space-y-2">
      {items.map((t) => (
        <div key={(t.name ?? t.tableName) ?? crypto.randomUUID()} className="rounded border border-gray-700/50 px-3 py-2">
          <div className="text-sm font-medium">{t.name ?? t.tableName}</div>
          <div className="text-xs opacity-70">{[t.schema, (t.type ?? t.tableType)].filter(Boolean).join(' • ')}</div>
          {t.description ? (
            <div className="text-xs opacity-60">{t.description}</div>
          ) : null}
        </div>
      ))}
    </div>
  )
}


