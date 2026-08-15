import { useState } from 'react'
import { Wallet } from 'lucide-react'
import type {
  RetirementAccount,
  RetirementAccountType,
} from '../../utils/deferredCompensation'
import { defaultWithdrawalTaxRate } from '../../utils/deferredCompensation'
import { formatCurrency } from '../../utils/calculations'
import AgeInput from './AgeInput'
import CurrencyInput from './CurrencyInput'
import PercentageInput from './PercentageInput'
import InputGroup from './InputGroup'
import { ChevronRight, X } from 'lucide-react'

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

const WITHDRAWAL_TAX_TOOLTIPS: Record<RetirementAccountType, string> = {
  deferred: 'Deferred compensation pays out as ordinary income, so withdrawals default to a 25% flat estimate. This is an estimate, not a bracket calculation — set your own expected rate.',
  traditional: 'Traditional 401(k) and IRA withdrawals are taxed as ordinary income, so they default to a 25% flat estimate. This is an estimate, not a bracket calculation — set your own expected rate.',
  roth: 'Qualified Roth withdrawals are tax-free, so this defaults to 0%.',
  hsa: 'Qualified HSA withdrawals for medical costs are tax-free, so this defaults to 0%.',
  taxable: 'Defaults to 0% because only the gain is taxable and this calculator does not track your cost basis. Enter an effective rate on the full withdrawal if you want to approximate capital-gains tax.',
  savings: 'Defaults to 0% because only the interest is taxable and this calculator does not separate interest from principal. Enter an effective rate on the full withdrawal if you want to approximate it.',
  other: 'Set the flat rate you expect to pay on withdrawals from this account.',
}

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

  const updateAccountType = (id: string, type: RetirementAccountType) => {
    onChange(accounts.map(account => {
      if (account.id !== id) return account
      // Only re-apply the type default when the user has not set their own rate.
      const keepsDefaultRate = account.withdrawalTaxRate === defaultWithdrawalTaxRate(account.type)
      return {
        ...account,
        type,
        withdrawalTaxRate: keepsDefaultRate ? defaultWithdrawalTaxRate(type) : account.withdrawalTaxRate,
      }
    }))
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
      withdrawalTaxRate: defaultWithdrawalTaxRate('taxable'),
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
          <h3 className="text-sm font-semibold text-content-muted">
            Retirement buckets
          </h3>
          <p className="text-xs text-content-subtle mt-0.5">
            Each account can have its own availability and withdrawal rules.
          </p>
        </div>
        <button
          type="button"
          onClick={addAccount}
          className="shrink-0 inline-flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium text-accent bg-accent-subtle hover:bg-accent-subtle-hover rounded-control transition-colors"
        >
          <span aria-hidden="true">+</span>
          Add account
        </button>
      </div>

      {accounts.length === 0 ? (
        <div className="text-center py-10 bg-surface-sunken rounded-container border-2 border-dashed border-border-strong">
          <Wallet className="mx-auto mb-2 h-7 w-7 text-content-subtle" aria-hidden="true" strokeWidth={1.5} />
          <p className="text-sm font-medium text-content">
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
                className="bg-surface-sunken rounded-control border border-border-subtle overflow-hidden"
              >
                <div className="flex items-center">
                  <button
                    type="button"
                    onClick={() => toggleExpanded(account.id)}
                    className="min-w-0 flex-1 px-4 py-3 flex items-center justify-between gap-3 text-left hover:bg-surface-sunken/50 transition-colors"
                    aria-expanded={expanded}
                  >
                    <span className="flex items-center gap-3 min-w-0">
                      <ChevronRight className={`w-4 h-4 shrink-0 text-content-subtle transition-transform ${expanded ? 'rotate-90' : ''}`} aria-hidden="true" strokeWidth={1.5} />
                      <span className="truncate font-medium text-content">
                        {account.name || `Account ${index + 1}`}
                      </span>
                    </span>
                    <span className="text-right shrink-0">
                      <span className="block text-sm font-semibold text-content">
                        {formatCurrency(account.balance)}
                      </span>
                      <span className="block text-xs text-content-subtle">
                        {typeLabel}
                      </span>
                    </span>
                  </button>
                  <button
                    type="button"
                    onClick={() => removeAccount(account.id)}
                    className="p-3 mr-1 text-content-subtle hover:text-danger transition-colors"
                    aria-label={`Remove ${account.name || `account ${index + 1}`}`}
                  >
                    <X className="w-4 h-4" strokeWidth={1.5} aria-hidden="true" />
                  </button>
                </div>

                {expanded && (
                  <div className="px-4 pb-4 pt-3 border-t border-border-subtle grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
                    <div>
                      <label
                        htmlFor={`account-name-${account.id}`}
                        className="block text-sm font-medium text-content-muted mb-1.5"
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
                        className="w-full px-3 py-2.5 bg-surface-raised border border-border-strong rounded-control text-content focus:ring-2 focus-visible:ring-ring focus-visible:border-accent"
                      />
                    </div>
                    <div>
                      <label
                        htmlFor={`account-type-${account.id}`}
                        className="block text-sm font-medium text-content-muted mb-1.5"
                      >
                        Account type
                      </label>
                      <select
                        id={`account-type-${account.id}`}
                        value={account.type}
                        onChange={event => updateAccountType(
                          account.id,
                          event.target.value as RetirementAccountType,
                        )}
                        className="w-full px-3 py-2.5 bg-surface-raised border border-border-strong rounded-control text-content focus:ring-2 focus-visible:ring-ring focus-visible:border-accent"
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
                      label="Contributions (today’s dollars)"
                      value={account.annualContribution}
                      onChange={value => updateAccount(account.id, 'annualContribution', value)}
                      tooltip="Entered in today’s dollars and escalated with inflation each year, the same way your spending is. Contributions stop at your semi-retirement age."
                      periodic
                    />
                    <PercentageInput
                      label="Expected Annual Return"
                      value={account.annualReturn}
                      onChange={value => updateAccount(account.id, 'annualReturn', value)}
                      min={0}
                      max={0.2}
                      tooltip="Average annual investment return before inflation. Deferred compensation keeps earning this return on the undistributed balance throughout the payout period."
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
                      <p className="text-xs text-content-subtle mt-1">
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
                        tooltip="Each year pays the remaining balance divided by the remaining payout years, so the balance keeps earning your expected return and is fully distributed by the end of this period."
                      />
                    ) : (
                      <PercentageInput
                        label="Annual withdrawal rate"
                        value={account.withdrawalRate}
                        onChange={value => updateAccount(account.id, 'withdrawalRate', value)}
                        min={0}
                        max={0.2}
                        tooltip="A spending policy, not a hard limit. This share of the remaining balance is taken each year, and the plan only goes above it in a year that would otherwise fall short. Raise it to draw this account down at your stated pace instead."
                      />
                    )}
                    <PercentageInput
                      label="Withdrawal tax rate"
                      value={account.withdrawalTaxRate}
                      onChange={value => updateAccount(account.id, 'withdrawalTaxRate', value)}
                      min={0}
                      max={0.6}
                      tooltip={WITHDRAWAL_TAX_TOOLTIPS[account.type]}
                    />
                  </div>
                )}
              </div>
            )
          })}
        </div>
      )}

      {accounts.length > 0 && (
        <div className="flex items-center justify-between pt-3 border-t border-border-subtle">
          <span className="text-sm text-content-muted">Current total</span>
          <span className="font-bold text-content">
            {formatCurrency(totalBalance)}
          </span>
        </div>
      )}
    </div>
  )
}
