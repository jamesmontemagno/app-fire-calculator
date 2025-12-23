import { useSearchParams } from 'react-router-dom'
import { useCallback, useMemo } from 'react'

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
}

export function useCalculatorParams() {
  const [searchParams, setSearchParams] = useSearchParams()

  const params = useMemo((): CalculatorParams => {
    const getParam = (key: keyof CalculatorParams): number => {
      const urlKey = PARAM_KEYS[key]
      const value = searchParams.get(urlKey)
      if (value === null) return DEFAULTS[key]
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
    }
  }, [searchParams])

  const setParam = useCallback((key: keyof CalculatorParams, value: number) => {
    const urlKey = PARAM_KEYS[key]
    setSearchParams(prev => {
      const newParams = new URLSearchParams(prev)
      if (value === DEFAULTS[key]) {
        newParams.delete(urlKey)
      } else {
        newParams.set(urlKey, value.toString())
      }
      return newParams
    }, { replace: true })
  }, [setSearchParams])

  const setParams = useCallback((updates: Partial<CalculatorParams>) => {
    setSearchParams(prev => {
      const newParams = new URLSearchParams(prev)
      Object.entries(updates).forEach(([key, value]) => {
        const urlKey = PARAM_KEYS[key as keyof CalculatorParams]
        if (value === DEFAULTS[key as keyof CalculatorParams]) {
          newParams.delete(urlKey)
        } else {
          newParams.set(urlKey, value.toString())
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
    setParams,
    resetParams,
    copyUrl,
    hasCustomParams,
  }
}

export type { CalculatorParams }
