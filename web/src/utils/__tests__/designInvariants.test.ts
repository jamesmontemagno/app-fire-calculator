/**
 * Guards the five invariants the web UI redesign established, plus one the label-wrap fix added.
 *
 * The sixth guard is a different shape from the other five and worth reading separately. Those ban a
 * visible thing; this one REQUIRES an invisible thing, because the period qualifier on a periodic
 * input renders nothing at all — it exists solely to reach the accessible name. That makes it look
 * exactly like dead markup to anyone tidying up, and deleting it costs a screen reader the period on
 * the one field whose entire ambiguity is monthly-vs-annual. Measured, not assumed:
 *
 *   as shipped         "Retirement spending (today's dollars) (per year) More information"
 *   qualifier deleted  "Retirement spending (today's dollars) More information"
 *
 * The in-field `/yr` adornment does not rescue it: that span is `pointer-events-none` and sits
 * OUTSIDE the `<label>`, so it contributes nothing to the name. Hence the guard is scoped to label
 * content specifically, not to the file as a whole.
 *
 * The redesign removed 57 emoji, 743 hand-placed `dark:` utilities, 44 inline `<svg>` elements,
 * 13 stray hex colours and 7 gradient panels. Nothing in the suite noticed any of that, which meant
 * every one of those numbers could drift back to non-zero at full green. The user's requirement was
 * "doesn't use emoji at all"; a requirement no test asserts is a requirement with an expiry date.
 *
 * Two properties matter more than the checks themselves, because a scanner that reports a clean
 * codebase it never actually read is worse than no scanner at all:
 *
 * 1. Every detector is validated against a known positive BEFORE it is trusted on real source, and
 *    against a known negative so it cannot pass by matching everything. This is not hypothetical.
 *    A `grep -P '[\x{1F300}-\x{1FAFF}]'` returned 0 against the 57 emoji that were really there, and
 *    a `git grep -oE '#[0-9A-Fa-f]{6}\b'` returned 0 because `\b` is not a word boundary in that
 *    engine. Both looked exactly like success.
 * 2. Files are DISCOVERED, never enumerated. A hardcoded list is correct the day it is written and
 *    silently wrong the day someone adds a file — which is the one file you actually wanted checked.
 */

import { describe, expect, it } from 'vitest'

/**
 * Every source file that ships to the browser, discovered rather than listed.
 *
 * A glob, not an enumeration: a file added tomorrow is scanned tomorrow. A hardcoded list is correct
 * the day it is written and silently wrong the day someone adds the one file you wanted checked.
 * `eager` so a miss surfaces as an empty record the vacuity guards below catch, rather than as an
 * unawaited promise that quietly resolves to nothing.
 *
 * `.css` is in the pattern, and getting it there took a config change. Vitest defaults `css: false`,
 * which stubs CSS modules to an empty string even when the import carries `?raw` — so this glob
 * originally reported the token file clean without reading a byte of it. `vitest.config.ts` now sets
 * `css: true`, and `reads real source, not a stub` below asserts the bytes actually arrived, because
 * a config that silently reverts would otherwise restore the silent pass.
 */
const ALL_SOURCES = import.meta.glob('../../**/*.{ts,tsx,css}', {
  query: '?raw',
  import: 'default',
  eager: true,
}) as Record<string, string>

/**
 * Test files are excluded because these invariants are about what reaches a browser.
 *
 * That is the same distinction the PR drew when it kept four `→` characters: they live in
 * `excelExport.test.ts` comments and one assertion-failure message in `excelFormulaEquivalence.test.ts`,
 * so no user ever sees them. Excluding tests also excludes THIS file, whose positive controls are
 * deliberately full of the things being banned.
 */
function isShipped(path: string): boolean {
  return !path.includes('/__tests__/') && !/\.test\.[cm]?[jt]sx?$/.test(path)
}

const SHIPPED: Array<[string, string]> = Object.entries(ALL_SOURCES)
  .filter(([path]) => isShipped(path))
  .sort(([a], [b]) => a.localeCompare(b))

/** `../../pages/Home.tsx` -> `pages/Home.tsx`. */
function shortPath(path: string): string {
  return path.replace(/^(\.\.\/)+/, '')
}

