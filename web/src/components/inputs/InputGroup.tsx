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
  onSliderChange?: (value: number) => void
  min?: number
  max?: number
  step?: number
  showSlider?: boolean
}

export default function InputGroup({
  label,
  tooltip,
  helperText,
  prefix,
  suffix,
  value,
  onChange,
  onSliderChange,
  min,
  max,
  step = 1,
  showSlider = false,
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
        className="flex items-center gap-1.5 text-sm font-medium text-content-muted mb-1.5"
      >
        {label}
        {tooltip && <Tooltip content={tooltip} />}
      </label>
      <div>
        <div className="relative">
          {prefix && (
            <span className="absolute left-3 top-1/2 -translate-y-1/2 text-content-subtle pointer-events-none">
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
              bg-surface-raised 
              border border-border-strong 
              rounded-control 
              text-content
              placeholder-content-subtle
              focus-visible:ring-2 focus-visible:ring-ring focus-visible:border-accent
              transition-colors
              ${prefix ? 'pl-8' : ''}
              ${suffix ? 'pr-12' : ''}
            `}
            {...props}
          />
          {suffix && (
            <span className="absolute right-3 top-1/2 -translate-y-1/2 text-content-subtle pointer-events-none">
              {suffix}
            </span>
          )}
        </div>
        {showSlider && min !== undefined && max !== undefined && (
          <input
            type="range"
            value={value}
            onChange={(event) => {
              const newValue = parseFloat(event.target.value)
              ;(onSliderChange ?? onChange)(newValue)
            }}
            min={min}
            max={max}
            step={step}
            aria-label={`${label} slider`}
            className="
              mt-3 w-full h-2
              bg-border-subtle
              rounded-control appearance-none cursor-pointer
              accent-accent
            "
          />
        )}
        {helperText && (
          <p id={helperTextId} className="mt-1.5 text-xs text-content-subtle">
            {helperText}
          </p>
        )}
      </div>
    </div>
  )
}
