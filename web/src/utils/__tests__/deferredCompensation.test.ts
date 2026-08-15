import { describe, expect, it } from 'vitest'

import type {
  DeferredCompensationInputs,
  RetirementAccount,
  RetirementExpense,
  RetirementIncomeSource,
} from '../deferredCompensation'
import {
  ORDINARY_INCOME_TAX_RATE,
  calculateDeferredCompensation,
  defaultWithdrawalTaxRate,
} from '../deferredCompensation'

/**
 * Deferred compensation planner.
 *
 * `currentYear` is injectable, so every projection here is deterministic rather than dependent on
 * the wall clock. Expectations come from the underlying identities: expenses inflate geometrically,
 * a deferred account distributes its whole balance over its payout window, and every dollar leaving
 * an account is either spendable income or withdrawal tax.
 */

const account = (overrides: Partial<RetirementAccount> = {}): RetirementAccount => ({
  id: 'acct',
  name: 'Account',
  type: 'taxable',
  balance: 500_000,
  annualContribution: 0,
  annualReturn: 0.05,
  availableAge: 0,
  withdrawalRate: 1,
  payoutYears: 10,
  withdrawalTaxRate: 0,
  ...overrides,
})

const income = (overrides: Partial<RetirementIncomeSource> = {}): RetirementIncomeSource => ({
  id: 'job',
  name: 'Job',
  type: 'salary',
  annualAmount: 50_000,
  startAge: 0,
  endAge: 200,
  annualGrowth: 0,
  isAfterTax: true,
  taxRate: 0,
  ...overrides,
})

const expense = (overrides: Partial<RetirementExpense> = {}): RetirementExpense => ({
  id: 'extra',
  name: 'Extra',
  type: 'custom',
  annualAmount: 10_000,
  startAge: 0,
  ...overrides,
})

const plan = (overrides: Partial<DeferredCompensationInputs> = {}): DeferredCompensationInputs => ({
  currentAge: 45,
  semiRetirementAge: 55,
  planThroughAge: 90,
  annualExpenses: 80_000,
  inflationRate: 0.03,
  accounts: [account()],
  incomeSources: [],
  additionalExpenses: [],
  withdrawOnlyAfterRetirement: true,
  reinvestSurplus: false,
  currentYear: 2025,
  ...overrides,
})

describe('defaultWithdrawalTaxRate', () => {
  it('taxes tax-deferred withdrawals as ordinary income', () => {
    expect(ORDINARY_INCOME_TAX_RATE).toBe(0.25)
    expect(defaultWithdrawalTaxRate('deferred')).toBe(0.25)
    expect(defaultWithdrawalTaxRate('traditional')).toBe(0.25)
  })

  it('leaves genuinely tax-free and basis-tracked accounts untaxed', () => {
    // Roth and HSA are tax-free; taxable and savings are zero because the model tracks no cost
    // basis and taxing the whole withdrawal would overstate it.
    for (const type of ['roth', 'hsa', 'taxable', 'savings', 'other'] as const) {
      expect(defaultWithdrawalTaxRate(type)).toBe(0)
    }
  })
})

describe('projection structure', () => {
  it('covers every age from today through the planning horizon inclusive', () => {
    const result = calculateDeferredCompensation(plan())
    expect(result.projections).toHaveLength(46)
    expect(result.projections[0].age).toBe(45)
    expect(result.projections.at(-1)!.age).toBe(90)
  })

  it('derives calendar years from the injected start year', () => {
    const result = calculateDeferredCompensation(plan({ currentYear: 2030 }))
    result.projections.forEach((point, index) => expect(point.year).toBe(2030 + index))
  })

  it('floors fractional ages and never inverts the horizon', () => {
    const result = calculateDeferredCompensation(
      plan({ currentAge: 45.9, semiRetirementAge: 40, planThroughAge: 30 }),
    )
    expect(result.projections[0].age).toBe(45)
    expect(result.projections.at(-1)!.age).toBeGreaterThanOrEqual(45)
  })

  it('reports the starting balance before any growth', () => {
    expect(calculateDeferredCompensation(plan()).currentBalance).toBe(500_000)
  })

  it('ignores negative balances rather than subtracting them', () => {
    const result = calculateDeferredCompensation(
      plan({ accounts: [account({ balance: -100_000 })] }),
    )
    expect(result.currentBalance).toBe(0)
  })
})

