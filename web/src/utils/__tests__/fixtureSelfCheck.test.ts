import { describe, expect, it } from 'vitest'

import {
  amortize,
  deflate,
  inflatingSum,
  monthsToPayOffClosedForm,
  nominalEscalatingSeries,
  nominalFlatSeries,
  realBalanceClosedForm,
  realFixedPoint,
  realRate,
  roundHalfAwayFromZero,
  yearsToTargetClosedForm,
} from './oracles'
import {
  debtCases,
  decode,
  deferredCases,
  fireCases,
  healthcareCases,
  investmentCases,
  parityCases,
  withdrawalCases,
} from './parityFixtures'

/**
 * Oracle === fixture.
 *
 * This suite never touches `calculations.ts`. It re-derives the shared fixture's expectations a
 * third time, from the algebra in `oracles.ts`, so that a fixture value quietly regenerated from
 * either implementation fails here. Without this layer the fixture would only prove that web and
 * MAUI agree with each other — which is exactly the failure mode issue #54 describes, since two
 * implementations can agree on a number that is simply wrong.
 */

describe('fixture hygiene', () => {
  it('has unique ids', () => {
    const ids = parityCases.map((c) => c.id)
    expect(new Set(ids).size).toBe(ids.length)
  })

  it.each(parityCases.map((c) => [c.id, c] as const))(
    '%s documents how its expectations were derived',
    (_id, testCase) => {
      // A case without a real derivation is unreviewable: nobody can check it without running the
      // code, and "run the code and see" is the practice this fixture exists to prevent.
      expect(testCase.derivation.length).toBeGreaterThan(40)
      expect(testCase.description.length).toBeGreaterThan(10)
    },
  )
})

