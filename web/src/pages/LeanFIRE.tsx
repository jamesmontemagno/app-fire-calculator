import { useMemo } from 'react'
import { useCalculatorParams } from '../hooks/useCalculatorParams'
import { calculateLeanFIRE, formatCurrency } from '../utils/calculations'
import { exportToExcel, prepareInputsForExport, prepareResultsForExport } from '../utils/excelExport'
import { AgeInput, CurrencyInput, CurrencyPeriodProvider, PercentageInput, PeriodToggle, ToggleInput } from '../components/inputs'
import { AdvancedDetails, CalculatorFooter, Card, CardContent, CardHeader, ProgressToFIRE, ResultCard } from '../components/ui'
import { ProjectionChart } from '../components/charts'
import SEO from '../components/SEO'
import { calculatorSEO } from '../config/seo'

const LEAN_THRESHOLD = 40000

export default function LeanFIRE() {
  const {
    params, setParam, setParamDebounced, resetParams, saveParams, loadParams, copyUrl,
    hasCustomParams, hasUnsavedChanges, hasSavedParams, savedAt,
  } = useCalculatorParams()
  const results = useMemo(() => calculateLeanFIRE({
    currentAge: params.currentAge,
    retirementAge: params.retirementAge,
    currentSavings: params.currentSavings,
    annualContribution: params.annualContribution,
    annualIncome: params.annualIncome,
    expectedReturn: params.expectedReturn,
    inflationRate: params.inflationRate,
    withdrawalRate: params.withdrawalRate,
    annualExpenses: params.annualExpenses,
    contributionGrowth: params.contributionGrowth,
  }), [params])

  const handleExport = () => {
    const { values: inputs, formats: inputFormats } = prepareInputsForExport({
      currentAge: params.currentAge, retirementAge: params.retirementAge, currentSavings: params.currentSavings,
      annualContribution: params.annualContribution, annualIncome: params.annualIncome, expectedReturn: params.expectedReturn,
      inflationRate: params.inflationRate, withdrawalRate: params.withdrawalRate, annualExpenses: params.annualExpenses,
    })
    const { values: resultValues, formats: resultFormats } = prepareResultsForExport(results)
    exportToExcel({
      calculatorName: 'Lean FIRE', inputs, results: resultValues, projections: results.projections, inputFormats, resultFormats,
      resultFormulas: { fireNumber: '{annualExpenses}/{withdrawalRate}', savingsRate: 'IF({annualIncome}>0,{annualContribution}/{annualIncome},0)' },
    })
  }

  const isLean = params.annualExpenses <= LEAN_THRESHOLD

  return (
    <CurrencyPeriodProvider period={params.currencyPeriod} onChange={value => setParam('currencyPeriod', value)}>
      <SEO {...calculatorSEO.lean} />
      <div className="space-y-8">
        <header>
          <h1 className="text-2xl font-bold text-content sm:text-3xl">Lean FIRE Calculator</h1>
          <p className="mt-1 text-content-muted">Plan for financial independence with a deliberately low-cost retirement.</p>
        </header>

        <section aria-labelledby="lean-plan-heading">
          <Card>
            <CardHeader>
              <h2 id="lean-plan-heading" className="text-lg font-semibold text-content">Start with your plan</h2>
              <p className="mt-1 text-sm text-content-muted">Lean FIRE is typically based on spending of {formatCurrency(LEAN_THRESHOLD)} a year or less in today&apos;s dollars.</p>
              <PeriodToggle className="mt-3" />
            </CardHeader>
            <CardContent className="field-grid grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
              <AgeInput label="Current age" value={params.currentAge} onChange={value => setParam('currentAge', value)} onSliderChange={value => setParamDebounced('currentAge', value)} tooltip="Your current age." min={18} max={80} showSlider />
              <AgeInput label="Target retirement age" value={params.retirementAge} onChange={value => setParam('retirementAge', value)} onSliderChange={value => setParamDebounced('retirementAge', value)} tooltip="The age you want this plan to support." min={params.currentAge} max={90} showSlider />
              <CurrencyInput label="Current invested assets" value={params.currentSavings} onChange={value => setParam('currentSavings', value)} tooltip="Investments available for retirement." />
              <CurrencyInput label="Contributions" value={params.annualContribution} onChange={value => setParam('annualContribution', value)} tooltip="How much you expect to invest." periodic />
              <CurrencyInput label="After-tax take-home income" value={params.annualIncome} onChange={value => setParam('annualIncome', value)} tooltip="Income after taxes, used to calculate your savings rate." periodic />
              <CurrencyInput label="Retirement spending (today’s dollars)" value={params.annualExpenses} onChange={value => setParam('annualExpenses', value)} tooltip="Expected after-tax spending in retirement. Lean FIRE plans typically stay at or below $40,000; higher amounts are still calculated as entered." periodic />
            </CardContent>
          </Card>
        </section>

        <section aria-labelledby="lean-outlook-heading" className="space-y-4">
          <div>
            <h2 id="lean-outlook-heading" className="text-xl font-semibold text-content">Your outlook</h2>
            {!isLean && <p className="mt-1 text-sm text-warning">Your spending of {formatCurrency(params.annualExpenses)} is above the typical Lean FIRE range of {formatCurrency(LEAN_THRESHOLD)} a year. These results use the spending you entered.</p>}
          </div>
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
            <ResultCard label="Lean FIRE number" value={results.fireNumber} format="currency" highlight subtext="Target portfolio in today’s dollars" />
            <ResultCard label="Years to Lean FIRE" value={results.yearsToFIRE} format="years" subtext={`Estimated age ${Math.round(results.fireAge)}`} />
            <ResultCard label="Monthly spending" value={params.annualExpenses / 12} format="currency" subtext="Today’s dollars" />
            <ResultCard label={`Target age ${params.retirementAge}`} value={results.retirementGoal.isOnTrack ? 'On track' : 'Off track'} subtext={results.retirementGoal.message} />
          </div>
          <ProgressToFIRE currentSavings={params.currentSavings} fireNumber={results.fireNumber} yearsToFIRE={results.yearsToFIRE} label="Progress to Lean FIRE" targetLabel="Lean FIRE number" />
        </section>

        <AdvancedDetails description="These long-term assumptions change the projection without changing the plan inputs.">
          <PercentageInput label="Expected annual return" value={params.expectedReturn} onChange={value => setParam('expectedReturn', value)} onSliderChange={value => setParamDebounced('expectedReturn', value)} tooltip="Average annual investment return before inflation." min={0} max={0.15} />
          <PercentageInput label="Inflation rate" value={params.inflationRate} onChange={value => setParam('inflationRate', value)} onSliderChange={value => setParamDebounced('inflationRate', value)} tooltip="Expected annual price growth. Spending remains expressed in today’s dollars." min={0} max={0.1} />
          <PercentageInput label="Withdrawal rate" value={params.withdrawalRate} onChange={value => setParam('withdrawalRate', value)} onSliderChange={value => setParamDebounced('withdrawalRate', value)} tooltip="The share of the portfolio you plan to spend each year." min={0.02} max={0.06} />
          <ToggleInput
            label="Increase contributions with inflation"
            tooltip="On: the amount you invest rises with inflation, so its purchasing power stays constant. Off: you invest the same dollar amount every year and its purchasing power erodes."
            checked={params.contributionGrowth === 'inflation'}
            onChange={checked => setParam('contributionGrowth', checked ? 'inflation' : 'flat')}
            className="sm:col-span-2"
          />
        </AdvancedDetails>

        <section aria-labelledby="lean-projection-heading">
          <Card>
            <CardHeader><h2 id="lean-projection-heading" className="text-lg font-semibold text-content">Portfolio projection</h2></CardHeader>
            <CardContent><ProjectionChart data={results.projections} fireNumber={results.fireNumber} inflationRate={params.inflationRate} colorScheme="green" height={350} /></CardContent>
          </Card>
        </section>

        <p className="max-w-3xl text-sm text-content-muted">Lean FIRE asks your portfolio to support a lower spending level. Review housing, healthcare, and other flexible costs before relying on this estimate.</p>
        <CalculatorFooter onExport={handleExport} onReset={resetParams} onSave={saveParams} onLoad={loadParams} onCopy={copyUrl} hasCustomParams={hasCustomParams} hasUnsavedChanges={hasUnsavedChanges} hasSavedParams={hasSavedParams} savedAt={savedAt} />
      </div>
    </CurrencyPeriodProvider>
  )
}
