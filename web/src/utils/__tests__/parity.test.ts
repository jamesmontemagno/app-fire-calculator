import { describe, expect, it } from 'vitest'

import {
  calculateAvalanchePayoff,
  calculateHealthcareGap,
  calculateInvestmentGrowth,
  calculateReverseFIRE,
  calculateSnowballPayoff,
  calculateStandardFIRE,
  calculateWithdrawal,
} from '../calculations'
import { calculateDeferredCompensation } from '../deferredCompensation'
import {
  debtCases,
  decode,
  deferredCases,
  fireCases,
  healthcareCases,
  investmentCases,
  toDeferredInputs,
  toFireInputs,
  withdrawalCases,
} from './parityFixtures'

/**
 * Shipped TypeScript === shared fixture.
 *
 * The same fixture is asserted against `FinancialCalculator.cs` by
 * `app/MyFireNumber.Tests/Calculations/SharedParityFixtureTests.cs`. Neither platform can move a
 * number without the other's suite failing, which is the drift issue #54 reports.
 *
 * `fixtureSelfCheck.test.ts` independently proves the fixture's own numbers are right, so a green
 * run here means "web agrees with algebra", not merely "web agrees with itself".
 */

describe('standard FIRE parity', () => {
  it.each(fireCases.map((c) => [c.id, c] as const))('%s', (_id, testCase) => {
    const result = calculateStandardFIRE(toFireInputs(testCase))
    const expected = testCase.expected

    expect(result.fireNumber).toBe(expected.fireNumber)
    expect(result.yearsToFIRE).toBe(decode(expected.yearsToFire))
    expect(result.fireAge).toBe(decode(expected.fireAge))
    expect(result.coastFireNumber).toBe(expected.coastFireNumber)
    expect(result.savingsRate).toBeCloseTo(expected.savingsRate, 12)
    expect(result.monthlyContribution).toBeCloseTo(expected.monthlyContribution, 12)
    expect(result.projections).toHaveLength(expected.projectionCount)

    for (const sample of expected.projectionSamples) {
      const point = result.projections.find((p) => p.age === sample.age)
      expect(point, `no projection point at age ${sample.age}`).toBeDefined()
      expect(point!.portfolio).toBe(sample.portfolio)
      expect(point!.inflationAdjusted).toBe(sample.inflationAdjusted)
      expect(point!.totalContributions).toBe(sample.totalContributions)
      expect(point!.contributions).toBe(sample.contributions)
    }
  })

  it.each(fireCases.map((c) => [c.id, c] as const))('%s reverse FIRE', (_id, testCase) => {
    const i = testCase.inputs
    const result = calculateReverseFIRE(
      i.currentAge,
      i.retirementAge,
      i.currentSavings,
      i.annualExpenses,
      i.expectedReturn,
      i.inflationRate,
      i.withdrawalRate,
      i.contributionGrowth,
    )
    const expected = testCase.expected.reverse
    expect(result.requiredAnnualSavings).toBeCloseTo(expected.requiredAnnualSavings, 6)
    expect(result.requiredMonthlySavings).toBeCloseTo(expected.requiredMonthlySavings, 6)
    expect(result.currentWillGrowTo).toBe(expected.currentWillGrowTo)
    expect(result.alreadyAchievable).toBe(expected.alreadyAchievable)
  })
})

describe('debt payoff parity', () => {
  it.each(debtCases.map((c) => [c.id, c] as const))('%s', (_id, testCase) => {
    const { debts, monthlyPayment, extraPayment, strategy } = testCase.inputs
    const result =
      strategy === 'snowball'
        ? calculateSnowballPayoff(debts, monthlyPayment, extraPayment)
        : calculateAvalanchePayoff(debts, monthlyPayment, extraPayment)

    expect(result.totalMonths).toBe(testCase.expected.totalMonths)
    expect(result.totalInterest).toBe(testCase.expected.totalInterest)
    expect(result.totalPrincipal).toBe(testCase.expected.totalPrincipal)
    expect(result.monthlyPayment).toBe(testCase.expected.monthlyPayment)
    expect(result.payoffOrder).toEqual(testCase.expected.payoffOrder)
    // Projection rows are rounded to whole dollars for display, so the exact accrual
    // (sum of balance * rate / 12) is compared through the same rounding. This still separates
    // correct from broken by a wide margin: the #45 defect double-charged interest, reporting
    // 333 rather than 167 for the single-debt case. `debtPayoff.test.ts` pins an unrounded
    // accrual exactly, using a balance whose monthly interest is a whole number.
    expect(result.projections[0].interestPaid).toBe(Math.round(testCase.expected.firstMonthInterest))
  })
})

