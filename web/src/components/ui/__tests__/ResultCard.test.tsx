import { renderToStaticMarkup } from 'react-dom/server'
import { describe, expect, it } from 'vitest'

import ResultCard from '../ResultCard'
import { calculateStandardFIRE } from '../../../utils/calculations'
import type { FIREInputs } from '../../../utils/calculations'

/**
 * `Infinity` returns from the engine are mathematically correct — they mean the target is never
 * reached under the given assumptions — so they must NOT be "fixed" in the calculations. What must
 * hold is that they never reach the user as "Infinity years" or "$∞". `ResultCard` is the single
 * guard for that, and this file proves the guard actually fires.
 *
 * Rendered with `react-dom/server`'s renderToStaticMarkup, which runs in plain Node. No jsdom, no
 * testing-library, no new dependency.
 */

const render = (element: Parameters<typeof renderToStaticMarkup>[0]) => renderToStaticMarkup(element)

const UNREACHABLE_TEXT = 'Not reachable'
const UNREACHABLE_SUBTEXT = 'The current return, inflation and contribution assumptions never reach this target.'

describe('finite values render through their format', () => {
  it.each([
    ['currency', 1_200_000, '$1,200,000'],
    ['years', 24.4, '24.4 years'],
    ['percent', 0.335, '33.5%'],
  ] as const)('formats a %s value', (format, value, expected) => {
    expect(render(<ResultCard label="L" value={value} format={format} />)).toContain(expected)
  })

  it('renders a plain number with locale grouping when no format is given', () => {
    expect(render(<ResultCard label="L" value={1_234_567} />)).toContain('1,234,567')
  })

  it('passes a string value through untouched', () => {
    expect(render(<ResultCard label="L" value="Already there" format="currency" />)).toContain('Already there')
  })

  it('renders zero rather than treating it as absent', () => {
    // 0 is falsy; a naive `value && ...` guard would drop it. $0 is a real, meaningful result.
    expect(render(<ResultCard label="L" value={0} format="currency" />)).toContain('$0')
  })

  it('renders the label, subtext and icon', () => {
    const html = render(<ResultCard label="FIRE Number" value={1} subtext="at 4% SWR" icon="🔥" />)
    expect(html).toContain('FIRE Number')
    expect(html).toContain('at 4% SWR')
    expect(html).toContain('🔥')
  })
})

describe('unreachable values are guarded', () => {
  it.each([
    ['Infinity', Infinity],
    ['-Infinity', -Infinity],
    ['NaN', NaN],
  ] as const)('replaces %s with wording, for every format', (_label, value) => {
    for (const format of ['currency', 'years', 'percent', 'none'] as const) {
      const html = render(<ResultCard label="L" value={value} format={format} />)
      expect(html).toContain(UNREACHABLE_TEXT)
      // The exact strings a broken guard would leak.
      expect(html).not.toContain('Infinity')
      expect(html).not.toContain('NaN')
      expect(html).not.toContain('∞')
    }
  })

  it('swaps the subtext for an explanation instead of showing the caller-supplied one', () => {
    const html = render(<ResultCard label="L" value={Infinity} format="years" subtext="on track" />)
    expect(html).toContain(UNREACHABLE_SUBTEXT)
    expect(html).not.toContain('on track')
  })

  it('honours caller overrides for both the value and the subtext', () => {
    const html = render(
      <ResultCard
        label="L"
        value={Infinity}
        format="currency"
        unreachableText="Never"
        unreachableSubtext="Increase contributions."
      />,
    )
    expect(html).toContain('Never')
    expect(html).toContain('Increase contributions.')
    expect(html).not.toContain(UNREACHABLE_TEXT)
  })

  it('shows the explanation even when the caller passed no subtext at all', () => {
    expect(render(<ResultCard label="L" value={Infinity} />)).toContain(UNREACHABLE_SUBTEXT)
  })

  it('does not treat the literal string "Infinity" as unreachable', () => {
    // The guard is `typeof value === 'number'`, so a string is passed through. Documents that the
    // guard is type-directed, not text-matching.
    expect(render(<ResultCard label="L" value="Infinity" />)).toContain('Infinity')
  })
})

describe('end to end from the engine', () => {
  it('renders an genuinely unreachable engine result without leaking Infinity', () => {
    // 2% nominal return against 5% inflation is a negative real rate. The deflated balance converges
    // to the fixed point -C/rho = 20000 / 0.0285714... = $700,000, which is below the $1.25M target,
    // so the target is never reached and Infinity is the correct answer. Derived algebraically, not
    // read from the code.
    const inputs: FIREInputs = {
      currentAge: 30,
      retirementAge: 55,
      currentSavings: 50_000,
      annualContribution: 20_000,
      annualIncome: 72_000,
      expectedReturn: 0.02,
      inflationRate: 0.05,
      withdrawalRate: 0.04,
      annualExpenses: 50_000,
      contributionGrowth: 'inflation',
    }
    const result = calculateStandardFIRE(inputs)
    expect(result.fireNumber).toBe(1_250_000)
    expect(result.yearsToFIRE).toBe(Infinity)
    expect(result.fireAge).toBe(Infinity)

    const html = render(
      <>
        <ResultCard label="FIRE Number" value={result.fireNumber} format="currency" />
        <ResultCard label="Years to FIRE" value={result.yearsToFIRE} format="years" />
        <ResultCard label="FIRE Age" value={result.fireAge} format="years" />
      </>,
    )
    expect(html).toContain('$1,250,000')
    expect(html).not.toContain('Infinity')
    expect(html).not.toContain('∞')
  })

  it('renders the shipped-default result with its real numbers', () => {
    // Shipped defaults reach FIRE at age 54.4 (rho = 1.07/1.03 - 1; n = 24.3839 -> 24.4).
    const result = calculateStandardFIRE({
      currentAge: 30,
      retirementAge: 55,
      currentSavings: 100_000,
      annualContribution: 24_000,
      annualIncome: 72_000,
      expectedReturn: 0.07,
      inflationRate: 0.03,
      withdrawalRate: 0.04,
      annualExpenses: 48_000,
      contributionGrowth: 'inflation',
    })
    const html = render(<ResultCard label="FIRE Age" value={result.fireAge} format="years" />)
    expect(html).toContain('54.4 years')
  })
})
