import { type ButtonHTMLAttributes, type ReactNode } from 'react'

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'outline' | 'ghost' | 'danger'
  size?: 'sm' | 'md' | 'lg'
  children: ReactNode
}

const variants = {
  primary: 'bg-accent text-accent-contrast hover:bg-accent-hover active:bg-accent-hover',
  secondary: 'bg-surface-sunken text-content hover:bg-border-subtle active:bg-border-subtle',
  outline: 'border border-border-strong text-content hover:bg-surface-sunken active:bg-surface-sunken',
  ghost: 'text-content-muted hover:bg-surface-sunken hover:text-content active:bg-surface-sunken',
  danger: 'border border-danger text-danger hover:bg-danger hover:text-surface-raised',
}

const sizes = {
  sm: 'h-8 px-3 text-sm',
  md: 'h-9 px-4 text-sm',
  lg: 'h-11 px-6 text-base',
}

export default function Button({
  variant = 'primary',
  size = 'md',
  children,
  className = '',
  disabled,
  ...props
}: ButtonProps) {
  return (
    <button
      className={`
        inline-flex items-center justify-center gap-2
        rounded-control font-medium transition-colors duration-150
        motion-reduce:transition-none
        focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-surface
        disabled:pointer-events-none disabled:opacity-50
        ${variants[variant]}
        ${sizes[size]}
        ${className}
      `}
      disabled={disabled}
      {...props}
    >
      {children}
    </button>
  )
}
