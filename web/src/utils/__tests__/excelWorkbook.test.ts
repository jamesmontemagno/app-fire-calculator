import ExcelJS from 'exceljs'
import { afterEach, describe, expect, it } from 'vitest'

import { calculateSnowballPayoff } from '../calculations'
import { exportToExcel, prepareInputsForExport, prepareResultsForExport } from '../excelExport'

/**
 * The rest of the export suite checks the format *token* a field is assigned. This file checks that
 * the token survives the trip into a real `.xlsx` and lands on the cell as a number format.
 *
 * That last hop is where #62 was visible: `prepareResultsForExport` said `'currency'`, and
 * `getExcelFormat` faithfully turned that into numFmt `'$#,##0'`, so a 25-month debt payoff opened
 * as `$25` in the spreadsheet a user downloaded and forwarded to someone else. Asserting the token
 * alone would not have caught the impact, and asserting it now would not prove the impact is gone.
 */

/** `exportToExcel` triggers a browser download, so the suite stands in for the DOM to catch it. */
async function buildWorkbook(run: () => Promise<void>): Promise<ExcelJS.Workbook> {
  const globals = globalThis as Record<string, unknown>
  const saved = { Blob: globals.Blob, window: globals.window, document: globals.document }
  let captured: ArrayBuffer | undefined

  globals.Blob = class {
    constructor(parts: ArrayBuffer[]) {
      captured = parts[0]
    }
  }
  globals.window = { URL: { createObjectURL: () => 'blob:test', revokeObjectURL: () => {} } }
  globals.document = { createElement: () => ({ click: () => {} }) }

  try {
    await run()
  } finally {
    Object.assign(globals, saved)
  }

  const workbook = new ExcelJS.Workbook()
  await workbook.xlsx.load(captured!)
  return workbook
}

/** The label/value pairs of a two-column sheet, keyed by the label a user reads. */
function readSheet(workbook: ExcelJS.Workbook, name: string) {
  const rows: Record<string, { value: unknown; numFmt?: string }> = {}
  workbook.getWorksheet(name)?.eachRow((row, index) => {
    if (index === 1) return // header
    rows[String(row.getCell(1).value)] = {
      value: row.getCell(2).value,
      numFmt: row.getCell(2).numFmt,
    }
  })
  return rows
}

const DEBT_INPUTS = {
  strategy: 'snowball',
  mode: 'budget',
  monthlyBudget: 500,
  targetMonths: 24,
  extraPayment: 0,
  totalDebts: 2,
  totalDebt: 21_500,
}

async function debtPayoffWorkbook() {
  const results = calculateSnowballPayoff(
    [{ id: '1', name: 'Card', balance: 10_000, rate: 0.2, minPayment: 200 }],
    500,
  )
  const { values: inputs, formats: inputFormats } = prepareInputsForExport(DEBT_INPUTS)
  const { values: resultValues, formats: resultFormats } = prepareResultsForExport(results)

  return buildWorkbook(() =>
    exportToExcel({
      calculatorName: 'Debt Payoff',
      inputs,
      results: resultValues,
      inputFormats,
      resultFormats,
    }),
  )
}

afterEach(() => {
  // `buildWorkbook` restores the globals it replaces; this is the backstop if a test throws first.
  const globals = globalThis as Record<string, unknown>
  if (typeof globals.window === 'object' && globals.window !== null && !('addEventListener' in globals.window)) {
    delete globals.window
    delete globals.document
  }
})

describe('formats that reach the spreadsheet', () => {
  it('#62: a 25-month payoff opens as 25, not $25', async () => {
    const results = readSheet(await debtPayoffWorkbook(), 'Results')

    expect(results['Total Months']).toEqual({ value: 25, numFmt: '#,##0' })

    // The genuinely-currency siblings on the same sheet are untouched, which is what made the
    // defect easy to miss: three of the four rows were right.
    expect(results['Total Interest'].numFmt).toBe('$#,##0')
    expect(results['Total Principal'].numFmt).toBe('$#,##0')
    expect(results['Monthly Payment'].numFmt).toBe('$#,##0')
  })

  it('#64: text inputs carry no number format, and totalDebt carries currency', async () => {
    const inputs = readSheet(await debtPayoffWorkbook(), 'Inputs')

    // 'strategy' contains 'rate', so this cell used to carry numFmt '0.0%' around the word
    // "snowball". 'mode' matched nothing and fell through to '#,##0'.
    expect(inputs.Strategy).toEqual({ value: 'snowball', numFmt: undefined })
    expect(inputs.Mode).toEqual({ value: 'budget', numFmt: undefined })

    // A real dollar amount that the inputs list had no entry for, so it exported bare.
    expect(inputs['Total Debt']).toEqual({ value: 21_500, numFmt: '$#,##0' })

    // A count of debts is not money, and the budget is.
    expect(inputs['Total Debts'].numFmt).toBe('#,##0')
    expect(inputs['Monthly Budget'].numFmt).toBe('$#,##0')
  })

  it('omits a null result rather than writing a labelled blank row', async () => {
    const { values, formats } = prepareResultsForExport({
      endingBalance: 250_000,
      firstShortfallAge: null,
    })
    const workbook = await buildWorkbook(() =>
      exportToExcel({
        calculatorName: 'Retirement Cash Flow',
        inputs: {},
        results: values,
        resultFormats: formats,
      }),
    )

    const results = readSheet(workbook, 'Results')
    expect(results).not.toHaveProperty('First Shortfall Age')
    expect(results['Ending Balance']).toEqual({ value: 250_000, numFmt: '$#,##0' })
  })

  it('writes unreachable results as words rather than "$∞"', async () => {
    const { values, formats } = prepareResultsForExport({ fireNumber: Infinity })
    const workbook = await buildWorkbook(() =>
      exportToExcel({
        calculatorName: 'Standard FIRE',
        inputs: {},
        results: values,
        resultFormats: formats,
      }),
    )

    expect(readSheet(workbook, 'Results')['Fire Number']).toEqual({
      value: 'Not reachable',
      numFmt: undefined,
    })
  })
})