describe('fire cases match closed-form algebra', () => {
  it.each(fireCases.map((c) => [c.id, c] as const))(
    '%s fireNumber is expenses / withdrawalRate',
    (_id, testCase) => {
      const { annualExpenses, withdrawalRate } = testCase.inputs
      expect(testCase.expected.fireNumber).toBeCloseTo(annualExpenses / withdrawalRate, 6)
    },
  )

  it.each(fireCases.map((c) => [c.id, c] as const))(
    '%s yearsToFire matches n = ln((C + T*rho)/(C + PV*rho)) / ln(1+rho)',
    (_id, testCase) => {
      const { currentSavings, annualContribution, expectedReturn, inflationRate, contributionGrowth } =
        testCase.inputs
      const target = testCase.expected.fireNumber
      const years = decode(testCase.expected.yearsToFire)

      if (contributionGrowth === 'flat') {
        // Flat contributions have no closed form in today's dollars, so the oracle is a bisection
        // over an independently simulated nominal series instead.
        if (!Number.isFinite(years)) return
        const atYears = deflate(
          nominalFlatSeries(currentSavings, annualContribution, expectedReturn, Math.ceil(years))[
            Math.ceil(years)
          ],
          inflationRate,
          Math.ceil(years),
        )
        const before = Math.floor(years)
        const atBefore = deflate(
          nominalFlatSeries(currentSavings, annualContribution, expectedReturn, before)[before],
          inflationRate,
          before,
        )
        expect(atBefore).toBeLessThan(target)
        expect(atYears).toBeGreaterThanOrEqual(target)
        return
      }

      const rho = realRate(expectedReturn, inflationRate)
      const exact = yearsToTargetClosedForm(currentSavings, annualContribution, rho, target)

      if (exact === null) {
        expect(years).toBe(Number.POSITIVE_INFINITY)
        return
      }
      // The app rounds the headline to one decimal for display, so the fixture stores it rounded.
      // Asserting equality against round(exact, 1) is stricter than a tolerance: it pins the exact
      // decimal a user sees, and still fails if the underlying algebra moves by more than 0.05.
      expect(years).toBe(Math.round(exact * 10) / 10)
    },
  )

  it.each(fireCases.map((c) => [c.id, c] as const))('%s fireAge is currentAge + yearsToFire', (_id, testCase) => {
    const years = decode(testCase.expected.yearsToFire)
    const age = decode(testCase.expected.fireAge)
    if (!Number.isFinite(years)) {
      expect(age).toBe(Number.POSITIVE_INFINITY)
      return
    }
    // The app rounds the headline age to one decimal for display.
    expect(age).toBeCloseTo(Math.round((testCase.inputs.currentAge + years) * 10) / 10, 10)
  })

  it.each(fireCases.map((c) => [c.id, c] as const))(
    '%s projection samples match the deflated nominal recurrence',
    (_id, testCase) => {
      const { currentSavings, annualContribution, expectedReturn, inflationRate, contributionGrowth } =
        testCase.inputs

      for (const sample of testCase.expected.projectionSamples) {
        const n = sample.age - testCase.inputs.currentAge
        const nominal =
          contributionGrowth === 'flat'
            ? nominalFlatSeries(currentSavings, annualContribution, expectedReturn, n)[n]
            : nominalEscalatingSeries(currentSavings, annualContribution, expectedReturn, inflationRate, n)[n]

        expect(sample.portfolio).toBe(Math.round(nominal))
        expect(sample.inflationAdjusted).toBe(Math.round(deflate(nominal, inflationRate, n)))
      }
    },
  )

  it.each(
    fireCases
      .filter((c) => c.inputs.contributionGrowth === 'inflation')
      .map((c) => [c.id, c] as const),
  )(
    '%s deflated projection equals the closed form exactly, not approximately',
    (_id, testCase) => {
      // The audit's central identity. Because contributions are entered in today's dollars and
      // escalate as C(1+i)^k, deflating the nominal recurrence reproduces the closed form exactly.
      const { currentSavings, annualContribution, expectedReturn, inflationRate } = testCase.inputs
      for (const sample of testCase.expected.projectionSamples) {
        const n = sample.age - testCase.inputs.currentAge
        const closedForm = realBalanceClosedForm(
          currentSavings,
          annualContribution,
          expectedReturn,
          inflationRate,
          n,
        )
        expect(sample.inflationAdjusted).toBe(Math.round(closedForm))
      }
    },
  )

  it.each(fireCases.map((c) => [c.id, c] as const))(
    '%s projection brackets the FIRE target at the headline age',
    (_id, testCase) => {
      // Issue #46: the chart crossing and the headline number disagreeing.
      const years = decode(testCase.expected.yearsToFire)
      if (!Number.isFinite(years) || years <= 0 || Number.isInteger(years)) return
      const target = testCase.expected.fireNumber
      const samples = testCase.expected.projectionSamples
      const below = samples.find((s) => s.age - testCase.inputs.currentAge === Math.floor(years))
      const above = samples.find((s) => s.age - testCase.inputs.currentAge === Math.ceil(years))
      if (!below || !above) return
      expect(below.inflationAdjusted).toBeLessThan(target)
      expect(above.inflationAdjusted).toBeGreaterThanOrEqual(target)
    },
  )

  it('the unreachable case is bounded by its fixed point, so Infinity is correct', () => {
    const testCase = fireCases.find((c) => c.id === 'fire-unreachable-negative-real-return')
    expect(testCase).toBeDefined()
    const { annualContribution, expectedReturn, inflationRate } = testCase!.inputs
    const rho = realRate(expectedReturn, inflationRate)
    expect(rho).toBeLessThan(0)
    const ceiling = realFixedPoint(annualContribution, rho)
    expect(ceiling).toBeCloseTo(700000, 6)
    expect(ceiling).toBeLessThan(testCase!.expected.fireNumber)
    expect(decode(testCase!.expected.fireAge)).toBe(Number.POSITIVE_INFINITY)
  })

  it('the degenerate rho = 0 case is the exact linear solution', () => {
    const testCase = fireCases.find((c) => c.id === 'fire-degenerate-zero-real-return')
    expect(testCase).toBeDefined()
    const { currentSavings, annualContribution, expectedReturn, inflationRate, currentAge } =
      testCase!.inputs
    expect(realRate(expectedReturn, inflationRate)).toBe(0)
    // 50,000 + 20,000n = 1,250,000  =>  n = 60 exactly.
    const n = (testCase!.expected.fireNumber - currentSavings) / annualContribution
    expect(n).toBe(60)
    expect(decode(testCase!.expected.fireAge)).toBe(currentAge + 60)
  })
})

