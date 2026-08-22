import { describe, expect, it } from 'vitest'

import inventory from '../../../../shared/parity/periodic-fields.json'

import {
  DEFAULT_CURRENCY_PERIOD,
  MONTHS_PER_YEAR,
  convertPeriod,
  formatPeriodAmount,
  formatTypedAmount,
  isCurrencyPeriod,
  isSameToCent,
  parseCurrencyPeriod,
  periodQualifier,
  periodSuffix,
  resolveEditedAmount,
  type CurrencyPeriod,
} from '../currencyPeriod'

/**
 * Pins `src/utils/currencyPeriod.ts`, the arithmetic behind the monthly/annual display toggle.
 *
 * <h3>Why this file exists</h3>
 *
 * PR #77 ported this module to `app/MyFireNumber.Core/Presentation/CurrencyPeriodMath.cs`, which is
 * covered by `CurrencyPeriodMathTests` and `PeriodicAmountFieldTests`. Until this file, web had none
 * of its own — the logic was exercised only incidentally through page-level tests. That asymmetry was
 * the actual hazard: if the two implementations disagreed, MAUI would fail its own suite while web
 * stayed green regardless, so the *untested* implementation was the one being treated as canonical.
 * The cases below deliberately follow the MAUI suite's structure so the two can be read side by side.
 *
 * <h3>Read this before adding a round-trip test</h3>
 *
 * A "toggle the period N times and assert the stored value is unchanged" test **passes with
 * `resolveEditedAmount` deleted entirely.** This is not hypothetical; it was demonstrated by sabotage
 * on the MAUI side during #77, and reconfirmed here (see the sabotage table in the PR).
 *
 * The reason is that nothing in a bare toggle ever writes back. `CurrencyInput` renders from the
 * stored value every time, so flipping `displayPeriod` alone is a pure read. Drift only appears when
 * the *rounded figure on screen* is submitted as an edit — which is what happens the moment a user
 * touches a field they are not really changing, and what MAUI's two-way `Entry` binding does
 * automatically. {@link retypeWhatIsOnScreen} is the web analogue of that echo, and
 * {@link retypeUnguarded} computes what the very same text would have stored without the guard, to
 * prove the echo is load-bearing rather than decorative.
 */

// ---------------------------------------------------------------------------
// The edit path, replicated from CurrencyInput
// ---------------------------------------------------------------------------

/**
 * The sanitizer in `CurrencyInput.handleChange`.
 *
 * Kept verbatim because two of its consequences are pinned below as preconditions: it strips the
 * grouping separators `formatPeriodAmount` adds, and it strips `-`, which is the first of the two
 * reasons a negative amount cannot reach the rounding helper.
 */
const sanitize = (typed: string): string => typed.replace(/[^0-9.]/g, '')

/**
 * The tail of `CurrencyInput.handleChange`, after the raw text has been parsed.
 *
 * Split out from {@link editField} so the negative floor can be exercised at all. Routing a negative
 * through `editField` cannot reach this branch, because {@link sanitize} has already removed the
 * minus sign — a fact worth knowing rather than papering over, and pinned below.
 */
function submitTypedAmount(
  typed: number,
  storedValue: number,
  displayPeriod: CurrencyPeriod,
  storedPeriod: CurrencyPeriod,
): number {
  if (Number.isNaN(typed)) return 0

  const typedDisplayAmount = typed < 0 ? 0 : typed
  return resolveEditedAmount(typedDisplayAmount, storedValue, displayPeriod, storedPeriod)
}

/**
 * One edit through `CurrencyInput.handleChange`, minus React.
 *
 * The branches are the component's, in the component's order: sanitize, empty/bare-dot
 * short-circuits to 0, then {@link submitTypedAmount}. Clamping against `min`/`max` happens after
 * this and is the component's concern, not the module's, so it is out of scope here.
 */
function editField(
  typedText: string,
  storedValue: number,
  displayPeriod: CurrencyPeriod,
  storedPeriod: CurrencyPeriod,
): number {
  const raw = sanitize(typedText)
  if (raw === '' || raw === '.') return 0

  return submitTypedAmount(parseFloat(raw), storedValue, displayPeriod, storedPeriod)
}

/** The exact text `CurrencyInput` puts on screen for a stored value in a given display period. */
function onScreenText(
  storedValue: number,
  storedPeriod: CurrencyPeriod,
  displayPeriod: CurrencyPeriod,
): string {
  return formatPeriodAmount(convertPeriod(storedValue, storedPeriod, displayPeriod))
}