describe('expense inflation', () => {
  it('escalates core expenses geometrically from today', () => {
    const result = calculateDeferredCompensation(plan())
    result.projections.forEach((point, k) => {
      expect(point.coreExpenses).toBe(Math.round(80_000 * Math.pow(1.03, k)))
    })
  })

  it('starts additional expenses only at their own age, then inflates from today', () => {
    // The multiplier is anchored to the plan start, not to the expense start, so an expense
    // beginning at 65 enters at its year-20 inflated value rather than its entered value.
    const result = calculateDeferredCompensation(
      plan({ additionalExpenses: [expense({ annualAmount: 10_000, startAge: 65 })] }),
    )
    const before = result.projections.find((p) => p.age === 64)!
    const at = result.projections.find((p) => p.age === 65)!
    expect(before.additionalExpenses).toBe(0)
    expect(at.additionalExpenses).toBe(Math.round(10_000 * Math.pow(1.03, 20)))
  })

  it('totals core and additional expenses', () => {
    const result = calculateDeferredCompensation(
      plan({ additionalExpenses: [expense({ startAge: 0 })] }),
    )
    result.projections.forEach((point) => {
      expect(Math.abs(point.expenses - (point.coreExpenses + point.additionalExpenses))).toBeLessThanOrEqual(1)
    })
  })

  it('holds expenses flat when inflation is zero', () => {
    const result = calculateDeferredCompensation(plan({ inflationRate: 0 }))
    result.projections.forEach((point) => expect(point.coreExpenses).toBe(80_000))
  })
})

describe('contribution escalation', () => {
  it('escalates contributions with inflation while still working', () => {
    // The #51/#57 fix: contributions are entered in today's dollars, so the nominal amount paid in
    // year k is C(1+i)^k, matching how expenses are escalated. A balance that grew by only the flat
    // entered amount would fall short of this by a widening margin.
    const result = calculateDeferredCompensation(
      plan({
        accounts: [account({ balance: 100_000, annualContribution: 20_000, annualReturn: 0.06, withdrawalRate: 0 })],
        incomeSources: [income({ annualAmount: 500_000 })],
      }),
    )

    let balance = 100_000
    for (let k = 1; k <= 10; k++) {
      balance = balance * 1.06
      if (45 + k < 55) balance += 20_000 * Math.pow(1.03, k)
      const point = result.projections.find((p) => p.age === 45 + k)!
      expect(point.totalBalance).toBe(Math.round(balance))
    }
  })

  it('stops contributing at retirement age', () => {
    const result = calculateDeferredCompensation(
      plan({
        accounts: [account({ balance: 100_000, annualContribution: 20_000, annualReturn: 0, withdrawalRate: 0 })],
        incomeSources: [income({ annualAmount: 500_000 })],
      }),
    )
    // Ten escalating contributions land at ages 46..54, then nothing further.
    let expected = 100_000
    for (let k = 1; k <= 9; k++) expected += 20_000 * Math.pow(1.03, k)
    expect(result.projections.find((p) => p.age === 54)!.totalBalance).toBe(Math.round(expected))
    expect(result.projections.find((p) => p.age === 55)!.totalBalance).toBe(Math.round(expected))
  })
})

