import { describe, expect, it } from 'vitest'

import type { FIREInputs, ProjectionPoint } from '../calculations'
import {
  calculateBaristaFIRE,
  calculateCoastFIRE,
  calculateFatFIRE,
  calculateLeanFIRE,
  calculateStandardFIRE,
  presentValue,
  realReturn,
} from '../calculations'
import { realBalanceClosedForm, realRate, yearsToTargetClosedForm } from './oracles'

/**
 * FIRE variants: the crossing invariant, threshold classification, and monotonicity.
 *
 * The crossing invariant is issue #46's regression guard: the age at which the deflated projection
 * series crosses the FIRE target must equal the headline FIRE age. A calculator can be wrong in two
 * independent places, so agreement between them is a real constraint rather than a restatement.
 */

const DEFAULTS: FIREInputs = {
  currentAge: 30,
  retirementAge: 55,
  currentSavings: 100_000,
  annualContribution: 24_000,
  annualIncome: 72_000,
  expectedReturn: 0.07,
  inflationRate: 0.03,
  withdrawalRate: 0.04,
  annualExpenses: 48_000,
  contributionGrowth: 'inflation',
}

/**
 * Asserts that the projection series brackets `target` exactly at `headlineAge`: the last whole year
 * before it is short, and the first whole year at or after it is funded.
 */
function expectCrossingMatchesHeadline(
  projections: ProjectionPoint[],
  headlineAge: number,
  target: number,
) {
  const below = projections.find((p) => p.age === Math.floor(headlineAge))
  const above = projections.find((p) => p.age === Math.ceil(headlineAge))
  expect(below, `no projection at age ${Math.floor(headlineAge)}`).toBeDefined()
  expect(above, `no projection at age ${Math.ceil(headlineAge)}`).toBeDefined()
  expect(below!.inflationAdjusted).toBeLessThan(target)
  expect(above!.inflationAdjusted).toBeGreaterThanOrEqual(target)
}

const SCENARIOS: { label: string; inputs: FIREInputs }[] = [
  { label: 'shipped defaults', inputs: DEFAULTS },
  { label: 'flat contributions', inputs: { ...DEFAULTS, contributionGrowth: 'flat' } },
  { label: 'older saver', inputs: { ...DEFAULTS, currentAge: 42, retirementAge: 60, currentSavings: 250_000, annualContribution: 36_000, annualExpenses: 80_000 } },
  { label: 'lean spender', inputs: { ...DEFAULTS, annualExpenses: 32_000 } },
  { label: 'fat spender', inputs: { ...DEFAULTS, annualExpenses: 120_000, annualContribution: 60_000, annualIncome: 200_000 } },
  { label: 'zero inflation', inputs: { ...DEFAULTS, inflationRate: 0 } },
  { label: 'high inflation', inputs: { ...DEFAULTS, expectedReturn: 0.09, inflationRate: 0.06 } },
  { label: 'no starting savings', inputs: { ...DEFAULTS, currentSavings: 0 } },
]