/**
 * Blank comment bodies, preserving length so reported offsets stay meaningful.
 *
 * Deliberately does NOT blank string literals, unlike `pageSources.ts`. Tailwind utilities live
 * inside `className="…"`, so blanking strings here would blank the very thing being scanned.
 */
function stripComments(source: string, path: string): string {
  const out = source.split('')
  // `//` opens a comment in TS/TSX but not in CSS, where it would eat the rest of a `url(https://…)`.
  const lineComments = !path.endsWith('.css')
  let i = 0

  while (i < source.length) {
    if (lineComments && source[i] === '/' && source[i + 1] === '/') {
      while (i < source.length && source[i] !== '\n') out[i++] = ' '
    } else if (source[i] === '/' && source[i + 1] === '*') {
      while (i < source.length && !(source[i] === '*' && source[i + 1] === '/')) {
        out[i] = source[i] === '\n' ? '\n' : ' '
        i += 1
      }
      out[i] = ' '
      out[i + 1] = ' '
      i += 2
    } else {
      i += 1
    }
  }

  return out.join('')
}

/**
 * Typographic characters that fall inside the pictograph ranges but are punctuation, not icons.
 *
 * The PR's ruling, encoded: an arrow in running text is typography and stays; a `✓` used as a status
 * indicator is an icon wearing a character's clothes and became a real `<Check>`. Note `✓` (U+2713)
 * is absent from this list on purpose — adding it would silently repeal that ruling.
 */
const TYPOGRAPHY = new Set(['\u2190', '\u2191', '\u2192', '\u2193', '\u2194', '\u21B5'])

/**
 * Codepoint ranges wide enough to catch pictographs the narrow "emoji" ranges miss.
 *
 * Includes the dingbats and miscellaneous-symbols blocks, where `✓`, `✗`, `★` and `⚠` live, and
 * U+FE0F, the variation selector that turns an otherwise-textual character into an emoji.
 */
const PICTOGRAPH =
  /[\u{1F000}-\u{1FAFF}\u{2600}-\u{27BF}\u{2190}-\u{21FF}\u{2300}-\u{23FF}\u{2B00}-\u{2BFF}\u{FE0F}\u{1F1E6}-\u{1F1FF}]/gu

function findEmoji(source: string): string[] {
  return [...source.matchAll(PICTOGRAPH)].map(m => m[0]).filter(glyph => !TYPOGRAPHY.has(glyph))
}

/**
 * Hex colour literals.
 *
 * The trailing lookahead does what `\b` failed to do in git grep: reject a longer hex run rather
 * than matching its first six characters. A leading lookbehind rejects the tail of a longer run for
 * the same reason.
 */
