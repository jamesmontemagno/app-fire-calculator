import { useRegisterSW } from 'virtual:pwa-register/react'

export default function UpdatePrompt() {
  const {
    needRefresh: [needRefresh, setNeedRefresh],
    updateServiceWorker,
  } = useRegisterSW({
    onRegisteredSW(swUrl: string) {
      console.log('Service Worker registered:', swUrl)
    },
    onRegisterError(error: Error) {
      console.error('Service Worker registration error:', error)
    },
  })

  if (!needRefresh) {
    return null
  }

  return (
    <div className="fixed bottom-4 left-1/2 -translate-x-1/2 z-50 max-w-md w-full mx-4">
      <div className="bg-surface-raised rounded-control shadow-lg border border-border-subtle p-4 flex items-center gap-4">
        <div className="flex-1">
          <p className="text-sm font-medium text-content">
            New version available!
          </p>
          <p className="text-xs text-content-muted mt-1">
            Refresh to get the latest features and improvements.
          </p>
        </div>
        <div className="flex gap-2">
          <button
            onClick={() => setNeedRefresh(false)}
            className="px-3 py-1.5 text-sm text-content-muted hover:text-content transition-colors"
          >
            Later
          </button>
          <button
            onClick={() => updateServiceWorker(true)}
            className="px-3 py-1.5 text-sm bg-orange-500 hover:bg-orange-600 text-white rounded-control transition-colors font-medium"
          >
            Refresh
          </button>
        </div>
      </div>
    </div>
  )
}
