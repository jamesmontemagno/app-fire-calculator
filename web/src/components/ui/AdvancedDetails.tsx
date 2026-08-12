import { type ReactNode, useId } from 'react'

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
    <details className={`group border-y border-gray-200 dark:border-gray-800 ${className}`}>
      <summary
        className="flex cursor-pointer list-none items-center justify-between gap-4 py-4 text-left font-semibold text-gray-900 marker:hidden dark:text-gray-100 focus:outline-none focus-visible:ring-2 focus-visible:ring-fire-500 focus-visible:ring-offset-2 dark:focus-visible:ring-offset-gray-950"
        aria-describedby={descriptionId}
      >
        <span>
          {summary}
          <span id={descriptionId} className="mt-0.5 block text-sm font-normal text-gray-600 dark:text-gray-400">
            {description}
          </span>
        </span>
        <svg
          className="h-5 w-5 shrink-0 text-gray-500 transition-transform duration-200 motion-reduce:transition-none group-open:rotate-180 dark:text-gray-400"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
          aria-hidden="true"
        >
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="m6 9 6 6 6-6" />
        </svg>
      </summary>
      <div className="grid gap-4 border-t border-gray-200 py-5 sm:grid-cols-2 dark:border-gray-800">
        {children}
      </div>
    </details>
  )
}
