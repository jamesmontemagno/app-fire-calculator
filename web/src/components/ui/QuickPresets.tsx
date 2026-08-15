import type { LucideIcon } from 'lucide-react'
import { Gauge, Rabbit, Rocket, Gem, Zap } from 'lucide-react'

interface Preset {
  name: string
  icon: LucideIcon
  description: string
  values: Record<string, number>
}

interface QuickPresetsProps {
  onApply: (values: Record<string, number>) => void
  presets?: Preset[]
}

const defaultPresets: Preset[] = [
  {
    name: 'Conservative',
    icon: Gauge,
    description: '15% savings rate, 6% return',
    values: {
      currentAge: 30,
      retirementAge: 65,
      currentSavings: 50000,
      annualContribution: 12000,
      annualExpenses: 60000,
      expectedReturn: 0.06,
      inflationRate: 0.03,
      withdrawalRate: 0.04,
    },
  },
  {
    name: 'Moderate',
    icon: Rabbit,
    description: '25% savings rate, 7% return',
    values: {
      currentAge: 30,
      retirementAge: 55,
      currentSavings: 100000,
      annualContribution: 24000,
      annualExpenses: 48000,
      expectedReturn: 0.07,
      inflationRate: 0.03,
      withdrawalRate: 0.04,
    },
  },
  {
    name: 'Aggressive',
    icon: Rocket,
    description: '50% savings rate, 7% return',
    values: {
      currentAge: 30,
      retirementAge: 45,
      currentSavings: 150000,
      annualContribution: 48000,
      annualExpenses: 40000,
      expectedReturn: 0.07,
      inflationRate: 0.03,
      withdrawalRate: 0.04,
    },
  },
  {
    name: 'Fat FIRE',
    icon: Gem,
    description: 'High income, high expenses',
    values: {
      currentAge: 35,
      retirementAge: 50,
      currentSavings: 500000,
      annualContribution: 100000,
      annualExpenses: 120000,
      expectedReturn: 0.07,
      inflationRate: 0.03,
      withdrawalRate: 0.035,
    },
  },
]

export default function QuickPresets({ onApply, presets = defaultPresets }: QuickPresetsProps) {
  return (
    <div className="bg-surface-sunken rounded-container p-4">
      <h3 className="mb-3 flex items-center gap-2 text-sm font-semibold text-content-muted">
        <Zap className="h-4 w-4" aria-hidden="true" strokeWidth={1.5} />
        Quick Presets
      </h3>
      <div className="flex flex-wrap gap-2">
        {presets.map((preset) => {
          const Icon = preset.icon
          return (
          <button
            key={preset.name}
            onClick={() => onApply(preset.values)}
            className="group flex items-center gap-2 rounded-control border border-border-subtle bg-surface-raised px-3 py-2 text-left transition-colors duration-150 hover:border-accent motion-reduce:transition-none focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-surface"
          >
            <Icon className="h-4 w-4 shrink-0 text-content-subtle group-hover:text-accent" aria-hidden="true" strokeWidth={1.5} />
            <div>
              <p className="text-sm font-medium text-content group-hover:text-accent">
                {preset.name}
              </p>
              <p className="text-xs text-content-subtle">{preset.description}</p>
            </div>
          </button>
          )
        })}
      </div>
    </div>
  )
}
