import { Link } from 'react-router-dom'
import { Card, CardContent } from '../components/ui'

const calculators = [
  {
    path: '/standard',
    icon: '🎯',
    name: 'Standard FIRE',
    description: 'The classic 25x expenses rule — calculate your "magic number" for full financial independence.',
    color: 'from-orange-500 to-red-500',
    bgColor: 'bg-orange-50 dark:bg-orange-900/20',
    borderColor: 'border-orange-200 dark:border-orange-800',
    audience: 'Best for: Anyone starting their FI journey',
  },
  {
    path: '/coast',
    icon: '⛵',
    name: 'Coast FIRE',
    description: 'Find how much you need now so compound growth does the rest — then coast to retirement.',
    color: 'from-blue-500 to-cyan-500',
    bgColor: 'bg-blue-50 dark:bg-blue-900/20',
    borderColor: 'border-blue-200 dark:border-blue-800',
    audience: 'Best for: Young savers wanting flexibility',
  },
  {
    path: '/lean',
    icon: '🌿',
    name: 'Lean FIRE',
    description: 'Achieve FI faster with a minimalist lifestyle — perfect for frugal-minded planners.',
    color: 'from-green-500 to-emerald-500',
    bgColor: 'bg-green-50 dark:bg-green-900/20',
    borderColor: 'border-green-200 dark:border-green-800',
    audience: 'Best for: Minimalists & early retirees',
  },
  {
    path: '/fat',
    icon: '💎',
    name: 'Fat FIRE',
    description: 'Retire without compromise — calculate FI while maintaining a comfortable lifestyle.',
    color: 'from-purple-500 to-pink-500',
    bgColor: 'bg-purple-50 dark:bg-purple-900/20',
    borderColor: 'border-purple-200 dark:border-purple-800',
    audience: 'Best for: High earners & luxury seekers',
  },
  {
    path: '/barista',
    icon: '☕',
    name: 'Barista FIRE',
    description: 'Blend part-time work with portfolio income — retire from corporate life earlier.',
    color: 'from-amber-500 to-orange-500',
    bgColor: 'bg-amber-50 dark:bg-amber-900/20',
    borderColor: 'border-amber-200 dark:border-amber-800',
    audience: 'Best for: Those wanting work-life balance',
  },
  {
    path: '/reverse',
    icon: '🔄',
    name: 'Reverse FIRE',
    description: 'Work backwards — set your target age and find out how much you need to save monthly.',
    color: 'from-teal-500 to-cyan-500',
    bgColor: 'bg-teal-50 dark:bg-teal-900/20',
    borderColor: 'border-teal-200 dark:border-teal-800',
    audience: 'Best for: Goal-oriented planners',
  },
  {
    path: '/withdrawal',
    icon: '📊',
    name: 'Withdrawal Rate',
    description: 'Test your portfolio\'s longevity — find your safe withdrawal rate for any scenario.',
    color: 'from-sky-500 to-blue-500',
    bgColor: 'bg-sky-50 dark:bg-sky-900/20',
    borderColor: 'border-sky-200 dark:border-sky-800',
    audience: 'Best for: Those at or near FIRE',
  },
  {
    path: '/savings-rate',
    icon: '🧮',
    name: 'Savings & Investment Rate',
    description: 'The most important metric — see how your savings rate impacts your time to FIRE.',
    color: 'from-indigo-500 to-purple-500',
    bgColor: 'bg-indigo-50 dark:bg-indigo-900/20',
    borderColor: 'border-indigo-200 dark:border-indigo-800',
    audience: 'Best for: Understanding your FI timeline',
  },
  {
    path: '/debt-payoff',
    icon: '💳',
    name: 'Debt Payoff',
    description: 'Eliminate debt faster with Snowball or Avalanche strategies — compare methods and see the impact of extra payments.',
    color: 'from-red-500 to-rose-500',
    bgColor: 'bg-red-50 dark:bg-red-900/20',
    borderColor: 'border-red-200 dark:border-red-800',
    audience: 'Best for: Tackling multiple debts strategically',
  },
  {
    path: '/healthcare',
    icon: '🏥',
    name: 'Healthcare Gap',
    description: 'The hidden cost of early retirement — estimate healthcare costs before Medicare.',
    color: 'from-rose-500 to-pink-500',
    bgColor: 'bg-rose-50 dark:bg-rose-900/20',
    borderColor: 'border-rose-200 dark:border-rose-800',
    audience: 'Best for: US-based early retirees',
  },
]

