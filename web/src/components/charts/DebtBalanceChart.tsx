import {
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  ReferenceLine,
  ComposedChart,
} from 'recharts'
import { formatCurrency } from '../../utils/calculations'
import { useTheme } from '../../context/ThemeContext'
import type { DebtPayoffMonth } from '../../utils/calculations'

interface DebtBalanceChartProps {
  data: DebtPayoffMonth[]
  milestones?: { month: number; debtName: string }[]
  comparisonData?: DebtPayoffMonth[]
  height?: number
}

export default function DebtBalanceChart({
  data,
  milestones = [],
  comparisonData,
  height = 300,
}: DebtBalanceChartProps) {
  const { resolvedTheme } = useTheme()
  const isDark = resolvedTheme === 'dark'

  const formatYAxis = (value: number) => {
    if (value >= 1000000) {
      return `$${(value / 1000000).toFixed(1)}M`
    }
    if (value >= 1000) {
      return `$${(value / 1000).toFixed(0)}K`
    }
    return `$${value}`
  }

  // Combine data for comparison
  const chartData = data.map((point, index) => ({
    ...point,
    comparisonBalance: comparisonData?.[index]?.totalBalance,
  }))

  const CustomTooltip = ({ active, payload }: any) => {
    if (active && payload && payload.length) {
      const point = payload[0].payload
      return (
        <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg p-3">
          <p className="font-semibold text-gray-900 dark:text-gray-100 mb-2">
            Month {point.month}
          </p>
          <div className="space-y-1 text-sm">
            <p className="text-gray-600 dark:text-gray-400">
              Remaining: <span className="font-medium text-red-600 dark:text-red-400">{formatCurrency(point.totalBalance)}</span>
            </p>
            {point.comparisonBalance !== undefined && (
              <p className="text-gray-600 dark:text-gray-400">
                With Extra: <span className="font-medium text-orange-600 dark:text-orange-400">{formatCurrency(point.comparisonBalance)}</span>
              </p>
            )}
            {point.debtsPaidOff && point.debtsPaidOff.length > 0 && (
              <p className="text-green-600 dark:text-green-400 font-medium mt-2">
                ✓ Paid off: {point.debtsPaidOff.join(', ')}
              </p>
            )}
          </div>
        </div>
      )
    }
    return null
  }

  return (
    <ResponsiveContainer width="100%" height={height}>
      <ComposedChart data={chartData} margin={{ top: 30, right: 10, left: 0, bottom: 0 }}>
        <defs>
          <linearGradient id="gradient-debt" x1="0" y1="0" x2="0" y2="1">
            <stop offset="5%" stopColor="#ef4444" stopOpacity={0.3} />
            <stop offset="95%" stopColor="#ef4444" stopOpacity={0} />
          </linearGradient>
          <linearGradient id="gradient-comparison" x1="0" y1="0" x2="0" y2="1">
            <stop offset="5%" stopColor="#f97316" stopOpacity={0.2} />
            <stop offset="95%" stopColor="#f97316" stopOpacity={0} />
          </linearGradient>
        </defs>
        <CartesianGrid 
          strokeDasharray="3 3" 
          stroke={isDark ? '#374151' : '#e5e7eb'} 
          vertical={false}
        />
        <XAxis 
          dataKey="month" 
          tick={{ fill: isDark ? '#9ca3af' : '#6b7280', fontSize: 12 }}
          tickLine={{ stroke: isDark ? '#4b5563' : '#d1d5db' }}
          axisLine={{ stroke: isDark ? '#4b5563' : '#d1d5db' }}
          label={{ value: 'Months', position: 'insideBottom', offset: -5, fill: isDark ? '#9ca3af' : '#6b7280' }}
        />
        <YAxis 
          tick={{ fill: isDark ? '#9ca3af' : '#6b7280', fontSize: 12 }}
          tickLine={{ stroke: isDark ? '#4b5563' : '#d1d5db' }}
          axisLine={{ stroke: isDark ? '#4b5563' : '#d1d5db' }}
          tickFormatter={formatYAxis}
          width={65}
        />
        <Tooltip content={<CustomTooltip />} />
        
        {comparisonData && (
          <Area
            type="monotone"
            dataKey="comparisonBalance"
            name="With Extra Payment"
            stroke="#f97316"
            strokeWidth={2}
            strokeDasharray="5 5"
            fill="url(#gradient-comparison)"
          />
        )}
        
        <Area
          type="monotone"
          dataKey="totalBalance"
          name="Debt Balance"
          stroke="#ef4444"
          strokeWidth={3}
          fill="url(#gradient-debt)"
        />
        
        {/* Milestone markers */}
        {milestones.map((milestone) => (
          <ReferenceLine
            key={`milestone-${milestone.month}`}
            x={milestone.month}
            stroke={isDark ? '#10b981' : '#059669'}
            strokeWidth={2}
            strokeDasharray="3 3"
            label={{
              value: `✓ ${milestone.debtName}`,
              position: 'insideTopRight',
              fill: isDark ? '#10b981' : '#059669',
              fontSize: 10,
              fontWeight: 600,
              offset: 5,
            }}
          />
        ))}
        
        <ReferenceLine
          y={0}
          stroke={isDark ? '#10b981' : '#059669'}
          strokeWidth={2}
          label={{
            value: 'Debt Free!',
            position: 'right',
            fill: isDark ? '#10b981' : '#059669',
            fontSize: 12,
            fontWeight: 600,
          }}
        />
      </ComposedChart>
    </ResponsiveContainer>
  )
}
