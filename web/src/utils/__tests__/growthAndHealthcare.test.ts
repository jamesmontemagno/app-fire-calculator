import { describe, expect, it } from 'vitest'

import { MEDICARE_AGE, calculateHealthcareGap, calculateInvestmentGrowth } from '../calculations'
import { deflate, inflatingSum, nominalEscalatingSeries, nominalFlatSeries } from './oracles'

/**
 * Savings & Investment growth, and the healthcare gap.
 *
 * Both have exact closed forms, so nothing here needs a recorded value:
 *  - growth is the same escalating-contribution recurrence the projections use;
 *  - the healthcare gap is a plain geometric series, A((1+i)^n - 1)/i.
 */

describe('calculateInvestmentGrowth', () => {
  // Signature: (startingAmount, contributionAmount, contributionFrequency, yearsInvesting,
  // expectedReturn, inflationRate, annualIncome, currentAge, contributionGrowth?)
  const defaults = () =>
    calculateInvestmentGrowth(100_000, 500, 'monthly', 30, 0.07, 0.03, 72_000, 30, 'inflation')

  it('annualises a monthly contribution by twelve', () => {
    const result = defaults()
    expect(result.annualContribution).toBe(6_000)
    expect(result.monthlyContribution).toBe(500)
  })

  it('accepts a yearly contribution unchanged', () => {
    const result = calculateInvestmentGrowth(100_000, 6_000, 'yearly', 30, 0.07, 0.03, 72_000, 30)
    expect(result.annualContribution).toBe(6_000)
    expect(result.monthlyContribution).toBe(500)
  })

  it('treats monthly and yearly contributions of equal annual size identically', () => {
    // The model compounds annually, so $500/mo and $6,000/yr must produce the same series.
    const monthly = calculateInvestmentGrowth(100_000, 500, 'monthly', 30, 0.07, 0.03, 72_000, 30)
    const yearly = calculateInvestmentGrowth(100_000, 6_000, 'yearly', 30, 0.07, 0.03, 72_000, 30)
    expect(yearly.finalNominalBalance).toBe(monthly.finalNominalBalance)
  })

  it('reproduces the Savings & Investment anchor', () => {
    // PV = 100,000, C = 6,000, r = 7%, i = 3%, n = 30 under the escalating-contribution recurrence
    // B_k = B_{k-1}(1.07) + 6000(1.03)^k, then deflated by 1.03^30.
    const result = defaults()
    expect(result.finalNominalBalance).toBeCloseTo(1_562_306.8565586861, 4)
    expect(result.finalInflationAdjustedBalance).toBeCloseTo(643_649.7392030951, 4)
  })

  it.each([
    ['inflation', 'inflation'],
    ['flat', 'flat'],
  ] as const)('%s growth follows an independently simulated series', (_label, growth) => {
    const result = calculateInvestmentGrowth(100_000, 500, 'monthly', 30, 0.07, 0.03, 72_000, 30, growth)
    const series =
      growth === 'flat'
        ? nominalFlatSeries(100_000, 6_000, 0.07, 30)
        : nominalEscalatingSeries(100_000, 6_000, 0.07, 0.03, 30)
    expect(result.finalNominalBalance).toBeCloseTo(series[30], 6)
    result.projections.forEach((point, k) => expect(point.portfolio).toBe(Math.round(series[k])))
  })

  it('deflates the final balance by exactly the horizon', () => {
    const result = defaults()
    expect(result.finalInflationAdjustedBalance).toBeCloseTo(
      deflate(result.finalNominalBalance, 0.03, 30),
      6,
    )
  })

  it('satisfies growth = final - invested', () => {
    for (const years of [1, 10, 30, 45]) {
      const result = calculateInvestmentGrowth(100_000, 500, 'monthly', years, 0.07, 0.03, 72_000, 30)
      expect(result.totalGrowth).toBeCloseTo(result.finalNominalBalance - result.totalInvested, 6)
    }
  })

  it('satisfies inflationImpact = nominal - real', () => {
    const result = defaults()
    expect(result.inflationImpact).toBeCloseTo(
      result.finalNominalBalance - result.finalInflationAdjustedBalance,
      6,
    )
  })

  it('counts the starting amount as invested capital, not growth', () => {
    const result = calculateInvestmentGrowth(100_000, 0, 'yearly', 0, 0.07, 0.03, 72_000, 30)
    expect(result.totalInvested).toBe(100_000)
    expect(result.totalGrowth).toBe(0)
    expect(result.finalNominalBalance).toBe(100_000)
  })

  it('sums every escalating contribution into totalInvested', () => {
    // 100,000 seed plus sum_{k=1..30} 6000(1.03)^k.
    const expected = 100_000 + inflatingSum(6_000 * 1.03, 0.03, 30)
    expect(defaults().totalInvested).toBeCloseTo(expected, 6)
  })

  it('reports savings rate as contribution over income, and zero when income is zero', () => {
    expect(defaults().savingsRate).toBeCloseTo(6_000 / 72_000, 12)
    const noIncome = calculateInvestmentGrowth(100_000, 500, 'monthly', 30, 0.07, 0.03, 0, 30)
    expect(noIncome.savingsRate).toBe(0)
  })

  it('emits one projection point per year inclusive, with ages advancing by one', () => {
    const result = defaults()
    expect(result.projections).toHaveLength(31)
    result.projections.forEach((point, index) => expect(point.age).toBe(30 + index))
  })

  it('makes no contribution in the seed year', () => {
    expect(defaults().projections[0].contributions).toBe(0)
  })

  it('inflation-matched contributions always end ahead of flat ones in real terms', () => {
    const inflation = calculateInvestmentGrowth(100_000, 500, 'monthly', 30, 0.07, 0.03, 72_000, 30, 'inflation')
    const flat = calculateInvestmentGrowth(100_000, 500, 'monthly', 30, 0.07, 0.03, 72_000, 30, 'flat')
    expect(flat.finalInflationAdjustedBalance).toBeLessThan(inflation.finalInflationAdjustedBalance)
  })

  it('a longer horizon never ends with less, at non-negative returns', () => {
    const balances = [5, 10, 20, 30, 40].map(
      (years) => calculateInvestmentGrowth(100_000, 500, 'monthly', years, 0.07, 0.03, 72_000, 30).finalNominalBalance,
    )
    for (let i = 1; i < balances.length; i++) expect(balances[i]).toBeGreaterThan(balances[i - 1])
  })

  it('is unaffected by inflation in nominal terms under flat contributions', () => {
    // A flat nominal contribution never sees the inflation rate, so the nominal path cannot move.
    const a = calculateInvestmentGrowth(100_000, 500, 'monthly', 30, 0.07, 0.02, 72_000, 30, 'flat')
    const b = calculateInvestmentGrowth(100_000, 500, 'monthly', 30, 0.07, 0.05, 72_000, 30, 'flat')
    expect(a.finalNominalBalance).toBe(b.finalNominalBalance)
    expect(a.finalInflationAdjustedBalance).toBeGreaterThan(b.finalInflationAdjustedBalance)
  })
})

