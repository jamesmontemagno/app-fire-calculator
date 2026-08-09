import { useState } from 'react'
import type {
  RetirementIncomeSource,
  RetirementIncomeType,
} from '../../utils/deferredCompensation'
import { formatCurrency } from '../../utils/calculations'
import AgeInput from './AgeInput'
import CurrencyInput from './CurrencyInput'
import PercentageInput from './PercentageInput'

interface RetirementIncomeListInputProps {
  sources: RetirementIncomeSource[]
  onChange: (sources: RetirementIncomeSource[]) => void
  currentAge: number
}

const INCOME_TYPES: { value: RetirementIncomeType; label: string }[] = [
  { value: 'salary', label: 'Salary / wages' },
  { value: 'pension', label: 'Pension' },
  { value: 'social-security', label: 'Social Security' },
  { value: 'rental', label: 'Rental income' },
  { value: 'custom', label: 'Custom income' },
]

export default function RetirementIncomeListInput({
  sources,
  onChange,
  currentAge,
}: RetirementIncomeListInputProps) {
  const [expandedIds, setExpandedIds] = useState<Set<string>>(new Set())

  const updateSource = <Key extends keyof RetirementIncomeSource>(
    id: string,
    key: Key,
    value: RetirementIncomeSource[Key],
  ) => {
    onChange(sources.map(source => (
      source.id === id ? { ...source, [key]: value } : source
    )))
  }

  const addSource = () => {
    const id = `income-${Date.now()}-${sources.length}`
    onChange([...sources, {
      id,
      name: '',
      type: 'custom',
      annualAmount: 0,
      startAge: currentAge,
      endAge: 100,
      annualGrowth: 0,
      isAfterTax: true,
      taxRate: 0.25,
    }])
    setExpandedIds(new Set([id]))
  }

  const removeSource = (id: string) => {
    setExpandedIds(previous => {
      const next = new Set(previous)
      next.delete(id)
      return next
    })
    onChange(sources.filter(source => source.id !== id))
  }

  const toggleExpanded = (id: string) => {
    setExpandedIds(previous => {
      const next = new Set(previous)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }

  const totalFirstYearIncome = sources
    .filter(source => source.startAge <= currentAge && source.endAge >= currentAge)
    .reduce((sum, source) => sum + (
      source.isAfterTax ? source.annualAmount : source.annualAmount * (1 - source.taxRate)
    ), 0)

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-3">
        <div>
          <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300">Outside income</h3>
          <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
            Add income that reduces the amount your portfolio needs to provide.
          </p>
        </div>
        <button
          type="button"
          onClick={addSource}
          className="shrink-0 inline-flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium text-fire-700 dark:text-fire-400 bg-fire-50 dark:bg-fire-900/30 hover:bg-fire-100 dark:hover:bg-fire-900/50 rounded-lg transition-colors"
        >
          <span aria-hidden="true">+</span>
          Add income
        </button>
      </div>

      {sources.length === 0 ? (
        <div className="text-center py-8 bg-gray-50 dark:bg-gray-800/50 rounded-xl border-2 border-dashed border-gray-300 dark:border-gray-700">
          <p className="text-sm font-medium text-gray-900 dark:text-gray-100">No outside income sources</p>
          <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
            Your portfolio will cover all retirement expenses.
          </p>
        </div>
      ) : (
        <div className="space-y-2">
          {sources.map((source, index) => {
            const expanded = expandedIds.has(source.id)
            const typeLabel = INCOME_TYPES.find(type => type.value === source.type)?.label
            const netAmount = source.isAfterTax
              ? source.annualAmount
              : source.annualAmount * (1 - source.taxRate)

            return (
              <div
                key={source.id}
                className="bg-gray-50 dark:bg-gray-800/50 rounded-lg border border-gray-200 dark:border-gray-700 overflow-hidden"
              >
                <div className="flex items-center">
                  <button
                    type="button"
                    onClick={() => toggleExpanded(source.id)}
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
                        {source.name || `${typeLabel} ${index + 1}`}
                      </span>
                    </span>
                    <span className="text-right shrink-0">
                      <span className="block text-sm font-semibold text-gray-900 dark:text-gray-100">
                        {formatCurrency(netAmount)}
                      </span>
                      <span className="block text-xs text-gray-500 dark:text-gray-400">
                        {typeLabel} · ages {source.startAge}–{source.endAge}
                      </span>
                    </span>
                  </button>
                  <button
                    type="button"
                    onClick={() => removeSource(source.id)}
                    className="p-3 mr-1 text-gray-400 hover:text-red-600 dark:hover:text-red-400 transition-colors"
                    aria-label={`Remove ${source.name || `income source ${index + 1}`}`}
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
                        htmlFor={`income-name-${source.id}`}
                        className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5"
                      >
                        Income name
                      </label>
                      <input
                        id={`income-name-${source.id}`}
                        type="text"
                        maxLength={80}
                        value={source.name}
                        onChange={event => updateSource(source.id, 'name', event.target.value)}
                        placeholder="e.g., Partner's salary"
                        className="w-full px-3 py-2.5 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg text-gray-900 dark:text-gray-100 focus:ring-2 focus:ring-fire-500 focus:border-fire-500"
                      />
                    </div>
                    <div>
                      <label
                        htmlFor={`income-type-${source.id}`}
                        className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5"
                      >
                        Income type
                      </label>
                      <select
                        id={`income-type-${source.id}`}
                        value={source.type}
                        onChange={event => updateSource(source.id, 'type', event.target.value as RetirementIncomeType)}
                        className="w-full px-3 py-2.5 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg text-gray-900 dark:text-gray-100 focus:ring-2 focus:ring-fire-500 focus:border-fire-500"
                      >
                        {INCOME_TYPES.map(type => (
                          <option key={type.value} value={type.value}>{type.label}</option>
                        ))}
                      </select>
                    </div>
                    <CurrencyInput
                      label={source.isAfterTax ? 'Annual after-tax amount' : 'Annual gross amount'}
                      value={source.annualAmount}
                      onChange={value => updateSource(source.id, 'annualAmount', value)}
                    />
                    <AgeInput
                      label="Starts at age"
                      value={source.startAge}
                      min={currentAge}
                      onChange={value => updateSource(source.id, 'startAge', value)}
                    />
                    <AgeInput
                      label="Ends at age"
                      value={source.endAge}
                      min={source.startAge}
                      onChange={value => updateSource(source.id, 'endAge', value)}
                    />
                    <PercentageInput
                      label="Annual growth"
                      value={source.annualGrowth}
                      min={0}
                      max={0.2}
                      onChange={value => updateSource(source.id, 'annualGrowth', value)}
                      tooltip="Defaults to 0%; growth begins from your current age."
                    />
                    <div className="sm:col-span-2 lg:col-span-3 space-y-3">
                      <label className="flex items-center gap-2 text-sm font-medium text-gray-700 dark:text-gray-300">
                        <input
                          type="checkbox"
                          checked={source.isAfterTax}
                          onChange={event => updateSource(source.id, 'isAfterTax', event.target.checked)}
                          className="h-4 w-4 rounded border-gray-300 text-fire-600 focus:ring-fire-500"
                        />
                        Amount is already after tax
                      </label>
                      {!source.isAfterTax && (
                        <PercentageInput
                          label="Effective tax rate"
                          value={source.taxRate}
                          min={0}
                          max={0.6}
                          onChange={value => updateSource(source.id, 'taxRate', value)}
                          tooltip="Applied only to this source to estimate the after-tax amount."
                        />
                      )}
                    </div>
                  </div>
                )}
              </div>
            )
          })}
        </div>
      )}

      {sources.length > 0 && (
        <div className="flex items-center justify-between pt-3 border-t border-gray-200 dark:border-gray-700">
          <span className="text-sm text-gray-600 dark:text-gray-400">After-tax income at current age</span>
          <span className="font-bold text-gray-900 dark:text-gray-100">{formatCurrency(totalFirstYearIncome)}</span>
        </div>
      )}
    </div>
  )
}