describe('debt cases match the amortization closed form', () => {
  it.each(debtCases.map((c) => [c.id, c] as const))(
    '%s first-month interest is balance * rate / 12',
    (_id, testCase) => {
      const expected = testCase.inputs.debts.reduce((sum, d) => sum + (d.balance * d.rate) / 12, 0)
      expect(testCase.expected.firstMonthInterest).toBeCloseTo(expected, 10)
    },
  )

  it('the single-debt case matches n = -ln(1 - P*m/A)/ln(1+m)', () => {
    const testCase = debtCases.find((c) => c.id === 'debt-single-20pct-apr')
    expect(testCase).toBeDefined()
    const debt = testCase!.inputs.debts[0]
    const payment = testCase!.inputs.monthlyPayment + testCase!.inputs.extraPayment

    const exact = monthsToPayOffClosedForm(debt.balance, debt.rate, payment)
    expect(exact).not.toBeNull()
    // 24.53 months of payments means the 25th month still has a balance to clear.
    expect(Math.ceil(exact!)).toBe(testCase!.expected.totalMonths)

    const simulated = amortize(debt.balance, debt.rate, payment)
    expect(simulated.months).toBe(25)
    expect(Math.round(simulated.totalInterest)).toBe(testCase!.expected.totalInterest)
    // Exactly one accrual per month: 10000 * 0.20/12. The #45 defect charged it more than once.
    expect(simulated.firstMonthInterest).toBeCloseTo(166.66666666666666, 10)
  })

  it.each(debtCases.map((c) => [c.id, c] as const))(
    '%s principal repaid equals the sum of balances',
    (_id, testCase) => {
      const owed = testCase.inputs.debts.reduce((sum, d) => sum + d.balance, 0)
      expect(testCase.expected.totalPrincipal).toBe(owed)
    },
  )

  it('avalanche never costs more interest than snowball', () => {
    // The pre-fix bug inverted this. Ordering by rate is optimal for total interest by an exchange
    // argument, so the reverse can never hold.
    const snowball = debtCases.find((c) => c.id === 'debt-multi-snowball')
    const avalanche = debtCases.find((c) => c.id === 'debt-multi-avalanche')
    expect(snowball).toBeDefined()
    expect(avalanche).toBeDefined()
    expect(avalanche!.expected.totalInterest).toBeLessThanOrEqual(snowball!.expected.totalInterest)
  })
})

describe('withdrawal cases match the drawdown recurrence', () => {
  it.each(withdrawalCases.map((c) => [c.id, c] as const))(
    '%s annual withdrawal is portfolio * rate',
    (_id, testCase) => {
      const { portfolioValue, withdrawalRate } = testCase.inputs
      expect(testCase.expected.annualWithdrawal).toBe(Math.round(portfolioValue * withdrawalRate))
      expect(testCase.expected.monthlyWithdrawal).toBe(Math.round((portfolioValue * withdrawalRate) / 12))
    },
  )

  it.each(withdrawalCases.map((c) => [c.id, c] as const))(
    '%s rate analysis years are non-increasing as the withdrawal rate rises',
    (_id, testCase) => {
      const rows = testCase.expected.rateAnalysis
      for (let i = 1; i < rows.length; i++) {
        expect(rows[i].rate).toBeGreaterThan(rows[i - 1].rate)
        expect(rows[i].years).toBeLessThanOrEqual(rows[i - 1].years)
      }
    },
  )
})

describe('investment cases match the growth closed form', () => {
  it.each(investmentCases.map((c) => [c.id, c] as const))(
    '%s final balances match an independently simulated series',
    (_id, testCase) => {
      const {
        startingAmount,
        expectedReturn,
        inflationRate,
        yearsInvesting,
        contributionGrowth,
      } = testCase.inputs
      const annual = testCase.expected.annualContribution

      const nominal =
        contributionGrowth === 'flat'
          ? nominalFlatSeries(startingAmount, annual, expectedReturn, yearsInvesting)[yearsInvesting]
          : nominalEscalatingSeries(
              startingAmount,
              annual,
              expectedReturn,
              inflationRate,
              yearsInvesting,
            )[yearsInvesting]

      expect(testCase.expected.finalNominalBalance).toBeCloseTo(nominal, 6)
      expect(testCase.expected.finalInflationAdjustedBalance).toBeCloseTo(
        deflate(nominal, inflationRate, yearsInvesting),
        6,
      )
    },
  )

  it.each(investmentCases.map((c) => [c.id, c] as const))(
    '%s satisfies growth = final - invested and impact = nominal - real',
    (_id, testCase) => {
      const e = testCase.expected
      expect(e.totalGrowth).toBeCloseTo(e.finalNominalBalance - e.totalInvested, 6)
      expect(e.inflationImpact).toBeCloseTo(e.finalNominalBalance - e.finalInflationAdjustedBalance, 6)
    },
  )

  it('the shipped-defaults case reproduces the audit anchor', () => {
    // PV = 100,000, C = 6,000/yr (500/mo), r = 7%, i = 3%, n = 30. Derived from the recurrence
    // B_k = B_{k-1}(1.07) + 6000(1.03)^k, then deflated by 1.03^30.
    const testCase = investmentCases.find((c) => c.id === 'investment-defaults')
    expect(testCase).toBeDefined()
    expect(testCase!.expected.finalNominalBalance).toBeCloseTo(1562306.8565586861, 4)
    expect(testCase!.expected.finalInflationAdjustedBalance).toBeCloseTo(643649.7392030951, 4)
  })
})

