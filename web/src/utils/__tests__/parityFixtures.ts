import rawFixture from '../../../../shared/parity/fire-parity-cases.json'

import type { ContributionGrowth, DebtItem, FIREInputs } from '../calculations'
import type {
  DeferredCompensationInputs,
  RetirementAccount,
  RetirementExpense,
  RetirementIncomeSource,
} from '../deferredCompensation'

/**
 * Typed view over `shared/parity/fire-parity-cases.json`.
 *
 * The fixture is imported as a module rather than read with `fs` so TypeScript sees its literal
 * shape. The `satisfies` assertion at the bottom of this file therefore makes a malformed case
 * fail `npm run build` at compile time, instead of silently producing a plausible-looking wrong
 * answer at runtime. That matters here: during the audit a malformed fixture produced a confident
 * 22-row bug report that was entirely fictional.
 */

/** JSON has no infinity literal, so the fixture carries it as a string sentinel. */
export type FixtureNumber = number | 'Infinity' | '-Infinity'

export function decode(value: FixtureNumber): number {
  if (value === 'Infinity') return Number.POSITIVE_INFINITY
  if (value === '-Infinity') return Number.NEGATIVE_INFINITY
  return value
}

export interface ProjectionSample {
  year: number
  age: number
  portfolio: number
  inflationAdjusted: number
  totalContributions: number
  contributions: number
}

/**
 * One projected retirement year of a deferred-compensation case.
 *
 * `surplus` is the value the app *displays*, rounded to whole dollars away from zero on both
 * platforms. The funded/shortfall fields alongside it are decided from the unrounded surplus, so a
 * case can legitimately pin `surplus: 0` next to `fundedYears: 3` — see the `deferred-sub-tolerance-gap`
 * case and issue #63.
 */
export interface DeferredAnnualSample {
  age: number
  totalIncome: number
  expenses: number
  surplus: number
  /**
   * Gross withdrawn beyond the per-account withdrawal-rate limits to keep the year funded — see
   * issue #56. Required on every sample, including the cases where it is `0`, so a case that omits
   * it fails to typecheck rather than silently skipping the assertion.
   */
  policyExcessWithdrawals: number
}

export interface FireCase {
  id: string
  kind: 'fire'
  description: string
  derivation: string
  inputs: {
    currentAge: number
    retirementAge: number
    currentSavings: number
    annualContribution: number
    annualIncome: number
    expectedReturn: number
    inflationRate: number
    withdrawalRate: number
    annualExpenses: number
    contributionGrowth: ContributionGrowth
  }
  expected: {
    fireNumber: number
    yearsToFire: FixtureNumber
    fireAge: FixtureNumber
    coastFireNumber: number
    savingsRate: number
    monthlyContribution: number
    projectionCount: number
    projectionSamples: ProjectionSample[]
    reverse: {
      requiredAnnualSavings: number
      requiredMonthlySavings: number
      currentWillGrowTo: number
      alreadyAchievable: boolean
    }
  }
}

export interface DebtCase {
  id: string
  kind: 'debt'
  description: string
  derivation: string
  inputs: {
    debts: DebtItem[]
    monthlyPayment: number
    extraPayment: number
    strategy: 'snowball' | 'avalanche'
  }
  expected: {
    totalMonths: number
    totalInterest: number
    totalPrincipal: number
    monthlyPayment: number
    firstMonthInterest: number
    payoffOrder: string[]
  }
}

export interface WithdrawalCase {
  id: string
  kind: 'withdrawal'
  description: string
  derivation: string
  inputs: {
    portfolioValue: number
    withdrawalRate: number
    expectedReturn: number
    inflationRate: number
    retirementYears: number
  }
  expected: {
    annualWithdrawal: number
    monthlyWithdrawal: number
    portfolioLongevity: number
    horizonFundedRatio: number
    endingBalance: number
    rateAnalysis: { rate: number; years: number; endBalance: number }[]
  }
}

export interface InvestmentCase {
  id: string
  kind: 'investment'
  description: string
  derivation: string
  inputs: {
    startingAmount: number
    contributionAmount: number
    contributionFrequency: 'monthly' | 'yearly'
    yearsInvesting: number
    expectedReturn: number
    inflationRate: number
    annualIncome: number
    currentAge: number
    contributionGrowth: ContributionGrowth
  }
  expected: {
    annualContribution: number
    monthlyContribution: number
    savingsRate: number
    finalNominalBalance: number
    finalInflationAdjustedBalance: number
    totalInvested: number
    totalGrowth: number
    inflationImpact: number
  }
}

