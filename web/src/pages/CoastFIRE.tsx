import { useMemo } from 'react'
import { useCalculatorParams } from '../hooks/useCalculatorParams'
import { calculateCoastFIRE, formatCurrency } from '../utils/calculations'
import { exportToExcel, prepareInputsForExport, prepareResultsForExport } from '../utils/excelExport'
import { AgeInput, CurrencyInput, CurrencyPeriodProvider, PercentageInput, PeriodToggle, ToggleInput } from '../components/inputs'
import { AdvancedDetails, CalculatorFooter, Card, CardContent, CardHeader, ProgressToFIRE, ResultCard } from '../components/ui'
import { ProjectionChart } from '../components/charts'
import SEO from '../components/SEO'
import { calculatorSEO } from '../config/seo'

export default function CoastFIRE() {
  const {
    params, setParam, setParamDebounced, resetParams, saveParams, loadParams, copyUrl,
    hasCustomParams, hasUnsavedChanges, hasSavedParams, savedAt,
  } = useCalculatorParams()
  const results = useMemo(() => calculateCoastFIRE(
    params.currentAge, params.retirementAge, params.currentSavings, params.annualContribution,
    params.expectedReturn, params.inflationRate, params.annualExpenses, params.withdrawalRate,
    params.contributionGrowth,
  ), [params])

  const handleExport = () => {
    const { values: inputs, formats: inputFormats } = prepareInputsForExport({
      currentAge: params.currentAge, retirementAge: params.retirementAge, currentSavings: params.currentSavings,
      annualContribution: params.annualContribution, expectedReturn: params.expectedReturn, inflationRate: params.inflationRate,
      annualExpenses: params.annualExpenses, withdrawalRate: params.withdrawalRate,
    })
    const { values: resultValues, formats: resultFormats } = prepareResultsForExport(results)
    exportToExcel({
      calculatorName: 'Coast FIRE', inputs, results: resultValues, projections: results.projections,
      additionalSheets: [{ name: 'With Contributions', data: results.projectionsWithContributions }],
      inputFormats, resultFormats, resultFormulas: { fireNumber: '{annualExpenses}/{withdrawalRate}' },
    })
  }

  return (
    <CurrencyPeriodProvider period={params.currencyPeriod} onChange={value => setParam('currencyPeriod', value)}>
      <SEO {...calculatorSEO.coast} />
      <div className="space-y-8">
        <header>
          <h1 className="text-2xl font-bold text-content sm:text-3xl">Coast FIRE Calculator</h1>
          <p className="mt-1 text-content-muted">Find the balance that can grow to your retirement target without more contributions.</p>
        </header>

        <section aria-labelledby="coast-plan-heading">
          <Card>
            <CardHeader>
              <h2 id="coast-plan-heading" className="text-lg font-semibold text-content">Start with your plan</h2>
              <p className="mt-1 text-sm text-content-muted">Your target age and today-dollar retirement spending set the Coast FIRE threshold.</p>
              <PeriodToggle className="mt-3" />
            </CardHeader>
            <CardContent className="field-grid grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
              <AgeInput label="Current age" value={params.currentAge} onChange={value => setParam('currentAge', value)} onSliderChange={value => setParamDebounced('currentAge', value)} tooltip="Your current age." min={18} max={80} showSlider />
              <AgeInput label="Target retirement age" value={params.retirementAge} onChange={value => setParam('retirementAge', value)} onSliderChange={value => setParamDebounced('retirementAge', value)} tooltip="The age your Coast portfolio should support." min={params.currentAge} max={90} showSlider />
              <CurrencyInput label="Current invested assets" value={params.currentSavings} onChange={value => setParam('currentSavings', value)} tooltip="Investments available for retirement." />
              <CurrencyInput label="Contributions" value={params.annualContribution} onChange={value => setParam('annualContribution', value)} tooltip="Contributions used only to estimate how soon you can reach Coast FIRE." periodic />
              <CurrencyInput label="Expenses" value={params.annualExpenses} onChange={value => setParam('annualExpenses', value)} tooltip="Expected after-tax spending in retirement, stated in today’s purchasing power." periodic />
            </CardContent>
          </Card>
        </section>

        <section aria-labelledby="coast-outlook-heading" className="space-y-4">
          <div>
            <h2 id="coast-outlook-heading" className="text-xl font-semibold text-content">{results.alreadyCoasting ? 'You are already Coast FIRE' : 'Your Coast FIRE outlook'}</h2>
            <p className="mt-1 text-sm text-content-muted">
              {results.alreadyCoasting ? 'Your current investments can reach the full FIRE target through projected growth alone.' : `Continue contributing to close the ${formatCurrency(Math.max(0, results.coastNumber - params.currentSavings))} Coast FIRE gap.`}
            </p>
          </div>
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
            <ResultCard label="Coast FIRE number" value={results.coastNumber} format="currency" highlight subtext="Amount needed today" />
            <ResultCard label="Full FIRE number" value={results.fireNumber} format="currency" subtext={`At age ${params.retirementAge}`} />
            <ResultCard label={results.alreadyCoasting ? 'Status' : 'Time to Coast FIRE'} value={results.alreadyCoasting ? 'Coasting' : results.yearsToCoast} format={results.alreadyCoasting ? 'none' : 'years'} subtext={results.alreadyCoasting ? 'No further contributions required for this estimate' : 'With current annual contributions'} />
          </div>
          <ProgressToFIRE currentSavings={params.currentSavings} fireNumber={results.coastNumber} yearsToFIRE={results.yearsToCoast} label="Progress to Coast FIRE" targetLabel="Coast number" />
        </section>

        <AdvancedDetails description="These assumptions govern how the balance and retirement spending change over time.">
          <PercentageInput label="Expected annual return" value={params.expectedReturn} onChange={value => setParam('expectedReturn', value)} onSliderChange={value => setParamDebounced('expectedReturn', value)} tooltip="Average annual investment return before inflation." min={0} max={0.15} />
          <PercentageInput label="Inflation rate" value={params.inflationRate} onChange={value => setParam('inflationRate', value)} onSliderChange={value => setParamDebounced('inflationRate', value)} tooltip="Expected annual price growth." min={0} max={0.1} />
          <PercentageInput label="Withdrawal rate" value={params.withdrawalRate} onChange={value => setParam('withdrawalRate', value)} onSliderChange={value => setParamDebounced('withdrawalRate', value)} tooltip="Share of the portfolio available for annual retirement spending." min={0.02} max={0.06} />
          <ToggleInput
            label="Increase contributions with inflation"
            tooltip="On: the amount you invest rises with inflation, so its purchasing power stays constant. Off: you invest the same dollar amount every year and its purchasing power erodes."
            checked={params.contributionGrowth === 'inflation'}
            onChange={checked => setParam('contributionGrowth', checked ? 'inflation' : 'flat')}
            className="sm:col-span-2"
          />
        </AdvancedDetails>

        <section aria-labelledby="coast-projection-heading">
          <Card>
            <CardHeader>
              <h2 id="coast-projection-heading" className="text-lg font-semibold text-content">Continue contributing</h2>
              <p className="mt-1 text-sm text-content-muted">The projection includes your stated contributions. The FIRE line represents the retirement target in today&apos;s dollars.</p>
            </CardHeader>
            <CardContent><ProjectionChart data={results.projectionsWithContributions} fireNumber={results.fireNumber} inflationRate={params.inflationRate} colorScheme="blue" height={350} /></CardContent>
          </Card>
        </section>

        <p className="max-w-3xl text-sm text-content-muted">Coast FIRE does not mean you can cover current living costs without work. It means your retirement savings could grow to the future target if the assumed return and inflation rates hold.</p>
        <CalculatorFooter onExport={handleExport} onReset={resetParams} onSave={saveParams} onLoad={loadParams} onCopy={copyUrl} hasCustomParams={hasCustomParams} hasUnsavedChanges={hasUnsavedChanges} hasSavedParams={hasSavedParams} savedAt={savedAt} />
      </div>
    </CurrencyPeriodProvider>
  )
}