/**
 * Re-enter the figure already displayed, changing nothing.
 *
 * This is the web counterpart to MAUI's binding echo, and the only thing that gives the round-trip
 * tests teeth. Dropping it makes them vacuous.
 */
function retypeWhatIsOnScreen(
  storedValue: number,
  storedPeriod: CurrencyPeriod,
  displayPeriod: CurrencyPeriod,
): number {
  return editField(onScreenText(storedValue, storedPeriod, displayPeriod), storedValue, displayPeriod, storedPeriod)
}

/** What that identical text would have stored if the edit converted straight back. */
function retypeUnguarded(
  storedValue: number,
  storedPeriod: CurrencyPeriod,
  displayPeriod: CurrencyPeriod,
): number {
  const typed = parseFloat(sanitize(onScreenText(storedValue, storedPeriod, displayPeriod)))
  return convertPeriod(typed, displayPeriod, storedPeriod)
}

const OTHER: Record<CurrencyPeriod, CurrencyPeriod> = { annual: 'monthly', monthly: 'annual' }

// ---------------------------------------------------------------------------

describe('convertPeriod', () => {
  it.each<[number, CurrencyPeriod, CurrencyPeriod, number]>([
    [50_000, 'annual', 'monthly', 50_000 / 12],
    [600, 'monthly', 'annual', 7_200],
    [1_234.56, 'annual', 'annual', 1_234.56],
    [1_234.56, 'monthly', 'monthly', 1_234.56],
    [0, 'annual', 'monthly', 0],
  ])('converts %d from %s to %s', (value, from, to, expected) => {
    expect(convertPeriod(value, from, to)).toBe(expected)
  })

  it('is bit-for-bit identity between equal periods', () => {
    // Called unconditionally by the display path, so "same period" must not introduce error of its
    // own. The shortcut is what guarantees that: a body that always did the arithmetic — `x / 12 * 12`
    // — is not the identity on a binary float, and would nudge a value every time it was merely
    // displayed.
    //
    // The values are chosen because they FAIL that round trip. An earlier draft of this test used
    // 1/3, which survives `x / 12 * 12` intact and let a sabotaged convertPeriod pass untouched.
    for (const awkward of [0.21, 0.23, 0.45, 0.9, 50_000.01, 50_000.21]) {
      expect(convertPeriod(awkward, 'annual', 'annual')).toBe(awkward)
      expect(convertPeriod(awkward, 'monthly', 'monthly')).toBe(awkward)
    }
  })

  it('the identity values really are ones the arithmetic would damage', () => {
    // Guards the guard. If these ever started surviving `x / 12 * 12`, the test above would still
    // pass while having quietly stopped distinguishing the shortcut from the arithmetic.
    for (const awkward of [0.21, 0.23, 0.45, 0.9, 50_000.01, 50_000.21]) {
      expect((awkward / MONTHS_PER_YEAR) * MONTHS_PER_YEAR).not.toBe(awkward)
    }
  })

  it('scales by twelve months, not by a hardcoded literal', () => {
    // Pins MONTHS_PER_YEAR to the arithmetic rather than trusting the constant's name.
    expect(MONTHS_PER_YEAR).toBe(12)
    expect(convertPeriod(1, 'annual', 'monthly')).toBe(1 / MONTHS_PER_YEAR)
    expect(convertPeriod(1, 'monthly', 'annual')).toBe(MONTHS_PER_YEAR)
  })

  it('is exactly reversible on values with an exact twelfth', () => {
    // Not true of every value — which is the entire reason resolveEditedAmount exists — but it must
    // hold where the arithmetic is exact, or the bug would be in convertPeriod itself.
    for (const annual of [0, 12, 1_200, 48_000, 60_000, 120_000]) {
      expect(convertPeriod(convertPeriod(annual, 'annual', 'monthly'), 'monthly', 'annual')).toBe(annual)
    }
  })
})

