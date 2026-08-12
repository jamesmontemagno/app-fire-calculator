import { useMemo } from 'react'
import { useCalculatorParams } from '../hooks/useCalculatorParams'
import { calculateAvalanchePayoff, calculateSnowballPayoff, formatCurrency, type DebtItem } from '../utils/calculations'
import { exportToExcel, prepareInputsForExport, prepareResultsForExport } from '../utils/excelExport'
import { CurrencyInput, InputGroup } from '../components/inputs'
import DebtListInput from '../components/inputs/DebtListInput'
import { CalculatorFooter, Card, CardContent, CardHeader, ResultCard } from '../components/ui'
import DebtBalanceChart from '../components/charts/DebtBalanceChart'
import DebtBreakdownChart from '../components/charts/DebtBreakdownChart'
import SEO from '../components/SEO'
import { calculatorSEO } from '../config/seo'

function calculateBudgetForTarget(
  debts: DebtItem[],
  totalMinimumPayments: number,
  targetMonths: number,
  strategy: 'snowball' | 'avalanche',
) {
  const calculate = strategy === 'snowball' ? calculateSnowballPayoff : calculateAvalanchePayoff
  let low = totalMinimumPayments
  let high = Math.max(totalMinimumPayments * 2, 1000)

  while (calculate(debts, high, 0).totalMonths > targetMonths && high < 1_000_000) {
    high *= 2
  }

  for (let iteration = 0; iteration < 40; iteration += 1) {
    const midpoint = (low + high) / 2
    if (calculate(debts, midpoint, 0).totalMonths <= targetMonths) high = midpoint
    else low = midpoint
  }

  return Math.ceil(high)
}

