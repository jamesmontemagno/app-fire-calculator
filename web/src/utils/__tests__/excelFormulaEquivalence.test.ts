import { describe, expect, it } from 'vitest'

import * as calculations from '../calculations'
import { prepareInputsForExport, prepareResultsForExport } from '../excelExport'
import type { CalculatorParams } from '../../hooks/useCalculatorParams'
import {
  PAGE_SOURCES,
  blankOutCommentsAndStrings,
  pageFileName,
  readCalculatorCall,
  readCallSiteEntries,
  readResultFormulas,
} from './pageSources'

/**
 * Does each exported Excel formula compute the number the app shows?
 *
 * #66 found six wrong `resultFormulas` strings across six pages. Two were dead keys, keyed to fields
 * the calculation never returns, so the formula silently never attached; the guard in
 * `excelExport.test.ts` closed that. The other four had correct keys and wrong ARITHMETIC — most of
 * them a guard present in `calculations.ts` and absent from the formula, so the spreadsheet a user
 * downloads renders `#DIV/0!` while the app calmly shows `0%`. Those five were caught by reading
 * carefully, and nothing stopped the next one.
 *
 * This suite substitutes real input values into each declared formula, evaluates it, and compares
 * the answer to the field the shipped calculation returns. A formula string and the function it
 * mirrors live in different files; this is the only thing tying them together.
 *
 * Everything except the scenario numbers is derived from shipping source. The page's own
 * `prepareInputsForExport` literal supplies the `{ref}` → value binding, and the page's own
 * `calculateX(...)` call supplies the calculation. A hand-written copy of either would agree with
 * itself forever while the page moved underneath it — and would also miss a page that swapped two
 * positional arguments, which this catches because the two sides then disagree.
 *
 * It fails closed, deliberately and everywhere. An unparseable formula, an unknown function, a
 * `{ref}` that resolves to nothing, a page whose calculator call cannot be followed, or a formula no
 * scenario reached is a FAILURE. This repo has shipped four checks whose blind spot happened to
 * match the thing they were checking, each passing at full green over an incomplete set. A skip here
 * would be the fifth.
 */

// ================================================================================================
// The formula language
// ================================================================================================

/**
 * The full vocabulary in use, verified by extracting every declared formula literal and tokenizing
 * it rather than by trusting a list: `+ - * /`, parentheses, `MAX`, `IF` with `>`, number literals,
 * and `{ref}` placeholders. Nothing else appears — no `MIN`, no `>=`, no cell ranges.
 *
 * Anything outside it throws. Excel would accept far more, but a formula this suite cannot evaluate
 * is a formula nobody is checking, and quietly passing over it is the failure this file exists to
 * prevent. Widen the evaluator when a page needs more; do not let it shrug.
 */
class FormulaError extends Error {}

type Token = { kind: 'number'; value: number } | { kind: 'ref'; name: string } | { kind: 'name'; name: string } | { kind: 'symbol'; value: string }

function tokenize(formula: string): Token[] {
  const tokens: Token[] = []
  let i = 0

  while (i < formula.length) {
    const char = formula[i]

    if (/\s/.test(char)) {
      i += 1
    } else if (/[0-9.]/.test(char)) {
      const match = /^[0-9]*\.?[0-9]+/.exec(formula.slice(i))
      if (!match) throw new FormulaError(`Not a number at offset ${i}: ${formula.slice(i, i + 8)}`)
      tokens.push({ kind: 'number', value: Number(match[0]) })
      i += match[0].length
    } else if (char === '{') {
      const close = formula.indexOf('}', i)
      if (close === -1) throw new FormulaError(`Unclosed {ref} at offset ${i}`)
      tokens.push({ kind: 'ref', name: formula.slice(i + 1, close) })
      i = close + 1
    } else if (/[A-Za-z_]/.test(char)) {
      const match = /^[A-Za-z_][A-Za-z0-9_.]*/.exec(formula.slice(i))!
      tokens.push({ kind: 'name', name: match[0] })
      i += match[0].length
    } else if ('+-*/(),>'.includes(char)) {
      tokens.push({ kind: 'symbol', value: char })
      i += 1
    } else {
      throw new FormulaError(
        `Unsupported character '${char}' at offset ${i} of \`${formula}\`. This evaluator covers ` +
          'only the vocabulary the pages use: + - * / ( ) , > MAX IF numbers {refs}.',
      )
    }
  }

  return tokens
}

type Node =
  | { kind: 'number'; value: number }
  | { kind: 'ref'; name: string }
  | { kind: 'binary'; operator: string; left: Node; right: Node }
  | { kind: 'call'; name: string; args: Node[] }

/**
 * Parsed to a tree first so `IF` can evaluate its branches lazily, the way Excel does.
 *
 * Eager evaluation would compute the guarded division even when the guard says not to, so
 * `IF({annualIncome}>0,{annualContribution}/{annualIncome},0)` — the exact shape #66 was about —
 * would divide by zero on the branch it exists to avoid.
 */
