import { useMemo, useState } from 'react'
import { useCalculatorParams } from '../hooks/useCalculatorParams'
import { 
  calculateSnowballPayoff, 
  calculateAvalanchePayoff, 
  formatCurrency,
} from '../utils/calculations'
import type { DebtItem } from '../utils/calculations'
import { CurrencyInput } from '../components/inputs'
import DebtListInput from '../components/inputs/DebtListInput'
import { Card, CardHeader, CardContent, ResultCard, UrlActions, Disclaimer } from '../components/ui'
import DebtBalanceChart from '../components/charts/DebtBalanceChart'
import DebtBreakdownChart from '../components/charts/DebtBreakdownChart'

export default function DebtPayoff() {
  const { params, resetParams, copyUrl, hasCustomParams } = useCalculatorParams()
  
  // Local state for debts (will sync with URL params)
  const [debts, setDebts] = useState<DebtItem[]>(params.debts || [])
  const [mode, setMode] = useState<'fixed' | 'target'>(params.debtMode || 'fixed')
  const [strategy, setStrategy] = useState<'snowball' | 'avalanche'>(params.debtStrategy || 'snowball')
  const [monthlyBudget, setMonthlyBudget] = useState(params.debtBudget || 1000)
  const [targetMonths, setTargetMonths] = useState(params.debtMonths || 36)
  const [extraPayment, setExtraPayment] = useState(params.debtExtra || 0)

  // Calculate results
  const results = useMemo(() => {
    if (debts.length === 0 || debts.some(d => d.balance <= 0)) {
      return null
    }

    const totalMinPayments = debts.reduce((sum, d) => sum + d.minPayment, 0)
    
    if (mode === 'fixed' && monthlyBudget < totalMinPayments) {
      return null // Can't afford minimum payments
    }

    const baseResult = strategy === 'snowball'
      ? calculateSnowballPayoff(debts, monthlyBudget, 0)
      : calculateAvalanchePayoff(debts, monthlyBudget, 0)

    const withExtraResult = extraPayment > 0
      ? (strategy === 'snowball'
          ? calculateSnowballPayoff(debts, monthlyBudget, extraPayment)
          : calculateAvalanchePayoff(debts, monthlyBudget, extraPayment))
      : null

    return { base: baseResult, withExtra: withExtraResult }
  }, [debts, mode, monthlyBudget, targetMonths, strategy, extraPayment])

  const comparisonResults = useMemo(() => {
    if (debts.length === 0 || debts.some(d => d.balance <= 0)) {
      return null
    }

    const totalMinPayments = debts.reduce((sum, d) => sum + d.minPayment, 0)
    if (monthlyBudget < totalMinPayments) return null

    const snowball = calculateSnowballPayoff(debts, monthlyBudget, extraPayment)
    const avalanche = calculateAvalanchePayoff(debts, monthlyBudget, extraPayment)

    return { snowball, avalanche }
  }, [debts, monthlyBudget, extraPayment])

  const totalDebt = debts.reduce((sum, d) => sum + d.balance, 0)
  const totalMinPayments = debts.reduce((sum, d) => sum + d.minPayment, 0)
  const canCalculate = debts.length > 0 && totalDebt > 0 && monthlyBudget >= totalMinPayments

  const extraPaymentSavings = useMemo(() => {
    if (!results?.base || !results?.withExtra) return null
    
    const monthsSaved = results.base.totalMonths - results.withExtra.totalMonths
    const interestSaved = results.base.totalInterest - results.withExtra.totalInterest
    
    return { monthsSaved, interestSaved }
  }, [results])

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-gray-100 flex items-center gap-3">
            <span className="text-3xl">💳</span>
            Debt Payoff Calculator
          </h1>
          <p className="text-gray-600 dark:text-gray-400 mt-1">
            Eliminate debt faster with Snowball or Avalanche strategies.
          </p>
        </div>
        <UrlActions onReset={resetParams} onCopy={copyUrl} hasCustomParams={hasCustomParams} />
      </div>

      {/* Info Banner */}
      <div className="bg-gradient-to-r from-red-50 to-rose-50 dark:from-red-900/20 dark:to-rose-900/20 border border-red-200 dark:border-red-800 rounded-xl p-4">
        <div className="flex items-start gap-3">
          <span className="text-2xl flex-shrink-0">💪</span>
          <div>
            <h3 className="font-semibold text-gray-900 dark:text-gray-100 mb-1">
              Two Proven Debt Payoff Methods
            </h3>
            <p className="text-sm text-gray-700 dark:text-gray-300">
              <strong>Snowball:</strong> Pay smallest debts first for quick wins and motivation. 
              <strong className="ml-2">Avalanche:</strong> Pay highest interest rates first to save the most money.
            </p>
          </div>
        </div>
      </div>

      {/* Mode Toggle */}
      <div className="flex justify-center">
        <div className="inline-flex bg-gray-100 dark:bg-gray-800 rounded-lg p-1">
          <button
            onClick={() => setMode('fixed')}
            className={`px-6 py-2 rounded-md font-medium transition-colors ${
              mode === 'fixed'
                ? 'bg-white dark:bg-gray-700 text-fire-700 dark:text-fire-400 shadow-sm'
                : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-100'
            }`}
          >
            Fixed Budget
          </button>
          <button
            onClick={() => setMode('target')}
            className={`px-6 py-2 rounded-md font-medium transition-colors ${
              mode === 'target'
                ? 'bg-white dark:bg-gray-700 text-fire-700 dark:text-fire-400 shadow-sm'
                : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-100'
            }`}
          >
            Target Timeline
          </button>
        </div>
      </div>

      <div className="grid lg:grid-cols-3 gap-6">
        {/* Inputs */}
        <Card className="lg:col-span-1">
          <CardHeader>
            <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Debt Information</h2>
          </CardHeader>
          <CardContent className="space-y-6">
            <DebtListInput debts={debts} onChange={setDebts} />

            <div className="pt-4 border-t border-gray-200 dark:border-gray-700">
              {mode === 'fixed' ? (
                <>
                  <CurrencyInput
                    label="Total Monthly Budget"
                    value={monthlyBudget}
                    onChange={setMonthlyBudget}
                    tooltip="Total amount you can pay toward debts each month"
                    min={totalMinPayments}
                    showInvalidState={true}
                  />
                  {monthlyBudget < totalMinPayments && totalMinPayments > 0 && (
                    <p className="text-sm text-red-600 dark:text-red-400 mt-2">
                      Budget must be at least ${totalMinPayments.toFixed(0)} (total minimum payments)
                    </p>
                  )}
                </>
              ) : (
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                    Target Payoff Timeline
                  </label>
                  <div className="flex items-center gap-3">
                    <input
                      type="number"
                      value={targetMonths}
                      onChange={(e) => setTargetMonths(Number(e.target.value))}
                      min={1}
                      max={360}
                      className="flex-1 px-3 py-2 bg-white dark:bg-gray-900 border border-gray-300 dark:border-gray-600 rounded-lg focus:ring-2 focus:ring-fire-500 focus:border-transparent text-gray-900 dark:text-gray-100"
                    />
                    <span className="text-sm text-gray-600 dark:text-gray-400">months</span>
                  </div>
                  <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
                    {(targetMonths / 12).toFixed(1)} years
                  </p>
                </div>
              )}

              <div className="mt-4">
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                  Extra Monthly Payment: ${extraPayment}
                </label>
                <input
                  type="range"
                  min={0}
                  max={1000}
                  step={25}
                  value={extraPayment}
                  onChange={(e) => setExtraPayment(Number(e.target.value))}
                  className="w-full h-2 bg-gray-200 dark:bg-gray-700 rounded-lg appearance-none cursor-pointer accent-fire-600"
                />
                <div className="flex justify-between text-xs text-gray-500 dark:text-gray-400 mt-1">
                  <span>$0</span>
                  <span>$1,000</span>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Results */}
        <div className="lg:col-span-2 space-y-6">
          {!canCalculate ? (
            <Card>
              <CardContent className="text-center py-12">
                <div className="text-4xl mb-3">📝</div>
                <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-2">
                  Add Your Debts to Get Started
                </h3>
                <p className="text-sm text-gray-600 dark:text-gray-400">
                  Enter your debt information in the left panel to see your payoff strategy.
                </p>
              </CardContent>
            </Card>
          ) : (
            <>
              {/* Strategy Toggle */}
              <div className="flex justify-center">
                <div className="inline-flex bg-gray-100 dark:bg-gray-800 rounded-lg p-1">
                  <button
                    onClick={() => setStrategy('snowball')}
                    className={`px-6 py-2 rounded-md font-medium transition-colors ${
                      strategy === 'snowball'
                        ? 'bg-white dark:bg-gray-700 text-fire-700 dark:text-fire-400 shadow-sm'
                        : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-100'
                    }`}
                  >
                    💪 Snowball
                  </button>
                  <button
                    onClick={() => setStrategy('avalanche')}
                    className={`px-6 py-2 rounded-md font-medium transition-colors ${
                      strategy === 'avalanche'
                        ? 'bg-white dark:bg-gray-700 text-fire-700 dark:text-fire-400 shadow-sm'
                        : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-100'
                    }`}
                  >
                    🎯 Avalanche
                  </button>
                </div>
              </div>

              {/* Strategy Explanation Card */}
              <Card className="bg-gradient-to-br from-blue-50 to-indigo-50 dark:from-blue-900/20 dark:to-indigo-900/20 border-blue-200 dark:border-blue-800">
                <CardContent className="p-4">
                  {strategy === 'snowball' ? (
                    <div className="flex items-start gap-3">
                      <span className="text-2xl flex-shrink-0">💪</span>
                      <div>
                        <h3 className="font-semibold text-gray-900 dark:text-gray-100 mb-1">
                          Snowball Method
                        </h3>
                        <p className="text-sm text-gray-700 dark:text-gray-300">
                          Pay off debts from <strong>smallest to largest balance</strong>, regardless of interest rate. 
                          This method provides quick wins and builds momentum as you eliminate debts one by one.
                          Best for staying motivated on your debt-free journey.
                        </p>
                      </div>
                    </div>
                  ) : (
                    <div className="flex items-start gap-3">
                      <span className="text-2xl flex-shrink-0">🎯</span>
                      <div>
                        <h3 className="font-semibold text-gray-900 dark:text-gray-100 mb-1">
                          Avalanche Method
                        </h3>
                        <p className="text-sm text-gray-700 dark:text-gray-300">
                          Pay off debts from <strong>highest to lowest interest rate</strong>, regardless of balance. 
                          This method saves you the most money on interest over time and is mathematically optimal.
                          Best for maximizing long-term savings.
                        </p>
                      </div>
                    </div>
                  )}
                </CardContent>
              </Card>

              {/* Key Results */}
              <div className="grid sm:grid-cols-3 gap-4">
                <ResultCard
                  label="Debt-Free In"
                  value={`${results?.base.totalMonths || 0} months`}
                  subtext={`${((results?.base.totalMonths || 0) / 12).toFixed(1)} years`}
                  icon="🎯"
                />
                <ResultCard
                  label="Total Interest"
                  value={formatCurrency(results?.base.totalInterest || 0)}
                  subtext={`Plus ${formatCurrency(totalDebt)} principal`}
                  icon="💰"
                />
                <ResultCard
                  label="Monthly Payment"
                  value={formatCurrency(monthlyBudget + extraPayment)}
                  subtext={`${formatCurrency(monthlyBudget)} base + ${formatCurrency(extraPayment)} extra`}
                  icon="📅"
                />
              </div>

              {/* Extra Payment Impact */}
              {extraPaymentSavings && (
                <Card className="bg-gradient-to-br from-green-50 to-emerald-50 dark:from-green-900/20 dark:to-emerald-900/20 border-green-200 dark:border-green-800">
                  <CardHeader>
                    <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
                      💡 Extra Payment Impact
                    </h3>
                  </CardHeader>
                  <CardContent>
                    <div className="grid sm:grid-cols-3 gap-4">
                      <div>
                        <div className="text-sm text-gray-600 dark:text-gray-400 mb-1">Extra Payment</div>
                        <div className="text-2xl font-bold text-gray-900 dark:text-gray-100">
                          {formatCurrency(extraPayment)}/mo
                        </div>
                      </div>
                      <div>
                        <div className="text-sm text-gray-600 dark:text-gray-400 mb-1">Time Saved</div>
                        <div className="text-2xl font-bold text-green-600 dark:text-green-400">
                          {extraPaymentSavings.monthsSaved} months
                        </div>
                      </div>
                      <div>
                        <div className="text-sm text-gray-600 dark:text-gray-400 mb-1">Interest Saved</div>
                        <div className="text-2xl font-bold text-green-600 dark:text-green-400">
                          {formatCurrency(extraPaymentSavings.interestSaved)}
                        </div>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              )}

              {/* Payoff Order */}
              {results?.base.payoffOrder && results.base.payoffOrder.length > 0 && (
                <Card>
                  <CardHeader>
                    <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
                      Payoff Order
                    </h3>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-2">
                      {results.base.payoffOrder.map((debtName, index) => (
                        <div
                          key={index}
                          className="flex items-center gap-3 p-3 bg-gray-50 dark:bg-gray-800/50 rounded-lg"
                        >
                          <div className="flex-shrink-0 w-8 h-8 bg-fire-100 dark:bg-fire-900/30 text-fire-700 dark:text-fire-400 rounded-full flex items-center justify-center font-semibold text-sm">
                            {index + 1}
                          </div>
                          <div className="flex-1">
                            <div className="font-medium text-gray-900 dark:text-gray-100">
                              {debtName}
                            </div>
                            <div className="text-xs text-gray-500 dark:text-gray-400">
                              Paid off in month {results.base.debtMilestones.find(m => m.debtName === debtName)?.month}
                            </div>
                          </div>
                          <div className="text-green-600 dark:text-green-400">
                            <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                            </svg>
                          </div>
                        </div>
                      ))}
                    </div>
                  </CardContent>
                </Card>
              )}

              {/* Charts */}
              {results?.base && (
                <>
                  <Card>
                    <CardHeader>
                      <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
                        Debt Payoff Timeline
                      </h3>
                    </CardHeader>
                    <CardContent>
                      <DebtBalanceChart
                        data={results.base.projections}
                        milestones={results.base.debtMilestones}
                        comparisonData={results.withExtra?.projections}
                        height={350}
                      />
                    </CardContent>
                  </Card>

                  <Card>
                    <CardHeader>
                      <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
                        Payment Breakdown
                      </h3>
                    </CardHeader>
                    <CardContent>
                      <DebtBreakdownChart
                        data={results.base.projections}
                        height={350}
                      />
                      <div className="mt-4 pt-4 border-t border-gray-200 dark:border-gray-700">
                        <div className="grid grid-cols-2 gap-4 text-center">
                          <div>
                            <div className="text-sm text-gray-600 dark:text-gray-400 mb-1">
                              Total Principal
                            </div>
                            <div className="text-xl font-bold text-emerald-600 dark:text-emerald-400">
                              {formatCurrency(results.base.totalPrincipal)}
                            </div>
                          </div>
                          <div>
                            <div className="text-sm text-gray-600 dark:text-gray-400 mb-1">
                              Total Interest
                            </div>
                            <div className="text-xl font-bold text-red-600 dark:text-red-400">
                              {formatCurrency(results.base.totalInterest)}
                            </div>
                          </div>
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                </>
              )}

              {/* Strategy Comparison */}
              {comparisonResults && (
                <Card className="bg-gradient-to-br from-purple-50 to-pink-50 dark:from-purple-900/20 dark:to-pink-900/20 border-purple-200 dark:border-purple-800">
                  <CardHeader>
                    <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
                      📊 Snowball vs Avalanche Comparison
                    </h3>
                  </CardHeader>
                  <CardContent>
                    <div className="grid sm:grid-cols-2 gap-4">
                      <div className="p-4 bg-white/50 dark:bg-gray-800/50 rounded-lg">
                        <div className="flex items-center gap-2 mb-3">
                          <span className="text-xl">💪</span>
                          <h4 className="font-semibold text-gray-900 dark:text-gray-100">Snowball</h4>
                        </div>
                        <div className="space-y-2 text-sm">
                          <div className="flex justify-between">
                            <span className="text-gray-600 dark:text-gray-400">Payoff Time:</span>
                            <span className="font-medium text-gray-900 dark:text-gray-100">
                              {comparisonResults.snowball.totalMonths} months
                            </span>
                          </div>
                          <div className="flex justify-between">
                            <span className="text-gray-600 dark:text-gray-400">Total Interest:</span>
                            <span className="font-medium text-gray-900 dark:text-gray-100">
                              {formatCurrency(comparisonResults.snowball.totalInterest)}
                            </span>
                          </div>
                        </div>
                      </div>
                      
                      <div className="p-4 bg-white/50 dark:bg-gray-800/50 rounded-lg">
                        <div className="flex items-center gap-2 mb-3">
                          <span className="text-xl">🎯</span>
                          <h4 className="font-semibold text-gray-900 dark:text-gray-100">Avalanche</h4>
                        </div>
                        <div className="space-y-2 text-sm">
                          <div className="flex justify-between">
                            <span className="text-gray-600 dark:text-gray-400">Payoff Time:</span>
                            <span className="font-medium text-gray-900 dark:text-gray-100">
                              {comparisonResults.avalanche.totalMonths} months
                            </span>
                          </div>
                          <div className="flex justify-between">
                            <span className="text-gray-600 dark:text-gray-400">Total Interest:</span>
                            <span className="font-medium text-gray-900 dark:text-gray-100">
                              {formatCurrency(comparisonResults.avalanche.totalInterest)}
                            </span>
                          </div>
                        </div>
                      </div>
                    </div>
                    
                    <div className="mt-4 p-3 bg-green-50 dark:bg-green-900/30 rounded-lg">
                      <p className="text-sm text-gray-700 dark:text-gray-300">
                        <strong>Winner:</strong> {comparisonResults.avalanche.totalInterest < comparisonResults.snowball.totalInterest ? 'Avalanche' : 'Snowball'} saves{' '}
                        <strong className="text-green-600 dark:text-green-400">
                          {formatCurrency(Math.abs(comparisonResults.snowball.totalInterest - comparisonResults.avalanche.totalInterest))}
                        </strong>{' '}
                        in interest and finishes{' '}
                        <strong className="text-green-600 dark:text-green-400">
                          {Math.abs(comparisonResults.snowball.totalMonths - comparisonResults.avalanche.totalMonths)} months
                        </strong>{' '}
                        {comparisonResults.avalanche.totalMonths < comparisonResults.snowball.totalMonths ? 'faster' : 'slower'}.
                      </p>
                    </div>
                  </CardContent>
                </Card>
              )}
            </>
          )}
        </div>
      </div>

      <Disclaimer />
    </div>
  )
}
