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
import { formatCurrency } from '../../utils/calculations'
import { useTheme } from '../../context/ThemeContext'
import { chartTheme } from './chartTheme'
import { usePrefersReducedMotion } from '../../hooks/usePrefersReducedMotion'

export interface BalanceSeries {
  /** Key into each data row holding this series' dollar value. */
  key: string
  name: string
  tone?: 'primary' | 'secondary' | 'positive'
  dashed?: boolean
}

interface BalanceProjectionChartProps {
  /**
   * Rows keyed by the series and axis keys. Typed loosely so any projection point type can be
   * passed without a mapping step; non-numeric values are simply not plotted.
   */
  data: ReadonlyArray<object>
  /** Key into each data row used for the horizontal axis, typically a calendar year. */
  xKey: string
  xLabel?: string
  series: BalanceSeries[]
  height?: number
}

/**
 * Year-by-year account balances for calculators that trace a few dollar series over a fixed
 * horizon, such as the 72(t) commitment period or a Roth conversion ladder.
 */
export default function BalanceProjectionChart({
  data,
  xKey,
  xLabel,
  series,
  height = 300,
}: BalanceProjectionChartProps) {
  const { resolvedTheme } = useTheme()
  const c = chartTheme(resolvedTheme === 'dark')
  const animate = !usePrefersReducedMotion()

  const toneColor = (tone: BalanceSeries['tone']) => {
    switch (tone) {
      case 'secondary':
        return c.secondary
      case 'positive':
        return c.positive
      default:
        return c.primary
    }
  }

  const formatYAxis = (value: number) => {
    if (value >= 1_000_000) return `$${(value / 1_000_000).toFixed(1)}M`
    if (value >= 1_000) return `$${(value / 1_000).toFixed(0)}K`
    return `$${value}`
  }

  const CustomTooltip = ({ active, payload }: any) => {
    if (!active || !payload || payload.length === 0) return null
    const point = payload[0].payload as Record<string, unknown>
    return (
      <div className="rounded-container border border-border-subtle bg-surface-raised p-3 shadow-lg">
        <p className="mb-2 font-semibold text-content">
          {xLabel ? `${xLabel} ` : ''}{String(point[xKey])}
          {typeof point.age === 'number' && <span className="text-content-muted"> · Age {point.age}</span>}
        </p>
        <div className="space-y-1 text-sm">
          {series.map(item => {
            const value = point[item.key]
            if (typeof value !== 'number') return null
            return (
              <p key={item.key} className="text-content-muted">
                {item.name}:{' '}
                <span className="tabular font-medium" style={{ color: toneColor(item.tone) }}>
                  {formatCurrency(value)}
                </span>
              </p>
            )
          })}
        </div>
      </div>
    )
  }

  return (
    <ResponsiveContainer width="100%" height={height}>
      <LineChart data={[...data]} margin={{ top: 10, right: 10, left: 0, bottom: 0 }}>
        <CartesianGrid strokeDasharray="3 3" stroke={c.grid} vertical={false} />
        <XAxis
          dataKey={xKey}
          tick={{ fill: c.axisText, fontSize: 12 }}
          tickLine={{ stroke: c.axisLine }}
          axisLine={{ stroke: c.axisLine }}
        />
        <YAxis
          tick={{ fill: c.axisText, fontSize: 12 }}
          tickLine={{ stroke: c.axisLine }}
          axisLine={{ stroke: c.axisLine }}
          tickFormatter={formatYAxis}
          width={65}
          domain={['auto', 'auto']}
        />
        <Tooltip content={<CustomTooltip />} />
        <Legend
          wrapperStyle={{ paddingTop: 16 }}
          formatter={value => <span className="text-sm text-content-muted">{value}</span>}
        />
        {series.map(item => (
          <Line
            key={item.key}
            type="monotone"
            dataKey={item.key}
            name={item.name}
            stroke={toneColor(item.tone)}
            strokeWidth={2}
            strokeDasharray={item.dashed ? '5 5' : undefined}
            dot={false}
            activeDot={{ r: 4 }}
            isAnimationActive={animate}
          />
        ))}
      </LineChart>
    </ResponsiveContainer>
  )
}
