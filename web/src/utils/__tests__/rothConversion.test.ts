import { describe, expect, it } from 'vitest'

import {
  calculateRothConversion,
  validateRothConversionInputs,
  type RothConversionInputs,
} from '../rothConversion'

/**
 * Mirrors app/MyFireNumber.Tests/Calculations/RothConversionCalculatorTests.cs so the web and
 * MAUI implementations agree on the five-tax-year ladder and the growth ordering.
 */
const INPUTS: RothConversionInputs = {
  currentAge: 45,
  startYear: 2026,
  traditionalBalance: 500_000,
  rothBalance: 50_000,
  annualConversion: 40_000,
  conversionYears: 3,
  expectedReturn: 0,
  estimatedTaxRate: 0.2,
}

describe('calculateRothConversion', () => {
  it('builds the five-tax-year conversion ladder', () => {
    const result = calculateRothConversion(INPUTS)

    expect(result.totalConverted).toBe(120_000)
    expect(result.totalEstimatedTaxes).toBe(24_000)
    expect(result.firstAccessibleYear).toBe(2031)
    expect(result.endingTraditionalBalance).toBe(380_000)
    expect(result.endingRothBalance).toBe(170_000)
    expect(result.projections).toHaveLength(8)
    expect(result.projections[5].newlyAccessiblePrincipal).toBe(40_000)
    expect(result.projections.at(-1)?.cumulativeAccessiblePrincipal).toBe(120_000)
  })

  it('limits each conversion to the remaining traditional balance', () => {
    const result = calculateRothConversion({ ...INPUTS, traditionalBalance: 60_000, annualConversion: 40_000 })

    expect(result.totalConverted).toBe(60_000)
    expect(result.endingTraditionalBalance).toBe(0)
    expect(result.projections.filter(point => point.conversion > 0)).toHaveLength(2)
  })

  it('releases converted principal at age 60 without waiting five years', () => {
    const result = calculateRothConversion({ ...INPUTS, currentAge: 58 })

    expect(result.firstAccessibleYear).toBe(2028)
    expect(result.projections[2].newlyAccessiblePrincipal).toBe(120_000)
    expect(result.projections[2].cumulativeAccessiblePrincipal).toBe(120_000)
  })

  it('grows balances before each year’s conversion', () => {
    const result = calculateRothConversion({
      ...INPUTS,
      traditionalBalance: 100_000,
      rothBalance: 0,
      annualConversion: 10_000,
      conversionYears: 1,
      expectedReturn: 0.1,
    })

    expect(result.projections[0].endingTraditionalBalance).toBe(100_000)
    expect(result.projections[0].endingRothBalance).toBe(10_000)
    expect(result.endingTraditionalBalance).toBe(161_051)
    expect(result.endingRothBalance).toBe(16_105)
  })

  it('reports no accessible year when nothing was converted', () => {
    const result = calculateRothConversion({ ...INPUTS, traditionalBalance: 0 })
    expect(result.totalConverted).toBe(0)
    expect(result.firstAccessibleYear).toBeNull()
  })

  it('rejects invalid rates and durations', () => {
    expect(() => calculateRothConversion({ ...INPUTS, estimatedTaxRate: 1.01 })).toThrow(RangeError)
    expect(() => calculateRothConversion({ ...INPUTS, conversionYears: 0 })).toThrow(RangeError)
    expect(() => calculateRothConversion({ ...INPUTS, annualConversion: 0 })).toThrow(RangeError)
    expect(validateRothConversionInputs(INPUTS)).toBeNull()
  })
})
