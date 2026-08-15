import { useEffect, useState, useRef } from 'react'
import { usePrefersReducedMotion } from '../../hooks/usePrefersReducedMotion'

interface AnimatedNumberProps {
  value: number
  format?: 'currency' | 'percent' | 'years' | 'number'
  duration?: number
  className?: string
}

export default function AnimatedNumber({
  value,
  format = 'number',
  duration = 500,
  className = '',
}: AnimatedNumberProps) {
  const [displayValue, setDisplayValue] = useState(value)
  const previousValue = useRef(value)
  const animationRef = useRef<number | undefined>(undefined)
  const prefersReducedMotion = usePrefersReducedMotion()

  useEffect(() => {
    // A count-up is decorative: the final figure is the information. Honour the
    // preference by jumping straight to it rather than animating faster.
    if (prefersReducedMotion) {
      previousValue.current = value
      setDisplayValue(value)
      return
    }

    const startValue = previousValue.current
    const endValue = value
    const startTime = performance.now()

    const animate = (currentTime: number) => {
      const elapsed = currentTime - startTime
      const progress = Math.min(elapsed / duration, 1)
      
      // Ease out cubic
      const easeProgress = 1 - Math.pow(1 - progress, 3)
      
      const currentValue = startValue + (endValue - startValue) * easeProgress
      setDisplayValue(currentValue)

      if (progress < 1) {
        animationRef.current = requestAnimationFrame(animate)
      } else {
        previousValue.current = endValue
      }
    }

    animationRef.current = requestAnimationFrame(animate)

    return () => {
      if (animationRef.current) {
        cancelAnimationFrame(animationRef.current)
      }
    }
  }, [value, duration, prefersReducedMotion])

  const formatValue = () => {
    switch (format) {
      case 'currency':
        return new Intl.NumberFormat('en-US', {
          style: 'currency',
          currency: 'USD',
          minimumFractionDigits: 0,
          maximumFractionDigits: 0,
        }).format(displayValue)
      case 'percent':
        return `${(displayValue * 100).toFixed(1)}%`
      case 'years':
        return `${displayValue.toFixed(1)} years`
      default:
        return displayValue.toLocaleString(undefined, { maximumFractionDigits: 0 })
    }
  }

  return <span className={`tabular ${className}`}>{formatValue()}</span>
}
