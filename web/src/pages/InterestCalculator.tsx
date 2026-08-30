import { useMemo } from 'react'
import { BalanceProjectionChart } from '../components/charts'
import { CurrencyInput, InputGroup, PercentageInput } from '../components/inputs'
import { CalculatorFooter, Card, CardContent, CardHeader, ResultCard } from '../components/ui'
import SEO from '../components/SEO'
import { calculatorSEO } from '../config/seo'
import { useCalculatorParams } from '../hooks/useCalculatorParams'
import { calculateInterest, formatCurrency } from '../utils/calculations'
import { exportToExcel, prepareInputsForExport, prepareResultsForExport } from '../utils/excelExport'

export default function InterestCalculator() {
  const {
    params, setParam, setParamDebounced, resetParams, saveParams, loadParams, copyUrl,
    hasCustomParams, hasUnsavedChanges, hasSavedParams, savedAt,
  } = useCalculatorParams()
  const results = useMemo(() => calculateInterest(
    params.interestStartingBalance,
    params.interestMonthlyContribution,
    params.interestAnnualRate,
    params.interestYears,
  ), [params.interestAnnualRate, params.interestMonthlyContribution, params.interestStartingBalance, params.interestYears])

  const handleExport = () => {
    const { values: inputs, formats: inputFormats } = prepareInputsForExport({
      startingBalance: params.interestStartingBalance,
      monthlyContribution: params.interestMonthlyContribution,
      annualInterestRate: params.interestAnnualRate,
      years: params.interestYears,
    })
    const { values: resultValues, formats: resultFormats } = prepareResultsForExport(results)
    exportToExcel({
      calculatorName: 'Interest Calculator',
      inputs,
      results: resultValues,
      projections: results.projections,
      inputFormats,
      resultFormats,
    })
  }

  return (
    <>
      <SEO {...calculatorSEO.interest} />
      <div className="space-y-8">
        <header>
          <h1 className="text-2xl font-bold text-content sm:text-3xl">Interest Calculator</h1>
          <p className="mt-1 text-content-muted">Estimate how monthly compounding and regular deposits grow your money.</p>
        </header>

        <section aria-labelledby="interest-inputs-heading">
          <Card>
            <CardHeader>
              <h2 id="interest-inputs-heading" className="text-lg font-semibold text-content">Your savings plan</h2>
              <p className="mt-1 text-sm text-content-muted">Deposits are added at the end of each month.</p>
            </CardHeader>
            <CardContent className="field-grid grid gap-4 sm:grid-cols-2">
              <CurrencyInput label="Starting balance" value={params.interestStartingBalance} onChange={value => setParam('interestStartingBalance', value)} tooltip="The amount already in the account." />
              <CurrencyInput label="Monthly contribution" value={params.interestMonthlyContribution} onChange={value => setParam('interestMonthlyContribution', value)} tooltip="The amount deposited at the end of each month." />
              <PercentageInput label="Annual interest rate" value={params.interestAnnualRate} onChange={value => setParam('interestAnnualRate', value)} onSliderChange={value => setParamDebounced('interestAnnualRate', value)} tooltip="The stated annual rate, compounded monthly." min={0} max={0.5} />
              <InputGroup label="Time period" value={params.interestYears} onChange={value => setParam('interestYears', value)} onSliderChange={value => setParamDebounced('interestYears', value)} tooltip="How many years the balance earns interest." suffix="years" min={1} max={60} showSlider />
            </CardContent>
          </Card>
        </section>

        <section aria-labelledby="interest-results-heading" className="space-y-4">
          <h2 id="interest-results-heading" className="text-xl font-semibold text-content">Your interest outlook</h2>
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
            <ResultCard label="Ending balance" value={results.endingBalance} format="currency" highlight subtext={`After ${params.interestYears} years`} />
            <ResultCard label="Interest earned" value={results.interestEarned} format="currency" subtext="Growth above your deposits" />
            <ResultCard label="Total contributions" value={results.totalContributions} format="currency" subtext="Starting balance plus deposits" />
            <ResultCard label="Effective annual yield" value={results.effectiveAnnualYield} format="percent" subtext="From monthly compounding" />
          </div>
        </section>

        <section aria-labelledby="interest-projection-heading">
          <Card>
            <CardHeader>
              <h2 id="interest-projection-heading" className="text-lg font-semibold text-content">Balance growth</h2>
              <p className="mt-1 text-sm text-content-muted">Compare your deposits with the projected balance.</p>
            </CardHeader>
            <CardContent>
              <BalanceProjectionChart
                data={results.projections}
                xKey="year"
                xLabel="Year"
                series={[
                  { key: 'balance', name: 'Balance', tone: 'primary' },
                  { key: 'totalContributions', name: 'Contributions', tone: 'secondary', dashed: true },
                ]}
                height={360}
              />
            </CardContent>
          </Card>
        </section>

        <section aria-labelledby="interest-method-heading">
          <h2 id="interest-method-heading" className="text-lg font-semibold text-content">How this estimate works</h2>
          <p className="mt-2 text-sm text-content-muted">
            Each month, the prior balance earns one-twelfth of the annual rate, then the monthly contribution is added.
            At this rate, your deposits contribute {formatCurrency(results.totalContributions)} and estimated interest contributes {formatCurrency(results.interestEarned)}.
          </p>
        </section>

        <CalculatorFooter onExport={handleExport} onReset={resetParams} onSave={saveParams} onLoad={loadParams} onCopy={copyUrl} hasCustomParams={hasCustomParams} hasUnsavedChanges={hasUnsavedChanges} hasSavedParams={hasSavedParams} savedAt={savedAt} />
      </div>
    </>
  )
}
