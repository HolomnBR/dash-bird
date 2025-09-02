import { Slot } from '@radix-ui/react-slot'
import React from 'react'

export type ButtonProps = React.ButtonHTMLAttributes<HTMLButtonElement> & {
  asChild?: boolean
  variant?: 'default' | 'outline' | 'ghost'
}

export function Button({ asChild, className, variant = 'default', ...props }: ButtonProps) {
  const Comp: any = asChild ? Slot : 'button'
  
  const variantClasses = {
    default: 'bg-blue-600 text-white hover:bg-blue-700 active:bg-blue-800',
    outline: 'border border-gray-300 bg-white text-gray-700 hover:bg-gray-50 active:bg-gray-100',
    ghost: 'text-gray-700 hover:bg-gray-100 active:bg-gray-200'
  }
  
  return (
    <Comp
      className={[
        'inline-flex items-center justify-center rounded-md text-sm font-medium transition-colors',
        'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-blue-500 focus-visible:ring-offset-2',
        'disabled:pointer-events-none disabled:opacity-50',
        variantClasses[variant],
        'h-9 px-4 py-2',
        className,
      ]
        .filter(Boolean)
        .join(' ')}
      {...props}
    />
  )
}


