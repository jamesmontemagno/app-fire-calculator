import { useState } from 'react'
import type {
  RetirementAccount,
  RetirementAccountType,
} from '../../utils/deferredCompensation'
import { formatCurrency } from '../../utils/calculations'
import AgeInput from './AgeInput'
import CurrencyInput from './CurrencyInput'
import PercentageInput from './PercentageInput'
import InputGroup from './InputGroup'

interface RetirementAccountListInputProps {
  accounts: RetirementAccount[]
  onChange: (accounts: RetirementAccount[]) => void
  currentAge: number
  currentYear: number
}

const ACCOUNT_TYPES: { value: RetirementAccountType; label: string }[] = [
  { value: 'deferred', label: 'Deferred compensation' },
  { value: 'traditional', label: '401(k) / 403(b) / Traditional IRA' },
  { value: 'roth', label: 'Roth IRA / Roth 401(k)' },
  { value: 'taxable', label: 'Taxable portfolio' },
  { value: 'savings', label: 'Savings / cash' },
  { value: 'hsa', label: 'Health Savings Account (HSA)' },
  { value: 'other', label: 'Other account' },
]

export default function RetirementAccountListInput({
  accounts,
  onChange,
  currentAge,
  currentYear,
}: RetirementAccountListInputProps) {
  const [expandedIds, setExpandedIds] = useState<Set<string>>(new Set())

  const updateAccount = <Key extends keyof RetirementAccount>(
    id: string,
    key: Key,
    value: RetirementAccount[Key],
  ) => {
    onChange(accounts.map(account => (
      account.id === id ? { ...account, [key]: value } : account
    )))
  }

  const addAccount = () => {
    const id = `account-${Date.now()}-${accounts.length}`
    const account: RetirementAccount = {
      id,
      name: '',
      type: 'taxable',
      balance: 0,
      annualContribution: 0,
      annualReturn: 0.07,
      availableAge: 55,
      withdrawalRate: 0.04,
      payoutYears: 1,
    }
    setExpandedIds(new Set([id]))
    onChange([...accounts, account])
  }

  const removeAccount = (id: string) => {
    setExpandedIds(previous => {
      const next = new Set(previous)
      next.delete(id)
      return next
    })
    onChange(accounts.filter(account => account.id !== id))
  }

  const toggleExpanded = (id: string) => {
    setExpandedIds(previous => {
      const next = new Set(previous)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }

  const totalBalance = accounts.reduce((sum, account) => sum + account.balance, 0)

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-3">
        <div>
          <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300">
            Retirement buckets
          </h3>
          <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
            Each account can have its own availability and withdrawal rules.
          </p>
        </div>
        <button
          type="button"
          onClick={addAccount}
          className="shrink-0 inline-flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium text-fire-700 dark:text-fire-400 bg-fire-50 dark:bg-fire-900/30 hover:bg-fire-100 dark:hover:bg-fire-900/50 rounded-lg transition-colors"
        >
          <span aria-hidden="true">+</span>
          Add account
        </button>
      </div>

      {accounts.length === 0 ? (
        <div className="text-center py-10 bg-gray-50 dark:bg-gray-800/50 rounded-xl border-2 border-dashed border-gray-300 dark:border-gray-700">
          <div className="text-3xl mb-2" aria-hidden="true">🪣</div>
          <p className="text-sm font-medium text-gray-900 dark:text-gray-100">
            Add an account to build your retirement cash flow
          </p>
        </div>
      ) : (
        <div className="space-y-2">
          {accounts.map((account, index) => {
            const expanded = expandedIds.has(account.id)
            const typeLabel = ACCOUNT_TYPES.find(type => type.value === account.type)?.label

            return (
              <div
                key={account.id}
                className="bg-gray-50 dark:bg-gray-800/50 rounded-lg border border-gray-200 dark:border-gray-700 overflow-hidden"
              >
                <div className="flex items-center">
                  <button
                    type="button"
                    onClick={() => toggleExpanded(account.id)}
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
                        {account.name || `Account ${index + 1}`}
                      </span>
                    </span>
                    <span className="text-right shrink-0">
                      <span className="block text-sm font-semibold text-gray-900 dark:text-gray-100">
                        {formatCurrency(account.balance)}
                      </span>
                      <span className="block text-xs text-gray-500 dark:text-gray-400">
                        {typeLabel}
                      </span>
                    </span>
                  </button>
                  <button
                    type="button"
                    onClick={() => removeAccount(account.id)}
                    className="p-3 mr-1 text-gray-400 hover:text-red-600 dark:hover:text-red-400 transition-colors"
                    aria-label={`Remove ${account.name || `account ${index + 1}`}`}
                  >
                    <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                    </svg>
                  </button>
                </div>

                {expanded && (
                  <div className="px-4 pb-4 pt-3 border-t border-gray-200 dark:border-gray-700 grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
                    <div>
                      <label
                        htmlFor={`account-name-${account.id}`}
                        className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5"
                      >
                        Account name
                      </label>
                      <input
                        id={`account-name-${account.id}`}
                        type="text"
                        maxLength={80}
                        value={account.name}
                        onChange={event => updateAccount(account.id, 'name', event.target.value)}
                        placeholder="e.g., Company deferred comp"
                        className="w-full px-3 py-2.5 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg text-gray-900 dark:text-gray-100 focus:ring-2 focus:ring-fire-500 focus:border-fire-500"
                      />
                    </div>
                    <div>
                      <label
                        htmlFor={`account-type-${account.id}`}
                        className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5"
                      >
                        Account type
                      </label>
                      <select
                        id={`account-type-${account.id}`}
                        value={account.type}
                        onChange={event => updateAccount(
                          account.id,
                          'type',
                          event.target.value as RetirementAccountType,
                        )}
                        className="w-full px-3 py-2.5 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg text-gray-900 dark:text-gray-100 focus:ring-2 focus:ring-fire-500 focus:border-fire-500"
                      >
                        {ACCOUNT_TYPES.map(type => (
                          <option key={type.value} value={type.value}>{type.label}</option>
                        ))}
                      </select>
                    </div>
                    <CurrencyInput
                      label="Current Balance"
                      value={account.balance}
                      onChange={value => updateAccount(account.id, 'balance', value)}
                      tooltip="The amount currently held in this account"
                    />
                    <CurrencyInput
                      label="Contributions"
                      value={account.annualContribution}
                      onChange={value => updateAccount(account.id, 'annualContribution', value)}
                      tooltip="Contributions stop at your semi-retirement age."
                      periodic
                    />
                    <PercentageInput
                      label="Expected Annual Return"
                      value={account.annualReturn}
                      onChange={value => updateAccount(account.id, 'annualReturn', value)}
                      min={0}
                      max={0.2}
                      tooltip="Average annual investment return before inflation"
                    />
                    <div>
                      <AgeInput
                        label={account.type === 'deferred' ? 'Vests / payout starts at age' : 'Available at age'}
                        value={account.availableAge}
                        onChange={value => updateAccount(account.id, 'availableAge', value)}
                        tooltip={account.type === 'deferred'
                          ? 'Required payouts begin at this age, even when other income covers spending.'
                          : 'The first age at which this bucket can cover a cash-flow gap.'}
                      />
                      <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
                        Calendar year {currentYear + account.availableAge - currentAge}
                      </p>
                    </div>
                    {account.type === 'deferred' ? (
                      <InputGroup
                        label="Payout period"
                        value={account.payoutYears}
                        onChange={value => updateAccount(account.id, 'payoutYears', value)}
                        suffix="years"
                        min={1}
                        max={30}
                        tooltip="The balance is distributed evenly over this many years."
                      />
                    ) : (
                      <PercentageInput
                        label="Annual withdrawal rate"
                        value={account.withdrawalRate}
                        onChange={value => updateAccount(account.id, 'withdrawalRate', value)}
                        min={0}
                        max={0.2}
                        tooltip="The maximum percentage of the remaining balance available to cover that year’s gap."
                      />
                    )}
                  </div>
                )}
              </div>
            )
          })}
        </div>
      )}

      {accounts.length > 0 && (
        <div className="flex items-center justify-between pt-3 border-t border-gray-200 dark:border-gray-700">
          <span className="text-sm text-gray-600 dark:text-gray-400">Current total</span>
          <span className="font-bold text-gray-900 dark:text-gray-100">
            {formatCurrency(totalBalance)}
          </span>
        </div>
      )}
    </div>
  )
}
