import { Apple, Play } from 'lucide-react'

interface MobileAppPromoProps {
  title?: string
  description?: string
  className?: string
}

const APP_STORE_URL = 'https://apps.apple.com/us/app/my-fire-number/id6799772608'
const GOOGLE_PLAY_URL = 'https://play.google.com/store/apps/details?id=com.refractored.myfirenumber'

export default function MobileAppPromo({
  title = 'My Fire Number is now on your phone',
  description = 'Take the official My Fire Number app with you to calculate, plan, and stay focused on your path to financial independence.',
  className = '',
}: MobileAppPromoProps) {
  return (
    <section className={`rounded-container border border-border-subtle bg-surface-raised p-6 sm:p-8 ${className}`}>
      <div className="flex flex-col gap-6 sm:flex-row sm:items-center">
        <div className="h-16 w-16 shrink-0 overflow-hidden rounded-container border border-border-subtle bg-surface-sunken shadow-sm">
          <img
            src={`${import.meta.env.BASE_URL}my-fire-number-app-icon.png`}
            alt="My Fire Number app icon"
            className="h-full w-full object-cover"
          />
        </div>
        <div className="flex-1">
          <h2 className="text-xl font-semibold text-content">{title}</h2>
          <p className="mt-2 max-w-2xl text-sm text-content-muted">{description}</p>
          <div className="mt-5 flex flex-col gap-3 sm:flex-row">
            <a
              href={APP_STORE_URL}
              target="_blank"
              rel="noopener noreferrer"
              className="inline-flex h-14 w-44 items-center gap-3 rounded-container border border-border-subtle bg-surface-sunken px-4 text-left transition-colors hover:border-border-strong hover:bg-accent-subtle motion-reduce:transition-none focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-surface"
            >
              <Apple className="h-6 w-6 shrink-0 fill-current text-content" aria-hidden="true" />
              <span className="leading-tight">
                <span className="block text-xs text-content-muted">Download on the</span>
                <strong className="block text-sm font-semibold text-content">App Store</strong>
              </span>
            </a>
            <a
              href={GOOGLE_PLAY_URL}
              target="_blank"
              rel="noopener noreferrer"
              className="inline-flex h-14 w-44 items-center gap-3 rounded-container border border-border-subtle bg-surface-sunken px-4 text-left transition-colors hover:border-border-strong hover:bg-accent-subtle motion-reduce:transition-none focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-surface"
            >
              <Play className="h-6 w-6 shrink-0 fill-current text-content" aria-hidden="true" />
              <span className="leading-tight">
                <span className="block text-xs text-content-muted">Get it on</span>
                <strong className="block text-sm font-semibold text-content">Google Play</strong>
              </span>
            </a>
          </div>
        </div>
      </div>
    </section>
  )
}
