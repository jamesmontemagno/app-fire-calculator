import { describe, expect, it } from 'vitest'

import type { DebtItem } from '../calculations'
import {
  calculateAvalanchePayoff,
  calculateDebtPayoffByTimeline,
  calculateSnowballPayoff,
} from '../calculations'
import { amortize, monthsToPayOffClosedForm } from './oracles'

/**
 * Debt payoff.
 *
 * Issue #45: interest was accrued more than once per month, so a $10,000 card at 20% APR paid at
 * $500/mo was reported as 34 months and $6,617 of interest, with a first month of $333.33 — exactly
 * double the correct $166.67. The oracle here is the standard amortization closed form
 *
 *   n = -ln(1 - P*m/A) / ln(1+m),   m = APR/12
 *
 * cross-checked against a from-scratch monthly recurrence that accrues interest exactly once.
 */

const card = (balance: number, rate: number, minPayment: number, name = 'Card'): DebtItem => ({
  id: name.toLowerCase().replace(/\s+/g, '-'),
  name,
  balance,
  rate,
  minPayment,
})

describe('single debt', () => {
  it('matches the closed-form month count across a parameter table', () => {
    const table: [number, number, number][] = [
      [10_000, 0.2, 500],
      [10_000, 0.2, 1_000],
      [25_000, 0.18, 800],
      [5_000, 0.24, 250],
      [40_000, 0.06, 900],
      [15_000, 0.1499, 400],
    ]
    for (const [balance, rate, payment] of table) {
      const exact = monthsToPayOffClosedForm(balance, rate, payment)
      expect(exact, `unreachable payoff for ${balance}/${rate}/${payment}`).not.toBeNull()
      const result = calculateSnowballPayoff([card(balance, rate, payment)], payment)
      // The lender charges interest before the payment lands, so a fractional month still needs a
      // whole payment to clear.
      expect(result.totalMonths).toBe(Math.ceil(exact!))
    }
  })

  it('reproduces the $10,000 at 20% APR anchor exactly', () => {
    // n = -ln(1 - 10000*(0.2/12)/500)/ln(1 + 0.2/12) = 24.53 months, so 25 payments.
    // Total interest from the once-per-month recurrence is $2,266.
    const result = calculateSnowballPayoff([card(10_000, 0.2, 500)], 500)
    expect(result.totalMonths).toBe(25)
    expect(result.totalInterest).toBe(2_266)
    expect(result.totalPrincipal).toBe(10_000)
  })

  it('charges interest exactly once in the first month', () => {
    // Chosen so the exact monthly interest is a whole number and rounding cannot hide a factor of
    // two: 12,000 * 0.20/12 = 200 exactly. The #45 defect would report 400 here.
    const result = calculateSnowballPayoff([card(12_000, 0.2, 500)], 500)
    expect(result.projections[0].interestPaid).toBe(200)
  })

  it.each([
    [10_000, 0.2, 500],
    [12_000, 0.2, 500],
    [25_000, 0.18, 800],
    [5_000, 0.24, 250],
  ] as const)(
    'total interest matches an independent recurrence for %s at %s paying %s',
    (balance, rate, payment) => {
      const oracle = amortize(balance, rate, payment)
      const result = calculateSnowballPayoff([card(balance, rate, payment)], payment)
      expect(result.totalMonths).toBe(oracle.months)
      expect(result.totalInterest).toBe(Math.round(oracle.totalInterest))
    },
  )

  it('accrues no interest at all on a 0% balance', () => {
    // 6,000 at 0% paying 500 clears in exactly 12 months with nothing added.
    const result = calculateSnowballPayoff([card(6_000, 0, 500)], 500)
    expect(result.totalMonths).toBe(12)
    expect(result.totalInterest).toBe(0)
  })

  it('a larger payment never costs more interest or takes longer', () => {
    const results = [400, 500, 750, 1_000, 2_000].map((payment) =>
      calculateSnowballPayoff([card(10_000, 0.2, payment)], payment),
    )
    for (let i = 1; i < results.length; i++) {
      expect(results[i].totalMonths).toBeLessThanOrEqual(results[i - 1].totalMonths)
      expect(results[i].totalInterest).toBeLessThanOrEqual(results[i - 1].totalInterest)
    }
  })

  it('a higher rate never costs less interest', () => {
    const interest = [0, 0.05, 0.1, 0.2, 0.29].map(
      (rate) => calculateSnowballPayoff([card(10_000, rate, 500)], 500).totalInterest,
    )
    for (let i = 1; i < interest.length; i++) expect(interest[i]).toBeGreaterThanOrEqual(interest[i - 1])
  })
})

