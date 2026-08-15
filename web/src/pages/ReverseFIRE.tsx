import { useMemo } from 'react'
import { useCalculatorParams } from '../hooks/useCalculatorParams'
import { formatCurrency, generateProjections } from '../utils/calculations'
import { exportToExcel, prepareInputsForExport, prepareResultsForExport } from '../utils/excelExport'
import { AgeInput, CurrencyInput, PercentageInput } from '../components/inputs'
import { AdvancedDetails, CalculatorFooter, Card, CardContent, CardHeader, ResultCard } from '../components/ui'
import { ProjectionChart } from '../components/charts'
import SEO from '../components/SEO'
import { calculatorSEO } from '../config/seo'

function calculateReverseFIRE(
  currentAge: number,
  targetRetirementAge: number,
  currentSavings: number,
  annualExpenses: number,
  expectedReturn: number,
  inflationRate: number,
  withdrawalRate: number,
) {
  const yearsToFIRE = Math.max(1, targetRetirementAge - currentAge)
  const fireNumber = annualExpenses / withdrawalRate
  const realReturn = (1 + expectedReturn) / (1 + inflationRate) - 1
  const compoundFactor = Math.pow(1 + realReturn, yearsToFIRE)
  const futureValueOfCurrent = currentSavings * compoundFactor
  const requiredAnnualSavings = futureValueOfCurrent >= fireNumber
    ? 0
    : realReturn === 0
      ? (fireNumber - currentSavings) / yearsToFIRE
      : (fireNumber - futureValueOfCurrent) * realReturn / (compoundFactor - 1)
  const safeAnnualSavings = Math.max(0, requiredAnnualSavings)

  return {
    fireNumber,
    yearsToFIRE,
    requiredAnnualSavings: safeAnnualSavings,
    requiredMonthlySavings: safeAnnualSavings / 12,
    projections: generateProjections(currentAge, currentSavings, safeAnnualSavings, expectedReturn, inflationRate, yearsToFIRE + 10),
    alreadyAchievable: futureValueOfCurrent >= fireNumber,
    currentWillGrowTo: Math.round(futureValueOfCurrent),
  }
}

