import { useMemo } from 'react'
import { useCalculatorParams } from '../hooks/useCalculatorParams'
import { calculateBaristaFIRE, formatCurrency } from '../utils/calculations'
import { exportToExcel, formatInputsForExport, formatResultsForExport } from '../utils/excelExport'
import { CurrencyInput, PercentageInput, AgeInput } from '../components/inputs'
import { Card, CardHeader, CardContent, ResultCard, UrlActions, ProgressToFIRE, Disclaimer, ExportButton } from '../components/ui'
import { ProjectionChart } from '../components/charts'
import SEO from '../components/SEO'
import { calculatorSEO } from '../config/seo'

export default function BaristaFIRE() {
  const { params, setParam, resetParams, copyUrl, hasCustomParams } = useCalculatorParams()

  const results = useMemo(() => {
    return calculateBaristaFIRE(
      params.currentAge,
      params.currentSavings,
      params.annualContribution,
      params.expectedReturn,
      params.inflationRate,
      params.annualExpenses,
      params.withdrawalRate,
      params.partTimeIncome
    )
  }, [params])

  const portfolioReduction = results.fullFireNumber - results.baristaNumber
  const reductionPercent = (portfolioReduction / results.fullFireNumber) * 100

  const handleExport = () => {
    const inputs = {
      currentAge: params.currentAge,
      currentSavings: params.currentSavings,
      annualContribution: params.annualContribution,
      expectedReturn: params.expectedReturn,
      inflationRate: params.inflationRate,
      annualExpenses: params.annualExpenses,
      withdrawalRate: params.withdrawalRate,
      partTimeIncome: params.partTimeIncome,
    }

    exportToExcel({
      calculatorName: 'Barista FIRE',
      inputs: formatInputsForExport(inputs),
      results: formatResultsForExport(results),
      projections: results.projections,
    })
  }

  return (
    <>
      <SEO {...calculatorSEO.barista} />
      <div className="space-y-6">
        {/* Header */}
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div>
            <h1 className="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-gray-100 flex items-center gap-3">
              <span className="text-3xl" role="img" aria-label="Coffee emoji">☕</span>
              Barista FIRE Calculator
            </h1>
            <p className="text-gray-600 dark:text-gray-400 mt-1">
              Blend part-time work with portfolio income to retire from corporate life earlier.
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
        fireNumber={results.baristaNumber}
        yearsToFIRE={results.yearsToBaristaFIRE}
        label="Progress to Barista FIRE"
        targetLabel="Barista Number"
      />

      {/* Barista FIRE Explanation Banner */}
      <div className="bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-xl p-4">
        <div className="flex gap-3">
          <span className="text-2xl">☕</span>
          <div>
            <h3 className="font-semibold text-amber-900 dark:text-amber-100">What is Barista FIRE?</h3>
            <p className="text-sm text-amber-700 dark:text-amber-300 mt-1">
              Barista FIRE (named after the stereotype of working part-time at Starbucks for benefits) means having 
              enough invested to cover most expenses, while working a part-time or low-stress job to cover the gap 
              and potentially access health insurance benefits.
            </p>
          </div>
        </div>
      </div>

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
            />
            <CurrencyInput
              label="Current Savings"
              value={params.currentSavings}
              onChange={(v) => setParam('currentSavings', v)}
            />
            <CurrencyInput
              label="Annual Contribution"
              value={params.annualContribution}
              onChange={(v) => setParam('annualContribution', v)}
            />
            <CurrencyInput
              label="Annual Expenses"
              value={params.annualExpenses}
              onChange={(v) => setParam('annualExpenses', v)}
              tooltip="Total yearly spending needs"
            />
            
            <div className="border-t border-gray-200 dark:border-gray-700 pt-4">
              <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3 flex items-center gap-2">
                ☕ Part-Time Work
              </h3>
              <CurrencyInput
                label="Part-Time Annual Income"
                value={params.partTimeIncome}
                onChange={(v) => setParam('partTimeIncome', v)}
                tooltip="Expected yearly income from part-time work"
              />
              <p className="text-xs text-gray-500 dark:text-gray-400 mt-2">
                Tip: $15-25/hr × 20 hrs/week = $15,600-26,000/year
              </p>
            </div>

            <PercentageInput
              label="Expected Return"
              value={params.expectedReturn}
              onChange={(v) => setParam('expectedReturn', v)}
              min={0}
              max={0.15}
            />
            <PercentageInput
              label="Inflation Rate"
              value={params.inflationRate}
              onChange={(v) => setParam('inflationRate', v)}
              min={0}
              max={0.10}
            />
            <PercentageInput
              label="Safe Withdrawal Rate"
              value={params.withdrawalRate}
              onChange={(v) => setParam('withdrawalRate', v)}
              min={0.02}
              max={0.06}
            />
          </CardContent>
        </Card>

        {/* Results */}
        <div className="lg:col-span-2 space-y-6">
          {/* Comparison Banner */}
          <div className="bg-gradient-to-r from-amber-50 to-orange-50 dark:from-amber-900/20 dark:to-orange-900/20 border border-amber-200 dark:border-amber-800 rounded-xl p-4">
            <div className="flex items-center justify-between flex-wrap gap-4">
              <div>
                <p className="text-sm text-amber-700 dark:text-amber-300">Portfolio savings vs Full FIRE</p>
                <p className="text-2xl font-bold text-amber-900 dark:text-amber-100">
                  {formatCurrency(portfolioReduction)} less needed
                </p>
              </div>
              <div className="text-right">
                <p className="text-sm text-amber-700 dark:text-amber-300">Reduction</p>
                <p className="text-2xl font-bold text-amber-900 dark:text-amber-100">
                  {reductionPercent.toFixed(0)}%
                </p>
              </div>
            </div>
          </div>

          {/* Key Metrics */}
          <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
            <ResultCard
              label="Barista FIRE Number"
              value={results.baristaNumber}
              format="currency"
              highlight
              subtext="Target portfolio value"
            />
            <ResultCard
              label="Full FIRE Number"
              value={results.fullFireNumber}
              format="currency"
              subtext="Without part-time work"
            />
            <ResultCard
              label="Years to Barista FIRE"
              value={results.yearsToBaristaFIRE}
              format="years"
              icon="⏱️"
              subtext={`At age ${Math.round(params.currentAge + results.yearsToBaristaFIRE)}`}
            />
          </div>

          {/* Income Breakdown */}
          <Card>
            <CardHeader>
              <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Income Breakdown in Barista FIRE</h2>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                <div className="flex items-center gap-4">
                  <div className="flex-1">
                    <div className="flex justify-between text-sm mb-1">
                      <span className="text-gray-600 dark:text-gray-400">Portfolio Withdrawals</span>
                      <span className="font-medium text-gray-900 dark:text-gray-100">
                        {formatCurrency(params.annualExpenses - params.partTimeIncome)}/year
                      </span>
                    </div>
                    <div className="h-3 bg-amber-100 dark:bg-amber-900/30 rounded-full overflow-hidden">
                      <div 
                        className="h-full bg-amber-500"
                        style={{ width: `${((params.annualExpenses - params.partTimeIncome) / params.annualExpenses) * 100}%` }}
                      />
                    </div>
                  </div>
                </div>
                <div className="flex items-center gap-4">
                  <div className="flex-1">
                    <div className="flex justify-between text-sm mb-1">
                      <span className="text-gray-600 dark:text-gray-400">Part-Time Income</span>
                      <span className="font-medium text-gray-900 dark:text-gray-100">
                        {formatCurrency(params.partTimeIncome)}/year
                      </span>
                    </div>
                    <div className="h-3 bg-green-100 dark:bg-green-900/30 rounded-full overflow-hidden">
                      <div 
                        className="h-full bg-green-500"
                        style={{ width: `${(params.partTimeIncome / params.annualExpenses) * 100}%` }}
                      />
                    </div>
                  </div>
                </div>
                <div className="pt-2 border-t border-gray-200 dark:border-gray-700">
                  <div className="flex justify-between">
                    <span className="font-medium text-gray-900 dark:text-gray-100">Total Annual Income</span>
                    <span className="font-bold text-gray-900 dark:text-gray-100">{formatCurrency(params.annualExpenses)}</span>
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Chart */}
          <Card>
            <CardHeader>
              <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Portfolio Projection</h2>
            </CardHeader>
            <CardContent>
              <ProjectionChart
                data={results.projections}
                fireNumber={results.baristaNumber}
                colorScheme="amber"
                height={350}
              />
            </CardContent>
          </Card>

          {/* Benefits of Barista FIRE */}
          <Card>
            <CardHeader>
              <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Benefits of Barista FIRE</h2>
            </CardHeader>
            <CardContent>
              <div className="grid sm:grid-cols-2 gap-4">
                <div className="flex gap-3">
                  <span className="text-xl">🏥</span>
                  <div>
                    <h4 className="font-medium text-gray-900 dark:text-gray-100">Health Insurance</h4>
                    <p className="text-sm text-gray-600 dark:text-gray-400">Many part-time jobs offer benefits, bridging to Medicare</p>
                  </div>
                </div>
                <div className="flex gap-3">
                  <span className="text-xl">🤝</span>
                  <div>
                    <h4 className="font-medium text-gray-900 dark:text-gray-100">Social Connection</h4>
                    <p className="text-sm text-gray-600 dark:text-gray-400">Stay engaged with a community and routine</p>
                  </div>
                </div>
                <div className="flex gap-3">
                  <span className="text-xl">⚡</span>
                  <div>
                    <h4 className="font-medium text-gray-900 dark:text-gray-100">Earlier Freedom</h4>
                    <p className="text-sm text-gray-600 dark:text-gray-400">Leave your corporate job years earlier</p>
                  </div>
                </div>
                <div className="flex gap-3">
                  <span className="text-xl">🎯</span>
                  <div>
                    <h4 className="font-medium text-gray-900 dark:text-gray-100">Lower Target</h4>
                    <p className="text-sm text-gray-600 dark:text-gray-400">Need {reductionPercent.toFixed(0)}% less in your portfolio</p>
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>

      <Disclaimer />
    </div>
    </>
  )
}
