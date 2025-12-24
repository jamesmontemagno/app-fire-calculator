import { useState } from 'react'
import type { DebtItem } from '../../utils/calculations'
import { CurrencyInput, PercentageInput } from './index'
import { formatCurrency, formatPercent } from '../../utils/calculations'

interface DebtListInputProps {
  debts: DebtItem[]
  onChange: (debts: DebtItem[]) => void
}

export default function DebtListInput({ debts, onChange }: DebtListInputProps) {
  const [expandedIds, setExpandedIds] = useState<Set<string>>(new Set())

  const toggleExpanded = (id: string) => {
    setExpandedIds(prev => {
      const next = new Set(prev)
      if (next.has(id)) {
        next.delete(id)
      } else {
        next.add(id)
      }
      return next
    })
  }

  const addDebt = () => {
    const newDebt: DebtItem = {
      id: Date.now().toString(),
      name: '',
      balance: 0,
      rate: 0,
      minPayment: 0,
    }
    // Collapse all existing debts, expand the new one
    setExpandedIds(new Set([newDebt.id]))
    onChange([...debts, newDebt])
  }

  const removeDebt = (id: string) => {
    setExpandedIds(prev => {
      const next = new Set(prev)
      next.delete(id)
      return next
    })
    onChange(debts.filter(d => d.id !== id))
  }

  const updateDebt = (id: string, field: keyof DebtItem, value: string | number) => {
    onChange(
      debts.map(d =>
        d.id === id ? { ...d, [field]: value } : d
      )
    )
  }

  const totalDebt = debts.reduce((sum, d) => sum + d.balance, 0)
  const totalMinPayments = debts.reduce((sum, d) => sum + d.minPayment, 0)

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between mb-2">
        <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300">Your Debts</h3>
        <button
          onClick={addDebt}
          className="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium text-fire-700 dark:text-fire-400 bg-fire-50 dark:bg-fire-900/30 hover:bg-fire-100 dark:hover:bg-fire-900/50 rounded-lg transition-colors"
        >
          <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
          </svg>
          Add Debt
        </button>
      </div>

      {debts.length === 0 ? (
        <div className="text-center py-12 bg-gray-50 dark:bg-gray-800/50 rounded-xl border-2 border-dashed border-gray-300 dark:border-gray-700">
          <div className="text-4xl mb-3">💳</div>
          <h4 className="text-sm font-medium text-gray-900 dark:text-gray-100 mb-1">No debts added yet</h4>
          <p className="text-xs text-gray-500 dark:text-gray-400 mb-4">
            Click "Add Debt" to start tracking your debt payoff journey
          </p>
        </div>
      ) : (
        <div className="space-y-2">
          {debts.map((debt, index) => {
            const isExpanded = expandedIds.has(debt.id)
            const debtName = debt.name || `Debt #${index + 1}`
            
            return (
              <div
                key={debt.id}
                className="bg-gray-50 dark:bg-gray-800/50 rounded-lg border border-gray-200 dark:border-gray-700 overflow-hidden"
              >
                {/* Collapsed Header - Always visible */}
                <button
                  onClick={() => toggleExpanded(debt.id)}
                  className="w-full px-4 py-3 flex items-center justify-between hover:bg-gray-100 dark:hover:bg-gray-700/50 transition-colors"
                >
                  <div className="flex items-center gap-3">
                    <svg 
                      className={`w-4 h-4 text-gray-500 transition-transform ${isExpanded ? 'rotate-90' : ''}`} 
                      fill="none" 
                      viewBox="0 0 24 24" 
                      stroke="currentColor"
                    >
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                    </svg>
                    <span className="font-medium text-gray-900 dark:text-gray-100">
                      {debtName}
                    </span>
                  </div>
                  <div className="flex items-center gap-4">
                    {!isExpanded && debt.balance > 0 && (
                      <div className="text-right">
                        <div className="text-sm font-semibold text-red-600 dark:text-red-400">
                          {formatCurrency(debt.balance)}
                        </div>
                        <div className="text-xs text-gray-500 dark:text-gray-400">
                          {formatPercent(debt.rate)} APR
                        </div>
                      </div>
                    )}
                    <button
                      onClick={(e) => {
                        e.stopPropagation()
                        removeDebt(debt.id)
                      }}
                      className="p-1 text-gray-400 hover:text-red-600 dark:hover:text-red-400 transition-colors"
                      aria-label="Remove debt"
                    >
                      <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                      </svg>
                    </button>
                  </div>
                </button>

                {/* Expanded Content */}
                {isExpanded && (
                  <div className="px-4 pb-4 pt-2 border-t border-gray-200 dark:border-gray-700 space-y-3">
                    <div>
                      <label className="block text-xs font-medium text-gray-700 dark:text-gray-300 mb-1">
                        Debt Name
                      </label>
                      <input
                        type="text"
                        value={debt.name}
                        onChange={(e) => updateDebt(debt.id, 'name', e.target.value)}
                        placeholder="e.g., Credit Card, Car Loan"
                        className="w-full px-3 py-2 text-sm bg-white dark:bg-gray-900 border border-gray-300 dark:border-gray-600 rounded-lg focus:ring-2 focus:ring-fire-500 focus:border-transparent text-gray-900 dark:text-gray-100 placeholder-gray-400"
                      />
                    </div>

                    <CurrencyInput
                      label="Current Balance"
                      value={debt.balance}
                      onChange={(value) => updateDebt(debt.id, 'balance', value)}
                      min={0}
                    />

                    <PercentageInput
                      label="Annual Interest Rate"
                      value={debt.rate}
                      onChange={(value) => updateDebt(debt.id, 'rate', value)}
                      min={0}
                      max={0.5}
                      step={0.0025}
                    />

                    <CurrencyInput
                      label="Minimum Monthly Payment"
                      value={debt.minPayment}
                      onChange={(value) => updateDebt(debt.id, 'minPayment', value)}
                      min={0}
                    />
                  </div>
                )}
              </div>
            )
          })}
        </div>
      )}

      {debts.length > 0 && (
        <div className="mt-4 pt-4 border-t border-gray-200 dark:border-gray-700">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <div className="text-xs text-gray-600 dark:text-gray-400 mb-1">Total Debt</div>
              <div className="text-lg font-bold text-red-600 dark:text-red-400">
                {formatCurrency(totalDebt)}
              </div>
            </div>
            <div>
              <div className="text-xs text-gray-600 dark:text-gray-400 mb-1">Total Min. Payments</div>
              <div className="text-lg font-bold text-gray-900 dark:text-gray-100">
                {formatCurrency(totalMinPayments)}/mo
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
