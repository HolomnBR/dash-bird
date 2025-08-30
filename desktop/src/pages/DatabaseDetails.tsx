import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { Header } from '../components/molecules/Header'
import { ArrowLeft, Trash2 } from 'lucide-react'
import { DatabaseConfigApi } from '../api'
import type { DatabaseConfig, AddDatabaseRequest } from '../api'
import { Button } from '../components/atoms/Button'
import { TableList } from '../components/organisms/TableList'

export function DatabaseDetails() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()

  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [db, setDb] = useState<DatabaseConfig | null>(null)
  const [message, setMessage] = useState<string | null>(null)
  const isCreate = !id || id === 'new'
  const [showTables, setShowTables] = useState(false)

  const [form, setForm] = useState<AddDatabaseRequest>({
    name: '',
    database: '',
    server: 'localhost',
    username: 'SYSDBA',
    password: 'masterkey',
    port: 3050,
    charset: 'UTF8',
  })

  useEffect(() => {
    if (isCreate) {
      setLoading(false)
      return
    }
    if (!id) {
      setError('ID inválido')
      setLoading(false)
      return
    }
    DatabaseConfigApi.getDatabase(id)
      .then((data) => {
        const wrapped = data as unknown as { data?: unknown }
        const obj = (wrapped?.data as DatabaseConfig) ?? (data as DatabaseConfig)
        setDb(obj)
      })
      .catch((err: unknown) => setError(err instanceof Error ? err.message : String(err)))
      .finally(() => setLoading(false))
  }, [id, isCreate])

  async function handleTest() {
    if (!id) return
    setMessage(null)
    try {
      const res = await DatabaseConfigApi.testConnection(id)
      const msg = (res as unknown as { message?: string })?.message
      setMessage(msg || 'Conexão OK')
    } catch (e) {
      setMessage(e instanceof Error ? e.message : String(e))
    }
  }

  function openTables() {
    if (!id) return
    setShowTables(true)
  }

  function openQueryTool() {
    navigate(`/query-tool${id ? `?databaseId=${encodeURIComponent(id)}` : ''}`)
  }

  async function handleSave() {
    setMessage(null)
    try {
      const result = await DatabaseConfigApi.addDatabase(form)
      const created = (result as unknown as { data?: DatabaseConfig })?.data ?? (result as DatabaseConfig)
      const newId = created?.id
      if (newId) navigate(`/databases/${newId}`)
      else setMessage('Salvo')
    } catch (e) {
      setMessage(e instanceof Error ? e.message : String(e))
    }
  }

  async function handleRemove() {
    if (!id) return
    const ok = confirm('Remover esta base de dados?')
    if (!ok) return
    setMessage(null)
    try {
      await DatabaseConfigApi.removeDatabase(id)
      navigate('/')
    } catch (e) {
      setMessage(e instanceof Error ? e.message : String(e))
    }
  }

  return (
    <main className="mx-auto max-w-3xl">
      <Header
        leftSlot={
          <Link to="/" aria-label="Voltar" title="Voltar">
            <ArrowLeft size={18} className="opacity-80 hover:opacity-100" />
          </Link>
        }
        title={isCreate ? 'Nova base de dados' : (db?.name || db?.database || 'Base de dados')}
        subtitle={isCreate ? '' : (db?.server ? `${db.server}` : '')}
        rightSlot={!isCreate ? (
          <button
            className="text-red-500 hover:text-red-400"
            onClick={handleRemove}
            title="Remover"
            aria-label="Remover"
          >
            <Trash2 size={18} />
          </button>
        ) : null}
      />
      <div className="p-6 space-y-4">
        {loading && <div className="text-sm opacity-70">Carregando...</div>}
        {error && <div className="text-sm text-red-400">Erro: {error}</div>}
        {!loading && !error && !isCreate && db && (
          <div className="space-y-3 text-sm">
            <div><span className="opacity-70">Servidor:</span> {db.server}</div>
            <div><span className="opacity-70">Database:</span> {db.database}</div>
            <div className="grid grid-cols-2 gap-3">
              <div><span className="opacity-70">Porta:</span> {db.port}</div>
              <div><span className="opacity-70">Usuário:</span> {db.username}</div>
              <div><span className="opacity-70">Charset:</span> {db.charset}</div>
              <div><span className="opacity-70">Criado em:</span> {new Date(db.createdAt).toLocaleString()}</div>
            </div>
            <div className="flex gap-2 pt-2">
              <Button onClick={handleTest}>Testar conexão</Button>
              <Button onClick={openTables}>Tabelas</Button>
              <Button onClick={openQueryTool}>Query Tool</Button>
            </div>
            {message && <div className="pt-2 text-xs opacity-80">{message}</div>}
            {showTables && id ? (
              <div className="pt-4">
                <h3 className="mb-2 text-sm opacity-70">Tabelas</h3>
                <TableList databaseId={id} />
              </div>
            ) : null}
          </div>
        )}

        {!loading && !error && isCreate && (
          <div className="space-y-3 text-sm">
            <div className="grid grid-cols-1 gap-3">
              <label className="block">
                <span className="block text-xs opacity-70">Nome (opcional)</span>
                <input className="w-full rounded border border-gray-700/50 bg-transparent px-3 py-2"
                  value={form.name ?? ''}
                  onChange={(e) => setForm({ ...form, name: e.target.value })} />
              </label>
              <label className="block">
                <span className="block text-xs opacity-70">Caminho .FDB</span>
                <div className="flex gap-2">
                  <input className="w-full rounded border border-gray-700/50 bg-transparent px-3 py-2"
                    value={form.database}
                    onChange={(e) => setForm({ ...form, database: e.target.value })}
                  />
                  <Button onClick={async () => {
                    const paths = await window.system.openFile([{ name: 'Firebird Database', extensions: ['fdb'] }])
                    if (paths && paths[0]) setForm({ ...form, database: paths[0].replace(/\\/g, '/') })
                  }}>Selecionar</Button>
                </div>
              </label>
              <label className="block">
                <span className="block text-xs opacity-70">Servidor</span>
                <input className="w-full rounded border border-gray-700/50 bg-transparent px-3 py-2"
                  value={form.server ?? ''}
                  onChange={(e) => setForm({ ...form, server: e.target.value })} />
              </label>
              <div className="grid grid-cols-3 gap-3">
                <label className="block">
                  <span className="block text-xs opacity-70">Usuário</span>
                  <input className="w-full rounded border border-gray-700/50 bg-transparent px-3 py-2"
                    value={form.username ?? ''}
                    onChange={(e) => setForm({ ...form, username: e.target.value })} />
                </label>
                <label className="block">
                  <span className="block text-xs opacity-70">Senha</span>
                  <input type="password" className="w-full rounded border border-gray-700/50 bg-transparent px-3 py-2"
                    value={form.password ?? ''}
                    onChange={(e) => setForm({ ...form, password: e.target.value })} />
                </label>
                <label className="block">
                  <span className="block text-xs opacity-70">Porta</span>
                  <input type="number" className="w-full rounded border border-gray-700/50 bg-transparent px-3 py-2"
                    value={form.port ?? 3050}
                    onChange={(e) => setForm({ ...form, port: Number(e.target.value) })} />
                </label>
              </div>
              <label className="block">
                <span className="block text-xs opacity-70">Charset</span>
                <input className="w-full rounded border border-gray-700/50 bg-transparent px-3 py-2"
                  value={form.charset ?? ''}
                  onChange={(e) => setForm({ ...form, charset: e.target.value })} />
              </label>
            </div>
            <div className="flex gap-2 pt-2">
              <Button onClick={handleSave}>
                Salvar
              </Button>
            </div>
            {message && <div className="pt-2 text-xs opacity-80">{message}</div>}
          </div>
        )}
      </div>
    </main>
  )
}


