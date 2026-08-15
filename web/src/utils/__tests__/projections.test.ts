import { describe, expect, it } from 'vitest'

import { generateProjections, realReturn } from '../calculations'
import { deflate, nominalEscalatingSeries, nominalFlatSeries, realBalanceClosedForm, realRate } from './oracles'

/**
 * The inflation identity.
 *
 * Contributions are entered in today's dollars and are actually paid as C(1+i)^k nominal at the end
 * of year k. Under that convention the deflated projection is *identical* to the closed form used
 * for the headline FIRE age, not an approximation of it:
 *
 *   P_n/(1+i)^n = PV(1+rho)^n + C((1+rho)^n - 1)/rho,     rho = (1+r)/(1+i) - 1
 *
 * That equality is the strongest oracle available in this codebase, because it ties the chart the
 * user sees to the number the headline reports. Issue #46 was precisely those two disagreeing.
 */

const SCENARIOS = [
  { label: 'shipped defaults', pv: 100_000, c: 24_000, r: 0.07, i: 0.03 },
  { label: 'high inflation', pv: 250_000, c: 30_000, r: 0.09, i: 0.06 },
  { label: 'zero inflation', pv: 50_000, c: 12_000, r: 0.05, i: 0 },
  { label: 'zero return', pv: 80_000, c: 10_000, r: 0, i: 0.02 },
  { label: 'degenerate rho = 0', pv: 50_000, c: 20_000, r: 0.05, i: 0.05 },
  { label: 'negative rho', pv: 100_000, c: 20_000, r: 0.02, i: 0.05 },
  { label: 'no contribution', pv: 400_000, c: 0, r: 0.07, i: 0.03 },
] as const

describe('generateProjections structure', () => {
  it('emits one point per year inclusive of both endpoints', () => {
    expect(generateProjections(30, 100_000, 24_000, 0.07, 0.03, 25)).toHaveLength(26)
    expect(generateProjections(30, 100_000, 24_000, 0.07, 0.03, 0)).toHaveLength(1)
  })

  it('advances age by exactly one year per point', () => {
    const points = generateProjections(42, 100_000, 24_000, 0.07, 0.03, 20)
    points.forEach((point, index) => expect(point.age).toBe(42 + index))
  })

  it('starts at the current savings with no growth or discounting applied', () => {
    const [first] = generateProjections(30, 100_000, 24_000, 0.07, 0.03, 10)
    expect(first.portfolio).toBe(100_000)
    expect(first.inflationAdjusted).toBe(100_000)
    expect(first.totalContributions).toBe(100_000)
    expect(first.contributions).toBe(100_000)
  })
})

describe.each(SCENARIOS.map((s) => [s.label, s] as const))('inflation identity: %s', (_label, s) => {
  const YEARS = 30
  const projections = generateProjections(30, s.pv, s.c, s.r, s.i, YEARS, 'inflation')

  it('portfolio matches a from-scratch nominal recurrence B_k = B_{k-1}(1+r) + C(1+i)^k', () => {
    const series = nominalEscalatingSeries(s.pv, s.c, s.r, s.i, YEARS)
    projections.forEach((point, k) => expect(point.portfolio).toBe(Math.round(series[k])))
  })

  it('inflationAdjusted equals the deflated nominal series', () => {
    const series = nominalEscalatingSeries(s.pv, s.c, s.r, s.i, YEARS)
    projections.forEach((point, k) =>
      expect(point.inflationAdjusted).toBe(Math.round(deflate(series[k], s.i, k))),
    )
  })

  it('inflationAdjusted equals the closed form exactly, so the chart and headline cannot diverge', () => {
    projections.forEach((point, k) =>
      expect(point.inflationAdjusted).toBe(
        Math.round(realBalanceClosedForm(s.pv, s.c, s.r, s.i, k)),
      ),
    )
  })

  it('recurrence and closed form agree to floating-point noise, confirming an identity', () => {
    // If this were an approximation the gap would grow with n. Asserting a relative tolerance at
    // year 30 rules that out.
    const series = nominalEscalatingSeries(s.pv, s.c, s.r, s.i, YEARS)
    const recurrence = deflate(series[YEARS], s.i, YEARS)
    const closedForm = realBalanceClosedForm(s.pv, s.c, s.r, s.i, YEARS)
    expect(Math.abs(recurrence - closedForm) / Math.max(1, Math.abs(closedForm))).toBeLessThan(1e-12)
  })
})

