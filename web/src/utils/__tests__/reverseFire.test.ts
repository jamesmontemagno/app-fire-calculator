import { describe, expect, it } from 'vitest'

import { calculateReverseFIRE, calculateStandardFIRE, realReturn } from '../calculations'
import { realBalanceClosedForm, realRate } from './oracles'

/**
 * Reverse FIRE: "what must I save to retire at age X?"
 *
 * The strongest available oracle is the round trip. Feeding the required savings this function
 * reports back into `calculateStandardFIRE` must return the target retirement age. The two
 * functions solve the same equation for different unknowns, so agreement is a genuine constraint
 * rather than a restatement of one implementation.
 */

describe('calculateReverseFIRE', () => {
  // Signature: (currentAge, targetRetirementAge, currentSavings, annualExpenses, expectedReturn,
  // inflationRate, withdrawalRate, contributionGrowth?)
  const reverse = (overrides: Partial<{
    currentAge: number
    targetRetirementAge: number
    currentSavings: number
    annualExpenses: number
    expectedReturn: number
    inflationRate: number
    withdrawalRate: number
    contributionGrowth: 'inflation' | 'flat'
  }> = {}) => {
    const p = {
      currentAge: 30,
      targetRetirementAge: 55,
      currentSavings: 100_000,
      annualExpenses: 48_000,
      expectedReturn: 0.07,
      inflationRate: 0.03,
      withdrawalRate: 0.04,
      contributionGrowth: 'inflation' as const,
      ...overrides,
    }
    return calculateReverseFIRE(
      p.currentAge,
      p.targetRetirementAge,
      p.currentSavings,
      p.annualExpenses,
      p.expectedReturn,
      p.inflationRate,
      p.withdrawalRate,
      p.contributionGrowth,
    )
  }

  it('targets expenses divided by the withdrawal rate', () => {
    expect(reverse().fireNumber).toBe(1_200_000)
  })

  it('lands exactly on the target in today\u2019s dollars', () => {
    // The defining property: saving the reported amount for the reported horizon reaches the FIRE
    // number precisely. Checked against an independently written closed form, not the projections.
    const result = reverse()
    const landed = realBalanceClosedForm(100_000, result.requiredAnnualSavings, 0.07, 0.03, 25)
    expect(landed).toBeCloseTo(1_200_000, 6)
  })

  it('matches the annuity payment formula C = (T - PV(1+rho)^n) * rho / ((1+rho)^n - 1)', () => {
    // Independent algebra: solve T = PV(1+rho)^n + C((1+rho)^n - 1)/rho for C.
    const rho = realRate(0.07, 0.03)
    const growth = Math.pow(1 + rho, 25)
    const expected = ((1_200_000 - 100_000 * growth) * rho) / (growth - 1)
    expect(reverse().requiredAnnualSavings).toBeCloseTo(expected, 6)
    expect(expected).toBeCloseTo(22_946.80026660227, 6)
  })

  it('monthly savings is the annual figure over twelve', () => {
    const result = reverse()
    expect(result.requiredMonthlySavings).toBeCloseTo(result.requiredAnnualSavings / 12, 12)
  })

  it('round trips through calculateStandardFIRE back to the target age', () => {
    // The key cross-check between the two solvers.
    for (const targetRetirementAge of [45, 50, 55, 60, 65]) {
      const result = reverse({ targetRetirementAge })
      const forward = calculateStandardFIRE({
        currentAge: 30,
        retirementAge: targetRetirementAge,
        currentSavings: 100_000,
        annualContribution: result.requiredAnnualSavings,
        annualIncome: 72_000,
        expectedReturn: 0.07,
        inflationRate: 0.03,
        withdrawalRate: 0.04,
        annualExpenses: 48_000,
        contributionGrowth: 'inflation',
      })
      expect(forward.fireAge).toBeCloseTo(targetRetirementAge, 1)
    }
  })

  it('round trips for flat nominal contributions too', () => {
    for (const targetRetirementAge of [45, 55, 65]) {
      const result = reverse({ targetRetirementAge, contributionGrowth: 'flat' })
      const forward = calculateStandardFIRE({
        currentAge: 30,
        retirementAge: targetRetirementAge,
        currentSavings: 100_000,
        annualContribution: result.requiredAnnualSavings,
        annualIncome: 72_000,
        expectedReturn: 0.07,
        inflationRate: 0.03,
        withdrawalRate: 0.04,
        annualExpenses: 48_000,
        contributionGrowth: 'flat',
      })
      expect(forward.fireAge).toBeCloseTo(targetRetirementAge, 1)
    }
  })

  it('reports what existing savings alone will grow to, in today\u2019s dollars', () => {
    // PV(1+rho)^n with no contributions at all.
    const expected = 100_000 * Math.pow(1 + realReturn(0.07, 0.03), 25)
    expect(reverse().currentWillGrowTo).toBe(Math.round(expected))
  })

  it('requires nothing further when existing savings already reach the target', () => {
    const result = reverse({ currentSavings: 600_000 })
    expect(result.alreadyAchievable).toBe(true)
    expect(result.requiredAnnualSavings).toBe(0)
    expect(result.requiredMonthlySavings).toBe(0)
  })

  it('never reports a negative required contribution', () => {
    for (const savings of [0, 100_000, 500_000, 900_000, 5_000_000]) {
      expect(reverse({ currentSavings: savings }).requiredAnnualSavings).toBeGreaterThanOrEqual(0)
    }
  })

  it('a longer horizon never requires saving more', () => {
    const amounts = [40, 45, 50, 55, 60, 65].map(
      (age) => reverse({ targetRetirementAge: age }).requiredAnnualSavings,
    )
    for (let i = 1; i < amounts.length; i++) expect(amounts[i]).toBeLessThanOrEqual(amounts[i - 1])
  })

  it('higher expenses never require saving less', () => {
    const amounts = [24_000, 48_000, 72_000, 96_000].map(
      (expenses) => reverse({ annualExpenses: expenses }).requiredAnnualSavings,
    )
    for (let i = 1; i < amounts.length; i++) expect(amounts[i]).toBeGreaterThanOrEqual(amounts[i - 1])
  })

  it('flat nominal contributions always demand more than inflation-matched ones', () => {
    // A fixed nominal payment loses purchasing power, so more of it is needed to hit the same
    // today's-dollars target.
    const inflation = reverse({ contributionGrowth: 'inflation' }).requiredAnnualSavings
    const flat = reverse({ contributionGrowth: 'flat' }).requiredAnnualSavings
    expect(flat).toBeGreaterThan(inflation)
  })

  it('solves the zero-real-return case linearly', () => {
    // rho = 0 makes the annuity factor 0/0; the limit is the straight-line split of the shortfall.
    // (1,200,000 - 100,000)/25 = 44,000 exactly.
    const result = reverse({ expectedReturn: 0.05, inflationRate: 0.05 })
    expect(realReturn(0.05, 0.05)).toBe(0)
    expect(result.requiredAnnualSavings).toBeCloseTo(44_000, 6)
  })

  it('treats a horizon of zero or less as a single year rather than dividing by zero', () => {
    for (const targetRetirementAge of [30, 25]) {
      const result = reverse({ targetRetirementAge })
      expect(result.yearsToFIRE).toBe(1)
      expect(Number.isFinite(result.requiredAnnualSavings)).toBe(true)
    }
  })
})
