import ExcelJS from 'exceljs'

/**
 * Export calculator data to Excel spreadsheet
 * Creates a workbook with multiple sheets for inputs, results, and projections
 */

/**
 * How a value is rendered in its cell.
 *
 * `'text'` is a positive declaration that a field is deliberately non-numeric — a string or a
 * boolean — and must carry no number format. It is not the same as a field having no entry here,
 * which means nobody has declared it yet.
 */
export type ExportFormat = 'currency' | 'percent' | 'number' | 'age' | 'years' | 'text'

interface ExportData {
  calculatorName: string
  inputs: Record<string, any>
  results: Record<string, any>
  projections?: Array<Record<string, any>>
  additionalSheets?: Array<{
    name: string
    data: Array<Record<string, any>>
  }>
  // New: Support for formulas in results
  resultFormulas?: Record<string, string>
  // New: Support for formatting hints
  inputFormats?: Record<string, ExportFormat>
  resultFormats?: Record<string, ExportFormat>
}

/** Written in place of Infinity/NaN scalars so an exported workbook never contains "$∞". */
const UNREACHABLE_EXPORT_VALUE = 'Not reachable'

/**
 * Explicit headers for projection columns whose auto-derived name hides which dollars they are in.
 *
 * Contributions escalate with inflation by default, so the series is nominal and rises year over
 * year; "Contributions" alone reads as a constant. Only the display label is overridden — the keys
 * are what the formula builder below indexes on, so they must stay untouched.
 */
const PROJECTION_HEADER_LABELS: Record<string, string> = {
  portfolio: 'Portfolio (future $)',
  inflationAdjusted: 'Portfolio (today’s $)',
  contributions: 'Contribution that year (future $)',
  totalContributions: 'Contributions to date (future $)',
}

/**
 * Format a value for Excel export
 * Converts special types to strings/numbers appropriately
 */
function formatValue(value: any): any {
  if (value === null || value === undefined) return ''
  
  // Keep numbers as numbers for Excel
  if (typeof value === 'number') {
    return value
  }
  
  if (typeof value === 'boolean') return value
  if (typeof value === 'string') return value
  
  // For objects/arrays, convert to JSON string
  if (typeof value === 'object') {
    return JSON.stringify(value)
  }
  
  return String(value)
}

/**
 * Get Excel format string for a given format type
 */
function getExcelFormat(format?: ExportFormat): string | undefined {
  switch (format) {
    case 'currency':
      return '$#,##0'
    case 'percent':
      return '0.0%'
    case 'years':
      return '0.0'
    case 'age':
      return '0'
    case 'number':
      return '#,##0'
    // 'text' is a declared non-numeric field: exported, but deliberately unformatted.
    default:
      return undefined
  }
}

/**
 * Create a mapping of input keys to their cell references
 */
function createInputCellMap(inputs: Record<string, any>): Map<string, string> {
  const cellMap = new Map<string, string>()
  let rowIndex = 2 // Start at row 2 (after header)
  
  for (const key of Object.keys(inputs)) {
    cellMap.set(key, `Inputs!B${rowIndex}`)
    rowIndex++
  }
  
  return cellMap
}

/**
 * Export calculator data to Excel file using ExcelJS with formulas
 */