describe.each(SCENARIOS.map((s) => [s.label, s] as const))('flat contributions: %s', (_label, s) => {
  const YEARS = 30
  const projections = generateProjections(30, s.pv, s.c, s.r, s.i, YEARS, 'flat')

  it('portfolio matches B_k = B_{k-1}(1+r) + C with no escalation', () => {
    const series = nominalFlatSeries(s.pv, s.c, s.r, YEARS)
    projections.forEach((point, k) => expect(point.portfolio).toBe(Math.round(series[k])))
  })

  it('contributions stay nominally constant after the seed year', () => {
    projections.slice(1).forEach((point) => expect(point.contributions).toBe(s.c))
  })
})

describe('growth mode comparison', () => {
  it('flat never outgrows inflation-matched contributions in real terms', () => {
    // Same nominal contribution in year one, but flat loses purchasing power thereafter, so its
    // deflated balance can never lead. Equality holds only when i = 0 or C = 0.
    const inflation = generateProjections(30, 100_000, 24_000, 0.07, 0.03, 40, 'inflation')
    const flat = generateProjections(30, 100_000, 24_000, 0.07, 0.03, 40, 'flat')
    flat.forEach((point, k) => expect(point.inflationAdjusted).toBeLessThanOrEqual(inflation[k].inflationAdjusted))
  })

  it('the two modes coincide when inflation is zero', () => {
    const inflation = generateProjections(30, 100_000, 24_000, 0.07, 0, 20, 'inflation')
    const flat = generateProjections(30, 100_000, 24_000, 0.07, 0, 20, 'flat')
    expect(flat.map((p) => p.portfolio)).toEqual(inflation.map((p) => p.portfolio))
  })
})

describe('degenerate and unreachable real returns', () => {
  it('rho = 0 grows linearly in today\u2019s dollars', () => {
    // r = i = 5% means every year adds exactly the real contribution and nothing compounds:
    // 50,000 + 20,000k. This is the divide-by-zero the annuity closed form must special-case.
    expect(realReturn(0.05, 0.05)).toBe(0)
    const projections = generateProjections(30, 50_000, 20_000, 0.05, 0.05, 60)
    projections.forEach((point, k) => expect(point.inflationAdjusted).toBe(50_000 + 20_000 * k))
    expect(projections[60].inflationAdjusted).toBe(1_250_000)
  })

  it('rho < 0 converges to the fixed point -C/rho and never exceeds it', () => {
    // A 2% return against 5% inflation. The real balance rises but is bounded by 700,000 exactly,
    // which is why a 1,250,000 target correctly reports Infinity rather than some large year count.
    const rho = realRate(0.02, 0.05)
    const ceiling = -20_000 / rho
    expect(ceiling).toBeCloseTo(700_000, 6)

    const projections = generateProjections(30, 100_000, 20_000, 0.02, 0.05, 100)
    projections.forEach((point) => expect(point.inflationAdjusted).toBeLessThan(Math.ceil(ceiling)))

    // Exact approach law: rewriting the recurrence about its fixed point gives
    //   B_k = B* - (B* - B_0)(1+rho)^k
    // so the gap to the ceiling shrinks geometrically. At k = 100, (1+rho)^100 is still ~0.055,
    // hence ~666,900 rather than something arbitrarily close to 700,000 — convergence is slow,
    // which is exactly why no finite year count reaches a 1,250,000 target.
    projections.forEach((point, k) => {
      const exact = ceiling - (ceiling - 100_000) * Math.pow(1 + rho, k)
      expect(point.inflationAdjusted).toBe(Math.round(exact))
    })

    for (let k = 1; k < projections.length; k++) {
      expect(projections[k].inflationAdjusted).toBeGreaterThanOrEqual(projections[k - 1].inflationAdjusted)
    }
  })
})

describe('totalContributions accounting', () => {
  it('equals the seed plus every nominal contribution paid so far', () => {
    const projections = generateProjections(30, 100_000, 24_000, 0.07, 0.03, 20, 'inflation')
    let running = 100_000
    projections.forEach((point, k) => {
      if (k > 0) running += 24_000 * Math.pow(1.03, k)
      expect(point.totalContributions).toBe(Math.round(running))
    })
  })

  it('never exceeds the portfolio when returns are non-negative', () => {
    const projections = generateProjections(30, 100_000, 24_000, 0.07, 0.03, 30)
    projections.forEach((point) => expect(point.totalContributions).toBeLessThanOrEqual(point.portfolio))
  })
})
