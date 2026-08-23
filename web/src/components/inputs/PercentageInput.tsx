import { useId } from 'react'
import Tooltip from '../ui/Tooltip'

interface PercentageInputProps {
  label: string
  tooltip?: string
  value: number // as decimal (0.07 for 7%)
  onChange: (value: number) => void
  onSliderChange?: (value: number) => void
  min?: number
  max?: number
  step?: number
  showSlider?: boolean
  /** Decimal places shown in the text field. Rates quoted to the basis point need two. */
  decimals?: number
  className?: string
}

export default function PercentageInput({
  label,
  tooltip,
  value,
  onChange,
  onSliderChange,
  min = 0,
  max = 1,
  step = 0.005,
  showSlider = true,
  decimals = 1,
  className = '',
}: PercentageInputProps) {
  const id = useId()
  const sliderId = useId()
  
  // Convert decimal to percentage for display
  const displayValue = (value * 100).toFixed(decimals)

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const raw = e.target.value
    
    // Handle empty input
    if (raw === '' || raw === '-') {
      onChange(min)
      return
    }
    
    const percentValue = parseFloat(raw)
    
    // Guard against NaN
    if (isNaN(percentValue)) {
      onChange(min)
      return
    }
    
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
    ;(onSliderChange ?? onChange)(parseFloat(e.target.value))
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
              bg-surface-raised 
              border border-border-strong 
              rounded-control 
              text-content
              placeholder-content-subtle
              focus-visible:ring-2 focus-visible:ring-ring focus-visible:border-accent
              transition-colors
              text-right
            "
          />
          <span className="absolute right-3 top-1/2 -translate-y-1/2 text-content-subtle pointer-events-none">
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
              bg-border-subtle 
              rounded-control appearance-none cursor-pointer
              accent-accent
            "
          />
        )}
      </div>
    </div>
  )
}