describe('healthcare cases match the geometric sum', () => {
  it.each(healthcareCases.map((c) => [c.id, c] as const))(
    '%s total cost is A((1+i)^n - 1)/i',
    (_id, testCase) => {
      const { monthlyPremium, annualDeductible, annualOutOfPocket, inflationRate } = testCase.inputs
      const annual = monthlyPremium * 12 + annualDeductible + annualOutOfPocket
      expect(testCase.expected.annualCost).toBe(annual)
      expect(testCase.expected.totalCost).toBe(
        Math.round(inflatingSum(annual, inflationRate, testCase.expected.gapYears)),
      )
    },
  )

  it.each(healthcareCases.map((c) => [c.id, c] as const))(
    '%s gap runs from early retirement to Medicare at 65',
    (_id, testCase) => {
      expect(testCase.expected.gapYears).toBe(Math.max(0, 65 - testCase.inputs.earlyRetirementAge))
    },
  )
})

/**
 * The deferred cases come in two constructions, and each gets its own oracle below.
 *
 * The issue #63 cases are account-less single-pension plans, where the thing that most needs
 * independent checking is the surplus and the funded/shortfall verdict derived from it — the two
 * values that disagreed across platforms. The issue #56 cases have accounts and no income at all,
 * because what they pin is how far a withdrawal may exceed the stated rate policy.
 *
 * Both are deliberately flat plans, so the arithmetic is exact and can be done by hand. Each family's
 * first test asserts that the case really is the construction its `derivation` claims rather than
 * trusting the prose; a case that quietly grew an account or a non-zero inflation rate would make the
 * closed form wrong while still looking plausible.
 */
const pensionCases = deferredCases.filter((c) => c.inputs.accounts.length === 0)
const capFlexCases = deferredCases.filter((c) => c.inputs.accounts.length > 0)