describe('calculateStandardFIRE', () => {
  it.each(SCENARIOS.map((s) => [s.label, s.inputs] as const))(
    '%s: the projection crosses the target exactly at the headline FIRE age',
    (_label, inputs) => {
      const result = calculateStandardFIRE(inputs)
      expect(Number.isFinite(result.fireAge)).toBe(true)
      expectCrossingMatchesHeadline(result.projections, result.fireAge, result.fireNumber)
    },
  )

  it.each(SCENARIOS.map((s) => [s.label, s.inputs] as const))(
    '%s: fireNumber * withdrawalRate returns annual expenses',
    (_label, inputs) => {
      const result = calculateStandardFIRE(inputs)
      expect(result.fireNumber * inputs.withdrawalRate).toBeCloseTo(inputs.annualExpenses, 6)
    },
  )

  it.each(SCENARIOS.map((s) => [s.label, s.inputs] as const))(
    '%s: fireAge is currentAge plus yearsToFIRE',
    (_label, inputs) => {
      const result = calculateStandardFIRE(inputs)
      expect(result.fireAge).toBeCloseTo(inputs.currentAge + result.yearsToFIRE, 9)
    },
  )

  it('savings rate is contribution over income', () => {
    expect(calculateStandardFIRE(DEFAULTS).savingsRate).toBeCloseTo(24_000 / 72_000, 12)
  })

  it('savings rate is zero rather than NaN when income is zero', () => {
    // Guarding the divide-by-zero matters: NaN would propagate into the export and the chart.
    const result = calculateStandardFIRE({ ...DEFAULTS, annualIncome: 0 })
    expect(result.savingsRate).toBe(0)
    expect(Number.isNaN(result.savingsRate)).toBe(false)
  })

  it('coast number is the FIRE target discounted at the real rate to the target retirement age', () => {
    const result = calculateStandardFIRE(DEFAULTS)
    const expected = presentValue(1_200_000, realReturn(0.07, 0.03), 25)
    expect(result.coastFireNumber).toBe(Math.round(expected))
  })

  it('matches the closed-form year count with inflation-escalating contributions', () => {
    // n = ln((C + T*rho)/(C + PV*rho))/ln(1+rho) with rho = 1.07/1.03 - 1.
    const exact = yearsToTargetClosedForm(100_000, 24_000, realRate(0.07, 0.03), 1_200_000)
    expect(exact).not.toBeNull()
    expect(calculateStandardFIRE(DEFAULTS).yearsToFIRE).toBe(Math.round(exact! * 10) / 10)
  })

  it('reports the shipped-default anchor of 54.4', () => {
    // Derived, not recorded: rho = 0.038834951456..., n = 24.3838964605..., 30 + n rounds to 54.4.
    expect(calculateStandardFIRE(DEFAULTS).fireAge).toBe(54.4)
  })

  it('brackets the anchor crossing between ages 54 and 55', () => {
    // Closed form in today's dollars: PV(1+rho)^k + C((1+rho)^k - 1)/rho at k = 24 and k = 25.
    const result = calculateStandardFIRE(DEFAULTS)
    const at54 = result.projections.find((p) => p.age === 54)!
    const at55 = result.projections.find((p) => p.age === 55)!
    expect(at54.inflationAdjusted).toBe(Math.round(realBalanceClosedForm(100_000, 24_000, 0.07, 0.03, 24)))
    expect(at55.inflationAdjusted).toBe(Math.round(realBalanceClosedForm(100_000, 24_000, 0.07, 0.03, 25)))
    expect(at54.inflationAdjusted).toBeLessThan(1_200_000)
    expect(at55.inflationAdjusted).toBeGreaterThan(1_200_000)
  })

  it('returns 0 years when already funded', () => {
    const result = calculateStandardFIRE({ ...DEFAULTS, currentSavings: 1_500_000 })
    expect(result.yearsToFIRE).toBe(0)
    expect(result.fireAge).toBe(30)
  })

  it('reports Infinity when the target sits above the reachable ceiling', () => {
    // 2% return against 5% inflation converges to -C/rho = 700,000, below the 1.25M target.
    const result = calculateStandardFIRE({
      ...DEFAULTS,
      expectedReturn: 0.02,
      inflationRate: 0.05,
      annualContribution: 20_000,
      annualExpenses: 50_000,
      currentSavings: 100_000,
    })
    expect(result.fireAge).toBe(Infinity)
    expect(result.yearsToFIRE).toBe(Infinity)
    expect(result.retirementGoal.isOnTrack).toBe(false)
    expect(result.retirementGoal.message).toContain('not reachable')
  })

  it('caps the projection horizon at 50 years', () => {
    const result = calculateStandardFIRE({ ...DEFAULTS, annualContribution: 1_000, currentSavings: 1_000 })
    expect(result.projections.length).toBeLessThanOrEqual(51)
  })

  describe('monotonicity', () => {
    it('a larger contribution never delays FIRE', () => {
      const ages = [12_000, 18_000, 24_000, 36_000, 60_000].map(
        (c) => calculateStandardFIRE({ ...DEFAULTS, annualContribution: c }).fireAge,
      )
      for (let i = 1; i < ages.length; i++) expect(ages[i]).toBeLessThanOrEqual(ages[i - 1])
    })

    it('higher expenses never bring FIRE forward', () => {
      const ages = [30_000, 48_000, 72_000, 96_000].map(
        (e) => calculateStandardFIRE({ ...DEFAULTS, annualExpenses: e }).fireAge,
      )
      for (let i = 1; i < ages.length; i++) expect(ages[i]).toBeGreaterThanOrEqual(ages[i - 1])
    })

    it('a higher return never delays FIRE', () => {
      const ages = [0.04, 0.05, 0.07, 0.09].map(
        (r) => calculateStandardFIRE({ ...DEFAULTS, expectedReturn: r }).fireAge,
      )
      for (let i = 1; i < ages.length; i++) expect(ages[i]).toBeLessThanOrEqual(ages[i - 1])
    })

    it('higher inflation never brings FIRE forward', () => {
      const ages = [0, 0.02, 0.03, 0.05].map(
        (i) => calculateStandardFIRE({ ...DEFAULTS, inflationRate: i }).fireAge,
      )
      for (let i = 1; i < ages.length; i++) expect(ages[i]).toBeGreaterThanOrEqual(ages[i - 1])
    })

    it('more starting savings never delays FIRE', () => {
      const ages = [0, 50_000, 100_000, 400_000].map(
        (s) => calculateStandardFIRE({ ...DEFAULTS, currentSavings: s }).fireAge,
      )
      for (let i = 1; i < ages.length; i++) expect(ages[i]).toBeLessThanOrEqual(ages[i - 1])
    })
  })

  describe('retirement goal assessment', () => {
    it('is on track when FIRE lands at or before the target age', () => {
      // The shipped defaults reach FIRE at 54.4, which is before the target retirement age of 55.
      const result = calculateStandardFIRE(DEFAULTS)
      expect(result.fireAge).toBe(54.4)
      expect(result.retirementGoal.isOnTrack).toBe(true)
      expect(result.retirementGoal.targetAgeGap).toBeCloseTo(54.4 - 55, 9)
      expect(result.retirementGoal.message).toContain('On track')
    })

    it('is off track when FIRE lands after the target age', () => {
      // Halving the contribution pushes FIRE past 55.
      const result = calculateStandardFIRE({ ...DEFAULTS, annualContribution: 12_000 })
      expect(result.fireAge).toBeGreaterThan(55)
      expect(result.retirementGoal.isOnTrack).toBe(false)
      expect(result.retirementGoal.targetAgeGap).toBeCloseTo(result.fireAge - 55, 9)
      expect(result.retirementGoal.message).toContain('Off track')
    })

    it('falls back to the calculated age when no retirement age is supplied', () => {
      const { retirementAge: _omitted, ...withoutTarget } = DEFAULTS
      const result = calculateStandardFIRE(withoutTarget)
      expect(result.retirementGoal.targetRetirementAge).toBe(result.fireAge)
      expect(result.retirementGoal.targetAgeGap).toBe(0)
      expect(result.retirementGoal.isOnTrack).toBe(true)
    })
  })
})

