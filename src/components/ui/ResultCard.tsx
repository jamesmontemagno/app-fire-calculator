import { formatCurrency } from '../../utils/calculations'

interface ResultCardProps {
  label: string
  value: number | string
  format?: 'currency' | 'years' | 'percent' | 'none'
  highlight?: boolean
  subtext?: string
  icon?: string
}

export default function ResultCard({
  label,
  value,
  format = 'none',
  highlight = false,
  subtext,
  icon,
}: ResultCardProps) {
  const formatValue = () => {
    if (typeof value === 'string') return value
    
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
          {subtext && (
            <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">{subtext}</p>
          )}
        </div>
        {icon && <span className="text-2xl">{icon}</span>}
      </div>
    </div>
  )
}
