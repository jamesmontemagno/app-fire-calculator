import ExcelJS from 'exceljs'
import { formatCurrency, formatPercent } from './calculations'

/**
 * Export calculator data to Excel spreadsheet
 * Creates a workbook with multiple sheets for inputs, results, and projections
 */

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
  inputFormats?: Record<string, 'currency' | 'percent' | 'number' | 'age'>
  resultFormats?: Record<string, 'currency' | 'percent' | 'number' | 'years'>
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
function getExcelFormat(format?: 'currency' | 'percent' | 'number' | 'age' | 'years'): string | undefined {
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
    projectionSheet.columns = headers.map(header => ({
      header: header.replace(/([A-Z])/g, ' $1').replace(/^./, str => str.toUpperCase()).trim(),
      key: header,
      width: 15
    }))
    
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
 * Helper to prepare input values for Excel export
 * Returns raw numeric values (not formatted strings) and format hints
 */
export function prepareInputsForExport(params: any): { 
  values: Record<string, any>
  formats: Record<string, 'currency' | 'percent' | 'number' | 'age'>
} {
  const values: Record<string, any> = {}
  const formats: Record<string, 'currency' | 'percent' | 'number' | 'age'> = {}
  
  // Define patterns for different value types
  const currencyKeys = ['savings', 'contribution', 'expenses', 'income', 'value', 'budget', 'payment', 'premium', 'deductible', 'pocket']
  const percentKeys = ['rate', 'return', 'inflation', 'withdrawal', 'swr']
  const ageKeys = ['age']
  
  for (const [key, value] of Object.entries(params)) {
    // Skip complex objects like debts array
    if (Array.isArray(value) || (typeof value === 'object' && value !== null)) {
      continue
    }
    
    // Store raw numeric value
    values[key] = value
    
    const lowerKey = key.toLowerCase()
    
    // Determine format type
    if (ageKeys.some(pattern => lowerKey.includes(pattern))) {
      formats[key] = 'age'
    } else if (percentKeys.some(pattern => lowerKey.includes(pattern))) {
      formats[key] = 'percent'
    } else if (currencyKeys.some(pattern => lowerKey.includes(pattern))) {
      formats[key] = 'currency'
    } else {
      formats[key] = 'number'
    }
  }
  
  return { values, formats }
}

/**
 * Helper to prepare result values for Excel export
 * Returns raw numeric values (not formatted strings) and format hints
 */
export function prepareResultsForExport(results: any): {
  values: Record<string, any>
  formats: Record<string, 'currency' | 'percent' | 'number' | 'years'>
} {
  const values: Record<string, any> = {}
  const formats: Record<string, 'currency' | 'percent' | 'number' | 'years'> = {}
  
  // Define patterns for different value types
  const currencyKeys = ['number', 'value', 'balance', 'withdrawal', 'income', 'interest', 'payment', 'savings', 'cost', 'total', 'principal']
  const percentKeys = ['rate', 'savingsrate', 'successrate', 'percent', 'progress']
  const timeKeys = ['years', 'months', 'age', 'longevity']
  
  for (const [key, value] of Object.entries(results)) {
    // Skip arrays and complex objects (they go in separate sheets)
    if (Array.isArray(value) || (typeof value === 'object' && value !== null)) {
      continue
    }
    
    // Store raw value
    values[key] = value
    
    // Determine format type
    if (typeof value === 'number') {
      const lowerKey = key.toLowerCase()
      
      if (percentKeys.some(pattern => lowerKey.includes(pattern))) {
        formats[key] = 'percent'
      } else if (currencyKeys.some(pattern => lowerKey.includes(pattern))) {
        formats[key] = 'currency'
      } else if (timeKeys.some(pattern => lowerKey.includes(pattern))) {
        formats[key] = 'years'
      } else {
        formats[key] = 'number'
      }
    }
  }
  
  return { values, formats }
}

/**
 * Helper to format input values for display (DEPRECATED - kept for backward compatibility)
 * Use prepareInputsForExport instead for Excel exports with formulas
 */
export function formatInputsForExport(params: any): Record<string, any> {
  const formatted: Record<string, any> = {}
  
  // Define patterns for different value types
  const currencyKeys = ['savings', 'contribution', 'expenses', 'income', 'value', 'budget', 'payment', 'premium', 'deductible', 'pocket']
  const percentKeys = ['rate', 'return', 'inflation', 'withdrawal', 'swr']
  const ageKeys = ['age']
  const timeKeys = ['years', 'months']
  
  for (const [key, value] of Object.entries(params)) {
    // Skip complex objects like debts array
    if (Array.isArray(value) || (typeof value === 'object' && value !== null)) {
      continue
    }
    
    const lowerKey = key.toLowerCase()
    
    // Format based on the key name patterns
    if (ageKeys.some(pattern => lowerKey.includes(pattern))) {
      formatted[key] = value
    } else if (percentKeys.some(pattern => lowerKey.includes(pattern))) {
      formatted[key] = formatPercent(value as number)
    } else if (currencyKeys.some(pattern => lowerKey.includes(pattern))) {
      formatted[key] = formatCurrency(value as number)
    } else if (timeKeys.some(pattern => lowerKey.includes(pattern))) {
      formatted[key] = value
    } else {
      formatted[key] = value
    }
  }
  
  return formatted
}

/**
 * Helper to format result values for display (DEPRECATED - kept for backward compatibility)
 * Use prepareResultsForExport instead for Excel exports with formulas
 */
export function formatResultsForExport(results: any): Record<string, any> {
  const formatted: Record<string, any> = {}
  
  // Define patterns for different value types
  const currencyKeys = ['number', 'value', 'balance', 'withdrawal', 'income', 'interest', 'payment', 'savings', 'cost', 'total', 'principal']
  const percentKeys = ['rate', 'savingsrate', 'successrate', 'percent', 'progress']
  const timeKeys = ['years', 'months', 'age', 'longevity']
  
  for (const [key, value] of Object.entries(results)) {
    // Skip arrays and complex objects (they go in separate sheets)
    if (Array.isArray(value) || (typeof value === 'object' && value !== null)) {
      continue
    }
    
    // Format based on the key name and value type
    if (typeof value === 'number') {
      const lowerKey = key.toLowerCase()
      
      if (percentKeys.some(pattern => lowerKey.includes(pattern))) {
        formatted[key] = formatPercent(value)
      } else if (currencyKeys.some(pattern => lowerKey.includes(pattern))) {
        formatted[key] = formatCurrency(value)
      } else if (timeKeys.some(pattern => lowerKey.includes(pattern))) {
        formatted[key] = Math.round(value * 10) / 10 // Round to 1 decimal
      } else {
        formatted[key] = value
      }
    } else {
      formatted[key] = value
    }
  }
  
  return formatted
}
