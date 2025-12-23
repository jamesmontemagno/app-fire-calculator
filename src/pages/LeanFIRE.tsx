import { useMemo } from 'react'
import { useCalculatorParams } from '../hooks/useCalculatorParams'
import { calculateLeanFIRE, formatCurrency } from '../utils/calculations'
import { CurrencyInput, PercentageInput, AgeInput } from '../components/inputs'
import { Card, CardHeader, CardContent, ResultCard, UrlActions, ProgressToFIRE, Disclaimer } from '../components/ui'
import { ProjectionChart } from '../components/charts'

const LEAN_THRESHOLD = 40000

export default function LeanFIRE() {
  const { params, setParam, resetParams, copyUrl, hasCustomParams } = useCalculatorParams()

  // Use lean-appropriate defaults
  const leanExpenses = Math.min(params.annualExpenses, LEAN_THRESHOLD)

  const results = useMemo(() => {
    return calculateLeanFIRE({
      currentAge: params.currentAge,
      retirementAge: params.retirementAge,
      currentSavings: params.currentSavings,
      annualContribution: params.annualContribution,
      expectedReturn: params.expectedReturn,
      inflationRate: params.inflationRate,
      withdrawalRate: params.withdrawalRate,
      annualExpenses: leanExpenses,
    })
  }, [params, leanExpenses])

  const isLean = params.annualExpenses <= LEAN_THRESHOLD

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-gray-100 flex items-center gap-3">
            <span className="text-3xl">🌿</span>
            Lean FIRE Calculator
          </h1>
          <p className="text-gray-600 dark:text-gray-400 mt-1">
            Achieve financial independence faster with a minimalist lifestyle.
          </p>
        </div>
        <UrlActions onReset={resetParams} onCopy={copyUrl} hasCustomParams={hasCustomParams} />
      </div>

      {/* Progress Bar */}
      <ProgressToFIRE 
        currentSavings={params.currentSavings} 
        fireNumber={results.fireNumber}
        yearsToFIRE={results.yearsToFIRE}
      />

      {/* Lean FIRE Explanation Banner */}
      <div className="bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-xl p-4">
        <div className="flex gap-3">
          <span className="text-2xl">🌿</span>
          <div>
            <h3 className="font-semibold text-green-900 dark:text-green-100">What is Lean FIRE?</h3>
            <p className="text-sm text-green-700 dark:text-green-300 mt-1">
              Lean FIRE means achieving financial independence with minimal expenses (typically ≤$40,000/year 
              for a household). It requires living frugally but allows you to retire much earlier than 
              traditional or Fat FIRE approaches.
            </p>
          </div>
        </div>
      </div>

      {/* Lean Status Warning */}
      {!isLean && (
        <div className="bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-xl p-4">
          <div className="flex gap-3">
            <span className="text-2xl">⚠️</span>
            <div>
              <h3 className="font-semibold text-amber-900 dark:text-amber-100">Expenses Above Lean Threshold</h3>
              <p className="text-sm text-amber-700 dark:text-amber-300 mt-1">
                Your expenses ({formatCurrency(params.annualExpenses)}) exceed the typical Lean FIRE threshold 
                of {formatCurrency(LEAN_THRESHOLD)}. Consider the <strong>Standard FIRE</strong> or{' '}
                <strong>Fat FIRE</strong> calculators, or reduce your expected expenses.
              </p>
            </div>
          </div>
        </div>
      )}

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
            <div>
              <CurrencyInput
                label="Annual Expenses (Lean)"
                value={params.annualExpenses}
                onChange={(v) => setParam('annualExpenses', v)}
                tooltip="For Lean FIRE, keep this ≤$40,000"
                max={100000}
              />
              <div className="mt-2">
                <div className="flex justify-between text-xs text-gray-500 dark:text-gray-400 mb-1">
                  <span>Lean threshold</span>
                  <span>{formatCurrency(LEAN_THRESHOLD)}</span>
                </div>
                <div className="h-2 bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden">
                  <div 
                    className={`h-full transition-all ${
                      isLean ? 'bg-green-500' : 'bg-amber-500'
                    }`}
                    style={{ width: `${Math.min(100, (params.annualExpenses / LEAN_THRESHOLD) * 100)}%` }}
                  />
                </div>
              </div>
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
          {/* Key Metrics */}
          <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
            <ResultCard
              label="Lean FIRE Number"
              value={results.fireNumber}
              format="currency"
              highlight
              icon="🌿"
              subtext={isLean ? "You're in Lean territory!" : "Based on your expenses"}
            />
            <ResultCard
              label="Years to Lean FIRE"
              value={results.yearsToFIRE}
              format="years"
              icon="⏱️"
              subtext={`At age ${Math.round(results.fireAge)}`}
            />
            <ResultCard
              label="Monthly Budget"
              value={params.annualExpenses / 12}
              format="currency"
              icon="📅"
              subtext="In retirement"
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
                colorScheme="green"
                height={350}
              />
            </CardContent>
          </Card>

          {/* Lean FIRE Tips */}
          <Card>
            <CardHeader>
              <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Lean FIRE Tips</h2>
            </CardHeader>
            <CardContent>
              <div className="grid sm:grid-cols-2 gap-4">
                <div className="flex gap-3">
                  <span className="text-xl">🏠</span>
                  <div>
                    <h4 className="font-medium text-gray-900 dark:text-gray-100">Housing</h4>
                    <p className="text-sm text-gray-600 dark:text-gray-400">Consider geo-arbitrage, house hacking, or paid-off housing</p>
                  </div>
                </div>
                <div className="flex gap-3">
                  <span className="text-xl">🚗</span>
                  <div>
                    <h4 className="font-medium text-gray-900 dark:text-gray-100">Transportation</h4>
                    <p className="text-sm text-gray-600 dark:text-gray-400">Bike, walk, or use one reliable used car</p>
                  </div>
                </div>
                <div className="flex gap-3">
                  <span className="text-xl">🍳</span>
                  <div>
                    <h4 className="font-medium text-gray-900 dark:text-gray-100">Food</h4>
                    <p className="text-sm text-gray-600 dark:text-gray-400">Cook at home, meal prep, grow some food</p>
                  </div>
                </div>
                <div className="flex gap-3">
                  <span className="text-xl">🎭</span>
                  <div>
                    <h4 className="font-medium text-gray-900 dark:text-gray-100">Entertainment</h4>
                    <p className="text-sm text-gray-600 dark:text-gray-400">Free hobbies, libraries, nature, community events</p>
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>

      <Disclaimer />
    </div>
  )
}