describe('calculateLeanFIRE', () => {
  it('classifies at or below the $40k threshold as lean', () => {
    expect(calculateLeanFIRE({ ...DEFAULTS, annualExpenses: 39_999 }).isLean).toBe(true)
    expect(calculateLeanFIRE({ ...DEFAULTS, annualExpenses: 40_000 }).isLean).toBe(true)
    expect(calculateLeanFIRE({ ...DEFAULTS, annualExpenses: 40_001 }).isLean).toBe(false)
  })

  it('exposes the threshold it used', () => {
    expect(calculateLeanFIRE(DEFAULTS).leanThreshold).toBe(40_000)
  })

  it('leaves the standard numbers untouched', () => {
    const standard = calculateStandardFIRE(DEFAULTS)
    const lean = calculateLeanFIRE(DEFAULTS)
    expect(lean.fireNumber).toBe(standard.fireNumber)
    expect(lean.fireAge).toBe(standard.fireAge)
    expect(lean.coastFireNumber).toBe(standard.coastFireNumber)
  })

  it.each(SCENARIOS.map((s) => [s.label, s.inputs] as const))(
    '%s: crossing invariant still holds',
    (_label, inputs) => {
      const result = calculateLeanFIRE(inputs)
      expectCrossingMatchesHeadline(result.projections, result.fireAge, result.fireNumber)
    },
  )
})

