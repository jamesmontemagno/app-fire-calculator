import { describe, expect, it } from 'vitest'

import {
  calculateBaristaFIRE,
  calculateCoastFIRE,
  calculateFatFIRE,
  calculateInvestmentGrowth,
  calculateLeanFIRE,
  calculateReverseFIRE,
  calculateSnowballPayoff,
  calculateStandardFIRE,
  calculateWithdrawal,
} from '../calculations'
import type { FIREInputs } from '../calculations'
import { prepareInputsForExport, prepareResultsForExport } from '../excelExport'

/**
 * `prepareResultsForExport` enumerates every scalar on a result object and picks percent/currency
 * formatting by substring match on the key name. That design means **any new scalar field on a
 * result type silently appears in the user's spreadsheet**, formatted by whatever its name happens
 * to contain.
 *
 * It bit the cross-platform audit three separate times: a new boolean leaked a row into the export,
 * `'rate'` matched while `'ratio'` did not so percent formatting was silently dropped, and a null
 * field emitted a blank row.
 *
 * The key-set guards below are the point of this file. They fail when a result type gains or loses
 * an exported field, forcing a conscious decision about the workbook rather than letting it change
 * by accident.
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

describe('exported key sets', () => {
  // If one of these fails, a result type gained or lost a scalar. Decide whether it belongs in the
  // user's workbook and update the list deliberately — do not just paste the new set in.
  it.each([
    [
      'StandardFIRE',
      () => calculateStandardFIRE(DEFAULTS),
      ['fireNumber', 'yearsToFIRE', 'fireAge', 'savingsRate', 'monthlyContribution', 'coastFireNumber'],
    ],
    [
      'LeanFIRE',
      () => calculateLeanFIRE(DEFAULTS),
      ['fireNumber', 'yearsToFIRE', 'fireAge', 'savingsRate', 'monthlyContribution', 'coastFireNumber', 'isLean', 'leanThreshold'],
    ],
    [
      'FatFIRE',
      () => calculateFatFIRE(DEFAULTS),
      ['fireNumber', 'yearsToFIRE', 'fireAge', 'savingsRate', 'monthlyContribution', 'coastFireNumber', 'isFat', 'fatThreshold'],
    ],
    [
      'CoastFIRE',
      () => calculateCoastFIRE(30, 55, 100_000, 24_000, 0.07, 0.03, 48_000, 0.04),
      ['coastNumber', 'yearsToCoast', 'alreadyCoasting', 'fireNumber'],
    ],
    [
      'BaristaFIRE',
      () => calculateBaristaFIRE(30, 100_000, 24_000, 0.07, 0.03, 48_000, 0.04, 20_000),
      ['baristaNumber', 'fullFireNumber', 'yearsToBaristaFIRE', 'partTimeIncomeNeeded', 'savingsFromPartTime'],
    ],
    [
      'Withdrawal',
      () => calculateWithdrawal(1_000_000, 0.04, 0.07, 0.03, 30),
      ['portfolioLongevity', 'horizonFundedRatio', 'annualWithdrawal', 'monthlyWithdrawal', 'endingBalance'],
    ],
    [
      'ReverseFIRE',
      () => calculateReverseFIRE(30, 55, 100_000, 48_000, 0.07, 0.03, 0.04),
      ['fireNumber', 'yearsToFIRE', 'requiredAnnualSavings', 'requiredMonthlySavings', 'alreadyAchievable', 'currentWillGrowTo'],
    ],
    [
      'InvestmentGrowth',
      () => calculateInvestmentGrowth(100_000, 500, 'monthly', 30, 0.07, 0.03, 72_000, 30),
      ['savingsRate', 'annualContribution', 'monthlyContribution', 'finalNominalBalance', 'finalInflationAdjustedBalance', 'totalInvested', 'totalGrowth', 'inflationImpact'],
    ],
    [
      'DebtPayoff',
      () => calculateSnowballPayoff([{ id: '1', name: 'Card', balance: 10_000, rate: 0.2, minPayment: 200 }], 500),
      ['totalMonths', 'totalInterest', 'totalPrincipal', 'monthlyPayment'],
    ],
  ] as const)('%s exports exactly its known scalar fields', (_label, build, expected) => {
    const { values } = prepareResultsForExport(build())
    expect(Object.keys(values).sort()).toEqual([...expected].sort())
  })
})

describe('structural filtering', () => {
  it('omits arrays, which belong on their own sheets', () => {
    const { values } = prepareResultsForExport(calculateStandardFIRE(DEFAULTS))
    expect(values).not.toHaveProperty('projections')
  })

  it('omits nested objects', () => {
    // StandardFIRE carries a `retirementGoal` object that must not be flattened into rows.
    const { values } = prepareResultsForExport(calculateStandardFIRE(DEFAULTS))
    expect(values).not.toHaveProperty('retirementGoal')
  })

  it('omits an empty array as readily as a populated one', () => {
    const { values } = prepareResultsForExport({ rows: [], total: 5 })
    expect(Object.keys(values)).toEqual(['total'])
  })
})

describe('non-finite values', () => {
  it('replaces Infinity with wording rather than writing "$∞" into a cell', () => {
    const unreachable = calculateStandardFIRE({
      ...DEFAULTS,
      expectedReturn: 0.02,
      inflationRate: 0.05,
      annualContribution: 20_000,
      annualExpenses: 50_000,
    })
    expect(unreachable.fireAge).toBe(Infinity)

    const { values, formats } = prepareResultsForExport(unreachable)
    expect(values.fireAge).toBe('Not reachable')
    expect(values.yearsToFIRE).toBe('Not reachable')
    // No numeric format is attached, so nothing tries to render the text as currency or years.
    expect(formats).not.toHaveProperty('fireAge')
    expect(formats).not.toHaveProperty('yearsToFIRE')
  })

  it('handles -Infinity and NaN the same way', () => {
    const { values } = prepareResultsForExport({ a: -Infinity, b: NaN, c: 42 })
    expect(values.a).toBe('Not reachable')
    expect(values.b).toBe('Not reachable')
    expect(values.c).toBe(42)
  })

  it('leaves finite values, including zero and negatives, as numbers', () => {
    const { values } = prepareResultsForExport({ zero: 0, negative: -1_500 })
    expect(values.zero).toBe(0)
    expect(values.negative).toBe(-1_500)
  })
})

describe('format inference by key name', () => {
  it.each([
    ['withdrawalRate', 'percent'],
    ['savingsRate', 'percent'],
    ['horizonFundedRatio', 'percent'],
    ['percentComplete', 'percent'],
    ['progress', 'percent'],
  ] as const)('%s is formatted as a percentage', (key, expected) => {
    expect(prepareResultsForExport({ [key]: 0.04 }).formats[key]).toBe(expected)
  })

  it('formats horizonFundedRatio as a percentage', () => {
    // The audit's second bite: 'rate' matched but 'ratio' did not, so this ratio silently exported
    // as a bare number. 'ratio' is now in the percent list and must stay there.
    const { formats } = prepareResultsForExport(calculateWithdrawal(1_000_000, 0.05, 0.03, 0.03, 40))
    expect(formats.horizonFundedRatio).toBe('percent')
  })

  it.each([
    ['fireNumber', 'currency'],
    ['endingBalance', 'currency'],
    ['annualWithdrawal', 'currency'],
    ['totalInterest', 'currency'],
    ['monthlyPayment', 'currency'],
    ['requiredAnnualSavings', 'currency'],
    ['totalCost', 'currency'],
    ['totalPrincipal', 'currency'],
    ['portfolioValue', 'currency'],
  ] as const)('%s is formatted as currency', (key, expected) => {
    expect(prepareResultsForExport({ [key]: 1_000 }).formats[key]).toBe(expected)
  })

  it.each([
    ['yearsToFIRE', 'years'],
    ['fireAge', 'years'],
    ['portfolioLongevity', 'years'],
    ['monthsRemaining', 'years'],
  ] as const)('%s is formatted as a duration', (key, expected) => {
    expect(prepareResultsForExport({ [key]: 25 }).formats[key]).toBe(expected)
  })

  it('falls back to plain number for an unrecognised key', () => {
    expect(prepareResultsForExport({ mysteryField: 7 }).formats.mysteryField).toBe('number')
  })

  it('checks percent before currency, so a key matching both is a percentage', () => {
    // 'savingsRate' contains both 'savings' (currency) and 'rate' (percent). Percent wins because
    // it is tested first, which is the behaviour the result cards depend on.
    expect(prepareResultsForExport({ savingsRate: 0.33 }).formats.savingsRate).toBe('percent')
  })

  it('matches key names case-insensitively', () => {
    expect(prepareResultsForExport({ TotalInterest: 500 }).formats.TotalInterest).toBe('currency')
    expect(prepareResultsForExport({ WITHDRAWALRATE: 0.04 }).formats.WITHDRAWALRATE).toBe('percent')
  })

  it('attaches no format to non-numeric values', () => {
    const { values, formats } = prepareResultsForExport({ label: 'Standard', flag: true })
    expect(values.label).toBe('Standard')
    expect(values.flag).toBe(true)
    expect(formats).not.toHaveProperty('label')
    expect(formats).not.toHaveProperty('flag')
  })
})

describe('known hazards, pinned as characterization', () => {
  /*
   * The behaviours below are pre-existing and are NOT changed by this test-only work. They are
   * pinned so the next person sees them stated rather than rediscovering them from a user's
   * spreadsheet. Booleans and null are tracked in issue #64; totalMonths has its own issue, #62.
   */

  it('booleans still reach the workbook as untyped rows', () => {
    // `typeof true` is neither array nor object, so a boolean passes the filter and lands in the
    // export with no format hint. LeanFIRE, FatFIRE and CoastFIRE all pass raw results containing
    // isLean / isFat / alreadyCoasting straight through.
    const lean = prepareResultsForExport(calculateLeanFIRE(DEFAULTS))
    expect(lean.values.isLean).toBe(false)
    expect(lean.formats).not.toHaveProperty('isLean')

    const coast = prepareResultsForExport(
      calculateCoastFIRE(30, 55, 100_000, 24_000, 0.07, 0.03, 48_000, 0.04),
    )
    expect(coast.values.alreadyCoasting).toBe(false)
    expect(coast.formats).not.toHaveProperty('alreadyCoasting')
  })

  it('null still emits a row', () => {
    // The guard is `typeof value === 'object' && value !== null`, so null falls through to
    // `values[key] = null` and produces a blank row. Callers work around this at the call site
    // (DeferredCompensation.tsx uses `?? 0`) rather than in this helper.
    const { values, formats } = prepareResultsForExport({ firstShortfallAge: null, fireNumber: 1_200_000 })
    expect(values).toHaveProperty('firstShortfallAge')
    expect(values.firstShortfallAge).toBeNull()
    expect(formats).not.toHaveProperty('firstShortfallAge')
  })

  it('undefined is dropped, unlike null', () => {
    // Object.entries skips nothing, but `typeof undefined` is 'undefined', so it is stored as a
    // value with no format. Pinned to document the asymmetry with null above.
    const { values } = prepareResultsForExport({ maybe: undefined, sure: 1 })
    expect(values.maybe).toBeUndefined()
    expect(values.sure).toBe(1)
  })

  it('a threshold constant is exported as if it were a result', () => {
    // leanThreshold / fatThreshold are configuration, not outcomes, but they are scalars on the
    // result type so they reach the user's workbook. Documented, not changed.
    const { values, formats } = prepareResultsForExport(calculateLeanFIRE(DEFAULTS))
    expect(values.leanThreshold).toBe(40_000)
    expect(formats.leanThreshold).toBe('number')
  })

  it('totalMonths is exported as CURRENCY, not a duration', () => {
    /*
     * FINDING (live, user-visible, pre-existing). Tracked in issue #62, deliberately not fixed here.
     *
     * This is the same substring-collision class as the 'rate' / 'ratio' bug the audit already hit,
     * and it is currently shipping.
     *
     * 'totalmonths' contains 'total', which is in currencyKeys. currencyKeys is tested BEFORE
     * timeKeys, so the intended 'months' match in timeKeys is never reached. The 'currency' hint
     * maps to numFmt '$#,##0' (getExcelFormat), so a debt payoff that takes 25 months is written
     * into the user's spreadsheet as "$25".
     *
     * DebtPayoff.tsx passes the raw result to prepareResultsForExport and forwards the derived
     * formats to the workbook without overriding them, so nothing downstream corrects this.
     *
     * Asserted as the WRONG value on purpose. When this is fixed, this test SHOULD fail — that is
     * the signal, and the expectation below should flip to 'years'.
     */
    const debt = calculateSnowballPayoff([{ id: '1', name: 'Card', balance: 10_000, rate: 0.2, minPayment: 200 }], 500)
    expect(debt.totalMonths).toBe(25)

    const { formats } = prepareResultsForExport(debt)
    expect(formats.totalMonths).toBe('currency')

    // The genuinely-currency siblings are unaffected and correct.
    expect(formats.totalInterest).toBe('currency')
    expect(formats.totalPrincipal).toBe('currency')
    expect(formats.monthlyPayment).toBe('currency')
  })
})

