import { useCallback, useMemo, useState } from 'react'
import { useLocation, useSearchParams } from 'react-router-dom'
import type {
  RetirementAccount,
  RetirementAccountType,
  RetirementIncomeSource,
  RetirementIncomeType,
} from '../utils/deferredCompensation'

export interface DeferredCompensationParams {
  currentAge: number
  semiRetirementAge: number
  planThroughAge: number
  annualExpenses: number
  inflationRate: number
  accounts: RetirementAccount[]
  incomeSources: RetirementIncomeSource[]
  withdrawOnlyAfterRetirement: boolean
  reinvestSurplus: boolean
}

const ACCOUNT_TYPES: RetirementAccountType[] = [
  'deferred',
  'traditional',
  'roth',
  'taxable',
  'savings',
  'hsa',
  'other',
]

const INCOME_TYPES: RetirementIncomeType[] = [
  'salary',
  'pension',
  'social-security',
  'rental',
  'custom',
]

const DEFAULTS: DeferredCompensationParams = {
  currentAge: 45,
  semiRetirementAge: 55,
  planThroughAge: 90,
  annualExpenses: 80000,
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
  incomeSources: [
    {
      id: 'part-time-income',
      name: 'Part-time income',
      type: 'salary',
      annualAmount: 20000,
      startAge: 55,
      endAge: 65,
      annualGrowth: 0,
      isAfterTax: true,
      taxRate: 0.25,
    },
  ],
  withdrawOnlyAfterRetirement: true,
  reinvestSurplus: true,
}

const PARAM_KEYS: Record<keyof DeferredCompensationParams, string> = {
  currentAge: 'dcAge',
  semiRetirementAge: 'dcRetire',
  planThroughAge: 'dcThrough',
  annualExpenses: 'dcExpenses',
  inflationRate: 'dcInflation',
  accounts: 'dcAccounts',
  incomeSources: 'dcIncomeSources',
  withdrawOnlyAfterRetirement: 'dcRetireOnly',
  reinvestSurplus: 'dcReinvest',
}

const STORAGE_KEY_PREFIX = 'fire-calc-deferred-params'

function loadFromStorage(storageKey: string): Partial<DeferredCompensationParams> | null {
  if (typeof window === 'undefined') return null
  try {
    const stored = localStorage.getItem(storageKey)
    return stored ? JSON.parse(stored) : null
  } catch {
    return null
  }
}

function saveToStorage(storageKey: string, params: DeferredCompensationParams): void {
  if (typeof window === 'undefined') return
  try {
    localStorage.setItem(storageKey, JSON.stringify(params))
  } catch {
    // Silently fail if storage is unavailable
  }
}

function clearStorage(storageKey: string): void {
  if (typeof window === 'undefined') return
  try {
    localStorage.removeItem(storageKey)
  } catch {
    // Silently fail if storage is unavailable
  }
}

function matchesSavedParams(
  params: DeferredCompensationParams,
  savedParams: Partial<DeferredCompensationParams> | null,
): boolean {
  return (Object.keys(DEFAULTS) as (keyof DeferredCompensationParams)[]).every(key =>
    JSON.stringify(params[key]) === JSON.stringify(savedParams?.[key] ?? DEFAULTS[key]),
  )
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
        annualReturn: Math.min(1, Math.max(-1, numeric(account.annualReturn, 0))),
        availableAge: Math.min(120, Math.max(0, numeric(account.availableAge, 60))),
        withdrawalRate: Math.min(1, Math.max(0, numeric(account.withdrawalRate, 0.04))),
        payoutYears: Math.min(100, Math.max(1, Math.round(numeric(account.payoutYears, 1)))),
      }]
    }).slice(0, 20)
  } catch {
    return DEFAULTS.accounts
  }
}

const sanitizeIncomeSources = (value: string | null): RetirementIncomeSource[] => {
  if (!value) return DEFAULTS.incomeSources

  try {
    const parsed: unknown = JSON.parse(value)
    if (!Array.isArray(parsed)) return DEFAULTS.incomeSources

    return parsed.flatMap((item, index) => {
      if (!item || typeof item !== 'object') return []
      const source = item as Partial<RetirementIncomeSource>
      const numeric = (candidate: unknown, fallback: number) =>
        typeof candidate === 'number' && Number.isFinite(candidate) ? candidate : fallback
      const type = INCOME_TYPES.includes(source.type as RetirementIncomeType)
        ? source.type as RetirementIncomeType
        : 'custom'

      return [{
        id: typeof source.id === 'string' ? source.id.slice(0, 80) : `income-${index}`,
        name: typeof source.name === 'string' ? source.name.slice(0, 80) : `Income ${index + 1}`,
        type,
        annualAmount: Math.max(0, numeric(source.annualAmount, 0)),
        startAge: Math.min(120, Math.max(0, Math.floor(numeric(source.startAge, 0)))),
        endAge: Math.min(120, Math.max(0, Math.floor(numeric(source.endAge, 120)))),
        annualGrowth: Math.min(1, Math.max(-1, numeric(source.annualGrowth, 0))),
        isAfterTax: typeof source.isAfterTax === 'boolean' ? source.isAfterTax : true,
        taxRate: Math.min(1, Math.max(0, numeric(source.taxRate, 0.25))),
      }]
    }).slice(0, 20)
  } catch {
    return DEFAULTS.incomeSources
  }
}

