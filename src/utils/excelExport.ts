import * as XLSX from 'xlsx'
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
    // For percentages stored as decimals, keep them as decimals
    // Excel will format them with percentage format
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
 * Export calculator data to Excel file
 */
export function exportToExcel(data: ExportData): void {
  const workbook = XLSX.utils.book_new()
  
  // Sheet 1: Inputs
  const inputRows = [
    ['Input Parameter', 'Value'],
    ...objectToRows(data.inputs)
  ]
  const inputSheet = XLSX.utils.aoa_to_sheet(inputRows)
  
  // Set column widths
  inputSheet['!cols'] = [
    { wch: 25 },  // Input Parameter column
    { wch: 20 }   // Value column
  ]
  
  XLSX.utils.book_append_sheet(workbook, inputSheet, 'Inputs')
  
  // Sheet 2: Results
  const resultRows = [
    ['Result', 'Value'],
    ...objectToRows(data.results)
  ]
  const resultSheet = XLSX.utils.aoa_to_sheet(resultRows)
  
  // Set column widths
  resultSheet['!cols'] = [
    { wch: 25 },  // Result column
    { wch: 20 }   // Value column
  ]
  
  XLSX.utils.book_append_sheet(workbook, resultSheet, 'Results')
  
  // Sheet 3: Projections (if available)
  if (data.projections && data.projections.length > 0) {
    const projectionSheet = XLSX.utils.json_to_sheet(data.projections)
    
    // Auto-size columns
    const colWidths = Object.keys(data.projections[0]).map(() => ({ wch: 15 }))
    projectionSheet['!cols'] = colWidths
    
    XLSX.utils.book_append_sheet(workbook, projectionSheet, 'Projections')
  }
  
  // Additional sheets (for specialized calculators)
  if (data.additionalSheets) {
    for (const sheet of data.additionalSheets) {
      if (sheet.data && sheet.data.length > 0) {
        const additionalSheet = XLSX.utils.json_to_sheet(sheet.data)
        
        // Auto-size columns
        const colWidths = Object.keys(sheet.data[0]).map(() => ({ wch: 15 }))
        additionalSheet['!cols'] = colWidths
        
        XLSX.utils.book_append_sheet(workbook, additionalSheet, sheet.name)
      }
    }
  }
  
  // Generate filename with timestamp
  const timestamp = new Date().toISOString().split('T')[0]
  const filename = `${data.calculatorName.replace(/\s+/g, '_')}_${timestamp}.xlsx`
  
  // Write file
  XLSX.writeFile(workbook, filename)
}

/**
 * Helper to format input values for display
 */
export function formatInputsForExport(params: any): Record<string, any> {
  const formatted: Record<string, any> = {}
  
  for (const [key, value] of Object.entries(params)) {
    // Skip complex objects like debts array
    if (Array.isArray(value) || (typeof value === 'object' && value !== null)) {
      continue
    }
    
    // Format based on the key name
    if (key.toLowerCase().includes('age')) {
      formatted[key] = value
    } else if (key.toLowerCase().includes('rate') || key.toLowerCase().includes('return') || key.toLowerCase().includes('inflation')) {
      formatted[key] = formatPercent(value as number)
    } else if (key.toLowerCase().includes('savings') || key.toLowerCase().includes('contribution') || 
               key.toLowerCase().includes('expenses') || key.toLowerCase().includes('income') ||
               key.toLowerCase().includes('value') || key.toLowerCase().includes('budget') || 
               key.toLowerCase().includes('payment')) {
      formatted[key] = formatCurrency(value as number)
    } else if (key.toLowerCase().includes('years') || key.toLowerCase().includes('months')) {
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
  
  for (const [key, value] of Object.entries(results)) {
    // Skip arrays and complex objects (they go in separate sheets)
    if (Array.isArray(value) || (typeof value === 'object' && value !== null)) {
      continue
    }
    
    // Format based on the key name and value type
    if (typeof value === 'number') {
      if (key.toLowerCase().includes('rate') || key === 'savingsRate' || key === 'successRate') {
        formatted[key] = formatPercent(value)
      } else if (key.toLowerCase().includes('number') || key.toLowerCase().includes('value') || 
                 key.toLowerCase().includes('balance') || key.toLowerCase().includes('withdrawal') ||
                 key.toLowerCase().includes('income') || key.toLowerCase().includes('interest') ||
                 key.toLowerCase().includes('payment') || key.toLowerCase().includes('savings')) {
        formatted[key] = formatCurrency(value)
      } else if (key.toLowerCase().includes('years') || key.toLowerCase().includes('months') ||
                 key.toLowerCase().includes('age') || key.toLowerCase().includes('longevity')) {
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
