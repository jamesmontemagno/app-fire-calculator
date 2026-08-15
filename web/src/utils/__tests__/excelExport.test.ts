import { describe, expect, it } from 'vitest'

import {
  calculateBaristaFIRE,
  calculateCoastFIRE,
  calculateFatFIRE,
  calculateHealthcareGap,
  calculateInvestmentGrowth,
  calculateLeanFIRE,
  calculateReverseFIRE,
  calculateSnowballPayoff,
  calculateStandardFIRE,
  calculateWithdrawal,
} from '../calculations'
import type { FIREInputs } from '../calculations'
import { isExportFieldDeclared, prepareInputsForExport, prepareResultsForExport } from '../excelExport'

/**
 * Both export helpers used to infer a cell format by substring-matching the key name against
 * hand-maintained lists whose order decided precedence. That produced seven shipped defects —
 * `$25` for a 25-month payoff, `$3` for a count of three income sources, the string `"snowball"`
 * in a percent cell — because a name like `totalMonths` or `incomeSourceCount` contains a word
 * belonging to the wrong list.
 *
 * Lookup is now exact, against a single declared map. This file guards the two properties that
 * keep it that way:
 *
 *   - `exported fields are declared` walks the real calculator results and the real page call
 *     sites, so a field that reaches a workbook without a declared format fails here first.
 *   - `exported key sets` pins which fields reach the workbook at all, so a new scalar becomes a
 *     decision rather than an accident.
 *
 * Both derive what they check from real code rather than a hand-kept list. A hand-kept list would
 * be the same maintenance failure one level up.
 */

const DEFAULTS: FIREInputs = {
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
}

const DEBTS = [{ id: '1', name: 'Card', balance: 10_000, rate: 0.2, minPayment: 200 }]

/** Every result type a page hands to `prepareResultsForExport`, built by the real calculator. */
const RESULT_BUILDERS = {
  StandardFIRE: () => calculateStandardFIRE(DEFAULTS),
  LeanFIRE: () => calculateLeanFIRE(DEFAULTS),
  FatFIRE: () => calculateFatFIRE(DEFAULTS),
  CoastFIRE: () => calculateCoastFIRE(30, 55, 100_000, 24_000, 0.07, 0.03, 48_000, 0.04),
  BaristaFIRE: () => calculateBaristaFIRE(30, 100_000, 24_000, 0.07, 0.03, 48_000, 0.04, 20_000),
  Withdrawal: () => calculateWithdrawal(1_000_000, 0.04, 0.07, 0.03, 30),
  ReverseFIRE: () => calculateReverseFIRE(30, 55, 100_000, 48_000, 0.07, 0.03, 0.04),
  InvestmentGrowth: () => calculateInvestmentGrowth(100_000, 500, 'monthly', 30, 0.07, 0.03, 72_000, 30),
  HealthcareGap: () => calculateHealthcareGap(30, 55, 600, 3_000, 2_000, 0.03),
  DebtPayoff: () => calculateSnowballPayoff(DEBTS, 500),
} as const

// ================================================================================================
// Coverage: nothing reaches a workbook without a declared format
// ================================================================================================

/**
 * The source of every calculator page, read through Vite rather than `node:fs` so the suite keeps
 * type-checking under `npm run build` without pulling in Node type definitions.
 */
const PAGE_SOURCES = import.meta.glob('../../pages/*.tsx', {
  query: '?raw',
  import: 'default',
  eager: true,
}) as Record<string, string>

/**
 * Read the object literals the pages actually pass to the export helpers.
 *
 * The input shapes are built inline inside each page's export handler, so there is nothing to
 * import. Listing them here by hand would reintroduce exactly the drift this file exists to catch:
 * the list would be correct the day it was written and quietly wrong afterwards. Reading the call
 * sites keeps the check tied to shipping code, so adding `mortgageBalance: …` to a page fails this
 * test rather than surfacing in someone's spreadsheet.
 */
function readCallSiteKeys(fnName: string): Map<string, string[]> {
  const byFile = new Map<string, string[]>()

  for (const [path, raw] of Object.entries(PAGE_SOURCES)) {
    const source = blankOutCommentsAndStrings(raw)
    const keys: string[] = []
    const call = new RegExp(`\\b${fnName}\\s*\\(\\s*\\{`, 'g')

    for (const match of source.matchAll(call)) {
      const open = match.index + match[0].length - 1
      let depth = 0
      let close = -1

      for (let i = open; i < source.length; i += 1) {
        const char = source[i]
        if (char === '{' || char === '[' || char === '(') depth += 1
        else if (char === '}' || char === ']' || char === ')') {
          depth -= 1
          if (depth === 0) {
            close = i
            break
          }
        }
      }
      // An unbalanced literal means the scan lost track, and a scan that quietly returns fewer
      // keys is worse than no scan at all: it reports success over fields nobody checked.
      if (close === -1) throw new Error(`Could not find the end of the ${fnName} call in ${path}`)

      keys.push(...topLevelKeys(source.slice(open + 1, close)))
    }

    if (keys.length > 0) byFile.set(path.split('/').pop() ?? path, keys)
  }

  return byFile
}

