import { useLocation, useSearchParams } from 'react-router-dom'
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'

import type { DebtItem } from '../utils/calculations'
import { STANDARD_STORAGE_KEY_PREFIX } from '../utils/savedCalculationStorage'

interface SavedCalculatorParams {
  params: Partial<CalculatorParams>
  savedAt: string | null
}

interface CalculatorParams {
  currentAge: number
  retirementAge: number
  currentSavings: number
  annualContribution: number
  annualIncome: number
  expectedReturn: number
  inflationRate: number
  withdrawalRate: number
  annualExpenses: number
  partTimeIncome: number
  portfolioValue: number
  retirementYears: number
  // Debt payoff parameters
  debts: DebtItem[]
  debtBudget: number
  debtExtra: number
  debtMonths: number
  debtMode: 'fixed' | 'target'
  debtStrategy: 'snowball' | 'avalanche'
  savingsFrequency: 'monthly' | 'yearly'
  savingsContribution: number
  savingsYears: number
  healthcareMonthlyPremium: number
  healthcareAnnualDeductible: number
  healthcareAnnualOutOfPocket: number
}

const DEFAULTS: CalculatorParams = {
  currentAge: 30,
  retirementAge: 55,
  currentSavings: 100000,
  annualContribution: 24000,
  annualIncome: 72000,
  expectedReturn: 0.07,
  inflationRate: 0.03,
  withdrawalRate: 0.04,
  annualExpenses: 48000,
  partTimeIncome: 20000,
  portfolioValue: 1000000,
  retirementYears: 30,
  debts: [],
  debtBudget: 1000,
  debtExtra: 0,
  debtMonths: 36,
  debtMode: 'fixed',
  debtStrategy: 'snowball',
  savingsFrequency: 'monthly',
  savingsContribution: 500,
  savingsYears: 30,
  healthcareMonthlyPremium: 600,
  healthcareAnnualDeductible: 2500,
  healthcareAnnualOutOfPocket: 2000,
}

const PARAM_KEYS: Record<keyof CalculatorParams, string> = {
  currentAge: 'age',
  retirementAge: 'retire',
  currentSavings: 'savings',
  annualContribution: 'contrib',
  annualIncome: 'income',
  expectedReturn: 'return',
  inflationRate: 'inflation',
  withdrawalRate: 'swr',
  annualExpenses: 'expenses',
  partTimeIncome: 'parttime',
  portfolioValue: 'portfolio',
  retirementYears: 'years',
  debts: 'debts',
  debtBudget: 'budget',
  debtExtra: 'extra',
  debtMonths: 'months',
  debtMode: 'mode',
  debtStrategy: 'strategy',
  savingsFrequency: 'savingsFrequency',
  savingsContribution: 'savingsContribution',
  savingsYears: 'savingsYears',
  healthcareMonthlyPremium: 'healthcarePremium',
  healthcareAnnualDeductible: 'healthcareDeductible',
  healthcareAnnualOutOfPocket: 'healthcareOop',
}

interface NumericBounds {
  min?: number
  max?: number
}

const NUMERIC_BOUNDS: Partial<Record<keyof CalculatorParams, NumericBounds>> = {
  currentAge: { min: 18, max: 80 },
  retirementAge: { min: 18, max: 90 },
  currentSavings: { min: 0 },
  annualContribution: { min: 0 },
  annualIncome: { min: 0 },
  expectedReturn: { min: 0, max: 0.15 },
  inflationRate: { min: 0, max: 0.1 },
  withdrawalRate: { min: 0.025, max: 0.06 },
  annualExpenses: { min: 0 },
  partTimeIncome: { min: 0 },
  portfolioValue: { min: 0 },
  retirementYears: { min: 10, max: 60 },
  debtBudget: { min: 0 },
  debtExtra: { min: 0 },
  debtMonths: { min: 1, max: 360 },
  savingsContribution: { min: 0 },
  savingsYears: { min: 1, max: 50 },
  healthcareMonthlyPremium: { min: 0, max: 3000 },
  healthcareAnnualDeductible: { min: 0, max: 20000 },
  healthcareAnnualOutOfPocket: { min: 0, max: 20000 },
}

const ROUTE_NUMERIC_BOUNDS: Record<string, Partial<Record<keyof CalculatorParams, NumericBounds>>> = {
  '/standard': {
    withdrawalRate: { min: 0.02, max: 0.06 },
  },
  '/withdrawal': {
    withdrawalRate: { min: 0.02, max: 0.08 },
  },
  '/healthcare': {
    currentAge: { min: 18, max: 64 },
    retirementAge: { min: 18, max: 64 },
    inflationRate: { min: 0, max: 0.15 },
  },
}

function parseNumericParam(
  value: string,
  fallback: number,
  bounds?: NumericBounds,
): number {
  if (value.trim() === '') return fallback

  const parsed = Number(value)
  if (!Number.isFinite(parsed)) return fallback

  return Math.min(
    bounds?.max ?? Number.POSITIVE_INFINITY,
    Math.max(bounds?.min ?? Number.NEGATIVE_INFINITY, parsed),
  )
}

