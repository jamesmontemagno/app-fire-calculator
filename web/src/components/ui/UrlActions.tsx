import { useState } from 'react'
import { Check, Copy, RotateCcw } from 'lucide-react'
import Button from './Button'

interface UrlActionsProps {
  onReset: () => void
  onCopy: () => Promise<boolean>
  hasCustomParams: boolean
}

export default function UrlActions({
  onReset,
  onCopy,
  hasCustomParams,
}: UrlActionsProps) {
  const [copied, setCopied] = useState(false)

  const handleCopy = async () => {
    const success = await onCopy()
    if (success) {
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    }
  }

  return (
    <div className="flex items-center gap-2">
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
  )
}
