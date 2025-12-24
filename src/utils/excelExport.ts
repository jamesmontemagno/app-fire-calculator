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
 * Convert a data object to Excel-friendly format
 * Creates an array of rows with label/value pairs
 */
function objectToRows(data: Record<string, any>, formatLabels = true): any[][] {
  const rows: any[][] = []
  
  for (const [key, value] of Object.entries(data)) {
    // Convert camelCase to Title Case
    const label = formatLabels 
      ? key.replace(/([A-Z])/g, ' $1').replace(/^./, str => str.toUpperCase()).trim()
      : key
    
    rows.push([label, formatValue(value)])
  }
  
  return rows
}

/**
 * Export calculator data to Excel file using ExcelJS
 */
export async function exportToExcel(data: ExportData): Promise<void> {
  const workbook = new ExcelJS.Workbook()
  
  // Sheet 1: Inputs
  const inputSheet = workbook.addWorksheet('Inputs')
  inputSheet.columns = [
    { header: 'Input Parameter', key: 'param', width: 25 },
    { header: 'Value', key: 'value', width: 20 }
  ]
  
  const inputRows = objectToRows(data.inputs)
  inputRows.forEach(row => {
    inputSheet.addRow({ param: row[0], value: row[1] })
  })
  
  // Style header row
  inputSheet.getRow(1).font = { bold: true }
  inputSheet.getRow(1).fill = {
    type: 'pattern',
    pattern: 'solid',
    fgColor: { argb: 'FFE0E0E0' }
  }
  
  // Sheet 2: Results
  const resultSheet = workbook.addWorksheet('Results')
  resultSheet.columns = [
    { header: 'Result', key: 'result', width: 25 },
    { header: 'Value', key: 'value', width: 20 }
  ]
  
  const resultRows = objectToRows(data.results)
  resultRows.forEach(row => {
    resultSheet.addRow({ result: row[0], value: row[1] })
  })
  
  // Style header row
  resultSheet.getRow(1).font = { bold: true }
  resultSheet.getRow(1).fill = {
    type: 'pattern',
    pattern: 'solid',
    fgColor: { argb: 'FFE0E0E0' }
  }
  
  // Sheet 3: Projections (if available)
  if (data.projections && data.projections.length > 0) {
    const projectionSheet = workbook.addWorksheet('Projections')
    
    // Get column headers from first row
    const headers = Object.keys(data.projections[0] || {})
    projectionSheet.columns = headers.map(header => ({
      header: header.replace(/([A-Z])/g, ' $1').replace(/^./, str => str.toUpperCase()).trim(),
      key: header,
      width: 15
    }))
    
    // Add data rows
    data.projections.forEach(row => {
      projectionSheet.addRow(row)
    })
    
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
 * Helper to format input values for display
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
 * Helper to format result values for display
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