describe('deferred account payouts', () => {
  const deferred = account({
    id: 'defcomp',
    type: 'deferred',
    balance: 500_000,
    annualReturn: 0,
    availableAge: 55,
    payoutYears: 10,
    withdrawalTaxRate: 0,
  })

  it('distributes the whole balance over the payout window, leaving nothing stranded', () => {
    // With no growth, an even split of 500,000 over 10 years is 50,000 a year and exactly zero left.
    const result = calculateDeferredCompensation(
      plan({ accounts: [deferred], incomeSources: [income({ annualAmount: 200_000 })] }),
    )
    for (let age = 55; age <= 64; age++) {
      expect(result.projections.find((p) => p.age === age)!.withdrawals.defcomp).toBe(50_000)
    }
    expect(result.projections.find((p) => p.age === 64)!.balances.defcomp).toBe(0)
    expect(result.projections.find((p) => p.age === 65)!.withdrawals.defcomp).toBe(0)
  })

  it('pays nothing before the available age or after the window closes', () => {
    const result = calculateDeferredCompensation(
      plan({ accounts: [deferred], incomeSources: [income({ annualAmount: 200_000 })] }),
    )
    expect(result.projections.find((p) => p.age === 54)!.deferredIncome).toBe(0)
    expect(result.projections.find((p) => p.age === 65)!.deferredIncome).toBe(0)
  })

  it('still empties the account when the balance keeps earning', () => {
    // Each year distributes the remaining balance over the remaining years, so growth is absorbed
    // into later payments rather than stranded at the end.
    const result = calculateDeferredCompensation(
      plan({
        accounts: [account({ ...deferred, annualReturn: 0.06 })],
        incomeSources: [income({ annualAmount: 200_000 })],
      }),
    )
    expect(result.projections.find((p) => p.age === 64)!.balances.defcomp).toBe(0)
  })

  it('pays out over exactly one year when payoutYears is zero or negative', () => {
    for (const payoutYears of [0, -5]) {
      const result = calculateDeferredCompensation(
        plan({
          accounts: [account({ ...deferred, payoutYears })],
          incomeSources: [income({ annualAmount: 200_000 })],
        }),
      )
      expect(result.projections.find((p) => p.age === 55)!.withdrawals.defcomp).toBe(500_000)
      expect(result.projections.find((p) => p.age === 56)!.withdrawals.defcomp).toBe(0)
    }
  })

  it('withholds the estimated tax from spendable deferred income', () => {
    const result = calculateDeferredCompensation(
      plan({
        accounts: [account({ ...deferred, withdrawalTaxRate: 0.25 })],
        incomeSources: [income({ annualAmount: 200_000 })],
      }),
    )
    const point = result.projections.find((p) => p.age === 55)!
    expect(point.withdrawals.defcomp).toBe(50_000)
    expect(point.deferredIncome).toBe(37_500)
    expect(point.withdrawalTaxes).toBe(12_500)
  })

  it('clamps a nonsensical tax rate into [0, 1]', () => {
    for (const [rate, expectedNet] of [
      [-1, 50_000],
      [2, 0],
    ] as const) {
      const result = calculateDeferredCompensation(
        plan({
          accounts: [account({ ...deferred, withdrawalTaxRate: rate })],
          incomeSources: [income({ annualAmount: 200_000 })],
        }),
      )
      expect(result.projections.find((p) => p.age === 55)!.deferredIncome).toBe(expectedNet)
    }
  })
})

describe('income sources', () => {
  it('pays only between start and end age', () => {
    const result = calculateDeferredCompensation(
      plan({ incomeSources: [income({ annualAmount: 40_000, startAge: 62, endAge: 70 })] }),
    )
    expect(result.projections.find((p) => p.age === 61)!.outsideIncome).toBe(0)
    expect(result.projections.find((p) => p.age === 62)!.outsideIncome).toBe(40_000)
    expect(result.projections.find((p) => p.age === 70)!.outsideIncome).toBe(40_000)
    expect(result.projections.find((p) => p.age === 71)!.outsideIncome).toBe(0)
  })

  it('grows by its own rate compounded from today, not from its start age', () => {
    const result = calculateDeferredCompensation(
      plan({ incomeSources: [income({ annualAmount: 40_000, startAge: 62, endAge: 70, annualGrowth: 0.02 })] }),
    )
    expect(result.projections.find((p) => p.age === 62)!.outsideIncome).toBe(
      Math.round(40_000 * Math.pow(1.02, 17)),
    )
  })

  it('applies the tax rate only to pre-tax sources', () => {
    const afterTax = calculateDeferredCompensation(
      plan({ incomeSources: [income({ annualAmount: 40_000, isAfterTax: true, taxRate: 0.3 })] }),
    )
    const preTax = calculateDeferredCompensation(
      plan({ incomeSources: [income({ annualAmount: 40_000, isAfterTax: false, taxRate: 0.3 })] }),
    )
    expect(afterTax.projections[0].outsideIncome).toBe(40_000)
    expect(preTax.projections[0].outsideIncome).toBe(28_000)
  })

  it('breaks income out by source consistently with the total', () => {
    const result = calculateDeferredCompensation(
      plan({
        incomeSources: [
          income({ id: 'a', annualAmount: 30_000 }),
          income({ id: 'b', annualAmount: 12_000 }),
        ],
      }),
    )
    const point = result.projections[0]
    const summed = Object.values(point.incomeBySource).reduce((sum, value) => sum + value, 0)
    expect(Math.abs(summed - point.outsideIncome)).toBeLessThanOrEqual(1)
  })
})