export interface HealthcareCase {
  id: string
  kind: 'healthcare'
  description: string
  derivation: string
  inputs: {
    currentAge: number
    earlyRetirementAge: number
    monthlyPremium: number
    annualDeductible: number
    annualOutOfPocket: number
    inflationRate: number
  }
  expected: {
    gapYears: number
    annualCost: number
    totalCost: number
    avgAnnualCost: number
  }
}

export interface DeferredCase {
  id: string
  kind: 'deferred'
  description: string
  derivation: string
  inputs: {
    currentAge: number
    semiRetirementAge: number
    planThroughAge: number
    annualExpenses: number
    inflationRate: number
    withdrawOnlyAfterRetirement: boolean
    reinvestSurplus: boolean
    accounts: RetirementAccount[]
    incomeSources: RetirementIncomeSource[]
    additionalExpenses: RetirementExpense[]
  }
  expected: {
    projectionCount: number
    currentBalance: number
    balanceAtSemiRetirement: number
    endingBalance: number
    firstYearIncome: number
    firstYearSurplus: number
    retirementYears: number
    fundedYears: number
    yearsFullyCovered: number
    /** `null` means the plan never falls short, which is an answer rather than a missing value. */
    firstShortfallAge: number | null
    annualSamples: DeferredAnnualSample[]
  }
}

export type ParityCase =
  | FireCase
  | DebtCase
  | WithdrawalCase
  | InvestmentCase
  | HealthcareCase
  | DeferredCase

/**
 * The compile-time guard. If a case in the JSON gains a stray field, loses a required one, or uses
 * a value outside a union (a `contributionGrowth` of `"indexed"`, say), this line fails `tsc` and
 * therefore fails `npm run build`.
 */
const fixture = rawFixture as { cases: ParityCase[] }
export const parityCases = fixture.cases satisfies ParityCase[]

function casesOfKind<K extends ParityCase['kind']>(kind: K): Extract<ParityCase, { kind: K }>[] {
  return parityCases.filter(
    (testCase): testCase is Extract<ParityCase, { kind: K }> => testCase.kind === kind,
  )
}

export const fireCases = casesOfKind('fire')
export const debtCases = casesOfKind('debt')
export const withdrawalCases = casesOfKind('withdrawal')
export const investmentCases = casesOfKind('investment')
export const healthcareCases = casesOfKind('healthcare')
export const deferredCases = casesOfKind('deferred')

/** Maps a fixture case's named inputs onto the single-object shape the FIRE calculators take. */
export function toFireInputs(testCase: FireCase): FIREInputs {
  const { inputs } = testCase
  return {
    currentAge: inputs.currentAge,
    retirementAge: inputs.retirementAge,
    currentSavings: inputs.currentSavings,
    annualContribution: inputs.annualContribution,
    annualIncome: inputs.annualIncome,
    expectedReturn: inputs.expectedReturn,
    inflationRate: inputs.inflationRate,
    withdrawalRate: inputs.withdrawalRate,
    annualExpenses: inputs.annualExpenses,
    contributionGrowth: inputs.contributionGrowth,
  }
}

/**
 * Maps a fixture case's named inputs onto the deferred-compensation engine's call shape.
 *
 * `currentYear` is pinned rather than defaulted to `new Date().getFullYear()` so the case is
 * reproducible, but no expectation reads the calendar year: the two platforms derive it differently
 * (web from the system clock, MAUI from an explicit `ProjectionStartYear`), so it is covered
 * per-platform instead of pretending it is shared.
 */
export function toDeferredInputs(testCase: DeferredCase): DeferredCompensationInputs {
  const { inputs } = testCase
  return {
    currentAge: inputs.currentAge,
    semiRetirementAge: inputs.semiRetirementAge,
    planThroughAge: inputs.planThroughAge,
    annualExpenses: inputs.annualExpenses,
    inflationRate: inputs.inflationRate,
    accounts: inputs.accounts,
    incomeSources: inputs.incomeSources,
    additionalExpenses: inputs.additionalExpenses,
    withdrawOnlyAfterRetirement: inputs.withdrawOnlyAfterRetirement,
    reinvestSurplus: inputs.reinvestSurplus,
    currentYear: 2025,
  }
}
