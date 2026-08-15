export type RetirementAccountType =
  | 'deferred'
  | 'traditional'
  | 'roth'
  | 'taxable'
  | 'savings'
  | 'hsa'
  | 'other'

export type RetirementIncomeType =
  | 'salary'
  | 'pension'
  | 'social-security'
  | 'rental'
  | 'custom'

export type RetirementExpenseType =
  | 'healthcare'
  | 'travel'
  | 'housing'
  | 'family'
  | 'education'
  | 'long-term-care'
  | 'custom'

export interface RetirementAccount {
  id: string
  name: string
  type: RetirementAccountType
  balance: number
  annualContribution: number
  annualReturn: number
  availableAge: number
  withdrawalRate: number
  payoutYears: number
  /**
   * Flat estimated tax applied to withdrawals from this account. It is an estimate, not a
   * bracket calculation, and it never models cost basis.
   */
  withdrawalTaxRate: number
}

/**
 * Withdrawals from tax-deferred accounts are ordinary income, so they default to the same rate the
 * app already uses for ordinary income sources. Roth and HSA withdrawals are genuinely tax-free.
 * Taxable and savings accounts default to zero because only the gain or interest portion is
 * taxable and this model tracks no cost basis; taxing the full withdrawal would overstate it.
 */
export const ORDINARY_INCOME_TAX_RATE = 0.25

export function defaultWithdrawalTaxRate(type: RetirementAccountType): number {
  return type === 'deferred' || type === 'traditional' ? ORDINARY_INCOME_TAX_RATE : 0
}

export interface RetirementIncomeSource {
  id: string
  name: string
  type: RetirementIncomeType
  annualAmount: number
  startAge: number
  endAge: number
  annualGrowth: number
  isAfterTax: boolean
  taxRate: number
}

export interface RetirementExpense {
  id: string
  name: string
  type: RetirementExpenseType
  annualAmount: number
  startAge: number
}

export interface RetirementCashFlowPoint {
  age: number
  year: number
  totalBalance: number
  outsideIncome: number
  /** Deferred payouts after estimated withdrawal tax. */
  deferredIncome: number
  /** Gap withdrawals after estimated withdrawal tax. */
  portfolioWithdrawals: number
  /** Spendable income: outside income plus after-tax deferred payouts and gap withdrawals. */
  totalIncome: number
  expenses: number
  /**
   * Displayed surplus, rounded to whole dollars away from zero. This is a presentation value — the
   * funded/shortfall verdict is decided from the unrounded surplus by `isShortfall`, never from this
   * field. See issue #63.
   */
  surplus: number
  /** Gross amounts leaving each account, before withdrawal tax. */
  withdrawals: Record<string, number>
  balances: Record<string, number>
  incomeBySource: Record<string, number>
  coreExpenses: number
  additionalExpenses: number
  expensesByItem: Record<string, number>
  /** Estimated tax on this year's deferred payouts and gap withdrawals. */
  withdrawalTaxes: number
  /** Gross balance the per-account withdrawal-rate limit held back while a gap was still unmet. */
  policyLimitedWithdrawals: number
}

export interface DeferredCompensationInputs {
  currentAge: number
  semiRetirementAge: number
  planThroughAge: number
  annualExpenses: number
  inflationRate: number
  accounts: RetirementAccount[]
  incomeSources: RetirementIncomeSource[]
  additionalExpenses: RetirementExpense[]
  withdrawOnlyAfterRetirement: boolean
  reinvestSurplus: boolean
  currentYear?: number
}

export interface DeferredCompensationResult {
  projections: RetirementCashFlowPoint[]
  currentBalance: number
  balanceAtSemiRetirement: number
  /** Spendable first-year income, after estimated withdrawal tax. */
  firstYearIncome: number
  firstYearSurplus: number
  endingBalance: number
  /** Consecutive covered years starting at retirement age, stopping at the first shortfall. */
  fundedYears: number
  /** Every covered year at or after retirement age, including years after a shortfall. */
  yearsFullyCovered: number
  /** Age of the first shortfall at or after retirement, or null when the plan never falls short. */
  firstShortfallAge: number | null
  /** Projected years at or after retirement age. */
  retirementYears: number
}

const round = (value: number) => Math.round(Math.max(0, value))

/**
 * Rounds a value that is allowed to be negative, for display only.
 *
 * `surplus` is the one field the clamping `round` helper above cannot serve, because clamping at
 * zero would hide every shortfall. Bare `Math.round` is not usable either: it rounds half *up*
 * (toward +Infinity), so `Math.round(-2.5)` is `-2` while the MAUI mirror's
 * `MidpointRounding.AwayFromZero` gives `-3`. That pairing was issue #63. Both platforms now round
 * signed money away from zero.
 *
 * The trailing `+ 0` normalizes negative zero to positive zero. `Math.round(-0.4)` is `-0`, which
 * formats as `"-$0"` through `Intl.NumberFormat` and as `-$0` through C# `ToString("C0")`, so leaving
 * it in the data model lets a screen show a negative surplus for a year that is not short. IEEE 754
 * gives `-0 + 0 === +0` while leaving every other value — including `NaN` and both infinities —
 * untouched, so the MAUI mirror applies the identical `+ 0d`.
 */
