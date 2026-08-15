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
import { Check } from 'lucide-react'
import { formatCurrency } from '../../utils/calculations'
import { useTheme } from '../../context/ThemeContext'
import { chartTheme } from './chartTheme'
import type { DebtPayoffMonth } from '../../utils/calculations'

interface DebtBalanceChartProps {
  data: DebtPayoffMonth[]
  milestones?: { month: number; debtName: string }[]
  comparisonData?: DebtPayoffMonth[]
  height?: number
}

/**
 * Recharts' `label` accepts a render function returning an SVG element, not only
 * the `{ value }` object form, so the milestone marker is drawn as a real check
 * mark on the chart's own canvas. The mark is decorative: the dashed rule and
 * the debt name beside it already carry the meaning, and the whole chart is
 * exposed through the data table rather than through its SVG internals.
 */
function renderMilestoneLabel(debtName: string, color: string) {
  return (props: any) => {
    const { viewBox } = props
    const x = (viewBox?.x ?? 0) + 6
    const y = (viewBox?.y ?? 0) + 10
    return (
      <g aria-hidden="true">
        <path
          d={`M${x} ${y + 3.5}l2.6 2.6 5-5.4`}
          fill="none"
          stroke={color}
          strokeWidth={1.8}
          strokeLinecap="round"
          strokeLinejoin="round"
        />
        <text x={x + 11} y={y + 7} fill={color} fontSize={10} fontWeight={600}>
          {debtName}
        </text>
      </g>
    )
  }
}

export default function DebtBalanceChart({
  data,
  milestones = [],
  comparisonData,
  height = 300,
}: DebtBalanceChartProps) {
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

  // Combine data for comparison
  const chartData = data.map((point, index) => ({
    ...point,
    comparisonBalance: comparisonData?.[index]?.totalBalance,
  }))

  const CustomTooltip = ({ active, payload }: any) => {
    if (active && payload && payload.length) {
      const point = payload[0].payload
      return (
        <div className="rounded-container border border-border-subtle bg-surface-raised p-3 shadow-lg">
          <p className="mb-2 font-semibold text-content">
            Month {point.month}
          </p>
          <div className="space-y-1 text-sm">
            <p className="text-content-muted">
              Remaining: <span className="tabular font-medium text-danger">{formatCurrency(point.totalBalance)}</span>
            </p>
            {point.comparisonBalance !== undefined && (
              <p className="text-content-muted">
                With Extra: <span className="tabular font-medium text-accent">{formatCurrency(point.comparisonBalance)}</span>
              </p>
            )}
            {point.debtsPaidOff && point.debtsPaidOff.length > 0 && (
              <p className="mt-2 flex items-start gap-1.5 font-medium text-success">
                <Check className="mt-0.5 h-4 w-4 shrink-0" aria-hidden="true" strokeWidth={2} />
                <span>Paid off: {point.debtsPaidOff.join(', ')}</span>
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
            <stop offset="5%" stopColor={c.negative} stopOpacity={0.24} />
            <stop offset="95%" stopColor={c.negative} stopOpacity={0} />
          </linearGradient>
          <linearGradient id="gradient-comparison" x1="0" y1="0" x2="0" y2="1">
            <stop offset="5%" stopColor={c.primary} stopOpacity={0.18} />
            <stop offset="95%" stopColor={c.primary} stopOpacity={0} />
          </linearGradient>
        </defs>
        <CartesianGrid strokeDasharray="3 3" stroke={c.grid} vertical={false} />
        <XAxis 
          dataKey="month" 
          tick={{ fill: c.axisText, fontSize: 12 }}
          tickLine={{ stroke: c.axisLine }}
          axisLine={{ stroke: c.axisLine }}
          label={{ value: 'Months', position: 'insideBottom', offset: -5, fill: c.axisText }}
        />
        <YAxis 
          tick={{ fill: c.axisText, fontSize: 12 }}
          tickLine={{ stroke: c.axisLine }}
          axisLine={{ stroke: c.axisLine }}
          tickFormatter={formatYAxis}
          width={65}
        />
        <Tooltip content={<CustomTooltip />} />
        
        {comparisonData && (
          <Area
            type="monotone"
            dataKey="comparisonBalance"
            name="With Extra Payment"
            stroke={c.primary}
            strokeWidth={2}
            strokeDasharray="5 5"
            fill="url(#gradient-comparison)"
          />
        )}
        
        <Area
          type="monotone"
          dataKey="totalBalance"
          name="Debt Balance"
          stroke={c.negative}
          strokeWidth={2.5}
          fill="url(#gradient-debt)"
        />
        
        {/* Milestone markers */}
        {milestones.map((milestone) => (
          <ReferenceLine
            key={`milestone-${milestone.month}`}
            x={milestone.month}
            stroke={c.positive}
            strokeWidth={2}
            strokeDasharray="3 3"
            label={renderMilestoneLabel(milestone.debtName, c.positive)}
          />
        ))}
        
        <ReferenceLine
          y={0}
          stroke={c.positive}
          strokeWidth={2}
          label={{
            value: 'Debt free',
            position: 'right',
            fill: c.positive,
            fontSize: 12,
            fontWeight: 600,
          }}
        />
      </ComposedChart>
    </ResponsiveContainer>
  )
}
