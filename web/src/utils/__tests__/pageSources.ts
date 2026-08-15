/**
 * One parser for every page-source scan the export suites do.
 *
 * `excelExport.test.ts` and `excelFormulaEquivalence.test.ts` both need to read what the calculator
 * pages actually pass to the export helpers. #68 hardened this scan once — comments and string
 * literals are blanked length-preservingly before anything counts a brace, and an unbalanced literal
 * throws rather than returning a short list — and #73 requires that hardening be reused rather than
 * re-implemented. A second parser is a second set of blind spots, and the two would drift.
 *
 * Not a test file: it declares no tests and is imported only by tests. It lives under `src/` so
 * `tsconfig.json` (`include: ["src"]`) type-checks it during `npm run build`, the same reason
 * `oracles.ts` and `parityFixtures.ts` do.
 */

/**
 * The source of every calculator page, read through Vite rather than `node:fs` so the suites keep
 * type-checking under `npm run build` without pulling in Node type definitions.
 *
 * Exported as the live object: `excelExport.test.ts` swaps an entry in place to prove the scanner
 * survives hostile source, so this must stay the same reference everywhere.
 */
export const PAGE_SOURCES = import.meta.glob('../../pages/*.tsx', {
  query: '?raw',
  import: 'default',
  eager: true,
}) as Record<string, string>

/** `../../pages/StandardFIRE.tsx` -> `StandardFIRE.tsx`. */
export function pageFileName(path: string): string {
  return path.split('/').pop() ?? path
}

/**
 * Replace the contents of comments and string literals with spaces, preserving length and so every
 * offset, before anything counts a brace.
 *
 * A `//` comment or a string holding an unbalanced `)` would otherwise end a scan early and drop
 * every key after it, silently. That is the one failure mode these suites cannot afford: they would
 * still pass, over a shorter list, which is indistinguishable from success.
 */
