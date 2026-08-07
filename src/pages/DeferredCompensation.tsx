import { Fragment, useMemo, useState } from 'react'
import { AgeInput, CurrencyInput, PercentageInput, RetirementIncomeListInput } from '../components/inputs'
import RetirementAccountListInput from '../components/inputs/RetirementAccountListInput'
import RetirementCashFlowChart from '../components/charts/RetirementCashFlowChart'
import RetirementBucketBalanceChart from '../components/charts/RetirementBucketBalanceChart'
import {
  Card,
  CardContent,
  CardHeader,
  Disclaimer,
  ExportButton,
  ResultCard,
  UrlActions,
} from '../components/ui'
import SEO from '../components/SEO'
import { calculatorSEO } from '../config/seo'
import { useDeferredCompensationParams } from '../hooks/useDeferredCompensationParams'
import { calculateDeferredCompensation } from '../utils/deferredCompensation'
import { formatCurrency } from '../utils/calculations'
import {
  exportToExcel,
  prepareInputsForExport,
  prepareResultsForExport,
} from '../utils/excelExport'

export default function DeferredCompensation() {
  const [expandedAges, setExpandedAges] = useState<Set<number>>(new Set())
  const {
    params,
    setParam,
    resetParams,
    saveParams,
    loadParams,
    copyUrl,
    hasCustomParams,
    hasUnsavedChanges,
    hasSavedParams,
    savedAt,
  } = useDeferredCompensationParams()

  const currentYear = new Date().getFullYear()
  const results = useMemo(
    () => calculateDeferredCompensation({ ...params, currentYear }),
    [params, currentYear],
  )
  const incomeSourcesById = useMemo(
    () => new Map(params.incomeSources.map((source, index) => [source.id, { source, index }])),
    [params.incomeSources],
  )
  const accountsById = useMemo(
    () => new Map(params.accounts.map((account, index) => [account.id, { account, index }])),
    [params.accounts],
  )

  const toggleAnnualDetail = (age: number) => {
    setExpandedAges(previous => {
      const next = new Set(previous)
      if (next.has(age)) next.delete(age)
      else next.add(age)
      return next
    })
  }

  const handleExport = () => {
    const { values: inputs, formats: inputFormats } = prepareInputsForExport({
      currentAge: params.currentAge,
      retirementAge: params.semiRetirementAge,
      planThroughAge: params.planThroughAge,
      annualExpenses: params.annualExpenses,
      inflationRate: params.inflationRate,
      withdrawOnlyAfterRetirement: params.withdrawOnlyAfterRetirement,
      reinvestSurplus: params.reinvestSurplus,
      incomeSourceCount: params.incomeSources.length,
      accountCount: params.accounts.length,
    })
    const { values: resultValues, formats: resultFormats } = prepareResultsForExport(results)

    exportToExcel({
      calculatorName: 'Retirement Cash Flow',
      inputs,
      results: resultValues,
      projections: results.projections,
      additionalSheets: [
        { name: 'Income Sources', data: params.incomeSources },
        { name: 'Accounts', data: params.accounts },
      ],
      inputFormats,
      resultFormats,
    })
  }

  return (
    <>
      <SEO {...calculatorSEO['retirement-cash-flow']} />
      <div className="space-y-6">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div>
            <h1 className="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-gray-100 flex items-center gap-3">
              <span className="text-3xl" role="img" aria-label="Calendar and money">🗓️</span>
              Retirement Cash Flow
            </h1>
            <p className="text-gray-600 dark:text-gray-400 mt-1">
              See how income offsets spending before your portfolio fills the remaining gap.
            </p>
          </div>
          <div className="flex flex-wrap gap-2">
            <ExportButton onExport={handleExport} />
            <UrlActions
              onReset={resetParams}
              onSave={saveParams}
              onLoad={loadParams}
              onCopy={copyUrl}
              hasCustomParams={hasCustomParams}
              hasUnsavedChanges={hasUnsavedChanges}
              hasSavedParams={hasSavedParams}
              savedAt={savedAt}
            />
          </div>
        </div>

        <div className="bg-indigo-50 dark:bg-indigo-900/20 border border-indigo-200 dark:border-indigo-800 rounded-xl p-4">
          <div className="flex gap-3">
            <span className="text-2xl" aria-hidden="true">📊</span>
            <div>
              <h2 className="font-semibold text-indigo-900 dark:text-indigo-100">Plan the gap, not gross income</h2>
              <p className="text-sm text-indigo-700 dark:text-indigo-300 mt-1">
                Expenses are shown in today&apos;s dollars and grow with inflation. Each income source lowers
                the amount your portfolio needs to withdraw.
              </p>
            </div>
          </div>
        </div>

        <Card>
          <CardHeader>
            <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Retirement scenario</h2>
          </CardHeader>
          <CardContent className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
              <AgeInput label="Current age" value={params.currentAge} onChange={value => setParam('currentAge', value)} />
              <AgeInput
                label="Retirement age"
                value={params.semiRetirementAge}
                onChange={value => setParam('semiRetirementAge', value)}
                min={params.currentAge}
                tooltip="Portfolio withdrawals begin at this age unless you allow them earlier."
              />
              <AgeInput
                label="Plan through age"
                value={params.planThroughAge}
                onChange={value => setParam('planThroughAge', value)}
                min={params.semiRetirementAge}
              />
              <CurrencyInput
                label="Annual retirement spending"
                value={params.annualExpenses}
                onChange={value => setParam('annualExpenses', value)}
                tooltip="Your after-tax annual spending target in today’s dollars."
                allowMonthlyToggle
              />
              <PercentageInput
                label="Inflation rate"
                value={params.inflationRate}
                onChange={value => setParam('inflationRate', value)}
                min={0}
                max={0.15}
              />
              <label className="flex items-start gap-2 text-sm text-gray-700 dark:text-gray-300">
                <input
                  type="checkbox"
                  checked={params.withdrawOnlyAfterRetirement}
                  onChange={event => setParam('withdrawOnlyAfterRetirement', event.target.checked)}
                  className="mt-0.5 h-4 w-4 rounded border-gray-300 text-fire-600 focus:ring-fire-500"
                />
                <span>
                  <span className="font-medium">Wait until retirement to withdraw</span>
                  <span className="block text-xs text-gray-500 dark:text-gray-400 mt-0.5">
                    Leave this off to let accounts cover a gap as soon as each is available.
                  </span>
                </span>
              </label>
              <label className="flex items-start gap-2 text-sm text-gray-700 dark:text-gray-300">
                <input
                  type="checkbox"
                  checked={params.reinvestSurplus}
                  onChange={event => setParam('reinvestSurplus', event.target.checked)}
                  className="mt-0.5 h-4 w-4 rounded border-gray-300 text-fire-600 focus:ring-fire-500"
                />
                <span>
                  <span className="font-medium">Reinvest income surplus</span>
                  <span className="block text-xs text-gray-500 dark:text-gray-400 mt-0.5">
                    Add income above expenses back into accounts proportionally.
                  </span>
                </span>
              </label>
          </CardContent>
        </Card>

        <div className="grid sm:grid-cols-2 lg:grid-cols-4 gap-4">
          <ResultCard label="At retirement" value={results.balanceAtSemiRetirement} format="currency" highlight />
          <ResultCard label="First-year income" value={results.firstYearIncome} format="currency" />
          <ResultCard
            label="Funded years"
            value={results.fundedYears}
            format="years"
            subtext={`of ${results.projections.filter(point => point.age >= params.semiRetirementAge).length} projected`}
          />
          <ResultCard label="Ending portfolio" value={results.endingBalance} format="currency" />
        </div>

        <Card>
          <CardHeader>
            <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Income sources</h2>
          </CardHeader>
          <CardContent>
            <RetirementIncomeListInput
              sources={params.incomeSources}
              onChange={sources => setParam('incomeSources', sources)}
              currentAge={params.currentAge}
            />
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Accounts and withdrawal limits</h2>
          </CardHeader>
          <CardContent>
            <RetirementAccountListInput
              accounts={params.accounts}
              onChange={accounts => setParam('accounts', accounts)}
              currentAge={params.currentAge}
              currentYear={currentYear}
            />
          </CardContent>
        </Card>

        <div className="grid xl:grid-cols-2 gap-6">
          <Card>
            <CardHeader>
              <div>
                <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Retirement cash flow</h2>
                <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
                  Compare income, spending, gap withdrawals, and portfolio value in one view.
                </p>
              </div>
            </CardHeader>
            <CardContent>
              <RetirementCashFlowChart data={results.projections} />
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Bucket balances over time</h2>
            </CardHeader>
            <CardContent>
              <RetirementBucketBalanceChart data={results.projections} accounts={params.accounts} />
            </CardContent>
          </Card>
        </div>

        <Card>
          <CardHeader>
            <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Annual cash-flow detail</h2>
          </CardHeader>
          <CardContent>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700">
                    {['Age / year', 'Income & required payouts', 'Gap withdrawals', 'Expenses', 'Surplus / gap', 'Portfolio'].map(label => (
                      <th key={label} className="text-left py-3 px-3 font-semibold text-gray-900 dark:text-gray-100 whitespace-nowrap">{label}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {results.projections.map(point => {
                    const expanded = expandedAges.has(point.age)
                    const activeSources = Object.entries(point.incomeBySource).filter(([, amount]) => amount > 0)
                    const activeAccountWithdrawals = Object.entries(point.withdrawals).filter(([, amount]) => amount > 0)
                    return (
                      <Fragment key={point.age}>
                        <tr className="border-b border-gray-100 dark:border-gray-800">
                          <td className="py-3 px-3 text-gray-900 dark:text-gray-100 whitespace-nowrap">
                            <button
                              type="button"
                              onClick={() => toggleAnnualDetail(point.age)}
                              className="inline-flex items-center gap-2 text-left hover:text-fire-600 dark:hover:text-fire-400"
                              aria-expanded={expanded}
                              aria-label={`${expanded ? 'Hide' : 'Show'} income and withdrawals for age ${point.age}`}
                            >
                              <svg className={`w-4 h-4 text-gray-500 transition-transform ${expanded ? 'rotate-90' : ''}`} fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                              </svg>
                              <span>{point.age} <span className="text-gray-500">/ {point.year}</span></span>
                            </button>
                          </td>
                          <td className="py-3 px-3">{formatCurrency(point.outsideIncome + point.deferredIncome)}</td>
                          <td className="py-3 px-3">{formatCurrency(point.portfolioWithdrawals)}</td>
                          <td className="py-3 px-3">{formatCurrency(point.expenses)}</td>
                          <td className={`py-3 px-3 font-medium ${point.surplus >= 0 ? 'text-green-600 dark:text-green-400' : 'text-red-600 dark:text-red-400'}`}>
                            {point.surplus >= 0 ? '+' : '−'}{formatCurrency(Math.abs(point.surplus))}
                          </td>
                          <td className="py-3 px-3 font-medium text-gray-900 dark:text-gray-100">{formatCurrency(point.totalBalance)}</td>
                        </tr>
                        {expanded && (
                          <tr className="border-b border-gray-100 dark:border-gray-800 bg-gray-50 dark:bg-gray-800/50">
                            <td colSpan={6} className="p-4">
                              <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
                                {activeSources.map(([id, amount]) => (
                                  <div key={id} className="rounded-lg bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 p-3">
                                    <p className="text-xs font-semibold text-gray-700 dark:text-gray-300">
                                      {incomeSourcesById.get(id)?.source.name || `Income source ${(incomeSourcesById.get(id)?.index ?? 0) + 1}`}
                                    </p>
                                    <p className="mt-1 font-semibold text-emerald-600 dark:text-emerald-400">{formatCurrency(amount)}</p>
                                  </div>
                                ))}
                                {activeAccountWithdrawals.map(([id, amount]) => {
                                  const accountDetails = accountsById.get(id)
                                  const accountName = accountDetails?.account.name
                                    || (accountDetails ? `Account ${accountDetails.index + 1}` : 'Account')
                                  return (
                                    <div key={id} className="rounded-lg bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 p-3">
                                      <p className="text-xs font-semibold text-gray-700 dark:text-gray-300">
                                        {accountName} withdrawal
                                      </p>
                                      <p className="mt-1 font-semibold text-violet-600 dark:text-violet-400">{formatCurrency(amount)}</p>
                                    </div>
                                  )
                                })}
                                {activeSources.length === 0 && activeAccountWithdrawals.length === 0 && (
                                  <p className="text-sm text-gray-500 dark:text-gray-400">No income or account withdrawals this year.</p>
                                )}
                              </div>
                            </td>
                          </tr>
                        )}
                      </Fragment>
                    )
                  })}
                </tbody>
              </table>
            </div>
          </CardContent>
        </Card>

        <Disclaimer />
      </div>
    </>
  )
}
