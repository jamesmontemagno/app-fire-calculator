import Card, { CardContent } from './Card'
import ExportButton from './ExportButton'
import MobileAppPromo from './MobileAppPromo'
import UrlActions from './UrlActions'

interface CalculatorFooterProps {
  onExport: () => void | Promise<void>
  exportDisabled?: boolean
  onReset: () => void
  onCopy: () => Promise<boolean>
  hasCustomParams: boolean
}

export default function CalculatorFooter({
  onExport,
  exportDisabled = false,
  onReset,
  onCopy,
  hasCustomParams,
}: CalculatorFooterProps) {
  return (
    <div className="space-y-6">
      <Card>
        <CardContent>
          <div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
            <div>
              <h2 className="font-semibold text-content">Share or export this calculation</h2>
              <p className="mt-1 text-sm text-content-muted">
                Copy a link with the current values or export a workbook. Save and load stay in the bar at the top of the page.
              </p>
            </div>
            <div className="flex flex-wrap items-center gap-2">
              <ExportButton onExport={onExport} disabled={exportDisabled} />
              <UrlActions
                onReset={onReset}
                onCopy={onCopy}
                hasCustomParams={hasCustomParams}
              />
            </div>
          </div>
        </CardContent>
      </Card>
      <MobileAppPromo />
    </div>
  )
}