export function blankOutCommentsAndStrings(source: string): string {
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

/**
 * Index of the bracket closing the one that opens at `open`, counting on blanked source.
 *
 * Returns -1 when the source runs out first. Every caller turns that into a throw: a scan that
 * quietly returns fewer keys is worse than no scan at all, because it reports success over fields
 * nobody checked.
 */
function matchBracket(blanked: string, open: number): number {
  let depth = 0

  for (let i = open; i < blanked.length; i += 1) {
    const char = blanked[i]
    if (char === '{' || char === '[' || char === '(') depth += 1
    else if (char === '}' || char === ']' || char === ')') {
      depth -= 1
      if (depth === 0) return i
    }
  }

  return -1
}

/** Property names declared directly on an object literal body, ignoring anything nested. */
export function topLevelKeys(body: string): string[] {
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
export function topLevelEntries(blanked: string, raw: string): Array<[string, string]> {
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

/** Top-level comma-separated segments of an argument list, as raw text. */
export function topLevelSegments(blanked: string, raw: string): string[] {
  const segments: string[] = []
  let depth = 0
  let start = 0

  const take = (from: number, to: number) => {
    if (blanked.slice(from, to).trim()) segments.push(raw.slice(from, to))
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

  return segments
}

/** Raw and blanked text of one bracketed body, sliced at identical offsets. */
export interface LiteralBody {
  blanked: string
  raw: string
}

/**
 * Every bracketed body whose opening bracket is the last character matched by `marker`.
 *
 * `marker` is matched against blanked source, so a mention inside a comment or a string never
 * produces a phantom body.
 */
function readBodies(path: string, marker: RegExp, what: string): LiteralBody[] {
  const raw = PAGE_SOURCES[path]
  const blanked = blankOutCommentsAndStrings(raw)
  const bodies: LiteralBody[] = []

  for (const match of blanked.matchAll(marker)) {
    const open = match.index + match[0].length - 1
    const close = matchBracket(blanked, open)
    if (close === -1) throw new Error(`Could not find the end of the ${what} in ${path}`)
    bodies.push({ blanked: blanked.slice(open + 1, close), raw: raw.slice(open + 1, close) })
  }

  return bodies
}

/**
 * Read the object literals the pages actually pass to the export helpers.
 *
 * The input shapes are built inline inside each page's export handler, so there is nothing to
 * import. Listing them here by hand would reintroduce exactly the drift these suites exist to catch:
 * the list would be correct the day it was written and quietly wrong afterwards. Reading the call
 * sites keeps the check tied to shipping code, so adding `mortgageBalance: …` to a page fails
 * rather than surfacing in someone's spreadsheet.
 */
export function readCallSiteKeys(fnName: string): Map<string, string[]> {
  const byFile = new Map<string, string[]>()

  for (const path of Object.keys(PAGE_SOURCES)) {
    const keys = readBodies(path, callMarker(fnName), `${fnName} call`).flatMap(body =>
      topLevelKeys(body.blanked),
    )
    if (keys.length > 0) byFile.set(pageFileName(path), keys)
  }

  return byFile
}

/** As `readCallSiteKeys`, but pairs each key with the raw source text of its value. */
export function readCallSiteEntries(fnName: string): Map<string, Array<[string, string]>> {
  const byFile = new Map<string, Array<[string, string]>>()

  for (const path of Object.keys(PAGE_SOURCES)) {
    const entries = readBodies(path, callMarker(fnName), `${fnName} call`).flatMap(body =>
      topLevelEntries(body.blanked, body.raw),
    )
    if (entries.length > 0) byFile.set(pageFileName(path), entries)
  }

  return byFile
}

function callMarker(fnName: string): RegExp {
  return new RegExp(`\\b${fnName}\\s*\\(\\s*\\{`, 'g')
}

/** Every `fnName(` call in a page that is followed by an object literal. */
export function countLiteralCallSites(path: string, fnName: string): number {
  const blanked = blankOutCommentsAndStrings(PAGE_SOURCES[path])
  let count = 0

  for (const call of blanked.matchAll(new RegExp(`\\b${fnName}\\s*\\(`, 'g'))) {
    // Pages either build the shape inline or hand over a result object wholesale. Only the inline
    // ones are this scanner's job.
    if (/^\s*\{/.test(blanked.slice(call.index + call[0].length))) count += 1
  }

  return count
}

/** The `resultFormulas: { … }` marker, exported so vacuity guards locate literals the same way. */
export const RESULT_FORMULAS_MARKER = /\bresultFormulas\s*:\s*\{/g

/** One declared `resultFormulas` entry. */
export interface DeclaredFormula {
  /** Raw source text of the value, which is not always a bare string — see SavingsRate.tsx. */
  expression: string
  /** Every `{ref}` named anywhere in the value, in source order. */
  refs: string[]
}

/**
 * Read each page's `resultFormulas` object literal as key -> declared value.
 *
 * Keys are read from blanked source so nesting counts correctly; each value is recovered from the
 * RAW slice at the same offsets, because blanking emptied every string literal.
 */
export function readResultFormulas(): Map<string, Map<string, DeclaredFormula>> {
  const byFile = new Map<string, Map<string, DeclaredFormula>>()

  for (const path of Object.keys(PAGE_SOURCES)) {
    const formulas = new Map<string, DeclaredFormula>()

    for (const body of readBodies(path, RESULT_FORMULAS_MARKER, 'resultFormulas')) {
      for (const [key, expression] of topLevelEntries(body.blanked, body.raw)) {
        formulas.set(key, {
          expression,
          refs: [...expression.matchAll(/\{([A-Za-z_$][\w$]*)\}/g)].map(match => match[1]),
        })
      }
    }

    if (formulas.size > 0) byFile.set(pageFileName(path), formulas)
  }

  return byFile
}

/** The calculator invocation whose result a page exports. */
export interface CalculatorCall {
  /** The exported name in `../calculations`, e.g. `calculateHealthcareGap`. */
  fnName: string
  /** Positional argument expressions, or `null` when the page passes a single object literal. */
  positional: string[] | null
  /** Object-literal argument entries, or `null` when the page passes positional arguments. */
  named: Array<[string, string]> | null
}

/**
 * Locate the calculator call whose output a page hands to `prepareResultsForExport`.
 *
 * Derived rather than declared: a hand-kept page -> calculator map would still agree with itself
 * after a page started passing `params.currentAge` where it used to pass `params.retirementAge`,
 * which is precisely the drift an equivalence check exists to notice.
 *
 * Throws on any page it cannot follow. A page whose results it cannot resolve is a page whose
 * formulas go unchecked, and silently checking nothing is the failure mode #73 forbids.
 */
export function readCalculatorCall(file: string): CalculatorCall {
  const path = Object.keys(PAGE_SOURCES).find(candidate => pageFileName(candidate) === file)
  if (!path) throw new Error(`No page source named ${file}`)

  const raw = PAGE_SOURCES[path]
  const blanked = blankOutCommentsAndStrings(raw)

  const exported = [...blanked.matchAll(/\bprepareResultsForExport\s*\(\s*([A-Za-z_$][\w$]*)\s*\)/g)]
  if (exported.length !== 1) {
    throw new Error(
      `${file} does not pass exactly one identifier to prepareResultsForExport (found ` +
        `${exported.length}), so this scan cannot tell which calculator produced the exported ` +
        'results. Teach the scanner the new shape — do not skip the page.',
    )
  }
  const resultsIdentifier = exported[0][1]

  const binding = new RegExp(
    `\\bconst\\s+${resultsIdentifier}\\s*=\\s*useMemo\\s*\\(\\s*\\(\\s*\\)\\s*=>\\s*([A-Za-z_$][\\w$]*)\\s*\\(`,
    'g',
  )
  const bindings = [...blanked.matchAll(binding)]
  if (bindings.length !== 1) {
    throw new Error(
      `${file} has ${bindings.length} \`const ${resultsIdentifier} = useMemo(() => calculateX(…))\` ` +
        'bindings; this scan needs exactly one. Teach the scanner the new shape — do not skip the page.',
    )
  }

  const match = bindings[0]
  const fnName = match[1]
  const open = match.index + match[0].length - 1
  const close = matchBracket(blanked, open)
  if (close === -1) throw new Error(`Could not find the end of the ${fnName} call in ${file}`)

  const argsBlanked = blanked.slice(open + 1, close)
  const argsRaw = raw.slice(open + 1, close)

  if (argsBlanked.trim().startsWith('{')) {
    const objectOpen = argsBlanked.indexOf('{')
    const objectClose = matchBracket(argsBlanked, objectOpen)
    if (objectClose === -1) throw new Error(`Could not find the end of the ${fnName} argument in ${file}`)
    if (argsBlanked.slice(objectClose + 1).trim()) {
      throw new Error(
        `${file} passes an object literal plus further arguments to ${fnName}; this scan handles ` +
          'one or the other. Teach the scanner the new shape — do not skip the page.',
      )
    }
    return {
      fnName,
      positional: null,
      named: topLevelEntries(
        argsBlanked.slice(objectOpen + 1, objectClose),
        argsRaw.slice(objectOpen + 1, objectClose),
      ),
    }
  }

  return { fnName, positional: topLevelSegments(argsBlanked, argsRaw), named: null }
}
