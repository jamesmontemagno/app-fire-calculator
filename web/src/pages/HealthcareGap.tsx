import { useMemo } from 'react'
import { useCalculatorParams } from '../hooks/useCalculatorParams'
import { MEDICARE_AGE, calculateHealthcareGap, formatCurrency } from '../utils/calculations'
import { exportToExcel, prepareInputsForExport, prepareResultsForExport } from '../utils/excelExport'
import { AgeInput, CurrencyInput, CurrencyPeriodProvider, PercentageInput, PeriodToggle } from '../components/inputs'
import { AdvancedDetails, CalculatorFooter, Card, CardContent, CardHeader, ResultCard } from '../components/ui'
import SEO from '../components/SEO'
import { calculatorSEO } from '../config/seo'

export default function HealthcareGap() {
  const {
    params, setParam, setParamDebounced, resetParams, saveParams, loadParams, copyUrl,
    hasCustomParams, hasUnsavedChanges, hasSavedParams, savedAt,
  } = useCalculatorParams()
  const results = useMemo(() => calculateHealthcareGap(
    params.currentAge, params.retirementAge, params.healthcareMonthlyPremium,
    params.healthcareAnnualDeductible, params.healthcareAnnualOutOfPocket, params.inflationRate,
  ), [params])

  const handleExport = () => {
    const { values: inputs, formats: inputFormats } = prepareInputsForExport({
      currentAge: params.currentAge, earlyRetirementAge: params.retirementAge, medicareAge: MEDICARE_AGE,
      monthlyPremium: params.healthcareMonthlyPremium, annualDeductible: params.healthcareAnnualDeductible,
      annualOutOfPocket: params.healthcareAnnualOutOfPocket, inflationRate: params.inflationRate,
    })
    const { values: resultValues, formats: resultFormats } = prepareResultsForExport(results)
    exportToExcel({
      calculatorName: 'Healthcare Gap', inputs, results: resultValues, projections: results.yearlyBreakdown,
      inputFormats, resultFormats,
      resultFormulas: {
        gapYears: 'MAX(0,{medicareAge}-{earlyRetirementAge})',
        annualCost: '({monthlyPremium}*12)+{annualDeductible}+{annualOutOfPocket}',
      },
    })
  }

  return (
    <CurrencyPeriodProvider period={params.currencyPeriod} onChange={value => setParam('currencyPeriod', value)}>
      <SEO {...calculatorSEO.healthcare} />
      <div className="space-y-8">
        <header>
          <h1 className="text-2xl font-bold text-gray-900 sm:text-3xl dark:text-gray-100">Healthcare Gap Calculator</h1>
          <p className="mt-1 text-gray-600 dark:text-gray-400">Estimate healthcare costs between early retirement and Medicare eligibility at age {MEDICARE_AGE}.</p>
        </header>

        <section aria-labelledby="healthcare-plan-heading">
          <Card>
            <CardHeader>
              <h2 id="healthcare-plan-heading" className="text-lg font-semibold text-gray-900 dark:text-gray-100">Start with your coverage plan</h2>
              <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">Enter the costs you expect to pay before Medicare begins. Every amount below uses the same period, so switch them all to monthly or annual with one control. These values are saved with this calculator.</p>
              <PeriodToggle className="mt-3" />
            </CardHeader>
            <CardContent className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
              <AgeInput label="Current age" value={params.currentAge} onChange={value => setParam('currentAge', value)} onSliderChange={value => setParamDebounced('currentAge', value)} tooltip="Used to label the cost timeline." min={18} max={64} showSlider />
              <AgeInput label="Early retirement age" value={params.retirementAge} onChange={value => setParam('retirementAge', value)} onSliderChange={value => setParamDebounced('retirementAge', value)} tooltip="When employer-sponsored coverage ends." min={params.currentAge} max={64} showSlider />
              <CurrencyInput label="Health insurance premium" value={params.healthcareMonthlyPremium} onChange={value => setParam('healthcareMonthlyPremium', value)} tooltip="Expected health insurance premium." max={3000} periodic storedPeriod="monthly" />
              <CurrencyInput label="Deductible" value={params.healthcareAnnualDeductible} onChange={value => setParam('healthcareAnnualDeductible', value)} tooltip="Expected deductible before the plan pays." max={20000} periodic />
              <CurrencyInput label="Out-of-pocket costs" value={params.healthcareAnnualOutOfPocket} onChange={value => setParam('healthcareAnnualOutOfPocket', value)} tooltip="Expected copays, coinsurance, and other medical costs." max={20000} periodic />
            </CardContent>
          </Card>
        </section>

        <section aria-labelledby="healthcare-outlook-heading" className="space-y-4">
          <div>
            <h2 id="healthcare-outlook-heading" className="text-xl font-semibold text-gray-900 dark:text-gray-100">Your healthcare gap</h2>
            <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">This plan covers {results.gapYears} years between retirement and Medicare eligibility.</p>
          </div>
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
            <ResultCard label="Total healthcare gap cost" value={results.totalCost} format="currency" highlight subtext={`Across ${results.gapYears} years`} />
            <ResultCard label="First-year cost" value={results.annualCost} format="currency" subtext="Before inflation" />
            <ResultCard label="Average annual cost" value={results.avgAnnualCost} format="currency" subtext="Inflation-adjusted over the gap" />
          </div>
        </section>

        <AdvancedDetails description="Inflation is a long-term assumption; the coverage inputs above are the main decisions.">
          <PercentageInput label="Healthcare inflation rate" value={params.inflationRate} onChange={value => setParam('inflationRate', value)} onSliderChange={value => setParamDebounced('inflationRate', value)} tooltip="Expected annual increase in premiums, deductibles, and out-of-pocket costs." min={0} max={0.15} />
        </AdvancedDetails>

        <section aria-labelledby="healthcare-detail-heading">
          <Card>
            <CardHeader>
              <h2 id="healthcare-detail-heading" className="text-lg font-semibold text-gray-900 dark:text-gray-100">Cost timeline</h2>
              <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">A year-by-year estimate of premium, deductible, and out-of-pocket costs.</p>
            </CardHeader>
            <CardContent>
              {results.yearlyBreakdown.length === 0 ? (
                <p className="text-sm text-gray-600 dark:text-gray-400">Retiring at or after Medicare eligibility leaves no coverage gap in this estimate.</p>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b border-gray-200 dark:border-gray-700">
                        {['Age', 'Year', 'Premium', 'Deductible', 'Out-of-pocket', 'Total'].map(label => <th key={label} className="whitespace-nowrap px-3 py-3 text-left font-semibold text-gray-900 dark:text-gray-100">{label}</th>)}
                      </tr>
                    </thead>
                    <tbody>
                      {results.yearlyBreakdown.map(row => (
                        <tr key={row.age} className="border-b border-gray-100 dark:border-gray-800">
                          <td className="px-3 py-3 text-gray-900 dark:text-gray-100">{row.age}</td>
                          <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{row.year}</td>
                          <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{formatCurrency(row.premium)}</td>
                          <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{formatCurrency(row.deductible)}</td>
                          <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{formatCurrency(row.outOfPocket)}</td>
                          <td className="px-3 py-3 font-medium text-gray-900 dark:text-gray-100">{formatCurrency(row.cost)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </CardContent>
          </Card>
        </section>

        <p className="max-w-3xl text-sm text-gray-600 dark:text-gray-400">Actual insurance options, subsidies, medical needs, and eligibility rules vary by location and household. Use this as a planning estimate, not a coverage quote.</p>
        <CalculatorFooter onExport={handleExport} onReset={resetParams} onSave={saveParams} onLoad={loadParams} onCopy={copyUrl} hasCustomParams={hasCustomParams} hasUnsavedChanges={hasUnsavedChanges} hasSavedParams={hasSavedParams} savedAt={savedAt} />
      </div>
    </CurrencyPeriodProvider>
  )
}
