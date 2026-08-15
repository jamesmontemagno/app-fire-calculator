import { useMemo } from 'react'
import { useCalculatorParams } from '../hooks/useCalculatorParams'
import { calculateInvestmentGrowth, formatCurrency } from '../utils/calculations'
import { exportToExcel, prepareInputsForExport, prepareResultsForExport } from '../utils/excelExport'
import { AgeInput, CurrencyInput, CurrencyPeriodProvider, InputGroup, PercentageInput, PeriodToggle, ToggleInput } from '../components/inputs'
import { AdvancedDetails, CalculatorFooter, Card, CardContent, CardHeader, ResultCard } from '../components/ui'
import { ProjectionChart } from '../components/charts'
import SEO from '../components/SEO'
import { calculatorSEO } from '../config/seo'

export default function SavingsRate() {
  const {
    params, setParam, setParamDebounced, resetParams, saveParams, loadParams, copyUrl,
    hasCustomParams, hasUnsavedChanges, hasSavedParams, savedAt,
  } = useCalculatorParams()
  const results = useMemo(() => calculateInvestmentGrowth(
    params.currentSavings, params.savingsContribution, params.savingsFrequency, params.savingsYears,
    params.expectedReturn, params.inflationRate, params.annualIncome, params.currentAge,
    params.contributionGrowth,
  ), [params])

  const handleExport = () => {
    const { values: inputs, formats: inputFormats } = prepareInputsForExport({
      currentSavings: params.currentSavings,
      contributionAmount: params.savingsContribution,
      contributionFrequency: params.savingsFrequency,
      yearsInvesting: params.savingsYears,
      annualIncome: params.annualIncome,
      expectedReturn: params.expectedReturn,
      inflationRate: params.inflationRate,
      currentAge: params.currentAge,
    })
    const { values: resultValues, formats: resultFormats } = prepareResultsForExport(results)
    exportToExcel({
      calculatorName: 'Savings & Investment Rate', inputs, results: resultValues, projections: results.projections,
      inputFormats, resultFormats,
      resultFormulas: {
        savingsRate: params.savingsFrequency === 'monthly'
          ? 'IF({annualIncome}>0,({contributionAmount}*12)/{annualIncome},0)'
          : 'IF({annualIncome}>0,{contributionAmount}/{annualIncome},0)',
      },
    })
  }

  return (
    <CurrencyPeriodProvider period={params.currencyPeriod} onChange={value => setParam('currencyPeriod', value)}>
      <SEO {...calculatorSEO['savings-rate']} />
      <div className="space-y-8">
        <header>
          <h1 className="text-2xl font-bold text-content sm:text-3xl">Savings &amp; Investment Rate Calculator</h1>
          <p className="mt-1 text-content-muted">See how a repeatable contribution plan can grow over time.</p>
        </header>

        <section aria-labelledby="savings-plan-heading">
          <Card>
            <CardHeader>
              <h2 id="savings-plan-heading" className="text-lg font-semibold text-content">Start with your plan</h2>
              <p className="mt-1 text-sm text-content-muted">Use after-tax take-home income to see the share of income you are investing.</p>
              <PeriodToggle className="mt-3" />
            </CardHeader>
            <CardContent className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
              <AgeInput label="Current age" value={params.currentAge} onChange={value => setParam('currentAge', value)} onSliderChange={value => setParamDebounced('currentAge', value)} tooltip="Used to label the projection timeline." min={18} max={80} showSlider />
              <CurrencyInput label="Starting amount" value={params.currentSavings} onChange={value => setParam('currentSavings', value)} tooltip="Investments already in the account." />
              <div className="space-y-2">
                <span className="block text-sm font-medium text-content-muted">Contribution frequency</span>
                <div className="inline-flex rounded-control border border-border-subtle p-1" role="group" aria-label="Contribution frequency">
                  {(['monthly', 'yearly'] as const).map(frequency => (
                    <button
                      key={frequency}
                      type="button"
                      onClick={() => setParam('savingsFrequency', frequency)}
                      aria-pressed={params.savingsFrequency === frequency}
                      className={`rounded-control px-3 py-2 text-sm font-medium ${params.savingsFrequency === frequency ? 'bg-accent text-accent-contrast' : 'text-content-muted hover:bg-surface-sunken hover:text-content'}`}
                    >
                      {frequency === 'monthly' ? 'Monthly' : 'Yearly'}
                    </button>
                  ))}
                </div>
              </div>
              <CurrencyInput label={`${params.savingsFrequency === 'monthly' ? 'Monthly' : 'Annual'} contribution`} value={params.savingsContribution} onChange={value => setParam('savingsContribution', value)} tooltip={`Amount you invest each ${params.savingsFrequency === 'monthly' ? 'month' : 'year'}.`} />
              <InputGroup label="Years investing" value={params.savingsYears} onChange={value => setParam('savingsYears', value)} onSliderChange={value => setParamDebounced('savingsYears', value)} tooltip="How long contributions and investment growth continue." suffix="years" min={1} max={50} showSlider />
              <CurrencyInput label="After-tax take-home income" value={params.annualIncome} onChange={value => setParam('annualIncome', value)} tooltip="Income after taxes, used to calculate the savings rate." periodic />
            </CardContent>
          </Card>
        </section>

        <section aria-labelledby="savings-outlook-heading" className="space-y-4">
          <div>
            <h2 id="savings-outlook-heading" className="text-xl font-semibold text-content">Your outlook</h2>
            <p className="mt-1 text-sm text-content-muted">The inflation-adjusted result reports purchasing power in today&apos;s dollars.</p>
          </div>
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
            <ResultCard label="Investment rate" value={results.savingsRate} format="percent" highlight subtext={`${formatCurrency(results.annualContribution)} invested yearly`} />
            <ResultCard label="Projected balance" value={results.finalNominalBalance} format="currency" subtext={`In ${params.savingsYears} years`} />
            <ResultCard label="Value in today's dollars" value={results.finalInflationAdjustedBalance} format="currency" subtext="Inflation-adjusted purchasing power" />
            <ResultCard label="Investment growth" value={results.totalGrowth} format="currency" subtext="Projected returns above contributions" />
          </div>
        </section>

        <AdvancedDetails description="These assumptions control the nominal and inflation-adjusted growth scenarios.">
          <PercentageInput label="Expected annual return" value={params.expectedReturn} onChange={value => setParam('expectedReturn', value)} onSliderChange={value => setParamDebounced('expectedReturn', value)} tooltip="Average annual investment return before inflation." min={0} max={0.15} />
          <PercentageInput label="Inflation rate" value={params.inflationRate} onChange={value => setParam('inflationRate', value)} onSliderChange={value => setParamDebounced('inflationRate', value)} tooltip="Expected annual price growth." min={0} max={0.1} />
          <ToggleInput
            label="Increase contributions with inflation"
            tooltip="On: the amount you invest rises with inflation, so its purchasing power stays constant. Off: you invest the same dollar amount every year and its purchasing power erodes."
            checked={params.contributionGrowth === 'inflation'}
            onChange={checked => setParam('contributionGrowth', checked ? 'inflation' : 'flat')}
            className="sm:col-span-2"
          />
        </AdvancedDetails>

        <section aria-labelledby="savings-projection-heading">
          <Card>
            <CardHeader>
              <h2 id="savings-projection-heading" className="text-lg font-semibold text-content">Investment growth projection</h2>
              <p className="mt-1 text-sm text-content-muted">The dashed series shows the same plan in today&apos;s dollars.</p>
            </CardHeader>
            <CardContent><ProjectionChart data={results.projections} showMilestones={false} colorScheme="purple" height={380} /></CardContent>
          </Card>
        </section>

        <section aria-labelledby="savings-breakdown-heading">
          <h2 id="savings-breakdown-heading" className="text-lg font-semibold text-content">Supporting analysis</h2>
          <dl className="mt-3 grid gap-3 text-sm sm:grid-cols-2 xl:grid-cols-4">
            <div><dt className="text-content-muted">Starting amount</dt><dd className="mt-1 font-semibold text-content">{formatCurrency(params.currentSavings)}</dd></div>
            <div><dt className="text-content-muted">Total contributions</dt><dd className="mt-1 font-semibold text-content">{formatCurrency(results.totalInvested)}</dd></div>
            <div><dt className="text-content-muted">Inflation impact</dt><dd className="mt-1 font-semibold text-content">{formatCurrency(results.inflationImpact)}</dd></div>
            <div><dt className="text-content-muted">Monthly contribution</dt><dd className="mt-1 font-semibold text-content">{formatCurrency(results.monthlyContribution)}</dd></div>
          </dl>
        </section>

        <CalculatorFooter onExport={handleExport} onReset={resetParams} onSave={saveParams} onLoad={loadParams} onCopy={copyUrl} hasCustomParams={hasCustomParams} hasUnsavedChanges={hasUnsavedChanges} hasSavedParams={hasSavedParams} savedAt={savedAt} />
      </div>
    </CurrencyPeriodProvider>
  )
}
