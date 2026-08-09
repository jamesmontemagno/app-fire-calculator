import {
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  ReferenceLine,
} from 'recharts'
import { formatCurrency } from '../../utils/calculations'
import { useTheme } from '../../context/ThemeContext'

interface WithdrawalChartProps {
  data: { year: number; balance: number; withdrawal: number }[]
  height?: number
}

export default function WithdrawalChart({
  data,
  height = 300,
}: WithdrawalChartProps) {
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
      return (
        <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg p-3">
          <p className="font-semibold text-gray-900 dark:text-gray-100 mb-2">
            Year {point.year}
          </p>
          <div className="space-y-1 text-sm">
            <p className="text-gray-600 dark:text-gray-400">
              Balance: <span className="font-medium text-gray-900 dark:text-gray-100">{formatCurrency(point.balance)}</span>
            </p>
            <p className="text-gray-600 dark:text-gray-400">
              Withdrawal: <span className="font-medium text-gray-900 dark:text-gray-100">{formatCurrency(point.withdrawal)}</span>
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
          <linearGradient id="gradient-withdrawal" x1="0" y1="0" x2="0" y2="1">
            <stop offset="5%" stopColor="#0ea5e9" stopOpacity={0.3} />
            <stop offset="95%" stopColor="#0ea5e9" stopOpacity={0} />
          </linearGradient>
        </defs>
        <CartesianGrid 
          strokeDasharray="3 3" 
          stroke={isDark ? '#374151' : '#e5e7eb'} 
          vertical={false}
        />
        <XAxis 
          dataKey="year" 
          tick={{ fill: isDark ? '#9ca3af' : '#6b7280', fontSize: 12 }}
          tickLine={{ stroke: isDark ? '#4b5563' : '#d1d5db' }}
          axisLine={{ stroke: isDark ? '#4b5563' : '#d1d5db' }}
          tickFormatter={(value) => `Yr ${value}`}
        />
        <YAxis 
          tick={{ fill: isDark ? '#9ca3af' : '#6b7280', fontSize: 12 }}
          tickLine={{ stroke: isDark ? '#4b5563' : '#d1d5db' }}
          axisLine={{ stroke: isDark ? '#4b5563' : '#d1d5db' }}
          tickFormatter={formatYAxis}
          width={65}
        />
        <Tooltip content={<CustomTooltip />} />
        
        <Area
          type="monotone"
          dataKey="balance"
          name="Portfolio Balance"
          stroke="#0ea5e9"
          strokeWidth={2}
          fill="url(#gradient-withdrawal)"
        />
        
        <ReferenceLine
          y={0}
          stroke={isDark ? '#ef4444' : '#dc2626'}
          strokeWidth={1}
        />
      </AreaChart>
    </ResponsiveContainer>
  )
}
