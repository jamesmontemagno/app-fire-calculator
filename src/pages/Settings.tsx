import { useEffect, useState } from 'react'
import SEO from '../components/SEO'
import { Button, Card, CardContent, CardHeader } from '../components/ui'
import ConfirmationDialog from '../components/ui/ConfirmationDialog'
import { calculators } from '../config/calculators'
import {
  DEFERRED_STORAGE_KEY_PREFIX,
  STANDARD_STORAGE_KEY_PREFIX,
} from '../utils/savedCalculationStorage'

interface SavedCalculation {
  key: string
  section: string
  savedAt: string | null
}

function getSavedCalculations(): SavedCalculation[] {
  if (typeof window === 'undefined') return []

  return calculators.flatMap(calculator => {
    const prefix = calculator.storagePrefix === 'deferred'
      ? DEFERRED_STORAGE_KEY_PREFIX
      : STANDARD_STORAGE_KEY_PREFIX
    const key = `${prefix}:${calculator.path}`
    const value = localStorage.getItem(key)
    if (!value) return []

    try {
      const parsed: unknown = JSON.parse(value)
      const savedAt = parsed && typeof parsed === 'object' && 'savedAt' in parsed
        && typeof parsed.savedAt === 'string' && !Number.isNaN(Date.parse(parsed.savedAt))
        ? parsed.savedAt
        : null
      return [{ key, section: calculator.name, savedAt }]
    } catch {
      return [{ key, section: calculator.name, savedAt: null }]
    }
  })
}

export default function Settings() {
  const [savedCalculations, setSavedCalculations] = useState<SavedCalculation[]>(getSavedCalculations)
  const [confirmingClearAll, setConfirmingClearAll] = useState(false)

  useEffect(() => {
    const updateSavedCalculations = () => setSavedCalculations(getSavedCalculations())
    window.addEventListener('storage', updateSavedCalculations)
    return () => window.removeEventListener('storage', updateSavedCalculations)
  }, [])

  const deleteCalculation = (key: string) => {
    localStorage.removeItem(key)
    setSavedCalculations(getSavedCalculations())
  }

  const clearAllCalculations = () => {
    savedCalculations.forEach(({ key }) => localStorage.removeItem(key))
    setSavedCalculations([])
    setConfirmingClearAll(false)
  }

  return (
    <>
      <SEO title="Settings | FIRE Calculator" description="Manage calculator data saved locally in your browser." />
      <div className="space-y-6">
        <div>
          <h1 className="flex items-center gap-3 text-2xl font-bold text-gray-900 dark:text-gray-100 sm:text-3xl">
            <span role="img" aria-label="Settings">⚙️</span>
            Settings
          </h1>
          <p className="mt-1 text-gray-600 dark:text-gray-400">
            Manage calculations saved locally in this browser.
          </p>
        </div>

        <Card>
          <CardHeader>
            <h2 className="font-semibold text-gray-900 dark:text-gray-100">Saved calculations</h2>
            <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
              This data stays on this device and is never sent to a server.
            </p>
          </CardHeader>
          <CardContent>
            {savedCalculations.length === 0 ? (
              <p className="text-sm text-gray-600 dark:text-gray-400">No calculator data is saved in this browser.</p>
            ) : (
              <>
                <div className="overflow-x-auto">
                  <table className="w-full text-left text-sm">
                    <thead className="border-b border-gray-200 text-gray-500 dark:border-gray-800 dark:text-gray-400">
                      <tr>
                        <th scope="col" className="pb-3 font-medium">Section</th>
                        <th scope="col" className="pb-3 font-medium">Last saved</th>
                        <th scope="col" className="pb-3 text-right font-medium">Actions</th>
                      </tr>
                    </thead>
                    <tbody>
                      {savedCalculations.map(calculation => (
                        <tr key={calculation.key} className="border-b border-gray-100 dark:border-gray-800">
                          <td className="py-4 font-medium text-gray-900 dark:text-gray-100">{calculation.section}</td>
                          <td className="py-4 text-gray-600 dark:text-gray-400">
                            {calculation.savedAt ? new Date(calculation.savedAt).toLocaleString() : 'Date unavailable'}
                          </td>
                          <td className="py-4 text-right">
                            <Button variant="ghost" size="sm" onClick={() => deleteCalculation(calculation.key)}>
                              Delete
                            </Button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
                <div className="mt-4 flex justify-end">
                  <Button variant="outline" onClick={() => setConfirmingClearAll(true)}>
                    Delete all saved calculations
                  </Button>
                </div>
              </>
            )}
          </CardContent>
        </Card>
      </div>

      {confirmingClearAll && (
        <ConfirmationDialog
          title="Delete all saved calculations?"
          confirmLabel="Delete all"
          onCancel={() => setConfirmingClearAll(false)}
          onConfirm={clearAllCalculations}
        >
          This permanently removes all saved calculator data from this browser.
        </ConfirmationDialog>
      )}
    </>
  )
}
