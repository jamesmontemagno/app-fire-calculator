import { useMemo } from 'react'
import { useCalculatorParams } from '../hooks/useCalculatorParams'
import { formatCurrency } from '../utils/calculations'
import { AgeInput } from '../components/inputs'
import { Card, CardHeader, CardContent, ResultCard, UrlActions, Disclaimer } from '../components/ui'

// Healthcare cost estimates by age (annual, US average) - for future use
// const HEALTHCARE_COSTS = {
//   under40: { min: 4000, avg: 6000, max: 10000 },
//   '40-49': { min: 5500, avg: 8000, max: 13000 },
//   '50-54': { min: 7000, avg: 10000, max: 16000 },
//   '55-59': { min: 9000, avg: 13000, max: 20000 },
//   '60-64': { min: 12000, avg: 17000, max: 25000 },
// }

function calculateHealthcareGap(
  currentAge: number,
  earlyRetirementAge: number,
  medicareAge: number,
  monthlyPremium: number,
  annualDeductible: number,
  annualOutOfPocket: number,
  inflationRate: number
) {
  const gapYears = Math.max(0, medicareAge - earlyRetirementAge)
  
  // Annual healthcare cost
  const annualCost = (monthlyPremium * 12) + annualDeductible + annualOutOfPocket
  
  // Calculate total cost with inflation
  let totalCost = 0
  const yearlyBreakdown = []
  
  for (let i = 0; i < gapYears; i++) {
    const age = earlyRetirementAge + i
    const yearCost = annualCost * Math.pow(1 + inflationRate, i)
    totalCost += yearCost
    yearlyBreakdown.push({
      age,
      year: new Date().getFullYear() + (earlyRetirementAge - currentAge) + i,
      cost: Math.round(yearCost),
      premium: Math.round(monthlyPremium * 12 * Math.pow(1 + inflationRate, i)),
      deductible: Math.round(annualDeductible * Math.pow(1 + inflationRate, i)),
      outOfPocket: Math.round(annualOutOfPocket * Math.pow(1 + inflationRate, i)),
    })
  }
  
  // ACA subsidy estimates (simplified)
  const calculateSubsidy = (income: number) => {
    // Very simplified - actual ACA subsidies are complex
    if (income < 30000) return annualCost * 0.7 // ~70% subsidy
    if (income < 50000) return annualCost * 0.5 // ~50% subsidy
    if (income < 75000) return annualCost * 0.3 // ~30% subsidy
    if (income < 100000) return annualCost * 0.15 // ~15% subsidy
    return 0
  }
  
  return {
    gapYears,
    annualCost,
    totalCost: Math.round(totalCost),
    avgAnnualCost: gapYears > 0 ? Math.round(totalCost / gapYears) : 0,
    yearlyBreakdown,
    estimatedSubsidy30k: Math.round(calculateSubsidy(30000)),
    estimatedSubsidy50k: Math.round(calculateSubsidy(50000)),
    estimatedSubsidy75k: Math.round(calculateSubsidy(75000)),
  }
}

