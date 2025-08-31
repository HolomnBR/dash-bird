import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Button } from '../components/atoms/Button'
import { Header } from '../components/molecules/Header'
import { ArrowLeft } from 'lucide-react'

export function Settings() {
  const [alias, setAlias] = useState<string>('')
  const [deviceName, setDeviceName] = useState<string>('')
  const [machineId, setMachineId] = useState<string>('')
  const [nodeId, setNodeId] = useState<string>('')

  useEffect(() => {
    if (window.system) {
      window.system.getNodeConfig().then((cfg) => {
        setDeviceName(cfg.machineName)
        setMachineId(cfg.machineId)
        setAlias(cfg.alias || '')
        setNodeId(cfg.nodeId)
      })
    }
  }, [])

  async function saveAlias() {
    if (window.system) {
      const cfg = await window.system.setAlias(alias)
      setAlias(cfg.alias || '')
    } else {
      localStorage.setItem('device.alias', alias)
    }
  }

  const headerTitle = alias ? `${deviceName} (${alias})` : deviceName || 'Configurações'

  return (
    <main className="mx-auto max-w-3xl">
      <Header
        leftSlot={
          <Link to="/" aria-label="Voltar" title="Voltar">
            <ArrowLeft size={18} className="opacity-80 hover:opacity-100" />
          </Link>
        }
        title={headerTitle}
        subtitle={machineId}
      />
      <div className="p-6 space-y-6">
        <div className="text-xs opacity-60">Node ID: {nodeId}</div>
        <div className="space-y-2">
          <label className="block text-sm opacity-70">Alias (opcional)</label>
          <input
            className="w-full rounded border border-gray-600 bg-transparent px-3 py-2 text-sm outline-none focus:border-gray-400"
            value={alias}
            onChange={(e) => setAlias(e.target.value)}
            placeholder="Dê um nome para esta máquina"
          />
          <div className="flex gap-2">
            <Button onClick={saveAlias}>Salvar alias</Button>
          </div>
        </div>
      </div>
    </main>
  )
}