describe('period values arriving from outside the app', () => {
  /**
   * The web counterpart to MAUI's `Undefined_period_values_throw_rather_than_defaulting_to_annual`.
   *
   * `CurrencyPeriod` is a string union, so there is no runtime enum to violate and nothing to throw:
   * an unknown value can only enter through a URL query parameter or stored state. Web therefore
   * guards at the boundary and coerces, where MAUI guards at every entry point and throws. Both
   * refuse to let an unrecognised period reach the arithmetic; this pins web's half.
   */
  it.each(['annual', 'monthly'])('accepts %s', value => {
    expect(isCurrencyPeriod(value)).toBe(true)
    expect(parseCurrencyPeriod(value)).toBe(value)
  })

  it.each<[string | null | undefined, string]>([
    ['Annual', 'a capitalised period'],
    ['ANNUAL', 'a shouted period'],
    ['yearly', 'a synonym'],
    ['', 'an empty parameter'],
    ['99', 'a numeric period'],
    [null, 'a missing parameter'],
    [undefined, 'an absent parameter'],
  ])('falls back to the default for %s (%s)', value => {
    expect(isCurrencyPeriod(value)).toBe(false)
    expect(parseCurrencyPeriod(value)).toBe(DEFAULT_CURRENCY_PERIOD)
  })

  it('rejects non-string values that a parsed URL or storage blob could produce', () => {
    for (const value of [0, 1, {}, [], true, NaN]) {
      expect(isCurrencyPeriod(value)).toBe(false)
    }
  })

  it('defaults to annual', () => {
    // Every field except the healthcare premium is stored annually, so an unrecognised period must
    // land on the period that displays those fields untouched.
    expect(DEFAULT_CURRENCY_PERIOD).toBe('annual')
    expect(convertPeriod(50_000, 'annual', DEFAULT_CURRENCY_PERIOD)).toBe(50_000)
  })
})

describe('isSameToCent', () => {
  it.each<[number, number, boolean]>([
    [4_166.67, 50_000 / 12, true],
    [4_166.67, 4_166.67, true],
    [4_166.67, 4_166.68, false],
    [0, 0.004, true],
    [0, 0.006, false],
  ])('%d vs %d is %s', (a, b, expected) => {
    expect(isSameToCent(a, b)).toBe(expected)
  })

  it('is symmetric', () => {
    expect(isSameToCent(50_000 / 12, 4_166.67)).toBe(true)
    expect(isSameToCent(4_166.68, 4_166.67)).toBe(false)
  })

  it.each([NaN, Infinity, -Infinity])('rejects the non-finite amount %s', value => {
    // Not merely "returns false for one argument": a non-finite value is never the same as anything,
    // including itself, because letting NaN === NaN read as "no edit" would freeze the field.
    expect(isSameToCent(value, value)).toBe(false)
    expect(isSameToCent(value, 1)).toBe(false)
    expect(isSameToCent(1, value)).toBe(false)
  })

  /**
   * Midpoint rounding, pinned through the only surface that exposes it.
   *
   * `Math.round` is not exported, so these go through `isSameToCent`, whose `Math.round(x * 100)` is
   * the sole consumer. Each pair is chosen so `x * 100` lands on an exact IEEE-754 midpoint — checked
   * in `the cent midpoints are exact` below — and so that the expected result *disagrees* with the
   * plausible alternatives:
   *
   * ```
   *  cents   JS Math.round   half-to-even   half-away-from-zero
   *    0.5         1               0                1
   *    2.5         3               2                3
   *    4.5         5               4                5
   *   -0.5        -0              -0               -1
   *   -1.5        -1              -2               -2
   * ```
   *
   * The positive rows matter most: half-to-even disagrees at every even boundary, and half a cent on
   * a positive amount is exactly where currency lives. 3.5 is deliberately absent — `0.035 * 100` is
   * `3.5000000000000004`, so it would pin float representation rather than a rounding rule.
   */
  it.each<[number, number, boolean, string]>([
    [0.005, 0.01, true, '0.5 cents rounds up to 1, matching 0.01'],
    [0.005, 0, false, '0.5 cents does not round down to 0'],
    [0.025, 0.03, true, '2.5 cents rounds up to 3 (half-to-even would say 2)'],
    [0.025, 0.02, false, '2.5 cents does not round to the even 2'],
    [0.045, 0.05, true, '4.5 cents rounds up to 5 (half-to-even would say 4)'],
    [0.045, 0.04, false, '4.5 cents does not round to the even 4'],
    [-0.005, 0, true, '-0.5 cents rounds toward +0, not away from zero to -1'],
    [-0.005, -0.01, false, '-0.5 cents does not round away from zero'],
    [-0.015, -0.01, true, '-1.5 cents rounds to -1, not away from zero to -2'],
    [-0.015, -0.02, false, '-1.5 cents does not round away from zero'],
  ])('%d vs %d is %s — %s', (a, b, expected) => {
    expect(isSameToCent(a, b)).toBe(expected)
  })

  it('the cent midpoints are exact, so the table above pins rounding and not float error', () => {
    // Guards the guard. If any of these stopped being exact the rows would still pass while having
    // quietly stopped testing a midpoint at all — which is how 3.5 was disqualified.
    expect(0.005 * 100).toBe(0.5)
    expect(0.025 * 100).toBe(2.5)
    expect(0.045 * 100).toBe(4.5)
    expect(-0.005 * 100).toBe(-0.5)
    expect(-0.015 * 100).toBe(-1.5)
  })

  /**
   * The `-0.5` row above cannot see the sign of its own result, so the sign is pinned here.
   *
   * `Math.round(-0.5)` is `-0`, and `-0 === 0` is `true`, so an equality assertion is blind to it.
   * Inside `isSameToCent` that blindness is harmless and in fact correct — `-0` and `+0` cents are
   * the same amount of money — but it means the row proves nothing about the sign, so it is stated
   * explicitly rather than assumed. `Object.is` is what distinguishes the two zeros.
   */
  it('treats negative zero cents and positive zero cents as the same amount', () => {
    expect(Object.is(Math.round(-0.005 * 100), -0)).toBe(true)
    expect(Object.is(Math.round(0 * 100), 0)).toBe(true)

    // Different signs of zero, same money. This is the behaviour that makes the -0.5 row read `true`.
    expect(isSameToCent(-0.004, 0)).toBe(true)
    expect(isSameToCent(-0, 0)).toBe(true)
  })
})

