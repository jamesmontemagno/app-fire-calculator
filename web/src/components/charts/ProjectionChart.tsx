import { Check } from 'lucide-react'
import {
  AreaChart,
  Area,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  ReferenceLine,
  Legend,
} from 'recharts'
import type { ProjectionPoint } from '../../utils/calculations'
import { formatCurrency } from '../../utils/calculations'
import { useTheme } from '../../context/ThemeContext'
import { chartTheme } from './chartTheme'
import { usePrefersReducedMotion } from '../../hooks/usePrefersReducedMotion'

interface ProjectionChartProps {
  data: ProjectionPoint[]
  fireNumber?: number
  /**
   * Supplying the inflation rate lets the chart draw the FIRE target in future dollars, so the
   * nominal series and the today's-dollar series are each compared against a target in their own units.
   */
  inflationRate?: number
  showInflationAdjusted?: boolean
  showMilestones?: boolean
  height?: number
  colorScheme?: 'orange' | 'green' | 'purple' | 'blue' | 'amber'
}


export default function ProjectionChart({
  data,
  fireNumber,
  inflationRate,
  showInflationAdjusted = true,
  showMilestones = true,
  height = 300,
  colorScheme = 'orange',
}: ProjectionChartProps) {
  const { resolvedTheme } = useTheme()
  const isDark = resolvedTheme === 'dark'
  const c = chartTheme(isDark)
  const colors = { primary: c.primary, secondary: c.secondary }
  const prefersReducedMotion = usePrefersReducedMotion()
  const animate = !prefersReducedMotion

  // The FIRE number is stated in today's dollars, so the nominal series has to be compared against a
  // target that inflates with it. Both crossings then land on the same age as the headline FIRE age.
  const showFutureTarget = fireNumber !== undefined && inflationRate !== undefined
  const chartData = showFutureTarget
    ? data.map((point, index) => ({
        ...point,
        fireTarget: Math.round(fireNumber * Math.pow(1 + inflationRate, index)),
      }))
    : data

  // Calculate milestone values
  const milestones = fireNumber ? [
    { percent: 25, value: fireNumber * 0.25, label: '25%' },
    { percent: 50, value: fireNumber * 0.5, label: '50%' },
    { percent: 75, value: fireNumber * 0.75, label: '75%' },
  ] : []

  const formatYAxis = (value: number) => {
    if (value >= 1000000) {
      return `$${(value / 1000000).toFixed(1)}M`
    }
    if (value >= 1000) {
      return `$${(value / 1000).toFixed(0)}K`
    }
    return `$${value}`
  }

  // Calculate max plotted value for Y-axis domain
  const maxPortfolio = Math.max(
    ...data.map(d => d.portfolio),
    ...(showFutureTarget ? [fireNumber * Math.pow(1 + inflationRate, Math.max(0, data.length - 1))] : []),
  )
  const yAxisMax = Math.ceil(maxPortfolio * 1.05) // Add 5% padding

  const CustomTooltip = ({ active, payload }: any) => {
    if (active && payload && payload.length) {
      const point = payload[0].payload as ProjectionPoint & { fireTarget?: number }
      const targetForYear = point.fireTarget ?? fireNumber
      const reachedFire = targetForYear !== undefined && point.portfolio >= targetForYear
      return (
        <div className="rounded-container border border-border-subtle bg-surface-raised p-3 shadow-lg">
          <p className="mb-2 font-semibold text-content">
            Age {point.age} ({point.year})
          </p>
          <div className="space-y-1 text-sm">
            <p className="text-content-muted">
              Portfolio (future $): <span className="font-medium" style={{ color: colors.primary }}>{formatCurrency(point.portfolio)}</span>
            </p>
            {showInflationAdjusted && (
              <p className="text-content-muted">
                In today&apos;s dollars: <span className="font-medium" style={{ color: colors.secondary }}>{formatCurrency(point.inflationAdjusted)}</span>
              </p>
            )}
            <p className="text-content-muted">
              Total Contributed: <span className="tabular font-medium text-content">{formatCurrency(point.totalContributions)}</span>
            </p>
            {fireNumber !== undefined && (
              <p className={reachedFire ? 'font-medium text-success' : 'text-content-muted'}>
                FIRE Number: <span className="tabular font-medium">{formatCurrency(fireNumber)}</span>
                {showFutureTarget && (
                  <span className="tabular ml-1">({formatCurrency(point.fireTarget ?? fireNumber)} in future $)</span>
                )}
                {reachedFire && (
                  <Check className="ml-1 inline h-4 w-4 align-text-bottom" aria-label="Reached" strokeWidth={2} />
                )}
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
      <AreaChart data={chartData} margin={{ top: 10, right: 10, left: 0, bottom: 0 }}>
        <defs>
          <linearGradient id={`gradient-${colorScheme}`} x1="0" y1="0" x2="0" y2="1">
            <stop offset="5%" stopColor={colors.primary} stopOpacity={0.3} />
            <stop offset="95%" stopColor={colors.primary} stopOpacity={0} />
          </linearGradient>
          <linearGradient id={`gradient-${colorScheme}-secondary`} x1="0" y1="0" x2="0" y2="1">
            <stop offset="5%" stopColor={colors.secondary} stopOpacity={0.2} />
            <stop offset="95%" stopColor={colors.secondary} stopOpacity={0} />
          </linearGradient>
        </defs>
        <CartesianGrid strokeDasharray="3 3" stroke={c.grid} vertical={false} />
        <XAxis 
          dataKey="age" 
          tick={{ fill: c.axisText, fontSize: 12 }}
          tickLine={{ stroke: c.axisLine }}
          axisLine={{ stroke: c.axisLine }}
          tickFormatter={(value) => `${value}`}
        />
        <YAxis 
          tick={{ fill: c.axisText, fontSize: 12 }}
          tickLine={{ stroke: c.axisLine }}
          axisLine={{ stroke: c.axisLine }}
          tickFormatter={formatYAxis}
          width={65}
          domain={[0, yAxisMax]}
        />
        <Tooltip content={<CustomTooltip />} />
        <Legend 
          wrapperStyle={{ paddingTop: 16 }}
          formatter={(value) => <span className="text-sm text-content-muted">{value}</span>}
        />
        
        {showInflationAdjusted && (
          <Area
            type="monotone"
            dataKey="inflationAdjusted"
            name="In Today's Dollars"
            stroke={colors.secondary}
            strokeWidth={2}
            fill={`url(#gradient-${colorScheme}-secondary)`}
            strokeDasharray="5 5"
            isAnimationActive={animate}
          />
        )}
        
        <Area
          type="monotone"
          dataKey="portfolio"
          name="Portfolio Value (Future Dollars)"
          stroke={colors.primary}
          strokeWidth={2}
          fill={`url(#gradient-${colorScheme})`}
          isAnimationActive={animate}
        />

        {showFutureTarget && (
          <Line
            type="monotone"
            dataKey="fireTarget"
            name="FIRE Target (Future Dollars)"
            stroke={c.negative}
            strokeWidth={2}
            strokeDasharray="8 4"
            dot={false}
            activeDot={false}
            isAnimationActive={animate}
          />
        )}
        
        {/* Milestone lines */}
        {showMilestones && milestones.map((milestone) => (
          <ReferenceLine
            key={milestone.percent}
            y={milestone.value}
            stroke={c.neutral}
            strokeWidth={1}
            strokeDasharray="4 4"
            label={{
              value: milestone.label,
              fill: c.neutral,
              fontSize: 10,
              position: 'left',
            }}
          />
        ))}
        
        {fireNumber && (
          <ReferenceLine
            y={fireNumber}
            stroke={c.negative}
            strokeWidth={2}
            strokeDasharray="8 4"
            label={{
              value: showFutureTarget ? "FIRE (today's $)" : 'FIRE',
              fill: c.negative,
              fontSize: 11,
              fontWeight: 600,
              position: 'insideBottomLeft',
            }}
          />
        )}
      </AreaChart>
    </ResponsiveContainer>
  )
}
