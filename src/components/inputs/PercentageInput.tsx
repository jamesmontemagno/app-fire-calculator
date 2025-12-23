import { useId } from 'react'

interface PercentageInputProps {
  label: string
  tooltip?: string
  value: number // as decimal (0.07 for 7%)
  onChange: (value: number) => void
  min?: number
  max?: number
  step?: number
  showSlider?: boolean
  className?: string
}

export default function PercentageInput({
  label,
  tooltip,
  value,
  onChange,
  min = 0,
  max = 1,
  step = 0.005,
  showSlider = true,
  className = '',
}: PercentageInputProps) {
  const id = useId()
  const sliderId = useId()
  
  // Convert decimal to percentage for display
  const displayValue = (value * 100).toFixed(1)

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const percentValue = parseFloat(e.target.value) || 0
    const decimalValue = percentValue / 100
    
    if (decimalValue < min) {
      onChange(min)
    } else if (decimalValue > max) {
      onChange(max)
    } else {
      onChange(decimalValue)
    }
  }

  const handleSliderChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    onChange(parseFloat(e.target.value))
  }

  return (
    <div className={className}>
      <label 
        htmlFor={id} 
        className="flex items-center gap-1.5 text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5"
      >
        {label}
        {tooltip && (
          <span className="group relative">
            <svg className="w-4 h-4 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 cursor-help" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            <span className="absolute left-1/2 -translate-x-1/2 bottom-full mb-2 px-3 py-2 bg-gray-900 dark:bg-gray-700 text-white text-xs rounded-lg opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all whitespace-nowrap z-50 max-w-xs text-center">
              {tooltip}
              <span className="absolute left-1/2 -translate-x-1/2 top-full border-4 border-transparent border-t-gray-900 dark:border-t-gray-700" />
            </span>
          </span>
        )}
      </label>
      
      <div className="flex items-center gap-3">
        <div className="relative flex-shrink-0 w-24">
          <input
            id={id}
            type="number"
            value={displayValue}
            onChange={handleInputChange}
            step={step * 100}
            min={min * 100}
            max={max * 100}
            className="
              w-full pl-3 pr-8 py-2.5 
              bg-white dark:bg-gray-800 
              border border-gray-300 dark:border-gray-600 
              rounded-lg 
              text-gray-900 dark:text-gray-100
              placeholder-gray-400 dark:placeholder-gray-500
              focus:ring-2 focus:ring-fire-500 focus:border-fire-500 dark:focus:ring-fire-400 dark:focus:border-fire-400
              transition-colors
              text-right
            "
          />
          <span className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-500 dark:text-gray-400 pointer-events-none">
            %
          </span>
        </div>
        
        {showSlider && (
          <input
            id={sliderId}
            type="range"
            value={value}
            onChange={handleSliderChange}
            min={min}
            max={max}
            step={step}
            className="
              flex-1 h-2 
              bg-gray-200 dark:bg-gray-700 
              rounded-lg appearance-none cursor-pointer
              accent-fire-500 dark:accent-fire-400
            "
          />
        )}
      </div>
    </div>
  )
}
