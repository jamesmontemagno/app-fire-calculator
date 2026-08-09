import {
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Legend,
} from 'recharts'
import { formatCurrency } from '../../utils/calculations'
import { useTheme } from '../../context/ThemeContext'
import type { DebtPayoffMonth } from '../../utils/calculations'

interface DebtBreakdownChartProps {
  data: DebtPayoffMonth[]
  height?: number
}

export default function DebtBreakdownChart({
  data,
  height = 300,
}: DebtBreakdownChartProps) {
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

  const CustomTooltip = ({ active, payload }: any) => {
    if (active && payload && payload.length) {
      const point = payload[0].payload
      const totalPaid = point.cumulativePrincipal + point.cumulativeInterest
      return (
        <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg p-3">
          <p className="font-semibold text-gray-900 dark:text-gray-100 mb-2">
            Month {point.month}
          </p>
          <div className="space-y-1 text-sm">
            <p className="text-gray-600 dark:text-gray-400">
              Total Paid: <span className="font-medium text-gray-900 dark:text-gray-100">{formatCurrency(totalPaid)}</span>
            </p>
            <p className="text-gray-600 dark:text-gray-400">
              Principal: <span className="font-medium text-emerald-600 dark:text-emerald-400">{formatCurrency(point.cumulativePrincipal)}</span>
            </p>
            <p className="text-gray-600 dark:text-gray-400">
              Interest: <span className="font-medium text-red-600 dark:text-red-400">{formatCurrency(point.cumulativeInterest)}</span>
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
          <linearGradient id="gradient-principal" x1="0" y1="0" x2="0" y2="1">
            <stop offset="5%" stopColor="#10b981" stopOpacity={0.6} />
            <stop offset="95%" stopColor="#10b981" stopOpacity={0.3} />
          </linearGradient>
          <linearGradient id="gradient-interest" x1="0" y1="0" x2="0" y2="1">
            <stop offset="5%" stopColor="#ef4444" stopOpacity={0.6} />
            <stop offset="95%" stopColor="#ef4444" stopOpacity={0.3} />
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
        <Legend 
          wrapperStyle={{ paddingTop: '10px' }}
          iconType="square"
          formatter={(value) => <span className={isDark ? 'text-gray-300' : 'text-gray-700'}>{value}</span>}
        />
        
        <Area
          type="monotone"
          dataKey="cumulativePrincipal"
          stackId="1"
          name="Principal Paid"
          stroke="#10b981"
          strokeWidth={2}
          fill="url(#gradient-principal)"
        />
        
        <Area
          type="monotone"
          dataKey="cumulativeInterest"
          stackId="1"
          name="Interest Paid"
          stroke="#ef4444"
          strokeWidth={2}
          fill="url(#gradient-interest)"
        />
      </AreaChart>
    </ResponsiveContainer>
  )
}
