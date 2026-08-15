import { describe, expect, it } from 'vitest'

import inventory from '../../../../shared/parity/periodic-fields.json'

import { PAGE_SOURCES, blankOutCommentsAndStrings, pageFileName } from './pageSources'

/**
 * Pins the web pages to `shared/parity/periodic-fields.json`, the same file
 * `app/MyFireNumber.Tests/Presentation/SharedPeriodicFieldInventoryTests.cs` reads.
 *
 * Adds no runtime code and changes no behaviour. It exists because "web and MAUI agree about which
 * amounts are recurring" was otherwise enforced by review alone, and the MAUI side cannot check its
 * own XAML from a unit test at all — its test project cannot reference the MAUI single-project. A
 * field made periodic on one platform and forgotten on the other is invisible to both suites unless
 * something outside both of them holds the list.
 *
 * The scan reuses `blankOutCommentsAndStrings` and `PAGE_SOURCES` from `pageSources.ts` rather than
 * parsing pages a second way. That module was extracted for exactly this in #74, and #73 records why:
 * a second parser is a second set of blind spots, and the two drift.
 */

interface SharedField {
  key: string
  storedPeriod: string
}

interface SharedCalculator {
  id: string
  webPage: string
  fields: SharedField[]
}

const CALCULATORS = inventory.calculators as SharedCalculator[]

/**
 * The `<CurrencyInput …/>` elements on one page, as raw source text.
 *
 * Throws when an element is unterminated rather than returning the ones found so far. A scan that
 * quietly returns a shorter list is the failure this whole file is trying to prevent: it would still
 * report success, over fields nobody checked.
 */
function currencyInputElements(fileName: string, source: string): Array<{ blanked: string; raw: string }> {
  const blanked = blankOutCommentsAndStrings(source)
  const elements: Array<{ blanked: string; raw: string }> = []
  const opening = '<CurrencyInput'

  let from = 0
  for (;;) {
    const start = blanked.indexOf(opening, from)
    if (start === -1) break

    let depth = 0
    let end = -1
    for (let i = start; i < blanked.length; i += 1) {
      const char = blanked[i]
      if (char === '{') depth += 1
      else if (char === '}') depth -= 1
      else if (depth === 0 && char === '/' && blanked[i + 1] === '>') {
        end = i + 2
        break
      }
    }

    if (end === -1) {
      throw new Error(
        `${fileName}: a <CurrencyInput> starting at offset ${start} is never closed. ` +
          'Refusing to report a partial field list.',
      )
    }

    elements.push({ blanked: blanked.slice(start, end), raw: source.slice(start, end) })
    from = end
  }

  return elements
}

/** The periodic fields one page declares, as `key:storedPeriod`, sorted. */
function scanPeriodicFields(fileName: string, source: string): string[] {
  const found: string[] = []

  for (const element of currencyInputElements(fileName, source)) {
    // `periodic` is a bare boolean prop; `storedPeriod` must not match it.
    if (!/\bperiodic\b/.test(element.blanked)) continue

    const key = /value=\{params\.(\w+)\}/.exec(element.blanked)?.[1]
    if (!key) {
      throw new Error(
        `${fileName}: a periodic <CurrencyInput> has no value={params.…} binding, so its field name ` +
          `cannot be determined. Element: ${element.raw.slice(0, 120)}`,
      )
    }

    // The prop's value is a string literal, so it survives only in the unblanked source.
    const storedPeriod = /storedPeriod="(\w+)"/.exec(element.raw)?.[1] ?? 'annual'
    found.push(`${key}:${storedPeriod}`)
  }

  return found.sort()
}

function sourceFor(webPage: string): string {
  const path = Object.keys(PAGE_SOURCES).find(candidate => pageFileName(candidate) === webPage)
  if (!path) throw new Error(`No page source for '${webPage}' named in periodic-fields.json.`)
  return PAGE_SOURCES[path]
}

describe('shared periodic field inventory', () => {
  it.each(CALCULATORS.map(calculator => [calculator.webPage, calculator] as const))(
    '%s declares the shared periodic fields in the shared periods',
    (webPage, calculator) => {
      const expected = calculator.fields.map(field => `${field.key}:${field.storedPeriod}`).sort()

      expect(scanPeriodicFields(webPage, sourceFor(webPage))).toEqual(expected)
    },
  )

  it('names every page that uses the period mechanism', () => {
    // Enumerated from the pages themselves, not from a count. A tenth page gaining a periodic field
    // fails here instead of passing quietly, which `expect(pages.length).toBe(9)` would not do.
    const usesMechanism = Object.entries(PAGE_SOURCES)
      .filter(([, source]) => /\bperiodic\b|CurrencyPeriodProvider/.test(blankOutCommentsAndStrings(source)))
      .map(([path]) => pageFileName(path))
      .sort()

    const named = CALCULATORS.map(calculator => calculator.webPage).sort()

    expect(named).toEqual(expect.arrayContaining(usesMechanism))
  })

  it('is not silently empty', () => {
    // Guards the guard. If the artifact failed to load, or the scanner matched nothing, every
    // comparison above would be an empty array equalling an empty array — indistinguishable from
    // success. These are the two facts that give the comparisons their meaning.
    expect(CALCULATORS.flatMap(calculator => calculator.fields).length).toBeGreaterThan(0)
    expect(
      CALCULATORS.flatMap(calculator => calculator.fields).filter(field => field.storedPeriod === 'monthly'),
    ).not.toHaveLength(0)
  })

  it('reads fields past a comment or a string holding an unbalanced brace', () => {
    // The ways the scan could give up early and silently report a shorter list.
    const hostile = [
      'export default function Hostile() {',
      '  return (',
      '    <CurrencyPeriodProvider>',
      "      <CurrencyInput label='spend {A' value={params.annualExpenses} periodic />",
      '      {/* <CurrencyInput value={params.ignoredByComment} periodic /> */}',
      '      <CurrencyInput label="premium" value={params.healthcareMonthlyPremium} periodic storedPeriod="monthly" />',
      '    </CurrencyPeriodProvider>',
      '  )',
      '}',
    ].join('\n')

    expect(scanPeriodicFields('Hostile.tsx', hostile)).toEqual([
      'annualExpenses:annual',
      'healthcareMonthlyPremium:monthly',
    ])
  })

  it('throws rather than truncating when an element is never closed', () => {
    const truncated = '<CurrencyInput value={params.annualExpenses} periodic'

    expect(() => scanPeriodicFields('Truncated.tsx', truncated)).toThrow(/never closed/)
  })
})