const HEX = /(?<![0-9a-fA-F#])#(?:[0-9a-fA-F]{8}|[0-9a-fA-F]{6}|[0-9a-fA-F]{4}|[0-9a-fA-F]{3})(?![0-9a-fA-F])/g

/** A `dark:` variant with a utility attached. `const dark: ChartTheme` has a space and is not one. */
const DARK_VARIANT = /(?<![\w-])dark:\S/g

/** A hand-rolled inline SVG element. Recharts' own `<defs>`/`<path>` children are not `<svg>` tags. */
const INLINE_SVG = /<svg[\s>]/g

/** Tailwind gradient background utilities, in both the v3 and v4 spellings. */
const GRADIENT = /\bbg-(?:gradient|linear|radial|conic)-/g

/**
 * The two files allowed to hold raw hex: the token definitions themselves, and the chart palette.
 * Everywhere else, a colour must come from a token.
 *
 * Charts need concrete values because Recharts takes colours as props and so cannot ride the `dark`
 * variant. Both are covered by the 132-pair contrast guard instead.
 */
const HEX_ALLOWED = ['index.css', 'components/charts/chartTheme.ts']

/**
 * A `<label>…</label>` element.
 *
 * Scoping to the label is the whole point rather than an optimisation. A periodic input renders the
 * period twice: once as this qualifier inside the label, and once as a `/yr` adornment outside it.
 * Only the first reaches the accessible name, so a file-wide search for `periodQualifier` would be
 * satisfied by markup that announces nothing.
 */
const LABEL_BLOCK = /<label[\s>][\s\S]*?<\/label>/g

/** Any call to the qualifier helper. The `(` excludes the bare identifier on the import line. */
const QUALIFIER_CALL = /periodQualifier\(/g

/**
 * The qualifier wrapped in a screen-reader-only span.
 *
 * `[^<]*` spans the newline and the literal `(` that sit between the tag and the call, while
 * stopping at the next tag so a distant sr-only span elsewhere in the label cannot vouch for a
 * qualifier it does not contain.
 */
const SR_ONLY_QUALIFIER = /<span\s+className="sr-only"[^>]*>[^<]*periodQualifier\(/g

/**
 * The file that must carry a hidden qualifier, named rather than discovered — the one deliberate
 * exception to this file's discover-never-enumerate rule, and it exists because of that rule.
 *
 * A guard that only inspects files calling `periodQualifier` evaporates the moment someone deletes
 * the call, which is precisely the deletion it is here to catch: the file drops out of the scan and
 * the suite goes green on the regression. Naming the file makes its absence a failure. The sweep
 * below still covers anything new that adopts the helper.
 */
const QUALIFIER_REQUIRED = 'components/inputs/CurrencyInput.tsx'

function findQualifierViolations(source: string): string[] {
  const labels = (source.match(LABEL_BLOCK) ?? []).join('\n')
  const calls = (labels.match(QUALIFIER_CALL) ?? []).length
  const hidden = (labels.match(SR_ONLY_QUALIFIER) ?? []).length

  if (calls === 0) {
    return ['no period qualifier inside <label> (the /yr adornment is outside it and does not count)']
  }
  if (hidden < calls) {
    return [`${calls - hidden} qualifier(s) rendered as visible label text instead of sr-only`]
  }
  return []
}

function report(hits: Array<[string, string[]]>): string {
  return hits.map(([path, found]) => `  ${path}: ${found.join(' ')}`).join('\n')
}

function scan(match: (source: string, path: string) => string[]): Array<[string, string[]]> {
  return SHIPPED.map(([path, source]): [string, string[]] => [
    shortPath(path),
    match(stripComments(source, path), path),
  ]).filter(([, found]) => found.length > 0)
}

describe('design invariant detectors', () => {
  // Every detector proves it works before any of them is believed about real source. A detector
  // that matches nothing and a detector that matches everything both report "clean".
  it('flags pictographs and spares typography', () => {
    expect(findEmoji('const icon = "🔥"')).toEqual(['🔥'])
    expect(findEmoji('<span>✓ done</span>')).toEqual(['✓'])
    expect(findEmoji('⚠️')).toHaveLength(2) // the glyph and its variation selector
    expect(findEmoji('Save → share')).toEqual([]) // the PR's ruling: arrows are punctuation
    expect(findEmoji('const total = price * 2 // $40,000')).toEqual([])
  })

  it('flags hex colours of every length without truncating longer runs', () => {
    expect('color: #b54100'.match(HEX)).toEqual(['#b54100'])
    expect('#fff and #ffff and #ffffffff'.match(HEX)).toEqual(['#fff', '#ffff', '#ffffffff'])
    // The bug that made a real scan return zero: matching the first 6 of a 7-char run, or refusing
    // to match at all. Neither may happen.
    expect('#1234567'.match(HEX)).toBeNull()
    expect('href="#section"'.match(HEX)).toBeNull()
  })

  it('flags dark: utilities but not type annotations', () => {
    expect('className="dark:bg-black"'.match(DARK_VARIANT)).toHaveLength(1)
    expect('const dark: ChartTheme = {}'.match(DARK_VARIANT)).toBeNull()
  })

  it('flags inline svg but not svg-ish identifiers', () => {
    expect('<svg className="h-4">'.match(INLINE_SVG)).toHaveLength(1)
    expect('<svg>'.match(INLINE_SVG)).toHaveLength(1)
    expect('const svgPath = "M0 0"'.match(INLINE_SVG)).toBeNull()
  })

  it('flags gradient utilities but not SVG gradient defs', () => {
    expect('className="bg-gradient-to-r"'.match(GRADIENT)).toHaveLength(1)
    expect('className="bg-linear-to-br"'.match(GRADIENT)).toHaveLength(1)
    expect('<linearGradient id="g">'.match(GRADIENT)).toBeNull()
  })

  // This detector guards markup that renders nothing, so both ways of losing it have to be caught:
  // rendering the qualifier visibly, and deleting it outright. Each is exercised here before the
  // detector is believed about real source.
  it('accepts a qualifier hidden inside the label', () => {
    const shipped = `
      <label htmlFor={id} className="flex items-center gap-1.5">
        {label}
        {periodic && (
          <span className="sr-only">
            ({periodQualifier(displayPeriod)})
          </span>
        )}
        {tooltip && <Tooltip content={tooltip} />}
      </label>
      <span className="absolute right-3 pointer-events-none">{periodSuffix(displayPeriod)}</span>`
    expect(findQualifierViolations(shipped)).toEqual([])
  })

  it('flags a qualifier rendered as visible label text', () => {
    const visible = `
      <label htmlFor={id}>
        {label}
        <span className="text-xs font-normal text-content-subtle">
          ({periodQualifier(displayPeriod)})
        </span>
      </label>`
    expect(findQualifierViolations(visible)).toHaveLength(1)
    expect(findQualifierViolations(visible)[0]).toContain('visible label text')
  })

  it('flags a deleted qualifier, and is not fooled by the adornment outside the label', () => {
    // The exact shape of the regression: the span looks like dead markup, so it goes, and the `/yr`
    // adornment below is mistaken for the period surviving. It is outside the label and does not.
    const deleted = `
      <label htmlFor={id}>
        {label}
        {tooltip && <Tooltip content={tooltip} />}
      </label>
      <span className="absolute right-3 pointer-events-none">{periodQualifier(displayPeriod)}</span>`
    expect(findQualifierViolations(deleted)).toHaveLength(1)
    expect(findQualifierViolations(deleted)[0]).toContain('no period qualifier inside <label>')
  })

  it('flags a qualifier that is hidden once but also printed visibly', () => {
    const both = `
      <label htmlFor={id}>
        {label}
        <span className="sr-only">({periodQualifier(displayPeriod)})</span>
        <span className="text-xs">({periodQualifier(displayPeriod)})</span>
      </label>`
    expect(findQualifierViolations(both)).toHaveLength(1)
  })

  it('does not let a distant sr-only span vouch for a visible qualifier', () => {
    const decoy = `
      <label htmlFor={id}>
        <span className="sr-only">Required</span>
        <span className="text-xs">({periodQualifier(displayPeriod)})</span>
      </label>`
    expect(findQualifierViolations(decoy)).toHaveLength(1)
  })

  it('strips comments without blanking the strings utilities live in', () => {
    expect(stripComments('// dark:text-red\nclassName="p-2"', 'a.tsx')).toContain('className="p-2"')
    expect(stripComments('// dark:text-red\n', 'a.tsx')).not.toContain('dark:')
    expect(stripComments('/* dark:text-red */', 'a.css')).not.toContain('dark:')
    // `//` opens no comment in CSS, so a URL must survive it.
    expect(stripComments('background: url(https://x/y)', 'a.css')).toContain('https://x/y')
  })
})

describe('scanned file set', () => {
  // A guard whose glob quietly stops matching passes over nothing at all. These assertions make that
  // present as a failure instead of as success, which is the failure mode #73 forbids.
  it('discovers the shipped source tree', () => {
    expect(SHIPPED.length).toBeGreaterThan(60)
    expect(SHIPPED.every(([, source]) => source.length > 0)).toBe(true)
  })

  it('includes a file from every area the invariants cover', () => {
    const paths = SHIPPED.map(([path]) => shortPath(path))
    for (const expected of [
      'index.css',
      'pages/Home.tsx',
      'config/calculators.ts',
      'components/layout/Sidebar.tsx',
      'components/charts/chartTheme.ts',
      'components/ui/ResultCard.tsx',
    ]) {
      expect(paths).toContain(expected)
    }
    expect(paths.filter(path => path.endsWith('.css')).length).toBeGreaterThan(0)
  })

  // The whole point of `css: true`. Without it this file arrives as '' and every invariant passes
  // over it vacuously — a clean report on a file nobody opened.
  it('reads real source, not a stub', () => {
    const css = SHIPPED.find(([path]) => shortPath(path) === 'index.css')?.[1] ?? ''
    expect(css.length).toBeGreaterThan(1000)
    expect(css).toContain('@custom-variant dark')
    expect(css).toContain('--app-surface')
  })

  it('excludes tests, so the invariants describe shipped output only', () => {
    const paths = SHIPPED.map(([path]) => shortPath(path))
    expect(paths.some(path => path.includes('__tests__'))).toBe(false)
    // This file is the proof: it is full of banned glyphs and must never be scanned.
    expect(paths).not.toContain('utils/__tests__/designInvariants.test.ts')
  })
})

describe('web UI design invariants', () => {
  it('ships no emoji or pictographic icons', () => {
    const hits = scan(source => findEmoji(source))
    expect(hits.length, `Use a lucide icon instead of a glyph:\n${report(hits)}`).toBe(0)
  })

  it('ships no raw hex colours outside the token definitions', () => {
    const hits = scan((source, path) =>
      HEX_ALLOWED.some(allowed => shortPath(path) === allowed) ? [] : (source.match(HEX) ?? []),
    )
    expect(hits.length, `Use a semantic token or chartTheme:\n${report(hits)}`).toBe(0)
  })

  it('ships no dark: utilities, because the token layer resolves the theme', () => {
    const hits = scan(source => source.match(DARK_VARIANT) ?? [])
    expect(hits.length, `Use a semantic token; it flips itself:\n${report(hits)}`).toBe(0)
  })

  it('ships no hand-rolled inline SVG', () => {
    const hits = scan((source, path) => (path.endsWith('.tsx') ? (source.match(INLINE_SVG) ?? []) : []))
    expect(hits.length, `Use a lucide icon:\n${report(hits)}`).toBe(0)
  })

  it('ships no gradient backgrounds', () => {
    const hits = scan(source => source.match(GRADIENT) ?? [])
    expect(hits.length, `Use a flat surface token:\n${report(hits)}`).toBe(0)
  })
})

describe('accessible name invariants', () => {
  /**
   * The period must survive in the accessible name of every periodic input.
   *
   * Unlike the five bans above, this asserts the PRESENCE of markup that renders nothing. The span
   * is invisible by design and therefore looks removable; removing it silently drops "(per month)"
   * from what a screen reader announces, on a field whose whole ambiguity is the period. Nothing
   * else in the repo notices, because the visible output is byte-identical either way.
   */
  it('keeps the period qualifier inside the label, hidden rather than deleted', () => {
    const source = SHIPPED.find(([path]) => shortPath(path) === QUALIFIER_REQUIRED)?.[1]

    // Absence is a failure, not a skip. If this file is renamed, the guard must be moved with it.
    expect(source, `${QUALIFIER_REQUIRED} not found; move this guard to its new path`).toBeDefined()

    const violations = findQualifierViolations(stripComments(source ?? '', QUALIFIER_REQUIRED))
    expect(
      violations,
      `${QUALIFIER_REQUIRED}: ${violations.join('; ')}\n` +
        'The qualifier renders nothing on screen but carries the period into the accessible name. ' +
        'The /yr adornment sits outside the <label> and does not replace it.',
    ).toEqual([])
  })

  // The named file above catches deletion; this catches anything new that adopts the helper and
  // prints it visibly, which would reintroduce the label wrap the fix removed.
  //
  // `includes`, not `QUALIFIER_CALL.test`: that regex carries the `g` flag, so `.test` advances
  // `lastIndex` and returns false on every second call against the same input. A stateful filter
  // would skip half the files and still report clean.
  it('renders no period qualifier as visible label text anywhere', () => {
    const hits = scan((source, path) =>
      path.endsWith('.tsx') && source.includes('periodQualifier(') ? findQualifierViolations(source) : [],
    ).filter(([, found]) => found.some(problem => problem.includes('visible label text')))

    expect(hits.length, `Wrap it in <span className="sr-only">:\n${report(hits)}`).toBe(0)
  })
})
