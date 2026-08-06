import { Fragment, useMemo, useState } from 'react'
import { AgeInput, CurrencyInput, PercentageInput } from '../components/inputs'
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
import {
  calculateDeferredCompensation,
} from '../utils/deferredCompensation'
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
    copyUrl,
    hasCustomParams,
    hasUnsavedChanges,
  } = useDeferredCompensationParams()

  const currentYear = new Date().getFullYear()
  const results = useMemo(
    () => calculateDeferredCompensation({ ...params, currentYear }),
    [params, currentYear],
  )
  const retirementCashFlow = results.projections.filter(
    point => point.age >= params.semiRetirementAge,
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
      semiRetirementAge: params.semiRetirementAge,
      planThroughAge: params.planThroughAge,
      annualExpenses: params.annualExpenses,
      semiRetirementIncome: params.semiRetirementIncome,
      annualDividends: params.annualDividends,
      inflationRate: params.inflationRate,
      accountCount: params.accounts.length,
    })
    const { values: resultValues, formats: resultFormats } = prepareResultsForExport(results)

    exportToExcel({
      calculatorName: 'Retirement Cash Flow',
      inputs,
      results: resultValues,
      projections: retirementCashFlow,
      additionalSheets: [{
        name: 'Accounts',
        data: params.accounts,
      }],
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
              Combine account withdrawals, deferred payouts, dividends, and semi-retirement income.
            </p>
          </div>
          <div className="flex flex-wrap gap-2">
            <ExportButton onExport={handleExport} disabled={params.accounts.length === 0} />
            <UrlActions
              onReset={resetParams}
              onSave={saveParams}
              onCopy={copyUrl}
              hasCustomParams={hasCustomParams}
              hasUnsavedChanges={hasUnsavedChanges}
            />
          </div>
        </div>

        <div className="bg-indigo-50 dark:bg-indigo-900/20 border border-indigo-200 dark:border-indigo-800 rounded-xl p-4">
          <div className="flex gap-3">
            <span className="text-2xl" aria-hidden="true">🪣</span>
            <div>
              <h2 className="font-semibold text-indigo-900 dark:text-indigo-100">
                Plan income across every bucket
              </h2>
              <p className="text-sm text-indigo-700 dark:text-indigo-300 mt-1">
                Deferred compensation is paid over its selected period. Other accounts provide
                cash flow at their individual withdrawal rates once available.
              </p>
            </div>
          </div>
        </div>

        <div className="grid lg:grid-cols-3 gap-6">
          <Card>
            <CardHeader>
              <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
                Retirement scenario
              </h2>
            </CardHeader>
            <CardContent className="space-y-4">
              <AgeInput
                label="Current age"
                value={params.currentAge}
                onChange={value => setParam('currentAge', value)}
              />
              <AgeInput
                label="Semi-retirement age"
                value={params.semiRetirementAge}
                onChange={value => setParam('semiRetirementAge', value)}
                min={params.currentAge}
                tooltip="Contributions stop and planned retirement cash flow begins at this age."
              />
              <AgeInput
                label="Plan through age"
                value={params.planThroughAge}
                onChange={value => setParam('planThroughAge', value)}
                min={params.semiRetirementAge}
              />
              <CurrencyInput
                label="Current annual expenses"
                value={params.annualExpenses}
                onChange={value => setParam('annualExpenses', value)}
                tooltip="Expenses increase annually with inflation."
              />
              <CurrencyInput
                label="Annual semi-retirement income"
                value={params.semiRetirementIncome}
                onChange={value => setParam('semiRetirementIncome', value)}
                tooltip="Part-time, consulting, rental, or other annual income after semi-retirement."
              />
              <CurrencyInput
                label="Annual dividends"
                value={params.annualDividends}
                onChange={value => setParam('annualDividends', value)}
                tooltip="Expected dividend income per year after semi-retirement."
              />
              <PercentageInput
                label="Inflation rate"
                value={params.inflationRate}
                onChange={value => setParam('inflationRate', value)}
                min={0}
                max={0.15}
              />
            </CardContent>
          </Card>

          <div className="lg:col-span-2 space-y-6">
            <div className="grid sm:grid-cols-2 lg:grid-cols-4 gap-4">
              <ResultCard
                label="At semi-retirement"
                value={results.balanceAtSemiRetirement}
                format="currency"
                highlight
              />
              <ResultCard
                label="First-year income"
                value={results.firstYearIncome}
                format="currency"
              />
              <ResultCard
                label="Funded years"
                value={results.fundedYears}
                format="years"
                subtext={`of ${retirementCashFlow.length} projected`}
              />
              <ResultCard
                label="Ending balance"
                value={results.endingBalance}
                format="currency"
              />
            </div>

            <Card>
              <CardHeader>
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <div>
                    <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
                      Retirement cash flow
                    </h2>
                    <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
                      Income and expenses use the left scale; account balance uses the right.
                    </p>
                  </div>
                  <span className={`text-sm font-semibold ${
                    results.firstYearSurplus >= 0
                      ? 'text-green-600 dark:text-green-400'
                      : 'text-red-600 dark:text-red-400'
                  }`}>
                    First-year {results.firstYearSurplus >= 0 ? 'surplus' : 'gap'}:{' '}
                    {formatCurrency(Math.abs(results.firstYearSurplus))}
                  </span>
                </div>
              </CardHeader>
              <CardContent>
                <RetirementCashFlowChart data={retirementCashFlow} />
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <div>
                  <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
                    Bucket balances over time
                  </h2>
                  <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
                    See how each retirement account grows before withdrawals and changes during retirement.
                  </p>
                </div>
              </CardHeader>
              <CardContent>
                <RetirementBucketBalanceChart data={results.projections} accounts={params.accounts} />
              </CardContent>
            </Card>
          </div>
        </div>

        <Card>
          <CardHeader>
            <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
              Accounts and payout schedules
            </h2>
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

        <Card>
          <CardHeader>
            <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
              Annual cash-flow detail
            </h2>
          </CardHeader>
          <CardContent>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700">
                    {['Age / year', 'Income', 'Expenses', 'Surplus / gap', 'Balance'].map(label => (
                      <th key={label} className="text-left py-3 px-3 font-semibold text-gray-900 dark:text-gray-100 whitespace-nowrap">
                        {label}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {retirementCashFlow.map(point => {
                    const accountDetails = Object.entries(point.withdrawals).map(([id, withdrawal]) => {
                      const accountDetail = accountsById.get(id)
                      return {
                        account: accountDetail?.account,
                        index: accountDetail?.index,
                        withdrawal,
                        balance: point.balances[id] ?? 0,
                      }
                    })
                    const deferredAccounts = accountDetails.filter(
                      detail => detail.account?.type === 'deferred',
                    )
                    const portfolioAccounts = accountDetails.filter(
                      detail => detail.account?.type !== 'deferred',
                    )
                    const deferredIncome = deferredAccounts.reduce(
                      (sum, detail) => sum + detail.withdrawal,
                      0,
                    )
                    const portfolioIncome = portfolioAccounts.reduce(
                      (sum, detail) => sum + detail.withdrawal,
                      0,
                    )
                    const expanded = expandedAges.has(point.age)

                    return (
                      <Fragment key={point.age}>
                        <tr className="border-b border-gray-100 dark:border-gray-800">
                          <td className="py-3 px-3 text-gray-900 dark:text-gray-100 whitespace-nowrap">
                            <button
                              type="button"
                              onClick={() => toggleAnnualDetail(point.age)}
                              className="inline-flex items-center gap-2 text-left hover:text-fire-600 dark:hover:text-fire-400"
                              aria-expanded={expanded}
                              aria-label={`${expanded ? 'Hide' : 'Show'} income sources for age ${point.age}`}
                            >
                              <svg
                                className={`w-4 h-4 text-gray-500 transition-transform ${expanded ? 'rotate-90' : ''}`}
                                fill="none"
                                viewBox="0 0 24 24"
                                stroke="currentColor"
                                aria-hidden="true"
                              >
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                              </svg>
                              <span>
                                {point.age} <span className="text-gray-500">/ {point.year}</span>
                              </span>
                            </button>
                          </td>
                          <td className="py-3 px-3">{formatCurrency(point.totalIncome)}</td>
                          <td className="py-3 px-3">{formatCurrency(point.expenses)}</td>
                          <td className={`py-3 px-3 font-medium ${
                            point.surplus >= 0
                              ? 'text-green-600 dark:text-green-400'
                              : 'text-red-600 dark:text-red-400'
                          }`}>
                            {point.surplus >= 0 ? '+' : '−'}{formatCurrency(Math.abs(point.surplus))}
                          </td>
                          <td className="py-3 px-3 font-medium text-gray-900 dark:text-gray-100">
                            {formatCurrency(point.totalBalance)}
                          </td>
                        </tr>
                        {expanded && (
                          <tr className="border-b border-gray-100 dark:border-gray-800 bg-gray-50 dark:bg-gray-800/50">
                            <td colSpan={5} className="p-4">
                              <div className="grid gap-4 md:grid-cols-3">
                                <div className="rounded-lg bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 p-3">
                                  <p className="text-xs font-semibold uppercase tracking-wide text-violet-700 dark:text-violet-300">
                                    Deferred compensation
                                  </p>
                                  <p className="mt-1 font-semibold text-gray-900 dark:text-gray-100">
                                    {formatCurrency(deferredIncome)}
                                  </p>
                                  <div className="mt-2 space-y-1 text-xs text-gray-600 dark:text-gray-400">
                                    {deferredAccounts.map(detail => (
                                      <div key={detail.account?.id} className="flex justify-between gap-3">
                                        <span>{detail.account?.name || `Account ${(detail.index ?? 0) + 1}`}</span>
                                        <span>{formatCurrency(detail.withdrawal)}</span>
                                      </div>
                                    ))}
                                    {deferredAccounts.length === 0 && <span>No deferred payouts</span>}
                                  </div>
                                </div>
                                <div className="rounded-lg bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 p-3">
                                  <p className="text-xs font-semibold uppercase tracking-wide text-sky-700 dark:text-sky-300">
                                    Portfolio withdrawals
                                  </p>
                                  <p className="mt-1 font-semibold text-gray-900 dark:text-gray-100">
                                    {formatCurrency(portfolioIncome)}
                                  </p>
                                  <div className="mt-2 space-y-1 text-xs text-gray-600 dark:text-gray-400">
                                    {portfolioAccounts.map(detail => (
                                      <div key={detail.account?.id} className="flex justify-between gap-3">
                                        <span>{detail.account?.name || `Account ${(detail.index ?? 0) + 1}`}</span>
                                        <span>{formatCurrency(detail.withdrawal)}</span>
                                      </div>
                                    ))}
                                    {portfolioAccounts.length === 0 && <span>No portfolio withdrawals</span>}
                                  </div>
                                </div>
                                <div className="rounded-lg bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 p-3">
                                  <p className="text-xs font-semibold uppercase tracking-wide text-emerald-700 dark:text-emerald-300">
                                    Other income
                                  </p>
                                  <div className="mt-2 space-y-1 text-xs text-gray-600 dark:text-gray-400">
                                    <div className="flex justify-between gap-3">
                                      <span>Semi-retirement income</span>
                                      <span>{formatCurrency(point.employmentIncome)}</span>
                                    </div>
                                    <div className="flex justify-between gap-3">
                                      <span>Dividends</span>
                                      <span>{formatCurrency(point.dividendIncome)}</span>
                                    </div>
                                  </div>
                                </div>
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
