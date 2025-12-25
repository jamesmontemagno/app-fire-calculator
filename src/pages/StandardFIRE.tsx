import { useMemo } from 'react'
import { useCalculatorParams } from '../hooks/useCalculatorParams'
import { calculateStandardFIRE, formatCurrency } from '../utils/calculations'
import { exportToExcel, prepareInputsForExport, prepareResultsForExport } from '../utils/excelExport'
import { CurrencyInput, PercentageInput, AgeInput } from '../components/inputs'
import { Card, CardHeader, CardContent, ResultCard, UrlActions, ProgressToFIRE, QuickPresets, Disclaimer, ExportButton } from '../components/ui'
import { ProjectionChart } from '../components/charts'
import SEO from '../components/SEO'
import { calculatorSEO } from '../config/seo'

export default function StandardFIRE() {
  const { params, setParam, setParams, resetParams, copyUrl, hasCustomParams } = useCalculatorParams()

  const results = useMemo(() => {
    return calculateStandardFIRE({
      currentAge: params.currentAge,
      retirementAge: params.retirementAge,
      currentSavings: params.currentSavings,
      annualContribution: params.annualContribution,
      expectedReturn: params.expectedReturn,
      inflationRate: params.inflationRate,
      withdrawalRate: params.withdrawalRate,
      annualExpenses: params.annualExpenses,
    })
  }, [params])

  const handleExport = () => {
    const { values: inputValues, formats: inputFormats } = prepareInputsForExport({
      currentAge: params.currentAge,
      retirementAge: params.retirementAge,
      currentSavings: params.currentSavings,
      annualContribution: params.annualContribution,
      expectedReturn: params.expectedReturn,
      inflationRate: params.inflationRate,
      withdrawalRate: params.withdrawalRate,
      annualExpenses: params.annualExpenses,
    })

    const { values: resultValues, formats: resultFormats } = prepareResultsForExport(results)

    // Define formulas for calculated results
    const resultFormulas: Record<string, string> = {
      // FIRE Number = Annual Expenses / Withdrawal Rate
      fireNumber: '{annualExpenses}/{withdrawalRate}',
      // Savings Rate = Annual Contribution / (Annual Contribution + Annual Expenses)
      savingsRate: '{annualContribution}/({annualContribution}+{annualExpenses})',
      // Monthly Contribution = Annual Contribution / 12
      monthlyContribution: '{annualContribution}/12',
      // Coast FIRE Number = FIRE Number / ((1 + Real Return) ^ Years to Retirement)
      // Real Return = (1 + Expected Return) / (1 + Inflation) - 1
      // This is complex, so we'll use a calculated value for now
      // Years to FIRE is also complex (logarithmic formula), so we use calculated value
    }

    exportToExcel({
      calculatorName: 'Standard FIRE',
      inputs: inputValues,
      results: resultValues,
      projections: results.projections,
      inputFormats,
      resultFormats,
      resultFormulas,
    })
  }

  return (
    <>
      <SEO {...calculatorSEO.standard} />
      <div className="space-y-6">
        {/* Header */}
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div>
            <h1 className="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-gray-100 flex items-center gap-3">
              <span className="text-3xl" role="img" aria-label="Target emoji">🎯</span>
              Standard FIRE Calculator
            </h1>
            <p className="text-gray-600 dark:text-gray-400 mt-1">
              Calculate your path to financial independence using the 25x expenses rule.
            </p>
          </div>
          <div className="flex flex-wrap gap-2">
            <ExportButton onExport={handleExport} />
            <UrlActions onReset={resetParams} onCopy={copyUrl} hasCustomParams={hasCustomParams} />
        </div>
      </div>

      {/* Progress Bar */}
      <ProgressToFIRE 
        currentSavings={params.currentSavings} 
        fireNumber={results.fireNumber}
        yearsToFIRE={results.yearsToFIRE}
      />

      {/* Quick Presets */}
      <QuickPresets onApply={(values) => setParams(values as any)} />

      <div className="grid lg:grid-cols-3 gap-6">
        {/* Inputs */}
        <Card className="lg:col-span-1">
          <CardHeader>
            <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Your Information</h2>
          </CardHeader>
          <CardContent className="space-y-4">
            <AgeInput
              label="Current Age"
              value={params.currentAge}
              onChange={(v) => setParam('currentAge', v)}
              tooltip="Your current age"
            />
            <AgeInput
              label="Target Retirement Age"
              value={params.retirementAge}
              onChange={(v) => setParam('retirementAge', v)}
              tooltip="The age you want to retire"
            />
            <CurrencyInput
              label="Current Savings"
              value={params.currentSavings}
              onChange={(v) => setParam('currentSavings', v)}
              tooltip="Total invested assets (401k, IRA, brokerage)"
            />
            <CurrencyInput
              label="Annual Contribution"
              value={params.annualContribution}
              onChange={(v) => setParam('annualContribution', v)}
              tooltip="How much you save and invest per year"
              allowMonthlyToggle
            />
            <CurrencyInput
              label="Annual Expenses"
              value={params.annualExpenses}
              onChange={(v) => setParam('annualExpenses', v)}
              tooltip="Your expected yearly spending in retirement"
              allowMonthlyToggle
            />
            <PercentageInput
              label="Expected Return"
              value={params.expectedReturn}
              onChange={(v) => setParam('expectedReturn', v)}
              tooltip="Average annual investment return (7% is typical for stocks)"
              min={0}
              max={0.20}
            />
            <PercentageInput
              label="Inflation Rate"
              value={params.inflationRate}
              onChange={(v) => setParam('inflationRate', v)}
              tooltip="Expected annual inflation (3% is historical average)"
              min={0}
              max={0.10}
            />
            <PercentageInput
              label="Safe Withdrawal Rate"
              value={params.withdrawalRate}
              onChange={(v) => setParam('withdrawalRate', v)}
              tooltip="The 4% rule - withdraw 4% of portfolio yearly"
              min={0.02}
              max={0.06}
            />
          </CardContent>
        </Card>

        {/* Results */}
        <div className="lg:col-span-2 space-y-6">
          {/* Key Metrics */}
          <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
            <ResultCard
              label="FIRE Number"
              value={results.fireNumber}
              format="currency"
              highlight
              subtext="Target portfolio value"
            />
            <ResultCard
              label="Years to FIRE"
              value={results.yearsToFIRE}
              format="years"
              subtext={`At age ${Math.round(results.fireAge)}`}
            />
            <ResultCard
              label="Savings Rate"
              value={results.savingsRate}
              format="percent"
              subtext={`${formatCurrency(results.monthlyContribution)}/month`}
            />
          </div>

          {/* Chart */}
          <Card>
            <CardHeader>
              <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Portfolio Projection</h2>
            </CardHeader>
            <CardContent>
              <ProjectionChart
                data={results.projections}
                fireNumber={results.fireNumber}
                colorScheme="orange"
                height={350}
              />
            </CardContent>
          </Card>

          {/* Additional Info */}
          <Card>
            <CardHeader>
              <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Understanding Your Results</h2>
            </CardHeader>
            <CardContent className="prose dark:prose-invert max-w-none text-sm">
              <p>
                Your <strong>FIRE Number</strong> ({formatCurrency(results.fireNumber)}) is calculated as your 
                annual expenses ({formatCurrency(params.annualExpenses)}) divided by your withdrawal 
                rate ({(params.withdrawalRate * 100).toFixed(1)}%).
              </p>
              <p>
                At your current savings rate, you'll reach financial independence in approximately{' '}
                <strong>{results.yearsToFIRE.toFixed(1)} years</strong> (at age {Math.round(results.fireAge)}).
              </p>
              <p className="text-gray-500 dark:text-gray-400">
                The chart shows your portfolio growth over time. The dashed line represents inflation-adjusted 
                values (purchasing power). The red line is your FIRE target.
              </p>
            </CardContent>
          </Card>
        </div>
      </div>

      <Disclaimer />
    </div>
    </>
  )
}
