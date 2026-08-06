import { useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'
import type {
  RetirementAccount,
  RetirementAccountType,
} from '../utils/deferredCompensation'

export interface DeferredCompensationParams {
  currentAge: number
  semiRetirementAge: number
  planThroughAge: number
  annualExpenses: number
  semiRetirementIncome: number
  inflationRate: number
  accounts: RetirementAccount[]
}

const ACCOUNT_TYPES: RetirementAccountType[] = [
  'deferred',
  'traditional',
  'roth',
  'taxable',
  'savings',
]

const DEFAULTS: DeferredCompensationParams = {
  currentAge: 45,
  semiRetirementAge: 55,
  planThroughAge: 90,
  annualExpenses: 80000,
  semiRetirementIncome: 20000,
  inflationRate: 0.03,
  accounts: [
    {
      id: 'deferred-comp',
      name: 'Deferred Compensation',
      type: 'deferred',
      balance: 300000,
      annualContribution: 0,
      annualReturn: 0.05,
      availableAge: 55,
      withdrawalRate: 0,
      payoutYears: 5,
    },
    {
      id: '401k',
      name: '401(k)',
      type: 'traditional',
      balance: 500000,
      annualContribution: 23500,
      annualReturn: 0.07,
      availableAge: 60,
      withdrawalRate: 0.04,
      payoutYears: 1,
    },
  ],
}

const PARAM_KEYS: Record<keyof DeferredCompensationParams, string> = {
  currentAge: 'dcAge',
  semiRetirementAge: 'dcRetire',
  planThroughAge: 'dcThrough',
  annualExpenses: 'dcExpenses',
  semiRetirementIncome: 'dcIncome',
  inflationRate: 'dcInflation',
  accounts: 'dcAccounts',
}

const numberParam = (value: string | null, fallback: number) => {
  if (value === null) return fallback
  const parsed = Number(value)
  return Number.isFinite(parsed) ? parsed : fallback
}

const sanitizeAccounts = (value: string | null): RetirementAccount[] => {
  if (!value) return DEFAULTS.accounts

  try {
    const parsed: unknown = JSON.parse(value)
    if (!Array.isArray(parsed)) return DEFAULTS.accounts

    return parsed.flatMap((item, index) => {
      if (!item || typeof item !== 'object') return []
      const account = item as Partial<RetirementAccount>
      const type = ACCOUNT_TYPES.includes(account.type as RetirementAccountType)
        ? account.type as RetirementAccountType
        : 'taxable'
      const numeric = (candidate: unknown, fallback: number) =>
        typeof candidate === 'number' && Number.isFinite(candidate) ? candidate : fallback

      return [{
        id: typeof account.id === 'string' ? account.id : `account-${index}`,
        name: typeof account.name === 'string' ? account.name.slice(0, 80) : `Account ${index + 1}`,
        type,
        balance: Math.max(0, numeric(account.balance, 0)),
        annualContribution: Math.max(0, numeric(account.annualContribution, 0)),
        annualReturn: Math.max(-1, numeric(account.annualReturn, 0)),
        availableAge: Math.max(0, numeric(account.availableAge, 59.5)),
        withdrawalRate: Math.max(0, numeric(account.withdrawalRate, 0.04)),
        payoutYears: Math.max(1, Math.round(numeric(account.payoutYears, 1))),
      }]
    }).slice(0, 20)
  } catch {
    return DEFAULTS.accounts
  }
}

export function useDeferredCompensationParams() {
  const [searchParams, setSearchParams] = useSearchParams()

  const params = useMemo<DeferredCompensationParams>(() => ({
    currentAge: numberParam(searchParams.get(PARAM_KEYS.currentAge), DEFAULTS.currentAge),
    semiRetirementAge: numberParam(
      searchParams.get(PARAM_KEYS.semiRetirementAge),
      DEFAULTS.semiRetirementAge,
    ),
    planThroughAge: numberParam(
      searchParams.get(PARAM_KEYS.planThroughAge),
      DEFAULTS.planThroughAge,
    ),
    annualExpenses: numberParam(
      searchParams.get(PARAM_KEYS.annualExpenses),
      DEFAULTS.annualExpenses,
    ),
    semiRetirementIncome: numberParam(
      searchParams.get(PARAM_KEYS.semiRetirementIncome),
      DEFAULTS.semiRetirementIncome,
    ),
    inflationRate: numberParam(
      searchParams.get(PARAM_KEYS.inflationRate),
      DEFAULTS.inflationRate,
    ),
    accounts: sanitizeAccounts(searchParams.get(PARAM_KEYS.accounts)),
  }), [searchParams])

  const setParam = useCallback(<Key extends keyof DeferredCompensationParams>(
    key: Key,
    value: DeferredCompensationParams[Key],
  ) => {
    setSearchParams(previous => {
      const next = new URLSearchParams(previous)
      const defaultValue = DEFAULTS[key]
      const isDefault = JSON.stringify(value) === JSON.stringify(defaultValue)
      if (isDefault) {
        next.delete(PARAM_KEYS[key])
      } else {
        next.set(PARAM_KEYS[key], key === 'accounts' ? JSON.stringify(value) : String(value))
      }
      return next
    }, { replace: true })
  }, [setSearchParams])

  const resetParams = useCallback(() => {
    setSearchParams(previous => {
      const next = new URLSearchParams(previous)
      Object.values(PARAM_KEYS).forEach(key => next.delete(key))
      return next
    }, { replace: true })
  }, [setSearchParams])

  const copyUrl = useCallback(async () => {
    try {
      await navigator.clipboard.writeText(window.location.href)
      return true
    } catch {
      return false
    }
  }, [])

  return {
    params,
    setParam,
    resetParams,
    copyUrl,
    hasCustomParams: Object.values(PARAM_KEYS).some(key => searchParams.has(key)),
  }
}
