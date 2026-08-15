import { useMemo } from 'react'
import { useCalculatorParams } from '../hooks/useCalculatorParams'
import { calculateBaristaFIRE, formatCurrency } from '../utils/calculations'
import { exportToExcel, prepareInputsForExport, prepareResultsForExport } from '../utils/excelExport'
import { AgeInput, CurrencyInput, CurrencyPeriodProvider, PercentageInput, PeriodToggle, ToggleInput } from '../components/inputs'
import { AdvancedDetails, CalculatorFooter, Card, CardContent, CardHeader, ProgressToFIRE, ResultCard } from '../components/ui'
import { ProjectionChart } from '../components/charts'
import SEO from '../components/SEO'
import { calculatorSEO } from '../config/seo'

export default function BaristaFIRE() {
  const {
    params, setParam, setParamDebounced, resetParams, saveParams, loadParams, copyUrl,
    hasCustomParams, hasUnsavedChanges, hasSavedParams, savedAt,
  } = useCalculatorParams()
  const results = useMemo(() => calculateBaristaFIRE(
    params.currentAge, params.currentSavings, params.annualContribution, params.expectedReturn,
    params.inflationRate, params.annualExpenses, params.withdrawalRate, params.partTimeIncome,
    params.contributionGrowth,
  ), [params])
  const portfolioReduction = results.fullFireNumber - results.baristaNumber

  const handleExport = () => {
    const { values: inputs, formats: inputFormats } = prepareInputsForExport({
      currentAge: params.currentAge, currentSavings: params.currentSavings, annualContribution: params.annualContribution,
      expectedReturn: params.expectedReturn, inflationRate: params.inflationRate, annualExpenses: params.annualExpenses,
      withdrawalRate: params.withdrawalRate, partTimeIncome: params.partTimeIncome,
    })
    const { values: resultValues, formats: resultFormats } = prepareResultsForExport(results)
    exportToExcel({
      calculatorName: 'Barista FIRE', inputs, results: resultValues, projections: results.projections, inputFormats, resultFormats,
      resultFormulas: {
        fullFireNumber: '{annualExpenses}/{withdrawalRate}',
        baristaNumber: 'MAX(0,{annualExpenses}-{partTimeIncome})/{withdrawalRate}',
      },
    })
  }

  return (
    <CurrencyPeriodProvider period={params.currencyPeriod} onChange={value => setParam('currencyPeriod', value)}>
      <SEO {...calculatorSEO.barista} />
      <div className="space-y-8">
        <header>
          <h1 className="text-2xl font-bold text-gray-900 sm:text-3xl dark:text-gray-100">Barista FIRE Calculator</h1>
          <p className="mt-1 text-gray-600 dark:text-gray-400">See how part-time take-home income can reduce the portfolio needed for retirement.</p>
        </header>

        <section aria-labelledby="barista-plan-heading">
          <Card>
            <CardHeader>
              <h2 id="barista-plan-heading" className="text-lg font-semibold text-gray-900 dark:text-gray-100">Start with your plan</h2>
              <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">Pair today-dollar spending with the income you expect from flexible work.</p>
              <PeriodToggle className="mt-3" />
            </CardHeader>
            <CardContent className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
              <AgeInput label="Current age" value={params.currentAge} onChange={value => setParam('currentAge', value)} onSliderChange={value => setParamDebounced('currentAge', value)} tooltip="Your current age." min={18} max={80} showSlider />
              <CurrencyInput label="Current invested assets" value={params.currentSavings} onChange={value => setParam('currentSavings', value)} tooltip="Investments available for retirement." />
              <CurrencyInput label="Contributions" value={params.annualContribution} onChange={value => setParam('annualContribution', value)} tooltip="How much you expect to invest before Barista FIRE." periodic />
              <CurrencyInput label="Retirement spending (today's dollars)" value={params.annualExpenses} onChange={value => setParam('annualExpenses', value)} tooltip="Expected after-tax spending in retirement, expressed in today’s purchasing power." periodic />
              <CurrencyInput label="After-tax part-time take-home income" value={params.partTimeIncome} onChange={value => setParam('partTimeIncome', value)} tooltip="Expected income after taxes from part-time or flexible work." periodic />
            </CardContent>
          </Card>
        </section>

        <section aria-labelledby="barista-outlook-heading" className="space-y-4">
          <div>
            <h2 id="barista-outlook-heading" className="text-xl font-semibold text-gray-900 dark:text-gray-100">Your outlook</h2>
            <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">{formatCurrency(portfolioReduction)} less portfolio is needed than a plan without part-time income.</p>
          </div>
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
            <ResultCard label="Barista FIRE number" value={results.baristaNumber} format="currency" highlight subtext="Portfolio target with part-time income" />
            <ResultCard label="Full FIRE number" value={results.fullFireNumber} format="currency" subtext="Without part-time income" />
            <ResultCard label="Years to Barista FIRE" value={results.yearsToBaristaFIRE} format="years" subtext={`Estimated age ${Math.round(params.currentAge + results.yearsToBaristaFIRE)}`} />
          </div>
          <ProgressToFIRE currentSavings={params.currentSavings} fireNumber={results.baristaNumber} yearsToFIRE={results.yearsToBaristaFIRE} label="Progress to Barista FIRE" targetLabel="Barista FIRE number" />
        </section>

        <AdvancedDetails description="These long-term assumptions govern investment growth and portfolio withdrawals.">
          <PercentageInput label="Expected annual return" value={params.expectedReturn} onChange={value => setParam('expectedReturn', value)} onSliderChange={value => setParamDebounced('expectedReturn', value)} tooltip="Average annual investment return before inflation." min={0} max={0.15} />
          <PercentageInput label="Inflation rate" value={params.inflationRate} onChange={value => setParam('inflationRate', value)} onSliderChange={value => setParamDebounced('inflationRate', value)} tooltip="Expected annual price growth." min={0} max={0.1} />
          <PercentageInput label="Withdrawal rate" value={params.withdrawalRate} onChange={value => setParam('withdrawalRate', value)} onSliderChange={value => setParamDebounced('withdrawalRate', value)} tooltip="Share of the portfolio available for annual spending." min={0.02} max={0.06} />
          <ToggleInput
            label="Increase contributions with inflation"
            tooltip="On: the amount you invest rises with inflation, so its purchasing power stays constant. Off: you invest the same dollar amount every year and its purchasing power erodes."
            checked={params.contributionGrowth === 'inflation'}
            onChange={checked => setParam('contributionGrowth', checked ? 'inflation' : 'flat')}
            className="sm:col-span-2"
          />
        </AdvancedDetails>

        <section aria-labelledby="barista-projection-heading">
          <Card>
            <CardHeader><h2 id="barista-projection-heading" className="text-lg font-semibold text-gray-900 dark:text-gray-100">Portfolio projection</h2></CardHeader>
            <CardContent><ProjectionChart data={results.projections} fireNumber={results.baristaNumber} inflationRate={params.inflationRate} colorScheme="amber" height={350} /></CardContent>
          </Card>
        </section>

        <p className="max-w-3xl text-sm text-gray-600 dark:text-gray-400">This estimate assumes part-time income continues to cover the stated share of spending. Health coverage, taxes, and the reliability of that income deserve separate planning.</p>
        <CalculatorFooter onExport={handleExport} onReset={resetParams} onSave={saveParams} onLoad={loadParams} onCopy={copyUrl} hasCustomParams={hasCustomParams} hasUnsavedChanges={hasUnsavedChanges} hasSavedParams={hasSavedParams} savedAt={savedAt} />
      </div>
    </CurrencyPeriodProvider>
  )
}