describe('deferred cases match hand arithmetic', () => {
  /** Half the whole-dollar display unit. Stated here, not imported, so a change to the shipped
   *  constant has to be made deliberately in two places. */
  const SHORTFALL_TOLERANCE = 0.5

  it('every deferred case belongs to exactly one construction, and both are populated', () => {
    // The split is on account count, so it is total by construction; these assertions catch the
    // real hazard, which is a whole family silently emptying and its oracle then vacuously passing.
    expect(pensionCases.length + capFlexCases.length).toBe(deferredCases.length)
    expect(pensionCases.length).toBeGreaterThan(0)
    expect(capFlexCases.length).toBeGreaterThan(0)
  })

  it.each(pensionCases.map((c) => [c.id, c] as const))(
    '%s really is the flat single-pension construction its derivation claims',
    (_id, testCase) => {
      const i = testCase.inputs
      expect(i.inflationRate).toBe(0)
      expect(i.accounts).toEqual([])
      expect(i.additionalExpenses).toEqual([])
      expect(i.reinvestSurplus).toBe(false)
      expect(i.incomeSources).toHaveLength(1)

      const [source] = i.incomeSources
      expect(source.isAfterTax).toBe(true)
      expect(source.annualGrowth).toBe(0)
      // Active across every projected year, so its contribution is the same in each of them.
      expect(source.startAge).toBeLessThanOrEqual(i.currentAge)
      expect(source.endAge).toBeGreaterThanOrEqual(i.planThroughAge)
    },
  )

  it.each(pensionCases.map((c) => [c.id, c] as const))(
    '%s surplus is income minus expenses, rounded away from zero',
    (_id, testCase) => {
      const i = testCase.inputs
      // Zero inflation and zero growth make every year identical: income is the pension's nominal
      // amount and expenses are the entered figure, both exactly.
      const income = i.incomeSources[0].annualAmount
      const exactSurplus = income - i.annualExpenses

      // Textbook away-from-zero rounding produces NEGATIVE zero for a gap inside half a dollar.
      // Both platforms deliberately collapse that to positive zero, because -0 formats as "-$0"
      // through both Intl.NumberFormat and C# ToString("C0") while still satisfying `>= 0` — the
      // display half of issue #63. That normalization is modelled here rather than folded into the
      // oracle, so it stays a visible step of the convention instead of an invisible one.
      const rounded = roundHalfAwayFromZero(exactSurplus)
      const displayed = Object.is(rounded, -0) ? 0 : rounded

      expect(testCase.expected.firstYearSurplus).toBe(displayed)
      for (const sample of testCase.expected.annualSamples) {
        expect(sample.totalIncome).toBe(roundHalfAwayFromZero(income))
        expect(sample.expenses).toBe(roundHalfAwayFromZero(i.annualExpenses))
        expect(sample.surplus).toBe(displayed)
      }

      // Negative zero would satisfy every assertion above under `>=` comparisons and would format as
      // "-$0". `toBe` uses Object.is, so this states outright what the fixture must not contain.
      expect(Object.is(testCase.expected.firstYearSurplus, -0)).toBe(false)
    },
  )

  it.each(pensionCases.map((c) => [c.id, c] as const))(
    '%s verdict follows from the unrounded surplus, not the displayed one',
    (_id, testCase) => {
      const i = testCase.inputs
      const exactSurplus = i.incomeSources[0].annualAmount - i.annualExpenses
      const short = exactSurplus <= -SHORTFALL_TOLERANCE
      // Retirement age equals current age in these cases, so every projected year is a retirement
      // year, and every year has the same surplus.
      const years = i.planThroughAge - i.semiRetirementAge + 1

      expect(testCase.expected.retirementYears).toBe(years)
      expect(testCase.expected.projectionCount).toBe(years)
      expect(testCase.expected.fundedYears).toBe(short ? 0 : years)
      expect(testCase.expected.yearsFullyCovered).toBe(short ? 0 : years)
      expect(testCase.expected.firstShortfallAge).toBe(short ? i.semiRetirementAge : null)
    },
  )
})

/**
 * Independent oracle for the issue #56 cap-flex cases.
 *
 * It deliberately does **not** replay the engine's two passes. Re-walking "cap first, then flex the
 * remainder" in a second language would only prove the fixture was transcribed from the same
 * procedure, which is the failure mode `shared/parity/README.md` exists to prevent. Instead it
 * re-derives each year from the *policy statement* the change is supposed to implement:
 *
 *   1. A year is affordable when the spendable capacity of the reachable accounts covers the need.
 *   2. An affordable year takes exactly the need, net — no more, so surplus is 0.
 *   3. Gross withdrawals prorate by balance, so the gross total is the net need scaled by
 *      `reachableBalance / netCapacity`. This is the assertion that pins the ordering policy: a
 *      taxable-first rule satisfies (1) and (2) but produces a different gross total whenever the
 *      reachable accounts are taxed differently.
 *   4. An unaffordable year takes everything reachable and falls short by the remainder.
 *
 * Those four statements are checkable against the prose in each `derivation` without running
 * anything, which is the point.
 */
