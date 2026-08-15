import { formatCurrency } from '../../utils/calculations'

/**
 * Copy for results the model cannot reach. `yearsToFIRE` and friends legitimately return `Infinity`
 * when the target is unreachable (for example a 0% return against 3% inflation), and formatting that
 * straight through printed "Infinity years" and "$∞". The wording mirrors `retirementGoalMessage`
 * in `calculations.ts` so the cards and the goal assessment say the same thing.
 */
const UNREACHABLE_VALUE = 'Not reachable'
const UNREACHABLE_SUBTEXT = 'The current return, inflation and contribution assumptions never reach this target.'

interface ResultCardProps {
  label: string
  value: number | string
  format?: 'currency' | 'years' | 'percent' | 'none'
  highlight?: boolean
  subtext?: string
  unreachableText?: string
  unreachableSubtext?: string
}

export default function ResultCard({
  label,
  value,
  format = 'none',
  highlight = false,
  subtext,
  unreachableText = UNREACHABLE_VALUE,
  unreachableSubtext = UNREACHABLE_SUBTEXT,
}: ResultCardProps) {
  // Guarded here rather than at each call site so a new card cannot reintroduce the leak.
  const isUnreachable = typeof value === 'number' && !Number.isFinite(value)
  const displaySubtext = isUnreachable ? unreachableSubtext : subtext

  const formatValue = () => {
    if (typeof value === 'string') return value
    if (isUnreachable) return unreachableText

    switch (format) {
      case 'currency':
        return formatCurrency(value)
      case 'years':
        return `${value.toFixed(1)} years`
      case 'percent':
        return `${(value * 100).toFixed(1)}%`
      default:
        return value.toLocaleString()
    }
  }

  return (
    <div className={`
      rounded-container p-4 border
      ${highlight
        ? 'bg-accent-subtle border-accent/30'
        : 'bg-surface-sunken border-border-subtle'
      }
    `}>
      <p className="text-sm text-content-muted mb-1">{label}</p>
      <p className={`tabular text-2xl font-semibold tracking-tight ${
        highlight ? 'text-accent' : 'text-content'
      }`}>
        {formatValue()}
      </p>
      {displaySubtext && (
        <p className="text-xs text-content-subtle mt-1">{displaySubtext}</p>
      )}
    </div>
  )
}
