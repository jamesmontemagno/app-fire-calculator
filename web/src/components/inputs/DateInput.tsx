import { useId } from 'react'
import Tooltip from '../ui/Tooltip'

interface DateInputProps {
  label: string
  tooltip?: string
  /** ISO date (YYYY-MM-DD). */
  value: string
  onChange: (value: string) => void
  min?: string
  max?: string
  helperText?: string
  className?: string
}

export default function DateInput({
  label,
  tooltip,
  value,
  onChange,
  min,
  max,
  helperText,
  className = '',
}: DateInputProps) {
  const id = useId()
  const helperTextId = useId()

  return (
    <div className={className}>
      <label
        htmlFor={id}
        className="flex items-center gap-1.5 text-sm font-medium text-content-muted mb-1.5"
      >
        {label}
        {tooltip && <Tooltip content={tooltip} />}
      </label>
      <input
        id={id}
        type="date"
        value={value}
        min={min}
        max={max}
        onChange={event => {
          // A partially typed date reports an empty value; keep the last valid one until it is complete.
          if (event.target.value) onChange(event.target.value)
        }}
        aria-describedby={helperText ? helperTextId : undefined}
        className="
          w-full px-3 py-2.5
          bg-surface-raised
          border border-border-strong
          rounded-control
          text-content
          focus-visible:ring-2 focus-visible:ring-ring focus-visible:border-accent
          transition-colors
        "
      />
      {helperText && (
        <p id={helperTextId} className="mt-1.5 text-xs text-content-subtle">
          {helperText}
        </p>
      )}
    </div>
  )
}