function parseFormula(formula: string): Node {
  const tokens = tokenize(formula)
  let position = 0

  const peek = () => tokens[position]
  const expectSymbol = (value: string, what: string) => {
    const token = peek()
    if (!token || token.kind !== 'symbol' || token.value !== value) {
      throw new FormulaError(`Expected '${value}' ${what} in \`${formula}\``)
    }
    position += 1
  }

  const parseComparison = (): Node => {
    const left = parseSum()
    const token = peek()
    if (token && token.kind === 'symbol' && token.value === '>') {
      position += 1
      return { kind: 'binary', operator: '>', left, right: parseSum() }
    }
    return left
  }

  function parseSum(): Node {
    let left = parseProduct()
    for (;;) {
      const token = peek()
      if (!token || token.kind !== 'symbol' || (token.value !== '+' && token.value !== '-')) return left
      position += 1
      left = { kind: 'binary', operator: token.value, left, right: parseProduct() }
    }
  }

  function parseProduct(): Node {
    let left = parseUnary()
    for (;;) {
      const token = peek()
      if (!token || token.kind !== 'symbol' || (token.value !== '*' && token.value !== '/')) return left
      position += 1
      left = { kind: 'binary', operator: token.value, left, right: parseUnary() }
    }
  }

  function parseUnary(): Node {
    const token = peek()
    if (token && token.kind === 'symbol' && (token.value === '-' || token.value === '+')) {
      position += 1
      const operand = parseUnary()
      return token.value === '-'
        ? { kind: 'binary', operator: '-', left: { kind: 'number', value: 0 }, right: operand }
        : operand
    }
    return parsePrimary()
  }

  function parsePrimary(): Node {
    const token = peek()
    if (!token) throw new FormulaError(`Unexpected end of \`${formula}\``)

    if (token.kind === 'number') {
      position += 1
      return { kind: 'number', value: token.value }
    }

    if (token.kind === 'ref') {
      position += 1
      return { kind: 'ref', name: token.name }
    }

    if (token.kind === 'name') {
      position += 1
      expectSymbol('(', `after ${token.name}`)
      const args: Node[] = []
      if (!(peek()?.kind === 'symbol' && (peek() as { value: string }).value === ')')) {
        args.push(parseComparison())
        while (peek()?.kind === 'symbol' && (peek() as { value: string }).value === ',') {
          position += 1
          args.push(parseComparison())
        }
      }
      expectSymbol(')', `closing ${token.name}(`)
      return { kind: 'call', name: token.name, args }
    }

    if (token.value === '(') {
      position += 1
      const inner = parseComparison()
      expectSymbol(')', 'closing a group')
      return inner
    }

    throw new FormulaError(`Unexpected '${token.value}' in \`${formula}\``)
  }

  const tree = parseComparison()
  if (position !== tokens.length) {
    throw new FormulaError(
      `Trailing input in \`${formula}\` after offset ${position} of ${tokens.length} tokens. The ` +
        'formula parsed only in part, so the rest went unchecked.',
    )
  }
  return tree
}

/** Excel functions this evaluator implements, with the arity it accepts. */
const FUNCTIONS: Record<string, { minArgs: number; maxArgs: number }> = {
  MAX: { minArgs: 1, maxArgs: 255 },
  IF: { minArgs: 3, maxArgs: 3 },
}

export function evaluateFormula(formula: string, scope: Record<string, unknown>): number {
  const evaluate = (node: Node): number | boolean => {
    switch (node.kind) {
      case 'number':
        return node.value

      case 'ref': {
        if (!(node.name in scope)) {
          throw new FormulaError(
            `\`${formula}\` references {${node.name}}, which is not among the input fields this ` +
              `page exports: ${Object.keys(scope).sort().join(', ')}. In a real workbook the ` +
              '{key}→cell substitution leaves the literal text in the cell.',
          )
        }
        const value = scope[node.name]
        if (typeof value !== 'number' || !Number.isFinite(value)) {
          throw new FormulaError(
            `\`${formula}\` references {${node.name}}, whose exported value is ` +
              `${JSON.stringify(value)} rather than a finite number.`,
          )
        }
        return value
      }

      case 'binary': {
        const left = numeric(evaluate(node.left), node.operator)
        const right = numeric(evaluate(node.right), node.operator)
        switch (node.operator) {
          case '+':
            return left + right
          case '-':
            return left - right
          case '*':
            return left * right
          case '/':
            if (right === 0) {
              throw new FormulaError(
                `\`${formula}\` divides by zero. Excel renders that cell as #DIV/0! while the app ` +
                  'shows a real number — the #66 defect exactly.',
              )
            }
            return left / right
          case '>':
            return left > right
          default:
            throw new FormulaError(`Unsupported operator '${node.operator}' in \`${formula}\``)
        }
      }

      case 'call': {
        const signature = FUNCTIONS[node.name]
        if (!signature) {
          throw new FormulaError(
            `\`${formula}\` calls ${node.name}(), which this evaluator does not implement. ` +
              `Implemented: ${Object.keys(FUNCTIONS).sort().join(', ')}. Add it here — a formula ` +
              'this suite cannot evaluate is a formula nobody is checking.',
          )
        }
        if (node.args.length < signature.minArgs || node.args.length > signature.maxArgs) {
          throw new FormulaError(
            `\`${formula}\` calls ${node.name}() with ${node.args.length} argument(s); it takes ` +
              `${signature.minArgs}${signature.maxArgs === signature.minArgs ? '' : `–${signature.maxArgs}`}.`,
          )
        }

        if (node.name === 'IF') {
          const condition = evaluate(node.args[0])
          if (typeof condition !== 'boolean') {
            throw new FormulaError(`\`${formula}\` uses IF() with a non-comparison condition.`)
          }
          // Lazily, so the guarded branch is never computed when the guard says no.
          return evaluate(node.args[condition ? 1 : 2])
        }

        return Math.max(...node.args.map(arg => numeric(evaluate(arg), 'MAX')))
      }
    }
  }

  const numeric = (value: number | boolean, where: string): number => {
    if (typeof value !== 'number') {
      throw new FormulaError(`\`${formula}\` uses a comparison where '${where}' needs a number.`)
    }
    return value
  }

  const result = evaluate(parseFormula(formula))
  if (typeof result !== 'number') {
    throw new FormulaError(`\`${formula}\` evaluates to a comparison rather than a number.`)
  }
  return result
}

