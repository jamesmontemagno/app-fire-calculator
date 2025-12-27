import { Card, CardHeader, CardContent } from '../components/ui'
import SEO from '../components/SEO'
import { calculatorSEO } from '../config/seo'

interface App {
  title: string
  description: string
  url: string
  imageUrl: string
  category: string
}

const apps: App[] = [
  {
    title: "Tally AI",
    description: "Smart financial companion that helps you track spending, manage budgets, and achieve your money goals with AI-powered insights. Perfect for anyone on their FIRE journey who wants intelligent financial tracking.",
    url: "https://tallyai.money/",
    imageUrl: "https://myfirenumber.com/tallyai.jpg",
    category: "Budget & Tracking"
  },
]

export default function Apps() {
  return (
    <>
      <SEO {...calculatorSEO.apps} />
      <div className="space-y-6">
        {/* Header */}
        <div>
          <h1 className="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-gray-100 flex items-center gap-3">
            <span className="text-3xl" role="img" aria-label="Apps emoji">📱</span>
            Recommended FIRE Apps
          </h1>
          <p className="text-gray-600 dark:text-gray-400 mt-1">
            Essential apps to accelerate your financial independence journey.
          </p>
        </div>

        {/* Info Banner */}
        <div className="bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-xl p-4">
          <div className="flex gap-3">
            <span className="text-2xl" role="img" aria-label="Light bulb emoji">💡</span>
            <div>
            <h3 className="font-semibold text-amber-900 dark:text-amber-100">Smart Tools for Your Journey</h3>
            <p className="text-sm text-amber-700 dark:text-amber-300 mt-1">
              These apps complement your FIRE calculators with practical tools for budgeting, 
              tracking, and managing your finances. More recommendations coming soon!
            </p>
          </div>
        </div>
      </div>

      {/* Apps Grid */}
      <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-6">
        {apps.map((app) => (
          <a
            key={app.title}
            href={app.url}
            target="_blank"
            rel="noopener noreferrer"
            className="group"
          >
            <Card className="h-full hover:shadow-lg transition-shadow duration-200 hover:border-fire-300 dark:hover:border-fire-700">
              <CardContent className="p-4">
                <div className="aspect-video mb-4 overflow-hidden rounded-lg bg-gray-100 dark:bg-gray-800">
                  <img
                    src={app.imageUrl}
                    alt={`${app.title} app screenshot`}
                    className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-200"
                    loading="lazy"
                  />
                </div>
                <div className="mb-2">
                  <span className="inline-block px-2 py-1 text-xs font-medium text-fire-700 dark:text-fire-400 bg-fire-50 dark:bg-fire-900/30 rounded">
                    {app.category}
                  </span>
                </div>
                <h3 className="font-semibold text-gray-900 dark:text-gray-100 group-hover:text-fire-600 dark:group-hover:text-fire-400 transition-colors">
                  {app.title}
                </h3>
                <p className="text-sm text-gray-600 dark:text-gray-300 mt-2">
                  {app.description}
                </p>
                <div className="mt-4 flex items-center gap-2 text-fire-600 dark:text-fire-400 text-sm font-medium">
                  <span>Visit Website</span>
                  <svg className="w-4 h-4 group-hover:translate-x-1 transition-transform" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M14 5l7 7m0 0l-7 7m7-7H3" />
                  </svg>
                </div>
              </CardContent>
            </Card>
          </a>
        ))}
      </div>

      {/* Disclaimer */}
      <Card className="bg-gray-50 dark:bg-gray-800/50 border-gray-200 dark:border-gray-700">
        <CardHeader>
          <h2 className="text-sm font-semibold text-gray-700 dark:text-gray-300">Affiliate Disclosure</h2>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-gray-600 dark:text-gray-400">
            Some of the links above may be affiliate links. If you sign up or purchase through these links, we may earn a small 
            commission at no additional cost to you. This helps support the development of this free calculator. 
            We only recommend apps we genuinely believe will help you on your FIRE journey.
          </p>
        </CardContent>
      </Card>
    </div>
    </>
  )
}
