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
import { chartTheme, categorical } from './chartTheme'

interface RetirementCashFlowChartProps {
  data: RetirementCashFlowPoint[]
}

const compactCurrency = (value: number) => {
  if (Math.abs(value) >= 1_000_000) return `$${(value / 1_000_000).toFixed(1)}M`
  if (Math.abs(value) >= 1_000) return `$${(value / 1_000).toFixed(0)}K`
  return `$${value}`
}

export default function RetirementCashFlowChart({ data }: RetirementCashFlowChartProps) {
  const { resolvedTheme } = useTheme()
  const isDark = resolvedTheme === 'dark'
  const c = chartTheme(isDark)
  const cat = categorical(isDark)
  const axisColor = c.axisText
  const gridColor = c.grid

  const ChartTooltip = ({ active, payload }: any) => {
    if (!active || !payload?.length) return null
    const point = payload[0].payload as RetirementCashFlowPoint
    return (
      <div className="rounded-container border border-border-subtle bg-surface-raised p-3 shadow-lg">
        <p className="font-semibold text-content">
          Age {point.age} · {point.year}
        </p>
        <div className="space-y-1 text-sm mt-2">
          <p className="text-content-muted">
            Outside income: <strong className="tabular text-success">{formatCurrency(point.outsideIncome)}</strong>
          </p>
          <p className="text-content-muted">
            Deferred compensation: <strong className="tabular text-info">{formatCurrency(point.deferredIncome)}</strong>
          </p>
          <p className="text-content-muted">
            Core expenses: <strong className="tabular text-warning">{formatCurrency(point.coreExpenses)}</strong>
          </p>
          {point.additionalExpenses > 0 && (
            <p className="text-content-muted">
              Additional expenses: <strong className="tabular text-accent">{formatCurrency(point.additionalExpenses)}</strong>
            </p>
          )}
          <p className="text-content-muted">
            Total expenses: <strong className="tabular text-warning">{formatCurrency(point.expenses)}</strong>
          </p>
          <p className="text-content-muted">
            Portfolio withdrawal: <strong className="tabular text-content">{formatCurrency(point.portfolioWithdrawals)}</strong>
          </p>
          <p className="text-content-muted">
            Portfolio value: <strong className="tabular text-info">{formatCurrency(point.totalBalance)}</strong>
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
          yAxisId="cash-flow"
          tick={{ fill: axisColor, fontSize: 12 }}
          axisLine={{ stroke: gridColor }}
          tickLine={{ stroke: gridColor }}
          tickFormatter={compactCurrency}
          width={66}
        />
        <YAxis
          yAxisId="portfolio"
          orientation="right"
          tick={{ fill: axisColor, fontSize: 12 }}
          axisLine={{ stroke: gridColor }}
          tickLine={{ stroke: gridColor }}
          tickFormatter={compactCurrency}
          width={66}
        />
        <Tooltip content={<ChartTooltip />} />
        <Legend wrapperStyle={{ fontSize: 12 }} />
        <Line yAxisId="cash-flow" type="monotone" dataKey="totalIncome" name="Income available (after tax)" stroke={c.positive} strokeWidth={2} dot={false} />
        <Line yAxisId="cash-flow" type="monotone" dataKey="expenses" name="Expenses" stroke={cat[3]} strokeWidth={2} dot={false} />
        <Line yAxisId="cash-flow" type="monotone" dataKey="portfolioWithdrawals" name="Gap withdrawals (after tax)" stroke={cat[5]} strokeWidth={2} strokeDasharray="4 4" dot={false} />
        <Line yAxisId="portfolio" type="monotone" dataKey="totalBalance" name="Portfolio value" stroke={c.secondary} strokeWidth={2} dot={false} />
      </ComposedChart>
    </ResponsiveContainer>
  )
}