// ================================================================================================
// Reading a page's declared values
// ================================================================================================

/**
 * The shapes a page may use for a value this suite has to resolve — an exported input, a calculator
 * argument, or a formula.
 *
 * Most are a bare `params.something`. SavingsRate.tsx picks its formula with a ternary on
 * `params.savingsFrequency`, so one key declares two different formulas and a scanner that reads
 * "the string literal" would have to guess which. Both branches are resolved here and both are
 * required to be exercised.
 */
type ValueExpression =
  | { kind: 'literal'; value: string | number }
  | { kind: 'param'; name: string }
  | { kind: 'constant'; name: string }
  | {
      kind: 'ternary'
      param: string
      operator: '===' | '!=='
      literal: string
      whenTrue: ValueExpression
      whenFalse: ValueExpression
    }

class ExpressionError extends Error {}

/** Index of a character at nesting depth zero, ignoring anything inside brackets or strings. */
function findAtTopLevel(text: string, predicate: (blanked: string, index: number) => boolean): number {
  const blanked = blankOutCommentsAndStrings(text)
  let depth = 0

  for (let i = 0; i < blanked.length; i += 1) {
    const char = blanked[i]
    if ('([{'.includes(char)) depth += 1
    else if (')]}'.includes(char)) depth -= 1
    else if (depth === 0 && predicate(blanked, i)) return i
  }

  return -1
}