const roundSigned = (value: number) => Math.sign(value) * Math.round(Math.abs(value)) + 0

/**
 * Half of the whole-dollar unit the surplus is displayed in.
 *
 * The funded/shortfall verdict is a tolerance question, not an exact comparison: `surplus` is
 * `totalIncome - expenses`, and both operands accumulate floating-point error over as many as sixty
 * compounding steps, so a bare `surplus < 0` would report a shortfall for a residue of a millionth of
 * a cent. Half a dollar sits roughly thirteen orders of magnitude above that residue at realistic
 * balances, so no accumulation can reach it.
 *
 * It is exactly half a display unit for a second reason: `exact <= -0.5` is equivalent to
 * `roundSigned(exact) < 0` for every double, so the figure shown to the user and the verdict about
 * it can never contradict each other. A tighter threshold would flag a shortfall for a year the
 * table still renders as `$0`.
 */
const SHORTFALL_TOLERANCE = 0.5

/**
 * Decides whether a year is short, from the UNROUNDED surplus.
 *
 * Reading the rounded field instead is what made issue #63 severe: `Math.round(-0.5)` is `-0`, and
 * `-0 < 0` is `false`, so web reported a fifty-cent shortfall as a fully funded year while MAUI —
 * rounding to `-1` — reported failure at the first retirement age, from identical inputs. Keeping the
 * verdict on the exact value means no display rounding rule can move a headline again, and a negative
 * zero can never enter the comparison.
 */
const isShortfall = (exactSurplus: number) => exactSurplus <= -SHORTFALL_TOLERANCE

const clampRate = (value: number) => Math.min(1, Math.max(0, value))

const distributeSurplus = (
  balances: Map<string, number>,
  accounts: RetirementAccount[],
  surplus: number,
) => {
  if (surplus <= 0 || accounts.length === 0) return

  const totalBalance = accounts.reduce((sum, account) => sum + (balances.get(account.id) ?? 0), 0)
  for (const account of accounts) {
    const weight = totalBalance > 0
      ? (balances.get(account.id) ?? 0) / totalBalance
      : 1 / accounts.length
    balances.set(account.id, (balances.get(account.id) ?? 0) + surplus * weight)
  }
}

