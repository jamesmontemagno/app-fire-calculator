import { describe, expect, it } from 'vitest'
import { calculateInterest } from '../calculations'

describe('calculateInterest', () => {
  it('compounds monthly and adds contributions at month end', () => {
    const result = calculateInterest(10_000, 250, 0.05, 10)

    expect(result.endingBalance).toBeCloseTo(55_291, 0)
    expect(result.totalContributions).toBe(40_000)
    expect(result.interestEarned).toBeCloseTo(15_291, 0)
    expect(result.effectiveAnnualYield).toBeCloseTo(0.051162, 6)
    expect(result.projections).toHaveLength(11)
  })

  it('returns contributions only when the rate is zero', () => {
    const result = calculateInterest(1_000, 100, 0, 2)

    expect(result.endingBalance).toBe(3_400)
    expect(result.interestEarned).toBe(0)
    expect(result.effectiveAnnualYield).toBe(0)
  })
})
