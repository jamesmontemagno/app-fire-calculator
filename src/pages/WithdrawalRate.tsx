import { useMemo } from 'react'
import { useCalculatorParams } from '../hooks/useCalculatorParams'
import { calculateWithdrawal, formatCurrency } from '../utils/calculations'
import { exportToExcel, formatInputsForExport, formatResultsForExport } from '../utils/excelExport'
import { CurrencyInput, PercentageInput, InputGroup } from '../components/inputs'
import { Card, CardHeader, CardContent, ResultCard, UrlActions, Disclaimer, ExportButton } from '../components/ui'
import { WithdrawalChart } from '../components/charts'

export default function WithdrawalRate() {
  const { params, setParam, resetParams, copyUrl, hasCustomParams } = useCalculatorParams()

  const results = useMemo(() => {
    return calculateWithdrawal(
      params.portfolioValue,
      params.withdrawalRate,
      params.expectedReturn,
      params.inflationRate,
      params.retirementYears
    )
  }, [params])

  const handleExport = () => {
    const inputs = {
      portfolioValue: params.portfolioValue,
      withdrawalRate: params.withdrawalRate,
      expectedReturn: params.expectedReturn,
      inflationRate: params.inflationRate,
      retirementYears: params.retirementYears,
    }

    exportToExcel({
      calculatorName: 'Withdrawal Rate',
      inputs: formatInputsForExport(inputs),
      results: formatResultsForExport(results),
      projections: results.withdrawalProjections,
      additionalSheets: [{
        name: 'Rate Analysis',
        data: results.rateAnalysis,
      }],
    })
  }

  const getSuccessColor = (rate: number) => {
    const analysis = results.rateAnalysis.find(a => a.rate === rate)
    if (!analysis) return 'text-gray-600'
    if (analysis.years >= params.retirementYears) return 'text-green-600 dark:text-green-400'
    if (analysis.years >= params.retirementYears * 0.8) return 'text-amber-600 dark:text-amber-400'
    return 'text-red-600 dark:text-red-400'
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-gray-100 flex items-center gap-3">
            <span className="text-3xl">📊</span>
            Withdrawal Rate Calculator
          </h1>
          <p className="text-gray-600 dark:text-gray-400 mt-1">
            Test your portfolio's longevity and find your safe withdrawal rate.
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          <ExportButton onExport={handleExport} />
          <UrlActions onReset={resetParams} onCopy={copyUrl} hasCustomParams={hasCustomParams} />
        </div>
      </div>

      {/* 4% Rule Explanation Banner */}
      <div className="bg-sky-50 dark:bg-sky-900/20 border border-sky-200 dark:border-sky-800 rounded-xl p-4">
        <div className="flex gap-3">
          <span className="text-2xl">📚</span>
          <div>
            <h3 className="font-semibold text-sky-900 dark:text-sky-100">The 4% Rule</h3>
            <p className="text-sm text-sky-700 dark:text-sky-300 mt-1">
              The famous "4% rule" from the Trinity Study suggests withdrawing 4% of your portfolio in year one, 
              then adjusting for inflation each year. This has historically provided a 95%+ success rate over 
              30-year retirements. However, longer retirements may require lower rates (3-3.5%).
            </p>
          </div>
        </div>
      </div>

      <div className="grid lg:grid-cols-3 gap-6">
        {/* Inputs */}
        <Card className="lg:col-span-1">
          <CardHeader>
            <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Your Portfolio</h2>
          </CardHeader>
          <CardContent className="space-y-4">
            <CurrencyInput
              label="Portfolio Value"
              value={params.portfolioValue}
              onChange={(v) => setParam('portfolioValue', v)}
              tooltip="Current total invested assets"
            />
            <PercentageInput
              label="Withdrawal Rate"
              value={params.withdrawalRate}
              onChange={(v) => setParam('withdrawalRate', v)}
              tooltip="Percentage of portfolio to withdraw yearly"
              min={0.02}
              max={0.08}
              step={0.005}
            />
            <InputGroup
              label="Retirement Duration"
              value={params.retirementYears}
              onChange={(v) => setParam('retirementYears', v)}
              tooltip="How many years you need the portfolio to last"
              suffix="years"
              min={10}
              max={60}
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
          </CardContent>
        </Card>

        {/* Results */}
        <div className="lg:col-span-2 space-y-6">
          {/* Key Metrics */}
          <div className="grid sm:grid-cols-2 lg:grid-cols-4 gap-4">
            <ResultCard
              label="Annual Withdrawal"
              value={results.annualWithdrawal}
              format="currency"
              highlight
            />
            <ResultCard
              label="Monthly Withdrawal"
              value={results.monthlyWithdrawal}
              format="currency"
            />
            <ResultCard
              label="Portfolio Lasts"
              value={results.portfolioLongevity}
              format="years"
              subtext={results.portfolioLongevity >= params.retirementYears ? 'Sustainable' : 'May run out'}
            />
            <ResultCard
              label="Success Rate"
              value={results.successRate}
              format="percent"
              subtext={results.successRate >= 1 ? 'Good' : 'Risky'}
            />
          </div>

          {/* Chart */}
          <Card>
            <CardHeader>
              <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Portfolio Balance Over Time</h2>
            </CardHeader>
            <CardContent>
              <WithdrawalChart
                data={results.withdrawalProjections}
                height={300}
              />
            </CardContent>
          </Card>

          {/* Rate Analysis Table */}
          <Card>
            <CardHeader>
              <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Withdrawal Rate Analysis</h2>
              <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
                Compare different withdrawal rates for your portfolio of {formatCurrency(params.portfolioValue)}
              </p>
            </CardHeader>
            <CardContent>
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-gray-200 dark:border-gray-700">
                      <th className="text-left py-3 px-4 font-semibold text-gray-900 dark:text-gray-100">Rate</th>
                      <th className="text-left py-3 px-4 font-semibold text-gray-900 dark:text-gray-100">Annual Withdrawal</th>
                      <th className="text-left py-3 px-4 font-semibold text-gray-900 dark:text-gray-100">Monthly</th>
                      <th className="text-left py-3 px-4 font-semibold text-gray-900 dark:text-gray-100">Portfolio Lasts</th>
                      <th className="text-left py-3 px-4 font-semibold text-gray-900 dark:text-gray-100">Status</th>
                    </tr>
                  </thead>
                  <tbody>
                    {results.rateAnalysis.map((analysis) => {
                      const annualWithdrawal = params.portfolioValue * analysis.rate
                      const meetsGoal = analysis.years >= params.retirementYears
                      return (
                        <tr 
                          key={analysis.rate} 
                          className={`border-b border-gray-100 dark:border-gray-800 ${
                            analysis.rate === params.withdrawalRate ? 'bg-sky-50 dark:bg-sky-900/20' : ''
                          }`}
                        >
                          <td className="py-3 px-4">
                            <span className={`font-medium ${getSuccessColor(analysis.rate)}`}>
                              {(analysis.rate * 100).toFixed(1)}%
                            </span>
                            {analysis.rate === params.withdrawalRate && (
                              <span className="ml-2 text-xs bg-sky-100 dark:bg-sky-800 text-sky-700 dark:text-sky-300 px-2 py-0.5 rounded">
                                Selected
                              </span>
                            )}
                          </td>
                          <td className="py-3 px-4 text-gray-900 dark:text-gray-100">
                            {formatCurrency(annualWithdrawal)}
                          </td>
                          <td className="py-3 px-4 text-gray-600 dark:text-gray-400">
                            {formatCurrency(annualWithdrawal / 12)}
                          </td>
                          <td className="py-3 px-4">
                            <span className={getSuccessColor(analysis.rate)}>
                              {analysis.years >= 50 ? '50+ years' : `${analysis.years} years`}
                            </span>
                          </td>
                          <td className="py-3 px-4">
                            {meetsGoal ? (
                              <span className="inline-flex items-center gap-1 text-green-600 dark:text-green-400">
                                <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                                </svg>
                                Sustainable
                              </span>
                            ) : (
                              <span className="inline-flex items-center gap-1 text-red-600 dark:text-red-400">
                                <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                                </svg>
                                Too High
                              </span>
                            )}
                          </td>
                        </tr>
                      )
                    })}
                  </tbody>
                </table>
              </div>
            </CardContent>
          </Card>

          {/* Recommendations */}
          <Card>
            <CardHeader>
              <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Recommendations</h2>
            </CardHeader>
            <CardContent className="prose dark:prose-invert max-w-none text-sm">
              {results.portfolioLongevity >= params.retirementYears ? (
                <div className="flex gap-3 p-3 bg-green-50 dark:bg-green-900/20 rounded-lg">
                  <span className="text-xl">✅</span>
                  <p className="text-green-800 dark:text-green-200 m-0">
                    <strong>Your withdrawal rate is sustainable!</strong> At {(params.withdrawalRate * 100).toFixed(1)}%, 
                    your portfolio of {formatCurrency(params.portfolioValue)} should last {results.portfolioLongevity} years, 
                    meeting your {params.retirementYears}-year goal.
                  </p>
                </div>
              ) : (
                <div className="flex gap-3 p-3 bg-amber-50 dark:bg-amber-900/20 rounded-lg">
                  <span className="text-xl">⚠️</span>
                  <p className="text-amber-800 dark:text-amber-200 m-0">
                    <strong>Consider lowering your withdrawal rate.</strong> At {(params.withdrawalRate * 100).toFixed(1)}%, 
                    your portfolio would only last {results.portfolioLongevity} years. Try reducing to 3.5% or 3% for 
                    a longer-lasting portfolio.
                  </p>
                </div>
              )}
              
              <div className="mt-4 space-y-2 text-gray-600 dark:text-gray-400">
                <p>
                  <strong>For early retirees (40+ year retirements):</strong> Consider a 3-3.5% withdrawal rate 
                  for extra safety margin.
                </p>
                <p>
                  <strong>Flexible spending:</strong> Being willing to reduce spending during market downturns 
                  significantly improves success rates.
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