describe('calculateFatFIRE', () => {
  it('classifies at or above the $100k threshold as fat', () => {
    expect(calculateFatFIRE({ ...DEFAULTS, annualExpenses: 99_999 }).isFat).toBe(false)
    expect(calculateFatFIRE({ ...DEFAULTS, annualExpenses: 100_000 }).isFat).toBe(true)
    expect(calculateFatFIRE({ ...DEFAULTS, annualExpenses: 100_001 }).isFat).toBe(true)
  })

  it('exposes the threshold it used', () => {
    expect(calculateFatFIRE(DEFAULTS).fatThreshold).toBe(100_000)
  })

  it('never classifies the same expenses as both lean and fat', () => {
    for (const expenses of [10_000, 40_000, 70_000, 100_000, 250_000]) {
      const lean = calculateLeanFIRE({ ...DEFAULTS, annualExpenses: expenses }).isLean
      const fat = calculateFatFIRE({ ...DEFAULTS, annualExpenses: expenses }).isFat
      expect(lean && fat).toBe(false)
    }
  })
})

describe('calculateCoastFIRE', () => {
  // Signature check, deliberately spelled out: (currentAge, targetRetirementAge, currentSavings,
  // annualContribution, expectedReturn, inflationRate, annualExpenses, withdrawalRate).
  // annualExpenses comes BEFORE withdrawalRate. The JSDoc example on this function has the two
  // reversed, which is a live trap for anyone writing a call from the docs.
  const coast = () => calculateCoastFIRE(30, 55, 100_000, 24_000, 0.07, 0.03, 48_000, 0.04)

  it('derives the FIRE number from expenses and withdrawal rate, not the reverse', () => {
    // If the two arguments were swapped this would be 0.04/48000, so the assertion doubles as a
    // guard against calling this function the way its own example does.
    expect(coast().fireNumber).toBe(1_200_000)
  })

  it('coast number is the FIRE target discounted at the real rate over the years remaining', () => {
    // Coast = FV / (1+rho)^n with rho = 1.07/1.03 - 1 and n = 25.
    const expected = 1_200_000 / Math.pow(1 + realRate(0.07, 0.03), 25)
    expect(coast().coastNumber).toBe(Math.round(expected))
  })

  it('a portfolio at the coast number grows to the FIRE number with no further contributions', () => {
    // The defining property of coasting. The reported coast number is rounded to whole dollars and
    // then compounds for 25 years at the real rate, so the half-dollar rounding error is amplified
    // by (1+rho)^25 ~= 2.59. The tolerance below is that bound, not a fudge factor.
    const result = coast()
    const growth = Math.pow(1 + realRate(0.07, 0.03), 25)
    const grown = result.coastNumber * growth
    expect(Math.abs(grown - 1_200_000)).toBeLessThanOrEqual(0.5 * growth)
  })

  it('flags already coasting when savings exceed the coast number', () => {
    const result = calculateCoastFIRE(30, 55, 900_000, 24_000, 0.07, 0.03, 48_000, 0.04)
    expect(result.alreadyCoasting).toBe(true)
    expect(result.yearsToCoast).toBe(0)
  })

  it('does not flag coasting when savings fall short', () => {
    const result = coast()
    expect(result.alreadyCoasting).toBe(false)
    expect(result.yearsToCoast).toBeGreaterThan(0)
  })

  it('the no-contribution projection crosses the coast number at the reported coast age', () => {
    const result = coast()
    expectCrossingMatchesHeadline(
      result.projectionsWithContributions,
      30 + result.yearsToCoast,
      result.coastNumber,
    )
  })

  it('the coast projection makes no contributions after the seed year', () => {
    const result = coast()
    result.projections.slice(1).forEach((point) => expect(point.contributions).toBe(0))
  })

  it('contributing always reaches at least as far as coasting', () => {
    const result = coast()
    result.projections.forEach((point, index) =>
      expect(point.portfolio).toBeLessThanOrEqual(result.projectionsWithContributions[index].portfolio),
    )
  })

  it('a later retirement age lowers the coast number', () => {
    // More compounding time means less is needed today. Strictly monotone for a positive real rate.
    const numbers = [45, 50, 55, 60, 65].map(
      (age) => calculateCoastFIRE(30, age, 100_000, 24_000, 0.07, 0.03, 48_000, 0.04).coastNumber,
    )
    for (let i = 1; i < numbers.length; i++) expect(numbers[i]).toBeLessThan(numbers[i - 1])
  })

  it('collapses to the full FIRE number when retirement is today', () => {
    // No time to compound, so present value equals future value.
    const result = calculateCoastFIRE(55, 55, 100_000, 24_000, 0.07, 0.03, 48_000, 0.04)
    expect(result.coastNumber).toBe(1_200_000)
  })
})