function parseValueExpression(expression: string, where: string): ValueExpression {
  const text = expression.trim()

  const question = findAtTopLevel(
    text,
    (blanked, i) => blanked[i] === '?' && blanked[i + 1] !== '.' && blanked[i + 1] !== '?' && blanked[i - 1] !== '?',
  )
  if (question !== -1) {
    const colon = findColonFor(text, question)
    if (colon === -1) throw new ExpressionError(`${where}: could not find the ':' of a ternary in \`${text}\``)
    return {
      kind: 'ternary',
      ...parseCondition(text.slice(0, question), where),
      whenTrue: parseValueExpression(text.slice(question + 1, colon), where),
      whenFalse: parseValueExpression(text.slice(colon + 1), where),
    }
  }

  const quoted = /^(['"])((?:\\.|(?!\1)[^\\])*)\1$/.exec(text)
  if (quoted) return { kind: 'literal', value: quoted[2] }

  if (/^-?[0-9][0-9_]*(\.[0-9]+)?$/.test(text)) {
    return { kind: 'literal', value: Number(text.replace(/_/g, '')) }
  }

  const param = /^params\.([A-Za-z_$][\w$]*)$/.exec(text)
  if (param) return { kind: 'param', name: param[1] }

  if (/^[A-Za-z_$][\w$]*$/.test(text)) return { kind: 'constant', name: text }

  throw new ExpressionError(
    `${where}: cannot resolve \`${text}\`. This suite understands params.x, a module constant, a ` +
      "literal, and `params.x === 'y' ? a : b`. Teach it the new shape — leaving it unresolved " +
      'would mean checking nothing here.',
  )
}

/** The `:` matching a ternary's `?`, skipping any nested ternary. */
function findColonFor(text: string, question: number): number {
  const blanked = blankOutCommentsAndStrings(text)
  let depth = 0
  let pending = 0

  for (let i = question + 1; i < blanked.length; i += 1) {
    const char = blanked[i]
    if ('([{'.includes(char)) depth += 1
    else if (')]}'.includes(char)) depth -= 1
    else if (depth === 0 && char === '?') pending += 1
    else if (depth === 0 && char === ':') {
      if (pending === 0) return i
      pending -= 1
    }
  }

  return -1
}

function parseCondition(condition: string, where: string): { param: string; operator: '===' | '!=='; literal: string } {
  const match = /^\s*params\.([A-Za-z_$][\w$]*)\s*(===|!==)\s*(['"])(.*?)\3\s*$/.exec(condition)
  if (!match) {
    throw new ExpressionError(
      `${where}: cannot resolve the ternary condition \`${condition.trim()}\`. Only ` +
        "`params.x === 'literal'` is understood.",
    )
  }
  return { param: match[1], operator: match[2] as '===' | '!==', literal: match[4] }
}

/** Numeric and string constants exported by `../calculations`, e.g. `MEDICARE_AGE`. */
const MODULE_CONSTANTS: Record<string, string | number> = Object.fromEntries(
  Object.entries(calculations as Record<string, unknown>).filter(
    (entry): entry is [string, string | number] => typeof entry[1] === 'number' || typeof entry[1] === 'string',
  ),
)

function resolveValueExpression(expression: ValueExpression, params: CalculatorParams, where: string): unknown {
  switch (expression.kind) {
    case 'literal':
      return expression.value

    case 'param': {
      if (!(expression.name in params)) {
        throw new ExpressionError(
          `${where}: the page reads params.${expression.name}, which this suite's scenarios do not ` +
            'define. Add it to every scenario rather than letting the reference resolve to undefined.',
        )
      }
      return (params as unknown as Record<string, unknown>)[expression.name]
    }

    case 'constant': {
      if (!(expression.name in MODULE_CONSTANTS)) {
        throw new ExpressionError(
          `${where}: the page reads \`${expression.name}\`, which is not a string or number ` +
            'exported by ../calculations, so this suite cannot resolve it.',
        )
      }
      return MODULE_CONSTANTS[expression.name]
    }

    case 'ternary': {
      const actual = (params as unknown as Record<string, unknown>)[expression.param]
      const equal = actual === expression.literal
      const branch = (expression.operator === '===' ? equal : !equal) ? expression.whenTrue : expression.whenFalse
      return resolveValueExpression(branch, params, where)
    }
  }
}

/** Every string a value expression can evaluate to, across all its branches. */
function reachableStringLiterals(expression: ValueExpression): string[] {
  switch (expression.kind) {
    case 'literal':
      return typeof expression.value === 'string' ? [expression.value] : []
    case 'ternary':
      return [...reachableStringLiterals(expression.whenTrue), ...reachableStringLiterals(expression.whenFalse)]
    default:
      return []
  }
}

// ================================================================================================
// Scenarios
// ================================================================================================

/**
 * Typed against the real `CalculatorParams`, so the compiler rejects a scenario that forgets a
 * field. A missing field would otherwise resolve to `undefined`, sail into a calculator, and come
 * back as `NaN` — a divergence in the test rather than in the product.
 */
const BASE_PARAMS: CalculatorParams = {
  currentAge: 30,
  retirementAge: 55,
  currentSavings: 100_000,
  annualContribution: 24_000,
  annualIncome: 72_000,
  expectedReturn: 0.07,
  inflationRate: 0.03,
  withdrawalRate: 0.04,
  annualExpenses: 48_000,
  partTimeIncome: 20_000,
  portfolioValue: 1_000_000,
  retirementYears: 30,
  debts: [],
  debtBudget: 1_000,
  debtExtra: 0,
  debtMonths: 36,
  debtMode: 'fixed',
  debtStrategy: 'snowball',
  savingsFrequency: 'monthly',
  savingsContribution: 500,
  savingsYears: 30,
  healthcareMonthlyPremium: 600,
  healthcareAnnualDeductible: 2_500,
  healthcareAnnualOutOfPocket: 2_000,
  contributionGrowth: 'inflation',
  currencyPeriod: 'annual',
}

/**
 * One happy path would agree with a broken formula and prove nothing: every guard in
 * `calculations.ts` is inactive at the defaults, so a formula missing that guard computes the same
 * number. Each scenario below turns exactly one of them on.
 */
const SCENARIOS: Array<{ id: string; why: string; params: CalculatorParams }> = [
  {
    id: 'baseline',
    why: 'the shipped defaults, where every guard is inactive',
    params: BASE_PARAMS,
  },
  {
    id: 'zero-income',
    why: 'annualIncome = 0 activates the `annualIncome > 0` guard (calculations.ts:418, :1109); ' +
      'without it the savings-rate formula divides by zero and the cell renders #DIV/0!',
    params: { ...BASE_PARAMS, annualIncome: 0 },
  },
  {
    id: 'part-time-exceeds-expenses',
    why: 'partTimeIncome > annualExpenses activates `Math.max(0, …)` (calculations.ts:606); without ' +
      'it the Barista number goes negative',
    params: { ...BASE_PARAMS, partTimeIncome: 60_000, annualExpenses: 48_000 },
  },
  {
    id: 'retire-at-medicare',
    why: 'retirementAge >= MEDICARE_AGE activates `Math.max(0, …)` (calculations.ts:1190); without ' +
      'it the healthcare gap goes negative',
    params: { ...BASE_PARAMS, retirementAge: 67 },
  },
  {
    id: 'alternate-rates',
    why: 'rates and amounts that share no factor with the defaults, so a formula that hard-codes a ' +
      'constant — `{annualExpenses}*25` for `{annualExpenses}/{withdrawalRate}` — stops agreeing',
    params: {
      ...BASE_PARAMS,
      currentAge: 41,
      retirementAge: 58,
      withdrawalRate: 0.035,
      expectedReturn: 0.061,
      inflationRate: 0.025,
      annualExpenses: 61_234,
      annualContribution: 13_579,
      annualIncome: 91_234,
      currentSavings: 250_500,
      partTimeIncome: 17_777,
      portfolioValue: 1_234_567,
      savingsContribution: 733,
      healthcareMonthlyPremium: 815,
      healthcareAnnualDeductible: 3_450,
      healthcareAnnualOutOfPocket: 1_925,
    },
  },
  {
    id: 'yearly-contributions',
    why: "savingsFrequency = 'yearly' selects the other branch of SavingsRate.tsx's formula ternary, " +
      'which no other scenario reaches',
    params: { ...BASE_PARAMS, savingsFrequency: 'yearly', savingsContribution: 9_000, annualIncome: 84_000 },
  },
]

// ================================================================================================
// How closely a formula has to agree
// ================================================================================================

/**
 * `calculations.ts` rounds some exported fields and not others, while the formula in the workbook is
 * always unrounded. So the comparison has to know which.
 *
 * The tempting shortcut — one tolerance wide enough to absorb `Math.round`, i.e. ±0.5 — is a trap,
 * and precisely the kind this repo keeps falling into: a check whose blind spot matches the thing it
 * checks. `savingsRate` is a fraction in [0, 1] exported as a percentage, so ±0.5 on that field
 * admits nearly its entire range. Concretely, SavingsRate.tsx's two formula branches differ by 12×;
 * picking the wrong one at a $1,000 monthly contribution against $72,000 of income gives 0.1667
 * where the app shows 0.0139. That is a real, in-scope defect, it is 0.153 away, and a ±0.5
 * tolerance waves it straight through.
 *
 * So instead of widening the tolerance, mirror the rounding:
 *
 *   'whole' — `Math.round(evaluated)` must EQUAL the shipped value. Not a slack knob: it is the same
 *             function `calculations.ts` applies to that field.
 *   'exact' — agreement to a relative 1e-9, i.e. float noise only.
 *
 * Both self-check. A field marked 'whole' that is not actually rounded fails the moment a scenario
 * produces a fraction; a field marked 'exact' that is rounded fails the moment one does not land on
 * an integer; and every 'whole' field is asserted to be integral and to carry a non-percent format.
 */
type Agreement = 'exact' | 'whole'

/**
 * Keyed by page and field, not by field name, because nothing guarantees that the same name rounds
 * the same way on every page. That used to be demonstrable rather than hypothetical: `fireNumber`
 * was `Math.round`ed by calculateStandardFIRE and calculateCoastFIRE but returned raw by
 * calculateReverseFIRE, keying on the name alone quietly mis-declared it, and this suite's own
 * self-check is what caught that on its first run. Issue #75 then fixed the calculator rather than
 * the declaration, so every `fireNumber` below now agrees on 'whole' and the example is historical.
 * The per-page keying stays: it is what made the divergence expressible instead of averaging it
 * away, and it is what a future one-page-rounds-differently change would need again.
 */
const AGREEMENT: Record<string, Agreement> = {
  // Rounded by calculations.ts at the line noted, so the unrounded formula is compared through the
  // same Math.round.
  'BaristaFIRE.tsx::baristaNumber': 'whole', //       :638
  'BaristaFIRE.tsx::fullFireNumber': 'whole', //      :639
  'CoastFIRE.tsx::fireNumber': 'whole', //            :550
  'FatFIRE.tsx::fireNumber': 'whole', //              :433 via calculateStandardFIRE
  'LeanFIRE.tsx::fireNumber': 'whole', //             :433 via calculateStandardFIRE
  'ReverseFIRE.tsx::fireNumber': 'whole', //          :1047
  'StandardFIRE.tsx::fireNumber': 'whole', //         :433
  'WithdrawalRate.tsx::annualWithdrawal': 'whole', // :767
  'WithdrawalRate.tsx::monthlyWithdrawal': 'whole', //:768
  // Returned unrounded, so the formula must agree to float noise.
  'FatFIRE.tsx::savingsRate': 'exact', //             :437
  'HealthcareGap.tsx::annualCost': 'exact', //        :1191
  'HealthcareGap.tsx::gapYears': 'exact', //          :1190
  'LeanFIRE.tsx::savingsRate': 'exact', //            :437
  'SavingsRate.tsx::savingsRate': 'exact', //         :1109
  'StandardFIRE.tsx::monthlyContribution': 'exact', //:438
  'StandardFIRE.tsx::savingsRate': 'exact', //        :437
}

function agreementKey(file: string, key: string): string {
  return `${file}::${key}`
}

function agrees(evaluated: number, shipped: number, agreement: Agreement): boolean {
  return agreement === 'whole'
    ? Math.round(evaluated) === shipped
    : Math.abs(evaluated - shipped) <= 1e-9 * Math.max(1, Math.abs(shipped))
}

// ================================================================================================
// The evaluation matrix
// ================================================================================================

interface EvaluationRow {
  file: string
  scenario: string
  why: string
  key: string
  formula: string
  /** Set when this one formula could not be evaluated; the row then carries no number. */
  error?: string
  evaluated: number
  shipped: unknown
  inputs: Record<string, unknown>
}

interface PageEvaluation {
  file: string
  /** Present when the page could not be read or run at all; every row is then missing. */
  error?: string
  rows: EvaluationRow[]
  /** `key::formula` pairs actually evaluated, for the coverage guard. */
  covered: Set<string>
}

const CALCULATORS = calculations as unknown as Record<string, (...args: unknown[]) => unknown>

/**
 * Run one page against one scenario exactly as the page would: resolve its exported inputs from its
 * own `prepareInputsForExport` literal, call the calculator its own `useMemo` calls with the
 * arguments it passes, and evaluate its own formulas against the resulting cells.
 */
function evaluatePage(file: string, scenario: { id: string; why: string; params: CalculatorParams }): {
  rows: EvaluationRow[]
  covered: Set<string>
} {
  const where = `${file} (${scenario.id})`
  const formulas = readResultFormulas().get(file)
  if (!formulas || formulas.size === 0) throw new ExpressionError(`${where}: no resultFormulas were read`)

  const inputEntries = readCallSiteEntries('prepareInputsForExport').get(file)
  if (!inputEntries || inputEntries.length === 0) {
    throw new ExpressionError(
      `${where}: declares resultFormulas but passes no readable object literal to ` +
        'prepareInputsForExport, so its {refs} have nothing to resolve against.',
    )
  }

  const rawInputs: Record<string, unknown> = {}
  for (const [key, expression] of inputEntries) {
    rawInputs[key] = resolveValueExpression(parseValueExpression(expression, where), scenario.params, where)
  }
  // The cells a formula actually references are the PREPARED inputs, not the raw ones.
  const inputs = prepareInputsForExport(rawInputs).values

  const call = readCalculatorCall(file)
  const calculator = CALCULATORS[call.fnName]
  if (typeof calculator !== 'function') {
    throw new ExpressionError(`${where}: ../calculations exports no function named ${call.fnName}`)
  }

  const args = call.positional
    ? call.positional.map(argument =>
        resolveValueExpression(parseValueExpression(argument, where), scenario.params, where),
      )
    : [
        Object.fromEntries(
          call.named!.map(([key, expression]) => [
            key,
            resolveValueExpression(parseValueExpression(expression, where), scenario.params, where),
          ]),
        ),
      ]

  const shipped = prepareResultsForExport(calculator(...args)).values
  const rows: EvaluationRow[] = []
  const covered = new Set<string>()

  for (const [key, declared] of formulas) {
    const expression = parseValueExpression(declared.expression, `${where} formula ${key}`)
    const formula = resolveValueExpression(expression, scenario.params, `${where} formula ${key}`)
    if (typeof formula !== 'string') {
      throw new ExpressionError(`${where}: the formula for ${key} resolved to ${typeof formula}, not a string`)
    }

    // Caught per formula, not per page: one unevaluable formula must not stop the other fields on
    // the page being checked, and the failure has to name the field it belongs to.
    const row: EvaluationRow = {
      file,
      scenario: scenario.id,
      why: scenario.why,
      key,
      formula,
      evaluated: NaN,
      shipped: shipped[key],
      inputs,
    }
    try {
      row.evaluated = evaluateFormula(formula, inputs)
    } catch (error) {
      row.error = (error as Error).message
    }

    rows.push(row)
    covered.add(`${key}::${formula}`)
  }

  return { rows, covered }
}

/**
 * Built eagerly, with failures captured per page rather than thrown during collection, so the suite
 * reports "HealthcareGap.tsx could not be read" as a named failing test instead of an unrunnable
 * file — and so assertion order never decides what gets checked.
 */
const EVALUATIONS: PageEvaluation[] = [...readResultFormulas().keys()].sort().map(file => {
  const rows: EvaluationRow[] = []
  const covered = new Set<string>()

  for (const scenario of SCENARIOS) {
    try {
      const result = evaluatePage(file, scenario)
      rows.push(...result.rows)
      for (const entry of result.covered) covered.add(entry)
    } catch (error) {
      return { file, error: `${scenario.id}: ${(error as Error).message}`, rows, covered }
    }
  }

  return { file, rows, covered }
})

function mismatchReport(row: EvaluationRow, agreement: Agreement): string {
  return [
    `${row.file} exports a formula for \`${row.key}\` that does not compute \`${row.key}\`.`,
    '',
    `  scenario   ${row.scenario} — ${row.why}`,
    `  formula    ${row.formula}`,
    `  evaluates  ${row.evaluated}`,
    `  app shows  ${String(row.shipped)}    (compared '${agreement}')`,
    '',
    `  inputs     ${JSON.stringify(row.inputs)}`,
    '',
    'The workbook a user downloads puts the formula in that cell, so the spreadsheet and the app it',
    'came from disagree. Fix the formula string on the page to match calculations.ts — do not relax',
    'this assertion, and do not widen the tolerance.',
  ].join('\n')
}

describe('exported formulas compute what the app computes', () => {
  it.each(EVALUATIONS.map(page => [page.file, page] as const))('%s can be read and run', (_file, page) => {
    expect(
      page.error,
      `${page.file} declares resultFormulas but this suite could not evaluate them: ${page.error}\n\n` +
        'A page whose formulas cannot be evaluated is a page whose formulas nobody is checking. Fix ' +
        'the scanner or the resolver — do not skip the page.',
    ).toBeUndefined()
  })

  it.each(
    EVALUATIONS.flatMap(page =>
      page.rows.map(row => [`${row.file} ${row.key} @ ${row.scenario}`, row] as const),
    ),
  )('%s', (_label, row) => {
    expect(
      row.error,
      `${row.file} exports a formula for \`${row.key}\` that could not be evaluated at all.\n\n` +
        `  scenario   ${row.scenario} — ${row.why}\n` +
        `  formula    ${row.formula}\n` +
        `  problem    ${row.error}\n\n` +
        'That formula still ships into the user\'s workbook. Fix it on the page, or widen the ' +
        'evaluator if the page needs vocabulary it does not cover — do not skip it.',
    ).toBeUndefined()

    const agreement = AGREEMENT[agreementKey(row.file, row.key)]
    expect(
      agreement,
      `${row.file} declares a formula for \`${row.key}\`, but AGREEMENT in this file has no ` +
        `'${agreementKey(row.file, row.key)}' entry saying how closely it must match. Look up ` +
        "whether calculations.ts rounds that field and add 'whole' or 'exact' — an undeclared " +
        'field would otherwise go unchecked.',
    ).toBeDefined()

    if (typeof row.shipped !== 'number') {
      // The app writes 'Not reachable' for a non-finite scalar; the formula must be non-finite too.
      expect(
        Number.isFinite(row.evaluated),
        `${row.file} shows ${JSON.stringify(row.shipped)} for \`${row.key}\` but its formula ` +
          `evaluates to the finite number ${row.evaluated}.`,
      ).toBe(false)
      return
    }

    if (agreement === 'whole') {
      expect(
        Number.isInteger(row.shipped),
        `${row.key} is declared 'whole' — mirroring a Math.round in calculations.ts — but the app ` +
          `returned the non-integer ${row.shipped} for it in the ${row.scenario} scenario. Either ` +
          "the rounding is gone and this should be 'exact', or the field changed shape.",
      ).toBe(true)
    }

    expect(agrees(row.evaluated, row.shipped, agreement), mismatchReport(row, agreement)).toBe(true)
  })
})

// ================================================================================================
// Coverage: no formula, page, or branch goes unevaluated
// ================================================================================================

describe('every declared formula was actually evaluated', () => {
  const declared = readResultFormulas()

  it('reaches every page that declares resultFormulas', () => {
    // Not a page count: `expect(pages).toBe(9)` passes the day a tenth page is added and missed,
    // which is the permissive direction. Locate each literal independently instead and require an
    // evaluation for it.
    //
    // Deliberately a looser pattern than the scanner's own RESULT_FORMULAS_MARKER, and written
    // separately rather than imported. Sharing the marker would mean a marker that tightened into
    // missing a page would also stop this guard seeing that page — the check and the thing it
    // checks failing together, which is how #68's truncated key list passed at full green.
    let literalSites = 0

    for (const path of Object.keys(PAGE_SOURCES)) {
      const file = pageFileName(path)
      const blanked = blankOutCommentsAndStrings(PAGE_SOURCES[path])

      for (const _ of blanked.matchAll(/\bresultFormulas\b/g)) {
        literalSites += 1
        const page = EVALUATIONS.find(evaluation => evaluation.file === file)
        expect(
          page,
          `${file} declares a resultFormulas literal that this suite never evaluated. Its exported ` +
            'formulas are unchecked. Fix the scanner or the resolver — do not delete this assertion.',
        ).toBeDefined()
        expect(page!.rows.length, `${file} was located but produced no evaluations.`).toBeGreaterThan(0)
      }
    }

    expect(
      literalSites,
      'No resultFormulas literal was found in any page, so every assertion above ran over an empty ' +
        'set. Either the pages stopped declaring formulas or the marker stopped matching.',
    ).toBeGreaterThan(0)
  })

  it('evaluates every formula string, including both branches of a ternary', () => {
    // SavingsRate.tsx declares two formulas under one key and picks between them at runtime. A
    // scenario set that only ever reaches one branch leaves the other shipping unchecked.
    for (const [file, formulas] of declared) {
      const page = EVALUATIONS.find(evaluation => evaluation.file === file)
      expect(page, `${file} declares formulas but has no evaluation`).toBeDefined()

      for (const [key, formula] of formulas) {
        const literals = reachableStringLiterals(parseValueExpression(formula.expression, `${file} ${key}`))
        expect(
          literals.length,
          `${file} declares \`${key}\` as an expression that can produce no formula string at all.`,
        ).toBeGreaterThan(0)

        for (const literal of literals) {
          expect(
            page!.covered.has(`${key}::${literal}`),
            `${file} can export \`${literal}\` for ${key}, but no scenario ever produced it, so it ` +
              'was never evaluated. Add a scenario that selects that branch.',
          ).toBe(true)
        }
      }
    }
  })

  it('declares how closely every formula key must agree', () => {
    const keys = [...declared]
      .flatMap(([file, formulas]) => [...formulas.keys()].map(key => agreementKey(file, key)))
      .sort()

    const undeclared = keys.filter(key => !(key in AGREEMENT))
    expect(
      undeclared,
      `No agreement mode declared for: ${undeclared.join(', ')}. Check whether calculations.ts ` +
        "rounds each field and add 'whole' or 'exact'.",
    ).toEqual([])

    // And the other direction, so the map cannot rot into entries guarding nothing.
    const orphaned = Object.keys(AGREEMENT).filter(key => !keys.includes(key))
    expect(
      orphaned,
      `AGREEMENT declares modes for formulas no page exports any more: ${orphaned.join(', ')}. ` +
        'Remove them; a stale entry reads as coverage that is not there.',
    ).toEqual([])

    expect(keys.length).toBeGreaterThan(0)
  })

  it("never lets a percentage field be compared as 'whole'", () => {
    // The trap this file's comparison design exists to avoid: rounding-to-whole on a value in
    // [0, 1] would accept literally any formula that produces a fraction below 0.5.
    for (const [key, agreement] of Object.entries(AGREEMENT)) {
      if (agreement !== 'whole') continue
      const field = key.split('::')[1]
      const format = prepareResultsForExport({ [field]: 0.5 }).formats[field]
      expect(
        format,
        `${key} is exported as a ${format} but compared as 'whole'. Rounding a fraction to an ` +
          'integer accepts almost anything.',
      ).not.toBe('percent')
    }
  })

  it('checks every scenario, not just the happy path', () => {
    for (const page of EVALUATIONS) {
      if (page.error) continue
      const scenarios = new Set(page.rows.map(row => row.scenario))
      expect(
        [...scenarios].sort(),
        `${page.file} was not evaluated against every scenario, so a guard one of them exists to ` +
          'exercise went unchecked.',
      ).toEqual(SCENARIOS.map(scenario => scenario.id).sort())
    }
  })
})

// ================================================================================================
// The evaluator fails closed
// ================================================================================================

describe('the formula evaluator refuses to guess', () => {
  const scope = { a: 10, b: 4, zero: 0, text: 'monthly' }

  it.each([
    ['{a}+{b}', 14],
    ['{a}-{b}', 6],
    ['{a}*{b}', 40],
    ['{a}/{b}', 2.5],
    ['({a}+{b})/2', 7],
    ['{a}*12', 120],
    ['MAX(0,{b}-{a})', 0],
    ['MAX(0,{a}-{b})', 6],
    ['IF({a}>0,{a}/{b},0)', 2.5],
    ['IF({zero}>0,{a}/{zero},0)', 0],
    ['({a}*12)+{b}+2', 126],
  ])('evaluates %s', (formula, expected) => {
    expect(evaluateFormula(formula, scope)).toBeCloseTo(expected, 10)
  })

  it('respects precedence rather than evaluating left to right', () => {
    expect(evaluateFormula('2+{b}*3', scope)).toBe(14)
    expect(evaluateFormula('(2+{b})*3', scope)).toBe(18)
  })

  it('does not evaluate the branch IF did not take', () => {
    // Excel semantics, and the whole point of the guard in calculations.ts:418. Eager evaluation
    // would divide by zero here and this suite would report a defect that is not there.
    expect(evaluateFormula('IF({zero}>0,{a}/{zero},0)', scope)).toBe(0)
  })

  it.each([
    ['MIN({a},{b})', /does not implement/],
    ['SUM({a})', /does not implement/],
    ['ROUND({a},0)', /does not implement/],
    ['IF({a}>0,{a})', /takes 3/],
    ['{missing}+1', /not among the input fields/],
    ['{text}+1', /rather than a finite number/],
    ['{a}/{zero}', /#DIV\/0!/],
    ['{a} {b}', /Trailing input/],
    ['({a}+{b}', /Expected '\)'/],
    ['{a}+', /Unexpected end/],
    ['{a} & {b}', /Unsupported character/],
    ['{a}>{b}', /evaluates to a comparison/],
    ['IF({a},1,0)', /non-comparison condition/],
  ])('throws on %s', (formula, expected) => {
    expect(() => evaluateFormula(formula, scope)).toThrow(expected)
  })

  it('reports an unknown function by name and lists what it does support', () => {
    expect(() => evaluateFormula('MIN({a},{b})', scope)).toThrow(/MIN\(\)/)
    expect(() => evaluateFormula('MIN({a},{b})', scope)).toThrow(/Implemented: IF, MAX/)
  })
})

describe('the value-expression resolver refuses to guess', () => {
  const params = { ...BASE_PARAMS }

  it('resolves a bare params reference', () => {
    expect(resolveValueExpression(parseValueExpression('params.annualIncome', 'test'), params, 'test')).toBe(72_000)
  })

  it('resolves a module constant', () => {
    expect(resolveValueExpression(parseValueExpression('MEDICARE_AGE', 'test'), params, 'test')).toBe(65)
  })

  it('resolves both branches of a ternary and reports both literals', () => {
    const expression = parseValueExpression("params.savingsFrequency === 'monthly' ? 'A' : 'B'", 'test')
    expect(resolveValueExpression(expression, { ...params, savingsFrequency: 'monthly' }, 'test')).toBe('A')
    expect(resolveValueExpression(expression, { ...params, savingsFrequency: 'yearly' }, 'test')).toBe('B')
    expect(reachableStringLiterals(expression)).toEqual(['A', 'B'])
  })

  it.each([
    ['someHelper(params.currentAge)', /cannot resolve/],
    ['params.currentAge + 1', /cannot resolve/],
    ['{ nested: 1 }', /cannot resolve/],
    ["params.savingsFrequency.length > 0 ? 'A' : 'B'", /cannot resolve the ternary condition/],
  ])('throws on %s', (expression, expected) => {
    expect(() => parseValueExpression(expression, 'test')).toThrow(expected)
  })

  it('throws when a page reads a param no scenario defines', () => {
    expect(() =>
      resolveValueExpression(parseValueExpression('params.notAThing', 'test'), params, 'test'),
    ).toThrow(/scenarios do not define/)
  })

  it('throws on a constant ../calculations does not export', () => {
    expect(() => resolveValueExpression(parseValueExpression('NOT_A_CONSTANT', 'test'), params, 'test')).toThrow(
      /not a string or number/,
    )
  })
})
