import Card, { CardContent } from './Card'
import Disclaimer from './Disclaimer'
import ExportButton from './ExportButton'
import UrlActions from './UrlActions'

interface CalculatorFooterProps {
  onExport: () => void | Promise<void>
  exportDisabled?: boolean
  onReset: () => void
  onSave: () => void
  onLoad: () => void
  onCopy: () => Promise<boolean>
  hasCustomParams: boolean
  hasUnsavedChanges: boolean
  hasSavedParams: boolean
  savedAt: string | null
}

export default function CalculatorFooter({
  onExport,
  exportDisabled = false,
  onReset,
  onSave,
  onLoad,
  onCopy,
  hasCustomParams,
  hasUnsavedChanges,
  hasSavedParams,
  savedAt,
}: CalculatorFooterProps) {
  return (
    <Card>
      <CardContent className="space-y-5">
        <div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
          <div>
            <h2 className="font-semibold text-gray-900 dark:text-gray-100">Keep this calculation</h2>
            <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
              Save locally in this browser, share a link with the current values, or export a workbook.
            </p>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <ExportButton onExport={onExport} disabled={exportDisabled} />
            <UrlActions
              onReset={onReset}
              onSave={onSave}
              onLoad={onLoad}
              onCopy={onCopy}
              hasCustomParams={hasCustomParams}
              hasUnsavedChanges={hasUnsavedChanges}
              hasSavedParams={hasSavedParams}
              savedAt={savedAt}
            />
          </div>
        </div>
        <Disclaimer embedded />
      </CardContent>
    </Card>
  )
}
