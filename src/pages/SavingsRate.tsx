import { useMemo, useState } from 'react'
import { useCalculatorParams } from '../hooks/useCalculatorParams'
import { formatCurrency } from '../utils/calculations'
import { CurrencyInput, PercentageInput } from '../components/inputs'
import { Card, CardHeader, CardContent, ResultCard, UrlActions, Disclaimer } from '../components/ui'
import { ProjectionChart } from '../components/charts'

// Calculate investment growth and savings rate
function calculateInvestmentGrowth(
  startingAmount: number,
  contributionAmount: number,
  contributionFrequency: 'monthly' | 'yearly',
  yearsInvesting: number,
  expectedReturn: number,
  inflationRate: number,
  annualIncome: number,
  currentAge: number = 30
) {
  // Convert contributions to annual
  const annualContribution = contributionFrequency === 'monthly' 
    ? contributionAmount * 12 
    : contributionAmount
  
  // Calculate savings rate
  const savingsRate = annualIncome > 0 ? annualContribution / annualIncome : 0
  
  // Generate projections
  const projections = []
  let nominalBalance = startingAmount
  let inflationAdjustedBalance = startingAmount
  let totalContributions = startingAmount
  const currentYear = new Date().getFullYear()
  
  // Add starting point
  projections.push({
    age: currentAge,
    year: currentYear,
    yearNumber: 0,
    portfolio: Math.round(nominalBalance),
    inflationAdjusted: Math.round(inflationAdjustedBalance),
    totalContributions: Math.round(totalContributions),
    contributions: 0,
  })
  
  // Real return for inflation-adjusted calculations
  const realReturn = (1 + expectedReturn) / (1 + inflationRate) - 1
  
  for (let i = 1; i <= yearsInvesting; i++) {
    // Nominal growth (no inflation adjustment)
    nominalBalance = nominalBalance * (1 + expectedReturn) + annualContribution
    
    // Inflation-adjusted growth
    inflationAdjustedBalance = inflationAdjustedBalance * (1 + realReturn) + annualContribution
    
    totalContributions += annualContribution
    
    projections.push({
      age: currentAge + i,
      year: currentYear + i,
      yearNumber: i,
      portfolio: Math.round(nominalBalance),
      inflationAdjusted: Math.round(inflationAdjustedBalance),
      totalContributions: Math.round(totalContributions),
      contributions: annualContribution,
    })
  }
  
  const finalNominalBalance = nominalBalance
  const finalInflationAdjustedBalance = inflationAdjustedBalance
  const totalInvested = totalContributions
  const totalGrowth = finalNominalBalance - totalInvested
  const inflationImpact = finalNominalBalance - finalInflationAdjustedBalance
  
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
    annualContribution,
    monthlyContribution: annualContribution / 12,
    finalNominalBalance,
    finalInflationAdjustedBalance,
    totalInvested,
    totalGrowth,
    inflationImpact,
    projections,
    savingsCategory,
    savingsCategoryColor,
  }
}

