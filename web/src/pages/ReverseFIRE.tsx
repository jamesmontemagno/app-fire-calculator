import { useMemo } from 'react'
import { useCalculatorParams } from '../hooks/useCalculatorParams'
import { formatCurrency, generateProjections } from '../utils/calculations'
import { exportToExcel, prepareInputsForExport, prepareResultsForExport } from '../utils/excelExport'
import { CurrencyInput, PercentageInput, AgeInput } from '../components/inputs'
import { Card, CardHeader, CardContent, ResultCard, UrlActions, Disclaimer, ExportButton } from '../components/ui'
import { ProjectionChart } from '../components/charts'
import SEO from '../components/SEO'
import { calculatorSEO } from '../config/seo'

// Calculate required monthly savings to reach FIRE by target age
function calculateReverseFIRE(
  currentAge: number,
  targetRetirementAge: number,
  currentSavings: number,
  annualExpenses: number,
  expectedReturn: number,
  inflationRate: number,
  withdrawalRate: number
) {
  const yearsToFIRE = Math.max(1, targetRetirementAge - currentAge)
  const fireNumber = annualExpenses / withdrawalRate
  
  // Real return
  const realReturn = (1 + expectedReturn) / (1 + inflationRate) - 1
  
  // Calculate required annual savings using future value formula
  // FV = PV(1+r)^n + PMT * (((1+r)^n - 1) / r)
  // Solve for PMT: PMT = (FV - PV(1+r)^n) * r / ((1+r)^n - 1)
  
  const compoundFactor = Math.pow(1 + realReturn, yearsToFIRE)
  const futureValueOfCurrent = currentSavings * compoundFactor
  
  let requiredAnnualSavings = 0
  if (futureValueOfCurrent >= fireNumber) {
    // Already have enough, no savings needed
    requiredAnnualSavings = 0
  } else if (realReturn === 0) {
    requiredAnnualSavings = (fireNumber - currentSavings) / yearsToFIRE
  } else {
    const deficit = fireNumber - futureValueOfCurrent
    requiredAnnualSavings = deficit * realReturn / (compoundFactor - 1)
  }
  
  const requiredMonthlySavings = requiredAnnualSavings / 12
  
  // Generate projections with required savings
  const projections = generateProjections(
    currentAge,
    currentSavings,
    requiredAnnualSavings,
    expectedReturn,
    inflationRate,
    yearsToFIRE + 10
  )
  
  // Check if already achievable
  const alreadyAchievable = futureValueOfCurrent >= fireNumber
  
  return {
    fireNumber,
    yearsToFIRE,
    requiredAnnualSavings: Math.max(0, requiredAnnualSavings),
    requiredMonthlySavings: Math.max(0, requiredMonthlySavings),
    projections,
    alreadyAchievable,
    currentWillGrowTo: Math.round(futureValueOfCurrent),
  }
}

