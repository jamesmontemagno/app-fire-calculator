import { useState } from 'react'
import { FileSpreadsheet, Check, LoaderCircle } from 'lucide-react'
import Button from './Button'
import { usePrefersReducedMotion } from '../../hooks/usePrefersReducedMotion'

interface ExportButtonProps {
  onExport: () => void | Promise<void>
  disabled?: boolean
  className?: string
}

/**
 * Button component for exporting calculator data to Excel
 * Shows loading state during export and success feedback
 */
export default function ExportButton({ onExport, disabled = false, className = '' }: ExportButtonProps) {
  const [isExporting, setIsExporting] = useState(false)
  const [showSuccess, setShowSuccess] = useState(false)
  const prefersReducedMotion = usePrefersReducedMotion()

  const handleExport = async () => {
    setIsExporting(true)
    
    try {
      const result = onExport()
      if (result instanceof Promise) {
        await result
      }
      setShowSuccess(true)
      setTimeout(() => setShowSuccess(false), 2000)
    } catch (error) {
      console.error('Export failed:', error)
    } finally {
      setIsExporting(false)
    }
  }

  return (
    <Button
      onClick={handleExport}
      disabled={disabled || isExporting}
      variant="secondary"
      className={className}
      title="Export to Excel spreadsheet"
    >
      {isExporting ? (
        <>
          <LoaderCircle
            className={`h-4 w-4 ${prefersReducedMotion ? '' : 'animate-spin'}`}
            aria-hidden="true"
            strokeWidth={1.5}
          />
          <span>Exporting...</span>
        </>
      ) : showSuccess ? (
        <>
          <Check className="h-4 w-4 text-success" aria-hidden="true" strokeWidth={2} />
          <span>Exported</span>
        </>
      ) : (
        <>
          <FileSpreadsheet className="h-4 w-4" aria-hidden="true" strokeWidth={1.5} />
          <span>Export to Excel</span>
        </>
      )}
    </Button>
  )
}
