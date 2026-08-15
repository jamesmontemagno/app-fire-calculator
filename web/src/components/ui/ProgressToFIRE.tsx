import { formatCurrency } from '../../utils/calculations'

interface ProgressToFIREProps {
  currentSavings: number
  fireNumber: number
  yearsToFIRE?: number
  showMilestones?: boolean
  label?: string
  targetLabel?: string
}

export default function ProgressToFIRE({ 
  currentSavings, 
  fireNumber, 
  yearsToFIRE,
  showMilestones = true,
  label = 'Progress to FIRE',
  targetLabel = 'FIRE Number',
}: ProgressToFIREProps) {
  // Safeguard against invalid values
  const safeFireNumber = fireNumber > 0 ? fireNumber : 1
  const rawProgress = (currentSavings / safeFireNumber) * 100
  const progress = Math.min(100, rawProgress)
  const displayProgress = rawProgress > 999 ? '>999' : rawProgress.toFixed(1)
  
  // Milestone percentages
  const milestones = [25, 50, 75, 100]
  
  // Determine status message
  let statusMessage = ''
  let statusColor = ''
  
  if (progress >= 100) {
    statusMessage = "You've reached FIRE!"
    statusColor = 'text-success'
  } else if (progress >= 75) {
    statusMessage = "Almost there! Final stretch!"
    statusColor = 'text-warning'
  } else if (progress >= 50) {
    statusMessage = "Halfway to freedom!"
    statusColor = 'text-info'
  } else if (progress >= 25) {
    statusMessage = "Great progress! Keep going!"
    statusColor = 'text-accent'
  } else {
    statusMessage = "Journey started!"
    statusColor = 'text-content-muted'
  }

  return (
    <div className="bg-surface-raised border border-border-subtle rounded-container p-4">
      {/* Header */}
      <div className="flex items-center justify-between mb-3">
        <div>
          <h3 className="text-sm font-semibold text-content-muted">{label}</h3>
          <p className={`text-sm font-medium ${statusColor}`}>{statusMessage}</p>
        </div>
        <div className="text-right">
          <p className="text-2xl font-bold text-content">
            {displayProgress}%
          </p>
          {yearsToFIRE !== undefined && Number.isFinite(yearsToFIRE) && yearsToFIRE > 0 && (
            <p className="text-xs text-content-subtle">
              ~{yearsToFIRE.toFixed(1)} years to go
            </p>
          )}
        </div>
      </div>

      {/* Progress Bar */}
      <div className="relative">
        <div 
          className="h-4 bg-surface-sunken rounded-full overflow-hidden"
          role="progressbar"
          aria-valuenow={Math.round(progress)}
          aria-valuemin={0}
          aria-valuemax={100}
          aria-label={`${displayProgress}% progress to FIRE goal`}
        >
          <div 
            className="h-full rounded-full bg-accent transition-[width] duration-500 ease-out motion-reduce:transition-none"
            style={{ width: `${progress}%` }}
          />
        </div>
        
        {/* Milestone markers */}
        {showMilestones && (
          <div className="absolute top-0 left-0 right-0 h-4 pointer-events-none">
            {milestones.slice(0, -1).map((milestone) => (
              <div
                key={milestone}
                className="absolute top-0 bottom-0 w-0.5 bg-border-strong"
                style={{ left: `${milestone}%` }}
              >
                <span className="absolute -top-5 left-1/2 -translate-x-1/2 text-xs text-content-subtle">
                  {milestone}%
                </span>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Stats */}
      <div className="flex justify-between mt-3 text-sm">
        <div>
          <p className="text-content-subtle">Current</p>
          <p className="font-semibold text-content">{formatCurrency(currentSavings)}</p>
        </div>
        <div className="text-right">
          <p className="text-content-subtle">{targetLabel}</p>
          <p className="font-semibold text-content">{formatCurrency(fireNumber)}</p>
        </div>
      </div>
    </div>
  )
}
