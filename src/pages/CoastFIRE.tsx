import { useMemo } from 'react'
import { useCalculatorParams } from '../hooks/useCalculatorParams'
import { calculateCoastFIRE, formatCurrency } from '../utils/calculations'
import { CurrencyInput, PercentageInput, AgeInput } from '../components/inputs'
import { Card, CardHeader, CardContent, ResultCard, UrlActions, ProgressToFIRE, Disclaimer } from '../components/ui'
import { ProjectionChart } from '../components/charts'

export default function CoastFIRE() {
  const { params, setParam, resetParams, copyUrl, hasCustomParams } = useCalculatorParams()

  const results = useMemo(() => {
    return calculateCoastFIRE(
      params.currentAge,
      params.retirementAge,
      params.currentSavings,
      params.annualContribution,
      params.expectedReturn,
      params.inflationRate,
      params.annualExpenses,
      params.withdrawalRate
    )
  }, [params])

  const coastStatus = results.alreadyCoasting
    ? { text: "You're already Coast FIRE! 🎉", color: 'text-green-600 dark:text-green-400' }
    : { text: `${results.yearsToCoast.toFixed(1)} years to Coast FIRE`, color: 'text-fire-600 dark:text-fire-400' }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-gray-100 flex items-center gap-3">
            <span className="text-3xl">⛵</span>
            Coast FIRE Calculator
          </h1>
          <p className="text-gray-600 dark:text-gray-400 mt-1">
            Find out how much you need now so compound growth does the rest.
          </p>
        </div>
        <UrlActions onReset={resetParams} onCopy={copyUrl} hasCustomParams={hasCustomParams} />
      </div>

      {/* Progress to Coast FIRE */}
      <ProgressToFIRE 
        currentSavings={params.currentSavings} 
        fireNumber={results.coastNumber}
        yearsToFIRE={results.yearsToCoast}
        label="Progress to Coast FIRE"
        targetLabel="Coast Number"
      />

      {/* Coast FIRE Explanation Banner */}
      <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-xl p-4">
        <div className="flex gap-3">
          <span className="text-2xl">💡</span>
          <div>
            <h3 className="font-semibold text-blue-900 dark:text-blue-100">What is Coast FIRE?</h3>
            <p className="text-sm text-blue-700 dark:text-blue-300 mt-1">
              Coast FIRE is when you've saved enough that compound growth alone will get you to your full FIRE 
              number by traditional retirement age — without any more contributions. You can then "coast" 
              with a lower-paying job that just covers current expenses.
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
            <AgeInput
              label="Target Retirement Age"
              value={params.retirementAge}
              onChange={(v) => setParam('retirementAge', v)}
              tooltip="Traditional retirement age (e.g., 60-67)"
              min={params.currentAge + 1}
              max={80}
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
              tooltip="How much you're currently saving per year"
            />
            <CurrencyInput
              label="Annual Expenses in Retirement"
              value={params.annualExpenses}
              onChange={(v) => setParam('annualExpenses', v)}
            />
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
          {/* Status Banner */}
          <div className={`text-center p-4 rounded-xl ${
            results.alreadyCoasting 
              ? 'bg-green-50 dark:bg-green-900/20 border-2 border-green-300 dark:border-green-700' 
              : 'bg-gray-50 dark:bg-gray-800/50 border-2 border-gray-200 dark:border-gray-700'
          }`}>
            <p className={`text-xl font-bold ${coastStatus.color}`}>
              {coastStatus.text}
            </p>
          </div>

          {/* Key Metrics */}
          <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
            <ResultCard
              label="Coast FIRE Number"
              value={results.coastNumber}
              format="currency"
              highlight
              icon="⛵"
              subtext="Amount needed today"
            />
            <ResultCard
              label="Full FIRE Number"
              value={results.fireNumber}
              format="currency"
              icon="🎯"
              subtext={`At age ${params.retirementAge}`}
            />
            <ResultCard
              label="Current Progress"
              value={Math.min(100, (params.currentSavings / results.coastNumber) * 100)}
              format="percent"
              icon="📊"
              subtext={`${formatCurrency(params.currentSavings)} saved`}
            />
          </div>

          {/* Chart */}
          <Card>
            <CardHeader>
              <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Coast vs Continue Contributing</h2>
            </CardHeader>
            <CardContent>
              <div className="mb-4 flex flex-wrap gap-4 text-sm">
                <div className="flex items-center gap-2">
                  <div className="w-3 h-3 rounded-full bg-blue-500"></div>
                  <span className="text-gray-600 dark:text-gray-400">Coast (no more contributions)</span>
                </div>
                <div className="flex items-center gap-2">
                  <div className="w-3 h-3 rounded-full bg-indigo-500"></div>
                  <span className="text-gray-600 dark:text-gray-400">Continue contributing</span>
                </div>
              </div>
              <ProjectionChart
                data={results.projectionsWithContributions}
                fireNumber={results.fireNumber}
                colorScheme="blue"
                height={350}
              />
            </CardContent>
          </Card>

          {/* Explanation */}
          <Card>
            <CardHeader>
              <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">How Coast FIRE Works</h2>
            </CardHeader>
            <CardContent className="prose dark:prose-invert max-w-none text-sm">
              <p>
                Your <strong>Coast FIRE Number</strong> ({formatCurrency(results.coastNumber)}) is the amount 
                you need invested today such that compound growth alone will reach your full FIRE number 
                ({formatCurrency(results.fireNumber)}) by age {params.retirementAge}.
              </p>
              {results.alreadyCoasting ? (
                <p className="text-green-600 dark:text-green-400">
                  <strong>Congratulations!</strong> You've already reached Coast FIRE. You could stop 
                  contributing to retirement and your portfolio would still grow to {formatCurrency(results.fireNumber)} by 
                  age {params.retirementAge}. Consider switching to work you love, even if it pays less!
                </p>
              ) : (
                <p>
                  You need {formatCurrency(results.coastNumber - params.currentSavings)} more to reach Coast FIRE. 
                  At your current savings rate, this will take approximately {results.yearsToCoast.toFixed(1)} years.
                </p>
              )}
            </CardContent>
          </Card>
        </div>
      </div>

      <Disclaimer />
    </div>
  )
}
