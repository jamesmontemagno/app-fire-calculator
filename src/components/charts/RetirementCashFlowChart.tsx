import {
  CartesianGrid,
  ComposedChart,
  Legend,
  Line,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import type { RetirementCashFlowPoint } from '../../utils/deferredCompensation'
import { formatCurrency } from '../../utils/calculations'
import { useTheme } from '../../context/ThemeContext'

interface RetirementCashFlowChartProps {
  data: RetirementCashFlowPoint[]
  view: 'portfolio' | 'withdrawals' | 'income-expenses'
}

const compactCurrency = (value: number) => {
  if (Math.abs(value) >= 1_000_000) return `$${(value / 1_000_000).toFixed(1)}M`
  if (Math.abs(value) >= 1_000) return `$${(value / 1_000).toFixed(0)}K`
  return `$${value}`
}

export default function RetirementCashFlowChart({ data, view }: RetirementCashFlowChartProps) {
  const { resolvedTheme } = useTheme()
  const isDark = resolvedTheme === 'dark'
  const axisColor = isDark ? '#9ca3af' : '#6b7280'
  const gridColor = isDark ? '#374151' : '#e5e7eb'

  const ChartTooltip = ({ active, payload }: any) => {
    if (!active || !payload?.length) return null
    const point = payload[0].payload as RetirementCashFlowPoint
    return (
      <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg p-3">
        <p className="font-semibold text-gray-900 dark:text-gray-100">
          Age {point.age} · {point.year}
        </p>
        <div className="space-y-1 text-sm mt-2">
          <p className="text-gray-600 dark:text-gray-400">
            Outside income: <strong className="text-green-600 dark:text-green-400">{formatCurrency(point.outsideIncome)}</strong>
          </p>
          <p className="text-gray-600 dark:text-gray-400">
            Deferred compensation: <strong className="text-indigo-600 dark:text-indigo-400">{formatCurrency(point.deferredIncome)}</strong>
          </p>
          <p className="text-gray-600 dark:text-gray-400">
            Expenses: <strong className="text-amber-600 dark:text-amber-400">{formatCurrency(point.expenses)}</strong>
          </p>
          <p className="text-gray-600 dark:text-gray-400">
            Portfolio withdrawal: <strong className="text-violet-600 dark:text-violet-400">{formatCurrency(point.portfolioWithdrawals)}</strong>
          </p>
          <p className="text-gray-600 dark:text-gray-400">
            Portfolio value: <strong className="text-sky-600 dark:text-sky-400">{formatCurrency(point.totalBalance)}</strong>
          </p>
        </div>
      </div>
    )
  }

  return (
    <ResponsiveContainer width="100%" height={340}>
      <ComposedChart data={data} margin={{ top: 10, right: 8, left: 4, bottom: 0 }}>
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
        <Tooltip content={<ChartTooltip />} />
        <Legend wrapperStyle={{ fontSize: 12 }} />
        {view === 'portfolio' && (
          <Line type="monotone" dataKey="totalBalance" name="Portfolio value" stroke="#0ea5e9" strokeWidth={2} dot={false} />
        )}
        {view === 'withdrawals' && (
          <Line type="monotone" dataKey="portfolioWithdrawals" name="Portfolio withdrawals" stroke="#8b5cf6" strokeWidth={2} dot={false} />
        )}
        {view === 'income-expenses' && (
          <>
            <Line type="monotone" dataKey="totalIncome" name="Income available" stroke="#16a34a" strokeWidth={2} dot={false} />
            <Line type="monotone" dataKey="expenses" name="Expenses" stroke="#f59e0b" strokeWidth={2} dot={false} />
          </>
        )}
      </ComposedChart>
    </ResponsiveContainer>
  )
}
