import { useState } from 'react'
import type {
  RetirementExpense,
  RetirementExpenseType,
} from '../../utils/deferredCompensation'
import { formatCurrency } from '../../utils/calculations'
import AgeInput from './AgeInput'
import CurrencyInput from './CurrencyInput'

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
          <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300">
            Future spending
          </h3>
          <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
            Add annual costs that begin later and stack on top of core spending.
          </p>
        </div>
        <button
          type="button"
          onClick={addExpense}
          className="shrink-0 inline-flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium text-fire-700 dark:text-fire-400 bg-fire-50 dark:bg-fire-900/30 hover:bg-fire-100 dark:hover:bg-fire-900/50 rounded-lg transition-colors"
        >
          <span aria-hidden="true">+</span>
          Add expense
        </button>
      </div>

      {expenses.length === 0 ? (
        <div className="text-center py-8 bg-gray-50 dark:bg-gray-800/50 rounded-xl border-2 border-dashed border-gray-300 dark:border-gray-700">
          <p className="text-sm font-medium text-gray-900 dark:text-gray-100">
            No additional expenses
          </p>
          <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
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
                className="bg-gray-50 dark:bg-gray-800/50 rounded-lg border border-gray-200 dark:border-gray-700 overflow-hidden"
              >
                <div className="flex items-center">
                  <button
                    type="button"
                    onClick={() => toggleExpanded(expense.id)}
                    className="min-w-0 flex-1 px-4 py-3 flex items-center justify-between gap-3 text-left hover:bg-gray-100 dark:hover:bg-gray-700/50 transition-colors"
                    aria-expanded={expanded}
                  >
                    <span className="flex items-center gap-3 min-w-0">
                      <svg
                        className={`w-4 h-4 shrink-0 text-gray-500 transition-transform ${expanded ? 'rotate-90' : ''}`}
                        fill="none"
                        viewBox="0 0 24 24"
                        stroke="currentColor"
                        aria-hidden="true"
                      >
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                      </svg>
                      <span className="truncate font-medium text-gray-900 dark:text-gray-100">
                        {expense.name || `${typeLabel} ${index + 1}`}
                      </span>
                    </span>
                    <span className="text-right shrink-0">
                      <span className="block text-sm font-semibold text-gray-900 dark:text-gray-100">
                        {formatCurrency(expense.annualAmount)}/yr
                      </span>
                      <span className="block text-xs text-gray-500 dark:text-gray-400">
                        {typeLabel} · starts at age {expense.startAge}
                      </span>
                    </span>
                  </button>
                  <button
                    type="button"
                    onClick={() => removeExpense(expense.id)}
                    className="p-3 mr-1 text-gray-400 hover:text-red-600 dark:hover:text-red-400 transition-colors"
                    aria-label={`Remove ${expense.name || `expense ${index + 1}`}`}
                  >
                    <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                    </svg>
                  </button>
                </div>

                {expanded && (
                  <div className="px-4 pb-4 pt-3 border-t border-gray-200 dark:border-gray-700 grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
                    <div>
                      <label
                        htmlFor={`expense-name-${expense.id}`}
                        className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5"
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
                        className="w-full px-3 py-2.5 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg text-gray-900 dark:text-gray-100 focus:ring-2 focus:ring-fire-500 focus:border-fire-500"
                      />
                    </div>
                    <div>
                      <label
                        htmlFor={`expense-type-${expense.id}`}
                        className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5"
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
                        className="w-full px-3 py-2.5 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg text-gray-900 dark:text-gray-100 focus:ring-2 focus:ring-fire-500 focus:border-fire-500"
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
                    <AgeInput
                      label="Starts at age"
                      value={expense.startAge}
                      onChange={value => updateExpense(expense.id, 'startAge', value)}
                      tooltip="This annual expense is included from this age through the end of the plan."
                    />
                  </div>
                )}
              </div>
            )
          })}
        </div>
      )}

      {expenses.length > 0 && (
        <div className="flex items-center justify-between pt-3 border-t border-gray-200 dark:border-gray-700">
          <span className="text-sm text-gray-600 dark:text-gray-400">Additional annual spending</span>
          <span className="font-bold text-gray-900 dark:text-gray-100">
            {formatCurrency(totalAnnualAmount)}
          </span>
        </div>
      )}
    </div>
  )
}
