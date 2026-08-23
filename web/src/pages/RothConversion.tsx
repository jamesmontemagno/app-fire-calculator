import { useMemo } from 'react'
import { ChevronDown, TriangleAlert } from 'lucide-react'
import { useCalculatorParams } from '../hooks/useCalculatorParams'
import { formatCurrency } from '../utils/calculations'
import {
  ROTH_CONVERSION_WAITING_PERIOD_YEARS,
  ROTH_PENALTY_FREE_AGE,
  calculateRothConversion,
  validateRothConversionInputs,
  type RothConversionInputs,
} from '../utils/rothConversion'
import { exportToExcel, prepareInputsForExport, prepareResultsForExport } from '../utils/excelExport'
import { AgeInput, CurrencyInput, InputGroup, PercentageInput } from '../components/inputs'
import { AdvancedDetails, CalculatorFooter, Card, CardContent, CardHeader, ResultCard } from '../components/ui'
import { BalanceProjectionChart } from '../components/charts'
import SEO from '../components/SEO'
import { calculatorSEO } from '../config/seo'

export default function RothConversion() {
  const {
    params, setParam, setParamDebounced, resetParams, saveParams, loadParams, copyUrl,
    hasCustomParams, hasUnsavedChanges, hasSavedParams, savedAt,
  } = useCalculatorParams()

  const inputs = useMemo((): RothConversionInputs => ({
    currentAge: params.currentAge,
    startYear: params.rothStartYear,
    traditionalBalance: params.rothTraditionalBalance,
    rothBalance: params.rothBalance,
    annualConversion: params.rothAnnualConversion,
    conversionYears: params.rothConversionYears,
    expectedReturn: params.expectedReturn,
    estimatedTaxRate: params.rothTaxRate,
  }), [params])
  const problem = useMemo(() => validateRothConversionInputs(inputs), [inputs])
  const results = useMemo(() => (problem ? null : calculateRothConversion(inputs)), [inputs, problem])

  const handleExport = () => {
    if (!results) return
    const { values: exportInputs, formats: inputFormats } = prepareInputsForExport({
      currentAge: inputs.currentAge, startYear: String(inputs.startYear),
      traditionalBalance: inputs.traditionalBalance, rothBalance: inputs.rothBalance,
      annualConversion: inputs.annualConversion, conversionYears: inputs.conversionYears,
      expectedReturn: inputs.expectedReturn, estimatedTaxRate: inputs.estimatedTaxRate,
    })
    const { values: resultValues, formats: resultFormats } = prepareResultsForExport({
      totalConverted: results.totalConverted, totalEstimatedTaxes: results.totalEstimatedTaxes,
      firstAccessibleYear: results.firstAccessibleYear === null ? null : String(results.firstAccessibleYear),
      endingTraditionalBalance: results.endingTraditionalBalance, endingRothBalance: results.endingRothBalance,
    })
    exportToExcel({
      calculatorName: 'Roth Conversion Strategy', inputs: exportInputs, results: resultValues, projections: results.projections,
      inputFormats, resultFormats,
    })
  }

  return (
    <>
      <SEO {...calculatorSEO['roth-conversion']} />
      <div className="space-y-8">
        <header>
          <h1 className="text-2xl font-bold text-content sm:text-3xl">Roth Conversion Strategy Calculator</h1>
          <p className="mt-1 text-content-muted">Model annual pre-tax retirement account conversions and the five-tax-year ladder for converted principal.</p>
        </header>

        <section aria-labelledby="roth-tax-heading" className="rounded-container border border-warning/40 bg-warning-subtle p-4 text-sm">
          <h2 id="roth-tax-heading" className="flex items-center gap-2 font-semibold text-content">
            <TriangleAlert className="h-4 w-4 shrink-0 text-warning" aria-hidden="true" strokeWidth={1.5} />
            Tax rules can materially change the result
          </h2>
          <p className="mt-2 text-content-muted">This estimate applies one tax rate only to the conversion amount. It does not calculate tax brackets, deductions, credits, state tax, Medicare premiums, ACA subsidies, required minimum distributions, or Roth distribution ordering. Pay conversion taxes from outside funds when possible and verify the strategy with a qualified tax professional.</p>
        </section>

        <section aria-labelledby="roth-plan-heading">
          <Card>
            <CardHeader>
              <h2 id="roth-plan-heading" className="text-lg font-semibold text-content">Start with your conversion plan</h2>
              <p className="mt-1 text-sm text-content-muted">Each year's conversion moves money from the traditional balance to the Roth balance and is limited to whatever remains. These values are saved with this calculator.</p>
            </CardHeader>
            <CardContent className="field-grid grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
              <AgeInput label="Current age" value={params.currentAge} onChange={value => setParam('currentAge', value)} onSliderChange={value => setParamDebounced('currentAge', value)} tooltip="Your age in the first conversion year." min={18} max={100} showSlider />
              <InputGroup label="First conversion year" value={params.rothStartYear} onChange={value => setParam('rothStartYear', value)} min={1900} max={2200} tooltip="Calendar year of the first planned conversion." />
              <CurrencyInput label="Traditional balance" value={params.rothTraditionalBalance} onChange={value => setParam('rothTraditionalBalance', value)} tooltip="Pre-tax retirement balance available for conversion." />
              <CurrencyInput label="Existing Roth balance" value={params.rothBalance} onChange={value => setParam('rothBalance', value)} tooltip="Current Roth balance before this strategy begins." />
              <CurrencyInput label="Annual conversion" value={params.rothAnnualConversion} onChange={value => setParam('rothAnnualConversion', value)} tooltip="Target gross amount converted each year. The final conversion is limited to the remaining traditional balance." />
              <InputGroup label="Conversion years" value={params.rothConversionYears} onChange={value => setParam('rothConversionYears', value)} onSliderChange={value => setParamDebounced('rothConversionYears', value)} min={1} max={50} showSlider suffix="years" tooltip="Number of annual conversions to model." />
            </CardContent>
          </Card>
        </section>

        {problem && (
          <p role="alert" className="rounded-container border border-warning/40 bg-warning-subtle p-4 text-sm text-content">{problem}</p>
        )}

        {results && (
          <section aria-labelledby="roth-outlook-heading" className="space-y-4">
            <div>
              <h2 id="roth-outlook-heading" className="text-xl font-semibold text-content">Strategy outlook</h2>
              <p className="mt-1 text-sm text-content-muted">{formatCurrency(results.totalConverted)} is planned for conversion over {inputs.conversionYears} years. Each year's converted principal is shown as accessible after {ROTH_CONVERSION_WAITING_PERIOD_YEARS} tax years.</p>
            </div>
            <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
              <ResultCard label="Total converted" value={results.totalConverted} format="currency" highlight subtext={`Over ${inputs.conversionYears} years`} />
              <ResultCard label="Estimated conversion taxes" value={results.totalEstimatedTaxes} format="currency" subtext={`At ${(inputs.estimatedTaxRate * 100).toFixed(1)}% on each conversion`} />
              <ResultCard label="First principal accessible" value={results.firstAccessibleYear === null ? 'Not available' : String(results.firstAccessibleYear)} subtext="Without the additional 10% tax" />
              <ResultCard label="Ending traditional balance" value={results.endingTraditionalBalance} format="currency" subtext={`At the end of ${results.projections.at(-1)?.calendarYear}`} />
              <ResultCard label="Ending Roth balance" value={results.endingRothBalance} format="currency" subtext="Conversions plus growth" />
              <ResultCard label="Accessible converted principal" value={results.projections.at(-1)?.cumulativeAccessiblePrincipal ?? 0} format="currency" subtext="By the end of the plan" />
            </div>
          </section>
        )}

        <AdvancedDetails description="Growth and the tax rate are long-term assumptions; the conversion plan above is the main decision.">
          <PercentageInput label="Expected return" value={params.expectedReturn} onChange={value => setParam('expectedReturn', value)} onSliderChange={value => setParamDebounced('expectedReturn', value)} tooltip="Annual growth applied to both balances before each year's conversion." min={0} max={0.15} />
          <PercentageInput label="Estimated tax rate" value={params.rothTaxRate} onChange={value => setParam('rothTaxRate', value)} onSliderChange={value => setParamDebounced('rothTaxRate', value)} tooltip="Estimated combined marginal rate applied to each conversion." min={0} max={0.6} />
        </AdvancedDetails>

        {results && (
          <section aria-labelledby="roth-ladder-heading">
            <Card>
              <CardHeader>
                <h2 id="roth-ladder-heading" className="text-lg font-semibold text-content">Account balances and the conversion ladder</h2>
                <p className="mt-1 text-sm text-content-muted">Traditional and Roth balances from {inputs.startYear} through {results.projections.at(-1)?.calendarYear}, with the year each conversion's principal becomes accessible.</p>
              </CardHeader>
              <CardContent>
                <BalanceProjectionChart data={results.projections} xKey="calendarYear" xLabel="Year" series={[
                  { key: 'endingTraditionalBalance', name: 'Traditional balance', tone: 'secondary' },
                  { key: 'endingRothBalance', name: 'Roth balance', tone: 'primary' },
                  { key: 'cumulativeAccessiblePrincipal', name: 'Accessible converted principal', tone: 'positive', dashed: true },
                ]} />
                <div className="mt-6 overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b border-border-subtle">
                        {['Year', 'Age', 'Conversion', 'Est. taxes', 'Traditional', 'Roth', 'Newly accessible', 'Accessible total'].map(label => <th key={label} className="whitespace-nowrap px-3 py-3 text-left font-semibold text-content">{label}</th>)}
                      </tr>
                    </thead>
                    <tbody>
                      {results.projections.map(row => (
                        <tr key={row.yearNumber} className="border-b border-border-subtle">
                          <td className="px-3 py-3 text-content">{row.calendarYear}</td>
                          <td className="px-3 py-3 text-content-muted">{row.age}</td>
                          <td className="px-3 py-3 font-medium text-content">{row.conversion > 0 ? formatCurrency(row.conversion) : '—'}</td>
                          <td className="px-3 py-3 text-content-muted">{row.estimatedTaxes > 0 ? formatCurrency(row.estimatedTaxes) : '—'}</td>
                          <td className="px-3 py-3 text-content-muted">{formatCurrency(row.endingTraditionalBalance)}</td>
                          <td className="px-3 py-3 text-content-muted">{formatCurrency(row.endingRothBalance)}</td>
                          <td className="px-3 py-3 text-content-muted">{row.newlyAccessiblePrincipal > 0 ? formatCurrency(row.newlyAccessiblePrincipal) : '—'}</td>
                          <td className="px-3 py-3 font-medium text-content">{formatCurrency(row.cumulativeAccessiblePrincipal)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </CardContent>
            </Card>
          </section>
        )}

        <details className="group border-y border-border-subtle">
          <summary className="flex cursor-pointer list-none items-center justify-between gap-4 py-4 font-semibold text-content marker:hidden focus:outline-none focus-visible:ring-2 focus-visible:ring-ring">
            How this is calculated
            <ChevronDown className="h-5 w-5 shrink-0 text-content-subtle transition-transform duration-200 motion-reduce:transition-none group-open:rotate-180" aria-hidden="true" strokeWidth={1.5} />
          </summary>
          <div className="space-y-3 border-t border-border-subtle py-5 text-sm text-content-muted">
            <p>At the start of each plan year, both account balances grow by the expected return. The planned conversion is then moved from the traditional balance to the Roth balance and is limited to the traditional balance available that year.</p>
            <p>Estimated conversion tax = conversion amount × estimated tax rate. Taxes are assumed to be paid from funds outside these accounts.</p>
            <p>Before age 59½, each year's converted principal becomes accessible after {ROTH_CONVERSION_WAITING_PERIOD_YEARS} tax years. For example, a {inputs.startYear} conversion is shown as accessible in {inputs.startYear + ROTH_CONVERSION_WAITING_PERIOD_YEARS}. At age {ROTH_PENALTY_FREE_AGE} in this annual model, converted principal is shown as accessible without that waiting period. Investment earnings are not counted as accessible converted principal.</p>
          </div>
        </details>

        <CalculatorFooter onExport={handleExport} exportDisabled={!results} onReset={resetParams} onSave={saveParams} onLoad={loadParams} onCopy={copyUrl} hasCustomParams={hasCustomParams} hasUnsavedChanges={hasUnsavedChanges} hasSavedParams={hasSavedParams} savedAt={savedAt} />
      </div>
    </>
  )
}
