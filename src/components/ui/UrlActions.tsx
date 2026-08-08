import { useEffect, useState } from 'react'
import Button from './Button'

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
          <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 4h11l3 3v13H5V4Zm3 0v6h7V4m-7 16v-6h8v6" />
          </svg>
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
            <svg className="w-4 h-4 text-green-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
            </svg>
            Copied!
          </>
        ) : (
          <>
            <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 5H6a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2v-1M8 5a2 2 0 002 2h2a2 2 0 002-2M8 5a2 2 0 012-2h2a2 2 0 012 2m0 0h2a2 2 0 012 2v3m2 4H10m0 0l3-3m-3 3l3 3" />
            </svg>
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
            <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
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
            <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
            </svg>
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

interface ConfirmationDialogProps {
  title: string
  children: string
  confirmLabel: string
  onCancel: () => void
  onConfirm: () => void
}

function ConfirmationDialog({
  title,
  children,
  confirmLabel,
  onCancel,
  onConfirm,
}: ConfirmationDialogProps) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4" role="presentation">
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby="confirmation-dialog-title"
        className="w-full max-w-md rounded-xl bg-white p-6 shadow-xl dark:bg-gray-900"
      >
        <h2 id="confirmation-dialog-title" className="text-lg font-semibold text-gray-900 dark:text-gray-100">
          {title}
        </h2>
        <p className="mt-2 text-sm text-gray-600 dark:text-gray-400">{children}</p>
        <div className="mt-6 flex justify-end gap-3">
          <Button variant="ghost" onClick={onCancel}>Cancel</Button>
          <Button onClick={onConfirm}>{confirmLabel}</Button>
        </div>
      </div>
    </div>
  )
}
