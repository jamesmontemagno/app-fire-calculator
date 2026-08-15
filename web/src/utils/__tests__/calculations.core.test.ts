import { describe, expect, it } from 'vitest'

import {
  contributionForYear,
  formatCurrency,
  formatPercent,
  futureValue,
  presentValue,
  realReturn,
  yearsToFIRETarget,
  yearsToTarget,
} from '../calculations'
import { realBalanceClosedForm, realRate, yearsToTargetClosedForm } from './oracles'

/**
 * Algebraic identities on the primitives every calculator is built from.
 *
 * These are properties, not recorded outputs: each one states a relationship that must hold for any
 * inputs, so they fail on a wrong formula rather than merely on a changed one.
 */

const RATES = [0, 0.01, 0.04, 0.07, 0.12]
const INFLATIONS = [0, 0.02, 0.03, 0.05]
const YEARS = [1, 5, 17, 30, 45]

describe('realReturn', () => {
  it.each(
    RATES.flatMap((r) => INFLATIONS.map((i) => [r, i] as const)),
  )('satisfies the Fisher relation (1+rho)(1+i) = (1+r) for r=%s i=%s', (r, i) => {
    expect((1 + realReturn(r, i)) * (1 + i)).toBeCloseTo(1 + r, 12)
  })

  it('is zero exactly when the return equals inflation', () => {
    // The degenerate case the annuity closed form divides by. It must be exactly 0, not merely
    // near it, or the divide-by-zero guard downstream will not trigger.
    expect(realReturn(0.05, 0.05)).toBe(0)
    expect(realReturn(0.03, 0.03)).toBe(0)
  })

  it('is negative when inflation outruns the return', () => {
    expect(realReturn(0.02, 0.05)).toBeLessThan(0)
  })
})

describe('futureValue / presentValue', () => {
  it.each(
    RATES.flatMap((r) => YEARS.map((n) => [r, n] as const)),
  )('round trips: presentValue(futureValue(PV, 0, r, n), r, n) = PV for r=%s n=%s', (r, n) => {
    const pv = 250_000
    expect(presentValue(futureValue(pv, 0, r, n), r, n)).toBeCloseTo(pv, 6)
  })

  it.each(
    RATES.flatMap((r) => YEARS.map((n) => [r, n] as const)),
  )('matches an independent annuity closed form for r=%s n=%s', (r, n) => {
    // FV = PV(1+r)^n + C((1+r)^n - 1)/r, with the r = 0 limit being PV + Cn.
    expect(futureValue(100_000, 12_000, r, n)).toBeCloseTo(
      realBalanceClosedForm(100_000, 12_000, r, 0, n),
      6,
    )
  })

  it('handles the zero-rate limit as simple accumulation', () => {
    // lim_{r->0} C((1+r)^n - 1)/r = Cn, so 10 years of 12,000 with no growth is exactly 120,000.
    expect(futureValue(0, 12_000, 0, 10)).toBe(120_000)
    expect(futureValue(50_000, 20_000, 0, 60)).toBe(1_250_000)
  })

  it('treats a non-positive horizon as no discounting', () => {
    expect(presentValue(1_000, 0.07, 0)).toBe(1_000)
    expect(presentValue(1_000, 0.07, -5)).toBe(1_000)
  })
})

describe('yearsToTarget', () => {
  it.each(
    [0.01, 0.04, 0.07].flatMap((r) => [500_000, 1_200_000, 3_000_000].map((t) => [r, t] as const)),
  )('is the inverse of futureValue for r=%s target=%s', (r, target) => {
    // The defining property: growing for exactly the returned number of years lands on the target.
    const n = yearsToTarget(100_000, 24_000, r, target)
    expect(Number.isFinite(n)).toBe(true)
    expect(futureValue(100_000, 24_000, r, n)).toBeCloseTo(target, 4)
  })

  it.each(
    [0.01, 0.04, 0.07].map((r) => [r] as const),
  )('matches n = ln((C + T*r)/(C + PV*r))/ln(1+r) for r=%s', (r) => {
    const exact = yearsToTargetClosedForm(100_000, 24_000, r, 1_200_000)
    expect(exact).not.toBeNull()
    expect(yearsToTarget(100_000, 24_000, r, 1_200_000)).toBeCloseTo(exact!, 10)
  })

  it('returns 0 when the target is already met', () => {
    expect(yearsToTarget(1_500_000, 24_000, 0.05, 1_200_000)).toBe(0)
    expect(yearsToTarget(1_200_000, 24_000, 0.05, 1_200_000)).toBe(0)
  })

  it('solves the zero-rate case linearly and exactly', () => {
    // 50,000 + 20,000n = 1,250,000 has the exact integer solution n = 60.
    expect(yearsToTarget(50_000, 20_000, 0, 1_250_000)).toBe(60)
  })

  it('is Infinity when there is no growth and no contribution', () => {
    expect(yearsToTarget(1_000, 0, 0, 5_000)).toBe(Infinity)
  })

  it('is Infinity when the balance converges below the target', () => {
    // rho < 0 gives the stable fixed point B* = -C/rho. With C = 20,000 and rho = -0.0285714...
    // that ceiling is exactly 700,000, so a 1,250,000 target is unreachable for any n. Infinity is
    // the mathematically correct answer, not a failure to converge.
    const rho = realRate(0.02, 0.05)
    expect(-20_000 / rho).toBeCloseTo(700_000, 6)
    expect(yearsToTarget(100_000, 20_000, rho, 1_250_000)).toBe(Infinity)
  })

  it('never reports a target as reached before the balance covers it', () => {
    // Monotone consistency: one year short of the answer must still be under the target.
    const n = yearsToTarget(100_000, 24_000, 0.04, 1_200_000)
    expect(futureValue(100_000, 24_000, 0.04, n - 1)).toBeLessThan(1_200_000)
  })
})

