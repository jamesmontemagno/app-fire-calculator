import { useState } from 'react'
import Button from './Button'

interface ExportButtonProps {
  onExport: () => void
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

  const handleExport = async () => {
    setIsExporting(true)
    
    try {
      await onExport()
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
          <span className="animate-spin">⏳</span>
          <span className="ml-2">Exporting...</span>
        </>
      ) : showSuccess ? (
        <>
          <span>✅</span>
          <span className="ml-2">Exported!</span>
        </>
      ) : (
        <>
          <span>📊</span>
          <span className="ml-2">Export to Excel</span>
        </>
      )}
    </Button>
  )
}
