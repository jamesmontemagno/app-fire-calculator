import { useEffect, useId, useRef, useState } from 'react'
import { Info } from 'lucide-react'

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
        className="inline-flex items-center justify-center w-4 h-4 focus:outline-none focus:ring-2 focus-visible:ring-ring rounded-full"
      >
        <Info className="w-4 h-4 text-content-subtle hover:text-content-muted cursor-help" aria-hidden="true" strokeWidth={1.5} />
      </button>
      <span
        id={tooltipId}
        role="tooltip"
        className={`
          absolute left-1/2 -translate-x-1/2 bottom-full mb-2 px-3 py-2 
          bg-content text-white text-xs rounded-control 
          w-max max-w-64 whitespace-normal z-50 text-center
          transition-all
          ${isVisible ? 'opacity-100 visible' : 'opacity-0 invisible'}
        `}
      >
        {content}
        <span className="absolute left-1/2 -translate-x-1/2 top-full border-4 border-transparent border-t-content" />
      </span>
    </span>
  )
}