export function useDeferredCompensationParams() {
  const [searchParams, setSearchParams] = useSearchParams()
  const location = useLocation()
  const storageKey = `${STORAGE_KEY_PREFIX}:${location.pathname}`
  const [savedParams, setSavedParams] = useState<Partial<DeferredCompensationParams> | null>(
    () => loadFromStorage(storageKey),
  )

  const params = useMemo<DeferredCompensationParams>(() => {
    const currentAge = Math.min(100, Math.max(
      18,
      numberParam(searchParams.get(PARAM_KEYS.currentAge), savedParams?.currentAge ?? DEFAULTS.currentAge),
    ))
    const semiRetirementAge = Math.min(100, Math.max(
      currentAge,
      numberParam(
        searchParams.get(PARAM_KEYS.semiRetirementAge),
        savedParams?.semiRetirementAge ?? DEFAULTS.semiRetirementAge,
      ),
    ))
    const planThroughAge = Math.min(120, Math.max(
      semiRetirementAge,
      numberParam(
        searchParams.get(PARAM_KEYS.planThroughAge),
        savedParams?.planThroughAge ?? DEFAULTS.planThroughAge,
      ),
    ))

    return {
      currentAge,
      semiRetirementAge,
      planThroughAge,
      annualExpenses: Math.max(0, numberParam(
        searchParams.get(PARAM_KEYS.annualExpenses),
        savedParams?.annualExpenses ?? DEFAULTS.annualExpenses,
      )),
      inflationRate: Math.min(1, Math.max(-1, numberParam(
        searchParams.get(PARAM_KEYS.inflationRate),
        savedParams?.inflationRate ?? DEFAULTS.inflationRate,
      ))),
      accounts: sanitizeAccounts(searchParams.get(PARAM_KEYS.accounts) ?? JSON.stringify(savedParams?.accounts ?? DEFAULTS.accounts)),
      incomeSources: sanitizeIncomeSources(
        searchParams.get(PARAM_KEYS.incomeSources)
          ?? JSON.stringify(savedParams?.incomeSources ?? DEFAULTS.incomeSources),
      ),
      withdrawOnlyAfterRetirement: searchParams.get(PARAM_KEYS.withdrawOnlyAfterRetirement)
        ? searchParams.get(PARAM_KEYS.withdrawOnlyAfterRetirement) === 'true'
        : savedParams?.withdrawOnlyAfterRetirement ?? DEFAULTS.withdrawOnlyAfterRetirement,
      reinvestSurplus: searchParams.get(PARAM_KEYS.reinvestSurplus)
        ? searchParams.get(PARAM_KEYS.reinvestSurplus) === 'true'
        : savedParams?.reinvestSurplus ?? DEFAULTS.reinvestSurplus,
    }
  }, [savedParams, searchParams])

  const setParam = useCallback(<Key extends keyof DeferredCompensationParams>(
    key: Key,
    value: DeferredCompensationParams[Key],
  ) => {
    setSearchParams(previous => {
      const next = new URLSearchParams(previous)
      const defaultValue = DEFAULTS[key]
      const isDefault = JSON.stringify(value) === JSON.stringify(defaultValue)
      const savedValue = savedParams?.[key]
      const matchesSavedValue = JSON.stringify(value) === JSON.stringify(savedValue)
      if (isDefault && (savedValue === undefined || matchesSavedValue)) {
        next.delete(PARAM_KEYS[key])
      } else {
        next.set(
          PARAM_KEYS[key],
          key === 'accounts' || key === 'incomeSources' ? JSON.stringify(value) : String(value),
        )
      }
      return next
    }, { replace: true })
  }, [savedParams, setSearchParams])

  const resetParams = useCallback(() => {
    setSearchParams(previous => {
      const next = new URLSearchParams(previous)
      Object.values(PARAM_KEYS).forEach(key => next.delete(key))
      return next
    }, { replace: true })
    clearStorage(storageKey)
    setSavedParams(null)
  }, [setSearchParams, storageKey])

  const saveParams = useCallback(() => {
    saveToStorage(storageKey, params)
    setSavedParams(params)
  }, [params, storageKey])

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
    saveParams,
    copyUrl,
    hasCustomParams: Object.values(PARAM_KEYS).some(key => searchParams.has(key)),
    hasUnsavedChanges: !matchesSavedParams(params, savedParams),
  }
}
