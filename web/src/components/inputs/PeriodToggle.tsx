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
      <span className="text-xs text-gray-600 dark:text-gray-400" id={labelId}>
        {label}
      </span>
      <div
        role="group"
        aria-labelledby={labelId}
        className="inline-flex rounded-lg border border-gray-300 p-0.5 dark:border-gray-600"
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
                rounded-md px-2.5 py-1 text-xs font-medium transition-colors
                focus:outline-none focus-visible:ring-2 focus-visible:ring-fire-500
                ${isSelected
                  ? 'bg-fire-600 text-white dark:bg-fire-500'
                  : 'text-gray-600 hover:bg-gray-100 dark:text-gray-400 dark:hover:bg-gray-700'
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