describe('cap-flex cases match the stated withdrawal policy', () => {
  const reachableAt = (testCase: (typeof capFlexCases)[number], age: number) =>
    testCase.inputs.accounts.filter((account) => age >= account.availableAge)

  it.each(capFlexCases.map((c) => [c.id, c] as const))(
    '%s really is the flat account-only construction its derivation claims',
    (_id, testCase) => {
      const i = testCase.inputs
      expect(i.inflationRate).toBe(0)
      expect(i.incomeSources).toEqual([])
      expect(i.additionalExpenses).toEqual([])
      expect(i.reinvestSurplus).toBe(false)
      // Retirement starts today, so every projected year is a retirement year and withdrawals are
      // permitted in all of them.
      expect(i.semiRetirementAge).toBe(i.currentAge)
      expect(i.accounts.length).toBeGreaterThan(0)

      for (const account of i.accounts) {
        expect(account.annualReturn).toBe(0)
        expect(account.annualContribution).toBe(0)
        // No deferred account: those follow a payout schedule gap withdrawals never touch, which
        // would invalidate the closed form below.
        expect(account.type).not.toBe('deferred')
      }

      // The closed form treats the gross total as purely balance-proportional, which is only exact
      // when the capped first pass cannot skew the per-account split: either there is one account,
      // or no account releases anything under its own rate. Asserting the precondition is also what
      // stops a reader assuming the shipped 4% default in the multi-account case and concluding the
      // fixture is wrong — the rates really are 0 there, on purpose.
      expect(i.accounts.length === 1 || i.accounts.every((a) => a.withdrawalRate === 0)).toBe(true)
    },
  )

  it.each(capFlexCases.map((c) => [c.id, c] as const))(
    '%s spends to the need, prorates gross by balance, and stops at the money that exists',
    (_id, testCase) => {
      const i = testCase.inputs
      const need = i.annualExpenses
      const balances = new Map(i.accounts.map((a) => [a.id, a.balance]))

      for (let age = i.currentAge; age <= i.planThroughAge; age++) {
        const reachable = reachableAt(testCase, age)
        const reachableBalance = reachable.reduce((sum, a) => sum + balances.get(a.id)!, 0)
        // Spendable capacity: a dollar in a 25%-taxed account only buys 75 cents of expenses.
        const netCapacity = reachable.reduce(
          (sum, a) => sum + balances.get(a.id)! * (1 - a.withdrawalTaxRate),
          0,
        )
        const affordable = need <= netCapacity
        const netTaken = affordable ? need : netCapacity
        const grossTaken = affordable ? need * (reachableBalance / netCapacity) : reachableBalance

        const sample = testCase.expected.annualSamples.find((s) => s.age === age)
        expect(sample).toBeDefined()
        expect(sample!.totalIncome).toBe(roundHalfAwayFromZero(netTaken))
        expect(sample!.expenses).toBe(roundHalfAwayFromZero(need))
        expect(sample!.surplus).toBe(affordable ? 0 : roundHalfAwayFromZero(netTaken - need))
        expect(Object.is(sample!.surplus, -0)).toBe(false)

        // Excess over policy. The stated rates release `balance * rate` gross, worth
        // `balance * rate * netFactor` of spending; if that already covers the need the policy is
        // never exceeded, and otherwise every capped dollar is used and the rest is the excess.
        const policyGross = reachable.reduce((sum, a) => sum + balances.get(a.id)! * a.withdrawalRate, 0)
        const policyNet = reachable.reduce(
          (sum, a) => sum + balances.get(a.id)! * a.withdrawalRate * (1 - a.withdrawalTaxRate),
          0,
        )
        expect(sample!.policyExcessWithdrawals).toBe(
          need <= policyNet ? 0 : roundHalfAwayFromZero(grossTaken - policyGross),
        )

        // Zero return and zero contribution, so next year's balance is this year's less what left.
        for (const account of reachable) {
          const share = reachableBalance > 0 ? balances.get(account.id)! / reachableBalance : 0
          balances.set(account.id, balances.get(account.id)! - grossTaken * share)
        }

        const totalBalance = i.accounts.reduce((sum, a) => sum + balances.get(a.id)!, 0)
        if (age === i.semiRetirementAge) {
          expect(testCase.expected.balanceAtSemiRetirement).toBe(roundHalfAwayFromZero(totalBalance))
          expect(testCase.expected.firstYearIncome).toBe(roundHalfAwayFromZero(netTaken))
          expect(testCase.expected.firstYearSurplus).toBe(
            affordable ? 0 : roundHalfAwayFromZero(netTaken - need),
          )
        }
        if (age === i.planThroughAge) {
          expect(testCase.expected.endingBalance).toBe(roundHalfAwayFromZero(totalBalance))
        }
      }

      const years = i.planThroughAge - i.semiRetirementAge + 1
      expect(testCase.expected.projectionCount).toBe(years)
      expect(testCase.expected.retirementYears).toBe(years)
      expect(testCase.expected.currentBalance).toBe(
        roundHalfAwayFromZero(i.accounts.reduce((sum, a) => sum + a.balance, 0)),
      )
    },
  )
})