describe('accounting identities', () => {
  const debts = [card(10_000, 0.2, 200, 'Card'), card(6_000, 0.08, 150, 'Loan'), card(3_000, 0.26, 100, 'Store')]

  it.each([
    ['snowball', calculateSnowballPayoff],
    ['avalanche', calculateAvalanchePayoff],
  ] as const)('%s repays exactly the principal owed', (_label, run) => {
    const result = run(debts, 450, 300)
    expect(result.totalPrincipal).toBe(19_000)
  })

  it.each([
    ['snowball', calculateSnowballPayoff],
    ['avalanche', calculateAvalanchePayoff],
  ] as const)('%s cumulative totals are the running sums of the monthly rows', (_label, run) => {
    const result = run(debts, 450, 300)
    let principal = 0
    let interest = 0
    result.projections.forEach((month) => {
      principal += month.principalPaid
      interest += month.interestPaid
      // Rows are individually rounded, so the running sum can drift by well under a dollar a month.
      expect(Math.abs(month.cumulativePrincipal - principal)).toBeLessThanOrEqual(result.projections.length)
      expect(Math.abs(month.cumulativeInterest - interest)).toBeLessThanOrEqual(result.projections.length)
    })
  })

  it.each([
    ['snowball', calculateSnowballPayoff],
    ['avalanche', calculateAvalanchePayoff],
  ] as const)('%s drives every balance to zero and names every debt exactly once', (_label, run) => {
    const result = run(debts, 450, 300)
    const last = result.projections[result.projections.length - 1]
    expect(last.totalBalance).toBe(0)
    expect(last.debtsRemaining).toHaveLength(0)
    expect([...result.payoffOrder].sort()).toEqual(['Card', 'Loan', 'Store'])
    expect(result.debtMilestones).toHaveLength(3)
  })

  it.each([
    ['snowball', calculateSnowballPayoff],
    ['avalanche', calculateAvalanchePayoff],
  ] as const)('%s reports the budget it was given', (_label, run) => {
    expect(run(debts, 450, 300).monthlyPayment).toBe(750)
  })

  it.each([
    ['snowball', calculateSnowballPayoff],
    ['avalanche', calculateAvalanchePayoff],
  ] as const)('%s numbers months consecutively from one', (_label, run) => {
    const result = run(debts, 450, 300)
    result.projections.forEach((month, index) => expect(month.month).toBe(index + 1))
    expect(result.totalMonths).toBe(result.projections.length)
  })

  it('total balances fall monotonically', () => {
    const result = calculateAvalanchePayoff(debts, 450, 300)
    for (let i = 1; i < result.projections.length; i++) {
      expect(result.projections[i].totalBalance).toBeLessThanOrEqual(result.projections[i - 1].totalBalance)
    }
  })
})

