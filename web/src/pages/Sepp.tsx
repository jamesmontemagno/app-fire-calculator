import { useId, useMemo } from 'react'
import { ChevronDown, TriangleAlert } from 'lucide-react'
import { useCalculatorParams } from '../hooks/useCalculatorParams'
import { formatCurrency } from '../utils/calculations'
import {
  SEPP_METHODS,
  SEPP_METHOD_LABELS,
  calculateSepp,
  isSeppMethod,
  seppResultFor,
  validateSeppInputs,
  type SeppInputs,
} from '../utils/sepp'
import { exportToExcel, prepareInputsForExport, prepareResultsForExport } from '../utils/excelExport'
import { CurrencyInput, DateInput, InputGroup, PercentageInput } from '../components/inputs'
import { AdvancedDetails, CalculatorFooter, Card, CardContent, CardHeader, ResultCard, Tooltip } from '../components/ui'
import { BalanceProjectionChart } from '../components/charts'
import SEO from '../components/SEO'
import { calculatorSEO } from '../config/seo'

function formatLongDate(isoDate: string): string {
  const [year, month, day] = isoDate.split('-').map(Number)
  return new Date(year, month - 1, day).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })
}

export default function Sepp() {
  const {
    params, setParam, setParamDebounced, resetParams, saveParams, loadParams, copyUrl,
    hasCustomParams, hasUnsavedChanges, hasSavedParams, savedAt,
  } = useCalculatorParams()
  const methodId = useId()

  const inputs = useMemo((): SeppInputs => ({
    accountBalance: params.seppBalance,
    expectedReturn: params.expectedReturn,
    birthDate: params.seppBirthDate,
    firstPaymentDate: params.seppFirstPaymentDate,
    interestRate: params.seppInterestRate,
    maximumInterestRate: params.seppMaxInterestRate,
    annuityFactor: params.seppAnnuityFactor > 0 ? params.seppAnnuityFactor : null,
    method: params.seppMethod,
  }), [params])
  const problem = useMemo(() => validateSeppInputs(inputs), [inputs])
  const results = useMemo(() => (problem ? null : calculateSepp(inputs)), [inputs, problem])
  const selected = results ? seppResultFor(results, inputs.method) : null

  const formatPayment = (payment: number | null) => (payment === null ? 'Enter actuarial factor' : formatCurrency(payment))

  const handleExport = () => {
    if (!results || !selected) return
    const { values: exportInputs, formats: inputFormats } = prepareInputsForExport({
      accountBalance: inputs.accountBalance, expectedReturn: inputs.expectedReturn,
      birthDate: inputs.birthDate, firstPaymentDate: inputs.firstPaymentDate,
      interestRate: inputs.interestRate, maximumInterestRate: inputs.maximumInterestRate,
      annuityFactor: inputs.annuityFactor, method: SEPP_METHOD_LABELS[inputs.method],
    })
    const { values: resultValues, formats: resultFormats } = prepareResultsForExport({
      startingAge: results.startingAge, lifeExpectancyFactor: results.lifeExpectancyFactor,
      requiredEndDate: results.requiredEndDate, requiredYears: results.requiredYears,
      annualPayment: selected.annualPayment, monthlyPayment: selected.monthlyPayment,
      rmdAnnualPayment: results.rmd.annualPayment, amortizationAnnualPayment: results.amortization.annualPayment,
      annuitizationAnnualPayment: results.annuitization.annualPayment,
    })
    exportToExcel({
      calculatorName: '72(t) SEPP', inputs: exportInputs, results: resultValues, projections: selected.projections,
      inputFormats, resultFormats,
    })
  }

  return (
    <>
      <SEO {...calculatorSEO.sepp} />
      <div className="space-y-8">
        <header>
          <h1 className="text-2xl font-bold text-content sm:text-3xl">72(t) / SEPP Calculator</h1>
          <p className="mt-1 text-content-muted">Estimate substantially equal periodic payments that may avoid the additional 10% tax on early retirement-plan distributions.</p>
        </header>

        <section aria-labelledby="sepp-eligibility-heading" className="rounded-container border border-warning/40 bg-warning-subtle p-4 text-sm">
          <h2 id="sepp-eligibility-heading" className="flex items-center gap-2 font-semibold text-content">
            <TriangleAlert className="h-4 w-4 shrink-0 text-warning" aria-hidden="true" strokeWidth={1.5} />
            Verify eligibility before relying on this estimate
          </h2>
          <p className="mt-2 text-content-muted">A SEPP series is a strict tax arrangement, not simply a withdrawal strategy. Employer plans may not permit early distributions; inherited IRAs generally do not need this exception; governmental 457(b) plans generally are not subject to the additional 10% tax; and a SIMPLE IRA within its first two years has different penalty rules. Roth distributions also require separate ordering analysis.</p>
        </section>

        <section aria-labelledby="sepp-account-heading">
          <Card>
            <CardHeader>
              <h2 id="sepp-account-heading" className="text-lg font-semibold text-content">Start with the account and dates</h2>
              <p className="mt-1 text-sm text-content-muted">Use one account dedicated to this series; do not combine unrelated accounts. The calculator uses the IRS Single Life table for the age you reach on the first payment date. These values are saved with this calculator.</p>
            </CardHeader>
            <CardContent className="field-grid grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
              <CurrencyInput label="Account balance" value={params.seppBalance} onChange={value => setParam('seppBalance', value)} tooltip="Balance dedicated to this SEPP series." />
              <DateInput label="Birth date" value={params.seppBirthDate} onChange={value => setParam('seppBirthDate', value)} tooltip="Determines your age on the first payment date and when you reach 59½." />
              <DateInput label="First payment date" value={params.seppFirstPaymentDate} onChange={value => setParam('seppFirstPaymentDate', value)} tooltip="The series must begin before age 59½ and continue for at least five years." />
              <div>
                <label htmlFor={methodId} className="mb-1.5 flex items-center gap-1.5 text-sm font-medium text-content-muted">
                  Calculation method
                  <Tooltip content="The RMD method recalculates each year; the two fixed methods keep the first-year payment for the whole series." />
                </label>
                <select
                  id={methodId}
                  value={params.seppMethod}
                  onChange={event => { if (isSeppMethod(event.target.value)) setParam('seppMethod', event.target.value) }}
                  className="w-full px-3 py-2.5 bg-surface-raised border border-border-strong rounded-control text-content focus:ring-2 focus-visible:ring-ring focus-visible:border-accent"
                >
                  {SEPP_METHODS.map(method => <option key={method} value={method}>{SEPP_METHOD_LABELS[method]}</option>)}
                </select>
              </div>
              <PercentageInput label="Chosen interest rate" value={params.seppInterestRate} onChange={value => setParam('seppInterestRate', value)} onSliderChange={value => setParamDebounced('seppInterestRate', value)} tooltip="Used for the fixed methods. It cannot exceed the permitted maximum." min={0} max={0.2} step={0.0005} decimals={2} />
              <PercentageInput label="Maximum permitted rate" value={params.seppMaxInterestRate} onChange={value => setParam('seppMaxInterestRate', value)} onSliderChange={value => setParamDebounced('seppMaxInterestRate', value)} tooltip="Enter the greater of 5% or 120% of the applicable federal mid-term rate for either of the two months before payments begin." min={0.05} max={0.2} step={0.0005} decimals={2} />
            </CardContent>
          </Card>
        </section>

        {problem && (
          <p role="alert" className="rounded-container border border-warning/40 bg-warning-subtle p-4 text-sm text-content">{problem}</p>
        )}

        {results && selected && (
          <section aria-labelledby="sepp-outlook-heading" className="space-y-4">
            <div>
              <h2 id="sepp-outlook-heading" className="text-xl font-semibold text-content">Estimated payment</h2>
              <p className="mt-1 text-sm text-content-muted">{SEPP_METHOD_LABELS[inputs.method]} for a {results.startingAge}-year-old using a Single Life factor of {results.lifeExpectancyFactor.toFixed(1)}.</p>
            </div>
            <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
              <ResultCard label="Annual payment" value={formatPayment(selected.annualPayment)} highlight subtext="First-year amount" />
              <ResultCard label="Monthly equivalent" value={selected.monthlyPayment === null ? 'Factor required' : formatCurrency(selected.monthlyPayment)} subtext="Annual payment ÷ 12" />
              <ResultCard label="Continue through" value={formatLongDate(results.requiredEndDate)} subtext={`${results.requiredYears} annual payment years`} />
              <ResultCard label="Starting age" value={`Age ${results.startingAge}`} subtext={`Single Life factor ${results.lifeExpectancyFactor.toFixed(1)}`} />
            </div>
          </section>
        )}

        <AdvancedDetails description="The annuity factor is only needed for fixed annuitization; expected return only shapes the illustrative projection.">
          <InputGroup label="Actuarial annuity factor" value={params.seppAnnuityFactor} onChange={value => setParam('seppAnnuityFactor', value)} min={0} step={0.001} helperText="Leave at 0 unless a qualified professional or compliant actuarial tool has supplied the exact factor for the IRS mortality table and your chosen rate. The app does not approximate it because an approximation is not an IRS safe-harbor calculation." tooltip="Required only for fixed annuitization." />
          <PercentageInput label="Expected account return" value={params.expectedReturn} onChange={value => setParam('expectedReturn', value)} onSliderChange={value => setParamDebounced('expectedReturn', value)} tooltip="Used only for the illustrative balance projection, not the IRS payment formula." min={0} max={0.15} />
        </AdvancedDetails>

        {results && selected && (
          <>
            <section aria-labelledby="sepp-methods-heading">
              <Card>
                <CardHeader>
                  <h2 id="sepp-methods-heading" className="text-lg font-semibold text-content">Method comparison</h2>
                  <p className="mt-1 text-sm text-content-muted">RMD payments must be recalculated annually. Fixed-method payments generally remain unchanged unless a permitted one-time switch to RMD is made.</p>
                </CardHeader>
                <CardContent>
                  <dl className="grid gap-4 sm:grid-cols-3">
                    {[
                      ['RMD first year', results.rmd.annualPayment],
                      ['Fixed amortization', results.amortization.annualPayment],
                      ['Fixed annuitization', results.annuitization.annualPayment],
                    ].map(([label, payment]) => (
                      <div key={label as string} className="rounded-container border border-border-subtle bg-surface-sunken p-4">
                        <dt className="text-sm text-content-muted">{label}</dt>
                        <dd className="tabular mt-1 text-lg font-semibold text-content">{formatPayment(payment as number | null)}</dd>
                      </div>
                    ))}
                  </dl>
                </CardContent>
              </Card>
            </section>

            <section aria-labelledby="sepp-chart-heading">
              <Card>
                <CardHeader>
                  <h2 id="sepp-chart-heading" className="text-lg font-semibold text-content">Illustrative account projection</h2>
                  <p className="mt-1 text-sm text-content-muted">Balance path for {SEPP_METHOD_LABELS[inputs.method].toLowerCase()} through the required commitment period, assuming {(inputs.expectedReturn * 100).toFixed(1)}% annual growth.</p>
                </CardHeader>
                <CardContent>
                  {selected.projections.length === 0 ? (
                    <p className="text-sm text-content-muted">Enter an actuarial annuity factor to project the fixed annuitization method.</p>
                  ) : (
                    <>
                      <BalanceProjectionChart data={selected.projections} xKey="calendarYear" xLabel="Year" series={[
                        { key: 'startingBalance', name: 'Starting balance', tone: 'secondary', dashed: true },
                        { key: 'endingBalance', name: 'Ending balance', tone: 'primary' },
                      ]} />
                      <div className="mt-6 overflow-x-auto">
                        <table className="w-full text-sm">
                          <thead>
                            <tr className="border-b border-border-subtle">
                              {['Year', 'Age', 'Starting balance', 'Payment', 'Ending balance'].map(label => <th key={label} className="whitespace-nowrap px-3 py-3 text-left font-semibold text-content">{label}</th>)}
                            </tr>
                          </thead>
                          <tbody>
                            {selected.projections.map(row => (
                              <tr key={row.yearNumber} className="border-b border-border-subtle">
                                <td className="px-3 py-3 text-content">{row.calendarYear}</td>
                                <td className="px-3 py-3 text-content-muted">{row.age}</td>
                                <td className="px-3 py-3 text-content-muted">{formatCurrency(row.startingBalance)}</td>
                                <td className="px-3 py-3 font-medium text-content">{formatCurrency(row.annualPayment)}</td>
                                <td className="px-3 py-3 text-content-muted">{formatCurrency(row.endingBalance)}</td>
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </div>
                    </>
                  )}
                </CardContent>
              </Card>
            </section>
          </>
        )}

        <section aria-labelledby="sepp-modification-heading" className="rounded-container border border-warning/40 bg-warning-subtle p-4 text-sm">
          <h2 id="sepp-modification-heading" className="flex items-center gap-2 font-semibold text-content">
            <TriangleAlert className="h-4 w-4 shrink-0 text-warning" aria-hidden="true" strokeWidth={1.5} />
            A modification can trigger retroactive tax and interest
          </h2>
          <p className="mt-2 text-content-muted">Payments generally must continue until the later of five years after the first payment or age 59½. Adding money, transferring funds, changing payments outside permitted rules, or otherwise modifying the account can break the series. Keep professional calculations and account records.</p>
        </section>

        <details className="group border-y border-border-subtle">
          <summary className="flex cursor-pointer list-none items-center justify-between gap-4 py-4 font-semibold text-content marker:hidden focus:outline-none focus-visible:ring-2 focus-visible:ring-ring">
            How this is calculated
            <ChevronDown className="h-5 w-5 shrink-0 text-content-subtle transition-transform duration-200 motion-reduce:transition-none group-open:rotate-180" aria-hidden="true" strokeWidth={1.5} />
          </summary>
          <div className="space-y-3 border-t border-border-subtle py-5 text-sm text-content-muted">
            <p><strong className="text-content">RMD method:</strong> prior-year account balance ÷ the IRS Single Life factor for that year's attained age; it is recalculated annually.</p>
            <p><strong className="text-content">Fixed amortization:</strong> payment = (rate × balance) ÷ (1 − (1 + rate)<sup>−life expectancy factor</sup>).</p>
            <p><strong className="text-content">Fixed annuitization:</strong> balance ÷ a compliant actuarial annuity factor. The app requires that factor rather than approximating it.</p>
            <p>The interest-rate ceiling is the greater of 5% or 120% of the applicable federal mid-term rate for either of the two months before payments begin. Verify the entered ceiling for your start month.</p>
          </div>
        </details>

        <CalculatorFooter onExport={handleExport} exportDisabled={!results} onReset={resetParams} onSave={saveParams} onLoad={loadParams} onCopy={copyUrl} hasCustomParams={hasCustomParams} hasUnsavedChanges={hasUnsavedChanges} hasSavedParams={hasSavedParams} savedAt={savedAt} />
      </div>
    </>
  )
}
