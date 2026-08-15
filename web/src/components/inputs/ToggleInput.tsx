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
            focus:outline-none focus-visible:ring-2 focus-visible:ring-fire-500 focus-visible:ring-offset-2
            dark:focus-visible:ring-fire-400 dark:focus-visible:ring-offset-gray-950
            ${checked ? 'bg-fire-600 dark:bg-fire-500' : 'bg-gray-300 dark:bg-gray-600'}
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
            className="flex items-center gap-1.5 text-sm font-medium text-gray-700 dark:text-gray-300"
          >
            {label}
            {tooltip && <Tooltip content={tooltip} />}
          </label>
          {description && (
            <p id={descriptionId} className="mt-1 text-sm text-gray-600 dark:text-gray-400">
              {description}
            </p>
          )}
        </div>
      </div>
    </div>
  )
}
