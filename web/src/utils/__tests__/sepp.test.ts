import { describe, expect, it } from 'vitest'

import {
  addMonths,
  ageOn,
  calculateSepp,
  parseIsoDate,
  seppMaximumPermittedInterestRate,
  seppSingleLifeFactor,
  validateSeppInputs,
  type SeppInputs,
} from '../sepp'

/**
 * Mirrors app/MyFireNumber.Tests/Calculations/SeppCalculatorTests.cs so the web and MAUI
 * implementations agree on the IRS table, the commitment period and every method's payment.
 */
const INPUTS: SeppInputs = {
  accountBalance: 500_000,
  expectedReturn: 0.05,
  birthDate: '1976-08-22',
  firstPaymentDate: '2026-08-22',
  interestRate: 0.05,
  maximumInterestRate: 0.0522,
  annuityFactor: 16.2,
  method: 'amortization',
}

describe('SEPP single life table', () => {
  it.each([
    [18, 67.0],
    [50, 36.2],
    [59, 28.0],
  ])('age %i uses the post-2022 IRS factor %f', (age, expected) => {
    expect(seppSingleLifeFactor(age)).toBe(expected)
  })

  it('rejects ages outside the retained range', () => {
    expect(() => seppSingleLifeFactor(17)).toThrow(RangeError)
    expect(() => seppSingleLifeFactor(71)).toThrow(RangeError)
  })
})

describe('SEPP maximum interest rate', () => {
  it('uses the greater of 5% or 120% of the federal mid-term rate', () => {
    expect(seppMaximumPermittedInterestRate(0.03)).toBeCloseTo(0.05, 10)
    expect(seppMaximumPermittedInterestRate(0.0435)).toBeCloseTo(0.0522, 10)
  })
})

describe('calculateSepp', () => {
  it('compares all three methods', () => {
    const result = calculateSepp(INPUTS)

    expect(result.startingAge).toBe(50)
    expect(result.lifeExpectancyFactor).toBe(36.2)
    expect(result.requiredEndDate).toBe('2036-02-22')
    expect(result.requiredYears).toBe(10)
    expect(result.rmd.annualPayment).toBe(13_812)
    expect(result.amortization.annualPayment).toBe(30_156)
    expect(result.annuitization.annualPayment).toBe(30_864)
    expect(result.amortization.projections).toHaveLength(10)
    expect(result.amortization.monthlyPayment).toBeCloseTo(30_156 / 12, 10)
  })

  it('uses five years when the participant is already near 59½', () => {
    const result = calculateSepp({ ...INPUTS, birthDate: '1967-10-01' })

    expect(result.requiredEndDate).toBe('2031-08-22')
    expect(result.requiredYears).toBe(5)
    expect(result.rmd.projections.at(-1)?.yearNumber).toBe(5)
    expect(result.rmd.projections.at(-1)?.age).toBe(62)
  })

  it('rejects an interest rate above the user-supplied IRS limit', () => {
    expect(() => calculateSepp({ ...INPUTS, interestRate: 0.0523 })).toThrow(RangeError)
    expect(validateSeppInputs({ ...INPUTS, interestRate: 0.0523 })).toMatch(/cannot exceed/)
  })

  it('allows a missing annuity factor when another method is selected', () => {
    const result = calculateSepp({ ...INPUTS, annuityFactor: null })

    expect(result.annuitization.annualPayment).toBeNull()
    expect(result.annuitization.projections).toEqual([])
  })

  it('requires an annuity factor when annuitization is selected', () => {
    expect(() => calculateSepp({ ...INPUTS, annuityFactor: null, method: 'annuitization' })).toThrow(RangeError)
  })

  it('rejects a first payment at or after age 59½', () => {
    expect(() => calculateSepp({ ...INPUTS, birthDate: '1967-02-22' })).toThrow(/59½/)
  })

  it('rejects malformed dates instead of producing NaN ages', () => {
    expect(validateSeppInputs({ ...INPUTS, birthDate: '1976-13-01' })).toMatch(/birth date/)
    expect(validateSeppInputs({ ...INPUTS, firstPaymentDate: 'soon' })).toMatch(/first payment date/)
  })

  it('never pays out more than the remaining balance', () => {
    const result = calculateSepp({ ...INPUTS, accountBalance: 1_000, expectedReturn: -0.5 })
    for (const point of result.amortization.projections) {
      expect(point.annualPayment).toBeLessThanOrEqual(point.startingBalance)
      expect(point.endingBalance).toBeGreaterThanOrEqual(0)
    }
  })
})

describe('SEPP date helpers', () => {
  it('clamps the day when a month is shorter, matching DateOnly.AddMonths', () => {
    expect(addMonths({ year: 2024, month: 1, day: 31 }, 1)).toEqual({ year: 2024, month: 2, day: 29 })
    expect(addMonths({ year: 2023, month: 8, day: 31 }, 6)).toEqual({ year: 2024, month: 2, day: 29 })
  })

  it('treats a February 29 birthday as February 28 in a common year', () => {
    const birth = parseIsoDate('1976-02-29')!
    expect(ageOn(birth, parseIsoDate('2026-02-28')!)).toBe(50)
    expect(ageOn(birth, parseIsoDate('2026-02-27')!)).toBe(49)
  })

  it('rejects impossible calendar dates', () => {
    expect(parseIsoDate('2026-02-30')).toBeNull()
    expect(parseIsoDate('2026-00-10')).toBeNull()
    expect(parseIsoDate('2024-02-29')).toEqual({ year: 2024, month: 2, day: 29 })
  })
})
