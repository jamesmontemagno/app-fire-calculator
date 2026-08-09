import { useId, useState, useEffect } from 'react'
import Tooltip from '../ui/Tooltip'

interface AgeInputProps {
  label: string
  tooltip?: string
  value: number
  onChange: (value: number) => void
  min?: number
  max?: number
  className?: string
}

export default function AgeInput({
  label,
  tooltip,
  value,
  onChange,
  min = 18,
  max = 100,
  className = '',
}: AgeInputProps) {
  const id = useId()
  const [inputValue, setInputValue] = useState(value.toString())

  // Sync with external value changes (e.g., from URL params or presets)
  useEffect(() => {
    const parsed = parseInt(inputValue)
    if (parsed !== value) {
      setInputValue(value.toString())
    }
  }, [value])

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const raw = e.target.value
    setInputValue(raw)
    
    // Only update parent if valid number in range
    const parsed = parseInt(raw)
    if (!isNaN(parsed) && parsed >= min && parsed <= max) {
      onChange(parsed)
    }
  }

  const handleBlur = () => {
    // On blur, validate and reset to last valid value if needed
    const parsed = parseInt(inputValue)
    if (isNaN(parsed) || parsed < min || parsed > max) {
      setInputValue(value.toString())
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
        <input
          id={id}
          type="number"
          value={inputValue}
          onChange={handleChange}
          onBlur={handleBlur}
          min={min}
          max={max}
          className="
            w-full px-3 py-2.5 pr-16
            bg-white dark:bg-gray-800 
            border border-gray-300 dark:border-gray-600 
            rounded-lg 
            text-gray-900 dark:text-gray-100
            placeholder-gray-400 dark:placeholder-gray-500
            focus:ring-2 focus:ring-fire-500 focus:border-fire-500 dark:focus:ring-fire-400 dark:focus:border-fire-400
            transition-colors
          "
        />
        <span className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-500 dark:text-gray-400 pointer-events-none text-sm">
          years old
        </span>
      </div>
    </div>
  )
}