describe('strategy ordering', () => {
  const debts = [card(10_000, 0.08, 200, 'Big Low Rate'), card(2_000, 0.26, 100, 'Small High Rate'), card(5_000, 0.15, 150, 'Middle')]

  it('snowball clears the smallest balance first', () => {
    expect(calculateSnowballPayoff(debts, 450, 300).payoffOrder[0]).toBe('Small High Rate')
  })

  it('avalanche clears the highest rate first', () => {
    expect(calculateAvalanchePayoff(debts, 450, 300).payoffOrder[0]).toBe('Small High Rate')
  })

  it('avalanche orders strictly by descending rate', () => {
    const ordered = [card(10_000, 0.05, 200, 'A'), card(2_000, 0.3, 100, 'B'), card(5_000, 0.18, 150, 'C')]
    expect(calculateAvalanchePayoff(ordered, 450, 300).payoffOrder).toEqual(['B', 'C', 'A'])
  })

  it('snowball orders strictly by ascending balance', () => {
    const ordered = [card(10_000, 0.05, 200, 'A'), card(2_000, 0.3, 100, 'B'), card(5_000, 0.18, 150, 'C')]
    expect(calculateSnowballPayoff(ordered, 450, 300).payoffOrder).toEqual(['B', 'C', 'A'])
  })

  it.each([
    [[card(10_000, 0.05, 200, 'A'), card(2_000, 0.3, 100, 'B'), card(5_000, 0.18, 150, 'C')], 450, 300],
    [[card(8_000, 0.07, 150, 'A'), card(12_000, 0.22, 250, 'B')], 400, 0],
    [[card(3_000, 0.29, 90, 'A'), card(20_000, 0.06, 300, 'B'), card(7_500, 0.14, 180, 'C')], 570, 500],
    [[card(1_000, 0.1, 50, 'A'), card(1_000, 0.25, 50, 'B'), card(1_000, 0.18, 50, 'C')], 150, 100],
  ] as const)('avalanche never costs more total interest than snowball (case %#)', (debtSet, minimums, extra) => {
    // Paying the most expensive balance first is optimal for total interest by an exchange
    // argument, so the reverse can never hold. The pre-fix bug inverted this.
    const snowball = calculateSnowballPayoff([...debtSet], minimums, extra)
    const avalanche = calculateAvalanchePayoff([...debtSet], minimums, extra)
    expect(avalanche.totalInterest).toBeLessThanOrEqual(snowball.totalInterest)
  })

  it('the two strategies coincide when every rate is identical', () => {
    // With no rate differences there is nothing for avalanche to exploit.
    const flat = [card(9_000, 0.15, 200, 'A'), card(4_000, 0.15, 120, 'B'), card(6_000, 0.15, 150, 'C')]
    const snowball = calculateSnowballPayoff(flat, 470, 200)
    const avalanche = calculateAvalanchePayoff(flat, 470, 200)
    expect(avalanche.totalInterest).toBe(snowball.totalInterest)
    expect(avalanche.totalMonths).toBe(snowball.totalMonths)
  })

  it('extra payments never extend the payoff or increase interest', () => {
    const results = [0, 100, 300, 1_000].map((extra) => calculateAvalanchePayoff(debts, 450, extra))
    for (let i = 1; i < results.length; i++) {
      expect(results[i].totalMonths).toBeLessThanOrEqual(results[i - 1].totalMonths)
      expect(results[i].totalInterest).toBeLessThanOrEqual(results[i - 1].totalInterest)
    }
  })
})

describe('calculateDebtPayoffByTimeline', () => {
  const debts = [card(10_000, 0.2, 200, 'Card'), card(5_000, 0.1, 150, 'Loan')]

  it('finds a payment that clears the debt within the target', () => {
    const solved = calculateDebtPayoffByTimeline(debts, 24, 'avalanche')
    expect(solved).not.toBeNull()
    expect(solved!.result.totalMonths).toBeLessThanOrEqual(24)
  })

  it('a shorter deadline never requires a smaller payment', () => {
    const payments = [48, 36, 24, 12].map(
      (months) => calculateDebtPayoffByTimeline(debts, months, 'avalanche')?.requiredPayment ?? Infinity,
    )
    for (let i = 1; i < payments.length; i++) expect(payments[i]).toBeGreaterThanOrEqual(payments[i - 1])
  })

  it('the payment it reports actually meets the deadline when replayed', () => {
    // Round trip through the very function the solver is searching over.
    const solved = calculateDebtPayoffByTimeline(debts, 18, 'snowball')
    expect(solved).not.toBeNull()
    const replay = calculateSnowballPayoff(debts, solved!.requiredPayment)
    expect(replay.totalMonths).toBeLessThanOrEqual(18)
  })
})

describe('degenerate inputs', () => {
  it('reports nothing owed for an empty debt list', () => {
    const result = calculateSnowballPayoff([], 500)
    expect(result.totalMonths).toBe(0)
    expect(result.totalInterest).toBe(0)
    expect(result.totalPrincipal).toBe(0)
    expect(result.payoffOrder).toEqual([])
  })

  it('gives up rather than looping forever when the payment cannot cover interest', () => {
    // 10,000 at 30% accrues 250/month; paying 100 means the balance grows without bound. The
    // closed form has no solution here, so the implementation must terminate on its own guard.
    expect(monthsToPayOffClosedForm(10_000, 0.3, 100)).toBeNull()
    const result = calculateSnowballPayoff([card(10_000, 0.3, 100)], 100)
    expect(result.projections.length).toBeGreaterThan(0)
    expect(Number.isFinite(result.totalMonths)).toBe(true)
    expect(result.projections[result.projections.length - 1].totalBalance).toBeGreaterThan(10_000)
  })
})
