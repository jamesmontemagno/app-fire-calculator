import { Link } from 'react-router-dom'
import Disclaimer from '../ui/Disclaimer'

/**
 * Rendered by AppLayout beneath every route so the educational disclaimer and the terms and
 * privacy links are never more than a scroll away, whichever page a shared URL lands on.
 */
export default function SiteFooter() {
  return (
    <footer className="mt-12 border-t border-border-subtle pt-8 text-sm text-content-subtle">
      <div className="mb-6 text-left">
        <Disclaimer embedded />
      </div>
      <div className="space-y-2 text-center">
        <p>Built with privacy in mind. No data ever leaves your browser.</p>
        <nav aria-label="Legal">
          <Link to="/legal#terms" className="text-accent hover:underline">
            Terms of use
          </Link>
          {' · '}
          <Link to="/legal#privacy" className="text-accent hover:underline">
            Privacy policy
          </Link>
          {' · '}
          <a
            href="https://github.com/jamesmontemagno/app-fire-calculator"
            className="text-accent hover:underline"
            target="_blank"
            rel="noopener noreferrer"
          >
            View on GitHub
          </a>
        </nav>
        <p>
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
      </div>
    </footer>
  )
}
