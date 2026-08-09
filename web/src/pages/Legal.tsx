import SEO from '../components/SEO'

export default function Legal() {
  return (
    <>
      <SEO
        title="Terms and Privacy | FIRE Calculator"
        description="Terms of use and privacy information for the FIRE Calculator website and My Fire Number mobile app."
      />
      <article className="mx-auto max-w-3xl space-y-10 text-gray-700 dark:text-gray-300">
        <header>
          <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-100 sm:text-4xl">
            Terms and privacy
          </h1>
          <p className="mt-3 leading-7">
            FIRE Calculator and the My Fire Number mobile app are educational planning tools.
            They are not financial, investment, tax, or legal advice.
          </p>
        </header>

        <section id="terms" className="scroll-mt-8 space-y-3">
          <h2 className="text-2xl font-semibold text-gray-900 dark:text-gray-100">Terms of use</h2>
          <p className="leading-7">
            You are responsible for reviewing and adapting every assumption before acting on a
            calculation. Projections are hypothetical estimates, not guarantees of investment
            returns, retirement timing, expenses, taxes, healthcare costs, or account outcomes.
          </p>
          <p className="leading-7">
            The website and mobile app may change as formulas, defaults, and educational content
            improve. Use the tools for personal learning and consult qualified professionals for
            decisions about your finances.
          </p>
        </section>

        <section id="privacy" className="scroll-mt-8 space-y-3">
          <h2 className="text-2xl font-semibold text-gray-900 dark:text-gray-100">Privacy policy</h2>
          <p className="leading-7">
            The website stores calculator inputs and preferences in your browser. The My Fire
            Number mobile app stores calculator drafts, plans, and preferences in its local
            on-device database. Neither product sends financial inputs to a server.
          </p>
          <p className="leading-7">
            We do not use analytics, advertising trackers, user accounts, or a custom cloud-sync
            service. Removing browser storage or deleting the mobile app may remove locally saved
            calculations and plans.
          </p>
        </section>
      </article>
    </>
  )
}
