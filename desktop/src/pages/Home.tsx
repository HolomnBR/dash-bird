import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Settings as SettingsIcon, Plus } from 'lucide-react'
import { Header } from '../components/molecules/Header'
import { DatabaseList } from '../components/organisms/DatabaseList'

export function Home() {
  const [deviceName, setDeviceName] = useState<string>('')
  const [machineId, setMachineId] = useState<string>('')
  

  useEffect(() => {
    // read alias if needed in the future
    if (window.system) {
      window.system.getInfo().then((info) => {
        setDeviceName(info.deviceName)
        setMachineId(info.machineId)
      })
    }
  }, [])

  return (
    <main className="mx-auto max-w-3xl">
      <Header
        leftSlot={
          <Link to="/settings" aria-label="Configurações" title="Configurações">
            <SettingsIcon size={18} className="opacity-80 hover:opacity-100" />
          </Link>
        }
        title={deviceName || '...'}
        subtitle={machineId || '...'}
      />
      <div className="p-6">
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


