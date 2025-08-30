import { Slot } from '@radix-ui/react-slot'
import React from 'react'

export type ButtonProps = React.ButtonHTMLAttributes<HTMLButtonElement> & {
  asChild?: boolean
}

export function Button({ asChild, className, ...props }: ButtonProps) {
  const Comp: any = asChild ? Slot : 'button'
  return (
    <Comp
      className={[
        'inline-flex items-center justify-center rounded-md text-sm font-medium transition-colors',
        'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-blue-500 focus-visible:ring-offset-2',
        'disabled:pointer-events-none disabled:opacity-50',
        'bg-blue-600 text-white hover:bg-blue-700 active:bg-blue-800',
        'h-9 px-4 py-2',
        className,
      ]
        .filter(Boolean)
        .join(' ')}
      {...props}
    />
  )
}


