import { TriangleAlert } from 'lucide-react'

interface DisclaimerProps {
  embedded?: boolean
}

export default function Disclaimer({ embedded = false }: DisclaimerProps) {
  return (
    <div className={embedded ? 'border-t border-border-subtle pt-5' : 'mt-8 border-t border-border-subtle pt-6'}>
      <div className={`${embedded ? '' : 'rounded-container bg-surface-sunken p-4'} text-xs text-content-muted`}>
        <p className="mb-2 flex items-center gap-2 font-semibold text-content">
          <TriangleAlert className="h-4 w-4 shrink-0 text-warning" aria-hidden="true" strokeWidth={1.5} />
          Disclaimer
        </p>
        <p className="mb-2">
          This calculator is provided for <strong>educational and informational purposes only</strong>. 
          It is not intended to be, and should not be construed as, financial, investment, tax, or legal advice.
        </p>
        <p className="mb-2">
          The results shown are hypothetical projections based on the inputs you provide and assumptions about 
          future returns, inflation, and other factors. <strong>Actual results will vary</strong> — past 
          performance does not guarantee future results.
        </p>
        <p>
          Before making any financial decisions, please consult with a qualified financial advisor, 
          tax professional, or other appropriate professional who can consider your individual circumstances.
        </p>
      </div>
    </div>
  )
}
