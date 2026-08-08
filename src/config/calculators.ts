/**
 * Centralized calculator metadata
 * Single source of truth for calculator names, colors, icons, and descriptions
 */

export interface CalculatorMetadata {
  path: string
  icon: string
  name: string
  label: string // Short label for navigation
  description: string
  color: string // Tailwind text color class
  bgColor: string // Background color class
  borderColor: string // Border color class
  audience: string // Target audience description
  storagePrefix?: 'standard' | 'deferred'
}

export const calculators: CalculatorMetadata[] = [
  {
    path: '/standard',
    icon: '🎯',
    name: 'Standard FIRE',
    label: 'Standard FIRE',
    description: 'The classic 25x expenses rule — calculate your "magic number" for full financial independence.',
    color: 'text-orange-500',
    bgColor: 'bg-orange-50 dark:bg-orange-900/20',
    borderColor: 'border-orange-200 dark:border-orange-800',
    audience: 'Best for: Anyone starting their FI journey',
  },
  {
    path: '/coast',
    icon: '⛵',
    name: 'Coast FIRE',
    label: 'Coast FIRE',
    description: 'Find how much you need now so compound growth does the rest — then coast to retirement.',
    color: 'text-blue-500',
    bgColor: 'bg-blue-50 dark:bg-blue-900/20',
    borderColor: 'border-blue-200 dark:border-blue-800',
    audience: 'Best for: Young savers wanting flexibility',
  },
  {
    path: '/lean',
    icon: '🌿',
    name: 'Lean FIRE',
    label: 'Lean FIRE',
    description: 'Achieve FI faster with a minimalist lifestyle — perfect for frugal-minded planners.',
    color: 'text-green-500',
    bgColor: 'bg-green-50 dark:bg-green-900/20',
    borderColor: 'border-green-200 dark:border-green-800',
    audience: 'Best for: Minimalists & early retirees',
  },
  {
    path: '/fat',
    icon: '💎',
    name: 'Fat FIRE',
    label: 'Fat FIRE',
    description: 'Retire without compromise — calculate FI while maintaining a comfortable lifestyle.',
    color: 'text-purple-500',
    bgColor: 'bg-purple-50 dark:bg-purple-900/20',
    borderColor: 'border-purple-200 dark:border-purple-800',
    audience: 'Best for: High earners & luxury seekers',
  },
  {
    path: '/barista',
    icon: '☕',
    name: 'Barista FIRE',
    label: 'Barista FIRE',
    description: 'Blend part-time work with portfolio income — retire from corporate life earlier.',
    color: 'text-amber-600',
    bgColor: 'bg-amber-50 dark:bg-amber-900/20',
    borderColor: 'border-amber-200 dark:border-amber-800',
    audience: 'Best for: Those wanting work-life balance',
  },
  {
    path: '/reverse',
    icon: '🔄',
    name: 'Reverse FIRE',
    label: 'Reverse FIRE',
    description: 'Work backwards — set your target age and find out how much you need to save monthly.',
    color: 'text-teal-500',
    bgColor: 'bg-teal-50 dark:bg-teal-900/20',
    borderColor: 'border-teal-200 dark:border-teal-800',
    audience: 'Best for: Goal-oriented planners',
  },
  {
    path: '/withdrawal',
    icon: '📊',
    name: 'Withdrawal Rate',
    label: 'Withdrawal Rate',
    description: "Test your portfolio's longevity — find your safe withdrawal rate for any scenario.",
    color: 'text-sky-500',
    bgColor: 'bg-sky-50 dark:bg-sky-900/20',
    borderColor: 'border-sky-200 dark:border-sky-800',
    audience: 'Best for: Those at or near FIRE',
  },
  {
    path: '/savings-rate',
    icon: '🧮',
    name: 'Savings & Investment Rate',
    label: 'Savings & Investment Rate',
    description: 'The most important metric — see how your savings rate impacts your time to FIRE.',
    color: 'text-indigo-500',
    bgColor: 'bg-indigo-50 dark:bg-indigo-900/20',
    borderColor: 'border-indigo-200 dark:border-indigo-800',
    audience: 'Best for: Understanding your FI timeline',
  },
  {
    path: '/debt-payoff',
    icon: '💳',
    name: 'Debt Payoff',
    label: 'Debt Payoff',
    description: 'Eliminate debt faster with Snowball or Avalanche strategies — compare methods and see the impact of extra payments.',
    color: 'text-red-500',
    bgColor: 'bg-red-50 dark:bg-red-900/20',
    borderColor: 'border-red-200 dark:border-red-800',
    audience: 'Best for: Tackling multiple debts strategically',
  },
  {
    path: '/healthcare',
    icon: '🏥',
    name: 'Healthcare Gap',
    label: 'Healthcare Gap',
    description: 'The hidden cost of early retirement — estimate healthcare costs before Medicare.',
    color: 'text-rose-500',
    bgColor: 'bg-rose-50 dark:bg-rose-900/20',
    borderColor: 'border-rose-200 dark:border-rose-800',
    audience: 'Best for: US-based early retirees',
  },
  {
    path: '/retirement-cash-flow',
    icon: '🗓️',
    name: 'Retirement Cash Flow',
    label: 'Retirement Cash Flow',
    description: 'Coordinate deferred payouts, retirement accounts, savings, and semi-retirement income in one cash-flow plan.',
    color: 'text-indigo-500',
    bgColor: 'bg-indigo-50 dark:bg-indigo-900/20',
    borderColor: 'border-indigo-200 dark:border-indigo-800',
    audience: 'Best for: Complex, multi-account retirement plans',
    storagePrefix: 'deferred',
  },
]

/**
 * Get calculator metadata by path
 */
export function getCalculatorByPath(path: string): CalculatorMetadata | undefined {
  return calculators.find(calc => calc.path === path)
}

/**
 * Get calculator metadata by name
 */
export function getCalculatorByName(name: string): CalculatorMetadata | undefined {
  return calculators.find(calc => calc.name.toLowerCase() === name.toLowerCase())
}