describe('calculateHealthcareGap', () => {
  // Signature: (currentAge, earlyRetirementAge, monthlyPremium, annualDeductible,
  // annualOutOfPocket, inflationRate)
  const defaults = () => calculateHealthcareGap(30, 55, 600, 2_500, 2_000, 0.03)

  it('runs from early retirement to Medicare eligibility', () => {
    expect(MEDICARE_AGE).toBe(65)
    expect(defaults().gapYears).toBe(10)
    expect(calculateHealthcareGap(30, 45, 600, 2_500, 2_000, 0.03).gapYears).toBe(20)
  })

  it('never reports a negative gap for someone retiring at or after 65', () => {
    for (const age of [65, 67, 80]) {
      const result = calculateHealthcareGap(30, age, 600, 2_500, 2_000, 0.03)
      expect(result.gapYears).toBe(0)
      expect(result.totalCost).toBe(0)
      expect(result.avgAnnualCost).toBe(0)
      expect(result.yearlyBreakdown).toEqual([])
    }
  })

  it('sums premium, deductible and out-of-pocket into the annual cost', () => {
    // 600*12 + 2500 + 2000 = 11,700.
    expect(defaults().annualCost).toBe(11_700)
  })

  it('totals the geometric series A((1+i)^n - 1)/i', () => {
    expect(defaults().totalCost).toBe(Math.round(inflatingSum(11_700, 0.03, 10)))
  })

  it.each([
    [55, 0.03],
    [50, 0.05],
    [45, 0],
    [60, 0.02],
  ] as const)('total matches the closed form for retirement at %s with inflation %s', (age, inflation) => {
    const result = calculateHealthcareGap(30, age, 600, 2_500, 2_000, inflation)
    expect(result.totalCost).toBe(Math.round(inflatingSum(11_700, inflation, 65 - age)))
  })

  it('degenerates to A*n when inflation is zero', () => {
    // The geometric sum divides by i, so i = 0 must be handled as simple multiplication.
    const result = calculateHealthcareGap(30, 55, 600, 2_500, 2_000, 0)
    expect(result.totalCost).toBe(117_000)
    expect(result.avgAnnualCost).toBe(11_700)
  })

  it('inflates the first year by nothing and each later year by one more step', () => {
    const result = defaults()
    result.yearlyBreakdown.forEach((year, index) => {
      expect(year.cost).toBe(Math.round(11_700 * Math.pow(1.03, index)))
      expect(year.premium).toBe(Math.round(7_200 * Math.pow(1.03, index)))
      expect(year.deductible).toBe(Math.round(2_500 * Math.pow(1.03, index)))
      expect(year.outOfPocket).toBe(Math.round(2_000 * Math.pow(1.03, index)))
    })
    expect(result.yearlyBreakdown[0].cost).toBe(11_700)
  })

  it('breaks the cost down into components that re-add to the yearly total', () => {
    // Each part is rounded independently, so allow the one-dollar slack that creates.
    defaults().yearlyBreakdown.forEach((year) => {
      const parts = year.premium + year.deductible + year.outOfPocket
      expect(Math.abs(parts - year.cost)).toBeLessThanOrEqual(1)
    })
  })

  it('runs the breakdown from the retirement age for exactly gapYears entries', () => {
    const result = defaults()
    expect(result.yearlyBreakdown).toHaveLength(10)
    result.yearlyBreakdown.forEach((year, index) => expect(year.age).toBe(55 + index))
    expect(result.yearlyBreakdown[result.yearlyBreakdown.length - 1].age).toBe(64)
  })

  it('averages the total across the gap', () => {
    const result = defaults()
    expect(result.avgAnnualCost).toBeGreaterThan(result.annualCost)
    expect(Math.abs(result.avgAnnualCost - result.totalCost / result.gapYears)).toBeLessThanOrEqual(1)
  })

  it('retiring earlier never costs less', () => {
    const totals = [62, 60, 55, 50, 45].map(
      (age) => calculateHealthcareGap(30, age, 600, 2_500, 2_000, 0.03).totalCost,
    )
    for (let i = 1; i < totals.length; i++) expect(totals[i]).toBeGreaterThan(totals[i - 1])
  })

  it('scales linearly with the annual cost', () => {
    // The series is homogeneous in A, so doubling every component doubles the total.
    const single = calculateHealthcareGap(30, 55, 600, 2_500, 2_000, 0.03).totalCost
    const double = calculateHealthcareGap(30, 55, 1_200, 5_000, 4_000, 0.03).totalCost
    expect(Math.abs(double - 2 * single)).toBeLessThanOrEqual(1)
  })
})
