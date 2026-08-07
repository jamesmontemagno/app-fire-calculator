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

export interface RetirementCashFlowPoint {
  age: number
  year: number
  totalBalance: number
  outsideIncome: number
  deferredIncome: number
  portfolioWithdrawals: number
  totalIncome: number
  expenses: number
  surplus: number
  withdrawals: Record<string, number>
  balances: Record<string, number>
  incomeBySource: Record<string, number>
}

export interface DeferredCompensationInputs {
  currentAge: number
  semiRetirementAge: number
  planThroughAge: number
  annualExpenses: number
  inflationRate: number
  accounts: RetirementAccount[]
  incomeSources: RetirementIncomeSource[]
  withdrawOnlyAfterRetirement: boolean
  reinvestSurplus: boolean
  currentYear?: number
}

export interface DeferredCompensationResult {
  projections: RetirementCashFlowPoint[]
  currentBalance: number
  balanceAtSemiRetirement: number
  firstYearIncome: number
  firstYearSurplus: number
  endingBalance: number
  fundedYears: number
}

const round = (value: number) => Math.round(Math.max(0, value))

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
  withdrawOnlyAfterRetirement,
  reinvestSurplus,
  currentYear = new Date().getFullYear(),
}: DeferredCompensationInputs): DeferredCompensationResult {
  const startAge = Math.max(0, Math.floor(currentAge))
  const retirementAge = Math.max(startAge, Math.floor(semiRetirementAge))
  const endAge = Math.max(retirementAge, Math.floor(planThroughAge))
  const balances = new Map(accounts.map(account => [account.id, Math.max(0, account.balance)]))
  const projections: RetirementCashFlowPoint[] = []

  for (let age = startAge; age <= endAge; age++) {
    const yearsFromNow = age - startAge
    const canWithdraw = !withdrawOnlyAfterRetirement || age >= retirementAge
    const accountWithdrawals: Record<string, number> = {}
    const accountBalances: Record<string, number> = {}
    const incomeBySource: Record<string, number> = {}

    for (const account of accounts) {
      let balance = balances.get(account.id) ?? 0
      if (age > startAge) {
        balance *= 1 + Math.max(-1, account.annualReturn)
        if (age < retirementAge) balance += Math.max(0, account.annualContribution)
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
        : grossAmount * (1 - Math.min(1, Math.max(0, source.taxRate)))
      incomeBySource[source.id] = round(netAmount)
      outsideIncome += netAmount
    }

    const expenses = Math.max(0, annualExpenses) * Math.pow(1 + Math.max(-1, inflationRate), yearsFromNow)
    let deferredIncome = 0

    for (const account of accounts.filter(
      account => account.type === 'deferred' && age >= account.availableAge,
    )) {
        const balance = balances.get(account.id) ?? 0
        const payoutStartAge = account.availableAge
        const payoutEndAge = payoutStartAge + Math.max(1, account.payoutYears) - 1
        const withdrawal = age >= payoutStartAge && age <= payoutEndAge
          ? balance / (payoutEndAge - age + 1)
          : 0
        balances.set(account.id, balance - withdrawal)
        accountWithdrawals[account.id] = withdrawal
        deferredIncome += withdrawal
    }

    let remainingGap = Math.max(0, expenses - outsideIncome - deferredIncome)
    let portfolioWithdrawals = 0

    if (canWithdraw) {
      for (const account of accounts.filter(
        account => account.type !== 'deferred' && age >= account.availableAge,
      )) {
        const balance = balances.get(account.id) ?? 0
        const withdrawal = Math.min(
          balance,
          remainingGap,
          balance * Math.min(1, Math.max(0, account.withdrawalRate)),
        )
        balances.set(account.id, balance - withdrawal)
        accountWithdrawals[account.id] = withdrawal
        portfolioWithdrawals += withdrawal
        remainingGap -= withdrawal
      }
    }

    const totalIncome = outsideIncome + deferredIncome + portfolioWithdrawals
    const surplus = totalIncome - expenses
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
      surplus: Math.round(surplus),
      withdrawals: accountWithdrawals,
      balances: accountBalances,
      incomeBySource,
    })
  }

  const retirementProjection = projections.find(point => point.age === retirementAge) ?? projections[0]
  const retirementProjections = projections.filter(point => point.age >= retirementAge)

  return {
    projections,
    currentBalance: round(accounts.reduce((sum, account) => sum + Math.max(0, account.balance), 0)),
    balanceAtSemiRetirement: retirementProjection?.totalBalance ?? 0,
    firstYearIncome: retirementProjection?.totalIncome ?? 0,
    firstYearSurplus: retirementProjection?.surplus ?? 0,
    endingBalance: projections.at(-1)?.totalBalance ?? 0,
    fundedYears: retirementProjections.filter(point => point.surplus >= 0).length,
  }
}
