// ============================================
// Monthly / annual display period
// ============================================

/**
 * The period a currency amount is expressed in.
 *
 * This is a *presentation* concern only. Every recurring amount is stored in one canonical period
 * (annual, except for the healthcare premium which is canonically monthly) and every calculation
 * runs on that canonical value. Showing an annual amount as a monthly one divides by 12 at the
 * display edge and nothing else: it never introduces intra-year compounding and never changes the
 * math. See `docs` in `calculations.ts` for the contribution model.
 */
export type CurrencyPeriod = 'annual' | 'monthly'

export const DEFAULT_CURRENCY_PERIOD: CurrencyPeriod = 'annual'

export const MONTHS_PER_YEAR = 12

export function isCurrencyPeriod(value: unknown): value is CurrencyPeriod {
  return value === 'annual' || value === 'monthly'
}

export function parseCurrencyPeriod(value: string | null | undefined): CurrencyPeriod {
  return isCurrencyPeriod(value) ? value : DEFAULT_CURRENCY_PERIOD
}

/** Convert an amount between periods. Exact for equal periods, so it is safe to call unconditionally. */
export function convertPeriod(value: number, from: CurrencyPeriod, to: CurrencyPeriod): number {
  if (from === to) return value
  return to === 'monthly' ? value / MONTHS_PER_YEAR : value * MONTHS_PER_YEAR
}

/** True when two amounts are indistinguishable once rounded to whole cents. */
export function isSameToCent(a: number, b: number): boolean {
  if (!Number.isFinite(a) || !Number.isFinite(b)) return false
  return Math.round(a * 100) === Math.round(b * 100)
}

/**
 * Resolve what a field edit should store.
 *
 * The displayed figure is rounded for readability, so converting it straight back would drift:
 * $50,000/yr shows as $4,166.67/mo, and $4,166.67 x 12 is $50,000.04. Whenever the amount that was
 * typed is the same (to the cent) as the amount already on screen, the user did not actually change
 * anything, so the stored value is returned untouched and the round trip is exactly lossless.
 * A genuine edit converts normally.
 */
export function resolveEditedAmount(
  typedDisplayAmount: number,
  storedValue: number,
  displayPeriod: CurrencyPeriod,
  storedPeriod: CurrencyPeriod,
): number {
  const currentDisplayAmount = convertPeriod(storedValue, storedPeriod, displayPeriod)
  if (isSameToCent(typedDisplayAmount, currentDisplayAmount)) return storedValue
  return convertPeriod(typedDisplayAmount, displayPeriod, storedPeriod)
}

/**
 * A rendering that carries a minus sign in front of nothing but zeros.
 *
 * Matched against the *formatted* text rather than the input number on purpose. Two different inputs
 * produce it and only one of them is negative zero: `Intl` keeps the sign while
 * `maximumFractionDigits` rounds the magnitude away, so every negative under half a cent — `-0.001`,
 * `-1e-7` — renders `"-0"` too, and those are ordinary negative numbers that `Object.is(value, -0)`
 * does not catch. Testing the output catches both without having to enumerate the inputs. The
 * optional fraction covers a future `minimumFractionDigits`, which would render `"-0.00"`.
 */
const NEGATIVE_ZERO_RENDERING = /^-0(?:\.0+)?$/

/**
 * Format an amount for a currency field. Cents are shown only when the amount has them, so annual
 * figures stay clean while monthly conversions keep the precision needed to edit them accurately.
 *
 * Zero is always rendered unsigned, matching `CurrencyPeriodMath.RoundHalfUp` on the MAUI side, which
 * returns positive zero where the JS `Math.round` this module was written against returns `-0`. Only
 * a rendering that is entirely zeros is rewritten, so a real fraction of a cent keeps its sign:
 * `-0.005` still renders `"-0.01"`.
 */
export function formatPeriodAmount(value: number): string {
  if (!Number.isFinite(value)) return '0'

  const formatted = new Intl.NumberFormat('en-US', { maximumFractionDigits: 2 }).format(value)
  return NEGATIVE_ZERO_RENDERING.test(formatted) ? formatted.slice(1) : formatted
}

/**
 * Group the digits a user is currently typing without discarding them.
 *
 * `formatPeriodAmount` cannot be used mid-edit: it would rewrite "4166.6" to "4,166.6" only after
 * parsing, dropping a trailing "." and any in-progress second decimal, which is what made the old
 * field snap while typing.
 */
export function formatTypedAmount(raw: string): string {
  const dotIndex = raw.indexOf('.')
  const integerPart = dotIndex === -1 ? raw : raw.slice(0, dotIndex)
  // Extra dots are ignored rather than dropped mid-word, matching the numeric parse below.
  const fractionPart = dotIndex === -1 ? '' : raw.slice(dotIndex + 1).replace(/\./g, '')

  const grouped = integerPart === ''
    ? ''
    : new Intl.NumberFormat('en-US').format(Number(integerPart) || 0)

  return dotIndex === -1 ? grouped : `${grouped}.${fractionPart}`
}

export function periodSuffix(period: CurrencyPeriod): string {
  return period === 'monthly' ? '/mo' : '/yr'
}

export function periodQualifier(period: CurrencyPeriod): string {
  return period === 'monthly' ? 'per month' : 'per year'
}
