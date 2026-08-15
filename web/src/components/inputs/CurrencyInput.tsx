import { useEffect, useId, useState } from 'react'
import Tooltip from '../ui/Tooltip'
import { useCurrencyPeriodContext } from './CurrencyPeriodProvider'
import {
  convertPeriod,
  formatPeriodAmount,
  formatTypedAmount,
  periodQualifier,
  periodSuffix,
  resolveEditedAmount,
  type CurrencyPeriod,
} from '../../utils/currencyPeriod'

interface CurrencyInputProps {
  label: string
  tooltip?: string
  value: number
  onChange: (value: number) => void
  min?: number
  max?: number
  className?: string
  /**
   * Marks the field as a recurring amount, so it follows the calculator's shared monthly/annual
   * preference and always states its period. Leave it off for balances and other one-off amounts.
   */
  periodic?: boolean
  /** Period the stored `value` is expressed in. Bounds and `onChange` always use this period. */
  storedPeriod?: CurrencyPeriod
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
  periodic = false,
  storedPeriod = 'annual',
  showInvalidState = false,
}: CurrencyInputProps) {
  const id = useId()
  const { period } = useCurrencyPeriodContext()
  // Text the user is part-way through typing. Rendering it verbatim is what stops the field
  // rewriting "4166.6" to "4,167" between keystrokes.
  const [draft, setDraft] = useState<string | null>(null)

  const displayPeriod = periodic ? period : storedPeriod
  const displayValue = convertPeriod(value, storedPeriod, displayPeriod)

  // A draft written in one period must not be reinterpreted in another.
  useEffect(() => setDraft(null), [displayPeriod])

  // Check if current value is invalid (below min)
  const isInvalid = showInvalidState && value < min

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    // Remove non-numeric characters except decimal point
    const raw = e.target.value.replace(/[^0-9.]/g, '')
    setDraft(raw)

    // Handle empty input or invalid numbers
    if (raw === '' || raw === '.') {
      onChange(0)
      return
    }

    const typed = parseFloat(raw)

    // Guard against NaN
    if (isNaN(typed)) {
      onChange(0)
      return
    }

    // Ensure non-negative if min is 0 or positive
    const typedDisplayAmount = min >= 0 && typed < 0 ? 0 : typed

    // Convert back to the stored period. Re-entering the figure already on screen is a no-op, so
    // switching periods and back never nudges the value the user entered.
    const newValue = resolveEditedAmount(typedDisplayAmount, value, displayPeriod, storedPeriod)

    // Bounds are always expressed in the stored period, so clamping happens after conversion.
    // `showInvalidState` fields flag a too-low value in the UI instead of clamping it.
    let finalValue = newValue
    if (max !== undefined && newValue > max) {
      finalValue = max
    } else if (!showInvalidState && newValue < min) {
      finalValue = min
    }

    // Keep the draft only while it still describes the stored value; otherwise the clamp would be
    // invisible until blur.
    setDraft(finalValue === newValue ? raw : null)
    onChange(finalValue)
  }

  const formattedValue = draft !== null ? formatTypedAmount(draft) : formatPeriodAmount(displayValue)

  return (
    <div className={className}>
      <div className="flex items-center justify-between mb-1.5">
        <label 
          htmlFor={id} 
          className="flex items-center gap-1.5 text-sm font-medium text-content-muted"
        >
          {label}
          {periodic && (
            <span className="text-xs font-normal text-content-subtle">
              ({periodQualifier(displayPeriod)})
            </span>
          )}
          {tooltip && <Tooltip content={tooltip} />}
        </label>
      </div>
      <div className="relative">
        <span className="absolute left-3 top-1/2 -translate-y-1/2 text-content-subtle pointer-events-none font-medium">
          $
        </span>
        <input
          id={id}
          type="text"
          inputMode="decimal"
          value={formattedValue}
          onChange={handleChange}
          onBlur={() => setDraft(null)}
          aria-invalid={isInvalid || undefined}
          className={`
            w-full pl-8 ${periodic ? 'pr-12' : 'pr-3'} py-2.5 
            bg-surface-raised 
            border rounded-control 
            text-content
            placeholder-content-subtle
            transition-colors
            ${isInvalid 
              ? 'border-danger focus:ring-2 focus-visible:ring-danger focus-visible:border-danger' 
              : 'border-border-strong focus-visible:ring-2 focus-visible:ring-ring focus-visible:border-accent'
            }
          `}
        />
        {periodic && (
          <span className="absolute right-3 top-1/2 -translate-y-1/2 text-xs text-content-subtle pointer-events-none">
            {periodSuffix(displayPeriod)}
          </span>
        )}
      </div>
    </div>
  )
}