// localStorage utilities
function loadFromStorage(storageKey: string): SavedCalculatorParams | null {
  if (typeof window === 'undefined') return null
  try {
    const stored = localStorage.getItem(storageKey)
    if (!stored) return null
    const parsed: unknown = JSON.parse(stored)
    if (!parsed || typeof parsed !== 'object') return null

    if ('params' in parsed) {
      const { params, savedAt } = parsed as SavedCalculatorParams
      if (!params || typeof params !== 'object') return null
      return {
        params,
        savedAt: typeof savedAt === 'string' && !Number.isNaN(Date.parse(savedAt))
          ? savedAt
          : null,
      }
    }

    // Saved calculations from before timestamps were added remain loadable.
    return { params: parsed as Partial<CalculatorParams>, savedAt: null }
  } catch {
    return null
  }
}

function saveToStorage(storageKey: string, savedParams: SavedCalculatorParams): void {
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
  params: CalculatorParams,
  savedParams: Partial<CalculatorParams> | null,
): boolean {
  return (Object.keys(DEFAULTS) as (keyof CalculatorParams)[]).every(key =>
    JSON.stringify(params[key]) === JSON.stringify(savedParams?.[key] ?? DEFAULTS[key]),
  )
}

function applyParamToSearchParams<Key extends keyof CalculatorParams>(
  searchParams: URLSearchParams,
  key: Key,
  value: CalculatorParams[Key],
  savedParams: Partial<CalculatorParams> | null,
) {
  const defaultValue = DEFAULTS[key]
  const isDefault = key === 'debts'
    ? JSON.stringify(value) === JSON.stringify(defaultValue)
    : value === defaultValue
  const savedValue = savedParams?.[key]
  const matchesSavedValue = key === 'debts'
    ? JSON.stringify(value) === JSON.stringify(savedValue)
    : value === savedValue

  if (isDefault && (savedValue === undefined || matchesSavedValue)) {
    searchParams.delete(PARAM_KEYS[key])
  } else {
    searchParams.set(PARAM_KEYS[key], key === 'debts' ? JSON.stringify(value) : String(value))
  }
}

