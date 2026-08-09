import { useId, useState } from 'react'
import Tooltip from '../ui/Tooltip'

interface CurrencyInputProps {
  label: string
  tooltip?: string
  value: number
  onChange: (value: number) => void
  min?: number
  max?: number
  className?: string
  allowMonthlyToggle?: boolean
  showInvalidState?: boolean // When true, shows red border if value is below min instead of clamping
}

export default function CurrencyInput({
  label,
  tooltip,
  value,
  onChange,
  min = 0,
  max,
  className = '',
  allowMonthlyToggle = false,
  showInvalidState = false,
}: CurrencyInputProps) {
  const id = useId()
  const [isMonthly, setIsMonthly] = useState(false)

  // When in monthly mode, display and edit monthly values, but store annual
  const displayValue = isMonthly ? value / 12 : value

  // Check if current value is invalid (below min)
  const isInvalid = showInvalidState && value < min

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    // Remove non-numeric characters except decimal point
    const raw = e.target.value.replace(/[^0-9.]/g, '')
    
    // Handle empty input or invalid numbers
    if (raw === '' || raw === '.') {
      onChange(0)
      return
    }
    
    let newValue = parseFloat(raw)
    
    // Guard against NaN
    if (isNaN(newValue)) {
      onChange(0)
      return
    }
    
    // Ensure non-negative if min is 0 or positive
    if (min >= 0 && newValue < 0) {
      newValue = 0
    }
    
    // Convert monthly to annual if in monthly mode
    if (isMonthly) {
      newValue = newValue * 12
    }
    
    // If showInvalidState is enabled, don't clamp the min value - just let it through
    if (showInvalidState) {
      if (max !== undefined && newValue > max) {
        onChange(max)
      } else {
        onChange(newValue)
      }
    } else {
      // Original behavior: clamp to min/max
      if (max !== undefined && newValue > max) {
        onChange(max)
      } else if (newValue < min) {
        onChange(min)
      } else {
        onChange(newValue)
      }
    }
  }

  const formattedValue = new Intl.NumberFormat('en-US').format(Math.round(displayValue))

  return (
    <div className={className}>
      <div className="flex items-center justify-between mb-1.5">
        <label 
          htmlFor={id} 
          className="flex items-center gap-1.5 text-sm font-medium text-gray-700 dark:text-gray-300"
        >
          {label}
          {isMonthly && <span className="text-xs text-gray-500">(monthly)</span>}
          {tooltip && <Tooltip content={tooltip} />}
        </label>
        
        {allowMonthlyToggle && (
          <button
            type="button"
            onClick={() => setIsMonthly(!isMonthly)}
            className="text-xs px-2 py-0.5 rounded bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-700 transition-colors"
          >
            {isMonthly ? 'Annual ↔' : 'Monthly ↔'}
          </button>
        )}
      </div>
      <div className="relative">
        <span className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-500 dark:text-gray-400 pointer-events-none font-medium">
          $
        </span>
        <input
          id={id}
          type="text"
          inputMode="numeric"
          value={formattedValue}
          onChange={handleChange}
          className={`
            w-full pl-8 pr-3 py-2.5 
            bg-white dark:bg-gray-800 
            border rounded-lg 
            text-gray-900 dark:text-gray-100
            placeholder-gray-400 dark:placeholder-gray-500
            transition-colors
            ${isInvalid 
              ? 'border-red-500 dark:border-red-400 focus:ring-2 focus:ring-red-500 focus:border-red-500 dark:focus:ring-red-400 dark:focus:border-red-400' 
              : 'border-gray-300 dark:border-gray-600 focus:ring-2 focus:ring-fire-500 focus:border-fire-500 dark:focus:ring-fire-400 dark:focus:border-fire-400'
            }
          `}
        />
        {allowMonthlyToggle && (
          <span className="absolute right-3 top-1/2 -translate-y-1/2 text-xs text-gray-400 pointer-events-none">
            {isMonthly ? '/mo' : '/yr'}
          </span>
        )}
      </div>
    </div>
  )
}
