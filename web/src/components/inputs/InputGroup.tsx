import { useId, type InputHTMLAttributes } from 'react'
import Tooltip from '../ui/Tooltip'

interface InputGroupProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'onChange'> {
  label: string
  tooltip?: string
  helperText?: string
  prefix?: string
  suffix?: string
  value: number
  onChange: (value: number) => void
  min?: number
  max?: number
  step?: number
}

export default function InputGroup({
  label,
  tooltip,
  helperText,
  prefix,
  suffix,
  value,
  onChange,
  min,
  max,
  step = 1,
  className = '',
  ...props
}: InputGroupProps) {
  const id = useId()
  const helperTextId = useId()

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newValue = parseFloat(e.target.value) || 0
    if (min !== undefined && newValue < min) {
      onChange(min)
    } else if (max !== undefined && newValue > max) {
      onChange(max)
    } else {
      onChange(newValue)
    }
  }

  return (
    <div className={className}>
      <label 
        htmlFor={id} 
        className="flex items-center gap-1.5 text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5"
      >
        {label}
        {tooltip && <Tooltip content={tooltip} />}
      </label>
      <div className="relative">
        {prefix && (
          <span className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-500 dark:text-gray-400 pointer-events-none">
            {prefix}
          </span>
        )}
        <input
          id={id}
          type="number"
          value={value}
          onChange={handleChange}
          min={min}
          max={max}
          step={step}
          aria-describedby={helperText ? helperTextId : undefined}
          className={`
            w-full px-3 py-2.5 
            bg-white dark:bg-gray-800 
            border border-gray-300 dark:border-gray-600 
            rounded-lg 
            text-gray-900 dark:text-gray-100
            placeholder-gray-400 dark:placeholder-gray-500
            focus:ring-2 focus:ring-fire-500 focus:border-fire-500 dark:focus:ring-fire-400 dark:focus:border-fire-400
            transition-colors
            ${prefix ? 'pl-8' : ''}
            ${suffix ? 'pr-12' : ''}
          `}
          {...props}
        />
        {suffix && (
          <span className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-500 dark:text-gray-400 pointer-events-none">
            {suffix}
          </span>
        )}
      </div>
      {helperText && (
        <p id={helperTextId} className="mt-1.5 text-xs text-gray-500 dark:text-gray-400">
          {helperText}
        </p>
      )}
    </div>
  )
}