describe('resolveEditedAmount', () => {
  /**
   * The heart of the module. Re-entering the figure already on screen stores nothing new, so the
   * rounding done for readability never leaks back into the stored value.
   */
  it('treats the displayed figure as no edit at all', () => {
    // 50000 / 12 is 4166.666..., displayed as 4166.67. Converting that back gives 50000.04.
    const resolved = resolveEditedAmount(4_166.67, 50_000, 'monthly', 'annual')

    expect(resolved).toBe(50_000)

    // The drift the guard prevents, stated independently of the guard.
    expect(4_166.67 * 12).not.toBe(50_000)
    expect(4_166.67 * 12).toBeCloseTo(50_000.04, 6)
  })

  it('returns the stored value bit-for-bit, not a value that merely rounds back to it', () => {
    // `toBe` is Object.is in vitest, so this also rules out a -0 sneaking through.
    const stored = 12_345.678_9
    const displayed = convertPeriod(stored, 'annual', 'monthly')

    expect(Object.is(resolveEditedAmount(displayed, stored, 'monthly', 'annual'), stored)).toBe(true)
  })

  it('converts a genuine edit', () => {
    expect(resolveEditedAmount(5_000, 50_000, 'monthly', 'annual')).toBe(60_000)
  })

  it('converts a genuine edit on a monthly-stored field', () => {
    // Healthcare premium: stored monthly, edited while displayed annually.
    expect(resolveEditedAmount(9_600, 600, 'annual', 'monthly')).toBe(800)
  })

  it('passes an edit through untouched when display and stored periods match', () => {
    expect(resolveEditedAmount(51_000, 50_000, 'annual', 'annual')).toBe(51_000)
    expect(resolveEditedAmount(700, 600, 'monthly', 'monthly')).toBe(700)
  })

  it('distinguishes a real edit that is only one cent away from the display', () => {
    // The guard must be an equality check to the cent, not a tolerance band. One cent up from the
    // displayed 4166.67 is a genuine edit and has to convert.
    const resolved = resolveEditedAmount(4_166.68, 50_000, 'monthly', 'annual')

    expect(resolved).not.toBe(50_000)
    expect(resolved).toBeCloseTo(50_000.16, 6)
  })

  it('does not swallow an edit to zero', () => {
    // Clearing the field is a real edit, and 0 is never "the same as" a non-zero display.
    expect(resolveEditedAmount(0, 50_000, 'monthly', 'annual')).toBe(0)
    expect(resolveEditedAmount(0, 50_000, 'annual', 'annual')).toBe(0)
  })

  it('holds a stored value whose monthly display rounds all the way to zero', () => {
    // 0.01/yr shows as "0" per month. The guard is what stops the display's own rounding from
    // erasing the value; without it, merely touching the field would store 0.
    expect(onScreenText(0.01, 'annual', 'monthly')).toBe('0')
    expect(retypeWhatIsOnScreen(0.01, 'annual', 'monthly')).toBe(0.01)
    expect(retypeUnguarded(0.01, 'annual', 'monthly')).toBe(0)
  })

  it('propagates a non-finite typed amount rather than inventing a value', () => {
    // isSameToCent rejects non-finite input, so this falls through to a straight conversion. The
    // component never lets it happen — pinned in `the component floors...` below — but the module's
    // own behaviour is stated rather than left to chance.
    expect(resolveEditedAmount(NaN, 50_000, 'monthly', 'annual')).toBeNaN()
    expect(resolveEditedAmount(1, NaN, 'monthly', 'annual')).toBe(12)
  })
})