describe('gap withdrawals', () => {
  it('withdraws exactly the shortfall when withdrawals are untaxed', () => {
    const result = calculateDeferredCompensation(
      plan({
        inflationRate: 0,
        accounts: [account({ balance: 1_000_000, annualReturn: 0, withdrawalRate: 1 })],
        incomeSources: [income({ annualAmount: 30_000 })],
      }),
    )
    const point = result.projections.find((p) => p.age === 55)!
    expect(point.expenses).toBe(80_000)
    expect(point.outsideIncome).toBe(30_000)
    expect(point.withdrawals.acct).toBe(50_000)
    expect(point.surplus).toBe(0)
  })

  it('grosses the withdrawal up so the spendable remainder still covers the gap', () => {
    // A 50,000 gap at a 20% withdrawal tax needs 62,500 gross: 62,500 * 0.8 = 50,000.
    const result = calculateDeferredCompensation(
      plan({
        inflationRate: 0,
        accounts: [account({ balance: 1_000_000, annualReturn: 0, withdrawalRate: 1, withdrawalTaxRate: 0.2 })],
        incomeSources: [income({ annualAmount: 30_000 })],
      }),
    )
    const point = result.projections.find((p) => p.age === 55)!
    expect(point.withdrawals.acct).toBe(62_500)
    expect(point.portfolioWithdrawals).toBe(50_000)
    expect(point.withdrawalTaxes).toBe(12_500)
    expect(point.surplus).toBe(0)
  })

  it('takes nothing before retirement when withdrawals are restricted', () => {
    const result = calculateDeferredCompensation(
      plan({ withdrawOnlyAfterRetirement: true, accounts: [account({ balance: 1_000_000 })] }),
    )
    result.projections
      .filter((p) => p.age < 55)
      .forEach((p) => expect(p.portfolioWithdrawals).toBe(0))
  })

  it('covers a pre-retirement gap when withdrawals are unrestricted', () => {
    const result = calculateDeferredCompensation(
      plan({ withdrawOnlyAfterRetirement: false, accounts: [account({ balance: 1_000_000 })] }),
    )
    expect(result.projections[0].portfolioWithdrawals).toBeGreaterThan(0)
  })

  it('respects the per-account availability age', () => {
    const result = calculateDeferredCompensation(
      plan({ accounts: [account({ balance: 1_000_000, availableAge: 60 })] }),
    )
    expect(result.projections.find((p) => p.age === 59)!.portfolioWithdrawals).toBe(0)
    expect(result.projections.find((p) => p.age === 60)!.portfolioWithdrawals).toBeGreaterThan(0)
  })

  it('records what the per-account withdrawal-rate cap held back', () => {
    // A 4% cap on 1,000,000 releases 40,000 against an 80,000 gap, so 40,000 stays put and is
    // reported rather than silently dropped.
    const result = calculateDeferredCompensation(
      plan({
        inflationRate: 0,
        accounts: [account({ balance: 1_000_000, annualReturn: 0, withdrawalRate: 0.04 })],
      }),
    )
    const point = result.projections.find((p) => p.age === 55)!
    expect(point.withdrawals.acct).toBe(40_000)
    expect(point.policyLimitedWithdrawals).toBe(40_000)
    expect(point.surplus).toBe(-40_000)
  })

  it('never withdraws more than the account holds', () => {
    const result = calculateDeferredCompensation(
      plan({ inflationRate: 0, accounts: [account({ balance: 30_000, annualReturn: 0, withdrawalRate: 1 })] }),
    )
    const point = result.projections.find((p) => p.age === 55)!
    expect(point.withdrawals.acct).toBeLessThanOrEqual(30_000)
    expect(point.balances.acct).toBe(0)
  })

  it('never reports a negative balance', () => {
    const result = calculateDeferredCompensation(
      plan({ accounts: [account({ balance: 100_000, withdrawalRate: 1 })] }),
    )
    result.projections.forEach((point) => {
      expect(point.totalBalance).toBeGreaterThanOrEqual(0)
      Object.values(point.balances).forEach((balance) => expect(balance).toBeGreaterThanOrEqual(0))
    })
  })
})

