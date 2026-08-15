import { createContext, useContext, useMemo, type ReactNode } from 'react'
import { DEFAULT_CURRENCY_PERIOD, type CurrencyPeriod } from '../../utils/currencyPeriod'

interface CurrencyPeriodContextValue {
  period: CurrencyPeriod
  setPeriod: (period: CurrencyPeriod) => void
}

/**
 * The monthly/annual preference is shared by every recurring money field on a calculator rather
 * than kept per field, so a card can never mix units and a shared link always reproduces what the
 * sender saw. The default keeps un-wrapped fields on annual.
 */
const CurrencyPeriodContext = createContext<CurrencyPeriodContextValue>({
  period: DEFAULT_CURRENCY_PERIOD,
  setPeriod: () => {},
})

interface CurrencyPeriodProviderProps {
  period: CurrencyPeriod
  onChange: (period: CurrencyPeriod) => void
  children: ReactNode
}

export function CurrencyPeriodProvider({ period, onChange, children }: CurrencyPeriodProviderProps) {
  const value = useMemo(() => ({ period, setPeriod: onChange }), [period, onChange])
  return <CurrencyPeriodContext.Provider value={value}>{children}</CurrencyPeriodContext.Provider>
}

export function useCurrencyPeriodContext(): CurrencyPeriodContextValue {
  return useContext(CurrencyPeriodContext)
}