describe('the lossless round trip through the field', () => {
  /**
   * The guarantee users actually feel: switching the view and touching a field never changes what it
   * holds. Each iteration re-enters the figure on screen, which is what a real edit session does and
   * what MAUI's two-way binding does on its own.
   */
  it.each([50_000, 48_000, 24_000, 72_000, 1, 0, 0.01, 100, 12_345.67, 1_000_000, 33_333.33])(
    'never moves the annual stored value %d across fifty toggles',
    stored => {
      let value = stored

      for (let i = 0; i < 50; i += 1) {
        value = retypeWhatIsOnScreen(value, 'annual', 'monthly')
        expect(Object.is(value, stored)).toBe(true)

        value = retypeWhatIsOnScreen(value, 'annual', 'annual')
        expect(Object.is(value, stored)).toBe(true)
      }
    },
  )

  it.each([600, 1_234.5678, 0, 2_999.99])(
    'never moves the monthly stored value %d across fifty toggles',
    stored => {
      let value = stored

      for (let i = 0; i < 50; i += 1) {
        value = retypeWhatIsOnScreen(value, 'monthly', 'annual')
        expect(Object.is(value, stored)).toBe(true)

        value = retypeWhatIsOnScreen(value, 'monthly', 'monthly')
        expect(Object.is(value, stored)).toBe(true)
      }
    },
  )

  /**
   * Proves the re-entry above is load-bearing, by computing what a field without the guard would
   * have stored from the very same text. If these two numbers were equal, every round-trip test in
   * this block would pass no matter what the production code did.
   */
  it('the re-entry is what gives the round trip its teeth', () => {
    expect(onScreenText(50_000, 'annual', 'monthly')).toBe('4,166.67')

    const unguarded = retypeUnguarded(50_000, 'annual', 'monthly')

    expect(unguarded).toBeCloseTo(50_000.04, 6)
    expect(Object.is(unguarded, 50_000)).toBe(false)

    // What the guarded path stores from that identical text.
    expect(Object.is(retypeWhatIsOnScreen(50_000, 'annual', 'monthly'), 50_000)).toBe(true)
  })

  it('drifts without the guard on more than one value, and drifts further each toggle', () => {
    // A single drifting example could be a coincidence of one number. It is not, and the error
    // compounds: an unguarded field walks away from the value the longer it is used.
    let unguarded = 50_000
    for (let i = 0; i < 5; i += 1) unguarded = retypeUnguarded(unguarded, 'annual', 'monthly')
    expect(unguarded).toBeGreaterThan(50_000)

    expect(retypeUnguarded(1, 'annual', 'monthly')).toBeCloseTo(0.96, 10)
    expect(retypeUnguarded(1_234.5678, 'monthly', 'annual')).not.toBe(1_234.5678)
  })

  it('survives an edit followed by a round trip', () => {
    // Editing 5000/mo onto a 50000/yr field stores 60000, and the toggle must show exactly that.
    const stored = editField('5000', 50_000, 'monthly', 'annual')
    expect(stored).toBe(60_000)

    expect(onScreenText(stored, 'annual', 'annual')).toBe('60,000')
    expect(onScreenText(stored, 'annual', 'monthly')).toBe('5,000')
    expect(Object.is(retypeWhatIsOnScreen(stored, 'annual', 'monthly'), 60_000)).toBe(true)
  })

  it('reads the grouped text the field actually renders', () => {
    // The round trip runs on "4,166.67", not on "4166.67". If the sanitizer stopped stripping the
    // separator, parseFloat("4,166.67") would be 4 and the field would store 48 cents a year.
    expect(onScreenText(50_000, 'annual', 'monthly')).toContain(',')
    expect(parseFloat('4,166.67')).toBe(4)
    expect(sanitize('4,166.67')).toBe('4166.67')
  })
})

