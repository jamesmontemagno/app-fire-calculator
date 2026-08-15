import { Lightbulb } from 'lucide-react'
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
  {
    title: "Track Your Dividends",
    description: "Comprehensive dividend tracking platform that helps you monitor your dividend income, analyze portfolio performance, and project future passive income. Ideal for dividend-focused FIRE strategies.",
    url: "https://trackyourdividends.com/",
    imageUrl: "https://myfirenumber.com/trackyourdividends.png",
    category: "Investment Tracking"
  },
]

export default function Apps() {
  return (
    <>
      <SEO {...calculatorSEO.apps} />
      <div className="space-y-6">
        {/* Header */}
        <div>
          <h1 className="text-2xl font-bold text-content sm:text-3xl">Recommended FIRE Apps</h1>
          <p className="text-content-muted mt-1">
            Essential apps to accelerate your financial independence journey.
          </p>
        </div>

        {/* Info Banner */}
        <div className="bg-warning-subtle border border-warning/30 rounded-container p-4">
          <div className="flex gap-3">
            <Lightbulb className="h-5 w-5 shrink-0 text-warning" aria-hidden="true" />
            <div>
            <h3 className="font-semibold text-content">Smart Tools for Your Journey</h3>
            <p className="text-sm text-warning mt-1">
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
            <Card className="h-full transition-colors duration-200 hover:border-border-strong motion-reduce:transition-none">
              <CardContent className="p-4">
                <div className="aspect-video mb-4 overflow-hidden rounded-control bg-surface-sunken">
                  <img
                    src={app.imageUrl}
                    alt={`${app.title} app screenshot`}
                    className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-200"
                    loading="lazy"
                  />
                </div>
                <div className="mb-2">
                  <span className="inline-block px-2 py-1 text-xs font-medium text-accent bg-accent-subtle rounded">
                    {app.category}
                  </span>
                </div>
                <h3 className="font-semibold text-content group-hover:text-accent transition-colors">
                  {app.title}
                </h3>
                <p className="text-sm text-content-muted mt-2">
                  {app.description}
                </p>
                <div className="mt-4 flex items-center gap-2 text-accent text-sm font-medium">
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
      <Card className="bg-surface-sunken border-border-subtle">
        <CardHeader>
          <h2 className="text-sm font-semibold text-content-muted">Disclaimer</h2>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-content-muted">
            We are not affiliated with or endorsed by any of the apps listed above. These recommendations are provided 
            for informational purposes only. We only recommend apps we genuinely believe will help you on your FIRE journey. 
            Please do your own research before using any third-party service.
          </p>
        </CardContent>
      </Card>
    </div>
    </>
  )
}
