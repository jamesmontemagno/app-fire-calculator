import { type ReactNode, useId } from 'react'
import { ChevronDown } from 'lucide-react'

interface AdvancedDetailsProps {
  children: ReactNode
  summary?: string
  description?: string
  className?: string
}

export default function AdvancedDetails({
  children,
  summary = 'Advanced assumptions',
  description = 'Adjust the long-term assumptions behind this estimate.',
  className = '',
}: AdvancedDetailsProps) {
  const descriptionId = useId()

  return (
    <details className={`group border-y border-border-subtle ${className}`}>
      <summary
        className="flex cursor-pointer list-none items-center justify-between gap-4 py-4 text-left font-semibold text-content marker:hidden focus:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-surface"
        aria-describedby={descriptionId}
      >
        <span>
          {summary}
          <span id={descriptionId} className="mt-0.5 block text-sm font-normal text-content-muted">
            {description}
          </span>
        </span>
        <ChevronDown
          className="h-5 w-5 shrink-0 text-content-subtle transition-transform duration-200 motion-reduce:transition-none group-open:rotate-180"
          aria-hidden="true"
          strokeWidth={1.5}
        />
      </summary>
      <div className="grid gap-4 border-t border-border-subtle py-5 sm:grid-cols-2">
        {children}
      </div>
    </details>
  )
}
