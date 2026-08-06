export type RetirementAccountType =
  | 'deferred'
  | 'traditional'
  | 'roth'
  | 'taxable'
  | 'savings'

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

export interface RetirementCashFlowPoint {
  age: number
  year: number
  totalBalance: number
  employmentIncome: number
  accountIncome: number
  totalIncome: number
  expenses: number
  surplus: number
  withdrawals: Record<string, number>
  balances: Record<string, number>
}

export interface DeferredCompensationInputs {
  currentAge: number
  semiRetirementAge: number
  planThroughAge: number
  annualExpenses: number
  semiRetirementIncome: number
  inflationRate: number
  accounts: RetirementAccount[]
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

export function calculateDeferredCompensation({
  currentAge,
  semiRetirementAge,
  planThroughAge,
  annualExpenses,
  semiRetirementIncome,
  inflationRate,
  accounts,
  currentYear = new Date().getFullYear(),
}: DeferredCompensationInputs): DeferredCompensationResult {
  const startAge = Math.max(0, Math.floor(currentAge))
  const retirementAge = Math.max(startAge, Math.floor(semiRetirementAge))
  const endAge = Math.max(retirementAge, Math.floor(planThroughAge))
  const balances = new Map(accounts.map(account => [account.id, Math.max(0, account.balance)]))
  const projections: RetirementCashFlowPoint[] = []

  for (let age = startAge; age <= endAge; age++) {
    const retired = age >= retirementAge
    const accountWithdrawals: Record<string, number> = {}
    const accountBalances: Record<string, number> = {}
    let accountIncome = 0

    for (const account of accounts) {
      let balance = balances.get(account.id) ?? 0

      if (age > startAge) {
        balance *= 1 + Math.max(-1, account.annualReturn)
        if (!retired) {
          balance += Math.max(0, account.annualContribution)
        }
      }

      let withdrawal = 0
      if (retired && age >= account.availableAge && balance > 0) {
        if (account.type === 'deferred') {
          const payoutStartAge = Math.max(account.availableAge, retirementAge)
          const payoutEndAge = payoutStartAge + Math.max(1, account.payoutYears) - 1
          if (age <= payoutEndAge) {
            const remainingPayments = payoutEndAge - age + 1
            withdrawal = balance / remainingPayments
          }
        } else {
          withdrawal = balance * Math.max(0, account.withdrawalRate)
        }
      }

      withdrawal = Math.min(balance, Math.max(0, withdrawal))
      balance -= withdrawal
      balances.set(account.id, balance)
      accountWithdrawals[account.id] = round(withdrawal)
      accountBalances[account.id] = round(balance)
      accountIncome += withdrawal
    }

    const yearsFromNow = age - startAge
    const expenses = Math.max(0, annualExpenses) * Math.pow(1 + Math.max(-1, inflationRate), yearsFromNow)
    const employmentIncome = retired ? Math.max(0, semiRetirementIncome) : 0
    const totalIncome = employmentIncome + accountIncome

    projections.push({
      age,
      year: currentYear + yearsFromNow,
      totalBalance: round(Array.from(balances.values()).reduce((sum, balance) => sum + balance, 0)),
      employmentIncome: round(employmentIncome),
      accountIncome: round(accountIncome),
      totalIncome: round(totalIncome),
      expenses: round(expenses),
      surplus: Math.round(totalIncome - expenses),
      withdrawals: accountWithdrawals,
      balances: accountBalances,
    })
  }

  const retirementProjection =
    projections.find(point => point.age === retirementAge) ?? projections[0]
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
