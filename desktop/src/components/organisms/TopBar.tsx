import { Header } from '../molecules/Header'
import { AuthButton } from '../molecules/AuthButton'
import { AnonymousStatus } from '../molecules/AnonymousStatus'

export function TopBar() {
  return (
    <div className="border-b border-gray-700/50">
      <div className="mx-auto max-w-5xl px-4">
        <Header 
          title="Dash Bird"
          subtitle="Sua IA conectada em seu FireBird"
          rightSlot={
            <div className="flex items-center space-x-4">
              <AnonymousStatus />
              <AuthButton />
            </div>
          }
        />
      </div>
    </div>
  )
}


