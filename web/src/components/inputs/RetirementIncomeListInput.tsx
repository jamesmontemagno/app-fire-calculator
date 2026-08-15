import { useState } from 'react'
import type {
  RetirementIncomeSource,
  RetirementIncomeType,
} from '../../utils/deferredCompensation'
import { formatCurrency } from '../../utils/calculations'
import AgeInput from './AgeInput'
import CurrencyInput from './CurrencyInput'
import PercentageInput from './PercentageInput'
import { ChevronRight, X } from 'lucide-react'

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
          <h3 className="text-sm font-semibold text-content-muted">Outside income</h3>
          <p className="text-xs text-content-subtle mt-0.5">
            Add income that reduces the amount your portfolio needs to provide.
          </p>
        </div>
        <button
          type="button"
          onClick={addSource}
          className="shrink-0 inline-flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium text-accent bg-accent-subtle hover:bg-accent-subtle-hover rounded-control transition-colors"
        >
          <span aria-hidden="true">+</span>
          Add income
        </button>
      </div>

      {sources.length === 0 ? (
        <div className="text-center py-8 bg-surface-sunken rounded-container border-2 border-dashed border-border-strong">
          <p className="text-sm font-medium text-content">No outside income sources</p>
          <p className="text-xs text-content-subtle mt-1">
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
                className="bg-surface-sunken rounded-control border border-border-subtle overflow-hidden"
              >
                <div className="flex items-center">
                  <button
                    type="button"
                    onClick={() => toggleExpanded(source.id)}
                    className="min-w-0 flex-1 px-4 py-3 flex items-center justify-between gap-3 text-left hover:bg-surface-sunken/50 transition-colors"
                    aria-expanded={expanded}
                  >
                    <span className="flex items-center gap-3 min-w-0">
                      <ChevronRight className={`w-4 h-4 shrink-0 text-content-subtle transition-transform ${expanded ? 'rotate-90' : ''}`} aria-hidden="true" strokeWidth={1.5} />
                      <span className="truncate font-medium text-content">
                        {source.name || `${typeLabel} ${index + 1}`}
                      </span>
                    </span>
                    <span className="text-right shrink-0">
                      <span className="block text-sm font-semibold text-content">
                        {formatCurrency(netAmount)}
                      </span>
                      <span className="block text-xs text-content-subtle">
                        {typeLabel} · ages {source.startAge}–{source.endAge}
                      </span>
                    </span>
                  </button>
                  <button
                    type="button"
                    onClick={() => removeSource(source.id)}
                    className="p-3 mr-1 text-content-subtle hover:text-danger transition-colors"
                    aria-label={`Remove ${source.name || `income source ${index + 1}`}`}
                  >
                    <X className="w-4 h-4" aria-hidden="true" strokeWidth={1.5} />
                  </button>
                </div>

                {expanded && (
                  <div className="px-4 pb-4 pt-3 border-t border-border-subtle grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
                    <div>
                      <label
                        htmlFor={`income-name-${source.id}`}
                        className="block text-sm font-medium text-content-muted mb-1.5"
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
                        className="w-full px-3 py-2.5 bg-surface-raised border border-border-strong rounded-control text-content focus:ring-2 focus-visible:ring-ring focus-visible:border-accent"
                      />
                    </div>
                    <div>
                      <label
                        htmlFor={`income-type-${source.id}`}
                        className="block text-sm font-medium text-content-muted mb-1.5"
                      >
                        Income type
                      </label>
                      <select
                        id={`income-type-${source.id}`}
                        value={source.type}
                        onChange={event => updateSource(source.id, 'type', event.target.value as RetirementIncomeType)}
                        className="w-full px-3 py-2.5 bg-surface-raised border border-border-strong rounded-control text-content focus:ring-2 focus-visible:ring-ring focus-visible:border-accent"
                      >
                        {INCOME_TYPES.map(type => (
                          <option key={type.value} value={type.value}>{type.label}</option>
                        ))}
                      </select>
                    </div>
                    <CurrencyInput
                      label={source.isAfterTax ? 'After-tax amount' : 'Gross amount'}
                      value={source.annualAmount}
                      onChange={value => updateSource(source.id, 'annualAmount', value)}
                      tooltip="The income expected from this source"
                      periodic
                    />
                    <AgeInput
                      label="Starts at age"
                      value={source.startAge}
                      min={currentAge}
                      onChange={value => updateSource(source.id, 'startAge', value)}
                      tooltip="The first age when this income is received"
                    />
                    <AgeInput
                      label="Ends at age"
                      value={source.endAge}
                      min={source.startAge}
                      onChange={value => updateSource(source.id, 'endAge', value)}
                      tooltip="The final age when this income is received"
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
                      <label className="flex items-center gap-2 text-sm font-medium text-content-muted">
                        <input
                          type="checkbox"
                          checked={source.isAfterTax}
                          onChange={event => updateSource(source.id, 'isAfterTax', event.target.checked)}
                          className="h-4 w-4 rounded border-border-strong text-accent focus-visible:ring-ring"
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
        <div className="flex items-center justify-between pt-3 border-t border-border-subtle">
          <span className="text-sm text-content-muted">After-tax income at current age</span>
          <span className="font-bold text-content">{formatCurrency(totalFirstYearIncome)}</span>
        </div>
      )}
    </div>
  )
}
