import { useEffect, useState } from 'react'
import { Save, Check, Copy, Clock, RotateCcw } from 'lucide-react'
import Button from './Button'
import ConfirmationDialog from './ConfirmationDialog'

interface UrlActionsProps {
  onReset: () => void
  onSave: () => void
  onLoad: () => void
  onCopy: () => Promise<boolean>
  hasCustomParams: boolean
  hasUnsavedChanges: boolean
  hasSavedParams: boolean
  savedAt: string | null
}

export default function UrlActions({
  onReset,
  onSave,
  onLoad,
  onCopy,
  hasCustomParams,
  hasUnsavedChanges,
  hasSavedParams,
  savedAt,
}: UrlActionsProps) {
  const [copied, setCopied] = useState(false)
  const [confirmingSave, setConfirmingSave] = useState(false)
  const [confirmingLoad, setConfirmingLoad] = useState(false)
  const savedDate = savedAt
    ? new Date(savedAt).toLocaleString()
    : 'a previous session'

  useEffect(() => {
    if (!hasSavedParams) setConfirmingLoad(false)
  }, [hasSavedParams])

  const handleCopy = async () => {
    const success = await onCopy()
    if (success) {
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    }
  }

  const handleLoad = () => {
    setConfirmingLoad(true)
  }

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
      <div className="flex items-center gap-2">
        <Button
          variant={hasUnsavedChanges ? 'primary' : 'outline'}
          size="sm"
          onClick={() => setConfirmingSave(true)}
          className="gap-1.5"
          aria-label={hasUnsavedChanges ? 'Save changes in this browser' : 'Save current values in this browser'}
        >
          <Save className="h-4 w-4" aria-hidden="true" strokeWidth={1.5} />
          Save
          {hasUnsavedChanges && <span className="w-2 h-2 rounded-full bg-current" aria-label="Unsaved changes" />}
        </Button>
        <Button
          variant="outline"
          size="sm"
          onClick={handleCopy}
          className="gap-1.5"
        >
        {copied ? (
          <>
            <Check className="h-4 w-4 text-success" aria-hidden="true" strokeWidth={2} />
            Copied
          </>
        ) : (
          <>
            <Copy className="h-4 w-4" aria-hidden="true" strokeWidth={1.5} />
            Copy Link
          </>
        )}
        </Button>
        {hasSavedParams && (
          <Button
            variant="outline"
            size="sm"
            onClick={handleLoad}
            className="gap-1.5"
            title={savedAt ? `Saved ${savedDate}` : 'Saved date unavailable'}
            aria-label={savedAt ? `Load calculation saved ${savedDate}` : 'Load saved calculation'}
          >
            <Clock className="h-4 w-4" aria-hidden="true" strokeWidth={1.5} />
            Load
          </Button>
        )}
        
        {hasCustomParams && (
          <Button
            variant="ghost"
            size="sm"
            onClick={onReset}
            className="gap-1.5"
            title="Reset values and clear saved data"
          >
            <RotateCcw className="h-4 w-4" aria-hidden="true" strokeWidth={1.5} />
            Reset
          </Button>
        )}
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
          Replace the current values with the calculation saved on {savedDate}.
        </ConfirmationDialog>
      )}
    </>
  )
}
