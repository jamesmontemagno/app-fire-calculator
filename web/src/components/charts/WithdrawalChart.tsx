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
import { chartTheme } from './chartTheme'

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
  const c = chartTheme(isDark)

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
        <div className="rounded-container border border-border-subtle bg-surface-raised p-3 shadow-lg">
          <p className="mb-2 font-semibold text-content">
            Year {point.year}
          </p>
          <div className="space-y-1 text-sm">
            <p className="text-content-muted">
              Balance: <span className="tabular font-medium text-content">{formatCurrency(point.balance)}</span>
            </p>
            <p className="text-content-muted">
              Withdrawal: <span className="tabular font-medium text-content">{formatCurrency(point.withdrawal)}</span>
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
          stroke={c.grid} 
          vertical={false}
        />
        <XAxis 
          dataKey="year" 
          tick={{ fill: c.axisText, fontSize: 12 }}
          tickLine={{ stroke: c.axisLine }}
          axisLine={{ stroke: c.axisLine }}
          tickFormatter={(value) => `Yr ${value}`}
        />
        <YAxis 
          tick={{ fill: c.axisText, fontSize: 12 }}
          tickLine={{ stroke: c.axisLine }}
          axisLine={{ stroke: c.axisLine }}
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
          stroke={c.negative}
          strokeWidth={1}
        />
      </AreaChart>
    </ResponsiveContainer>
  )
}