export default function SavingsRate() {
  const { params, setParam, resetParams, copyUrl, hasCustomParams } = useCalculatorParams()
  const [contributionFrequency, setContributionFrequency] = useState<'monthly' | 'yearly'>('monthly')
  const [yearsInvesting, setYearsInvesting] = useState(30)
  const [contributionAmount, setContributionAmount] = useState(500)
  const [annualIncome, setAnnualIncome] = useState(75000)

  const results = useMemo(() => {
    return calculateInvestmentGrowth(
      params.currentSavings,
      contributionAmount,
      contributionFrequency,
      yearsInvesting,
      params.expectedReturn,
      params.inflationRate,
      annualIncome,
      params.currentAge
    )
  }, [params.currentSavings, contributionAmount, contributionFrequency, yearsInvesting, params.expectedReturn, params.inflationRate, annualIncome, params.currentAge])

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-gray-100 flex items-center gap-3">
            <span className="text-3xl">💰</span>
            Savings & Investment Rate Calculator
          </h1>
          <p className="text-gray-600 dark:text-gray-400 mt-1">
            See how your investments can grow over time with consistent contributions.
          </p>
        </div>
        <UrlActions onReset={resetParams} onCopy={copyUrl} hasCustomParams={hasCustomParams} />
      </div>

      {/* Savings Rate Info Banner */}
      <div className="bg-indigo-50 dark:bg-indigo-900/20 border border-indigo-200 dark:border-indigo-800 rounded-xl p-4">
        <div className="flex gap-3">
          <span className="text-2xl">💡</span>
          <div>
            <h3 className="font-semibold text-indigo-900 dark:text-indigo-100">The Power of Compound Interest</h3>
            <p className="text-sm text-indigo-700 dark:text-indigo-300 mt-1">
              Consistent investing is the key to building wealth. Even small amounts invested regularly can 
              grow substantially over time thanks to compound interest. Start early and stay consistent!
            </p>
          </div>
        </div>
      </div>

      <div className="grid lg:grid-cols-3 gap-6">
        {/* Inputs */}
        <Card className="lg:col-span-1">
          <CardHeader>
            <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Investment Details</h2>
          </CardHeader>
          <CardContent className="space-y-4">
            <CurrencyInput
              label="Starting Amount"
              value={params.currentSavings}
              onChange={(v) => setParam('currentSavings', v)}
              tooltip="How much you're starting with"
            />
            
            {/* Contribution Frequency Toggle */}
            <div className="space-y-2">
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">
                Contribution Frequency
              </label>
              <div className="flex gap-2">
                <button
                  onClick={() => setContributionFrequency('monthly')}
                  className={`flex-1 py-2 px-4 rounded-lg font-medium transition-colors ${
                    contributionFrequency === 'monthly'
                      ? 'bg-indigo-600 text-white'
                      : 'bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700'
                  }`}
                >
                  Monthly
                </button>
                <button
                  onClick={() => setContributionFrequency('yearly')}
                  className={`flex-1 py-2 px-4 rounded-lg font-medium transition-colors ${
                    contributionFrequency === 'yearly'
                      ? 'bg-indigo-600 text-white'
                      : 'bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700'
                  }`}
                >
                  Yearly
                </button>
              </div>
            </div>

            <CurrencyInput
              label={`${contributionFrequency === 'monthly' ? 'Monthly' : 'Yearly'} Contribution`}
              value={contributionAmount}
              onChange={setContributionAmount}
              tooltip={`Amount you'll invest ${contributionFrequency}`}
            />

            <div className="space-y-2">
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">
                Years Investing
              </label>
              <input
                type="number"
                value={yearsInvesting}
                onChange={(e) => setYearsInvesting(Math.max(1, Math.min(50, parseInt(e.target.value) || 1)))}
                className="w-full px-4 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                min="1"
                max="50"
              />
            </div>

            <PercentageInput
              label="Expected Annual Return"
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

            <div className="pt-4 border-t border-gray-200 dark:border-gray-700">
              <CurrencyInput
                label="Annual Income (Optional)"
                value={annualIncome}
                onChange={setAnnualIncome}
                tooltip="For calculating your savings rate"
              />
            </div>
          </CardContent>
        </Card>

        {/* Results */}
        <div className="lg:col-span-2 space-y-6">
          {/* Main Result Highlight */}
          <div className="bg-gradient-to-r from-indigo-500 to-purple-500 rounded-xl p-6 text-white">
            <div className="space-y-4">
              <div>
                <p className="text-indigo-100 text-sm">Your Investment Will Grow To</p>
                <p className="text-5xl font-bold">{formatCurrency(results.finalNominalBalance)}</p>
                <p className="text-indigo-200 text-sm mt-1">in {yearsInvesting} years</p>
              </div>
              <div className="pt-4 border-t border-indigo-400/30">
                <p className="text-indigo-100 text-sm">Inflation-Adjusted Value (Today's Dollars)</p>
                <p className="text-3xl font-bold">{formatCurrency(results.finalInflationAdjustedBalance)}</p>
              </div>
            </div>
          </div>

          {/* Savings Rate Display */}
          {annualIncome > 0 && (
            <div className="bg-gradient-to-r from-green-500 to-emerald-500 rounded-xl p-6 text-white">
              <div className="flex items-center justify-between flex-wrap gap-4">
                <div>
                  <p className="text-green-100 text-sm">Your Savings Rate</p>
                  <p className="text-5xl font-bold">{(results.savingsRate * 100).toFixed(1)}%</p>
                  <p className={`mt-2 font-semibold ${results.savingsRate >= 0.2 ? 'text-green-200' : 'text-amber-200'}`}>
                    {results.savingsCategory}
                  </p>
                </div>
                <div className="text-right">
                  <p className="text-green-100 text-sm">You're Investing</p>
                  <p className="text-3xl font-bold">{formatCurrency(results.annualContribution)}</p>
                  <p className="text-green-200 text-sm">per year</p>
                </div>
              </div>
            </div>
          )}

          {/* Key Metrics */}
          <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
            <ResultCard
              label="Total Invested"
              value={results.totalInvested}
              format="currency"
              subtext="Your contributions"
            />
            <ResultCard
              label="Investment Growth"
              value={results.totalGrowth}
              format="currency"
              highlight
              subtext="Earnings from returns"
            />
            <ResultCard
              label="Inflation Impact"
              value={results.inflationImpact}
              format="currency"
              subtext="Purchasing power loss"
            />
          </div>

          {/* Chart */}
          <Card>
            <CardHeader>
              <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Investment Growth Projection</h2>
              <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                Compare nominal growth vs. inflation-adjusted purchasing power
              </p>
            </CardHeader>
            <CardContent>
              <ProjectionChart
                data={results.projections}
                showInflationAdjusted={true}
                showMilestones={false}
                colorScheme="purple"
                height={400}
              />
            </CardContent>
          </Card>

          {/* Breakdown Details */}
          <Card>
            <CardHeader>
              <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Investment Breakdown</h2>
            </CardHeader>
            <CardContent>
              <div className="space-y-3">
                <div className="flex justify-between items-center py-2 border-b border-gray-200 dark:border-gray-700">
                  <span className="text-gray-600 dark:text-gray-400">Starting Amount</span>
                  <span className="font-semibold text-gray-900 dark:text-gray-100">{formatCurrency(params.currentSavings)}</span>
                </div>
                <div className="flex justify-between items-center py-2 border-b border-gray-200 dark:border-gray-700">
                  <span className="text-gray-600 dark:text-gray-400">
                    {contributionFrequency === 'monthly' ? 'Monthly' : 'Yearly'} Contributions
                  </span>
                  <span className="font-semibold text-gray-900 dark:text-gray-100">{formatCurrency(contributionAmount)}</span>
                </div>
                <div className="flex justify-between items-center py-2 border-b border-gray-200 dark:border-gray-700">
                  <span className="text-gray-600 dark:text-gray-400">Total Contributions ({yearsInvesting} years)</span>
                  <span className="font-semibold text-gray-900 dark:text-gray-100">{formatCurrency(results.totalInvested)}</span>
                </div>
                <div className="flex justify-between items-center py-2 border-b border-gray-200 dark:border-gray-700">
                  <span className="text-gray-600 dark:text-gray-400">Investment Earnings</span>
                  <span className="font-semibold text-green-600 dark:text-green-400">{formatCurrency(results.totalGrowth)}</span>
                </div>
                <div className="flex justify-between items-center py-2 border-b border-gray-200 dark:border-gray-700">
                  <span className="text-gray-600 dark:text-gray-400">Final Balance</span>
                  <span className="font-bold text-lg text-indigo-600 dark:text-indigo-400">{formatCurrency(results.finalNominalBalance)}</span>
                </div>
                <div className="flex justify-between items-center py-2">
                  <span className="text-gray-600 dark:text-gray-400">Inflation-Adjusted Value</span>
                  <span className="font-bold text-lg text-gray-900 dark:text-gray-100">{formatCurrency(results.finalInflationAdjustedBalance)}</span>
                </div>
              </div>
              
              <div className="mt-4 p-3 bg-amber-50 dark:bg-amber-900/20 rounded-lg">
                <p className="text-sm text-amber-800 dark:text-amber-200">
                  <strong>Note:</strong> Inflation will reduce your purchasing power by {formatCurrency(results.inflationImpact)} 
                  over {yearsInvesting} years at {(params.inflationRate * 100).toFixed(1)}% annual inflation.
                </p>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>

      <Disclaimer />
    </div>
  )
}
