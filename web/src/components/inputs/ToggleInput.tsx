import { useId } from 'react'
import Tooltip from '../ui/Tooltip'

interface ToggleInputProps {
  label: string
  tooltip?: string
  description?: string
  checked: boolean
  onChange: (checked: boolean) => void
  className?: string
}

export default function ToggleInput({
  label,
  tooltip,
  description,
  checked,
  onChange,
  className = '',
}: ToggleInputProps) {
  const id = useId()
  const descriptionId = useId()

  return (
    <div className={className}>
      <div className="flex items-start gap-3">
        <button
          id={id}
          type="button"
          role="switch"
          aria-checked={checked}
          aria-describedby={description ? descriptionId : undefined}
          onClick={() => onChange(!checked)}
          className={`
            relative mt-0.5 inline-flex h-6 w-11 flex-shrink-0 items-center rounded-full
            transition-colors motion-reduce:transition-none
            focus:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2
            ${checked ? 'bg-accent' : 'bg-border-strong'}
          `}
        >
          <span
            className={`
              inline-block h-4 w-4 transform rounded-full bg-white shadow
              transition-transform motion-reduce:transition-none
              ${checked ? 'translate-x-6' : 'translate-x-1'}
            `}
          />
        </button>

        <div className="min-w-0">
          <label
            htmlFor={id}
            className="flex items-center gap-1.5 text-sm font-medium text-content-muted"
          >
            {label}
            {tooltip && <Tooltip content={tooltip} />}
          </label>
          {description && (
            <p id={descriptionId} className="mt-1 text-sm text-content-muted">
              {description}
            </p>
          )}
        </div>
      </div>
    </div>
  )
}
