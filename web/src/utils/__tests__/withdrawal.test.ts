import { describe, expect, it } from 'vitest'

import { calculateWithdrawal } from '../calculations'
import { drawdownSeries } from './oracles'

/**
 * Withdrawal / portfolio longevity.
 *
 * The oracle is an independently written drawdown recurrence:
 *
 *   B_0 = P,   B_k = B_{k-1}(1+r) - W_0(1+i)^{k-1}
 *
 * The withdrawal taken during year k is the one set at the start of that year, so the inflation
 * exponent is k-1. Getting that off by one is exactly the class of error the audit was chasing.
 */

const RATE_ANALYSIS_RATES = [0.03, 0.035, 0.04, 0.045, 0.05]

describe('calculateWithdrawal', () => {
  // Signature: (portfolioValue, withdrawalRate, expectedReturn, inflationRate, retirementYears)
  it('derives the withdrawal from portfolio and rate', () => {
    const result = calculateWithdrawal(1_000_000, 0.04, 0.07, 0.03, 30)
    expect(result.annualWithdrawal).toBe(40_000)
    expect(result.monthlyWithdrawal).toBe(Math.round(40_000 / 12))
  })

  it.each([
    [1_000_000, 0.04, 0.07, 0.03, 30],
    [1_000_000, 0.05, 0.03, 0.03, 40],
    [500_000, 0.05, 0.02, 0.04, 45],
    [1_000_000, 0.04, 0.04, 0.05, 45],
    [2_500_000, 0.03, 0.06, 0.02, 35],
  ] as const)(
    'balances follow B_k = B_{k-1}(1+r) - W(1+i)^{k-1} for (%s, %s, %s, %s, %s)',
    (portfolio, rate, ret, inflation, years) => {
      const result = calculateWithdrawal(portfolio, rate, ret, inflation, years)
      const oracle = drawdownSeries(portfolio, portfolio * rate, ret, inflation, years + 1)
      result.withdrawalProjections.forEach((point) => {
        expect(point.balance).toBe(Math.round(oracle[point.year]))
      })
    },
  )

  it('inflates each successive withdrawal by exactly one year', () => {
    const result = calculateWithdrawal(1_000_000, 0.04, 0.07, 0.03, 30)
    result.withdrawalProjections.forEach((point) => {
      expect(point.withdrawal).toBe(Math.round(40_000 * Math.pow(1.03, point.year)))
    })
  })

  it('numbers projection years consecutively from zero', () => {
    const result = calculateWithdrawal(1_000_000, 0.04, 0.07, 0.03, 30)
    result.withdrawalProjections.forEach((point, index) => expect(point.year).toBe(index))
  })

  it('every reported funded year really did end with a positive balance', () => {
    // The definition of portfolioLongevity, checked against the independent series rather than
    // against the loop that produced it.
    for (const [portfolio, rate, ret, inflation, years] of [
      [1_000_000, 0.05, 0.03, 0.03, 40],
      [500_000, 0.05, 0.02, 0.04, 45],
      [1_000_000, 0.04, 0.04, 0.05, 45],
    ] as const) {
      const result = calculateWithdrawal(portfolio, rate, ret, inflation, years)
      const oracle = drawdownSeries(portfolio, portfolio * rate, ret, inflation, years + 1)
      expect(oracle[result.portfolioLongevity]).toBeGreaterThan(0)
      if (result.portfolioLongevity < years) {
        expect(oracle[result.portfolioLongevity + 1]).toBeLessThanOrEqual(0)
      }
    }
  })

  it('caps longevity at the requested horizon when the portfolio survives', () => {
    const result = calculateWithdrawal(1_000_000, 0.04, 0.07, 0.03, 30)
    expect(result.portfolioLongevity).toBe(30)
    expect(result.horizonFundedRatio).toBe(1)
  })

  it('reports a fractional funded ratio when the portfolio runs out early', () => {
    const result = calculateWithdrawal(1_000_000, 0.05, 0.03, 0.03, 40)
    expect(result.portfolioLongevity).toBeLessThan(40)
    expect(result.horizonFundedRatio).toBeCloseTo(result.portfolioLongevity / 40, 12)
    expect(result.horizonFundedRatio).toBeLessThan(1)
  })

  it('never reports a funded ratio above 1 or below 0', () => {
    for (const rate of [0.02, 0.03, 0.04, 0.06, 0.10]) {
      const { horizonFundedRatio } = calculateWithdrawal(1_000_000, rate, 0.05, 0.03, 30)
      expect(horizonFundedRatio).toBeGreaterThanOrEqual(0)
      expect(horizonFundedRatio).toBeLessThanOrEqual(1)
    }
  })

  it('treats a zero-length horizon as fully funded rather than dividing by zero', () => {
    const result = calculateWithdrawal(1_000_000, 0.04, 0.07, 0.03, 0)
    expect(result.horizonFundedRatio).toBe(1)
    expect(Number.isNaN(result.horizonFundedRatio)).toBe(false)
  })

  it('never reports a negative ending balance', () => {
    for (const rate of [0.03, 0.05, 0.08, 0.15]) {
      expect(calculateWithdrawal(500_000, rate, 0.02, 0.04, 45).endingBalance).toBeGreaterThanOrEqual(0)
    }
  })

  describe('rate analysis table', () => {
    it('covers the five documented rates in ascending order', () => {
      const rows = calculateWithdrawal(1_000_000, 0.04, 0.07, 0.03, 30).rateAnalysis
      expect(rows.map((r) => r.rate)).toEqual(RATE_ANALYSIS_RATES)
    })

    it('years are non-increasing as the withdrawal rate rises', () => {
      // Spending more can never make a portfolio last longer.
      for (const [ret, inflation] of [
        [0.07, 0.03],
        [0.03, 0.03],
        [0.02, 0.04],
      ] as const) {
        const rows = calculateWithdrawal(1_000_000, 0.04, ret, inflation, 30).rateAnalysis
        for (let i = 1; i < rows.length; i++) {
          expect(rows[i].years).toBeLessThanOrEqual(rows[i - 1].years)
        }
      }
    })

    it('ending balances are non-increasing as the withdrawal rate rises', () => {
      const rows = calculateWithdrawal(1_000_000, 0.04, 0.07, 0.03, 30).rateAnalysis
      for (let i = 1; i < rows.length; i++) {
        expect(rows[i].endBalance).toBeLessThanOrEqual(rows[i - 1].endBalance)
      }
    })

    it('each row matches an independent drawdown at that rate', () => {
      const rows = calculateWithdrawal(1_000_000, 0.04, 0.03, 0.03, 40).rateAnalysis
      for (const row of rows) {
        const oracle = drawdownSeries(1_000_000, 1_000_000 * row.rate, 0.03, 0.03, 50)
        if (row.years < 50) {
          expect(oracle[row.years]).toBeGreaterThan(0)
          expect(oracle[row.years + 1]).toBeLessThanOrEqual(0)
        }
      }
    })

    it('agrees with the headline longevity when depletion beats both horizon caps', () => {
      // The headline stops at `retirementYears`; the table stops at a fixed 50-year horizon. When
      // the portfolio runs dry before either cap binds, both must report the same year. These three
      // cases all deplete early, so any disagreement here is a real inconsistency.
      for (const [portfolio, rate, ret, inflation, years] of [
        [1_000_000, 0.05, 0.03, 0.03, 40],
        [500_000, 0.05, 0.02, 0.04, 45],
        [1_000_000, 0.04, 0.04, 0.05, 45],
      ] as const) {
        const result = calculateWithdrawal(portfolio, rate, ret, inflation, years)
        const row = result.rateAnalysis.find((r) => Math.abs(r.rate - rate) < 1e-12)
        expect(row, `no rate analysis row for ${rate}`).toBeDefined()
        expect(result.portfolioLongevity).toBeLessThan(years)
        expect(row!.years).toBeLessThan(50)
        expect(result.portfolioLongevity).toBe(row!.years)
      }
    })

    it('the two horizons bind independently when the portfolio survives', () => {
      // Characterization, not a defect: a portfolio that outlives both windows reports the
      // headline capped at `retirementYears` (30) and the table capped at its own 50-year horizon.
      // They describe different questions, so they are allowed to differ here — but only here.
      const result = calculateWithdrawal(1_000_000, 0.04, 0.07, 0.03, 30)
      expect(result.portfolioLongevity).toBe(30)
      expect(result.rateAnalysis.find((r) => r.rate === 0.04)!.years).toBe(50)
    })
  })

  describe('economic sanity', () => {
    it('a portfolio earning more than it pays out never depletes', () => {
      // Withdrawals grow at 3% while the portfolio earns 7%: the balance rises without bound.
      const result = calculateWithdrawal(1_000_000, 0.03, 0.07, 0.03, 50)
      expect(result.portfolioLongevity).toBe(50)
      expect(result.endingBalance).toBeGreaterThan(1_000_000)
    })

    it('a portfolio paying out far more than it earns depletes quickly', () => {
      const result = calculateWithdrawal(1_000_000, 0.20, 0.02, 0.03, 40)
      expect(result.portfolioLongevity).toBeLessThan(10)
    })

    it('a larger portfolio at the same rate lasts exactly as long', () => {
      // The drawdown recurrence is homogeneous of degree one, so scaling the portfolio scales every
      // balance and leaves the depletion year unchanged.
      const small = calculateWithdrawal(500_000, 0.05, 0.03, 0.03, 40)
      const large = calculateWithdrawal(5_000_000, 0.05, 0.03, 0.03, 40)
      expect(large.portfolioLongevity).toBe(small.portfolioLongevity)
    })
  })
})
