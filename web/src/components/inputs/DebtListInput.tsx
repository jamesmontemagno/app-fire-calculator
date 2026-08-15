import { useState } from 'react'
import { CreditCard } from 'lucide-react'
import type { DebtItem } from '../../utils/calculations'
import { CurrencyInput, PercentageInput } from './index'
import { formatCurrency, formatPercent } from '../../utils/calculations'
import { ChevronRight, Plus, X } from 'lucide-react'

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
        <h3 className="text-sm font-semibold text-content-muted">Your Debts</h3>
        <button
          onClick={addDebt}
          className="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium text-accent bg-accent-subtle hover:bg-accent-subtle-hover rounded-control transition-colors"
        >
          <Plus className="w-4 h-4" strokeWidth={1.5} aria-hidden="true" />
          Add Debt
        </button>
      </div>

      {debts.length === 0 ? (
        <div className="text-center py-12 bg-surface-sunken rounded-container border-2 border-dashed border-border-strong">
          <CreditCard className="mx-auto mb-3 h-8 w-8 text-content-subtle" aria-hidden="true" strokeWidth={1.5} />
          <h4 className="text-sm font-medium text-content mb-1">No debts added yet</h4>
          <p className="text-xs text-content-subtle mb-4">
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
                className="bg-surface-sunken rounded-control border border-border-subtle overflow-hidden"
              >
                {/* Collapsed Header - Always visible */}
                <button
                  onClick={() => toggleExpanded(debt.id)}
                  className="w-full px-4 py-3 flex items-center justify-between hover:bg-surface-sunken/50 transition-colors"
                >
                  <div className="flex items-center gap-3">
                    <ChevronRight className={`w-4 h-4 text-content-subtle transition-transform ${isExpanded ? 'rotate-90' : ''}`} strokeWidth={1.5} aria-hidden="true" />
                    <span className="font-medium text-content">
                      {debtName}
                    </span>
                  </div>
                  <div className="flex items-center gap-4">
                    {!isExpanded && debt.balance > 0 && (
                      <div className="text-right">
                        <div className="text-sm font-semibold text-danger">
                          {formatCurrency(debt.balance)}
                        </div>
                        <div className="text-xs text-content-subtle">
                          {formatPercent(debt.rate)} APR
                        </div>
                      </div>
                    )}
                    <button
                      onClick={(e) => {
                        e.stopPropagation()
                        removeDebt(debt.id)
                      }}
                      className="p-1 text-content-subtle hover:text-danger transition-colors"
                      aria-label="Remove debt"
                    >
                      <X className="w-4 h-4" strokeWidth={1.5} aria-hidden="true" />
                    </button>
                  </div>
                </button>

                {/* Expanded Content */}
                {isExpanded && (
                  <div className="px-4 pb-4 pt-2 border-t border-border-subtle space-y-3">
                    <div>
                      <label className="block text-xs font-medium text-content-muted mb-1">
                        Debt Name
                      </label>
                      <input
                        type="text"
                        value={debt.name}
                        onChange={(e) => updateDebt(debt.id, 'name', e.target.value)}
                        placeholder="e.g., Credit Card, Car Loan"
                        className="w-full px-3 py-2 text-sm bg-surface-raised border border-border-strong rounded-control focus:ring-2 focus-visible:ring-ring focus:border-transparent text-content placeholder-content-subtle"
                      />
                    </div>

                    <CurrencyInput
                      label="Current Balance"
                      value={debt.balance}
                      onChange={(value) => updateDebt(debt.id, 'balance', value)}
                      min={0}
                      tooltip="The amount currently owed on this debt"
                    />

                    <PercentageInput
                      label="Annual Interest Rate"
                      value={debt.rate}
                      onChange={(value) => updateDebt(debt.id, 'rate', value)}
                      min={0}
                      max={0.5}
                      step={0.0025}
                      tooltip="The debt's annual percentage rate (APR)"
                    />

                    <CurrencyInput
                      label="Minimum Monthly Payment"
                      value={debt.minPayment}
                      onChange={(value) => updateDebt(debt.id, 'minPayment', value)}
                      min={0}
                      tooltip="The lender's required payment each month"
                    />
                  </div>
                )}
              </div>
            )
          })}
        </div>
      )}

      {debts.length > 0 && (
        <div className="mt-4 pt-4 border-t border-border-subtle">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <div className="text-xs text-content-muted mb-1">Total Debt</div>
              <div className="text-lg font-bold text-danger">
                {formatCurrency(totalDebt)}
              </div>
            </div>
            <div>
              <div className="text-xs text-content-muted mb-1">Total Min. Payments</div>
              <div className="text-lg font-bold text-content">
                {formatCurrency(totalMinPayments)}/mo
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
