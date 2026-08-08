import { type ReactNode, useEffect, useId, useRef } from 'react'
import Button from './Button'

interface ConfirmationDialogProps {
  title: string
  children: ReactNode
  confirmLabel: string
  onCancel: () => void
  onConfirm: () => void
}

export default function ConfirmationDialog({
  title,
  children,
  confirmLabel,
  onCancel,
  onConfirm,
}: ConfirmationDialogProps) {
  const titleId = useId()
  const dialogRef = useRef<HTMLDivElement>(null)
  const previouslyFocusedElementRef = useRef<HTMLElement | null>(null)

  useEffect(() => {
    previouslyFocusedElementRef.current = document.activeElement instanceof HTMLElement
      ? document.activeElement
      : null
    dialogRef.current?.querySelector<HTMLButtonElement>('[data-autofocus]')?.focus()

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        onCancel()
        return
      }

      if (event.key !== 'Tab') return

      const focusableElements = dialogRef.current?.querySelectorAll<HTMLElement>(
        'button:not([disabled])',
      )
      if (!focusableElements?.length) return

      const first = focusableElements[0]
      const last = focusableElements[focusableElements.length - 1]
      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault()
        last.focus()
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault()
        first.focus()
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => {
      window.removeEventListener('keydown', handleKeyDown)
      previouslyFocusedElementRef.current?.focus()
    }
  }, [onCancel])

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
      role="presentation"
      onClick={onCancel}
    >
      <div
        ref={dialogRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        className="w-full max-w-md rounded-xl bg-white p-6 shadow-xl dark:bg-gray-900"
        onClick={event => event.stopPropagation()}
      >
        <h2 id={titleId} className="text-lg font-semibold text-gray-900 dark:text-gray-100">
          {title}
        </h2>
        <div className="mt-2 text-sm text-gray-600 dark:text-gray-400">{children}</div>
        <div className="mt-6 flex justify-end gap-3">
          <Button data-autofocus variant="ghost" onClick={onCancel}>Cancel</Button>
          <Button onClick={onConfirm}>{confirmLabel}</Button>
        </div>
      </div>
    </div>
  )
}
