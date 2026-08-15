/**
 * Centralized calculator metadata
 * Single source of truth for calculator names, icons, accents, and descriptions
 */

import type { LucideIcon } from 'lucide-react'
import {
  BriefcaseMedical,
  CalendarClock,
  Coffee,
  Coins,
  CreditCard,
  Flame,
  HandCoins,
  PiggyBank,
  RotateCw,
  Sprout,
  TreePalm,
} from 'lucide-react'

export interface CalculatorMetadata {
  path: string
  /**
   * Rendered as a component, not a string. Every usage is decorative: the
   * calculator name is always adjacent, so callers pass `aria-hidden`.
   */
  icon: LucideIcon
  name: string
  label: string // Short label for navigation
  description: string
  /** Tailwind text-colour class from the harmonized calc token family. Icon glyph only. */
  accent: string
  audience: string // Target audience description
  storagePrefix: 'standard' | 'deferred'
}

export const calculators: CalculatorMetadata[] = [
  {
    path: '/standard',
    icon: Flame,
    name: 'Standard FIRE',
    label: 'Standard FIRE',
    description: 'The classic 25x expenses rule — calculate your "magic number" for full financial independence.',
    accent: 'text-calc-standard',
    audience: 'Best for: Anyone starting their FI journey',
    storagePrefix: 'standard',
  },
  {
    path: '/coast',
    icon: TreePalm,
    name: 'Coast FIRE',
    label: 'Coast FIRE',
    description: 'Find how much you need now so compound growth does the rest — then coast to retirement.',
    accent: 'text-calc-coast',
    audience: 'Best for: Young savers wanting flexibility',
    storagePrefix: 'standard',
  },
  {
    path: '/lean',
    icon: Sprout,
    name: 'Lean FIRE',
    label: 'Lean FIRE',
    description: 'Achieve FI faster with a minimalist lifestyle — perfect for frugal-minded planners.',
    accent: 'text-calc-lean',
    audience: 'Best for: Minimalists & early retirees',
    storagePrefix: 'standard',
  },
  {
    path: '/fat',
    icon: Coins,
    name: 'Fat FIRE',
    label: 'Fat FIRE',
    description: 'Retire without compromise — calculate FI while maintaining a comfortable lifestyle.',
    accent: 'text-calc-fat',
    audience: 'Best for: High earners & luxury seekers',
    storagePrefix: 'standard',
  },
  {
    path: '/barista',
    icon: Coffee,
    name: 'Barista FIRE',
    label: 'Barista FIRE',
    description: 'Blend part-time work with portfolio income — retire from corporate life earlier.',
    accent: 'text-calc-barista',
    audience: 'Best for: Those wanting work-life balance',
    storagePrefix: 'standard',
  },
  {
    path: '/reverse',
    icon: RotateCw,
    name: 'Reverse FIRE',
    label: 'Reverse FIRE',
    description: 'Work backwards — set your target age and find out how much you need to save monthly.',
    accent: 'text-calc-reverse',
    audience: 'Best for: Goal-oriented planners',
    storagePrefix: 'standard',
  },
  {
    path: '/withdrawal',
    icon: HandCoins,
    name: 'Withdrawal Rate',
    label: 'Withdrawal Rate',
    description: "Test your portfolio's longevity — find your safe withdrawal rate for any scenario.",
    accent: 'text-calc-withdrawal',
    audience: 'Best for: Those at or near FIRE',
    storagePrefix: 'standard',
  },
  {
    path: '/savings-rate',
    icon: PiggyBank,
    name: 'Savings & Investment Rate',
    label: 'Savings & Investment Rate',
    description: 'The most important metric — see how your savings rate impacts your time to FIRE.',
    accent: 'text-calc-savings',
    audience: 'Best for: Understanding your FI timeline',
    storagePrefix: 'standard',
  },
  {
    path: '/debt-payoff',
    icon: CreditCard,
    name: 'Debt Payoff',
    label: 'Debt Payoff',
    description: 'Eliminate debt faster with Snowball or Avalanche strategies — compare methods and see the impact of extra payments.',
    accent: 'text-calc-debt',
    audience: 'Best for: Tackling multiple debts strategically',
    storagePrefix: 'standard',
  },
  {
    path: '/healthcare',
    icon: BriefcaseMedical,
    name: 'Healthcare Gap',
    label: 'Healthcare Gap',
    description: 'The hidden cost of early retirement — estimate healthcare costs before Medicare.',
    accent: 'text-calc-healthcare',
    audience: 'Best for: US-based early retirees',
    storagePrefix: 'standard',
  },
  {
    path: '/retirement-cash-flow',
    icon: CalendarClock,
    name: 'Retirement Cash Flow',
    label: 'Retirement Cash Flow',
    description: 'Coordinate deferred payouts, retirement accounts, savings, and semi-retirement income in one cash-flow plan.',
    accent: 'text-calc-cashflow',
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