describe('the preconditions that keep negatives away from the rounding', () => {
  /**
   * `isSameToCent`'s negative rows are defensive only. Two independent guards stand between a user
   * and a negative amount, and both are pinned so that removing one is visible here rather than
   * quietly making those rows live.
   */
  it('the component strips the minus sign before anything parses it', () => {
    expect(sanitize('-1')).toBe('1')
    expect(sanitize('-0.5')).toBe('0.5')
    expect(sanitize('-50000')).toBe('50000')
  })

  it('the component floors a negative typed amount to zero while min >= 0', () => {
    // The second guard, and it is genuinely independent: handed a negative directly — bypassing the
    // sanitizer, which is the only way to reach this branch — the edit path stores 0 rather than
    // converting it into a negative annual amount.
    expect(submitTypedAmount(-1, 50_000, 'annual', 'annual')).toBe(0)
    expect(submitTypedAmount(-0.5, 50_000, 'monthly', 'annual')).toBe(0)
    expect(submitTypedAmount(-50_000, 50_000, 'annual', 'annual')).toBe(0)
  })

  it('the sanitizer is why the floor above never fires in the running app', () => {
    // Typing "-1" does not store 0, it stores 1: the minus is gone before anything sees a sign.
    // Stating this keeps the two guards from being mistaken for one guard tested twice.
    expect(editField('-1', 50_000, 'annual', 'annual')).toBe(1)
    expect(editField('-50000', 50_000, 'annual', 'annual')).toBe(50_000)
  })

  it('unusable text resolves to zero rather than to NaN', () => {
    // parseFloat("") is NaN and parseFloat(".") is NaN; both would poison the stored value if they
    // reached resolveEditedAmount, which is why the component short-circuits first.
    for (const typed of ['', '.', 'abc', '   ']) {
      expect(editField(typed, 50_000, 'monthly', 'annual')).toBe(0)
    }
  })

  /**
   * Issue #87, now fixed: `formatPeriodAmount` used to render `"-0"` here, where
   * `CurrencyPeriodMath.RoundHalfUp` on the MAUI side returns positive zero. The formatter is
   * asserted properly under `formatPeriodAmount` below; what this test still owns are the three
   * preconditions that keep a negative away from it in the first place, which the fix does not
   * replace. They are pinned so removing a guard shows up here rather than quietly making the
   * negative rows live.
   */
  it('cannot be handed a negative zero in the first place', () => {
    // `Object.is`, not `===`: `-0 === 0` is true, so an equality assertion would pass on either zero
    // and prove nothing about the sign these guards exist to keep out.
    expect(Object.is(convertPeriod(0, 'annual', 'monthly'), 0)).toBe(true)
    expect(Object.is(editField('-0', 50_000, 'annual', 'annual'), 0)).toBe(true)
    expect(formatPeriodAmount(convertPeriod(0, 'annual', 'monthly'))).toBe('0')
  })
})