export function useCalculatorParams() {
  const [searchParams, setSearchParams] = useSearchParams()
  const location = useLocation()
  const debounceTimersRef = useRef<Partial<Record<keyof CalculatorParams, ReturnType<typeof setTimeout>>>>({})
  const storageKey = `${STANDARD_STORAGE_KEY_PREFIX}:${location.pathname}`
  const [storedParams, setStoredParams] = useState<SavedCalculatorParams | null>(
    () => loadFromStorage(storageKey),
  )
  const [savedParams, setSavedParams] = useState<Partial<CalculatorParams> | null>(null)
  const [pendingParams, setPendingParams] = useState<Partial<CalculatorParams>>({})
  const pendingParamsRef = useRef<Partial<CalculatorParams>>({})

  const resolvedParams = useMemo((): CalculatorParams => {
    const routeBounds = ROUTE_NUMERIC_BOUNDS[location.pathname]
    const getParam = (key: keyof CalculatorParams): any => {
      const urlKey = PARAM_KEYS[key]
      const urlValue = searchParams.get(urlKey)
      
      // Priority: URL params > saved section values > defaults
      // If URL has a value, use it
      if (urlValue !== null) {
        // Handle special cases
        if (key === 'debts') {
          try {
            const parsed = JSON.parse(urlValue)
            return Array.isArray(parsed) ? parsed : DEFAULTS[key]
          } catch {
            try {
              const parsed = JSON.parse(decodeURIComponent(urlValue))
              return Array.isArray(parsed) ? parsed : DEFAULTS[key]
            } catch {
              return DEFAULTS[key]
            }
          }
        }
        if (key === 'debtMode') {
          return urlValue === 'fixed' || urlValue === 'target' ? urlValue : DEFAULTS[key]
        }
        if (key === 'debtStrategy') {
          return urlValue === 'snowball' || urlValue === 'avalanche' ? urlValue : DEFAULTS[key]
        }
        if (key === 'savingsFrequency') {
          return urlValue === 'monthly' || urlValue === 'yearly' ? urlValue : DEFAULTS[key]
        }
        
        return parseNumericParam(
          urlValue,
          DEFAULTS[key] as number,
          routeBounds?.[key] ?? NUMERIC_BOUNDS[key],
        )
      }
      
      // If no URL value, try saved values for this calculator section.
      if (savedParams && key in savedParams) {
        const storedValue = savedParams[key]
        // Only use stored value if it's not undefined or null
        if (storedValue !== undefined && storedValue !== null) {
          return storedValue
        }
      }
      
      // Fall back to defaults
      return DEFAULTS[key]
    }

    const currentAge = getParam('currentAge') as number
    const maximumRetirementAge = location.pathname === '/healthcare' ? 64 : 90
    const minimumRetirementAge = location.pathname === '/healthcare'
      ? currentAge
      : currentAge + 1

    return {
      currentAge,
      retirementAge: Math.min(
        maximumRetirementAge,
        Math.max(minimumRetirementAge, getParam('retirementAge') as number),
      ),
      currentSavings: getParam('currentSavings'),
      annualContribution: getParam('annualContribution'),
      annualIncome: getParam('annualIncome'),
      expectedReturn: getParam('expectedReturn'),
      inflationRate: getParam('inflationRate'),
      withdrawalRate: getParam('withdrawalRate'),
      annualExpenses: getParam('annualExpenses'),
      partTimeIncome: getParam('partTimeIncome'),
      portfolioValue: getParam('portfolioValue'),
      retirementYears: getParam('retirementYears'),
      debts: getParam('debts'),
      debtBudget: getParam('debtBudget'),
      debtExtra: getParam('debtExtra'),
      debtMonths: getParam('debtMonths'),
      debtMode: getParam('debtMode'),
      debtStrategy: getParam('debtStrategy'),
      savingsFrequency: getParam('savingsFrequency'),
      savingsContribution: getParam('savingsContribution'),
      savingsYears: getParam('savingsYears'),
      healthcareMonthlyPremium: getParam('healthcareMonthlyPremium'),
      healthcareAnnualDeductible: getParam('healthcareAnnualDeductible'),
      healthcareAnnualOutOfPocket: getParam('healthcareAnnualOutOfPocket'),
    }
  }, [location.pathname, savedParams, searchParams])
  const params = useMemo(() => ({ ...resolvedParams, ...pendingParams }), [pendingParams, resolvedParams])

  useEffect(() => {
    pendingParamsRef.current = pendingParams
  }, [pendingParams])

  useEffect(() => () => {
    Object.values(debounceTimersRef.current).forEach(timer => {
      if (timer) clearTimeout(timer)
    })
  }, [])

  const clearPendingParam = useCallback((key: keyof CalculatorParams) => {
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

  const setParam = useCallback(<Key extends keyof CalculatorParams>(
    key: Key,
    value: CalculatorParams[Key],
  ) => {
    clearPendingParam(key)
    setSearchParams(previous => {
      const next = new URLSearchParams(previous)
      applyParamToSearchParams(next, key, value, savedParams)
      return next
    }, { replace: true })
  }, [clearPendingParam, savedParams, setSearchParams])

  const setParamDebounced = useCallback(<Key extends keyof CalculatorParams>(
    key: Key,
    value: CalculatorParams[Key],
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
      ;(Object.keys(pending) as (keyof CalculatorParams)[]).forEach(key => {
        const value = pending[key]
        if (value !== undefined) applyParamToSearchParams(next, key, value, savedParams)
      })
      return next
    }, { replace: true })
    pendingParamsRef.current = {}
    setPendingParams({})
  }, [savedParams, setSearchParams])

  const setParams = useCallback((updates: Partial<CalculatorParams>) => {
    cancelPendingParams()
    setSearchParams(previous => {
      const next = new URLSearchParams(previous)
      ;(Object.keys(updates) as (keyof CalculatorParams)[]).forEach(key => {
        const value = updates[key]
        if (value !== undefined) applyParamToSearchParams(next, key, value, savedParams)
      })
      return next
    }, { replace: true })
  }, [cancelPendingParams, savedParams, setSearchParams])

  const resetParams = useCallback(() => {
    cancelPendingParams()
    setSearchParams(new URLSearchParams(), { replace: true })
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
    setSearchParams(() => {
      const next = new URLSearchParams()
      ;(Object.keys(DEFAULTS) as (keyof CalculatorParams)[]).forEach(key => {
        const value = storedParams.params[key]
        if (value === undefined || value === null) return
        next.set(PARAM_KEYS[key], key === 'debts' ? JSON.stringify(value) : String(value))
      })
      return next
    }, { replace: true })
    setSavedParams(storedParams.params)
  }, [cancelPendingParams, setSearchParams, storedParams])

  const copyUrl = useCallback(async () => {
    const pending = pendingParamsRef.current
    const next = new URLSearchParams(searchParams)
    ;(Object.keys(pending) as (keyof CalculatorParams)[]).forEach(key => {
      const value = pending[key]
      if (value !== undefined) applyParamToSearchParams(next, key, value, savedParams)
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

  const hasCustomParams = searchParams.toString().length > 0 || Object.keys(pendingParams).length > 0
  const hasUnsavedChanges = savedParams
    ? !matchesSavedParams(params, savedParams)
    : storedParams
      ? hasCustomParams && !matchesSavedParams(params, storedParams.params)
      : !matchesSavedParams(params, null)

  return {
    params,
    setParam,
    setParamDebounced,
    setParams,
    resetParams,
    saveParams,
    copyUrl,
    hasCustomParams,
    hasUnsavedChanges,
    hasSavedParams: storedParams !== null,
    savedAt: storedParams?.savedAt ?? null,
    loadParams,
  }
}

export type { CalculatorParams }