describe('accounting identities', () => {
  const rich = () =>
    calculateDeferredCompensation(
      plan({
        accounts: [
          account({ id: 'defcomp', type: 'deferred', balance: 400_000, availableAge: 55, payoutYears: 10, withdrawalTaxRate: 0.25 }),
          account({ id: 'brokerage', balance: 800_000, annualReturn: 0.05, withdrawalRate: 1 }),
        ],
        incomeSources: [income({ id: 'ss', annualAmount: 30_000, startAge: 67, endAge: 200 })],
        additionalExpenses: [expense({ id: 'travel', annualAmount: 12_000, startAge: 55 })],
      }),
    )

  it('spendable income is outside income plus after-tax payouts and withdrawals', () => {
    rich().projections.forEach((point) => {
      const parts = point.outsideIncome + point.deferredIncome + point.portfolioWithdrawals
      expect(Math.abs(point.totalIncome - parts)).toBeLessThanOrEqual(2)
    })
  })

  it('surplus is income minus expenses', () => {
    rich().projections.forEach((point) => {
      expect(Math.abs(point.surplus - (point.totalIncome - point.expenses))).toBeLessThanOrEqual(2)
    })
  })

  it('every gross dollar withdrawn is either spendable or tax', () => {
    // The tax identity: sum(gross) === deferredIncome + portfolioWithdrawals + withdrawalTaxes.
    rich().projections.forEach((point) => {
      const gross = Object.values(point.withdrawals).reduce((sum, value) => sum + value, 0)
      const accounted = point.deferredIncome + point.portfolioWithdrawals + point.withdrawalTaxes
      expect(Math.abs(gross - accounted)).toBeLessThanOrEqual(3)
    })
  })

  it('reports the retirement-year figures as the headline', () => {
    const result = rich()
    const atRetirement = result.projections.find((p) => p.age === 55)!
    expect(result.balanceAtSemiRetirement).toBe(atRetirement.totalBalance)
    expect(result.firstYearIncome).toBe(atRetirement.totalIncome)
    expect(result.firstYearSurplus).toBe(atRetirement.surplus)
    expect(result.endingBalance).toBe(result.projections.at(-1)!.totalBalance)
  })
})