describe('per-field stored periods', () => {
  interface SharedField {
    key: string
    storedPeriod: string
  }

  interface SharedCalculator {
    id: string
    webPage: string | null
    fields: SharedField[]
  }

  const FIELDS = (inventory.calculators as SharedCalculator[]).flatMap(calculator =>
    calculator.fields.map(field => ({ ...field, calculator: calculator.id })),
  )

  it('is not silently empty', () => {
    // Guards the guard: if the artifact failed to load, the loop below would be an empty loop
    // reporting success over nothing.
    expect(FIELDS.length).toBeGreaterThan(0)
    expect(FIELDS.filter(field => field.storedPeriod === 'monthly')).not.toHaveLength(0)
  })

  it('the healthcare premium is the only monthly-canonical field', () => {
    // The distinction that makes storedPeriod worth carrying per field. If it were dropped, the
    // premium would be the field that breaks, and it would break by 144x.
    expect(FIELDS.filter(field => field.storedPeriod === 'monthly').map(field => field.key)).toEqual([
      'healthcareMonthlyPremium',
    ])
  })

  it('shows a $600/mo premium as $7,200/yr, not as $50/mo', () => {
    // A mechanism that assumed every stored value was annual would read $600 as $50 a month. That is
    // silent, plausible-looking, and wrong by a factor of 144 over the round trip.
    expect(onScreenText(600, 'monthly', 'annual')).toBe('7,200')
    expect(onScreenText(600, 'monthly', 'monthly')).toBe('600')

    const asIfAnnual = onScreenText(600, 'annual', 'monthly')
    expect(asIfAnnual).toBe('50')
    expect(convertPeriod(600, 'monthly', 'annual') / convertPeriod(600, 'annual', 'monthly')).toBe(144)
  })

  it('editing the premium while displayed annually stores the monthly amount', () => {
    expect(editField('9,600', 600, 'annual', 'monthly')).toBe(800)
  })

  it.each(FIELDS.map(field => [`${field.calculator}.${field.key}`, field.storedPeriod] as const))(
    '%s round-trips losslessly in its declared %s period',
    (_name, storedPeriod) => {
      const period = storedPeriod as CurrencyPeriod
      expect(period === 'annual' || period === 'monthly').toBe(true)

      for (const stored of [0, 1, 600, 4_166.67, 50_000, 83_333.33]) {
        expect(Object.is(retypeWhatIsOnScreen(stored, period, OTHER[period]), stored)).toBe(true)
        expect(Object.is(retypeWhatIsOnScreen(stored, period, period), stored)).toBe(true)
      }
    },
  )
})

describe('formatPeriodAmount', () => {
  it.each<[number, string]>([
    [50_000, '50,000'],
    [50_000 / 12, '4,166.67'],
    [0, '0'],
    [1_234.5, '1,234.5'],
    [1_000_000, '1,000,000'],
    [0.01, '0.01'],
  ])('formats %d as %s', (value, expected) => {
    expect(formatPeriodAmount(value)).toBe(expected)
  })

  it('shows cents only when there are cents', () => {
    // Annual figures stay clean while monthly conversions keep the two decimals needed to edit them.
    expect(formatPeriodAmount(50_000)).not.toContain('.')
    expect(formatPeriodAmount(50_000 / 12)).toBe('4,166.67')
  })

  it('never shows more than two decimals', () => {
    // A third decimal would be unreachable to type back, so the round-trip guard would stop matching.
    expect(formatPeriodAmount(1 / 3)).toBe('0.33')
    expect(formatPeriodAmount(4_166.666_666_7)).toBe('4,166.67')
  })

  it.each([NaN, Infinity, -Infinity])('renders the non-finite value %s as 0', value => {
    // "NaN" or "∞" in a currency field would be unrecoverable without clearing it by hand.
    expect(formatPeriodAmount(value)).toBe('0')
  })

  it('groups thousands, unlike the MAUI port', () => {
    // A deliberate difference, not a divergence: web renders into a text input it re-sanitizes on
    // every keystroke, while the MAUI Entry keeps its text raw. Recorded so the two suites can be
    // compared without this looking like a bug in either.
    expect(formatPeriodAmount(50_000)).toBe('50,000')
    expect(sanitize(formatPeriodAmount(50_000))).toBe('50000')
  })

  /**
   * Issue #87.
   *
   * `Intl.NumberFormat` keeps the sign of a value whose magnitude `maximumFractionDigits: 2` rounds
   * away, so every one of these rendered `"-0"` before the normalisation.
   *
   * The rows are deliberately three *different* numbers. Only the first is negative zero; `-0.001`
   * and `-1e-7` are ordinary negative numbers that merely round to nothing. A fix written as
   * `Object.is(value, -0)` would have handled the rare input and left the likelier two broken, which
   * is why the normalisation is applied to the rendered text instead of to the input.
   */
  it.each<[string, number]>([
    ['negative zero', -0],
    ['a negative just under half a cent', -0.001],
    ['a negative far under half a cent', -1e-7],
  ])('renders %s as an unsigned zero', (_name, value) => {
    expect(formatPeriodAmount(value)).toBe('0')
    expect(formatPeriodAmount(value)).not.toContain('-')
  })

  it('the three rows above are genuinely different inputs', () => {
    // Guards the guard. `-0 === 0` is true, so an equality assertion is blind to the exact thing
    // being fixed — that blind spot is why the bug survived. `Object.is` is what can see it, and it
    // is also what shows the last two rows are not negative zero at all.
    expect(Object.is(-0, 0)).toBe(false)
    expect(Object.is(-0, -0)).toBe(true)
    expect(Object.is(-0.001, -0)).toBe(false)
    expect(Object.is(-1e-7, -0)).toBe(false)
    expect([-0.001, -1e-7].every(value => value < 0)).toBe(true)
  })

  it('keeps the sign of a negative big enough to survive the rounding', () => {
    // The normalisation rewrites a rendering that is nothing but zeros, so it cannot swallow a real
    // amount. Half a cent is the first magnitude that still shows, and it still shows as negative.
    expect(formatPeriodAmount(-0.005)).toBe('-0.01')
    expect(formatPeriodAmount(-0.01)).toBe('-0.01')
    expect(formatPeriodAmount(-1_234.5)).toBe('-1,234.5')
    expect(formatPeriodAmount(-50_000)).toBe('-50,000')
  })

  it('leaves positive amounts exactly as they were', () => {
    // The fix is scoped to the sign. Nothing above zero changes shape, including the boundary values
    // the normalisation inspects.
    expect(formatPeriodAmount(0)).toBe('0')
    expect(formatPeriodAmount(0.001)).toBe('0')
    expect(formatPeriodAmount(0.005)).toBe('0.01')
    expect(formatPeriodAmount(50_000 / 12)).toBe('4,166.67')
  })
})