export function calculateDeferredCompensation({
  currentAge,
  semiRetirementAge,
  planThroughAge,
  annualExpenses,
  inflationRate,
  accounts,
  incomeSources,
  additionalExpenses,
  withdrawOnlyAfterRetirement,
  reinvestSurplus,
  currentYear = new Date().getFullYear(),
}: DeferredCompensationInputs): DeferredCompensationResult {
  const startAge = Math.max(0, Math.floor(currentAge))
  const retirementAge = Math.max(startAge, Math.floor(semiRetirementAge))
  const endAge = Math.max(retirementAge, Math.floor(planThroughAge))
  const balances = new Map(accounts.map(account => [account.id, Math.max(0, account.balance)]))
  const projections: RetirementCashFlowPoint[] = []
  // The verdict below reads these, not the rounded `surplus` on the projection points. See #63.
  const exactSurplusByAge = new Map<number, number>()

  for (let age = startAge; age <= endAge; age++) {
    const yearsFromNow = age - startAge
    const inflationMultiplier = Math.pow(1 + Math.max(-1, inflationRate), yearsFromNow)
    const canWithdraw = !withdrawOnlyAfterRetirement || age >= retirementAge
    const accountWithdrawals: Record<string, number> = {}
    const accountBalances: Record<string, number> = {}
    const incomeBySource: Record<string, number> = {}
    const expensesByItem: Record<string, number> = {}

    for (const account of accounts) {
      let balance = balances.get(account.id) ?? 0
      if (age > startAge) {
        balance *= 1 + Math.max(-1, account.annualReturn)
        // Contributions are entered in today's dollars, so the nominal amount paid in year k is
        // the entered amount escalated by inflation, matching how expenses are escalated.
        if (age < retirementAge) {
          balance += Math.max(0, account.annualContribution) * inflationMultiplier
        }
      }
      balances.set(account.id, balance)
    }

    let outsideIncome = 0
    for (const source of incomeSources) {
      const isActive = age >= source.startAge && age <= source.endAge
      const grossAmount = isActive
        ? Math.max(0, source.annualAmount) * Math.pow(1 + Math.max(-1, source.annualGrowth), yearsFromNow)
        : 0
      const netAmount = source.isAfterTax
        ? grossAmount
        : grossAmount * (1 - clampRate(source.taxRate))
      incomeBySource[source.id] = round(netAmount)
      outsideIncome += netAmount
    }

    const coreExpenses = Math.max(0, annualExpenses) * inflationMultiplier
    let additionalExpenseTotal = 0
    for (const expense of additionalExpenses) {
      const amount = age >= expense.startAge
        ? Math.max(0, expense.annualAmount) * inflationMultiplier
        : 0
      expensesByItem[expense.id] = round(amount)
      additionalExpenseTotal += amount
    }
    const expenses = coreExpenses + additionalExpenseTotal
    let deferredIncome = 0
    let withdrawalTaxes = 0

    for (const account of accounts.filter(account => account.type === 'deferred')) {
      const payoutStartAge = account.availableAge
      const payoutEndAge = payoutStartAge + Math.max(1, account.payoutYears) - 1
      if (age < payoutStartAge || age > payoutEndAge) continue

      const balance = balances.get(account.id) ?? 0
      // The undistributed balance keeps earning, so each year distributes the remaining balance
      // over the remaining payout years. That honors the payout period exactly and leaves nothing
      // stranded in an account that gap withdrawals can never reach.
      const withdrawal = Math.min(balance, balance / (payoutEndAge - age + 1))
      const taxRate = clampRate(account.withdrawalTaxRate)
      balances.set(account.id, balance - withdrawal)
      accountWithdrawals[account.id] = withdrawal
      withdrawalTaxes += withdrawal * taxRate
      deferredIncome += withdrawal * (1 - taxRate)
    }

    let remainingGap = Math.max(0, expenses - outsideIncome - deferredIncome)
    let portfolioWithdrawals = 0
    let policyLimitedWithdrawals = 0

    if (canWithdraw) {
      for (const account of accounts.filter(
        account => account.type !== 'deferred' && age >= account.availableAge,
      )) {
        const balance = balances.get(account.id) ?? 0
        const taxRate = clampRate(account.withdrawalTaxRate)
        const netFactor = 1 - taxRate
        // Withdrawals are grossed up so the spendable remainder covers the gap.
        const grossNeeded = netFactor > 0 ? remainingGap / netFactor : Number.POSITIVE_INFINITY
        const policyLimit = balance * clampRate(account.withdrawalRate)
        const withdrawal = Math.min(balance, grossNeeded, policyLimit)
        if (policyLimit < Math.min(balance, grossNeeded)) {
          policyLimitedWithdrawals += Math.min(balance, grossNeeded) - policyLimit
        }
        balances.set(account.id, balance - withdrawal)
        accountWithdrawals[account.id] = withdrawal
        withdrawalTaxes += withdrawal * taxRate
        const spendable = withdrawal * netFactor
        portfolioWithdrawals += spendable
        remainingGap -= spendable
      }
    }

    const totalIncome = outsideIncome + deferredIncome + portfolioWithdrawals
    const surplus = totalIncome - expenses
    exactSurplusByAge.set(age, surplus)
    if (reinvestSurplus && surplus > 0) distributeSurplus(balances, accounts, surplus)

    for (const account of accounts) {
      accountWithdrawals[account.id] = round(accountWithdrawals[account.id] ?? 0)
      accountBalances[account.id] = round(balances.get(account.id) ?? 0)
    }

    projections.push({
      age,
      year: currentYear + yearsFromNow,
      totalBalance: round(Array.from(balances.values()).reduce((sum, balance) => sum + balance, 0)),
      outsideIncome: round(outsideIncome),
      deferredIncome: round(deferredIncome),
      portfolioWithdrawals: round(portfolioWithdrawals),
      totalIncome: round(totalIncome),
      expenses: round(expenses),
      coreExpenses: round(coreExpenses),
      additionalExpenses: round(additionalExpenseTotal),
      surplus: roundSigned(surplus),
      withdrawals: accountWithdrawals,
      balances: accountBalances,
      incomeBySource,
      expensesByItem,
      withdrawalTaxes: round(withdrawalTaxes),
      policyLimitedWithdrawals: round(policyLimitedWithdrawals),
    })
  }

  const retirementProjection = projections.find(point => point.age === retirementAge) ?? projections[0]
  const retirementProjections = projections.filter(point => point.age >= retirementAge)
  const shortAt = (point: RetirementCashFlowPoint) => isShortfall(exactSurplusByAge.get(point.age) ?? 0)
  const firstShortfall = retirementProjections.find(shortAt)
  const consecutiveFundedYears = firstShortfall
    ? retirementProjections.findIndex(shortAt)
    : retirementProjections.length

  return {
    projections,
    currentBalance: round(accounts.reduce((sum, account) => sum + Math.max(0, account.balance), 0)),
    balanceAtSemiRetirement: retirementProjection?.totalBalance ?? 0,
    firstYearIncome: retirementProjection?.totalIncome ?? 0,
    firstYearSurplus: retirementProjection?.surplus ?? 0,
    endingBalance: projections.at(-1)?.totalBalance ?? 0,
    fundedYears: consecutiveFundedYears,
    yearsFullyCovered: retirementProjections.filter(point => !shortAt(point)).length,
    firstShortfallAge: firstShortfall?.age ?? null,
    retirementYears: retirementProjections.length,
  }
}
