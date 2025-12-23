import { useMemo } from 'react'
import { useCalculatorParams } from '../hooks/useCalculatorParams'
import { formatCurrency } from '../utils/calculations'
import { CurrencyInput, PercentageInput } from '../components/inputs'
import { Card, CardHeader, CardContent, ResultCard, UrlActions, Disclaimer } from '../components/ui'
import ProgressToFIRE from '../components/ui/ProgressToFIRE'
import { ProjectionChart } from '../components/charts'

// Calculate savings rate and time to FIRE
function calculateSavingsRate(
  annualIncome: number,
  annualExpenses: number,
  currentSavings: number,
  expectedReturn: number,
  inflationRate: number,
  withdrawalRate: number
) {
  const annualSavings = annualIncome - annualExpenses
  const savingsRate = annualIncome > 0 ? annualSavings / annualIncome : 0
  
  // FIRE number based on expenses
  const fireNumber = annualExpenses / withdrawalRate
  
  // Real return
  const realReturn = (1 + expectedReturn) / (1 + inflationRate) - 1
  
  // Years to FIRE
  let years = 0
  let portfolio = currentSavings
  const maxYears = 100
  
  while (portfolio < fireNumber && years < maxYears) {
    portfolio = portfolio * (1 + realReturn) + annualSavings
    years++
  }
  
  const yearsToFIRE = years >= maxYears ? Infinity : years
  
  // Generate projections
  const projections = []
  let bal = currentSavings
  let totalContrib = currentSavings
  const currentYear = new Date().getFullYear()
  
  for (let i = 0; i <= Math.min(yearsToFIRE + 10, 50); i++) {
    projections.push({
      age: 30 + i, // placeholder age
      year: currentYear + i,
      portfolio: Math.round(bal),
      contributions: i === 0 ? currentSavings : annualSavings,
      totalContributions: Math.round(totalContrib),
      inflationAdjusted: Math.round(bal / Math.pow(1 + inflationRate, i)),
    })
    bal = bal * (1 + expectedReturn) + annualSavings
    totalContrib += annualSavings
  }
  
  // Savings rate categories
  let savingsCategory = ''
  let savingsCategoryColor = ''
  if (savingsRate >= 0.5) {
    savingsCategory = 'Extreme Saver'
    savingsCategoryColor = 'text-purple-600 dark:text-purple-400'
  } else if (savingsRate >= 0.3) {
    savingsCategory = 'Aggressive Saver'
    savingsCategoryColor = 'text-green-600 dark:text-green-400'
  } else if (savingsRate >= 0.2) {
    savingsCategory = 'Good Saver'
    savingsCategoryColor = 'text-blue-600 dark:text-blue-400'
  } else if (savingsRate >= 0.1) {
    savingsCategory = 'Average Saver'
    savingsCategoryColor = 'text-amber-600 dark:text-amber-400'
  } else {
    savingsCategory = 'Below Average'
    savingsCategoryColor = 'text-red-600 dark:text-red-400'
  }

  return {
    savingsRate,
    annualSavings,
    monthlySavings: annualSavings / 12,
    fireNumber,
    yearsToFIRE,
    projections,
    savingsCategory,
    savingsCategoryColor,
  }
}

