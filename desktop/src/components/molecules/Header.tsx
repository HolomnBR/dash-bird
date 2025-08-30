import type { ReactNode } from 'react'

type AppHeaderProps = {
  leftSlot?: ReactNode
  title?: string
  subtitle?: string
  rightSlot?: ReactNode
}

export function Header({ leftSlot, title, subtitle, rightSlot }: AppHeaderProps) {
  return (
    <header className="w-full flex items-center justify-between gap-3 py-3 px-4 border-b border-gray-700/50">
      <div className="flex items-center gap-3 min-w-0">
        <div className="shrink-0">{leftSlot}</div>
        <div className="min-w-0">
          <div className="text-sm font-semibold truncate">{title}</div>
          {subtitle ? (
            <div className="text-xs opacity-70 truncate">{subtitle}</div>
          ) : null}
        </div>
      </div>
      {rightSlot ? <div className="shrink-0">{rightSlot}</div> : null}
    </header>
  )
}