/**
 * Replace the contents of comments and string literals with spaces, preserving length and so every
 * offset, before anything counts a brace.
 *
 * A `//` comment or a string holding an unbalanced `)` would otherwise end a scan early and drop
 * every key after it, silently. That is the one failure mode this whole file cannot afford: the
 * coverage suites would still pass, over a shorter list, which is indistinguishable from success.
 */
function blankOutCommentsAndStrings(source: string): string {
  const out = source.split('')
  let i = 0

  const blankUntil = (isEnd: (index: number) => boolean, escapes: boolean) => {
    i += 1
    while (i < source.length && !isEnd(i)) {
      if (escapes && source[i] === '\\') out[i++] = ' '
      out[i] = source[i] === '\n' ? '\n' : ' '
      i += 1
    }
  }

  while (i < source.length) {
    const char = source[i]
    const next = source[i + 1]

    if (char === '/' && next === '/') {
      while (i < source.length && source[i] !== '\n') out[i++] = ' '
    } else if (char === '/' && next === '*') {
      out[i] = ' '
      blankUntil(index => source[index] === '*' && source[index + 1] === '/', false)
      out[i] = ' '
      out[i + 1] = ' '
      i += 2
    } else if (char === '"' || char === "'" || char === '`') {
      blankUntil(index => source[index] === char, true)
      i += 1
    } else {
      i += 1
    }
  }

  return out.join('')
}

/** Property names declared directly on an object literal body, ignoring anything nested. */
function topLevelKeys(body: string): string[] {
  const keys: string[] = []
  let depth = 0
  let start = 0

  const take = (segment: string) => {
    const text = segment.trim()
    if (!text) return
    const colon = text.indexOf(':')
    // A colon-less segment is shorthand (`totalDebt,`), which is a property name on its own.
    const name = (colon === -1 ? text : text.slice(0, colon)).trim()
    if (/^[A-Za-z_$][\w$]*$/.test(name)) keys.push(name)
  }

  for (let i = 0; i < body.length; i += 1) {
    const char = body[i]
    if (char === '{' || char === '[' || char === '(') depth += 1
    else if (char === '}' || char === ']' || char === ')') depth -= 1
    else if (char === ',' && depth === 0) {
      take(body.slice(start, i))
      start = i + 1
    }
  }
  take(body.slice(start))

  return keys
}

/**
 * Like `topLevelKeys`, but pairs each top-level property name with the RAW text of its value.
 *
 * Comma and colon positions are found in the length-preserved blanked body (so a brace or colon
 * inside a string never splits a segment), then the same offsets index into the raw body to recover
 * the untouched formula string that blanking would otherwise have emptied.
 */
function topLevelEntries(blanked: string, raw: string): Array<[string, string]> {
  const entries: Array<[string, string]> = []
  let depth = 0
  let start = 0

  const take = (from: number, to: number) => {
    const blankedSegment = blanked.slice(from, to)
    const colon = blankedSegment.indexOf(':')
    if (colon === -1) return
    const name = blankedSegment.slice(0, colon).trim()
    if (!/^[A-Za-z_$][\w$]*$/.test(name)) return
    entries.push([name, raw.slice(from + colon + 1, to)])
  }

  for (let i = 0; i < blanked.length; i += 1) {
    const char = blanked[i]
    if (char === '{' || char === '[' || char === '(') depth += 1
    else if (char === '}' || char === ']' || char === ')') depth -= 1
    else if (char === ',' && depth === 0) {
      take(start, i)
      start = i + 1
    }
  }
  take(start, blanked.length)

  return entries
}

function undeclared(keys: Iterable<string>): string[] {
  return [...new Set(keys)].filter(key => !isExportFieldDeclared(key)).sort()
}

function coverageFailure(source: string, missing: string[]): string {
  return [
    `${source} reaches an exported workbook with no declared format: ${missing.join(', ')}.`,
    '',
    'Undeclared fields still export, they just render unstyled — that is deliberate, so a missing',
    'declaration never costs a user their data. It does mean nobody has decided how these should',
    'look. Add each one to EXPORT_FIELD_FORMATS in web/src/utils/excelExport.ts:',
    '',
    "  'currency' / 'percent' / 'age'   dollars, rates, whole ages",
    "  'years'                          a duration carrying one decimal (25.4 years)",
    "  'number'                         a whole count (25 months, 3 accounts)",
    "  'text'                           deliberately non-numeric: a string or a boolean",
    '',
    'Guessing the format from the name is what this map replaced. Pick one explicitly.',
  ].join('\n')
}

