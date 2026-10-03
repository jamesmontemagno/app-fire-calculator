import { useEffect, useState } from 'react'
import { Clock, Save } from 'lucide-react'
import Button from './Button'
import ConfirmationDialog from './ConfirmationDialog'

interface SaveLoadBarProps {
  onSave: () => void
  onLoad: () => void
  hasUnsavedChanges: boolean
  hasSavedParams: boolean
  savedAt: string | null
}

export default function SaveLoadBar({
  onSave,
  onLoad,
  hasUnsavedChanges,
  hasSavedParams,
  savedAt,
}: SaveLoadBarProps) {
  const [confirmingSave, setConfirmingSave] = useState(false)
  const [confirmingLoad, setConfirmingLoad] = useState(false)
  const savedDate = savedAt ? new Date(savedAt) : null
  const savedDateLong = savedDate ? savedDate.toLocaleString() : 'a previous session'

  useEffect(() => {
    if (!hasSavedParams) setConfirmingLoad(false)
  }, [hasSavedParams])

  const confirmSave = () => {
    onSave()
    setConfirmingSave(false)
  }

  const confirmLoad = () => {
    onLoad()
    setConfirmingLoad(false)
  }

  return (
    <>
      {/* The mobile app header is sticky at top-0 and 4rem tall, so the bar parks beneath it until lg. */}
      <div className="sticky top-16 z-20 -mx-4 -mt-2 mb-6 bg-surface px-4 py-2 sm:-mx-6 sm:px-6 lg:top-0 lg:-mx-8 lg:px-8">
        <div
          role="group"
          aria-label="Saved calculation"
          className="flex items-center justify-between gap-3 rounded-container border border-border-subtle bg-surface-raised px-3 py-2"
        >
          <p
            role="status"
            className={`min-w-0 text-sm leading-tight ${hasUnsavedChanges ? 'font-medium text-content' : 'text-content-muted'}`}
          >
            {hasUnsavedChanges ? (
              'Unsaved changes'
            ) : savedDate ? (
              <>
                Saved{' '}
                <span className="sm:hidden">
                  {savedDate.toLocaleDateString(undefined, { month: 'short', day: 'numeric' })}
                </span>
                <span className="hidden sm:inline">
                  {savedDate.toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' })}
                </span>
              </>
            ) : hasSavedParams ? (
              'Saved in this browser'
            ) : (
              'Not saved yet'
            )}
          </p>
          <div className="flex shrink-0 items-center gap-2">
            <Button
              variant={hasUnsavedChanges ? 'primary' : 'outline'}
              size="sm"
              onClick={() => setConfirmingSave(true)}
              className="scroll-mt-0 gap-1.5"
              aria-label={hasUnsavedChanges ? 'Save changes in this browser' : 'Save current values in this browser'}
            >
              <Save className="h-4 w-4" aria-hidden="true" strokeWidth={1.5} />
              Save
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setConfirmingLoad(true)}
              disabled={!hasSavedParams}
              className="scroll-mt-0 gap-1.5"
              title={hasSavedParams && savedAt ? `Saved ${savedDateLong}` : undefined}
              aria-label={
                !hasSavedParams
                  ? 'Load saved calculation (nothing saved yet)'
                  : savedAt
                    ? `Load calculation saved ${savedDateLong}`
                    : 'Load saved calculation'
              }
            >
              <Clock className="h-4 w-4" aria-hidden="true" strokeWidth={1.5} />
              Load
            </Button>
          </div>
        </div>
      </div>
      {confirmingSave && (
        <ConfirmationDialog
          title="Save this calculation?"
          onCancel={() => setConfirmingSave(false)}
          onConfirm={confirmSave}
          confirmLabel="Save locally"
        >
          Your calculator values will be saved only in this browser on this device. They are not sent to a server and can be managed or deleted anytime in Settings.
        </ConfirmationDialog>
      )}
      {confirmingLoad && (
        <ConfirmationDialog
          title="Load saved calculation?"
          onCancel={() => setConfirmingLoad(false)}
          onConfirm={confirmLoad}
          confirmLabel="Load calculation"
        >
          Replace the current values with the calculation saved on {savedDateLong}.
        </ConfirmationDialog>
      )}
    </>
  )
}
