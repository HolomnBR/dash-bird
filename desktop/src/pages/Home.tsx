import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Settings as SettingsIcon, Plus, BarChart3 } from 'lucide-react'
import { Header } from '../components/molecules/Header'
import { DatabaseList } from '../components/organisms/DatabaseList'
import { LocalNodeInfo } from '../components/LocalNodeInfo'

export function Home() {
  const [deviceName, setDeviceName] = useState<string>('')
  const [machineId, setMachineId] = useState<string>('')
  const [alias, setAlias] = useState<string>('')
  const [nodeId, setNodeId] = useState<string>('')
  

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      try {
        if (!window.system) return
        // Primeiro, carregue do nodeConfig (main persistiu/atualizou)
        const cfg = await window.system.getNodeConfig()
        if (cancelled) return
        setDeviceName(cfg.machineName || '')
        setMachineId(cfg.machineId || '')
        setAlias(cfg.alias || '')
        setNodeId(cfg.nodeId || '')
      } catch {
        // Fallback mínimo
        try {
          const info = await window.system.getInfo()
          if (cancelled) return
          setDeviceName(info.deviceName || '')
          setMachineId(info.machineId || '')
          setAlias('')
        } catch {}
      }
    })()
    return () => { cancelled = true }
  }, [])

  return (
    <main className="mx-auto max-w-3xl">
      <Header
        leftSlot={
          <Link to="/dashboard" aria-label="Dashboard Estratégico" title="Dashboard Estratégico">
            <BarChart3 size={18} className="opacity-80 hover:opacity-100" />
          </Link>
        }
        title={alias ? `${deviceName} (${alias})` : (deviceName || 'Carregando...')}
        subtitle={machineId || ''}
      />
      <div className="p-6">
        <div className="mb-4">
          <LocalNodeInfo showDetails={true} />
        </div>
        <div className="mb-3 flex items-center justify-between">
          <h2 className="text-sm opacity-70">Bases configuradas</h2>
          <Link to="/databases/new" aria-label="Nova base" title="Nova base" className="opacity-80 hover:opacity-100">
            <Plus size={18} />
          </Link>
        </div>
        <DatabaseList />
      </div>
    </main>
  )
}


