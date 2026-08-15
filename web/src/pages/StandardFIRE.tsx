import { useMemo } from 'react'
import { useCalculatorParams } from '../hooks/useCalculatorParams'
import { calculateStandardFIRE, formatCurrency } from '../utils/calculations'
import { exportToExcel, prepareInputsForExport, prepareResultsForExport } from '../utils/excelExport'
import { AgeInput, CurrencyInput, CurrencyPeriodProvider, PercentageInput, PeriodToggle, ToggleInput } from '../components/inputs'
import {
  AdvancedDetails,
  CalculatorFooter,
  Card,
  CardContent,
  CardHeader,
  ProgressToFIRE,
  ResultCard,
} from '../components/ui'
import { ProjectionChart } from '../components/charts'
import SEO from '../components/SEO'
import { calculatorSEO } from '../config/seo'

export default function StandardFIRE() {
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
  } = useCalculatorParams()

  const results = useMemo(() => calculateStandardFIRE({
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
      currentAge: params.currentAge,
      retirementAge: params.retirementAge,
      currentSavings: params.currentSavings,
      annualContribution: params.annualContribution,
      annualIncome: params.annualIncome,
      expectedReturn: params.expectedReturn,
      inflationRate: params.inflationRate,
      withdrawalRate: params.withdrawalRate,
      annualExpenses: params.annualExpenses,
    })
    const { values: resultValues, formats: resultFormats } = prepareResultsForExport(results)
    exportToExcel({
      calculatorName: 'Standard FIRE',
      inputs,
      results: resultValues,
      projections: results.projections,
      inputFormats,
      resultFormats,
      resultFormulas: {
        fireNumber: '{annualExpenses}/{withdrawalRate}',
        savingsRate: '{annualContribution}/{annualIncome}',
        monthlyContribution: '{annualContribution}/12',
      },
    })
  }

  return (
    <CurrencyPeriodProvider period={params.currencyPeriod} onChange={value => setParam('currencyPeriod', value)}>
      <SEO {...calculatorSEO.standard} />
      <div className="space-y-8">
        <header>
          <h1 className="text-2xl font-bold text-gray-900 sm:text-3xl dark:text-gray-100">Standard FIRE Calculator</h1>
          <p className="mt-1 text-gray-600 dark:text-gray-400">Estimate a practical path to financial independence using your spending and savings plan.</p>
        </header>

        <section aria-labelledby="standard-plan-heading">
          <Card>
            <CardHeader>
              <h2 id="standard-plan-heading" className="text-lg font-semibold text-gray-900 dark:text-gray-100">Start with your plan</h2>
              <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">Use today&apos;s spending and after-tax income to make the estimate meaningful.</p>
              <PeriodToggle className="mt-3" />
            </CardHeader>
            <CardContent className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
              <AgeInput label="Current age" value={params.currentAge} onChange={value => setParam('currentAge', value)} onSliderChange={value => setParamDebounced('currentAge', value)} tooltip="Your current age." min={18} max={80} showSlider />
              <AgeInput label="Target retirement age" value={params.retirementAge} onChange={value => setParam('retirementAge', value)} onSliderChange={value => setParamDebounced('retirementAge', value)} tooltip="The age you want this plan to support." min={params.currentAge} max={90} showSlider />
              <CurrencyInput label="Current invested assets" value={params.currentSavings} onChange={value => setParam('currentSavings', value)} tooltip="Investments available for retirement, including workplace plans, IRAs, and brokerage accounts." />
              <CurrencyInput label="Contributions" value={params.annualContribution} onChange={value => setParam('annualContribution', value)} tooltip="How much you expect to invest." periodic />
              <CurrencyInput label="After-tax take-home income" value={params.annualIncome} onChange={value => setParam('annualIncome', value)} tooltip="Income after taxes. It is used to calculate your savings rate." periodic />
              <CurrencyInput label="Retirement spending (today's dollars)" value={params.annualExpenses} onChange={value => setParam('annualExpenses', value)} tooltip="Expected after-tax spending in retirement, expressed in today’s purchasing power." periodic />
            </CardContent>
          </Card>
        </section>

        <section aria-labelledby="standard-outlook-heading" className="space-y-4">
          <div>
            <h2 id="standard-outlook-heading" className="text-xl font-semibold text-gray-900 dark:text-gray-100">Your outlook</h2>
            <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">Updates immediately as you change the plan above.</p>
          </div>
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
            <ResultCard label="FIRE number" value={results.fireNumber} format="currency" highlight subtext="Target portfolio in today’s dollars" />
            <ResultCard label="Years to FIRE" value={results.yearsToFIRE} format="years" subtext={`Estimated age ${Math.round(results.fireAge)}`} />
            <ResultCard label="Savings rate" value={results.savingsRate} format="percent" subtext={`${formatCurrency(results.monthlyContribution)} per month`} />
            <ResultCard
              label={`Target age ${params.retirementAge}`}
              value={results.retirementGoal.isOnTrack ? 'On track' : 'Off track'}
              subtext={results.retirementGoal.message}
            />
          </div>
          <ProgressToFIRE currentSavings={params.currentSavings} fireNumber={results.fireNumber} yearsToFIRE={results.yearsToFIRE} />
        </section>

        <AdvancedDetails description="These slower-moving assumptions affect the projection and target calculation.">
          <PercentageInput label="Expected annual return" value={params.expectedReturn} onChange={value => setParam('expectedReturn', value)} onSliderChange={value => setParamDebounced('expectedReturn', value)} tooltip="Average annual investment return before inflation. This is an estimate, not a guarantee." min={0} max={0.15} />
          <PercentageInput label="Inflation rate" value={params.inflationRate} onChange={value => setParam('inflationRate', value)} onSliderChange={value => setParamDebounced('inflationRate', value)} tooltip="Expected annual increase in prices. Spending is kept in today’s dollars for this calculation." min={0} max={0.1} />
          <PercentageInput label="Withdrawal rate" value={params.withdrawalRate} onChange={value => setParam('withdrawalRate', value)} onSliderChange={value => setParamDebounced('withdrawalRate', value)} tooltip="The portion of your portfolio you plan to spend each year in retirement." min={0.02} max={0.06} />
          <ToggleInput
            label="Increase contributions with inflation"
            tooltip="On: the amount you invest rises with inflation, so its purchasing power stays constant. Off: you invest the same dollar amount every year and its purchasing power erodes."
            checked={params.contributionGrowth === 'inflation'}
            onChange={checked => setParam('contributionGrowth', checked ? 'inflation' : 'flat')}
            className="sm:col-span-2"
          />
        </AdvancedDetails>

        <section aria-labelledby="standard-projection-heading">
          <Card>
            <CardHeader>
              <h2 id="standard-projection-heading" className="text-lg font-semibold text-gray-900 dark:text-gray-100">Portfolio projection</h2>
              <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">Compare the projected portfolio with your FIRE number; the dashed line shows purchasing power in today&apos;s dollars.</p>
            </CardHeader>
            <CardContent><ProjectionChart data={results.projections} fireNumber={results.fireNumber} inflationRate={params.inflationRate} colorScheme="orange" height={350} /></CardContent>
          </Card>
        </section>

        <p className="max-w-3xl text-sm text-gray-600 dark:text-gray-400">
          Your target portfolio equals annual retirement spending divided by your withdrawal rate. The target-age result compares the estimated FIRE age with your retirement-age goal; it does not change the FIRE calculation.
        </p>

        <CalculatorFooter onExport={handleExport} onReset={resetParams} onSave={saveParams} onLoad={loadParams} onCopy={copyUrl} hasCustomParams={hasCustomParams} hasUnsavedChanges={hasUnsavedChanges} hasSavedParams={hasSavedParams} savedAt={savedAt} />
      </div>
    </CurrencyPeriodProvider>
  )
}