describe('exported fields are declared', () => {
  it.each(Object.entries(RESULT_BUILDERS))('%s results are fully declared', (label, build) => {
    const { values } = prepareResultsForExport(build())
    const missing = undeclared(Object.keys(values))
    expect(missing, coverageFailure(`${label} results`, missing)).toEqual([])
  })

  it.each([...readCallSiteKeys('prepareInputsForExport')])(
    '%s passes only declared input fields',
    (file, keys) => {
      const missing = undeclared(keys)
      expect(missing, coverageFailure(`${file} inputs`, missing)).toEqual([])
    },
  )

  it.each([...readCallSiteKeys('prepareResultsForExport')])(
    '%s passes only declared result fields',
    (file, keys) => {
      const missing = undeclared(keys)
      expect(missing, coverageFailure(`${file} results`, missing)).toEqual([])
    },
  )

  it('actually finds the page call sites it claims to check', () => {
    // Without this, renaming a helper would make the scan match nothing and the two suites above
    // would pass by checking zero fields. Rather than hard-code a page count — which passes when a
    // twelfth page is missed, i.e. fails in the permissive direction — this locates every call
    // independently and asserts that each one passing an object literal was actually read.
    for (const fnName of ['prepareInputsForExport', 'prepareResultsForExport'] as const) {
      const parsed = readCallSiteKeys(fnName)
      let literalCallSites = 0

      for (const [path, raw] of Object.entries(PAGE_SOURCES)) {
        const file = path.split('/').pop() ?? path
        const source = blankOutCommentsAndStrings(raw)

        for (const call of source.matchAll(new RegExp(`\\b${fnName}\\s*\\(`, 'g'))) {
          // Pages either build the shape inline or hand over a result object wholesale. Only the
          // inline ones are this scanner's job; the rest are covered by RESULT_BUILDERS above.
          if (!/^\s*\{/.test(source.slice(call.index + call[0].length))) continue
          literalCallSites += 1
          expect(
            parsed.get(file),
            `${file} passes an object literal to ${fnName}, but the call-site scanner in this ` +
              'file read no fields out of it, so those exports are unchecked. Fix the scanner — ' +
              'do not delete this assertion, and do not relax it to make the build go green.',
          ).toBeDefined()
        }
      }

      expect(literalCallSites).toBeGreaterThan(0)
    }

    expect(readCallSiteKeys('prepareInputsForExport').get('DebtPayoff.tsx')).toContain('totalDebt')
    expect(readCallSiteKeys('prepareResultsForExport').get('DeferredCompensation.tsx')).toContain(
      'firstShortfallAge',
    )
  })

  it('reads keys past a comment or a string holding an unbalanced brace', () => {
    // The three ways the scanner used to give up early and silently report a shorter list.
    const hostile = [
      'const handleExport = () => {',
      '  prepareInputsForExport(',
      '    {',
      '      currentAge,',
      "      label: 'plan {A',  // capped at 64 (see #62) :)",
      '      mortgageBalance,',
      '    },',
      '  )',
      '}',
    ].join('\n')

    const original = PAGE_SOURCES['../../pages/StandardFIRE.tsx']
    try {
      PAGE_SOURCES['../../pages/StandardFIRE.tsx'] = hostile
      expect(readCallSiteKeys('prepareInputsForExport').get('StandardFIRE.tsx')).toEqual([
        'currentAge',
        'label',
        'mortgageBalance',
      ])
    } finally {
      PAGE_SOURCES['../../pages/StandardFIRE.tsx'] = original
    }
  })
})

// ================================================================================================
// Which fields reach the workbook at all
// ================================================================================================

function keySetFailure(label: string, actual: string[], expected: readonly string[]): string {
  const added = actual.filter(key => !expected.includes(key))
  const removed = expected.filter(key => !actual.includes(key))

  return [
    `${label} no longer exports the fields this test expects.`,
    added.length > 0 ? `  now also exported: ${added.join(', ')}` : null,
    removed.length > 0 ? `  no longer exported: ${removed.join(', ')}` : null,
    '',
    'This test is not asking you to paste the new list in. Everything in it lands in a spreadsheet',
    'a user downloads and forwards to other people, so for each ADDED field pick one:',
    '',
    '  1. It belongs in the workbook — declare it in EXPORT_FIELD_FORMATS (web/src/utils/',
    '     excelExport.ts) with the format it should render as, then add it to the list below.',
    '  2. It does not belong — it is internal, or configuration rather than an outcome. Stop',
    '     passing the raw result and pass a curated object instead, the way',
    '     DeferredCompensation.tsx does.',
    "  3. It belongs but is not a number — declare it as 'text' and add it below.",
    '',
    'A REMOVED field means a row existing users currently see has disappeared from their download.',
    'Confirm that was intended before deleting it from the list.',
  ]
    .filter((line): line is string => line !== null)
    .join('\n')
}

describe('exported key sets', () => {
  it.each([
    [
      'StandardFIRE',
      ['fireNumber', 'yearsToFIRE', 'fireAge', 'savingsRate', 'monthlyContribution', 'coastFireNumber'],
    ],
    [
      'LeanFIRE',
      ['fireNumber', 'yearsToFIRE', 'fireAge', 'savingsRate', 'monthlyContribution', 'coastFireNumber', 'isLean', 'leanThreshold'],
    ],
    [
      'FatFIRE',
      ['fireNumber', 'yearsToFIRE', 'fireAge', 'savingsRate', 'monthlyContribution', 'coastFireNumber', 'isFat', 'fatThreshold'],
    ],
    ['CoastFIRE', ['coastNumber', 'yearsToCoast', 'alreadyCoasting', 'fireNumber']],
    [
      'BaristaFIRE',
      ['baristaNumber', 'fullFireNumber', 'yearsToBaristaFIRE', 'partTimeIncomeNeeded', 'savingsFromPartTime'],
    ],
    [
      'Withdrawal',
      ['portfolioLongevity', 'horizonFundedRatio', 'annualWithdrawal', 'monthlyWithdrawal', 'endingBalance'],
    ],
    [
      'ReverseFIRE',
      ['fireNumber', 'yearsToFIRE', 'requiredAnnualSavings', 'requiredMonthlySavings', 'alreadyAchievable', 'currentWillGrowTo'],
    ],
    [
      'InvestmentGrowth',
      ['savingsRate', 'annualContribution', 'monthlyContribution', 'finalNominalBalance', 'finalInflationAdjustedBalance', 'totalInvested', 'totalGrowth', 'inflationImpact'],
    ],
    ['HealthcareGap', ['gapYears', 'annualCost', 'totalCost', 'avgAnnualCost']],
    ['DebtPayoff', ['totalMonths', 'totalInterest', 'totalPrincipal', 'monthlyPayment']],
  ] as const)('%s exports exactly its known scalar fields', (label, expected) => {
    const { values } = prepareResultsForExport(RESULT_BUILDERS[label]())
    const actual = Object.keys(values).sort()
    expect(actual, keySetFailure(label, actual, expected)).toEqual([...expected].sort())
  })
})

// ================================================================================================
// Structural filtering
// ================================================================================================

describe('structural filtering', () => {
  it('omits arrays, which belong on their own sheets', () => {
    const { values } = prepareResultsForExport(calculateStandardFIRE(DEFAULTS))
    expect(values).not.toHaveProperty('projections')
  })

  it('omits nested objects', () => {
    // StandardFIRE carries a `retirementGoal` object that must not be flattened into rows.
    const { values } = prepareResultsForExport(calculateStandardFIRE(DEFAULTS))
    expect(values).not.toHaveProperty('retirementGoal')
  })

  it('omits an empty array as readily as a populated one', () => {
    const { values } = prepareResultsForExport({ rows: [], totalDebt: 5 })
    expect(Object.keys(values)).toEqual(['totalDebt'])
  })

  it('applies the same rules to inputs as to results', () => {
    // These were two near-copies that had already drifted: only one had a non-finite guard, only
    // one type-gated formatting, and their key lists disagreed about whether 'total' meant money.
    // They share one implementation now, so the two calls cannot disagree.
    const shape = { debts: [{ balance: 1 }], nested: {}, monthlyBudget: 500, mode: null }
    expect(prepareInputsForExport(shape)).toEqual(prepareResultsForExport(shape))
    expect(Object.keys(prepareInputsForExport(shape).values)).toEqual(['monthlyBudget'])
  })
})

// ================================================================================================
// Values that are not plain finite numbers
// ================================================================================================

describe('non-finite values', () => {
  it('replaces Infinity with wording rather than writing "$∞" into a cell', () => {
    const unreachable = calculateStandardFIRE({
      ...DEFAULTS,
      expectedReturn: 0.02,
      inflationRate: 0.05,
      annualContribution: 20_000,
      annualExpenses: 50_000,
    })
    expect(unreachable.fireAge).toBe(Infinity)

    const { values, formats } = prepareResultsForExport(unreachable)
    expect(values.fireAge).toBe('Not reachable')
    expect(values.yearsToFIRE).toBe('Not reachable')
    // No numeric format is attached, so nothing tries to render the text as currency or years.
    expect(formats).not.toHaveProperty('fireAge')
    expect(formats).not.toHaveProperty('yearsToFIRE')
  })

  it('handles -Infinity and NaN the same way', () => {
    const { values } = prepareResultsForExport({ fireNumber: -Infinity, totalCost: NaN, gapYears: 42 })
    expect(values.fireNumber).toBe('Not reachable')
    expect(values.totalCost).toBe('Not reachable')
    expect(values.gapYears).toBe(42)
  })

  it('guards non-finite inputs too, which it previously did not', () => {
    // Pinned under #64: only the results helper had this guard, so an infinite input was written
    // straight into a cell. Sharing one implementation removed the asymmetry.
    const { values, formats } = prepareInputsForExport({ annualIncome: Infinity })
    expect(values.annualIncome).toBe('Not reachable')
    expect(formats).not.toHaveProperty('annualIncome')
  })

  it('leaves finite values, including zero and negatives, as numbers', () => {
    const { values } = prepareResultsForExport({ totalGrowth: 0, firstYearSurplus: -1_500 })
    expect(values.totalGrowth).toBe(0)
    expect(values.firstYearSurplus).toBe(-1_500)
  })
})

describe('null and undefined', () => {
  it('omits null instead of emitting a blank labelled row', () => {
    // #64 item 2. `typeof null === 'object'`, but the old guard also required `value !== null`, so
    // null fell through to a blank row. DeferredCompensation.tsx worked around it with `?? 0`,
    // which wrote a 0 that reads as a shortfall at age 0.
    const { values, formats } = prepareResultsForExport({ firstShortfallAge: null, fireNumber: 1_200_000 })
    expect(values).not.toHaveProperty('firstShortfallAge')
    expect(formats).not.toHaveProperty('firstShortfallAge')
    expect(values.fireNumber).toBe(1_200_000)
  })

  it('omits undefined the same way, so the two are no longer asymmetric', () => {
    const { values } = prepareResultsForExport({ fireAge: undefined, fireNumber: 1 })
    expect(values).not.toHaveProperty('fireAge')
    expect(values.fireNumber).toBe(1)
  })

  it('still exports a real zero, which says something different from null', () => {
    const { values } = prepareResultsForExport({ firstShortfallAge: 0 })
    expect(values.firstShortfallAge).toBe(0)
  })
})

// ================================================================================================
// Declared formats
// ================================================================================================

describe('declared formats', () => {
  const format = (key: string, value: unknown) =>
    prepareResultsForExport({ [key]: value }).formats[key]

  it.each([
    ['withdrawalRate', 'percent'],
    ['savingsRate', 'percent'],
    ['horizonFundedRatio', 'percent'],
    ['expectedReturn', 'percent'],
    ['inflationRate', 'percent'],
  ] as const)('%s renders as a percentage', (key, expected) => {
    expect(format(key, 0.04)).toBe(expected)
  })

  it.each([
    ['fireNumber', 'currency'],
    ['endingBalance', 'currency'],
    ['annualWithdrawal', 'currency'],
    ['totalInterest', 'currency'],
    ['totalPrincipal', 'currency'],
    ['monthlyPayment', 'currency'],
    ['requiredAnnualSavings', 'currency'],
    ['totalCost', 'currency'],
    ['portfolioValue', 'currency'],
    // Both are dollar thresholds and used to render as bare numbers.
    ['leanThreshold', 'currency'],
    ['fatThreshold', 'currency'],
  ] as const)('%s renders as currency', (key, expected) => {
    expect(format(key, 1_000)).toBe(expected)
  })

  it.each([
    ['yearsToFIRE', 'years'],
    ['yearsToCoast', 'years'],
    ['portfolioLongevity', 'years'],
    ['gapYears', 'years'],
    // An age, but the calculators round it to one decimal, so the integer 'age' format would show
    // 51.5 as 52 — a silent change to the number rather than to its styling.
    ['fireAge', 'years'],
  ] as const)('%s renders with a decimal', (key, expected) => {
    expect(format(key, 25.4)).toBe(expected)
  })

  it.each([
    ['currentAge', 'age'],
    ['retirementAge', 'age'],
    ['medicareAge', 'age'],
    ['firstShortfallAge', 'age'],
  ] as const)('%s renders as a whole age', (key, expected) => {
    expect(format(key, 55)).toBe(expected)
  })

  it.each([
    ['totalMonths', 'number'],
    ['targetMonths', 'number'],
    ['totalDebts', 'number'],
    ['incomeSourceCount', 'number'],
    ['accountCount', 'number'],
    ['retirementYears', 'number'],
  ] as const)('%s renders as a whole count', (key, expected) => {
    expect(format(key, 25)).toBe(expected)
  })

  it('leaves an undeclared key unformatted rather than guessing', () => {
    const { values, formats } = prepareResultsForExport({ mysteryField: 7 })
    // Still exported: an unstyled number is never wrong, whereas a guessed one can be.
    expect(values.mysteryField).toBe(7)
    expect(formats).not.toHaveProperty('mysteryField')
  })

  it('matches key names exactly, so a differently-cased key is not a match', () => {
    // The old lookup lowercased both sides and used `.includes()`, which is how 'strategy' matched
    // 'rate'. Exact matching is what makes that class of collision unrepresentable.
    expect(prepareResultsForExport({ TotalInterest: 500 }).formats).not.toHaveProperty('TotalInterest')
    expect(prepareResultsForExport({ totalInterest: 500 }).formats.totalInterest).toBe('currency')
  })
})

// ================================================================================================
// The specific defects in #62 and #64
// ================================================================================================

describe('substring collisions that used to mis-format live exports', () => {
  it('#62: totalMonths is a count of months, not an amount of money', () => {
    // 'totalmonths'.includes('total'), and currencyKeys was tested before timeKeys, so a 25-month
    // payoff was written into the user's spreadsheet as "$25". Exact lookup makes that impossible.
    //
    // Declared 'number' (#,##0 -> "25") rather than 'years' (0.0 -> "25.0"): these are whole
    // months, and "25.0 months" is a smaller version of the same defect.
    //
    // This deliberately diverges from MAUI, which writes TotalMonths with DecimalStyleIndex
    // (DebtPayoffWorkbook.cs:61 -> format code "0.0") and so renders "25.0". MAUI is immune to the
    // name-inference class — it declares a style per cell at the call site — but it picked a
    // decimal style for a whole-number field. Web is correct here; the C# side is worth a look.
    const debt = calculateSnowballPayoff(DEBTS, 500)
    expect(debt.totalMonths).toBe(25)

    const { formats } = prepareResultsForExport(debt)
    expect(formats.totalMonths).toBe('number')

    // The genuinely-currency siblings are unchanged.
    expect(formats.totalInterest).toBe('currency')
    expect(formats.totalPrincipal).toBe('currency')
    expect(formats.monthlyPayment).toBe('currency')
  })

  it('#64: strategy is a word, not a percentage', () => {
    // 'strategy' contains 'rate' — st-RATE-gy — and percentKeys was tested first, so the string
    // "snowball" was written into a cell carrying numFmt '0.0%'.
    const { values, formats } = prepareInputsForExport({ strategy: 'snowball', mode: 'budget' })
    expect(values.strategy).toBe('snowball')
    expect(formats.strategy).toBe('text')
    expect(formats.mode).toBe('text')
  })

  it('#64: totalDebt is money, the inverse of the totalMonths defect', () => {
    // The inputs list had no 'total' entry at all, so a real dollar amount exported unformatted
    // while the results list treated 'total' as currency. One shared map ends that disagreement.
    const { formats } = prepareInputsForExport({ totalDebt: 21_500, monthlyBudget: 500 })
    expect(formats.totalDebt).toBe('currency')
    expect(formats.monthlyBudget).toBe('currency')
  })

  it('a count of income sources is a count, not an amount of money', () => {
    // Not filed: 'incomeSourceCount' contains 'income', so three income sources exported as "$3".
    // The same defect as #62 on a different field, found by enumerating every live call site.
    expect(prepareInputsForExport({ incomeSourceCount: 3 }).formats.incomeSourceCount).toBe('number')
  })

  it('a contribution frequency is a word, not an amount of money', () => {
    // Not filed: 'contributionFrequency' contains 'contribution', so "monthly" landed in a $ cell.
    const { values, formats } = prepareInputsForExport({ contributionFrequency: 'monthly' })
    expect(values.contributionFrequency).toBe('monthly')
    expect(formats.contributionFrequency).toBe('text')
  })

  it('input booleans are declared non-numeric instead of receiving a number format', () => {
    // Not filed: neither list matched 'withdrawOnlyAfterRetirement' or 'reinvestSurplus', and the
    // inputs helper never type-gated, so both booleans received numFmt '#,##0'.
    const { values, formats } = prepareInputsForExport({
      withdrawOnlyAfterRetirement: true,
      reinvestSurplus: false,
    })
    expect(values.withdrawOnlyAfterRetirement).toBe(true)
    expect(values.reinvestSurplus).toBe(false)
    expect(formats.withdrawOnlyAfterRetirement).toBe('text')
    expect(formats.reinvestSurplus).toBe('text')
  })

  it('result booleans stay in the workbook, now declared rather than accidental', () => {
    // #64 item 1. These carry real meaning — "you are already coasting" — so they are exported,
    // with no number format. Previously they were exported because nothing happened to filter them
    // out, which is not the same thing.
    const lean = prepareResultsForExport(calculateLeanFIRE(DEFAULTS))
    expect(lean.values.isLean).toBe(false)
    expect(lean.formats.isLean).toBe('text')

    const coast = prepareResultsForExport(
      calculateCoastFIRE(30, 55, 100_000, 24_000, 0.07, 0.03, 48_000, 0.04),
    )
    expect(coast.values.alreadyCoasting).toBe(false)
    expect(coast.formats.alreadyCoasting).toBe('text')
  })

  it('a name that merely contains "age" is no longer formatted as an age', () => {
    // Latent when filed under #64: ageKeys was checked first and 'age' is the shortest, most
    // collision-prone token in any of the lists, so a future 'mortgageBalance' or 'averageReturn'
    // would have exported with the age format ahead of the currency and percent rules. There is no
    // precedence to get wrong now, and neither name matches anything.
    const { formats } = prepareInputsForExport({ mortgageBalance: 300_000, averageReturn: 0.07 })
    expect(formats).not.toHaveProperty('mortgageBalance')
    expect(formats).not.toHaveProperty('averageReturn')

    // A real age is still correct, which is why the collision was easy to miss.
    expect(prepareInputsForExport({ currentAge: 30 }).formats.currentAge).toBe('age')
  })
})

// ================================================================================================
// Coverage: every declared result formula attaches to a real cell
// ================================================================================================

/**
 * `resultFormulas` turns a Results cell into a live Excel formula instead of a frozen value, but
 * only when two things line up, and nothing warns when they don't:
 *
 *   - the formula's KEY must match a field in the `results:` object (the output of
 *     `prepareResultsForExport(...)`). A key that matches nothing is silently inert — no error, no
 *     console warning, no visual difference — so the cell just keeps its plain value. That is how
 *     HealthcareGap shipped `yearsInGap`/`annualBaseCost` against a result exposing
 *     `gapYears`/`annualCost` (#66), and why it went unnoticed until someone opened the formula bar.
 *   - every `{inputKey}` INSIDE the formula must match a field passed to `prepareInputsForExport`,
 *     or the `{inputKey}`→cell-reference substitution leaves the literal `{inputKey}` in the cell.
 *
 * This suite derives both truth sets from real invocation — the calculator output and the page's
 * own input call site — rather than a hand-kept list, and refuses to pass while checking nothing.
 * It deliberately does not try to prove the formula's arithmetic equals `calculations.ts`; a clamp
 * or guard dropped from an otherwise valid formula (BaristaFIRE's `MAX`, savingsRate's divide-by-
 * zero guard) is a human read, not a mechanical one.
 */

/** Page file (as it appears in PAGE_SOURCES) → the RESULT_BUILDERS entry whose fields it exports. */
const PAGE_RESULT_BUILDER: Record<string, keyof typeof RESULT_BUILDERS> = {
  'BaristaFIRE.tsx': 'BaristaFIRE',
  'CoastFIRE.tsx': 'CoastFIRE',
  'FatFIRE.tsx': 'FatFIRE',
  'HealthcareGap.tsx': 'HealthcareGap',
  'LeanFIRE.tsx': 'LeanFIRE',
  'ReverseFIRE.tsx': 'ReverseFIRE',
  'SavingsRate.tsx': 'InvestmentGrowth',
  'StandardFIRE.tsx': 'StandardFIRE',
  'WithdrawalRate.tsx': 'Withdrawal',
}

/**
 * Read each page's `resultFormulas` object literal as key → formula string.
 *
 * Built on the same hardened scan as `readCallSiteKeys`: comments and strings are blanked
 * length-preservingly before any brace is counted, and an unbalanced literal throws rather than
 * returning a short list, so this can never quietly report fewer formulas than a page declares.
 */
function readResultFormulas(): Map<string, Map<string, string>> {
  const byFile = new Map<string, Map<string, string>>()

  for (const [path, raw] of Object.entries(PAGE_SOURCES)) {
    const source = blankOutCommentsAndStrings(raw)
    const marker = /\bresultFormulas\s*:\s*\{/g
    const formulas = new Map<string, string>()

    for (const match of source.matchAll(marker)) {
      const open = match.index + match[0].length - 1
      let depth = 0
      let close = -1

      for (let i = open; i < source.length; i += 1) {
        const char = source[i]
        if (char === '{' || char === '[' || char === '(') depth += 1
        else if (char === '}' || char === ']' || char === ')') {
          depth -= 1
          if (depth === 0) {
            close = i
            break
          }
        }
      }
      if (close === -1) throw new Error(`Could not find the end of resultFormulas in ${path}`)

      // Keys are read from blanked source so nesting counts correctly; each key's formula refs are
      // read from the RAW slice (blanking emptied every string literal). Attribute refs per key by
      // walking the same top-level segments the key extraction used.
      const blankedBody = source.slice(open + 1, close)
      const rawBody = raw.slice(open + 1, close)
      for (const [key, rawSegment] of topLevelEntries(blankedBody, rawBody)) {
        const formulaRefs = [...rawSegment.matchAll(/\{([A-Za-z_$][\w$]*)\}/g)].map(m => m[1])
        formulas.set(key, formulaRefs.join(' '))
      }
    }

    if (formulas.size > 0) byFile.set(path.split('/').pop() ?? path, formulas)
  }

  return byFile
}

describe('declared result formulas attach to real cells', () => {
  const formulasByFile = readResultFormulas()
  const inputKeysByFile = readCallSiteKeys('prepareInputsForExport')

  it.each([...formulasByFile])('%s keys all name a real result field', (file, formulas) => {
    const builder = PAGE_RESULT_BUILDER[file]
    expect(
      builder,
      `${file} declares resultFormulas but has no PAGE_RESULT_BUILDER entry, so this test cannot ` +
        'check its keys against real calculator output. Add the mapping — do not skip the page.',
    ).toBeDefined()

    const fields = new Set(Object.keys(prepareResultsForExport(RESULT_BUILDERS[builder]()).values))
    const dead = [...formulas.keys()].filter(key => !fields.has(key)).sort()
    expect(
      dead,
      `${file} declares resultFormulas key(s) that no field in its exported results provides: ` +
        `${dead.join(', ')}. A formula keyed to a nonexistent field never attaches — the cell ` +
        `silently keeps a plain value. Real fields are: ${[...fields].sort().join(', ')}.`,
    ).toEqual([])
  })

  it.each([...formulasByFile])('%s formulas reference only declared input fields', (file, formulas) => {
    const inputs = new Set(inputKeysByFile.get(file) ?? [])
    const unresolved = [...new Set([...formulas.values()].flatMap(refs => refs.split(' ').filter(Boolean)))]
      .filter(ref => !inputs.has(ref))
      .sort()
    expect(
      unresolved,
      `${file} has resultFormulas referencing {input} key(s) not passed to prepareInputsForExport: ` +
        `${unresolved.join(', ')}. The {key}->cell substitution would leave the literal text in the ` +
        `cell. Declared inputs are: ${[...inputs].sort().join(', ')}.`,
    ).toEqual([])
  })

  it('actually reads the resultFormulas it claims to check', () => {
    // The vacuity guard, in the spirit of #68: if the marker or the scan silently matched nothing,
    // the two suites above would pass over zero formulas. Rather than hard-code a page count (which
    // passes when a tenth page is missed), locate every `resultFormulas:` literal independently and
    // require that each one was read into a non-empty map.
    let literalSites = 0
    for (const [path, raw] of Object.entries(PAGE_SOURCES)) {
      const file = path.split('/').pop() ?? path
      const source = blankOutCommentsAndStrings(raw)
      for (const _ of source.matchAll(/\bresultFormulas\s*:\s*\{/g)) {
        literalSites += 1
        expect(
          formulasByFile.get(file)?.size,
          `${file} declares a resultFormulas literal, but readResultFormulas read no keys from it. ` +
            'Fix the scanner — do not delete this assertion or relax it to go green.',
        ).toBeGreaterThan(0)
      }
    }
    expect(literalSites).toBeGreaterThan(0)

    // A known-good pair, so the scan cannot pass by reading keys but losing their formula refs.
    expect(formulasByFile.get('HealthcareGap.tsx')?.get('gapYears')).toContain('medicareAge')
    expect(formulasByFile.get('HealthcareGap.tsx')?.has('annualCost')).toBe(true)
  })
})

// ================================================================================================
// Degenerate input
// ================================================================================================

describe('empty and degenerate input', () => {
  it('returns empty maps for an empty result object', () => {
    const { values, formats } = prepareResultsForExport({})
    expect(values).toEqual({})
    expect(formats).toEqual({})
  })

  it('does not invent rows for a result made only of arrays and objects', () => {
    const { values } = prepareResultsForExport({ rows: [1, 2], nested: { a: 1 } })
    expect(values).toEqual({})
  })

  it('tolerates a missing object rather than throwing mid-download', () => {
    expect(prepareResultsForExport(undefined).values).toEqual({})
    expect(prepareInputsForExport(null).values).toEqual({})
  })
})
