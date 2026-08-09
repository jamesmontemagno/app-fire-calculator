export default function Disclaimer() {
  return (
    <div className="mt-8 pt-6 border-t border-gray-200 dark:border-gray-800">
      <div className="bg-gray-50 dark:bg-gray-800/50 rounded-lg p-4 text-xs text-gray-500 dark:text-gray-400">
        <p className="font-semibold text-gray-600 dark:text-gray-300 mb-2">⚠️ Disclaimer</p>
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