export default function Home() {
  return (
    <div className="space-y-12">
      {/* Hero Section */}
      <div className="text-center py-8">
        <div className="flex justify-center mb-6">
          <span className="text-5xl animate-pulse">🔥</span>
        </div>
        <h1 className="text-4xl sm:text-5xl font-bold text-gray-900 dark:text-gray-100 mb-4">
          FIRE Calculators
        </h1>
        <p className="text-xl text-gray-600 dark:text-gray-400 max-w-2xl mx-auto mb-6">
          Plan your path to <strong>Financial Independence, Retire Early</strong>. 
          Free, private, and works completely offline.
        </p>
        
        {/* Privacy badges */}
        <div className="flex flex-wrap justify-center gap-3 mb-8">
          <span className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-300 rounded-full text-sm font-medium">
            <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
            </svg>
            100% Private
          </span>
          <span className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300 rounded-full text-sm font-medium">
            <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8.111 16.404a5.5 5.5 0 017.778 0M12 20h.01m-7.08-7.071c3.904-3.905 10.236-3.905 14.141 0M1.394 9.393c5.857-5.857 15.355-5.857 21.213 0" />
            </svg>
            Works Offline
          </span>
          <span className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-purple-100 dark:bg-purple-900/30 text-purple-700 dark:text-purple-300 rounded-full text-sm font-medium">
            <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21" />
            </svg>
            No Tracking
          </span>
          <span className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-orange-100 dark:bg-orange-900/30 text-orange-700 dark:text-orange-300 rounded-full text-sm font-medium">
            <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1" />
            </svg>
            Shareable URLs
          </span>
        </div>
      </div>

      {/* Quiz CTA */}
      <div className="bg-gradient-to-br from-fire-50 via-orange-50 to-amber-50 dark:from-fire-900/20 dark:via-orange-900/20 dark:to-amber-900/20 rounded-2xl p-8 border-2 border-fire-200 dark:border-fire-800">
        <div className="max-w-3xl mx-auto text-center">
          <span className="text-4xl mb-4 block">🧭</span>
          <h2 className="text-2xl font-bold text-gray-900 dark:text-gray-100 mb-3">
            Not sure which calculator to use?
          </h2>
          <p className="text-lg text-gray-600 dark:text-gray-400 mb-6">
            Take our quick quiz to find the perfect FIRE path for your situation. 
            Answer a few questions and we'll recommend the best calculator with your information pre-filled.
          </p>
          <Link
            to="/quiz"
            className="inline-flex items-center gap-2 px-8 py-4 bg-fire-600 hover:bg-fire-700 text-white text-lg font-semibold rounded-lg transition-colors shadow-lg hover:shadow-xl"
          >
            Find Your FIRE Path
            <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
            </svg>
          </Link>
          <p className="text-sm text-gray-500 dark:text-gray-400 mt-4">
            Takes 2-3 minutes · Personalized recommendation
          </p>
        </div>
      </div>

      {/* Calculator Grid */}
      <div>
        <h2 className="text-2xl font-bold text-gray-900 dark:text-gray-100 mb-6 text-center">
          Choose Your Calculator
        </h2>
        <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-6">
          {calculators.map((calc) => (
            <Link key={calc.path} to={calc.path} className="group">
              <Card className={`h-full transition-all duration-200 hover:shadow-lg hover:scale-[1.02] ${calc.borderColor} border-2`}>
                <CardContent className="p-6">
                  <div className="flex items-start gap-4">
                    <div className={`p-3 rounded-xl ${calc.bgColor}`}>
                      <span className="text-3xl">{calc.icon}</span>
                    </div>
                    <div className="flex-1 min-w-0">
                      <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 group-hover:text-fire-600 dark:group-hover:text-fire-400 transition-colors">
                        {calc.name}
                      </h3>
                      <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                        {calc.description}
                      </p>
                      <p className="text-xs text-gray-500 dark:text-gray-500 mt-3 font-medium">
                        {calc.audience}
                      </p>
                    </div>
                  </div>
                  <div className="mt-4 flex items-center text-sm font-medium text-fire-600 dark:text-fire-400 group-hover:translate-x-1 transition-transform">
                    Start calculating
                    <svg className="w-4 h-4 ml-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                    </svg>
                  </div>
                </CardContent>
              </Card>
            </Link>
          ))}
        </div>
      </div>

      {/* Recommended Books Section */}
      <div className="bg-gradient-to-br from-amber-50 to-orange-50 dark:from-amber-900/20 dark:to-orange-900/20 rounded-2xl p-8">
        <div className="text-center mb-8">
          <span className="text-4xl mb-4 block">📚</span>
          <h2 className="text-2xl font-bold text-gray-900 dark:text-gray-100 mb-2">
            Recommended FIRE Books
          </h2>
          <p className="text-gray-600 dark:text-gray-400">
            Essential reading to accelerate your financial independence journey.
          </p>
        </div>
        
        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-7 gap-4 mb-6">
          {[
            { title: "I Will Teach You to Be Rich", author: "Ramit Sethi", url: "https://amzn.to/3N1SrtP", image: "https://m.media-amazon.com/images/I/81c9SSbG3OL._SL1500_.jpg" },
            { title: "Money for Couples", author: "Ramit Sethi", url: "https://amzn.to/4pQ81Hn", image: "https://m.media-amazon.com/images/I/81G3ygJ-jOL._SL1500_.jpg" },
            { title: "The Psychology of Money", author: "Morgan Housel", url: "https://amzn.to/3Y74Jn9", image: "https://m.media-amazon.com/images/I/81Dky+tD+pL._SY522_.jpg" },
            { title: "The Bogleheads' Guide to Investing", author: "Larimore et al.", url: "https://amzn.to/3MXrOWU", image: "https://m.media-amazon.com/images/I/611brjp7lgL._SL1200_.jpg" },
            { title: "We Need to Talk", author: "Jennifer Risher", url: "https://amzn.to/3Y74Ij5", image: "https://m.media-amazon.com/images/I/81KH2bo+b0L._SL1500_.jpg" },
            { title: "Die with Zero", author: "Bill Perkins", url: "https://amzn.to/3LgBMlK", image: "https://m.media-amazon.com/images/I/61+4EHZ4faL._SL1500_.jpg" },
            { title: "The Little Book of Common Sense Investing", author: "John C. Bogle", url: "https://amzn.to/4pdtMQq", image: "https://m.media-amazon.com/images/I/81vPxCvGMcL._SL1500_.jpg" },
          ].map((book) => (
            <a
              key={book.title}
              href={book.url}
              target="_blank"
              rel="noopener noreferrer"
              className="group"
            >
              <div className="aspect-[2/3] rounded-lg overflow-hidden shadow-md group-hover:shadow-xl transition-shadow duration-200 bg-white dark:bg-gray-800">
                <img
                  src={book.image}
                  alt={book.title}
                  className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-200"
                  loading="lazy"
                />
              </div>
              <p className="mt-2 text-xs font-medium text-gray-700 dark:text-gray-300 line-clamp-1 group-hover:text-fire-600 dark:group-hover:text-fire-400 transition-colors">
                {book.title}
              </p>
              <p className="text-xs text-gray-500 dark:text-gray-400 line-clamp-1">
                {book.author}
              </p>
            </a>
          ))}
        </div>
        
        <div className="text-center">
          <Link
            to="/books"
            className="inline-flex items-center gap-2 px-6 py-3 bg-fire-600 hover:bg-fire-700 text-white font-medium rounded-lg transition-colors"
          >
            View All Books & Details
            <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M14 5l7 7m0 0l-7 7m7-7H3" />
            </svg>
          </Link>
        </div>
        
        <p className="text-xs text-gray-500 dark:text-gray-400 text-center mt-4">
          Affiliate links — purchases support this free calculator at no extra cost to you.
        </p>
      </div>

      {/* Privacy Section */}
      <div className="bg-gradient-to-br from-gray-50 to-gray-100 dark:from-gray-900 dark:to-gray-800 rounded-2xl p-8">
        <div className="text-center mb-8">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-gray-100 mb-2">
            Your Privacy is Our Priority
          </h2>
          <p className="text-gray-600 dark:text-gray-400">
            We built this calculator with privacy-first principles.
          </p>
        </div>
        
        <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-6">
          <div className="flex gap-4">
            <div className="flex-shrink-0 w-10 h-10 bg-green-100 dark:bg-green-900/30 rounded-lg flex items-center justify-center">
              <svg className="w-5 h-5 text-green-600 dark:text-green-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
              </svg>
            </div>
            <div>
              <h3 className="font-semibold text-gray-900 dark:text-gray-100">No Data Storage</h3>
              <p className="text-sm text-gray-600 dark:text-gray-400">No cookies, localStorage, or databases. Your data never leaves your browser.</p>
            </div>
          </div>
          
          <div className="flex gap-4">
            <div className="flex-shrink-0 w-10 h-10 bg-green-100 dark:bg-green-900/30 rounded-lg flex items-center justify-center">
              <svg className="w-5 h-5 text-green-600 dark:text-green-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
              </svg>
            </div>
            <div>
              <h3 className="font-semibold text-gray-900 dark:text-gray-100">No Analytics</h3>
              <p className="text-sm text-gray-600 dark:text-gray-400">Zero tracking scripts. No Google Analytics, no third-party code.</p>
            </div>
          </div>
          
          <div className="flex gap-4">
            <div className="flex-shrink-0 w-10 h-10 bg-green-100 dark:bg-green-900/30 rounded-lg flex items-center justify-center">
              <svg className="w-5 h-5 text-green-600 dark:text-green-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
              </svg>
            </div>
            <div>
              <h3 className="font-semibold text-gray-900 dark:text-gray-100">URL-Based Sharing</h3>
              <p className="text-sm text-gray-600 dark:text-gray-400">Save your calculations in the URL. Bookmark or share — your choice.</p>
            </div>
          </div>
          
          <div className="flex gap-4">
            <div className="flex-shrink-0 w-10 h-10 bg-green-100 dark:bg-green-900/30 rounded-lg flex items-center justify-center">
              <svg className="w-5 h-5 text-green-600 dark:text-green-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
              </svg>
            </div>
            <div>
              <h3 className="font-semibold text-gray-900 dark:text-gray-100">Works Offline</h3>
              <p className="text-sm text-gray-600 dark:text-gray-400">After first load, works without internet. Install as an app on your device.</p>
            </div>
          </div>
          
          <div className="flex gap-4">
            <div className="flex-shrink-0 w-10 h-10 bg-green-100 dark:bg-green-900/30 rounded-lg flex items-center justify-center">
              <svg className="w-5 h-5 text-green-600 dark:text-green-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
              </svg>
            </div>
            <div>
              <h3 className="font-semibold text-gray-900 dark:text-gray-100">Open Source</h3>
              <p className="text-sm text-gray-600 dark:text-gray-400">Verify everything yourself. All code is available on GitHub.</p>
            </div>
          </div>
          
          <div className="flex gap-4">
            <div className="flex-shrink-0 w-10 h-10 bg-green-100 dark:bg-green-900/30 rounded-lg flex items-center justify-center">
              <svg className="w-5 h-5 text-green-600 dark:text-green-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
              </svg>
            </div>
            <div>
              <h3 className="font-semibold text-gray-900 dark:text-gray-100">Client-Side Only</h3>
              <p className="text-sm text-gray-600 dark:text-gray-400">All calculations run in your browser. No server processing.</p>
            </div>
          </div>
        </div>
      </div>

      {/* What is FIRE Section */}
      <div className="prose dark:prose-invert max-w-none">
        <h2 className="text-2xl font-bold text-gray-900 dark:text-gray-100 mb-4">What is FIRE?</h2>
        <p className="text-gray-600 dark:text-gray-400">
          <strong>FIRE</strong> stands for <strong>Financial Independence, Retire Early</strong>. It's a financial movement 
          focused on extreme savings and investment to retire much earlier than traditional retirement age. 
          The core principle is simple: save aggressively, invest wisely, and once your investments can 
          cover your living expenses indefinitely, you achieve financial independence.
        </p>
        <p className="text-gray-600 dark:text-gray-400">
          The most common FIRE calculation uses the <strong>4% rule</strong> (or 25x rule): if you can live on 4% of 
          your portfolio per year, you need to save 25 times your annual expenses. For example, if you 
          spend $40,000 per year, you need $1,000,000 to be financially independent.
        </p>
      </div>

      {/* Footer */}
      <footer className="text-center text-sm text-gray-500 dark:text-gray-400 pt-8 border-t border-gray-200 dark:border-gray-800">
        <div className="bg-gray-50 dark:bg-gray-800/50 rounded-lg p-4 text-xs text-left mb-6">
          <p className="font-semibold text-gray-600 dark:text-gray-300 mb-2">⚠️ Disclaimer</p>
          <p>
            This calculator is provided for <strong>educational and informational purposes only</strong>. 
            It is not financial, investment, tax, or legal advice. Results are hypothetical projections 
            and <strong>actual results will vary</strong>. Please consult a qualified financial advisor 
            before making any financial decisions.
          </p>
        </div>
        <p>
          Built with privacy in mind. No data ever leaves your browser.
        </p>
        <p className="mt-2">
          <a 
            href="https://github.com/jamesmontemagno/app-fire-calculator" 
            className="text-fire-600 dark:text-fire-400 hover:underline"
            target="_blank"
            rel="noopener noreferrer"
          >
            View on GitHub
          </a>
        </p>
      </footer>
    </div>
  )
}
