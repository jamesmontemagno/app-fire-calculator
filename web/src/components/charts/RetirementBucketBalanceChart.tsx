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

interface RetirementBucketBalanceChartProps {
  data: RetirementCashFlowPoint[]
  accounts: RetirementAccount[]
}

const COLORS = [
  '#8b5cf6',
  '#0ea5e9',
  '#14b8a6',
  '#f59e0b',
  '#ec4899',
  '#84cc16',
  '#f97316',
  '#6366f1',
]

const compactCurrency = (value: number) => {
  if (Math.abs(value) >= 1_000_000) return `$${(value / 1_000_000).toFixed(1)}M`
  if (Math.abs(value) >= 1_000) return `$${(value / 1_000).toFixed(0)}K`
  return formatCurrency(value)
}

interface ChartTooltipProps {
  active?: boolean
  payload?: Array<{ payload: RetirementCashFlowPoint }>
  accounts: RetirementAccount[]
}

function ChartTooltip({ active, payload, accounts }: ChartTooltipProps) {
  if (!active || !payload?.length) return null
  const point = payload[0].payload

  return (
    <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg p-3">
      <p className="font-semibold text-gray-900 dark:text-gray-100">
        Age {point.age} · {point.year}
      </p>
      <div className="space-y-1 text-sm mt-2">
        {accounts.map((account, index) => (
          <p key={account.id} className="text-gray-600 dark:text-gray-400">
            <span
              className="inline-block w-2 h-2 rounded-full mr-1.5"
              style={{ backgroundColor: COLORS[index % COLORS.length] }}
            />
            {account.name || `Account ${index + 1}`}:{' '}
            <strong className="text-gray-900 dark:text-gray-100">
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
  const axisColor = isDark ? '#9ca3af' : '#6b7280'
  const gridColor = isDark ? '#374151' : '#e5e7eb'
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
        <Tooltip content={<ChartTooltip accounts={accounts} />} />
        <Legend wrapperStyle={{ fontSize: 12 }} />
        {accounts.map((account, index) => (
          <Line
            key={account.id}
            type="monotone"
            dataKey={account.id}
            name={account.name || `Account ${index + 1}`}
            stroke={COLORS[index % COLORS.length]}
            strokeWidth={2}
            dot={false}
          />
        ))}
      </LineChart>
    </ResponsiveContainer>
  )
}