describe('withdrawal parity', () => {
  it.each(withdrawalCases.map((c) => [c.id, c] as const))('%s', (_id, testCase) => {
    const i = testCase.inputs
    const result = calculateWithdrawal(
      i.portfolioValue,
      i.withdrawalRate,
      i.expectedReturn,
      i.inflationRate,
      i.retirementYears,
    )
    const expected = testCase.expected

    expect(result.annualWithdrawal).toBe(expected.annualWithdrawal)
    expect(result.monthlyWithdrawal).toBe(expected.monthlyWithdrawal)
    expect(result.portfolioLongevity).toBe(expected.portfolioLongevity)
    expect(result.horizonFundedRatio).toBeCloseTo(expected.horizonFundedRatio, 12)
    expect(result.endingBalance).toBe(expected.endingBalance)
    expect(result.rateAnalysis).toHaveLength(expected.rateAnalysis.length)
    result.rateAnalysis.forEach((row, index) => {
      expect(row.rate).toBeCloseTo(expected.rateAnalysis[index].rate, 12)
      expect(row.years).toBe(expected.rateAnalysis[index].years)
      expect(row.endBalance).toBe(expected.rateAnalysis[index].endBalance)
    })
  })
})

describe('investment growth parity', () => {
  it.each(investmentCases.map((c) => [c.id, c] as const))('%s', (_id, testCase) => {
    const i = testCase.inputs
    const result = calculateInvestmentGrowth(
      i.startingAmount,
      i.contributionAmount,
      i.contributionFrequency,
      i.yearsInvesting,
      i.expectedReturn,
      i.inflationRate,
      i.annualIncome,
      i.currentAge,
      i.contributionGrowth,
    )
    const expected = testCase.expected

    expect(result.annualContribution).toBeCloseTo(expected.annualContribution, 9)
    expect(result.monthlyContribution).toBeCloseTo(expected.monthlyContribution, 9)
    expect(result.savingsRate).toBeCloseTo(expected.savingsRate, 12)
    expect(result.finalNominalBalance).toBeCloseTo(expected.finalNominalBalance, 6)
    expect(result.finalInflationAdjustedBalance).toBeCloseTo(expected.finalInflationAdjustedBalance, 6)
    expect(result.totalInvested).toBeCloseTo(expected.totalInvested, 6)
    expect(result.totalGrowth).toBeCloseTo(expected.totalGrowth, 6)
    expect(result.inflationImpact).toBeCloseTo(expected.inflationImpact, 6)
  })
})

describe('healthcare gap parity', () => {
  it.each(healthcareCases.map((c) => [c.id, c] as const))('%s', (_id, testCase) => {
    const i = testCase.inputs
    const result = calculateHealthcareGap(
      i.currentAge,
      i.earlyRetirementAge,
      i.monthlyPremium,
      i.annualDeductible,
      i.annualOutOfPocket,
      i.inflationRate,
    )
    const expected = testCase.expected

    expect(result.gapYears).toBe(expected.gapYears)
    expect(result.annualCost).toBe(expected.annualCost)
    expect(result.totalCost).toBe(expected.totalCost)
    expect(result.avgAnnualCost).toBe(expected.avgAnnualCost)
  })
})

/**
 * Deferred-compensation cases exist because of issue #63, where the two platforms rounded a negative
 * `surplus` with different midpoint rules and then classified funded/shortfall from that rounded
 * value. The result was a categorical disagreement — web said "fully funded, never falls short",
 * MAUI said "fails at 60" — from identical inputs, and no shared case could catch it because none
 * produced a negative surplus.
 *
 * Each case asserts the surplus of every projected year, not just the headline, so the displayed
 * figure and the verdict derived from it are both pinned across platforms.
 */
describe('deferred compensation parity', () => {
  it.each(deferredCases.map((c) => [c.id, c] as const))('%s', (_id, testCase) => {
    const result = calculateDeferredCompensation(toDeferredInputs(testCase))
    const expected = testCase.expected

    expect(result.projections).toHaveLength(expected.projectionCount)
    expect(result.currentBalance).toBe(expected.currentBalance)
    expect(result.balanceAtSemiRetirement).toBe(expected.balanceAtSemiRetirement)
    expect(result.endingBalance).toBe(expected.endingBalance)
    expect(result.firstYearIncome).toBe(expected.firstYearIncome)
    expect(result.firstYearSurplus).toBe(expected.firstYearSurplus)
    expect(result.retirementYears).toBe(expected.retirementYears)
    expect(result.fundedYears).toBe(expected.fundedYears)
    expect(result.yearsFullyCovered).toBe(expected.yearsFullyCovered)
    expect(result.firstShortfallAge).toBe(expected.firstShortfallAge)

    for (const sample of expected.annualSamples) {
      const point = result.projections.find((p) => p.age === sample.age)
      expect(point).toBeDefined()
      expect(point!.totalIncome).toBe(sample.totalIncome)
      expect(point!.expenses).toBe(sample.expenses)
      expect(point!.surplus).toBe(sample.surplus)

      // `toBe` uses Object.is, so it already distinguishes -0 from 0 and the assertion above would
      // catch a regression. This states the intent outright, because negative zero silently
      // satisfying `>= 0` is the mechanism that made #63 severe.
      expect(Object.is(point!.surplus, -0)).toBe(false)
    }
  })
})