export default function ReverseFIRE() {
  const { params, setParam, resetParams, saveParams, loadParams, copyUrl, hasCustomParams, hasUnsavedChanges, hasSavedParams, savedAt } = useCalculatorParams()

  const results = useMemo(() => {
    return calculateReverseFIRE(
      params.currentAge,
      params.retirementAge,
      params.currentSavings,
      params.annualExpenses,
      params.expectedReturn,
      params.inflationRate,
      params.withdrawalRate
    )
  }, [params])

  const handleExport = () => {
    const { values: inputValues, formats: inputFormats } = prepareInputsForExport({
      currentAge: params.currentAge,
      targetRetirementAge: params.retirementAge,
      currentSavings: params.currentSavings,
      annualExpenses: params.annualExpenses,
      expectedReturn: params.expectedReturn,
      inflationRate: params.inflationRate,
      withdrawalRate: params.withdrawalRate,
    })

    const { values: resultValues, formats: resultFormats } = prepareResultsForExport(results)

    // Define formulas for calculated results
    const resultFormulas: Record<string, string> = {
      // FIRE Number = Annual Expenses / Withdrawal Rate
      fireNumber: '{annualExpenses}/{withdrawalRate}',
    }

    exportToExcel({
      calculatorName: 'Reverse FIRE',
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
      <SEO {...calculatorSEO.reverse} />
      <div className="space-y-6">
        {/* Header */}
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div>
            <h1 className="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-gray-100 flex items-center gap-3">
              <span className="text-3xl" role="img" aria-label="Recycle emoji">🔄</span>
              Reverse FIRE Calculator
            </h1>
            <p className="text-gray-600 dark:text-gray-400 mt-1">
              Find out how much you need to save monthly to FIRE by your target age.
            </p>
          </div>
          <div className="flex flex-wrap gap-2">
            <ExportButton onExport={handleExport} />
            <UrlActions onReset={resetParams} onSave={saveParams} onLoad={loadParams} onCopy={copyUrl} hasCustomParams={hasCustomParams} hasUnsavedChanges={hasUnsavedChanges} hasSavedParams={hasSavedParams} savedAt={savedAt} />
          </div>
      </div>

      <div className="grid lg:grid-cols-3 gap-6">
        {/* Inputs */}
        <Card className="lg:col-span-1">
          <CardHeader>
            <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Your Goals</h2>
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
              tooltip="When do you want to achieve FIRE?"
              min={params.currentAge + 1}
            />
            <CurrencyInput
              label="Current Invested Assets"
              value={params.currentSavings}
              onChange={(v) => setParam('currentSavings', v)}
              tooltip="Total invested assets (401k, IRA, brokerage)"
            />
            <CurrencyInput
              label="Annual Retirement Expenses"
              value={params.annualExpenses}
              onChange={(v) => setParam('annualExpenses', v)}
              tooltip="Your expected yearly spending in retirement"
            />
            <PercentageInput
              label="Expected Annual Return"
              value={params.expectedReturn}
              onChange={(v) => setParam('expectedReturn', v)}
              tooltip="Average annual investment return before inflation"
              min={0}
              max={0.15}
            />
            <PercentageInput
              label="Inflation Rate"
              value={params.inflationRate}
              onChange={(v) => setParam('inflationRate', v)}
              tooltip="Expected annual increase in prices"
              min={0}
              max={0.10}
            />
            <PercentageInput
              label="Safe Withdrawal Rate"
              value={params.withdrawalRate}
              onChange={(v) => setParam('withdrawalRate', v)}
              tooltip="Percentage of your portfolio withdrawn each year in retirement"
              min={0.02}
              max={0.06}
            />
          </CardContent>
        </Card>

        {/* Results */}
        <div className="lg:col-span-2 space-y-6">
          {/* Main Result */}
          {results.alreadyAchievable ? (
            <div className="bg-gradient-to-r from-green-500 to-emerald-500 rounded-xl p-6 text-white">
              <div className="flex items-center gap-4">
                <div>
                  <p className="text-green-100 text-sm">Great News!</p>
                  <p className="text-2xl font-bold">You're Already on Track!</p>
                  <p className="text-green-100 mt-1">
                    Your current savings of {formatCurrency(params.currentSavings)} will grow to{' '}
                    {formatCurrency(results.currentWillGrowTo)} by age {params.retirementAge}, 
                    exceeding your FIRE number of {formatCurrency(results.fireNumber)}.
                  </p>
                </div>
              </div>
            </div>
          ) : (
            <div className="bg-gradient-to-r from-teal-500 to-cyan-500 rounded-xl p-6 text-white">
              <div className="flex items-center justify-between flex-wrap gap-4">
                <div>
                  <p className="text-teal-100 text-sm">To FIRE by age {params.retirementAge}, you need to save</p>
                  <p className="text-5xl font-bold">{formatCurrency(results.requiredMonthlySavings)}</p>
                  <p className="text-teal-200 text-lg">per month</p>
                </div>
                <div className="text-right">
                  <p className="text-teal-100 text-sm">Or annually</p>
                  <p className="text-3xl font-bold">{formatCurrency(results.requiredAnnualSavings)}</p>
                  <p className="text-teal-200 text-sm">per year</p>
                </div>
              </div>
            </div>
          )}

          {/* Key Metrics */}
          <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
            <ResultCard
              label="FIRE Number"
              value={results.fireNumber}
              format="currency"
              highlight
              subtext="Target portfolio"
            />
            <ResultCard
              label="Years to FIRE"
              value={results.yearsToFIRE}
              format="years"
              subtext={`At age ${params.retirementAge}`}
            />
            <ResultCard
              label="Current Savings Will Grow To"
              value={results.currentWillGrowTo}
              format="currency"
              subtext={`By age ${params.retirementAge}`}
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
                colorScheme="blue"
                height={350}
              />
            </CardContent>
          </Card>
        </div>
      </div>

      <Disclaimer />
    </div>
    </>
  )
}