describe('calculateBaristaFIRE', () => {
  // Signature check: (currentAge, currentSavings, annualContribution, expectedReturn,
  // inflationRate, annualExpenses, withdrawalRate, partTimeAnnualIncome). There is NO
  // retirementAge parameter, unlike every other FIRE entry point.
  const barista = (partTime: number) =>
    calculateBaristaFIRE(30, 100_000, 24_000, 0.07, 0.03, 48_000, 0.04, partTime)

  it('reduces the required portfolio by the part-time income capitalised at the withdrawal rate', () => {
    // Portfolio only funds expenses the job does not: (48,000 - 20,000)/0.04 = 700,000.
    const result = barista(20_000)
    expect(result.fullFireNumber).toBe(1_200_000)
    expect(result.baristaNumber).toBe(700_000)
    expect(result.savingsFromPartTime).toBe(500_000)
  })

  it('savingsFromPartTime is partTimeIncome / withdrawalRate', () => {
    for (const income of [0, 10_000, 20_000, 40_000]) {
      expect(barista(income).savingsFromPartTime).toBe(Math.round(income / 0.04))
    }
  })

  it('equals standard FIRE when there is no part-time income', () => {
    const result = barista(0)
    expect(result.baristaNumber).toBe(result.fullFireNumber)
    expect(result.yearsToBaristaFIRE).toBe(calculateStandardFIRE(DEFAULTS).yearsToFIRE)
  })

  it('never demands a negative portfolio when part-time income exceeds expenses', () => {
    // The clamp matters: without it the required number would go negative and the year count
    // meaningless.
    const result = barista(60_000)
    expect(result.baristaNumber).toBe(0)
    expect(result.yearsToBaristaFIRE).toBe(0)
  })

  it('more part-time income never delays barista FIRE', () => {
    const ages = [0, 10_000, 20_000, 30_000].map((income) => barista(income).yearsToBaristaFIRE)
    for (let i = 1; i < ages.length; i++) expect(ages[i]).toBeLessThanOrEqual(ages[i - 1])
  })

  it('the projection crosses the barista number at the reported age', () => {
    const result = barista(20_000)
    expectCrossingMatchesHeadline(
      result.projections,
      30 + result.yearsToBaristaFIRE,
      result.baristaNumber,
    )
  })

  it('reaches barista FIRE no later than full FIRE', () => {
    expect(barista(20_000).yearsToBaristaFIRE).toBeLessThanOrEqual(
      calculateStandardFIRE(DEFAULTS).yearsToFIRE,
    )
  })
})
