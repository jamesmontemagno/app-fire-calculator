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
    statusColor = 'text-green-600 dark:text-green-400'
  } else if (progress >= 75) {
    statusMessage = "Almost there! Final stretch!"
    statusColor = 'text-orange-600 dark:text-orange-400'
  } else if (progress >= 50) {
    statusMessage = "Halfway to freedom!"
    statusColor = 'text-blue-600 dark:text-blue-400'
  } else if (progress >= 25) {
    statusMessage = "Great progress! Keep going!"
    statusColor = 'text-purple-600 dark:text-purple-400'
  } else {
    statusMessage = "Journey started!"
    statusColor = 'text-gray-600 dark:text-gray-400'
  }

  return (
    <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-xl p-4">
      {/* Header */}
      <div className="flex items-center justify-between mb-3">
        <div>
          <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300">{label}</h3>
          <p className={`text-sm font-medium ${statusColor}`}>{statusMessage}</p>
        </div>
        <div className="text-right">
          <p className="text-2xl font-bold text-gray-900 dark:text-gray-100">
            {displayProgress}%
          </p>
          {yearsToFIRE !== undefined && yearsToFIRE !== Infinity && yearsToFIRE > 0 && (
            <p className="text-xs text-gray-500 dark:text-gray-400">
              ~{yearsToFIRE.toFixed(1)} years to go
            </p>
          )}
        </div>
      </div>

      {/* Progress Bar */}
      <div className="relative">
        <div 
          className="h-4 bg-gray-100 dark:bg-gray-800 rounded-full overflow-hidden"
          role="progressbar"
          aria-valuenow={Math.round(progress)}
          aria-valuemin={0}
          aria-valuemax={100}
          aria-label={`${displayProgress}% progress to FIRE goal`}
        >
          <div 
            className="h-full bg-gradient-to-r from-fire-400 via-fire-500 to-fire-600 rounded-full transition-all duration-500 ease-out"
            style={{ width: `${progress}%` }}
          />
        </div>
        
        {/* Milestone markers */}
        {showMilestones && (
          <div className="absolute top-0 left-0 right-0 h-4 pointer-events-none">
            {milestones.slice(0, -1).map((milestone) => (
              <div
                key={milestone}
                className="absolute top-0 bottom-0 w-0.5 bg-gray-300 dark:bg-gray-600"
                style={{ left: `${milestone}%` }}
              >
                <span className="absolute -top-5 left-1/2 -translate-x-1/2 text-xs text-gray-400 dark:text-gray-500">
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
          <p className="text-gray-500 dark:text-gray-400">Current</p>
          <p className="font-semibold text-gray-900 dark:text-gray-100">{formatCurrency(currentSavings)}</p>
        </div>
        <div className="text-right">
          <p className="text-gray-500 dark:text-gray-400">{targetLabel}</p>
          <p className="font-semibold text-gray-900 dark:text-gray-100">{formatCurrency(fireNumber)}</p>
        </div>
      </div>
    </div>
  )
}