export default function HealthcareGap() {
  const { params, setParam, resetParams, copyUrl, hasCustomParams } = useCalculatorParams()
  
  // Healthcare-specific params with defaults
  const monthlyPremium = 600 // Default ACA silver plan
  const annualDeductible = 2500
  const annualOutOfPocket = 2000
  const medicareAge = 65

  const results = useMemo(() => {
    return calculateHealthcareGap(
      params.currentAge,
      params.retirementAge,
      medicareAge,
      monthlyPremium,
      annualDeductible,
      annualOutOfPocket,
      params.inflationRate
    )
  }, [params, monthlyPremium, annualDeductible, annualOutOfPocket])

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-gray-100 flex items-center gap-3">
            <span className="text-3xl">🏥</span>
            Healthcare Gap Calculator
          </h1>
          <p className="text-gray-600 dark:text-gray-400 mt-1">
            Estimate healthcare costs between early retirement and Medicare eligibility.
          </p>
        </div>
        <UrlActions onReset={resetParams} onCopy={copyUrl} hasCustomParams={hasCustomParams} />
      </div>

      {/* Warning Banner */}
      <div className="bg-rose-50 dark:bg-rose-900/20 border border-rose-200 dark:border-rose-800 rounded-xl p-4">
        <div className="flex gap-3">
          <span className="text-2xl">⚠️</span>
          <div>
            <h3 className="font-semibold text-rose-900 dark:text-rose-100">The Healthcare Gap is Real</h3>
            <p className="text-sm text-rose-700 dark:text-rose-300 mt-1">
              If you retire before age 65 in the US, you'll need to bridge the gap until Medicare. 
              Healthcare costs are often the #1 overlooked expense in early retirement planning. 
              This can add <strong>{formatCurrency(results.totalCost)}</strong> or more to your FIRE number!
            </p>
          </div>
        </div>
      </div>

      <div className="grid lg:grid-cols-3 gap-6">
        {/* Inputs */}
        <Card className="lg:col-span-1">
          <CardHeader>
            <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Your Situation</h2>
          </CardHeader>
          <CardContent className="space-y-4">
            <AgeInput
              label="Current Age"
              value={params.currentAge}
              onChange={(v) => setParam('currentAge', v)}
            />
            <AgeInput
              label="Early Retirement Age"
              value={params.retirementAge}
              onChange={(v) => setParam('retirementAge', v)}
              tooltip="When you plan to leave employer-sponsored insurance"
              max={64}
            />
            <div className="p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
              <p className="text-sm text-gray-600 dark:text-gray-400">
                Medicare eligibility: <span className="font-semibold text-gray-900 dark:text-gray-100">Age 65</span>
              </p>
              <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                Gap to cover: <span className="font-semibold text-gray-900 dark:text-gray-100">{results.gapYears} years</span>
              </p>
            </div>
            
            <div className="border-t border-gray-200 dark:border-gray-700 pt-4">
              <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">
                Healthcare Cost Assumptions
              </h3>
              <p className="text-xs text-gray-500 dark:text-gray-400 mb-3">
                Based on ACA Silver plan averages. Adjust based on your state and needs.
              </p>
              <div className="space-y-2 text-sm">
                <div className="flex justify-between">
                  <span className="text-gray-600 dark:text-gray-400">Monthly Premium</span>
                  <span className="font-medium text-gray-900 dark:text-gray-100">{formatCurrency(monthlyPremium)}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-600 dark:text-gray-400">Annual Deductible</span>
                  <span className="font-medium text-gray-900 dark:text-gray-100">{formatCurrency(annualDeductible)}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-600 dark:text-gray-400">Est. Out-of-Pocket</span>
                  <span className="font-medium text-gray-900 dark:text-gray-100">{formatCurrency(annualOutOfPocket)}</span>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Results */}
        <div className="lg:col-span-2 space-y-6">
          {/* Total Cost Highlight */}
          <div className="bg-gradient-to-r from-rose-500 to-pink-500 rounded-xl p-6 text-white">
            <div className="flex items-center justify-between flex-wrap gap-4">
              <div>
                <p className="text-rose-100 text-sm">Total Healthcare Gap Cost</p>
                <p className="text-5xl font-bold">{formatCurrency(results.totalCost)}</p>
                <p className="text-rose-200 mt-1">Over {results.gapYears} years until Medicare</p>
              </div>
              <div className="text-right">
                <p className="text-rose-100 text-sm">Add to FIRE Number</p>
                <p className="text-2xl font-bold">+{formatCurrency(results.totalCost)}</p>
              </div>
            </div>
          </div>

          {/* Key Metrics */}
          <div className="grid sm:grid-cols-3 gap-4">
            <ResultCard
              label="Gap Years"
              value={results.gapYears}
              format="years"
              icon="📅"
              subtext={`Age ${params.retirementAge} to ${medicareAge}`}
            />
            <ResultCard
              label="Year 1 Cost"
              value={results.annualCost}
              format="currency"
              icon="💵"
              subtext="Before inflation"
            />
            <ResultCard
              label="Avg Annual Cost"
              value={results.avgAnnualCost}
              format="currency"
              icon="📊"
              subtext="With inflation"
            />
          </div>

          {/* ACA Subsidy Info */}
          <Card>
            <CardHeader>
              <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">ACA Subsidy Potential</h2>
              <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
                Your retirement income affects ACA premium subsidies. Lower income = higher subsidies.
              </p>
            </CardHeader>
            <CardContent>
              <div className="grid sm:grid-cols-3 gap-4">
                <div className="p-4 bg-green-50 dark:bg-green-900/20 rounded-lg">
                  <p className="text-sm text-green-700 dark:text-green-300">$30k Income</p>
                  <p className="text-xl font-bold text-green-800 dark:text-green-200">
                    ~{formatCurrency(results.estimatedSubsidy30k)}
                  </p>
                  <p className="text-xs text-green-600 dark:text-green-400">Annual subsidy</p>
                </div>
                <div className="p-4 bg-amber-50 dark:bg-amber-900/20 rounded-lg">
                  <p className="text-sm text-amber-700 dark:text-amber-300">$50k Income</p>
                  <p className="text-xl font-bold text-amber-800 dark:text-amber-200">
                    ~{formatCurrency(results.estimatedSubsidy50k)}
                  </p>
                  <p className="text-xs text-amber-600 dark:text-amber-400">Annual subsidy</p>
                </div>
                <div className="p-4 bg-gray-50 dark:bg-gray-800 rounded-lg">
                  <p className="text-sm text-gray-700 dark:text-gray-300">$75k+ Income</p>
                  <p className="text-xl font-bold text-gray-800 dark:text-gray-200">
                    ~{formatCurrency(results.estimatedSubsidy75k)}
                  </p>
                  <p className="text-xs text-gray-600 dark:text-gray-400">Annual subsidy</p>
                </div>
              </div>
              <p className="text-xs text-gray-500 dark:text-gray-400 mt-4">
                * Subsidies are estimates based on simplified calculations. Actual amounts depend on your state, 
                household size, plan type, and exact income. Check healthcare.gov for accurate quotes.
              </p>
            </CardContent>
          </Card>

          {/* Year by Year Breakdown */}
          {results.yearlyBreakdown.length > 0 && (
            <Card>
              <CardHeader>
                <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Year-by-Year Breakdown</h2>
              </CardHeader>
              <CardContent>
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b border-gray-200 dark:border-gray-700">
                        <th className="text-left py-2 px-3 font-semibold text-gray-900 dark:text-gray-100">Age</th>
                        <th className="text-left py-2 px-3 font-semibold text-gray-900 dark:text-gray-100">Year</th>
                        <th className="text-left py-2 px-3 font-semibold text-gray-900 dark:text-gray-100">Premium</th>
                        <th className="text-left py-2 px-3 font-semibold text-gray-900 dark:text-gray-100">Deductible</th>
                        <th className="text-left py-2 px-3 font-semibold text-gray-900 dark:text-gray-100">Out-of-Pocket</th>
                        <th className="text-left py-2 px-3 font-semibold text-gray-900 dark:text-gray-100">Total</th>
                      </tr>
                    </thead>
                    <tbody>
                      {results.yearlyBreakdown.slice(0, 10).map((row) => (
                        <tr key={row.age} className="border-b border-gray-100 dark:border-gray-800">
                          <td className="py-2 px-3 font-medium text-gray-900 dark:text-gray-100">{row.age}</td>
                          <td className="py-2 px-3 text-gray-600 dark:text-gray-400">{row.year}</td>
                          <td className="py-2 px-3 text-gray-600 dark:text-gray-400">{formatCurrency(row.premium)}</td>
                          <td className="py-2 px-3 text-gray-600 dark:text-gray-400">{formatCurrency(row.deductible)}</td>
                          <td className="py-2 px-3 text-gray-600 dark:text-gray-400">{formatCurrency(row.outOfPocket)}</td>
                          <td className="py-2 px-3 font-medium text-gray-900 dark:text-gray-100">{formatCurrency(row.cost)}</td>
                        </tr>
                      ))}
                    </tbody>
                    <tfoot>
                      <tr className="bg-gray-50 dark:bg-gray-800">
                        <td colSpan={5} className="py-2 px-3 font-semibold text-gray-900 dark:text-gray-100">
                          Total ({results.gapYears} years)
                        </td>
                        <td className="py-2 px-3 font-bold text-rose-600 dark:text-rose-400">
                          {formatCurrency(results.totalCost)}
                        </td>
                      </tr>
                    </tfoot>
                  </table>
                </div>
              </CardContent>
            </Card>
          )}

          {/* Tips */}
          <Card>
            <CardHeader>
              <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Healthcare Gap Strategies</h2>
            </CardHeader>
            <CardContent>
              <div className="grid sm:grid-cols-2 gap-4">
                <div className="flex gap-3">
                  <span className="text-xl">☕</span>
                  <div>
                    <h4 className="font-medium text-gray-900 dark:text-gray-100">Barista FIRE</h4>
                    <p className="text-sm text-gray-600 dark:text-gray-400">Part-time job with health benefits (Starbucks, Costco, etc.)</p>
                  </div>
                </div>
                <div className="flex gap-3">
                  <span className="text-xl">💰</span>
                  <div>
                    <h4 className="font-medium text-gray-900 dark:text-gray-100">HSA Bridge</h4>
                    <p className="text-sm text-gray-600 dark:text-gray-400">Max out HSA while working, use tax-free in retirement</p>
                  </div>
                </div>
                <div className="flex gap-3">
                  <span className="text-xl">📉</span>
                  <div>
                    <h4 className="font-medium text-gray-900 dark:text-gray-100">Income Management</h4>
                    <p className="text-sm text-gray-600 dark:text-gray-400">Keep income low to qualify for ACA subsidies</p>
                  </div>
                </div>
                <div className="flex gap-3">
                  <span className="text-xl">🌍</span>
                  <div>
                    <h4 className="font-medium text-gray-900 dark:text-gray-100">Geo-Arbitrage</h4>
                    <p className="text-sm text-gray-600 dark:text-gray-400">Retire abroad with affordable healthcare options</p>
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
