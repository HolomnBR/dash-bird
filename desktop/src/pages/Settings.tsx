import { useEffect, useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { Button } from '../components/atoms/Button'
import { Header } from '../components/molecules/Header'
import { LoginCard } from '../components/molecules/LoginCard'
import { NodeManager } from '../components/organisms/NodeManager'
import { ArrowLeft } from 'lucide-react'

export function Settings() {
  const [alias, setAlias] = useState<string>('')
  const [deviceName, setDeviceName] = useState<string>('')
  const [machineId, setMachineId] = useState<string>('')
  const [nodeId, setNodeId] = useState<string>('')
  const [showSuccessMessage, setShowSuccessMessage] = useState<string | null>(null)
  const location = useLocation()

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

  useEffect(() => {
    // Verificar se há mensagem de sucesso do registro
    if (location.state?.message) {
      setShowSuccessMessage(location.state.message)
      // Limpar a mensagem após 5 segundos
      setTimeout(() => setShowSuccessMessage(null), 5000)
    }
  }, [location.state])

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
        {showSuccessMessage && (
          <div className="bg-green-100 border border-green-400 text-green-700 px-4 py-3 rounded">
            {showSuccessMessage}
          </div>
        )}

        {/* Card de Login */}
        <LoginCard />

        {/* Gerenciador de Nós */}
        <NodeManager />

        {/* Configuração do Alias */}
        <div className="bg-white rounded-lg border border-gray-200 p-6">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">Configuração do Nó</h3>
          <div className="text-xs text-gray-500 mb-4">Node ID: {nodeId}</div>
          
          <div className="space-y-2">
            <label className="block text-sm font-medium text-gray-700">Alias (opcional)</label>
            <input
              className="w-full rounded border border-gray-300 bg-white px-3 py-2 text-sm outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-200"
              value={alias}
              onChange={(e) => setAlias(e.target.value)}
              placeholder="Dê um nome para esta máquina"
            />
            <div className="flex gap-2">
              <Button onClick={saveAlias}>Salvar alias</Button>
            </div>
          </div>
        </div>
      </div>
    </main>
  )
}


