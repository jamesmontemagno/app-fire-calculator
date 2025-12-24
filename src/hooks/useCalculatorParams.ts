import { useSearchParams } from 'react-router-dom'
import { useCallback, useMemo, useRef } from 'react'

import type { DebtItem } from '../utils/calculations'

interface CalculatorParams {
  currentAge: number
  retirementAge: number
  currentSavings: number
  annualContribution: number
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
}

const DEFAULTS: CalculatorParams = {
  currentAge: 30,
  retirementAge: 55,
  currentSavings: 100000,
  annualContribution: 24000,
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
}

const PARAM_KEYS: Record<keyof CalculatorParams, string> = {
  currentAge: 'age',
  retirementAge: 'retire',
  currentSavings: 'savings',
  annualContribution: 'contrib',
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
}

export function useCalculatorParams() {
  const [searchParams, setSearchParams] = useSearchParams()
  const debounceTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  const params = useMemo((): CalculatorParams => {
    const getParam = (key: keyof CalculatorParams): any => {
      const urlKey = PARAM_KEYS[key]
      const value = searchParams.get(urlKey)
      if (value === null) return DEFAULTS[key]
      
      // Handle special cases
      if (key === 'debts') {
        try {
          const parsed = JSON.parse(decodeURIComponent(value))
          return Array.isArray(parsed) ? parsed : DEFAULTS[key]
        } catch {
          return DEFAULTS[key]
        }
      }
      if (key === 'debtMode') {
        return value === 'fixed' || value === 'target' ? value : DEFAULTS[key]
      }
      if (key === 'debtStrategy') {
        return value === 'snowball' || value === 'avalanche' ? value : DEFAULTS[key]
      }
      
      const parsed = parseFloat(value)
      return isNaN(parsed) ? DEFAULTS[key] : parsed
    }

    return {
      currentAge: getParam('currentAge'),
      retirementAge: getParam('retirementAge'),
      currentSavings: getParam('currentSavings'),
      annualContribution: getParam('annualContribution'),
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
    }
  }, [searchParams])

  const setParam = useCallback((key: keyof CalculatorParams, value: any) => {
    const urlKey = PARAM_KEYS[key]
    setSearchParams(prev => {
      const newParams = new URLSearchParams(prev)
      const defaultValue = DEFAULTS[key]
      
      // Compare with defaults
      const isDefault = key === 'debts'
        ? JSON.stringify(value) === JSON.stringify(defaultValue)
        : value === defaultValue
      
      if (isDefault) {
        newParams.delete(urlKey)
      } else {
        const stringValue = key === 'debts'
          ? encodeURIComponent(JSON.stringify(value))
          : value.toString()
        newParams.set(urlKey, stringValue)
      }
      return newParams
    }, { replace: true })
  }, [setSearchParams])

  // Debounced version of setParam for high-frequency updates (like slider inputs)
  const setParamDebounced = useCallback((key: keyof CalculatorParams, value: any, delay = 300) => {
    // Clear existing timer
    if (debounceTimerRef.current) {
      clearTimeout(debounceTimerRef.current)
    }
    
    // Set new timer
    debounceTimerRef.current = setTimeout(() => {
      setParam(key, value)
    }, delay)
  }, [setParam])

  const setParams = useCallback((updates: Partial<CalculatorParams>) => {
    setSearchParams(prev => {
      const newParams = new URLSearchParams(prev)
      Object.entries(updates).forEach(([key, value]) => {
        const typedKey = key as keyof CalculatorParams
        const urlKey = PARAM_KEYS[typedKey]
        const defaultValue = DEFAULTS[typedKey]
        
        const isDefault = key === 'debts'
          ? JSON.stringify(value) === JSON.stringify(defaultValue)
          : value === defaultValue
        
        if (isDefault) {
          newParams.delete(urlKey)
        } else {
          const stringValue = key === 'debts'
            ? encodeURIComponent(JSON.stringify(value))
            : value.toString()
          newParams.set(urlKey, stringValue)
        }
      })
      return newParams
    }, { replace: true })
  }, [setSearchParams])

  const resetParams = useCallback(() => {
    setSearchParams(new URLSearchParams(), { replace: true })
  }, [setSearchParams])

  const copyUrl = useCallback(async () => {
    try {
      await navigator.clipboard.writeText(window.location.href)
      return true
    } catch {
      return false
    }
  }, [])

  const hasCustomParams = searchParams.toString().length > 0

  return {
    params,
    setParam,
    setParamDebounced,
    setParams,
    resetParams,
    copyUrl,
    hasCustomParams,
  }
}

export type { CalculatorParams }
