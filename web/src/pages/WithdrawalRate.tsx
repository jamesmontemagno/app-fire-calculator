import { useMemo } from 'react'
import { useCalculatorParams } from '../hooks/useCalculatorParams'
import { calculateWithdrawal, formatCurrency } from '../utils/calculations'
import { exportToExcel, prepareInputsForExport, prepareResultsForExport } from '../utils/excelExport'
import { CurrencyInput, InputGroup, PercentageInput } from '../components/inputs'
import { AdvancedDetails, CalculatorFooter, Card, CardContent, CardHeader, ResultCard } from '../components/ui'
import { WithdrawalChart } from '../components/charts'
import SEO from '../components/SEO'
import { calculatorSEO } from '../config/seo'

export default function WithdrawalRate() {
  const {
    params, setParam, setParamDebounced, resetParams, saveParams, loadParams, copyUrl,
    hasCustomParams, hasUnsavedChanges, hasSavedParams, savedAt,
  } = useCalculatorParams()
  const results = useMemo(() => calculateWithdrawal(
    params.portfolioValue, params.withdrawalRate, params.expectedReturn, params.inflationRate, params.retirementYears,
  ), [params])

  const handleExport = () => {
    const { values: inputs, formats: inputFormats } = prepareInputsForExport({
      portfolioValue: params.portfolioValue, withdrawalRate: params.withdrawalRate, expectedReturn: params.expectedReturn,
      inflationRate: params.inflationRate, retirementYears: params.retirementYears,
    })
    const { values: resultValues, formats: resultFormats } = prepareResultsForExport(results)
    exportToExcel({
      calculatorName: 'Withdrawal Rate', inputs, results: resultValues, projections: results.withdrawalProjections,
      additionalSheets: [{ name: 'Rate Analysis', data: results.rateAnalysis }], inputFormats, resultFormats,
      resultFormulas: {
        annualWithdrawal: '{portfolioValue}*{withdrawalRate}',
        monthlyWithdrawal: '({portfolioValue}*{withdrawalRate})/12',
      },
    })
  }

  return (
    <>
      <SEO {...calculatorSEO.withdrawal} />
      <div className="space-y-8">
        <header>
          <h1 className="text-2xl font-bold text-gray-900 sm:text-3xl dark:text-gray-100">Withdrawal Rate Calculator</h1>
          <p className="mt-1 text-gray-600 dark:text-gray-400">Test how a starting withdrawal may affect portfolio longevity.</p>
        </header>

        <section aria-labelledby="withdrawal-plan-heading">
          <Card>
            <CardHeader>
              <h2 id="withdrawal-plan-heading" className="text-lg font-semibold text-gray-900 dark:text-gray-100">Start with your withdrawal plan</h2>
              <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">Choose a portfolio, a first-year withdrawal rate, and how long the money needs to last.</p>
            </CardHeader>
            <CardContent className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
              <CurrencyInput label="Portfolio value" value={params.portfolioValue} onChange={value => setParam('portfolioValue', value)} tooltip="Current total invested assets available for withdrawals." />
              <PercentageInput label="Withdrawal rate" value={params.withdrawalRate} onChange={value => setParam('withdrawalRate', value)} onSliderChange={value => setParamDebounced('withdrawalRate', value)} tooltip="Percentage of the starting portfolio withdrawn in the first year." min={0.02} max={0.08} step={0.005} />
              <InputGroup label="Retirement duration" value={params.retirementYears} onChange={value => setParam('retirementYears', value)} onSliderChange={value => setParamDebounced('retirementYears', value)} tooltip="How many years the portfolio needs to support withdrawals." suffix="years" min={10} max={60} showSlider />
            </CardContent>
          </Card>
        </section>

        <section aria-labelledby="withdrawal-outlook-heading" className="space-y-4">
          <div>
            <h2 id="withdrawal-outlook-heading" className="text-xl font-semibold text-gray-900 dark:text-gray-100">{results.portfolioLongevity >= params.retirementYears ? 'This plan reaches your time horizon' : 'This plan may fall short'}</h2>
            <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">The model increases withdrawals with inflation each year and projects a steady return.</p>
          </div>
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
            <ResultCard label="Annual withdrawal" value={results.annualWithdrawal} format="currency" highlight subtext="First year" />
            <ResultCard label="Monthly withdrawal" value={results.monthlyWithdrawal} format="currency" subtext="First year" />
            <ResultCard label="Portfolio lasts" value={results.portfolioLongevity} format="years" subtext={results.portfolioLongevity >= params.retirementYears ? 'Meets your selected duration' : 'Below your selected duration'} />
            <ResultCard label="Share of horizon funded" value={results.horizonFundedRatio} format="percent" subtext="Single fixed-return scenario, not a success probability" />
          </div>
        </section>

        <AdvancedDetails description="These assumptions affect how withdrawals grow and how the portfolio is projected to recover.">
          <PercentageInput label="Expected annual return" value={params.expectedReturn} onChange={value => setParam('expectedReturn', value)} onSliderChange={value => setParamDebounced('expectedReturn', value)} tooltip="Average annual investment return before inflation." min={0} max={0.15} />
          <PercentageInput label="Inflation rate" value={params.inflationRate} onChange={value => setParam('inflationRate', value)} onSliderChange={value => setParamDebounced('inflationRate', value)} tooltip="Expected annual increase in withdrawals and prices." min={0} max={0.1} />
        </AdvancedDetails>

        <section aria-labelledby="withdrawal-projection-heading">
          <Card>
            <CardHeader><h2 id="withdrawal-projection-heading" className="text-lg font-semibold text-gray-900 dark:text-gray-100">Portfolio balance over time</h2></CardHeader>
            <CardContent><WithdrawalChart data={results.withdrawalProjections} height={320} /></CardContent>
          </Card>
        </section>

        <section aria-labelledby="rate-analysis-heading">
          <Card>
            <CardHeader>
              <h2 id="rate-analysis-heading" className="text-lg font-semibold text-gray-900 dark:text-gray-100">Compare withdrawal rates</h2>
              <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">See the same portfolio tested against nearby first-year rates.</p>
            </CardHeader>
            <CardContent>
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-gray-200 dark:border-gray-700">
                      {['Rate', 'Annual withdrawal', 'Monthly withdrawal', 'Portfolio lasts', 'Assessment'].map(label => <th key={label} className="whitespace-nowrap px-3 py-3 text-left font-semibold text-gray-900 dark:text-gray-100">{label}</th>)}
                    </tr>
                  </thead>
                  <tbody>
                    {results.rateAnalysis.map(analysis => {
                      const meetsGoal = analysis.years >= params.retirementYears
                      return (
                        <tr key={analysis.rate} className={analysis.rate === params.withdrawalRate ? 'border-b border-gray-100 bg-gray-50 dark:border-gray-800 dark:bg-gray-800/50' : 'border-b border-gray-100 dark:border-gray-800'}>
                          <td className="px-3 py-3 font-medium text-gray-900 dark:text-gray-100">{(analysis.rate * 100).toFixed(1)}%</td>
                          <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{formatCurrency(params.portfolioValue * analysis.rate)}</td>
                          <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{formatCurrency(params.portfolioValue * analysis.rate / 12)}</td>
                          <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{analysis.years >= 50 ? '50+ years' : `${analysis.years} years`}</td>
                          <td className={`px-3 py-3 font-medium ${meetsGoal ? 'text-green-700 dark:text-green-400' : 'text-amber-700 dark:text-amber-300'}`}>{meetsGoal ? 'Meets duration' : 'May fall short'}</td>
                        </tr>
                      )
                    })}
                  </tbody>
                </table>
              </div>
            </CardContent>
          </Card>
        </section>

        <CalculatorFooter onExport={handleExport} onReset={resetParams} onSave={saveParams} onLoad={loadParams} onCopy={copyUrl} hasCustomParams={hasCustomParams} hasUnsavedChanges={hasUnsavedChanges} hasSavedParams={hasSavedParams} savedAt={savedAt} />
      </div>
    </>
  )
}
