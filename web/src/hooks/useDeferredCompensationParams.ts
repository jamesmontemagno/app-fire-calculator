import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useLocation, useSearchParams } from 'react-router-dom'
import type {
  RetirementAccount,
  RetirementAccountType,
  RetirementExpense,
  RetirementExpenseType,
  RetirementIncomeSource,
  RetirementIncomeType,
} from '../utils/deferredCompensation'
import { DEFERRED_STORAGE_KEY_PREFIX } from '../utils/savedCalculationStorage'

export interface DeferredCompensationParams {
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

const EXPENSE_TYPES: RetirementExpenseType[] = [
  'healthcare',
  'travel',
  'housing',
  'family',
  'education',
  'long-term-care',
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
      id: 'savings',
      name: 'Savings',
      type: 'savings',
      balance: 300000,
      annualContribution: 0,
      annualReturn: 0.05,
      availableAge: 18,
      withdrawalRate: 0.04,
      payoutYears: 1,
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
  additionalExpenses: [],
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
  additionalExpenses: 'dcAdditionalExpenses',
  withdrawOnlyAfterRetirement: 'dcRetireOnly',
  reinvestSurplus: 'dcReinvest',
}

interface SavedDeferredCompensationParams {
  params: Partial<DeferredCompensationParams>
  savedAt: string | null
}

function loadFromStorage(storageKey: string): SavedDeferredCompensationParams | null {
  if (typeof window === 'undefined') return null
  try {
    const stored = localStorage.getItem(storageKey)
    if (!stored) return null
    const parsed: unknown = JSON.parse(stored)
    if (!parsed || typeof parsed !== 'object') return null

    if ('params' in parsed) {
      const { params, savedAt } = parsed as SavedDeferredCompensationParams
      if (!params || typeof params !== 'object') return null
      return {
        params,
        savedAt: typeof savedAt === 'string' && !Number.isNaN(Date.parse(savedAt))
          ? savedAt
          : null,
      }
    }

    // Saved calculations from before timestamps were added remain loadable.
    return { params: parsed as Partial<DeferredCompensationParams>, savedAt: null }
  } catch {
    return null
  }
}

function saveToStorage(
  storageKey: string,
  savedParams: SavedDeferredCompensationParams,
): void {
  if (typeof window === 'undefined') return
  try {
    localStorage.setItem(storageKey, JSON.stringify(savedParams))
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

const sanitizeAdditionalExpenses = (value: string | null): RetirementExpense[] => {
  if (!value) return DEFAULTS.additionalExpenses

  try {
    const parsed: unknown = JSON.parse(value)
    if (!Array.isArray(parsed)) return DEFAULTS.additionalExpenses

    return parsed.flatMap((item, index) => {
      if (!item || typeof item !== 'object') return []
      const expense = item as Partial<RetirementExpense>
      const numeric = (candidate: unknown, fallback: number) =>
        typeof candidate === 'number' && Number.isFinite(candidate) ? candidate : fallback
      const type = EXPENSE_TYPES.includes(expense.type as RetirementExpenseType)
        ? expense.type as RetirementExpenseType
        : 'custom'

      return [{
        id: typeof expense.id === 'string' ? expense.id.slice(0, 80) : `expense-${index}`,
        name: typeof expense.name === 'string' ? expense.name.slice(0, 80) : `Expense ${index + 1}`,
        type,
        annualAmount: Math.max(0, numeric(expense.annualAmount, 0)),
        startAge: Math.min(120, Math.max(0, Math.floor(numeric(expense.startAge, 0)))),
      }]
    }).slice(0, 20)
  } catch {
    return DEFAULTS.additionalExpenses
  }
}

function applyDeferredParamToSearchParams<Key extends keyof DeferredCompensationParams>(
  searchParams: URLSearchParams,
  key: Key,
  value: DeferredCompensationParams[Key],
  savedParams: Partial<DeferredCompensationParams> | null,
) {
  const defaultValue = DEFAULTS[key]
  const isDefault = JSON.stringify(value) === JSON.stringify(defaultValue)
  const savedValue = savedParams?.[key]
  const matchesSavedValue = JSON.stringify(value) === JSON.stringify(savedValue)
  if (isDefault && (savedValue === undefined || matchesSavedValue)) {
    searchParams.delete(PARAM_KEYS[key])
  } else {
    searchParams.set(
      PARAM_KEYS[key],
      key === 'accounts' || key === 'incomeSources' || key === 'additionalExpenses'
        ? JSON.stringify(value)
        : String(value),
    )
  }
}

export function useDeferredCompensationParams() {
  const [searchParams, setSearchParams] = useSearchParams()
  const location = useLocation()
  const debounceTimersRef = useRef<Partial<Record<keyof DeferredCompensationParams, ReturnType<typeof setTimeout>>>>({})
  const storageKey = `${DEFERRED_STORAGE_KEY_PREFIX}:${location.pathname}`
  const [storedParams, setStoredParams] = useState<SavedDeferredCompensationParams | null>(
    () => loadFromStorage(storageKey),
  )
  const [savedParams, setSavedParams] = useState<Partial<DeferredCompensationParams> | null>(null)
  const [pendingParams, setPendingParams] = useState<Partial<DeferredCompensationParams>>({})
  const pendingParamsRef = useRef<Partial<DeferredCompensationParams>>({})

  const resolvedParams = useMemo<DeferredCompensationParams>(() => {
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
      additionalExpenses: sanitizeAdditionalExpenses(
        searchParams.get(PARAM_KEYS.additionalExpenses)
          ?? JSON.stringify(savedParams?.additionalExpenses ?? DEFAULTS.additionalExpenses),
      ),
      withdrawOnlyAfterRetirement: searchParams.get(PARAM_KEYS.withdrawOnlyAfterRetirement)
        ? searchParams.get(PARAM_KEYS.withdrawOnlyAfterRetirement) === 'true'
        : savedParams?.withdrawOnlyAfterRetirement ?? DEFAULTS.withdrawOnlyAfterRetirement,
      reinvestSurplus: searchParams.get(PARAM_KEYS.reinvestSurplus)
        ? searchParams.get(PARAM_KEYS.reinvestSurplus) === 'true'
        : savedParams?.reinvestSurplus ?? DEFAULTS.reinvestSurplus,
    }
  }, [savedParams, searchParams])
  const params = useMemo(() => ({ ...resolvedParams, ...pendingParams }), [pendingParams, resolvedParams])

  useEffect(() => {
    pendingParamsRef.current = pendingParams
  }, [pendingParams])

  useEffect(() => () => {
    Object.values(debounceTimersRef.current).forEach(timer => {
      if (timer) clearTimeout(timer)
    })
  }, [])

  const clearPendingParam = useCallback((key: keyof DeferredCompensationParams) => {
    const timer = debounceTimersRef.current[key]
    if (timer) clearTimeout(timer)
    delete debounceTimersRef.current[key]

    if (!(key in pendingParamsRef.current)) return
    const nextPendingParams = { ...pendingParamsRef.current }
    delete nextPendingParams[key]
    pendingParamsRef.current = nextPendingParams
    setPendingParams(nextPendingParams)
  }, [])

  const cancelPendingParams = useCallback(() => {
    Object.values(debounceTimersRef.current).forEach(timer => {
      if (timer) clearTimeout(timer)
    })
    debounceTimersRef.current = {}
    pendingParamsRef.current = {}
    setPendingParams({})
  }, [])

  const setParam = useCallback(<Key extends keyof DeferredCompensationParams>(
    key: Key,
    value: DeferredCompensationParams[Key],
  ) => {
    clearPendingParam(key)
    setSearchParams(previous => {
      const next = new URLSearchParams(previous)
      applyDeferredParamToSearchParams(next, key, value, savedParams)
      return next
    }, { replace: true })
  }, [clearPendingParam, savedParams, setSearchParams])

  const setParamDebounced = useCallback(<Key extends keyof DeferredCompensationParams>(
    key: Key,
    value: DeferredCompensationParams[Key],
    delay = 300,
  ) => {
    const timer = debounceTimersRef.current[key]
    if (timer) clearTimeout(timer)

    const nextPendingParams = { ...pendingParamsRef.current, [key]: value }
    pendingParamsRef.current = nextPendingParams
    setPendingParams(nextPendingParams)
    debounceTimersRef.current[key] = setTimeout(() => {
      delete debounceTimersRef.current[key]
      setParam(key, value)
    }, delay)
  }, [setParam])

  const flushPendingParams = useCallback(() => {
    const pending = pendingParamsRef.current
    if (Object.keys(pending).length === 0) return

    Object.values(debounceTimersRef.current).forEach(timer => {
      if (timer) clearTimeout(timer)
    })
    debounceTimersRef.current = {}
    setSearchParams(previous => {
      const next = new URLSearchParams(previous)
      ;(Object.keys(pending) as (keyof DeferredCompensationParams)[]).forEach(key => {
        const value = pending[key]
        if (value !== undefined) applyDeferredParamToSearchParams(next, key, value, savedParams)
      })
      return next
    }, { replace: true })
    pendingParamsRef.current = {}
    setPendingParams({})
  }, [savedParams, setSearchParams])

  const resetParams = useCallback(() => {
    cancelPendingParams()
    setSearchParams(previous => {
      const next = new URLSearchParams(previous)
      Object.values(PARAM_KEYS).forEach(key => next.delete(key))
      return next
    }, { replace: true })
    clearStorage(storageKey)
    setStoredParams(null)
    setSavedParams(null)
  }, [cancelPendingParams, setSearchParams, storageKey])

  const saveParams = useCallback(() => {
    flushPendingParams()
    const nextSavedParams = {
      params,
      savedAt: new Date().toISOString(),
    }
    saveToStorage(storageKey, nextSavedParams)
    setStoredParams(nextSavedParams)
    setSavedParams(params)
  }, [flushPendingParams, params, storageKey])

  const loadParams = useCallback(() => {
    if (!storedParams) return
    cancelPendingParams()
    setSearchParams(previous => {
      const next = new URLSearchParams(previous)
      ;(Object.keys(PARAM_KEYS) as (keyof DeferredCompensationParams)[]).forEach(key => {
        const value = storedParams.params[key]
        if (value === undefined || value === null) {
          next.delete(PARAM_KEYS[key])
          return
        }
        next.set(
          PARAM_KEYS[key],
          key === 'accounts' || key === 'incomeSources' || key === 'additionalExpenses'
            ? JSON.stringify(value)
            : String(value),
        )
      })
      return next
    }, { replace: true })
    setSavedParams(storedParams.params)
  }, [cancelPendingParams, setSearchParams, storedParams])

  const copyUrl = useCallback(async () => {
    const pending = pendingParamsRef.current
    const next = new URLSearchParams(searchParams)
    ;(Object.keys(pending) as (keyof DeferredCompensationParams)[]).forEach(key => {
      const value = pending[key]
      if (value !== undefined) applyDeferredParamToSearchParams(next, key, value, savedParams)
    })
    const url = new URL(window.location.href)
    url.search = next.toString()
    flushPendingParams()
    try {
      await navigator.clipboard.writeText(url.toString())
      return true
    } catch {
      return false
    }
  }, [flushPendingParams, savedParams, searchParams])

  return {
    params,
    setParam,
    setParamDebounced,
    resetParams,
    saveParams,
    copyUrl,
    hasCustomParams: Object.values(PARAM_KEYS).some(key => searchParams.has(key)) || Object.keys(pendingParams).length > 0,
    hasUnsavedChanges: savedParams
      ? !matchesSavedParams(params, savedParams)
      : storedParams
        ? Object.values(PARAM_KEYS).some(key => searchParams.has(key))
          && !matchesSavedParams(params, storedParams.params)
        : !matchesSavedParams(params, null),
    hasSavedParams: storedParams !== null,
    savedAt: storedParams?.savedAt ?? null,
    loadParams,
  }
}