describe('yearsToFIRETarget', () => {
  it.each(
    [
      [0.07, 0.03],
      [0.06, 0.02],
      [0.09, 0.04],
    ] as const,
  )('with inflation growth equals the real-rate closed form for r=%s i=%s', (r, i) => {
    const exact = yearsToTargetClosedForm(100_000, 24_000, realRate(r, i), 1_200_000)
    expect(exact).not.toBeNull()
    expect(yearsToFIRETarget(100_000, 24_000, r, i, 1_200_000, 'inflation')).toBeCloseTo(exact!, 10)
  })

  it.each(
    [
      [0.07, 0.03],
      [0.06, 0.02],
      [0.09, 0.04],
    ] as const,
  )('with flat growth the solved year lands on the target for r=%s i=%s', (r, i) => {
    // The flat path has no closed form, so the property asserted is the defining one: the deflated
    // balance at the returned time is the target. Verified against an independently simulated
    // nominal series, deflated, rather than against the solver's own internals.
    const years = yearsToFIRETarget(100_000, 24_000, r, i, 1_200_000, 'flat')
    expect(Number.isFinite(years)).toBe(true)

    let nominal = 100_000
    const whole = Math.floor(years)
    for (let k = 0; k < whole; k++) nominal = nominal * (1 + r) + 24_000
    const atFloor = nominal / Math.pow(1 + i, whole)
    const atCeil = (nominal * (1 + r) + 24_000) / Math.pow(1 + i, whole + 1)

    expect(atFloor).toBeLessThan(1_200_000)
    expect(atCeil).toBeGreaterThanOrEqual(1_200_000)
  })

  it('flat contributions always take at least as long as inflation-matched ones', () => {
    // A flat nominal contribution loses purchasing power every year, so it cannot reach a
    // today's-dollars target sooner than one that holds its value.
    for (const [r, i] of [
      [0.07, 0.03],
      [0.08, 0.02],
      [0.05, 0.04],
    ] as const) {
      const flat = yearsToFIRETarget(100_000, 24_000, r, i, 1_200_000, 'flat')
      const inflation = yearsToFIRETarget(100_000, 24_000, r, i, 1_200_000, 'inflation')
      expect(flat).toBeGreaterThanOrEqual(inflation)
    }
  })

  it('returns 0 when already funded, for both growth modes', () => {
    expect(yearsToFIRETarget(1_300_000, 24_000, 0.07, 0.03, 1_200_000, 'inflation')).toBe(0)
    expect(yearsToFIRETarget(1_300_000, 24_000, 0.07, 0.03, 1_200_000, 'flat')).toBe(0)
  })
})

describe('contributionForYear', () => {
  it('holds purchasing power constant under inflation growth', () => {
    // The convention the whole inflation identity rests on: the nominal contribution in year k is
    // C(1+i)^k, so deflating it by (1+i)^k returns exactly C for every k.
    for (const k of [0, 1, 7, 30]) {
      const nominal = contributionForYear(24_000, 0.03, k, 'inflation')
      expect(nominal / Math.pow(1.03, k)).toBeCloseTo(24_000, 8)
    }
  })

  it('keeps the nominal amount fixed under flat growth', () => {
    for (const k of [0, 1, 7, 30]) {
      expect(contributionForYear(24_000, 0.03, k, 'flat')).toBe(24_000)
    }
  })

  it('defaults to inflation growth', () => {
    expect(contributionForYear(24_000, 0.03, 5)).toBe(contributionForYear(24_000, 0.03, 5, 'inflation'))
  })
})

describe('formatters', () => {
  it('renders whole-dollar currency', () => {
    expect(formatCurrency(1_234_567)).toBe('$1,234,567')
    expect(formatCurrency(0)).toBe('$0')
  })

  it('renders decimals as percentages', () => {
    expect(formatPercent(0.07)).toBe('7.0%')
    expect(formatPercent(0.335)).toBe('33.5%')
    expect(formatPercent(0)).toBe('0.0%')
  })
})
