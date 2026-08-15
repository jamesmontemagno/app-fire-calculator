import { Link } from 'react-router-dom'
import { ArrowRight, Check, EyeOff, Link2, ShieldCheck, Wallet, Wifi } from 'lucide-react'
import { Card, CardContent, Disclaimer } from '../components/ui'
import { calculators } from '../config/calculators'
import SEO from '../components/SEO'

export default function Home() {
  return (
    <>
      <SEO
        title="FIRE Calculators - Free Financial Independence Calculator | Retire Early Planning Tools"
        description="Free FIRE calculators to plan your path to Financial Independence, Retire Early. Calculate Standard FIRE, Coast FIRE, Lean FIRE, Fat FIRE & more. 100% private, works offline, no tracking."
        keywords="FIRE calculator, financial independence calculator, retire early calculator, coast FIRE, lean FIRE, fat FIRE, barista FIRE, withdrawal rate, savings rate, 4% rule, retirement planning, early retirement"
        canonicalPath="/"
      />
      <div className="space-y-12">
        <header className="border-b border-border-subtle pb-10">
          <h1 className="text-3xl font-semibold tracking-tight text-content sm:text-4xl">
            FIRE Calculators
          </h1>
          <p className="mt-3 max-w-2xl text-lg text-content-muted">
            Plan your path to <strong className="font-semibold text-content">Financial Independence, Retire Early</strong>.
            Free, private, and works completely offline.
          </p>
          <ul className="mt-6 flex flex-wrap gap-x-6 gap-y-2 text-sm text-content-muted">
            {[
              { icon: ShieldCheck, label: '100% Private' },
              { icon: Wifi, label: 'Works Offline' },
              { icon: EyeOff, label: 'No Tracking' },
              { icon: Link2, label: 'Shareable URLs' },
            ].map(({ icon: Icon, label }) => (
              <li key={label} className="flex items-center gap-2">
                <Icon className="h-4 w-4 shrink-0 text-content-subtle" aria-hidden="true" strokeWidth={1.5} />
                {label}
              </li>
            ))}
          </ul>
        </header>

      {/* Quiz CTA */}
      <section className="rounded-container border border-border-subtle bg-surface-raised p-6 sm:flex sm:items-center sm:justify-between sm:gap-8">
        <div className="max-w-2xl">
          <h2 className="text-lg font-semibold text-content">
            Not sure which calculator to use?
          </h2>
          <p className="mt-2 text-sm text-content-muted">
            Take our quick quiz to find a FIRE starting point and compare nearby alternatives.
            Answer a few questions and we&apos;ll recommend the best calculator with your information pre-filled.
          </p>
          <p className="mt-2 text-xs text-content-subtle">
            Takes 2-3 minutes · Personalized recommendation
          </p>
        </div>
        <Link
          to="/quiz"
          className="mt-5 inline-flex h-10 shrink-0 items-center gap-2 rounded-control bg-accent px-5 text-sm font-medium text-accent-contrast transition-colors hover:bg-accent-hover motion-reduce:transition-none focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-surface sm:mt-0"
        >
          Find Your FIRE Path
          <ArrowRight className="h-4 w-4" aria-hidden="true" strokeWidth={1.5} />
        </Link>
      </section>

      {/* Calculator Grid */}
      <div>
        <h2 className="mb-6 text-xl font-semibold text-content">
          Choose Your Calculator
        </h2>
        <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-6">
          {calculators.map((calc) => {
            const Icon = calc.icon
            return (
            <Link key={calc.path} to={calc.path} className="group">
              <Card className="flex h-full flex-col border border-border-subtle transition-colors duration-200 hover:border-border-strong motion-reduce:transition-none">
                <CardContent className="flex flex-1 flex-col p-6">
                  <div className="flex items-start gap-4">
                    <div className="rounded-container bg-surface-sunken p-3">
                      <Icon className={`h-6 w-6 ${calc.accent}`} aria-hidden="true" strokeWidth={1.5} />
                    </div>
                    <div className="flex-1 min-w-0">
                      <h3 className="text-lg font-semibold text-content group-hover:text-accent transition-colors">
                        {calc.name}
                      </h3>
                      <p className="text-sm text-content-muted mt-1">
                        {calc.description}
                      </p>
                      <p className="text-xs text-content-subtle mt-3 font-medium">
                        {calc.audience}
                      </p>
                    </div>
                  </div>
                  <div className="mt-auto flex items-center pt-5 text-sm font-medium text-accent transition-transform group-hover:translate-x-1 motion-reduce:transition-none motion-reduce:group-hover:translate-x-0">
                    Start calculating
                    <ArrowRight className="ml-1 h-4 w-4" aria-hidden="true" strokeWidth={1.5} />
                  </div>
                </CardContent>
              </Card>
            </Link>
            )
          })}
        </div>
      </div>

      {/* Recommended Books Section */}
      <section className="rounded-container border border-border-subtle bg-surface-raised p-6 sm:p-8">
        <div className="mb-8">
          <h2 className="text-xl font-semibold text-content">
            Recommended FIRE Books
          </h2>
          <p className="mt-1 text-sm text-content-muted">
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
              <div className="aspect-[2/3] rounded-control overflow-hidden shadow-md group-hover:shadow-xl transition-shadow duration-200 bg-surface-raised">
                <img
                  src={book.image}
                  alt={book.title}
                  className="w-full h-full object-cover transition-transform duration-200 group-hover:scale-105 motion-reduce:transition-none motion-reduce:group-hover:scale-100"
                  loading="lazy"
                />
              </div>
              <p className="mt-2 text-xs font-medium text-content-muted line-clamp-1 group-hover:text-accent transition-colors">
                {book.title}
              </p>
              <p className="text-xs text-content-subtle line-clamp-1">
                {book.author}
              </p>
            </a>
          ))}
        </div>
        
        <div className="text-center">
          <Link
            to="/books"
            className="inline-flex h-10 items-center gap-2 rounded-control bg-accent px-5 text-sm font-medium text-accent-contrast transition-colors hover:bg-accent-hover motion-reduce:transition-none focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-surface"
          >
            View All Books & Details
            <ArrowRight className="w-4 h-4" strokeWidth={1.5} />
          </Link>
        </div>
        
        <p className="text-xs text-content-subtle text-center mt-4">
          Affiliate links — purchases support this free calculator at no extra cost to you.
        </p>
      </section>

      {/* Privacy Section */}
      <section className="rounded-container border border-border-subtle bg-surface-raised p-6 sm:p-8">
        <div className="mb-8">
          <h2 className="text-xl font-semibold text-content">
            Your Privacy is Our Priority
          </h2>
          <p className="mt-1 text-sm text-content-muted">
            We built this calculator with privacy-first principles.
          </p>
        </div>
        
        <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-6">
          <div className="flex gap-4">
            <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-control bg-surface-sunken">
              <Check className="h-4 w-4 text-success" aria-hidden="true" strokeWidth={2} />
            </div>
            <div>
              <h3 className="font-semibold text-content">No Financial Data Storage</h3>
              <p className="text-sm text-content-muted">Your financial data stays in URLs only—never stored. Only UI preferences (theme, layout) stored locally.</p>
            </div>
          </div>
          
          <div className="flex gap-4">
            <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-control bg-surface-sunken">
              <Check className="h-4 w-4 text-success" aria-hidden="true" strokeWidth={2} />
            </div>
            <div>
              <h3 className="font-semibold text-content">No Analytics</h3>
              <p className="text-sm text-content-muted">Zero tracking scripts. No Google Analytics, no third-party code.</p>
            </div>
          </div>
          
          <div className="flex gap-4">
            <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-control bg-surface-sunken">
              <Check className="h-4 w-4 text-success" aria-hidden="true" strokeWidth={2} />
            </div>
            <div>
              <h3 className="font-semibold text-content">URL-Based Sharing</h3>
              <p className="text-sm text-content-muted">Save your calculations in the URL. Bookmark or share — your choice.</p>
            </div>
          </div>
          
          <div className="flex gap-4">
            <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-control bg-surface-sunken">
              <Check className="h-4 w-4 text-success" aria-hidden="true" strokeWidth={2} />
            </div>
            <div>
              <h3 className="font-semibold text-content">Works Offline</h3>
              <p className="text-sm text-content-muted">After first load, works without internet. Install as an app on your device.</p>
            </div>
          </div>
          
          <div className="flex gap-4">
            <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-control bg-surface-sunken">
              <Check className="h-4 w-4 text-success" aria-hidden="true" strokeWidth={2} />
            </div>
            <div>
              <h3 className="font-semibold text-content">Open Source</h3>
              <p className="text-sm text-content-muted">Verify everything yourself. All code is available on GitHub.</p>
            </div>
          </div>
          
          <div className="flex gap-4">
            <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-control bg-surface-sunken">
              <Check className="h-4 w-4 text-success" aria-hidden="true" strokeWidth={2} />
            </div>
            <div>
              <h3 className="font-semibold text-content">Client-Side Only</h3>
              <p className="text-sm text-content-muted">All calculations run in your browser. No server processing.</p>
            </div>
          </div>
        </div>
      </section>

      {/* What is FIRE Section */}
      <section className="max-w-3xl">
        <h2 className="mb-3 text-xl font-semibold text-content">What is FIRE?</h2>
        <p className="text-content-muted">
          <strong>FIRE</strong> stands for <strong>Financial Independence, Retire Early</strong>. It's a financial movement 
          focused on extreme savings and investment to retire much earlier than traditional retirement age. 
          The core principle is simple: save aggressively, invest wisely, and once your investments can 
          cover your living expenses indefinitely, you achieve financial independence.
        </p>
        <p className="text-content-muted">
          The most common FIRE calculation uses the <strong>4% rule</strong> (or 25x rule): if you can live on 4% of 
          your portfolio per year, you need to save 25 times your annual expenses. For example, if you 
          spend $40,000 per year, you need $1,000,000 to be financially independent.
        </p>
      </section>

      {/* TallyAI Ad Section */}
      <section className="rounded-container border border-border-subtle bg-surface-raised p-6">
        <div className="flex flex-col gap-6 sm:flex-row sm:items-center">
          <div className="flex h-14 w-14 shrink-0 items-center justify-center rounded-container border border-border-subtle bg-surface-sunken">
            <Wallet className="h-6 w-6 text-content-muted" aria-hidden="true" strokeWidth={1.5} />
          </div>
          <div className="flex-1">
            <h3 className="mb-2 text-lg font-semibold text-content">
              Track Your Progress with Tally AI
            </h3>
            <p className="text-content-muted text-sm mb-4">
              Smart financial companion that helps you track spending, manage budgets, and achieve your FIRE goals 
              with AI-powered insights. Perfect complement to these calculators.
            </p>
            <a
              href="https://tallyai.money/"
              target="_blank"
              rel="noopener noreferrer"
              className="inline-flex h-9 items-center gap-2 rounded-control border border-border-strong px-4 text-sm font-medium text-content transition-colors hover:bg-surface-sunken motion-reduce:transition-none focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            >
              Learn More About Tally AI
              <ArrowRight className="w-4 h-4" strokeWidth={1.5} />
            </a>
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="text-center text-sm text-content-subtle pt-8 border-t border-border-subtle">
        <div className="mb-6 text-left">
          <Disclaimer />
        </div>
        <p>
          Built with privacy in mind. No data ever leaves your browser.
        </p>
        <p className="mt-2">
          <Link to="/legal#terms" className="text-accent hover:underline">
            Terms of use
          </Link>
          {' · '}
          <Link to="/legal#privacy" className="text-accent hover:underline">
            Privacy policy
          </Link>
        </p>
        <p className="mt-2">
          Find more tiny tools like this at{' '}
          <a
            href="https://www.tinytooltown.com/"
            className="text-accent hover:underline"
            target="_blank"
            rel="noopener noreferrer"
          >
            Tiny Tool Town
          </a>
          .
        </p>
        <p className="mt-2">
          <a 
            href="https://github.com/jamesmontemagno/app-fire-calculator" 
            className="text-accent hover:underline"
            target="_blank"
            rel="noopener noreferrer"
          >
            View on GitHub
          </a>
        </p>
      </footer>
    </div>
    </>
  )
}