export default function SavingsRate() {
  const { params, setParam, resetParams, copyUrl, hasCustomParams } = useCalculatorParams()
  
  // Use annualContribution as income proxy, derive from inputs
  const annualIncome = params.annualContribution + params.annualExpenses

  const results = useMemo(() => {
    return calculateSavingsRate(
      annualIncome,
      params.annualExpenses,
      params.currentSavings,
      params.expectedReturn,
      params.inflationRate,
      params.withdrawalRate
    )
  }, [annualIncome, params])

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-gray-100 flex items-center gap-3">
            <span className="text-3xl">🧮</span>
            Savings Rate Calculator
          </h1>
          <p className="text-gray-600 dark:text-gray-400 mt-1">
            Calculate your savings rate and see how it impacts your path to FIRE.
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

      {/* Savings Rate Info Banner */}
      <div className="bg-indigo-50 dark:bg-indigo-900/20 border border-indigo-200 dark:border-indigo-800 rounded-xl p-4">
        <div className="flex gap-3">
          <span className="text-2xl">💡</span>
          <div>
            <h3 className="font-semibold text-indigo-900 dark:text-indigo-100">Why Savings Rate Matters</h3>
            <p className="text-sm text-indigo-700 dark:text-indigo-300 mt-1">
              Your savings rate is the single most important factor in reaching FIRE. A 10% savings rate 
              means ~50 years to retirement, while a 50% rate can get you there in ~17 years. It matters 
              more than investment returns!
            </p>
          </div>
        </div>
      </div>

      <div className="grid lg:grid-cols-3 gap-6">
        {/* Inputs */}
        <Card className="lg:col-span-1">
          <CardHeader>
            <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Your Finances</h2>
          </CardHeader>
          <CardContent className="space-y-4">
            <CurrencyInput
              label="Annual Income (After Tax)"
              value={annualIncome}
              onChange={(v) => setParam('annualContribution', v - params.annualExpenses)}
              tooltip="Your take-home pay after taxes"
            />
            <CurrencyInput
              label="Annual Expenses"
              value={params.annualExpenses}
              onChange={(v) => setParam('annualExpenses', v)}
              tooltip="Total yearly spending"
            />
            <CurrencyInput
              label="Current Savings"
              value={params.currentSavings}
              onChange={(v) => setParam('currentSavings', v)}
              tooltip="Total invested assets"
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
          {/* Savings Rate Highlight */}
          <div className="bg-gradient-to-r from-indigo-500 to-purple-500 rounded-xl p-6 text-white">
            <div className="flex items-center justify-between flex-wrap gap-4">
              <div>
                <p className="text-indigo-100 text-sm">Your Savings Rate</p>
                <p className="text-5xl font-bold">{(results.savingsRate * 100).toFixed(1)}%</p>
                <p className={`mt-2 font-semibold ${results.savingsRate >= 0.2 ? 'text-green-200' : 'text-amber-200'}`}>
                  {results.savingsCategory}
                </p>
              </div>
              <div className="text-right">
                <p className="text-indigo-100 text-sm">You Save</p>
                <p className="text-3xl font-bold">{formatCurrency(results.monthlySavings)}</p>
                <p className="text-indigo-200 text-sm">per month</p>
              </div>
            </div>
          </div>

          {/* Key Metrics */}
          <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
            <ResultCard
              label="FIRE Number"
              value={results.fireNumber}
              format="currency"
              highlight
              icon="🎯"
              subtext="Target portfolio"
            />
            <ResultCard
              label="Years to FIRE"
              value={results.yearsToFIRE === Infinity ? 'Never' : results.yearsToFIRE}
              format={results.yearsToFIRE === Infinity ? 'none' : 'years'}
              icon="⏱️"
            />
            <ResultCard
              label="Annual Savings"
              value={results.annualSavings}
              format="currency"
              icon="💰"
            />
          </div>

          {/* Savings Rate Chart Reference */}
          <Card>
            <CardHeader>
              <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Savings Rate to Years to FIRE</h2>
            </CardHeader>
            <CardContent>
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-gray-200 dark:border-gray-700">
                      <th className="text-left py-2 px-3 font-semibold text-gray-900 dark:text-gray-100">Savings Rate</th>
                      <th className="text-left py-2 px-3 font-semibold text-gray-900 dark:text-gray-100">Years to FIRE</th>
                      <th className="text-left py-2 px-3 font-semibold text-gray-900 dark:text-gray-100">Category</th>
                    </tr>
                  </thead>
                  <tbody>
                    {[
                      { rate: 10, years: '51', category: 'Average', color: 'text-gray-600' },
                      { rate: 20, years: '37', category: 'Good', color: 'text-blue-600' },
                      { rate: 30, years: '28', category: 'Great', color: 'text-green-600' },
                      { rate: 40, years: '22', category: 'Aggressive', color: 'text-emerald-600' },
                      { rate: 50, years: '17', category: 'Very Aggressive', color: 'text-purple-600' },
                      { rate: 60, years: '12.5', category: 'Extreme', color: 'text-pink-600' },
                      { rate: 70, years: '8.5', category: 'Ultra Extreme', color: 'text-red-600' },
                    ].map((row) => (
                      <tr 
                        key={row.rate}
                        className={`border-b border-gray-100 dark:border-gray-800 ${
                          Math.abs(results.savingsRate * 100 - row.rate) < 5 ? 'bg-indigo-50 dark:bg-indigo-900/20' : ''
                        }`}
                      >
                        <td className="py-2 px-3 font-medium text-gray-900 dark:text-gray-100">{row.rate}%</td>
                        <td className="py-2 px-3 text-gray-600 dark:text-gray-400">{row.years} years</td>
                        <td className={`py-2 px-3 font-medium ${row.color} dark:opacity-80`}>{row.category}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <p className="text-xs text-gray-500 dark:text-gray-400 mt-3">
                *Assumes 5% real return and starting from $0. Your results may vary.
              </p>
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
                fireNumber={results.fireNumber}
                colorScheme="purple"
                height={350}
              />
            </CardContent>
          </Card>
        </div>
      </div>

      <Disclaimer />
    </div>
  )
}
