import { useState, useId } from 'react'

interface TooltipProps {
  content: string
}

export default function Tooltip({ content }: TooltipProps) {
  const [isVisible, setIsVisible] = useState(false)
  const tooltipId = useId()

  return (
    <span className="group relative">
      <button
        type="button"
        aria-label="More information"
        aria-describedby={tooltipId}
        onMouseEnter={() => setIsVisible(true)}
        onMouseLeave={() => setIsVisible(false)}
        onFocus={() => setIsVisible(true)}
        onBlur={() => setIsVisible(false)}
        className="inline-flex items-center justify-center w-4 h-4 focus:outline-none focus:ring-2 focus:ring-fire-500 rounded-full"
      >
        <svg 
          className="w-4 h-4 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 cursor-help" 
          fill="none" 
          viewBox="0 0 24 24" 
          stroke="currentColor"
          aria-hidden="true"
        >
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
      </button>
      <span
        id={tooltipId}
        role="tooltip"
        className={`
          absolute left-1/2 -translate-x-1/2 bottom-full mb-2 px-3 py-2 
          bg-gray-900 dark:bg-gray-700 text-white text-xs rounded-lg 
          whitespace-nowrap z-50 max-w-xs text-center
          transition-all
          ${isVisible ? 'opacity-100 visible' : 'opacity-0 invisible'}
        `}
      >
        {content}
        <span className="absolute left-1/2 -translate-x-1/2 top-full border-4 border-transparent border-t-gray-900 dark:border-t-gray-700" />
      </span>
    </span>
  )
}
