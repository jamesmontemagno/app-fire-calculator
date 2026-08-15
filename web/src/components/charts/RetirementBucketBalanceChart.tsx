import {
  CartesianGrid,
  Legend,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import type {
  RetirementAccount,
  RetirementCashFlowPoint,
} from '../../utils/deferredCompensation'
import { formatCurrency } from '../../utils/calculations'
import { useTheme } from '../../context/ThemeContext'
import { categorical, chartTheme } from './chartTheme'

interface RetirementBucketBalanceChartProps {
  data: RetirementCashFlowPoint[]
  accounts: RetirementAccount[]
}


const compactCurrency = (value: number) => {
  if (Math.abs(value) >= 1_000_000) return `$${(value / 1_000_000).toFixed(1)}M`
  if (Math.abs(value) >= 1_000) return `$${(value / 1_000).toFixed(0)}K`
  return formatCurrency(value)
}

interface ChartTooltipProps {
  active?: boolean
  payload?: Array<{ payload: RetirementCashFlowPoint }>
  accounts: RetirementAccount[]
  colors: string[]
}

function ChartTooltip({ active, payload, accounts, colors }: ChartTooltipProps) {
  if (!active || !payload?.length) return null
  const point = payload[0].payload

  return (
    <div className="rounded-container border border-border-subtle bg-surface-raised p-3 shadow-lg">
      <p className="font-semibold text-content">
        Age {point.age} · {point.year}
      </p>
      <div className="space-y-1 text-sm mt-2">
        {accounts.map((account, index) => (
          <p key={account.id} className="text-content-muted">
            <span
              className="inline-block w-2 h-2 rounded-full mr-1.5"
              style={{ backgroundColor: colors[index % colors.length] }}
            />
            {account.name || `Account ${index + 1}`}:{' '}
            <strong className="tabular font-medium text-content">
              {formatCurrency(point.balances[account.id] ?? 0)}
            </strong>
          </p>
        ))}
      </div>
    </div>
  )
}

export default function RetirementBucketBalanceChart({
  data,
  accounts,
}: RetirementBucketBalanceChartProps) {
  const { resolvedTheme } = useTheme()
  const isDark = resolvedTheme === 'dark'
  const c = chartTheme(isDark)
  const axisColor = c.axisText
  const gridColor = c.grid
  const colors = categorical(isDark)
  const chartData = data.map(point => ({ ...point, ...point.balances }))

  return (
    <ResponsiveContainer width="100%" height={340}>
      <LineChart data={chartData} margin={{ top: 10, right: 8, left: 4, bottom: 0 }}>
        <CartesianGrid strokeDasharray="3 3" stroke={gridColor} vertical={false} />
        <XAxis
          dataKey="age"
          tick={{ fill: axisColor, fontSize: 12 }}
          axisLine={{ stroke: gridColor }}
          tickLine={{ stroke: gridColor }}
          tickFormatter={age => `Age ${age}`}
        />
        <YAxis
          tick={{ fill: axisColor, fontSize: 12 }}
          axisLine={{ stroke: gridColor }}
          tickLine={{ stroke: gridColor }}
          tickFormatter={compactCurrency}
          width={66}
        />
        <Tooltip content={<ChartTooltip accounts={accounts} colors={colors} />} />
        <Legend wrapperStyle={{ fontSize: 12 }} />
        {accounts.map((account, index) => (
          <Line
            key={account.id}
            type="monotone"
            dataKey={account.id}
            name={account.name || `Account ${index + 1}`}
            stroke={colors[index % colors.length]}
            strokeWidth={2}
            dot={false}
          />
        ))}
      </LineChart>
    </ResponsiveContainer>
  )
}
