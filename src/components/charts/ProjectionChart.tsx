import {
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  ReferenceLine,
  Legend,
} from 'recharts'
import type { ProjectionPoint } from '../../utils/calculations'
import { formatCurrency } from '../../utils/calculations'
import { useTheme } from '../../context/ThemeContext'

interface ProjectionChartProps {
  data: ProjectionPoint[]
  fireNumber?: number
  showInflationAdjusted?: boolean
  showMilestones?: boolean
  height?: number
  colorScheme?: 'orange' | 'green' | 'purple' | 'blue' | 'amber'
}

const colorSchemes = {
  orange: {
    primary: '#f97316',
    primaryLight: '#fed7aa',
    secondary: '#6366f1',
    secondaryLight: '#c7d2fe',
  },
  green: {
    primary: '#22c55e',
    primaryLight: '#bbf7d0',
    secondary: '#6366f1',
    secondaryLight: '#c7d2fe',
  },
  purple: {
    primary: '#a855f7',
    primaryLight: '#e9d5ff',
    secondary: '#6366f1',
    secondaryLight: '#c7d2fe',
  },
  blue: {
    primary: '#0ea5e9',
    primaryLight: '#bae6fd',
    secondary: '#6366f1',
    secondaryLight: '#c7d2fe',
  },
  amber: {
    primary: '#d97706',
    primaryLight: '#fde68a',
    secondary: '#6366f1',
    secondaryLight: '#c7d2fe',
  },
}

export default function ProjectionChart({
  data,
  fireNumber,
  showInflationAdjusted = true,
  showMilestones = true,
  height = 300,
  colorScheme = 'orange',
}: ProjectionChartProps) {
  const { resolvedTheme } = useTheme()
  const isDark = resolvedTheme === 'dark'
  const colors = colorSchemes[colorScheme]

  // Calculate milestone values
  const milestones = fireNumber ? [
    { percent: 25, value: fireNumber * 0.25, label: '25%' },
    { percent: 50, value: fireNumber * 0.5, label: '50%' },
    { percent: 75, value: fireNumber * 0.75, label: '75%' },
  ] : []

  const formatYAxis = (value: number) => {
    if (value >= 1000000) {
      return `$${(value / 1000000).toFixed(1)}M`
    }
    if (value >= 1000) {
      return `$${(value / 1000).toFixed(0)}K`
    }
    return `$${value}`
  }

  const CustomTooltip = ({ active, payload }: any) => {
    if (active && payload && payload.length) {
      const point = payload[0].payload as ProjectionPoint
      return (
        <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg p-3">
          <p className="font-semibold text-gray-900 dark:text-gray-100 mb-2">
            Age {point.age} ({point.year})
          </p>
          <div className="space-y-1 text-sm">
            <p className="text-gray-600 dark:text-gray-400">
              Portfolio: <span className="font-medium text-gray-900 dark:text-gray-100">{formatCurrency(point.portfolio)}</span>
            </p>
            {showInflationAdjusted && (
              <p className="text-gray-600 dark:text-gray-400">
                Inflation Adjusted: <span className="font-medium text-gray-900 dark:text-gray-100">{formatCurrency(point.inflationAdjusted)}</span>
              </p>
            )}
            <p className="text-gray-600 dark:text-gray-400">
              Total Contributed: <span className="font-medium text-gray-900 dark:text-gray-100">{formatCurrency(point.totalContributions)}</span>
            </p>
          </div>
        </div>
      )
    }
    return null
  }

  return (
    <ResponsiveContainer width="100%" height={height}>
      <AreaChart data={data} margin={{ top: 10, right: 10, left: 0, bottom: 0 }}>
        <defs>
          <linearGradient id={`gradient-${colorScheme}`} x1="0" y1="0" x2="0" y2="1">
            <stop offset="5%" stopColor={colors.primary} stopOpacity={0.3} />
            <stop offset="95%" stopColor={colors.primary} stopOpacity={0} />
          </linearGradient>
          <linearGradient id={`gradient-${colorScheme}-secondary`} x1="0" y1="0" x2="0" y2="1">
            <stop offset="5%" stopColor={colors.secondary} stopOpacity={0.2} />
            <stop offset="95%" stopColor={colors.secondary} stopOpacity={0} />
          </linearGradient>
        </defs>
        <CartesianGrid 
          strokeDasharray="3 3" 
          stroke={isDark ? '#374151' : '#e5e7eb'} 
          vertical={false}
        />
        <XAxis 
          dataKey="age" 
          tick={{ fill: isDark ? '#9ca3af' : '#6b7280', fontSize: 12 }}
          tickLine={{ stroke: isDark ? '#4b5563' : '#d1d5db' }}
          axisLine={{ stroke: isDark ? '#4b5563' : '#d1d5db' }}
          tickFormatter={(value) => `${value}`}
        />
        <YAxis 
          tick={{ fill: isDark ? '#9ca3af' : '#6b7280', fontSize: 12 }}
          tickLine={{ stroke: isDark ? '#4b5563' : '#d1d5db' }}
          axisLine={{ stroke: isDark ? '#4b5563' : '#d1d5db' }}
          tickFormatter={formatYAxis}
          width={65}
        />
        <Tooltip content={<CustomTooltip />} />
        <Legend 
          wrapperStyle={{ paddingTop: 16 }}
          formatter={(value) => <span className="text-sm text-gray-600 dark:text-gray-400">{value}</span>}
        />
        
        {showInflationAdjusted && (
          <Area
            type="monotone"
            dataKey="inflationAdjusted"
            name="Inflation Adjusted"
            stroke={colors.secondary}
            strokeWidth={2}
            fill={`url(#gradient-${colorScheme}-secondary)`}
            strokeDasharray="5 5"
          />
        )}
        
        <Area
          type="monotone"
          dataKey="portfolio"
          name="Portfolio Value"
          stroke={colors.primary}
          strokeWidth={2}
          fill={`url(#gradient-${colorScheme})`}
        />
        
        {/* Milestone lines */}
        {showMilestones && milestones.map((milestone) => (
          <ReferenceLine
            key={milestone.percent}
            y={milestone.value}
            stroke={isDark ? '#6b7280' : '#9ca3af'}
            strokeWidth={1}
            strokeDasharray="4 4"
            label={{
              value: milestone.label,
              fill: isDark ? '#6b7280' : '#9ca3af',
              fontSize: 10,
              position: 'left',
            }}
          />
        ))}
        
        {fireNumber && (
          <ReferenceLine
            y={fireNumber}
            stroke={isDark ? '#ef4444' : '#dc2626'}
            strokeWidth={2}
            strokeDasharray="8 4"
            label={{
              value: `FIRE: ${formatCurrency(fireNumber)}`,
              fill: isDark ? '#ef4444' : '#dc2626',
              fontSize: 12,
              fontWeight: 600,
              position: 'right',
            }}
          />
        )}
      </AreaChart>
    </ResponsiveContainer>
  )
}
