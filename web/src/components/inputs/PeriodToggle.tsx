import { useId } from 'react'
import { useCurrencyPeriodContext } from './CurrencyPeriodProvider'
import type { CurrencyPeriod } from '../../utils/currencyPeriod'

const OPTIONS: { value: CurrencyPeriod; label: string }[] = [
  { value: 'monthly', label: 'Monthly' },
  { value: 'annual', label: 'Annual' },
]

interface PeriodToggleProps {
  className?: string
  label?: string
}

/**
 * One control for every recurring money field on the calculator. Switching it only changes how the
 * amounts are displayed and edited; the stored values and every calculation stay annual.
 */
export default function PeriodToggle({
  className = '',
  label = 'Show recurring amounts as',
}: PeriodToggleProps) {
  const { period, setPeriod } = useCurrencyPeriodContext()
  const labelId = useId()

  return (
    <div className={`flex items-center gap-2 ${className}`}>
      <span className="text-xs text-content-muted" id={labelId}>
        {label}
      </span>
      <div
        role="group"
        aria-labelledby={labelId}
        className="inline-flex rounded-control border border-border-subtle bg-surface-sunken p-0.5"
      >
        {OPTIONS.map(option => {
          const isSelected = option.value === period
          return (
            <button
              key={option.value}
              type="button"
              aria-pressed={isSelected}
              onClick={() => setPeriod(option.value)}
              className={`
                rounded-control px-2.5 py-1 text-xs font-medium transition-colors
                focus:outline-none focus-visible:ring-2 focus-visible:ring-ring
                ${isSelected
                  ? 'bg-accent text-accent-contrast'
                  : 'text-content-muted hover:text-content'
                }
              `}
            >
              {option.label}
            </button>
          )
        })}
      </div>
    </div>
  )
}
