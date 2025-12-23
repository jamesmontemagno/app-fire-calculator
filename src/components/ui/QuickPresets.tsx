interface Preset {
  name: string
  icon: string
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
    icon: '🐢',
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
    icon: '🏃',
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
    icon: '🚀',
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
    icon: '💎',
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
    <div className="bg-gray-50 dark:bg-gray-800/50 rounded-xl p-4">
      <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3 flex items-center gap-2">
        ⚡ Quick Presets
      </h3>
      <div className="flex flex-wrap gap-2">
        {presets.map((preset) => (
          <button
            key={preset.name}
            onClick={() => onApply(preset.values)}
            className="group flex items-center gap-2 px-3 py-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg hover:border-fire-400 dark:hover:border-fire-500 hover:shadow-sm transition-all text-left"
          >
            <span className="text-lg">{preset.icon}</span>
            <div>
              <p className="text-sm font-medium text-gray-900 dark:text-gray-100 group-hover:text-fire-600 dark:group-hover:text-fire-400">
                {preset.name}
              </p>
              <p className="text-xs text-gray-500 dark:text-gray-400">{preset.description}</p>
            </div>
          </button>
        ))}
      </div>
    </div>
  )
}
