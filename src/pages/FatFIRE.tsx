import { useMemo } from 'react'
import { useCalculatorParams } from '../hooks/useCalculatorParams'
import { calculateFatFIRE, formatCurrency } from '../utils/calculations'
import { exportToExcel, formatInputsForExport, formatResultsForExport } from '../utils/excelExport'
import { CurrencyInput, PercentageInput, AgeInput } from '../components/inputs'
import { Card, CardHeader, CardContent, ResultCard, UrlActions, ProgressToFIRE, Disclaimer, ExportButton } from '../components/ui'
import { ProjectionChart } from '../components/charts'

const FAT_THRESHOLD = 100000

export default function FatFIRE() {
  const { params, setParam, resetParams, copyUrl, hasCustomParams } = useCalculatorParams()

  const results = useMemo(() => {
    return calculateFatFIRE({
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

  const isFat = params.annualExpenses >= FAT_THRESHOLD

  const handleExport = () => {
    const inputs = {
      currentAge: params.currentAge,
      retirementAge: params.retirementAge,
      currentSavings: params.currentSavings,
      annualContribution: params.annualContribution,
      expectedReturn: params.expectedReturn,
      inflationRate: params.inflationRate,
      withdrawalRate: params.withdrawalRate,
      annualExpenses: params.annualExpenses,
    }

    exportToExcel({
      calculatorName: 'Fat FIRE',
      inputs: formatInputsForExport(inputs),
      results: formatResultsForExport(results),
      projections: results.projections,
    })
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-gray-100 flex items-center gap-3">
            <span className="text-3xl">💎</span>
            Fat FIRE Calculator
          </h1>
          <p className="text-gray-600 dark:text-gray-400 mt-1">
            Retire in style without compromising your lifestyle.
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

      {/* Fat FIRE Explanation Banner */}
      <div className="bg-purple-50 dark:bg-purple-900/20 border border-purple-200 dark:border-purple-800 rounded-xl p-4">
        <div className="flex gap-3">
          <span className="text-2xl">💎</span>
          <div>
            <h3 className="font-semibold text-purple-900 dark:text-purple-100">What is Fat FIRE?</h3>
            <p className="text-sm text-purple-700 dark:text-purple-300 mt-1">
              Fat FIRE means achieving financial independence while maintaining a luxurious or upper-middle-class 
              lifestyle (typically $100,000+/year in expenses). It requires a larger nest egg but allows you to 
              retire without sacrifice.
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
                label="Annual Expenses (Fat)"
                value={params.annualExpenses}
                onChange={(v) => setParam('annualExpenses', v)}
                tooltip="For Fat FIRE, typically $100,000+ per year"
              />
              <div className="mt-2">
                <div className="flex justify-between text-xs text-gray-500 dark:text-gray-400 mb-1">
                  <span>Fat threshold</span>
                  <span>{formatCurrency(FAT_THRESHOLD)}</span>
                </div>
                <div className="h-2 bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden">
                  <div 
                    className={`h-full transition-all ${
                      isFat ? 'bg-purple-500' : 'bg-gray-400'
                    }`}
                    style={{ width: `${Math.min(100, (params.annualExpenses / FAT_THRESHOLD) * 100)}%` }}
                  />
                </div>
                {isFat && (
                  <p className="text-xs text-purple-600 dark:text-purple-400 mt-1">Fat FIRE territory</p>
                )}
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
              tooltip="Fat FIRE often uses 3.5% for extra safety"
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
              label="Fat FIRE Number"
              value={results.fireNumber}
              format="currency"
              highlight
              subtext="Target portfolio value"
            />
            <ResultCard
              label="Years to Fat FIRE"
              value={results.yearsToFIRE}
              format="years"
              subtext={`At age ${Math.round(results.fireAge)}`}
            />
            <ResultCard
              label="Monthly Lifestyle"
              value={params.annualExpenses / 12}
              format="currency"
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
                colorScheme="purple"
                height={350}
              />
            </CardContent>
          </Card>

          {/* Fat FIRE Lifestyle Examples */}
          <Card>
            <CardHeader>
              <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">What Fat FIRE Lifestyle Includes</h2>
            </CardHeader>
            <CardContent>
              <div className="grid sm:grid-cols-2 gap-4">
                <div className="flex gap-3">
                  <span className="text-xl">🏡</span>
                  <div>
                    <h4 className="font-medium text-gray-900 dark:text-gray-100">Premium Housing</h4>
                    <p className="text-sm text-gray-600 dark:text-gray-400">Nice home in desirable area, no compromise on space</p>
                  </div>
                </div>
                <div className="flex gap-3">
                  <span className="text-xl">✈️</span>
                  <div>
                    <h4 className="font-medium text-gray-900 dark:text-gray-100">Travel Freedom</h4>
                    <p className="text-sm text-gray-600 dark:text-gray-400">Multiple trips per year, business class options</p>
                  </div>
                </div>
                <div className="flex gap-3">
                  <span className="text-xl">🍽️</span>
                  <div>
                    <h4 className="font-medium text-gray-900 dark:text-gray-100">Dining & Entertainment</h4>
                    <p className="text-sm text-gray-600 dark:text-gray-400">Fine dining, concerts, premium experiences</p>
                  </div>
                </div>
                <div className="flex gap-3">
                  <span className="text-xl">🏥</span>
                  <div>
                    <h4 className="font-medium text-gray-900 dark:text-gray-100">Healthcare</h4>
                    <p className="text-sm text-gray-600 dark:text-gray-400">Premium insurance, elective procedures, wellness</p>
                  </div>
                </div>
                <div className="flex gap-3">
                  <span className="text-xl">🎁</span>
                  <div>
                    <h4 className="font-medium text-gray-900 dark:text-gray-100">Generosity</h4>
                    <p className="text-sm text-gray-600 dark:text-gray-400">Help family, charitable giving, leave legacy</p>
                  </div>
                </div>
                <div className="flex gap-3">
                  <span className="text-xl">🛡️</span>
                  <div>
                    <h4 className="font-medium text-gray-900 dark:text-gray-100">Security Buffer</h4>
                    <p className="text-sm text-gray-600 dark:text-gray-400">Extra cushion for market downturns or surprises</p>
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