export default function ReverseFIRE() {
  const {
    params, setParam, setParamDebounced, resetParams, saveParams, loadParams, copyUrl,
    hasCustomParams, hasUnsavedChanges, hasSavedParams, savedAt,
  } = useCalculatorParams()
  const results = useMemo(() => calculateReverseFIRE(
    params.currentAge, params.retirementAge, params.currentSavings, params.annualExpenses,
    params.expectedReturn, params.inflationRate, params.withdrawalRate,
  ), [params])

  const handleExport = () => {
    const { values: inputs, formats: inputFormats } = prepareInputsForExport({
      currentAge: params.currentAge, targetRetirementAge: params.retirementAge, currentSavings: params.currentSavings,
      annualExpenses: params.annualExpenses, expectedReturn: params.expectedReturn, inflationRate: params.inflationRate,
      withdrawalRate: params.withdrawalRate,
    })
    const { values: resultValues, formats: resultFormats } = prepareResultsForExport(results)
    exportToExcel({
      calculatorName: 'Reverse FIRE', inputs, results: resultValues, projections: results.projections, inputFormats, resultFormats,
      resultFormulas: { fireNumber: '{annualExpenses}/{withdrawalRate}' },
    })
  }

  return (
    <>
      <SEO {...calculatorSEO.reverse} />
      <div className="space-y-8">
        <header>
          <h1 className="text-2xl font-bold text-gray-900 sm:text-3xl dark:text-gray-100">Reverse FIRE Calculator</h1>
          <p className="mt-1 text-gray-600 dark:text-gray-400">Work backward from a retirement age to estimate the savings required each month.</p>
        </header>

        <section aria-labelledby="reverse-plan-heading">
          <Card>
            <CardHeader>
              <h2 id="reverse-plan-heading" className="text-lg font-semibold text-gray-900 dark:text-gray-100">Start with your goal</h2>
              <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">Set a target age, then state the portfolio and spending you expect to need.</p>
            </CardHeader>
            <CardContent className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
              <AgeInput label="Current age" value={params.currentAge} onChange={value => setParam('currentAge', value)} onSliderChange={value => setParamDebounced('currentAge', value)} tooltip="Your current age." min={18} max={80} showSlider />
              <AgeInput label="Target retirement age" value={params.retirementAge} onChange={value => setParam('retirementAge', value)} onSliderChange={value => setParamDebounced('retirementAge', value)} tooltip="When you want to reach financial independence." min={params.currentAge} max={90} showSlider />
              <CurrencyInput label="Current invested assets" value={params.currentSavings} onChange={value => setParam('currentSavings', value)} tooltip="Investments already available for retirement." />
              <CurrencyInput label="Annual retirement spending (today's dollars)" value={params.annualExpenses} onChange={value => setParam('annualExpenses', value)} tooltip="Expected annual after-tax spending in retirement, expressed in today’s purchasing power." allowMonthlyToggle />
            </CardContent>
          </Card>
        </section>

        <section aria-labelledby="reverse-outlook-heading" className="space-y-4">
          <div>
            <h2 id="reverse-outlook-heading" className="text-xl font-semibold text-gray-900 dark:text-gray-100">{results.alreadyAchievable ? 'You are already on track' : 'Your savings requirement'}</h2>
            <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
              {results.alreadyAchievable
                ? `Your current investments are projected to grow to ${formatCurrency(results.currentWillGrowTo)} by age ${params.retirementAge}.`
                : `To reach the target by age ${params.retirementAge}, invest the amount below in addition to existing savings.`}
            </p>
          </div>
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
            <ResultCard label="Required monthly savings" value={results.requiredMonthlySavings} format="currency" highlight subtext={results.alreadyAchievable ? 'No additional savings required' : 'Per month'} />
            <ResultCard label="Required annual savings" value={results.requiredAnnualSavings} format="currency" subtext="Per year" />
            <ResultCard label="FIRE number" value={results.fireNumber} format="currency" subtext="Target portfolio in today’s dollars" />
            <ResultCard label="Current savings at target age" value={results.currentWillGrowTo} format="currency" subtext={`At age ${params.retirementAge}`} />
          </div>
        </section>

        <AdvancedDetails description="These slower-moving assumptions determine how current and future savings grow.">
          <PercentageInput label="Expected annual return" value={params.expectedReturn} onChange={value => setParam('expectedReturn', value)} onSliderChange={value => setParamDebounced('expectedReturn', value)} tooltip="Average annual investment return before inflation." min={0} max={0.15} />
          <PercentageInput label="Inflation rate" value={params.inflationRate} onChange={value => setParam('inflationRate', value)} onSliderChange={value => setParamDebounced('inflationRate', value)} tooltip="Expected annual price growth." min={0} max={0.1} />
          <PercentageInput label="Withdrawal rate" value={params.withdrawalRate} onChange={value => setParam('withdrawalRate', value)} onSliderChange={value => setParamDebounced('withdrawalRate', value)} tooltip="The portion of the retirement portfolio spent in the first year." min={0.02} max={0.06} />
        </AdvancedDetails>

        <section aria-labelledby="reverse-projection-heading">
          <Card>
            <CardHeader><h2 id="reverse-projection-heading" className="text-lg font-semibold text-gray-900 dark:text-gray-100">Portfolio projection</h2></CardHeader>
            <CardContent><ProjectionChart data={results.projections} fireNumber={results.fireNumber} colorScheme="blue" height={350} /></CardContent>
          </Card>
        </section>

        <p className="max-w-3xl text-sm text-gray-600 dark:text-gray-400">This calculation uses a steady, inflation-adjusted return and does not account for taxes, contribution limits, or a changing savings pattern.</p>
        <CalculatorFooter onExport={handleExport} onReset={resetParams} onSave={saveParams} onLoad={loadParams} onCopy={copyUrl} hasCustomParams={hasCustomParams} hasUnsavedChanges={hasUnsavedChanges} hasSavedParams={hasSavedParams} savedAt={savedAt} />
      </div>
    </>
  )
}