describe('formatTypedAmount', () => {
  /**
   * No MAUI counterpart: the `Entry` keeps typed text verbatim, so only web needs to group digits
   * mid-edit. `formatPeriodAmount` cannot do this job — it parses first, which drops a trailing "."
   * and any in-progress second decimal, and that is what made the old field snap while typing.
   */
  it.each<[string, string]>([
    ['50000', '50,000'],
    ['12345678', '12,345,678'],
    ['4166.6', '4,166.6'],
    ['1234.', '1,234.'],
    ['1234.50', '1,234.50'],
    ['', ''],
    ['.', '.'],
    ['.5', '.5'],
    ['0', '0'],
  ])('groups %s as %s', (raw, expected) => {
    expect(formatTypedAmount(raw)).toBe(expected)
  })

  it('keeps a trailing separator so the next keystroke is a decimal', () => {
    // The specific regression: parsing "1234." yields 1234, which renders "1,234" and moves the
    // caret past a separator the user is still typing.
    expect(formatTypedAmount('1234.')).toBe('1,234.')
    expect(formatPeriodAmount(parseFloat('1234.'))).toBe('1,234')
  })

  it('keeps a trailing zero in the cents', () => {
    // "1234.50" must not collapse to "1,234.5" while the user is still mid-cent.
    expect(formatTypedAmount('1234.50')).toBe('1,234.50')
    expect(formatTypedAmount('1234.00')).toBe('1,234.00')
  })

  it('ignores extra separators instead of dropping the digits after them', () => {
    // Matches the numeric parse: the digits survive so the field does not eat a keystroke.
    expect(formatTypedAmount('1.2.3')).toBe('1.23')
    expect(formatTypedAmount('1..2')).toBe('1.2')
  })

  it('falls back to zero for an unparseable integer part', () => {
    // Unreachable through the component, which strips non-digits first, but pinned because the
    // `|| 0` is the difference between "0" and "NaN" appearing in the field.
    expect(formatTypedAmount('abc')).toBe('0')
  })

  it('round-trips through the sanitizer for every value it produces', () => {
    // Whatever this renders must be readable back, or the next keystroke would be parsed against a
    // different number than the one on screen.
    for (const raw of ['50000', '4166.6', '1234.', '0', '1234.50']) {
      expect(sanitize(formatTypedAmount(raw))).toBe(raw)
    }
  })
})

describe('periodSuffix and periodQualifier', () => {
  it('names the period', () => {
    expect(periodSuffix('annual')).toBe('/yr')
    expect(periodSuffix('monthly')).toBe('/mo')
    expect(periodQualifier('annual')).toBe('per year')
    expect(periodQualifier('monthly')).toBe('per month')
  })

  it('never labels one period with the other', () => {
    // The label is the only thing telling a user which period a bare number is in, so a swap would
    // be a silent 12x misread rather than a cosmetic bug.
    expect(periodSuffix('annual')).not.toBe(periodSuffix('monthly'))
    expect(periodQualifier('annual')).not.toBe(periodQualifier('monthly'))
  })
})
