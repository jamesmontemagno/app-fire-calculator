interface MobileAppPromoProps {
  title?: string
  description?: string
  className?: string
}

const APP_STORE_URL = 'https://apps.apple.com/us/app/my-fire-number/id6799772608'
const GOOGLE_PLAY_URL = 'https://play.google.com/store/apps/details?id=com.refractored.myfirenumber'
const MICROSOFT_STORE_URL = 'https://apps.microsoft.com/store/detail/9NP4DM6PKP8K'

const downloads = [
  {
    platform: 'iPhone & iPad',
    url: APP_STORE_URL,
    badge: 'app-store.svg',
    alt: 'Download on the App Store',
    width: 119.66407,
    height: 40,
  },
  {
    platform: 'Mac',
    url: APP_STORE_URL,
    badge: 'mac-app-store.svg',
    alt: 'Download on the Mac App Store',
    width: 156.10054,
    height: 40,
  },
  {
    platform: 'Android',
    url: GOOGLE_PLAY_URL,
    badge: 'google-play.svg',
    alt: 'Get it on Google Play',
    width: 238.96,
    height: 70.87,
  },
  {
    platform: 'Windows',
    url: MICROSOFT_STORE_URL,
    badge: 'microsoft-store.svg',
    alt: 'Get it from Microsoft',
    width: 161,
    height: 44,
  },
]

export default function MobileAppPromo({
  title = 'My Fire Number for mobile and desktop',
  description = 'Calculate, plan, and stay focused on financial independence with the official app for iPhone, iPad, Android, Mac, and Windows.',
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
        <div className="min-w-0 flex-1">
          <h2 className="text-xl font-semibold text-content">{title}</h2>
          <p className="mt-2 max-w-2xl text-sm text-content-muted">{description}</p>
          <div className="mt-6 flex flex-wrap gap-6">
            {downloads.map(({ platform, url, badge, alt, width, height }) => (
              <a
                key={platform}
                href={url}
                target="_blank"
                rel="noopener noreferrer"
                aria-label={`Download My Fire Number for ${platform} (opens in a new tab)`}
                className="inline-flex flex-col items-start gap-3 rounded-control focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-4 focus-visible:ring-offset-surface"
              >
                <img
                  src={`${import.meta.env.BASE_URL}store-badges/${badge}`}
                  alt={alt}
                  width={width}
                  height={height}
                  className="h-12 w-auto"
                />
                <span className="text-xs font-medium text-content-muted">{platform}</span>
              </a>
            ))}
          </div>
        </div>
      </div>
    </section>
  )
}
