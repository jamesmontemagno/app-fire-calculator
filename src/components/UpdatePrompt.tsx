import { useEffect, useState } from 'react'
import { useRegisterSW } from 'virtual:pwa-register/react'

export default function UpdatePrompt() {
  const [showReload, setShowReload] = useState(false)
  
  const {
    needRefresh: [needRefresh],
    updateServiceWorker,
  } = useRegisterSW({
    onRegisteredSW(swUrl: string) {
      console.log('Service Worker registered:', swUrl)
    },
    onRegisterError(error: Error) {
      console.error('Service Worker registration error:', error)
    },
  })

  useEffect(() => {
    if (needRefresh) {
      setShowReload(true)
    }
  }, [needRefresh])

  if (!showReload) {
    return null
  }

  return (
    <div className="fixed bottom-4 left-1/2 -translate-x-1/2 z-50 max-w-md w-full mx-4">
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-lg border border-gray-200 dark:border-gray-700 p-4 flex items-center gap-4">
        <div className="flex-1">
          <p className="text-sm font-medium text-gray-900 dark:text-gray-100">
            New version available!
          </p>
          <p className="text-xs text-gray-600 dark:text-gray-400 mt-1">
            Refresh to get the latest features and improvements.
          </p>
        </div>
        <div className="flex gap-2">
          <button
            onClick={() => setShowReload(false)}
            className="px-3 py-1.5 text-sm text-gray-700 dark:text-gray-300 hover:text-gray-900 dark:hover:text-gray-100 transition-colors"
          >
            Later
          </button>
          <button
            onClick={() => updateServiceWorker(true)}
            className="px-3 py-1.5 text-sm bg-orange-500 hover:bg-orange-600 text-white rounded-lg transition-colors font-medium"
          >
            Refresh
          </button>
        </div>
      </div>
    </div>
  )
}