describe('empty and degenerate input', () => {
  it('returns empty maps for an empty result object', () => {
    const { values, formats } = prepareResultsForExport({})
    expect(values).toEqual({})
    expect(formats).toEqual({})
  })

  it('does not invent rows for a result made only of arrays and objects', () => {
    const { values } = prepareResultsForExport({ rows: [1, 2], nested: { a: 1 } })
    expect(values).toEqual({})
  })
})

/**
 * `prepareInputsForExport` is the sibling of `prepareResultsForExport` and has the same
 * substring-matching design, with two differences that both produce wrong formatting. Tracked in
 * issue #64. Not fixed here; this PR is test-only.
 *
 * All of these assert the CURRENT, WRONG behaviour on purpose. When #64 is fixed they SHOULD fail.
 */
describe('prepareInputsForExport hazards, pinned as characterization (issue #64)', () => {
  // The exact input DebtPayoff.tsx:71 passes, so these are the formats a real user's workbook gets.
  const debtInputs = () =>
    prepareInputsForExport({
      strategy: 'snowball',
      mode: 'budget',
      monthlyBudget: 500,
      targetMonths: 24,
      extraPayment: 0,
      totalDebts: 2,
      totalDebt: 21_500,
    })

  it('exports totalDebt without currency formatting', () => {
    // The exact inverse of the totalMonths bug in #62. There, a month count is formatted as money
    // because 'total' is in the results currencyKeys. Here, the inputs currencyKeys list
    // ('savings', 'contribution', 'expenses', 'income', 'value', 'budget', 'payment', 'premium',
    // 'deductible', 'pocket') has no 'total' entry at all, so totalDebt — a genuine dollar amount —
    // matches nothing and falls through to 'number'. Same root cause, opposite direction.
    expect(debtInputs().formats.totalDebt).toBe('number')

    // monthlyBudget is formatted correctly, via 'budget'. The list is not broken, just incomplete.
    expect(debtInputs().formats.monthlyBudget).toBe('currency')
  })

  it('assigns a numeric format to non-numeric values', () => {
    // Unlike prepareResultsForExport, the format branch here is not gated on
    // `typeof value === 'number'`, so text values receive a numeric format too.
    //
    // CORRECTION to issue #64, which records `strategy` as getting 'number': it actually gets
    // 'percent', because 'strategy' contains the substring 'rate' (st-RATE-gy) and percentKeys is
    // tested before currencyKeys. So the string "snowball" is written into a cell carrying numFmt
    // '0.0%'. 'mode' does get 'number' as filed.
    const { values, formats } = debtInputs()

    expect(values.strategy).toBe('snowball')
    expect(formats.strategy).toBe('percent')

    expect(values.mode).toBe('budget')
    expect(formats.mode).toBe('number')
  })

  it('has no non-finite guard, unlike its results sibling', () => {
    // prepareResultsForExport substitutes 'Not reachable' for Infinity and NaN. This one has no such
    // guard, so a non-finite input value is written straight through. Inputs are user-entered so
    // this is harder to reach than the results path, but the asymmetry is worth stating.
    const { values } = prepareInputsForExport({ annualIncome: Infinity })
    expect(values.annualIncome).toBe(Infinity)
  })

  it('checks ageKeys first, so any key containing "age" is formatted as an age', () => {
    // Latent rather than live: every current parameter containing 'age' is a genuine age
    // (currentAge, retirementAge, targetRetirementAge, earlyRetirementAge, medicareAge,
    // planThroughAge), so nothing is mis-formatted today. But 'age' is checked before percent and
    // currency, so a future 'mortgageBalance' or 'averageReturn' would silently export with the
    // age format. Pinned to document the ordering trap before it bites.
    const { formats } = prepareInputsForExport({ mortgageBalance: 300_000, averageReturn: 0.07 })
    expect(formats.mortgageBalance).toBe('age')
    expect(formats.averageReturn).toBe('age')

    // A real age is still correct, which is why the collision is easy to miss.
    expect(prepareInputsForExport({ currentAge: 30 }).formats.currentAge).toBe('age')
  })

  it('skips arrays and objects, matching its results sibling', () => {
    const { values } = prepareInputsForExport({ debts: [{ balance: 1 }], nested: {}, budget: 500 })
    expect(Object.keys(values)).toEqual(['budget'])
  })
})
