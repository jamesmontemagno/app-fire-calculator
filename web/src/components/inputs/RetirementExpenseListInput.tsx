import { useState } from 'react'
import type {
  RetirementExpense,
  RetirementExpenseType,
} from '../../utils/deferredCompensation'
import { formatCurrency } from '../../utils/calculations'
import AgeInput from './AgeInput'
import CurrencyInput from './CurrencyInput'
import { ChevronRight, X } from 'lucide-react'

interface RetirementExpenseListInputProps {
  expenses: RetirementExpense[]
  onChange: (expenses: RetirementExpense[]) => void
  currentAge: number
}

const EXPENSE_TYPES: { value: RetirementExpenseType; label: string }[] = [
  { value: 'healthcare', label: 'Healthcare' },
  { value: 'travel', label: 'Travel' },
  { value: 'housing', label: 'Housing' },
  { value: 'family', label: 'Family support' },
  { value: 'education', label: 'Education' },
  { value: 'long-term-care', label: 'Long-term care' },
  { value: 'custom', label: 'Other' },
]

export default function RetirementExpenseListInput({
  expenses,
  onChange,
  currentAge,
}: RetirementExpenseListInputProps) {
  const [expandedIds, setExpandedIds] = useState<Set<string>>(new Set())

  const updateExpense = <Key extends keyof RetirementExpense>(
    id: string,
    key: Key,
    value: RetirementExpense[Key],
  ) => {
    onChange(expenses.map(expense => (
      expense.id === id ? { ...expense, [key]: value } : expense
    )))
  }

  const addExpense = () => {
    const id = `expense-${Date.now()}-${expenses.length}`
    onChange([...expenses, {
      id,
      name: '',
      type: 'healthcare',
      annualAmount: 0,
      startAge: currentAge,
      endAge: Math.max(currentAge, 90),
    }])
    setExpandedIds(new Set([id]))
  }

  const removeExpense = (id: string) => {
    setExpandedIds(previous => {
      const next = new Set(previous)
      next.delete(id)
      return next
    })
    onChange(expenses.filter(expense => expense.id !== id))
  }

  const toggleExpanded = (id: string) => {
    setExpandedIds(previous => {
      const next = new Set(previous)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }

  const totalAnnualAmount = expenses.reduce((sum, expense) => sum + expense.annualAmount, 0)

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-3">
        <div>
          <h3 className="text-sm font-semibold text-content-muted">
            Future spending
          </h3>
          <p className="text-xs text-content-subtle mt-0.5">
            Add annual costs that begin later and stack on top of core spending.
          </p>
        </div>
        <button
          type="button"
          onClick={addExpense}
          className="shrink-0 inline-flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium text-accent bg-accent-subtle hover:bg-accent-subtle-hover rounded-control transition-colors"
        >
          <span aria-hidden="true">+</span>
          Add expense
        </button>
      </div>

      {expenses.length === 0 ? (
        <div className="text-center py-8 bg-surface-sunken rounded-container border-2 border-dashed border-border-strong">
          <p className="text-sm font-medium text-content">
            No additional expenses
          </p>
          <p className="text-xs text-content-subtle mt-1">
            The projection currently uses core annual spending only.
          </p>
        </div>
      ) : (
        <div className="space-y-2">
          {expenses.map((expense, index) => {
            const expanded = expandedIds.has(expense.id)
            const typeLabel = EXPENSE_TYPES.find(type => type.value === expense.type)?.label

            return (
              <div
                key={expense.id}
                className="bg-surface-sunken rounded-control border border-border-subtle overflow-hidden"
              >
                <div className="flex items-center">
                  <button
                    type="button"
                    onClick={() => toggleExpanded(expense.id)}
                    className="min-w-0 flex-1 px-4 py-3 flex items-center justify-between gap-3 text-left hover:bg-surface-sunken/50 transition-colors"
                    aria-expanded={expanded}
                  >
                    <span className="flex items-center gap-3 min-w-0">
                      <ChevronRight className={`w-4 h-4 shrink-0 text-content-subtle transition-transform ${expanded ? 'rotate-90' : ''}`} aria-hidden="true" strokeWidth={1.5} />
                      <span className="truncate font-medium text-content">
                        {expense.name || `${typeLabel} ${index + 1}`}
                      </span>
                    </span>
                    <span className="text-right shrink-0">
                      <span className="block text-sm font-semibold text-content">
                        {formatCurrency(expense.annualAmount)}/yr
                      </span>
                      <span className="block text-xs text-content-subtle">
                        {typeLabel} · ages {expense.startAge}–{expense.endAge}
                      </span>
                    </span>
                  </button>
                  <button
                    type="button"
                    onClick={() => removeExpense(expense.id)}
                    className="p-3 mr-1 text-content-subtle hover:text-danger transition-colors"
                    aria-label={`Remove ${expense.name || `expense ${index + 1}`}`}
                  >
                    <X className="w-4 h-4" aria-hidden="true" strokeWidth={1.5} />
                  </button>
                </div>

                {expanded && (
                  <div className="px-4 pb-4 pt-3 border-t border-border-subtle space-y-4">
                    <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
                      <div>
                        <label
                          htmlFor={`expense-name-${expense.id}`}
                          className="block text-sm font-medium text-content-muted mb-1.5"
                        >
                          Expense name
                        </label>
                        <input
                          id={`expense-name-${expense.id}`}
                          type="text"
                          maxLength={80}
                          value={expense.name}
                          onChange={event => updateExpense(expense.id, 'name', event.target.value)}
                          placeholder="e.g., Medicare premiums"
                          className="w-full px-3 py-2.5 bg-surface-raised border border-border-strong rounded-control text-content focus:ring-2 focus-visible:ring-ring focus-visible:border-accent"
                        />
                      </div>
                      <div>
                        <label
                          htmlFor={`expense-type-${expense.id}`}
                          className="block text-sm font-medium text-content-muted mb-1.5"
                        >
                          Expense type
                        </label>
                        <select
                          id={`expense-type-${expense.id}`}
                          value={expense.type}
                          onChange={event => updateExpense(
                            expense.id,
                            'type',
                            event.target.value as RetirementExpenseType,
                          )}
                          className="w-full px-3 py-2.5 bg-surface-raised border border-border-strong rounded-control text-content focus:ring-2 focus-visible:ring-ring focus-visible:border-accent"
                        >
                          {EXPENSE_TYPES.map(type => (
                            <option key={type.value} value={type.value}>{type.label}</option>
                          ))}
                        </select>
                      </div>
                      <CurrencyInput
                        label="Amount"
                        value={expense.annualAmount}
                        onChange={value => updateExpense(expense.id, 'annualAmount', value)}
                        tooltip="Enter today’s cost. It grows with the scenario inflation rate."
                        periodic
                      />
                    </div>
                    <div className="grid sm:grid-cols-2 gap-4">
                      <AgeInput
                        label="Starts at age"
                        value={expense.startAge}
                        onChange={value => updateExpense(expense.id, 'startAge', value)}
                        tooltip="The first age when this annual expense is included."
                      />
                      <AgeInput
                        label="Ends at age"
                        value={expense.endAge}
                        onChange={value => updateExpense(expense.id, 'endAge', value)}
                        tooltip="The last age when this annual expense is included."
                      />
                    </div>
                  </div>
                )}
              </div>
            )
          })}
        </div>
      )}

      {expenses.length > 0 && (
        <div className="flex items-center justify-between pt-3 border-t border-border-subtle">
          <span className="text-sm text-content-muted">Additional annual spending</span>
          <span className="font-bold text-content">
            {formatCurrency(totalAnnualAmount)}
          </span>
        </div>
      )}
    </div>
  )
}
