import { Fragment, useMemo, useState } from 'react'
import {
  AgeInput,
  CurrencyInput,
  CurrencyPeriodProvider,
  PeriodToggle,
  PercentageInput,
  RetirementExpenseListInput,
  RetirementIncomeListInput,
} from '../components/inputs'
import RetirementAccountListInput from '../components/inputs/RetirementAccountListInput'
import RetirementCashFlowChart from '../components/charts/RetirementCashFlowChart'
import RetirementBucketBalanceChart from '../components/charts/RetirementBucketBalanceChart'
import {
  Card,
  CardContent,
  CardHeader,
  AdvancedDetails,
  CalculatorFooter,
  ResultCard,
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
    setParamDebounced,
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
  const expensesById = useMemo(
    () => new Map(params.additionalExpenses.map((expense, index) => [expense.id, { expense, index }])),
    [params.additionalExpenses],
  )
  // Reports the years the plan had to spend past the stated withdrawal policy to stay funded, which
  // is the opposite of what this disclosure said before issue #56: the rate is no longer a hard
  // limit, so it can never be "what's binding" on a shortfall. A policy-exceeding year is usually a
  // funded year, so this is deliberately not scoped to the first shortfall the way it once was.
  const policyExcessPoints = useMemo(
    () =>
      results.projections.filter(
        point => point.age >= params.semiRetirementAge && point.policyExcessWithdrawals > 0,
      ),
    [params.semiRetirementAge, results.projections],
  )
  const firstPolicyExcessPoint = policyExcessPoints[0]

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
      additionalExpenseCount: params.additionalExpenses.length,
    })
    // prepareResultsForExport omits null rather than emitting a blank row, so a plan with no
    // shortfall simply has no "First Shortfall Age" row. This used to pass `?? 0`, which wrote a 0
    // that reads as a shortfall at age 0.
    const { values: resultValues, formats: resultFormats } = prepareResultsForExport({
      currentBalance: results.currentBalance,
      balanceAtSemiRetirement: results.balanceAtSemiRetirement,
      firstYearIncomeAfterTax: results.firstYearIncome,
      firstYearSurplus: results.firstYearSurplus,
      endingBalance: results.endingBalance,
      consecutiveFundedYears: results.fundedYears,
      retirementYears: results.retirementYears,
      yearsFullyCovered: results.yearsFullyCovered,
      firstShortfallAge: results.firstShortfallAge,
    })

    exportToExcel({
      calculatorName: 'Retirement Cash Flow',
      inputs,
      results: resultValues,
      projections: results.projections,
      additionalSheets: [
        { name: 'Income Sources', data: params.incomeSources },
        { name: 'Accounts', data: params.accounts },
        { name: 'Additional Expenses', data: params.additionalExpenses },
      ],
      inputFormats,
      resultFormats,
    })
  }

  return (
    <CurrencyPeriodProvider period={params.currencyPeriod} onChange={value => setParam('currencyPeriod', value)}>
      <SEO {...calculatorSEO['retirement-cash-flow']} />
      <div className="space-y-6">
        <header>
          <h1 className="text-2xl sm:text-3xl font-bold text-content">Retirement Cash Flow</h1>
          <p className="mt-1 text-content-muted">
            See how after-tax income offsets today-dollar retirement spending before your portfolio fills the remaining gap.
          </p>
          <p className="mt-1 text-sm text-content-subtle">
            Withdrawals are shown after an estimated flat tax you set per account. Results are shown in
            future dollars for the age listed on each card.
          </p>
        </header>

        <Card>
          <CardHeader>
            <h2 className="text-lg font-semibold text-content">Start with your retirement scenario</h2>
            <PeriodToggle className="mt-3" />
          </CardHeader>
          <CardContent className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
              <AgeInput
                label="Current Age"
                value={params.currentAge}
                onChange={value => setParam('currentAge', value)}
                onSliderChange={value => setParamDebounced('currentAge', value)}
                tooltip="Your current age"
                showSlider
              />
              <AgeInput
                label="Retirement Age"
                value={params.semiRetirementAge}
                onChange={value => setParam('semiRetirementAge', value)}
                onSliderChange={value => setParamDebounced('semiRetirementAge', value)}
                min={params.currentAge}
                tooltip="Portfolio withdrawals begin at this age unless you allow them earlier."
                showSlider
              />
              <AgeInput
                label="Plan Through Age"
                value={params.planThroughAge}
                onChange={value => setParam('planThroughAge', value)}
                onSliderChange={value => setParamDebounced('planThroughAge', value)}
                min={params.semiRetirementAge}
                tooltip="The final age included in this retirement cash-flow plan."
                showSlider
              />
              <CurrencyInput
                label="Retirement spending (today's dollars)"
                value={params.annualExpenses}
                onChange={value => setParam('annualExpenses', value)}
                tooltip="Your after-tax annual spending target in today’s dollars."
                periodic
              />
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <h2 className="text-lg font-semibold text-content">Income sources</h2>
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
            <h2 className="text-lg font-semibold text-content">Accounts and withdrawal limits</h2>
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
            <h2 className="text-lg font-semibold text-content">Additional expenses</h2>
          </CardHeader>
          <CardContent>
            <RetirementExpenseListInput
              expenses={params.additionalExpenses}
              onChange={expenses => setParam('additionalExpenses', expenses)}
              currentAge={params.currentAge}
            />
          </CardContent>
        </Card>

        <AdvancedDetails description="These slower-moving rules control inflation, timing, and what happens when income exceeds spending.">
          <PercentageInput
            label="Inflation rate"
            value={params.inflationRate}
            onChange={value => setParam('inflationRate', value)}
            onSliderChange={value => setParamDebounced('inflationRate', value)}
            tooltip="Expected annual increase in prices and retirement spending."
            min={0}
            max={0.15}
          />
          <div className="space-y-4">
            <label className="flex items-start gap-2 text-sm text-content-muted">
              <input
                type="checkbox"
                checked={params.withdrawOnlyAfterRetirement}
                onChange={event => setParam('withdrawOnlyAfterRetirement', event.target.checked)}
                className="mt-0.5 h-4 w-4 rounded border-gray-300 text-fire-600 focus-visible:ring-ring"
              />
              <span>
                <span className="font-medium">Wait until retirement to withdraw</span>
                <span className="mt-0.5 block text-xs text-content-subtle">Leave this off to let accounts cover a gap as soon as each is available.</span>
              </span>
            </label>
            <label className="flex items-start gap-2 text-sm text-content-muted">
              <input
                type="checkbox"
                checked={params.reinvestSurplus}
                onChange={event => setParam('reinvestSurplus', event.target.checked)}
                className="mt-0.5 h-4 w-4 rounded border-gray-300 text-fire-600 focus-visible:ring-ring"
              />
              <span>
                <span className="font-medium">Reinvest income surplus</span>
                <span className="mt-0.5 block text-xs text-content-subtle">Add income above spending back into accounts proportionally.</span>
              </span>
            </label>
          </div>
        </AdvancedDetails>

        <section aria-labelledby="cash-flow-outlook-heading" className="space-y-4">
          <div>
            <h2 id="cash-flow-outlook-heading" className="text-xl font-semibold text-content">Your outlook</h2>
            <p className="mt-1 text-sm text-content-muted">Results update from the scenario, income sources, accounts, and additional spending above.</p>
          </div>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5">
            <ResultCard
              label="At retirement"
              value={results.balanceAtSemiRetirement}
              format="currency"
              highlight
              subtext={`Future dollars at age ${params.semiRetirementAge}`}
            />
            <ResultCard
              label="First-year income"
              value={results.firstYearIncome}
              format="currency"
              subtext={`After tax · future dollars at age ${params.semiRetirementAge}`}
            />
            <ResultCard
              label="Funded years"
              value={results.fundedYears}
              format="years"
              subtext={`Consecutive from age ${params.semiRetirementAge} of ${results.retirementYears} projected`}
            />
            <ResultCard
              label="First shortfall"
              value={results.firstShortfallAge === null ? 'None' : `Age ${results.firstShortfallAge}`}
              subtext={
                results.firstShortfallAge === null
                  ? `Every year through age ${params.planThroughAge} is covered`
                  : `${results.yearsFullyCovered} of ${results.retirementYears} years are covered in total`
              }
            />
            <ResultCard
              label="Ending portfolio"
              value={results.endingBalance}
              format="currency"
              subtext={`Future dollars at age ${params.planThroughAge}`}
            />
          </div>
          {firstPolicyExcessPoint && (
            <p className="text-sm text-warning bg-warning-subtle border border-warning/30 rounded-control p-3">
              To stay funded, this plan withdraws more than your withdrawal-rate limits allow in{' '}
              {policyExcessPoints.length} of {results.retirementYears} retirement years, starting at age{' '}
              {firstPolicyExcessPoint.age} — {formatCurrency(firstPolicyExcessPoint.policyExcessWithdrawals)} above
              your limits that year. The withdrawal rate is a spending policy you set, not the amount of money
              available, so the plan spends past it rather than reporting a shortfall next to an untouched balance.
            </p>
          )}
        </section>

        <div className="grid xl:grid-cols-2 gap-6">
          <Card>
            <CardHeader>
              <div>
                <h2 className="text-lg font-semibold text-content">Retirement cash flow</h2>
                <p className="text-sm text-content-subtle mt-1">
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
              <h2 className="text-lg font-semibold text-content">Bucket balances over time</h2>
            </CardHeader>
            <CardContent>
              <RetirementBucketBalanceChart data={results.projections} accounts={params.accounts} />
            </CardContent>
          </Card>
        </div>

        <Card>
          <CardHeader>
            <h2 className="text-lg font-semibold text-content">Annual cash-flow detail</h2>
          </CardHeader>
          <CardContent>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-border-subtle">
                    {['Age / year', 'Income & required payouts (after tax)', 'Gap withdrawals (after tax)', 'Expenses', 'Surplus / gap', 'Portfolio'].map(label => (
                      <th key={label} className="text-left py-3 px-3 font-semibold text-content whitespace-nowrap">{label}</th>
                    ))}                  </tr>
                </thead>
                <tbody>
                  {results.projections.map(point => {
                    const expanded = expandedAges.has(point.age)
                    const activeSources = Object.entries(point.incomeBySource).filter(([, amount]) => amount > 0)
                    const activeAccountWithdrawals = Object.entries(point.withdrawals).filter(([, amount]) => amount > 0)
                    const activeAdditionalExpenses = Object.entries(point.expensesByItem).filter(([, amount]) => amount > 0)
                    return (
                      <Fragment key={point.age}>
                        <tr className="border-b border-border-subtle">
                          <td className="py-3 px-3 text-content whitespace-nowrap">
                            <button
                              type="button"
                              onClick={() => toggleAnnualDetail(point.age)}
                              className="inline-flex items-center gap-2 text-left hover:text-accent"
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
                          {/*
                            Reads the rounded `surplus` rather than the calculator's `isShortfall`
                            predicate, which is safe only because the two are exactly equivalent:
                            `roundSigned` rounds away from zero, so `surplus >= 0` holds for precisely
                            the years where `exact > -0.5`. Keep them in step — a tighter tolerance in
                            the calculator would let this cell render a green `+$0` for a year the
                            headline calls a shortfall. See issue #63.
                          */}
                          <td className={`py-3 px-3 font-medium ${point.surplus >= 0 ? 'text-success' : 'text-danger'}`}>
                            {point.surplus >= 0 ? '+' : '−'}{formatCurrency(Math.abs(point.surplus))}
                          </td>
                          <td className="py-3 px-3 font-medium text-content">{formatCurrency(point.totalBalance)}</td>
                        </tr>
                        {expanded && (
                          <tr className="border-b border-border-subtle bg-surface-sunken">
                            <td colSpan={6} className="p-4">
                              <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
                                {activeSources.map(([id, amount]) => (
                                  <div key={id} className="rounded-control bg-surface-raised border border-border-subtle p-3">
                                    <p className="text-xs font-semibold text-content-muted">
                                      {incomeSourcesById.get(id)?.source.name || `Income source ${(incomeSourcesById.get(id)?.index ?? 0) + 1}`}
                                    </p>
                                    <p className="mt-1 tabular font-semibold text-success">{formatCurrency(amount)}</p>
                                  </div>
                                ))}
                                {activeAccountWithdrawals.map(([id, amount]) => {
                                  const accountDetails = accountsById.get(id)
                                  const accountName = accountDetails?.account.name
                                    || (accountDetails ? `Account ${accountDetails.index + 1}` : 'Account')
                                  return (
                                    <div key={id} className="rounded-control bg-surface-raised border border-border-subtle p-3">
                                      <p className="text-xs font-semibold text-content-muted">
                                        {accountName} withdrawal (gross)
                                      </p>
                                      <p className="mt-1 tabular font-semibold text-info">{formatCurrency(amount)}</p>
                                    </div>
                                  )
                                })}
                                {point.withdrawalTaxes > 0 && (
                                  <div className="rounded-control bg-surface-raised border border-border-subtle p-3">
                                    <p className="text-xs font-semibold text-content-muted">
                                      Estimated withdrawal tax
                                    </p>
                                    <p className="mt-1 tabular font-semibold text-danger">−{formatCurrency(point.withdrawalTaxes)}</p>
                                  </div>
                                )}
                                <div className="rounded-control bg-surface-raised border border-border-subtle p-3">
                                  <p className="text-xs font-semibold text-content-muted">
                                    Core spending
                                  </p>
                                  <p className="mt-1 font-semibold text-warning">{formatCurrency(point.coreExpenses)}</p>
                                </div>
                                {activeAdditionalExpenses.map(([id, amount]) => {
                                  const expenseDetails = expensesById.get(id)
                                  const expenseName = expenseDetails?.expense.name
                                    || (expenseDetails ? `Additional expense ${expenseDetails.index + 1}` : 'Additional expense')
                                  return (
                                    <div key={id} className="rounded-control bg-surface-raised border border-border-subtle p-3">
                                      <p className="text-xs font-semibold text-content-muted">
                                        {expenseName}
                                      </p>
                                      <p className="mt-1 font-semibold text-warning">{formatCurrency(amount)}</p>
                                    </div>
                                  )
                                })}
                                {activeSources.length === 0 && activeAccountWithdrawals.length === 0 && activeAdditionalExpenses.length === 0 && (
                                  <p className="text-sm text-content-subtle">No income or account withdrawals this year.</p>
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

        <CalculatorFooter
          onExport={handleExport}
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
    </CurrencyPeriodProvider>
  )
}