export async function exportToExcel(data: ExportData): Promise<void> {
  const workbook = new ExcelJS.Workbook()
  
  // Create cell reference map for formulas
  const inputCellMap = createInputCellMap(data.inputs)
  
  // Sheet 1: Inputs (with raw numeric values, not formatted strings)
  const inputSheet = workbook.addWorksheet('Inputs')
  inputSheet.columns = [
    { header: 'Input Parameter', key: 'param', width: 30 },
    { header: 'Value', key: 'value', width: 20 }
  ]
  
  let inputRowIndex = 2
  for (const [key, value] of Object.entries(data.inputs)) {
    // Convert camelCase to Title Case for label
    const label = key.replace(/([A-Z])/g, ' $1').replace(/^./, str => str.toUpperCase()).trim()
    
    const row = inputSheet.addRow({ param: label, value: formatValue(value) })
    
    // Apply formatting to the value cell
    const format = data.inputFormats?.[key]
    if (format) {
      const excelFormat = getExcelFormat(format)
      if (excelFormat) {
        row.getCell(2).numFmt = excelFormat
      }
    }
    
    inputRowIndex++
  }
  
  // Style header row
  inputSheet.getRow(1).font = { bold: true }
  inputSheet.getRow(1).fill = {
    type: 'pattern',
    pattern: 'solid',
    fgColor: { argb: 'FFE0E0E0' }
  }
  
  // Sheet 2: Results (with formulas where applicable)
  const resultSheet = workbook.addWorksheet('Results')
  resultSheet.columns = [
    { header: 'Result', key: 'result', width: 30 },
    { header: 'Value', key: 'value', width: 20 }
  ]
  
  let resultRowIndex = 2
  for (const [key, value] of Object.entries(data.results)) {
    // Convert camelCase to Title Case for label
    const label = key.replace(/([A-Z])/g, ' $1').replace(/^./, str => str.toUpperCase()).trim()
    
    const row = resultSheet.addRow({ result: label })
    const valueCell = row.getCell(2)
    
    // Check if there's a formula for this result
    const formula = data.resultFormulas?.[key]
    if (formula) {
      // Replace input key references with actual cell references
      let excelFormula = formula
      for (const [inputKey, cellRef] of inputCellMap.entries()) {
        // Replace {inputKey} with cell reference
        excelFormula = excelFormula.replace(new RegExp(`\\{${inputKey}\\}`, 'g'), cellRef)
      }
      valueCell.value = { formula: excelFormula }
    } else {
      // No formula, use the calculated value
      valueCell.value = formatValue(value)
    }
    
    // Apply formatting to the value cell
    const format = data.resultFormats?.[key]
    if (format) {
      const excelFormat = getExcelFormat(format)
      if (excelFormat) {
        valueCell.numFmt = excelFormat
      }
    }
    
    resultRowIndex++
  }
  
  // Style header row
  resultSheet.getRow(1).font = { bold: true }
  resultSheet.getRow(1).fill = {
    type: 'pattern',
    pattern: 'solid',
    fgColor: { argb: 'FFE0E0E0' }
  }
  
  // Sheet 3: Projections (with formulas for compound interest calculations)
  if (data.projections && data.projections.length > 0) {
    const projectionSheet = workbook.addWorksheet('Projections')
    
    // Get column headers from first row
    const headers = Object.keys(data.projections[0] || {})
    projectionSheet.columns = headers.map(header => {
      const label = PROJECTION_HEADER_LABELS[header]
        ?? header.replace(/([A-Z])/g, ' $1').replace(/^./, str => str.toUpperCase()).trim()
      return { header: label, key: header, width: Math.max(15, label.length + 2) }
    })
    
    // Check if we have the necessary columns for formulas
    const hasPortfolio = headers.includes('portfolio')
    const hasInflationAdjusted = headers.includes('inflationAdjusted')
    
    // Add first row with actual values (baseline)
    const firstRow = data.projections[0]
    projectionSheet.addRow(firstRow)
    
    // Add subsequent rows with formulas
    for (let i = 1; i < data.projections.length; i++) {
      const row = data.projections[i]
      const newRow = projectionSheet.addRow({})
      
      let colIndex = 1
      for (const header of headers) {
        const cell = newRow.getCell(colIndex)
        
        if (header === 'portfolio' && hasPortfolio) {
          // Portfolio formula: Previous Portfolio * (1 + Expected Return) + Current Contribution
          const currentRowNum = i + 2 // +2 because row 1 is header, row 2 is first data row
          const prevRowNum = currentRowNum - 1
          const portfolioCol = headers.indexOf('portfolio') + 1
          const contributionsCol = headers.indexOf('contributions') + 1
          const expectedReturnRef = inputCellMap.get('expectedReturn')
          
          if (expectedReturnRef) {
            // Previous portfolio * (1 + return) + current contribution
            cell.value = { formula: `${projectionSheet.getCell(prevRowNum, portfolioCol).address}*(1+${expectedReturnRef})+${projectionSheet.getCell(currentRowNum, contributionsCol).address}` }
          } else {
            cell.value = row[header]
          }
          cell.numFmt = '$#,##0'
        } else if (header === 'inflationAdjusted' && hasInflationAdjusted) {
          // Inflation adjusted formula: Portfolio / ((1 + Inflation Rate) ^ Years)
          const currentRowNum = i + 2 // +2 because row 1 is header and we're on current row
          const portfolioCol = headers.indexOf('portfolio') + 1
          const inflationRateRef = inputCellMap.get('inflationRate')
          
          if (inflationRateRef) {
            // Years since start is just the row index (i)
            cell.value = { formula: `${projectionSheet.getCell(currentRowNum, portfolioCol).address}/((1+${inflationRateRef})^${i})` }
          } else {
            cell.value = row[header]
          }
          cell.numFmt = '$#,##0'
        } else if (header === 'totalContributions') {
          // Total contributions: Previous Total + Current Contribution
          const currentRowNum = i + 2 // +2 because row 1 is header, row 2 is first data row
          const prevRowNum = currentRowNum - 1
          const totalContributionsCol = headers.indexOf('totalContributions') + 1
          const contributionsCol = headers.indexOf('contributions') + 1
          
          cell.value = { formula: `${projectionSheet.getCell(prevRowNum, totalContributionsCol).address}+${projectionSheet.getCell(currentRowNum, contributionsCol).address}` }
          cell.numFmt = '$#,##0'
        } else {
          // For other columns (age, year, contributions), just use the value
          cell.value = row[header]
          
          // Apply formatting based on column type
          if (header.toLowerCase().includes('portfolio') || header.toLowerCase().includes('balance') || 
              header.toLowerCase().includes('contribution') || header.toLowerCase().includes('withdrawal')) {
            cell.numFmt = '$#,##0'
          }
        }
        
        colIndex++
      }
    }
    
    // Style header row
    projectionSheet.getRow(1).font = { bold: true }
    projectionSheet.getRow(1).fill = {
      type: 'pattern',
      pattern: 'solid',
      fgColor: { argb: 'FFE0E0E0' }
    }
  }
  
  // Additional sheets (for specialized calculators)
  if (data.additionalSheets) {
    for (const sheet of data.additionalSheets) {
      if (sheet.data && sheet.data.length > 0) {
        const additionalSheet = workbook.addWorksheet(sheet.name)
        
        // Get column headers from first row
        const headers = Object.keys(sheet.data[0] || {})
        additionalSheet.columns = headers.map(header => ({
          header: header.replace(/([A-Z])/g, ' $1').replace(/^./, str => str.toUpperCase()).trim(),
          key: header,
          width: 15
        }))
        
        // Add data rows
        sheet.data.forEach(row => {
          additionalSheet.addRow(row)
        })
        
        // Style header row
        additionalSheet.getRow(1).font = { bold: true }
        additionalSheet.getRow(1).fill = {
          type: 'pattern',
          pattern: 'solid',
          fgColor: { argb: 'FFE0E0E0' }
        }
      }
    }
  }
  
  // Generate filename with timestamp - sanitize for filesystem
  const timestamp = new Date().toISOString().split('T')[0]
  const safeName = data.calculatorName.replace(/[^a-zA-Z0-9]/g, '_')
  const filename = `${safeName}_${timestamp}.xlsx`
  
  // Write file
  const buffer = await workbook.xlsx.writeBuffer()
  const blob = new Blob([buffer], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' })
  const url = window.URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = filename
  a.click()
  window.URL.revokeObjectURL(url)
}

/**
 * The single declaration site for how every exported field is formatted.
 *
 * Lookup is by **exact** field name. That is the whole point. This map replaces two independently
 * maintained lists of substrings, matched with `.includes()` in an order that decided precedence,
 * which produced seven distinct formatting defects:
 *
 *   - `'rate'` matched but `'ratio'` did not, so percent formatting was silently dropped (#53).
 *   - `'totalmonths'` contains `'total'`, so a 25-month payoff exported as `$25` (#62).
 *   - `'strategy'` contains `'rate'` — st-RATE-gy — so the string `"snowball"` got numFmt `0.0%` (#64).
 *   - `'totalDebt'` matched nothing in the *inputs* list, so real dollars exported unformatted (#64).
 *   - `'incomeSourceCount'` contains `'income'`, so a count of 3 income sources exported as `$3`.
 *   - `'contributionFrequency'` contains `'contribution'`, so `"monthly"` landed in a `$#,##0` cell.
 *   - `'withdrawOnlyAfterRetirement'` matched nothing, so a boolean received numFmt `#,##0`.
 *
 * Exact matching makes every one of those unrepresentable rather than merely reordered. There is no
 * precedence to get wrong, and a name like `mortgageBalance` can no longer resolve to `age`.
 *
 * Inputs and results share this map deliberately. The `totalDebt` defect existed *because* the two
 * helpers kept separate lists; one map makes that divergence impossible. Adding a field here is the
 * conscious decision — `exportedFieldsAreDeclared` in the test suite fails until you make it.
 */
const EXPORT_FIELD_FORMATS: Record<string, ExportFormat> = {
  // --- Ages -------------------------------------------------------------------------------------
  currentAge: 'age',
  retirementAge: 'age',
  targetRetirementAge: 'age',
  earlyRetirementAge: 'age',
  medicareAge: 'age',
  planThroughAge: 'age',
  firstShortfallAge: 'age',

  // --- Rates ------------------------------------------------------------------------------------
  expectedReturn: 'percent',
  inflationRate: 'percent',
  withdrawalRate: 'percent',
  savingsRate: 'percent',
  horizonFundedRatio: 'percent',

  // --- Dollar inputs ----------------------------------------------------------------------------
  currentSavings: 'currency',
  annualContribution: 'currency',
  annualIncome: 'currency',
  annualExpenses: 'currency',
  partTimeIncome: 'currency',
  portfolioValue: 'currency',
  contributionAmount: 'currency',
  monthlyPremium: 'currency',
  annualDeductible: 'currency',
  annualOutOfPocket: 'currency',
  monthlyBudget: 'currency',
  extraPayment: 'currency',
  // A real dollar amount that the old inputs list had no entry for at all (#64).
  totalDebt: 'currency',
  accountBalance: 'currency',
  traditionalBalance: 'currency',
  rothBalance: 'currency',
  annualConversion: 'currency',

  // --- 72(t) / SEPP and Roth conversion ---------------------------------------------------------
  // Rates the IRS methods take as inputs; declared alongside the other percentages.
  interestRate: 'percent',
  maximumInterestRate: 'percent',
  estimatedTaxRate: 'percent',
  // The Single Life factor and an actuarial factor both carry one decimal (36.2), so '0.0' is
  // right even though neither is a duration.
  lifeExpectancyFactor: 'years',
  annuityFactor: 'years',
  // ISO dates, a method name and a calendar year. Years are 'text' so they never pick up a
  // thousands separator and render as 2,026.
  birthDate: 'text',
  firstPaymentDate: 'text',
  requiredEndDate: 'text',
  method: 'text',
  startYear: 'text',
  firstAccessibleYear: 'text',
  startingAge: 'age',
  requiredYears: 'number',
  conversionYears: 'number',
  annualPayment: 'currency',
  rmdAnnualPayment: 'currency',
  amortizationAnnualPayment: 'currency',
  annuitizationAnnualPayment: 'currency',
  totalConverted: 'currency',
  totalEstimatedTaxes: 'currency',
  endingTraditionalBalance: 'currency',
  endingRothBalance: 'currency',

  // --- Dollar results ---------------------------------------------------------------------------
  fireNumber: 'currency',
  coastFireNumber: 'currency',
  coastNumber: 'currency',
  baristaNumber: 'currency',
  fullFireNumber: 'currency',
  partTimeIncomeNeeded: 'currency',
  savingsFromPartTime: 'currency',
  monthlyContribution: 'currency',
  annualWithdrawal: 'currency',
  monthlyWithdrawal: 'currency',
  endingBalance: 'currency',
  requiredAnnualSavings: 'currency',
  requiredMonthlySavings: 'currency',
  currentWillGrowTo: 'currency',
  finalNominalBalance: 'currency',
  finalInflationAdjustedBalance: 'currency',
  totalInvested: 'currency',
  totalGrowth: 'currency',
  inflationImpact: 'currency',
  totalInterest: 'currency',
  totalPrincipal: 'currency',
  monthlyPayment: 'currency',
  currentBalance: 'currency',
  balanceAtSemiRetirement: 'currency',
  firstYearIncomeAfterTax: 'currency',
  firstYearSurplus: 'currency',
  annualCost: 'currency',
  totalCost: 'currency',
  avgAnnualCost: 'currency',
  // Configuration rather than an outcome, but it reaches the workbook and it is measured in dollars.
  leanThreshold: 'currency',
  fatThreshold: 'currency',

  // --- Durations --------------------------------------------------------------------------------
  // These carry a fractional part (rounded to one decimal by the calculators), so '0.0' is correct.
  yearsToFIRE: 'years',
  yearsToCoast: 'years',
  yearsToBaristaFIRE: 'years',
  // An age rather than a duration, but rounded to one decimal, so '0.0' preserves it where the
  // integer 'age' format would silently show 51.5 as 52.
  fireAge: 'years',
  portfolioLongevity: 'years',

  // --- Whole counts -----------------------------------------------------------------------------
  // Integers, so '#,##0' rather than the '0.0' of the duration formats above. `totalMonths` is the
  // #62 defect: it used to render as '$25' for a 25-month payoff.
  // gapYears looks like it belongs in the fractional bucket above with fireAge/portfolioLongevity,
  // but it is provably integral: gapYears = max(0, MEDICARE_AGE - earlyRetirementAge), the constant
  // 65 minus an age parsed with parseInt/step=1, so it can never be fractional. It gets '#,##0' so
  // web matches MAUI (#69), which renders it as 20, not 20.0. Do not move it back up.
  gapYears: 'number',
  totalMonths: 'number',
  targetMonths: 'number',
  retirementYears: 'number',
  yearsInvesting: 'number',
  consecutiveFundedYears: 'number',
  yearsFullyCovered: 'number',
  totalDebts: 'number',
  incomeSourceCount: 'number',
  accountCount: 'number',
  additionalExpenseCount: 'number',

  // --- Declared non-numeric ---------------------------------------------------------------------
  // Strings and booleans. Declaring them keeps a number format off a text cell without needing a
  // type guard to rescue them from a name-based guess.
  strategy: 'text',
  mode: 'text',
  contributionFrequency: 'text',
  withdrawOnlyAfterRetirement: 'text',
  reinvestSurplus: 'text',
  isLean: 'text',
  isFat: 'text',
  alreadyCoasting: 'text',
  alreadyAchievable: 'text',
}

/** True when a field has a declared format. Used by the test suite's coverage guard. */
export function isExportFieldDeclared(key: string): boolean {
  return Object.prototype.hasOwnProperty.call(EXPORT_FIELD_FORMATS, key)
}

/**
 * Flatten an inputs or results object into cell values plus their declared formats.
 *
 * Inputs and results share one implementation on purpose. They used to be two near-copies whose
 * behaviour had already diverged — only one had a non-finite guard, only one type-gated formatting,
 * and their key lists disagreed about whether `total*` meant money. Sharing the code means they
 * cannot drift apart again.
 *
 * An **undeclared** field is still exported, just without a number format. Losing a value from a
 * user's workbook is worse than showing it unstyled, and an unstyled number is never *wrong* — the
 * whole point of the defects above is that a guessed format is. `EXPORT_FIELD_FORMATS` is enforced
 * in CI instead, so an undeclared field fails a test long before it reaches anyone.
 */
function prepareForExport(source: any): {
  values: Record<string, any>
  formats: Record<string, ExportFormat>
} {
  const values: Record<string, any> = {}
  const formats: Record<string, ExportFormat> = {}

  for (const [key, value] of Object.entries(source ?? {})) {
    // Arrays and nested objects belong on their own sheets, not as a JSON blob in a labelled row.
    if (Array.isArray(value) || (typeof value === 'object' && value !== null)) {
      continue
    }

    // A null means "this did not happen" — no shortfall age, no payoff date. A blank labelled row
    // cannot be told apart from a missing one, and substituting 0 at the call site reads as a
    // shortfall at age 0. Omitting the row is the only option that generalises.
    if (value === null || value === undefined) {
      continue
    }

    // Non-finite results are legitimate ("the target is never reached"), but writing Infinity or
    // NaN into a numeric cell produces a spreadsheet that outlives the session and cannot be
    // reasoned about. Substitute the same wording the result cards use, with no numeric format.
    if (typeof value === 'number' && !Number.isFinite(value)) {
      values[key] = UNREACHABLE_EXPORT_VALUE
      continue
    }

    values[key] = value

    const format = EXPORT_FIELD_FORMATS[key]
    if (format) {
      formats[key] = format
    } else if (import.meta.env?.DEV && import.meta.env?.MODE !== 'test') {
      // The suite deliberately exercises undeclared keys, so it opts out of the noise. In the dev
      // server this is the first signal that a newly added field is heading for the workbook
      // unstyled; the `exported fields are declared` suite is what actually blocks it.
      console.warn(
        `[excelExport] "${key}" has no entry in EXPORT_FIELD_FORMATS, so it is exported unformatted. ` +
          'Add it there to choose how it renders.',
      )
    }
  }

  return { values, formats }
}

/**
 * Helper to prepare input values for Excel export
 * Returns raw values (not formatted strings) and their declared format hints
 */
export function prepareInputsForExport(params: any): {
  values: Record<string, any>
  formats: Record<string, ExportFormat>
} {
  return prepareForExport(params)
}

/**
 * Helper to prepare result values for Excel export
 * Returns raw values (not formatted strings) and their declared format hints
 */
export function prepareResultsForExport(results: any): {
  values: Record<string, any>
  formats: Record<string, ExportFormat>
} {
  return prepareForExport(results)
}
