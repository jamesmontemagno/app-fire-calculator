import { useMemo } from 'react'
import { useCalculatorParams } from '../hooks/useCalculatorParams'
import { formatCurrency, generateProjections } from '../utils/calculations'
import { CurrencyInput, PercentageInput, AgeInput } from '../components/inputs'
import { Card, CardHeader, CardContent, ResultCard, UrlActions, Disclaimer } from '../components/ui'
import ProgressToFIRE from '../components/ui/ProgressToFIRE'
import { ProjectionChart } from '../components/charts'

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
  const { params, setParam, resetParams, copyUrl, hasCustomParams } = useCalculatorParams()

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

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-gray-100 flex items-center gap-3">
            <span className="text-3xl">🎯</span>
            Reverse FIRE Calculator
          </h1>
          <p className="text-gray-600 dark:text-gray-400 mt-1">
            Find out how much you need to save monthly to FIRE by your target age.
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

      {/* Explanation Banner */}
      <div className="bg-teal-50 dark:bg-teal-900/20 border border-teal-200 dark:border-teal-800 rounded-xl p-4">
        <div className="flex gap-3">
          <span className="text-2xl">🔄</span>
          <div>
            <h3 className="font-semibold text-teal-900 dark:text-teal-100">How Reverse FIRE Works</h3>
            <p className="text-sm text-teal-700 dark:text-teal-300 mt-1">
              Instead of calculating when you'll FIRE based on your savings, this calculator works backwards: 
              you set your target retirement age, and it tells you exactly how much you need to save each month 
              to get there.
            </p>
          </div>
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
            />
            <AgeInput
              label="Target FIRE Age"
              value={params.retirementAge}
              onChange={(v) => setParam('retirementAge', v)}
              tooltip="When do you want to achieve FIRE?"
              min={params.currentAge + 1}
            />
            <CurrencyInput
              label="Current Savings"
              value={params.currentSavings}
              onChange={(v) => setParam('currentSavings', v)}
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

          {/* Scenario Comparison */}
          <Card>
            <CardHeader>
              <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Different Target Ages</h2>
            </CardHeader>
            <CardContent>
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-gray-200 dark:border-gray-700">
                      <th className="text-left py-2 px-3 font-semibold text-gray-900 dark:text-gray-100">Target Age</th>
                      <th className="text-left py-2 px-3 font-semibold text-gray-900 dark:text-gray-100">Years Away</th>
                      <th className="text-left py-2 px-3 font-semibold text-gray-900 dark:text-gray-100">Monthly Savings</th>
                      <th className="text-left py-2 px-3 font-semibold text-gray-900 dark:text-gray-100">Annual Savings</th>
                    </tr>
                  </thead>
                  <tbody>
                    {[40, 45, 50, 55, 60, 65].filter(age => age > params.currentAge).map((targetAge) => {
                      const calc = calculateReverseFIRE(
                        params.currentAge,
                        targetAge,
                        params.currentSavings,
                        params.annualExpenses,
                        params.expectedReturn,
                        params.inflationRate,
                        params.withdrawalRate
                      )
                      return (
                        <tr 
                          key={targetAge}
                          className={`border-b border-gray-100 dark:border-gray-800 ${
                            targetAge === params.retirementAge ? 'bg-teal-50 dark:bg-teal-900/20' : ''
                          }`}
                        >
                          <td className="py-2 px-3 font-medium text-gray-900 dark:text-gray-100">
                            Age {targetAge}
                            {targetAge === params.retirementAge && (
                              <span className="ml-2 text-xs bg-teal-100 dark:bg-teal-800 text-teal-700 dark:text-teal-300 px-2 py-0.5 rounded">
                                Selected
                              </span>
                            )}
                          </td>
                          <td className="py-2 px-3 text-gray-600 dark:text-gray-400">{targetAge - params.currentAge} years</td>
                          <td className="py-2 px-3 text-gray-900 dark:text-gray-100 font-medium">
                            {calc.alreadyAchievable ? (
                              <span className="text-green-600 dark:text-green-400">$0 (on track!)</span>
                            ) : (
                              formatCurrency(calc.requiredMonthlySavings)
                            )}
                          </td>
                          <td className="py-2 px-3 text-gray-600 dark:text-gray-400">
                            {calc.alreadyAchievable ? '-' : formatCurrency(calc.requiredAnnualSavings)}
                          </td>
                        </tr>
                      )
                    })}
                  </tbody>
                </table>
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
  )
}
