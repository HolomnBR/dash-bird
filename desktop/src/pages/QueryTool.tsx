import { Link, useLocation } from 'react-router-dom'
import { Header } from '../components/molecules/Header'
import { ArrowLeft } from 'lucide-react'

export function QueryTool() {
  const location = useLocation()
  const params = new URLSearchParams(location.search)
  const databaseId = params.get('databaseId') || ''

  return (
    <main className="mx-auto max-w-5xl">
      <Header
        leftSlot={
          <Link to="/" aria-label="Voltar" title="Voltar">
            <ArrowLeft size={18} className="opacity-80 hover:opacity-100" />
          </Link>
        }
        title="Query Tool"
        subtitle={databaseId ? `databaseId=${databaseId}` : ''}
      />
      <div className="p-6 text-sm opacity-70">Editor de consultas — em breve.</div>
    </main>
  )
}