export default function DebtPayoff() {
  const {
    params, setParam, resetParams, saveParams, loadParams, copyUrl,
    hasCustomParams, hasUnsavedChanges, hasSavedParams, savedAt,
  } = useCalculatorParams()
  const debts: DebtItem[] = params.debts
  const totalDebt = debts.reduce((sum, debt) => sum + debt.balance, 0)
  const totalMinPayments = debts.reduce((sum, debt) => sum + debt.minPayment, 0)
  const canCalculate = debts.length > 0
    && totalDebt > 0
    && (params.debtMode === 'target' || params.debtBudget >= totalMinPayments)

  const results = useMemo(() => {
    if (!canCalculate) return null
    const calculate = params.debtStrategy === 'snowball' ? calculateSnowballPayoff : calculateAvalanchePayoff
    const baseBudget = params.debtMode === 'target'
      ? calculateBudgetForTarget(debts, totalMinPayments, params.debtMonths, params.debtStrategy)
      : params.debtBudget
    const base = calculate(debts, baseBudget, 0)
    const withExtra = params.debtExtra > 0 ? calculate(debts, baseBudget, params.debtExtra) : null
    return { base, withExtra, baseBudget }
  }, [canCalculate, debts, params.debtBudget, params.debtExtra, params.debtMode, params.debtMonths, params.debtStrategy, totalMinPayments])

  const comparisonResults = useMemo(() => {
    if (!canCalculate) return null
    const baseBudget = results?.baseBudget ?? params.debtBudget
    return {
      snowball: calculateSnowballPayoff(debts, baseBudget, params.debtExtra),
      avalanche: calculateAvalanchePayoff(debts, baseBudget, params.debtExtra),
    }
  }, [canCalculate, debts, params.debtBudget, params.debtExtra, results?.baseBudget])

  const handleExport = () => {
    if (!results) return
    const { values: inputs, formats: inputFormats } = prepareInputsForExport({
      strategy: params.debtStrategy, mode: params.debtMode, monthlyBudget: params.debtBudget,
      targetMonths: params.debtMonths, extraPayment: params.debtExtra, totalDebts: debts.length, totalDebt,
    })
    const { values: resultValues, formats: resultFormats } = prepareResultsForExport(results.base)
    exportToExcel({
      calculatorName: 'Debt Payoff',
      inputs,
      results: resultValues,
      additionalSheets: [
        { name: 'Debt List', data: debts.map(debt => ({ name: debt.name, balance: debt.balance, rate: debt.rate, minPayment: debt.minPayment })) },
        { name: 'Payoff Projections', data: results.base.projections },
      ],
      inputFormats,
      resultFormats,
    })
  }

  return (
    <>
      <SEO {...calculatorSEO['debt-payoff']} />
      <div className="space-y-8">
        <header>
          <h1 className="text-2xl font-bold text-gray-900 sm:text-3xl dark:text-gray-100">Debt Payoff Calculator</h1>
          <p className="mt-1 text-gray-600 dark:text-gray-400">Compare a Snowball or Avalanche payoff plan using the money you can send each month.</p>
        </header>

        <section aria-labelledby="debt-plan-heading">
          <Card>
            <CardHeader>
              <h2 id="debt-plan-heading" className="text-lg font-semibold text-gray-900 dark:text-gray-100">Start with your debt plan</h2>
              <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">Add each balance and minimum payment, then choose how to direct your available monthly budget.</p>
            </CardHeader>
            <CardContent className="space-y-5">
              <DebtListInput debts={debts} onChange={value => setParam('debts', value)} />
              <div className="grid gap-4 border-t border-gray-200 pt-5 sm:grid-cols-2 xl:grid-cols-3 dark:border-gray-700">
                <div>
                  <span className="block text-sm font-medium text-gray-700 dark:text-gray-300">Planning mode</span>
                  <div className="mt-1 inline-flex rounded-lg border border-gray-200 p-1 dark:border-gray-700" role="group" aria-label="Debt payoff planning mode">
                    {(['fixed', 'target'] as const).map(mode => (
                      <button
                        key={mode}
                        type="button"
                        onClick={() => setParam('debtMode', mode)}
                        aria-pressed={params.debtMode === mode}
                        className={`rounded-md px-3 py-2 text-sm font-medium ${params.debtMode === mode ? 'bg-fire-600 text-white' : 'text-gray-700 hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-gray-800'}`}
                      >
                        {mode === 'fixed' ? 'Fixed budget' : 'Target date'}
                      </button>
                    ))}
                  </div>
                </div>
                <div>
                  <span className="block text-sm font-medium text-gray-700 dark:text-gray-300">Payoff method</span>
                  <div className="mt-1 inline-flex rounded-lg border border-gray-200 p-1 dark:border-gray-700" role="group" aria-label="Debt payoff strategy">
                    {(['snowball', 'avalanche'] as const).map(strategy => (
                      <button
                        key={strategy}
                        type="button"
                        onClick={() => setParam('debtStrategy', strategy)}
                        aria-pressed={params.debtStrategy === strategy}
                        className={`rounded-md px-3 py-2 text-sm font-medium ${params.debtStrategy === strategy ? 'bg-fire-600 text-white' : 'text-gray-700 hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-gray-800'}`}
                      >
                        {strategy === 'snowball' ? 'Snowball' : 'Avalanche'}
                      </button>
                    ))}
                  </div>
                </div>
                {params.debtMode === 'fixed' ? (
                  <CurrencyInput label="Monthly debt budget" value={params.debtBudget} onChange={value => setParam('debtBudget', value)} tooltip="Total amount available for all monthly debt payments." min={totalMinPayments} showInvalidState />
                ) : (
                  <InputGroup label="Target payoff timeline" value={params.debtMonths} onChange={value => setParam('debtMonths', value)} tooltip="The maximum number of months for the payoff plan." suffix="months" min={1} max={360} />
                )}
                <CurrencyInput label="Extra monthly payment" value={params.debtExtra} onChange={value => setParam('debtExtra', value)} tooltip="Additional money to apply after the regular monthly budget." />
              </div>
              {params.debtMode === 'fixed' && params.debtBudget < totalMinPayments && totalMinPayments > 0 && <p className="text-sm text-red-700 dark:text-red-300">Your monthly budget must cover at least {formatCurrency(totalMinPayments)} in minimum payments.</p>}
            </CardContent>
          </Card>
        </section>

        <section aria-labelledby="debt-outlook-heading" className="space-y-4">
          <div>
            <h2 id="debt-outlook-heading" className="text-xl font-semibold text-gray-900 dark:text-gray-100">Your payoff outlook</h2>
            <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
              {canCalculate
                ? `${params.debtStrategy === 'snowball' ? 'Snowball' : 'Avalanche'} directs extra money toward ${params.debtStrategy === 'snowball' ? 'the smallest balance' : 'the highest interest rate'} first.`
                : 'Add at least one debt and a monthly budget that covers minimum payments to see the payoff plan.'}
            </p>
          </div>
          {results ? (
            <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
              <ResultCard label="Debt-free in" value={results.base.totalMonths} format="none" highlight subtext={`${(results.base.totalMonths / 12).toFixed(1)} years`} />
              <ResultCard label="Total interest" value={results.base.totalInterest} format="currency" subtext={`On ${formatCurrency(totalDebt)} in balances`} />
              <ResultCard label="Monthly payment" value={results.baseBudget + params.debtExtra} format="currency" subtext={params.debtMode === 'target' ? `Estimated for ${params.debtMonths} months` : `${formatCurrency(params.debtExtra)} extra`} />
              <ResultCard label="Extra payment impact" value={results.withExtra ? results.base.totalMonths - results.withExtra.totalMonths : 0} format="none" subtext={results.withExtra ? 'Months saved' : 'Add an extra payment to compare'} />
            </div>
          ) : (
            <div className="border-y border-gray-200 py-5 text-sm text-gray-600 dark:border-gray-800 dark:text-gray-400">No payoff projection is available yet.</div>
          )}
        </section>

        {results && (
          <>
            <section aria-labelledby="debt-timeline-heading">
              <Card>
                <CardHeader><h2 id="debt-timeline-heading" className="text-lg font-semibold text-gray-900 dark:text-gray-100">Debt balance timeline</h2></CardHeader>
                <CardContent><DebtBalanceChart data={results.base.projections} milestones={results.base.debtMilestones} comparisonData={results.withExtra?.projections} height={350} /></CardContent>
              </Card>
            </section>

            <section aria-labelledby="debt-analysis-heading" className="grid gap-6 xl:grid-cols-2">
              <Card>
                <CardHeader><h2 id="debt-analysis-heading" className="text-lg font-semibold text-gray-900 dark:text-gray-100">Payment breakdown</h2></CardHeader>
                <CardContent><DebtBreakdownChart data={results.base.projections} height={330} /></CardContent>
              </Card>
              <Card>
                <CardHeader><h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Payoff order</h2></CardHeader>
                <CardContent>
                  <ol className="space-y-3">
                    {results.base.payoffOrder.map((debtName, index) => (
                      <li key={debtName} className="flex justify-between gap-4 border-b border-gray-100 pb-3 last:border-0 last:pb-0 dark:border-gray-800">
                        <span className="font-medium text-gray-900 dark:text-gray-100">{index + 1}. {debtName}</span>
                        <span className="text-sm text-gray-600 dark:text-gray-400">Month {results.base.debtMilestones.find(milestone => milestone.debtName === debtName)?.month}</span>
                      </li>
                    ))}
                  </ol>
                </CardContent>
              </Card>
            </section>

            {comparisonResults && (
              <section aria-labelledby="debt-comparison-heading">
                <h2 id="debt-comparison-heading" className="text-lg font-semibold text-gray-900 dark:text-gray-100">Method comparison</h2>
                <dl className="mt-3 grid gap-4 sm:grid-cols-2">
                  <div className="border-y border-gray-200 py-4 dark:border-gray-800">
                    <dt className="font-medium text-gray-900 dark:text-gray-100">Snowball</dt>
                    <dd className="mt-1 text-sm text-gray-600 dark:text-gray-400">{comparisonResults.snowball.totalMonths} months · {formatCurrency(comparisonResults.snowball.totalInterest)} interest</dd>
                  </div>
                  <div className="border-y border-gray-200 py-4 dark:border-gray-800">
                    <dt className="font-medium text-gray-900 dark:text-gray-100">Avalanche</dt>
                    <dd className="mt-1 text-sm text-gray-600 dark:text-gray-400">{comparisonResults.avalanche.totalMonths} months · {formatCurrency(comparisonResults.avalanche.totalInterest)} interest</dd>
                  </div>
                </dl>
              </section>
            )}
          </>
        )}

        <CalculatorFooter onExport={handleExport} exportDisabled={!canCalculate} onReset={resetParams} onSave={saveParams} onLoad={loadParams} onCopy={copyUrl} hasCustomParams={hasCustomParams} hasUnsavedChanges={hasUnsavedChanges} hasSavedParams={hasSavedParams} savedAt={savedAt} />
      </div>
    </>
  )
}
