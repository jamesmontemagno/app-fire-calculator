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
  icon?: string
  unreachableText?: string
  unreachableSubtext?: string
}

export default function ResultCard({
  label,
  value,
  format = 'none',
  highlight = false,
  subtext,
  icon,
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
      p-4 rounded-xl
      ${highlight 
        ? 'bg-fire-50 dark:bg-fire-900/20 border-2 border-fire-200 dark:border-fire-800' 
        : 'bg-gray-50 dark:bg-gray-800/50 border border-gray-200 dark:border-gray-700'
      }
    `}>
      <div className="flex items-start justify-between">
        <div>
          <p className="text-sm text-gray-600 dark:text-gray-400 mb-1">{label}</p>
          <p className={`text-2xl font-bold ${
            highlight 
              ? 'text-fire-600 dark:text-fire-400' 
              : 'text-gray-900 dark:text-gray-100'
          }`}>
            {formatValue()}
          </p>
          {displaySubtext && (
            <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">{displaySubtext}</p>
          )}
        </div>
        {icon && <span className="text-2xl">{icon}</span>}
      </div>
    </div>
  )
}