describe('funded-year bookkeeping', () => {
  it('counts every retirement year when the plan never falls short', () => {
    const result = calculateDeferredCompensation(
      plan({ incomeSources: [income({ annualAmount: 1_000_000, annualGrowth: 0.1 })] }),
    )
    expect(result.firstShortfallAge).toBeNull()
    expect(result.retirementYears).toBe(36)
    expect(result.fundedYears).toBe(36)
    expect(result.yearsFullyCovered).toBe(36)
  })

  it('stops the consecutive count at the first shortfall', () => {
    const result = calculateDeferredCompensation(
      plan({ accounts: [account({ balance: 200_000, annualReturn: 0, withdrawalRate: 1 })] }),
    )
    expect(result.firstShortfallAge).not.toBeNull()
    expect(result.fundedYears).toBe(result.projections.findIndex((p) => p.age === result.firstShortfallAge!) -
      result.projections.findIndex((p) => p.age === 55))
  })

  it('never counts consecutive funded years above total covered years', () => {
    // fundedYears stops at the first gap; yearsFullyCovered keeps counting afterwards, so the
    // former can never exceed the latter.
    for (const balance of [50_000, 200_000, 800_000, 5_000_000]) {
      const result = calculateDeferredCompensation(
        plan({ accounts: [account({ balance, annualReturn: 0.05, withdrawalRate: 1 })] }),
      )
      expect(result.fundedYears).toBeLessThanOrEqual(result.yearsFullyCovered)
      expect(result.yearsFullyCovered).toBeLessThanOrEqual(result.retirementYears)
    }
  })

  it('reports the first shortfall age exactly when one exists', () => {
    for (const balance of [50_000, 200_000, 800_000, 5_000_000]) {
      const result = calculateDeferredCompensation(
        plan({ accounts: [account({ balance, annualReturn: 0.05, withdrawalRate: 1 })] }),
      )
      const shortfall = result.projections.find((p) => p.age >= 55 && p.surplus < 0)
      expect(result.firstShortfallAge).toBe(shortfall?.age ?? null)
    }
  })

  it('a larger portfolio never funds fewer consecutive years', () => {
    const funded = [100_000, 500_000, 1_000_000, 3_000_000].map(
      (balance) =>
        calculateDeferredCompensation(
          plan({ accounts: [account({ balance, annualReturn: 0.05, withdrawalRate: 1 })] }),
        ).fundedYears,
    )
    for (let i = 1; i < funded.length; i++) expect(funded[i]).toBeGreaterThanOrEqual(funded[i - 1])
  })
})

describe('surplus reinvestment', () => {
  it('leaves the balance untouched when reinvestment is off', () => {
    const off = calculateDeferredCompensation(
      plan({ reinvestSurplus: false, incomeSources: [income({ annualAmount: 200_000 })] }),
    )
    const on = calculateDeferredCompensation(
      plan({ reinvestSurplus: true, incomeSources: [income({ annualAmount: 200_000 })] }),
    )
    expect(on.endingBalance).toBeGreaterThan(off.endingBalance)
  })

  it('spreads the surplus across accounts in proportion to their balances', () => {
    const result = calculateDeferredCompensation(
      plan({
        inflationRate: 0,
        reinvestSurplus: true,
        withdrawOnlyAfterRetirement: false,
        accounts: [
          account({ id: 'big', balance: 300_000, annualReturn: 0, withdrawalRate: 0 }),
          account({ id: 'small', balance: 100_000, annualReturn: 0, withdrawalRate: 0 }),
        ],
        incomeSources: [income({ annualAmount: 120_000 })],
      }),
    )
    // A 40,000 surplus splits 3:1, so 30,000 and 10,000.
    const first = result.projections[0]
    expect(first.surplus).toBe(40_000)
    expect(first.balances.big).toBe(330_000)
    expect(first.balances.small).toBe(110_000)
  })
})

describe('degenerate inputs', () => {
  it('handles a plan with no accounts at all', () => {
    const result = calculateDeferredCompensation(plan({ accounts: [] }))
    expect(result.currentBalance).toBe(0)
    expect(result.endingBalance).toBe(0)
    expect(result.projections.length).toBeGreaterThan(0)
    result.projections.forEach((point) => expect(Number.isFinite(point.surplus)).toBe(true))
  })

  it('produces at least one projection when every age is the same', () => {
    const result = calculateDeferredCompensation(
      plan({ currentAge: 60, semiRetirementAge: 60, planThroughAge: 60 }),
    )
    expect(result.projections).toHaveLength(1)
    expect(result.retirementYears).toBe(1)
  })

  it('does not produce NaN under a -100% return or worse', () => {
    const result = calculateDeferredCompensation(
      plan({ accounts: [account({ annualReturn: -5 })], inflationRate: -3 }),
    )
    result.projections.forEach((point) => {
      expect(Number.isNaN(point.totalBalance)).toBe(false)
      expect(Number.isNaN(point.expenses)).toBe(false)
      expect(Number.isFinite(point.surplus)).toBe(true)
    })
  })
})
