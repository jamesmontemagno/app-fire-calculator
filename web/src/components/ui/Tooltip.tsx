import { useEffect, useId, useRef, useState } from 'react'

interface TooltipProps {
  content: string
}

export default function Tooltip({ content }: TooltipProps) {
  const [isPinned, setIsPinned] = useState(false)
  const [isHovered, setIsHovered] = useState(false)
  const [isFocused, setIsFocused] = useState(false)
  const tooltipId = useId()
  const containerRef = useRef<HTMLSpanElement>(null)
  const isVisible = isPinned || isHovered || isFocused

  useEffect(() => {
    if (!isVisible) return

    const handlePointerDown = (event: PointerEvent) => {
      if (!containerRef.current?.contains(event.target as Node)) {
        setIsPinned(false)
        setIsHovered(false)
        setIsFocused(false)
      }
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setIsPinned(false)
        setIsHovered(false)
        setIsFocused(false)
      }
    }

    document.addEventListener('pointerdown', handlePointerDown)
    document.addEventListener('keydown', handleKeyDown)

    return () => {
      document.removeEventListener('pointerdown', handlePointerDown)
      document.removeEventListener('keydown', handleKeyDown)
    }
  }, [isVisible])

  const handleClick = () => {
    if (isPinned) {
      setIsPinned(false)
      setIsHovered(false)
      setIsFocused(false)
    } else {
      setIsPinned(true)
    }
  }

  return (
    <span ref={containerRef} className="group relative">
      <button
        type="button"
        aria-label="More information"
        aria-expanded={isVisible}
        aria-controls={tooltipId}
        onClick={handleClick}
        onMouseEnter={() => setIsHovered(true)}
        onMouseLeave={() => setIsHovered(false)}
        onFocus={() => setIsFocused(true)}
        onBlur={() => setIsFocused(false)}
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
          w-max max-w-64 whitespace-normal z-50 text-center
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
