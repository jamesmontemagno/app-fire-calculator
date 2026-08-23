// ============================================
// 72(t) / SEPP Calculator
// Mirrors app/MyFireNumber.Core/Calculations/SeppCalculator.cs
// ============================================

export type SeppMethod = 'rmd' | 'amortization' | 'annuitization'

export const SEPP_METHODS: SeppMethod[] = ['rmd', 'amortization', 'annuitization']

export function isSeppMethod(value: string): value is SeppMethod {
  return (SEPP_METHODS as string[]).includes(value)
}

export const SEPP_METHOD_LABELS: Record<SeppMethod, string> = {
  rmd: 'Required minimum distribution (changes yearly)',
  amortization: 'Fixed amortization',
  annuitization: 'Fixed annuitization',
}

export interface SeppInputs {
  accountBalance: number
  /** Used only for the illustrative balance projection, not the IRS payment formula. */
  expectedReturn: number
  /** ISO date (YYYY-MM-DD). */
  birthDate: string
  /** ISO date (YYYY-MM-DD). */
  firstPaymentDate: string
  interestRate: number
  maximumInterestRate: number
  /** Actuarial factor supplied by a qualified professional; null when not available. */
  annuityFactor: number | null
  method: SeppMethod
}

export interface SeppProjectionPoint {
  yearNumber: number
  calendarYear: number
  age: number
  startingBalance: number
  annualPayment: number
  endingBalance: number
}

export interface SeppMethodResult {
  method: SeppMethod
  annualPayment: number | null
  monthlyPayment: number | null
  projections: SeppProjectionPoint[]
}

export interface SeppResult {
  startingAge: number
  lifeExpectancyFactor: number
  /** ISO date (YYYY-MM-DD). */
  requiredEndDate: string
  requiredYears: number
  maximumInterestRate: number
  rmd: SeppMethodResult
  amortization: SeppMethodResult
  annuitization: SeppMethodResult
}

export const SEPP_MIN_AGE = 18
export const SEPP_MAX_AGE = 59

/** Treas. Reg. §1.401(a)(9)-9, Table I (Single Life), effective for 2022 and later. */
const SINGLE_LIFE_FACTORS: Record<number, number> = {
  18: 67.0, 19: 66.0, 20: 65.0, 21: 64.1, 22: 63.1,
  23: 62.1, 24: 61.1, 25: 60.2, 26: 59.2, 27: 58.2,
  28: 57.3, 29: 56.3, 30: 55.3, 31: 54.4, 32: 53.4,
  33: 52.5, 34: 51.5, 35: 50.5, 36: 49.6, 37: 48.6,
  38: 47.7, 39: 46.7, 40: 45.7, 41: 44.8, 42: 43.8,
  43: 42.9, 44: 41.9, 45: 41.0, 46: 40.0, 47: 39.0,
  48: 38.1, 49: 37.1, 50: 36.2, 51: 35.3, 52: 34.3,
  53: 33.4, 54: 32.5, 55: 31.6, 56: 30.6, 57: 29.8,
  58: 28.9, 59: 28.0, 60: 27.1, 61: 26.2, 62: 25.4,
  63: 24.5, 64: 23.7, 65: 22.9, 66: 22.0, 67: 21.2,
  68: 20.4, 69: 19.6, 70: 18.8,
}

/** The greater of 5% or 120% of the applicable federal mid-term rate (Notice 2022-6). */
export function seppMaximumPermittedInterestRate(federalMidTermRate: number): number {
  return Math.max(0.05, federalMidTermRate * 1.2)
}

export function seppSingleLifeFactor(age: number): number {
  const factor = SINGLE_LIFE_FACTORS[age]
  if (factor === undefined) {
    throw new RangeError('Age is outside the retained IRS Single Life table range.')
  }
  return factor
}

export interface IsoDate {
  year: number
  month: number
  day: number
}

const ISO_DATE = /^(\d{4})-(\d{2})-(\d{2})$/

export function parseIsoDate(value: string): IsoDate | null {
  const match = ISO_DATE.exec(value)
  if (!match) return null
  const year = Number(match[1])
  const month = Number(match[2])
  const day = Number(match[3])
  if (month < 1 || month > 12 || day < 1) return null
  if (day > new Date(Date.UTC(year, month, 0)).getUTCDate()) return null
  return { year, month, day }
}

export function formatIsoDate(date: IsoDate): string {
  const pad = (value: number) => String(value).padStart(2, '0')
  return `${date.year}-${pad(date.month)}-${pad(date.day)}`
}

export function todayIsoDate(): string {
  const now = new Date()
  return formatIsoDate({ year: now.getFullYear(), month: now.getMonth() + 1, day: now.getDate() })
}

function toUtc(date: IsoDate): Date {
  return new Date(Date.UTC(date.year, date.month - 1, date.day))
}

function dayNumber(date: IsoDate): number {
  return Math.round(toUtc(date).getTime() / 86_400_000)
}

function compare(a: IsoDate, b: IsoDate): number {
  return dayNumber(a) - dayNumber(b)
}

/** Mirrors DateOnly.AddYears/AddMonths: clamps the day to the end of the target month. */
export function addMonths(date: IsoDate, months: number): IsoDate {
  const total = date.year * 12 + (date.month - 1) + months
  const year = Math.floor(total / 12)
  const month = (total % 12) + 1
  const lastDay = new Date(Date.UTC(year, month, 0)).getUTCDate()
  return { year, month, day: Math.min(date.day, lastDay) }
}

export function addYears(date: IsoDate, years: number): IsoDate {
  return addMonths(date, years * 12)
}

export function ageOn(birthDate: IsoDate, date: IsoDate): number {
  if (compare(date, birthDate) < 0) {
    throw new RangeError('First payment date must be after birth date.')
  }
  const isLeap = (year: number) => (year % 4 === 0 && year % 100 !== 0) || year % 400 === 0
  const birthday: IsoDate = birthDate.month === 2 && birthDate.day === 29 && !isLeap(date.year)
    ? { year: date.year, month: 2, day: 28 }
    : { year: date.year, month: birthDate.month, day: birthDate.day }
  return date.year - birthDate.year - (compare(date, birthday) < 0 ? 1 : 0)
}

/** Ordinary-annuity payment over `years` (possibly fractional) at `rate`. */
function amortizedPayment(presentValue: number, rate: number, years: number): number {
  return rate === 0
    ? presentValue / years
    : (rate * presentValue) / (1 - Math.pow(1 + rate, -years))
}

export function age59AndAHalf(birthDate: IsoDate): IsoDate {
  return addMonths(addYears(birthDate, 59), 6)
}

/** A human-readable reason the inputs cannot be calculated, or null when they can. */
export function validateSeppInputs(inputs: SeppInputs): string | null {
  if (!(inputs.accountBalance > 0)) return 'Enter an account balance greater than zero.'
  if (inputs.expectedReturn < -1 || inputs.expectedReturn > 1) return 'Enter an expected return from -100% to 100%.'
  if (inputs.interestRate < 0 || inputs.maximumInterestRate < 0) return 'Interest rates cannot be negative.'
  if (inputs.interestRate > inputs.maximumInterestRate) {
    return 'The chosen interest rate cannot exceed the IRS limit you entered.'
  }
  if (inputs.annuityFactor !== null && !(inputs.annuityFactor > 0)) {
    return 'The actuarial annuity factor must be greater than zero.'
  }
  if (inputs.method === 'annuitization' && inputs.annuityFactor === null) {
    return 'Enter an actuarial annuity factor supplied by a qualified professional for fixed annuitization.'
  }

  const birth = parseIsoDate(inputs.birthDate)
  const firstPayment = parseIsoDate(inputs.firstPaymentDate)
  if (!birth) return 'Enter a valid birth date.'
  if (!firstPayment) return 'Enter a valid first payment date.'
  if (compare(firstPayment, birth) < 0) return 'The first payment date must be after the birth date.'
  if (compare(firstPayment, age59AndAHalf(birth)) >= 0) return 'The first payment must occur before age 59½.'

  const age = ageOn(birth, firstPayment)
  if (age < SEPP_MIN_AGE || age > SEPP_MAX_AGE) {
    return `The age on the first payment date must be from ${SEPP_MIN_AGE} through ${SEPP_MAX_AGE}.`
  }
  return null
}

export function calculateSepp(inputs: SeppInputs): SeppResult {
  const problem = validateSeppInputs(inputs)
  if (problem) throw new RangeError(problem)

  const birth = parseIsoDate(inputs.birthDate)!
  const firstPayment = parseIsoDate(inputs.firstPaymentDate)!
  const startingAge = ageOn(birth, firstPayment)
  const lifeExpectancyFactor = seppSingleLifeFactor(startingAge)

  // Payments must continue until the later of five years after the first payment or age 59½.
  const halfYear = age59AndAHalf(birth)
  const fiveYears = addYears(firstPayment, 5)
  const requiredEndDate = compare(halfYear, fiveYears) > 0 ? halfYear : fiveYears
  const requiredYears = Math.max(
    1,
    Math.ceil((dayNumber(requiredEndDate) - dayNumber(firstPayment)) / 365.2425),
  )

  const build = (
    method: SeppMethod,
    paymentForYear: (balance: number, age: number) => number,
  ): SeppMethodResult => {
    let balance = inputs.accountBalance
    const projections: SeppProjectionPoint[] = []
    for (let year = 0; year < requiredYears; year += 1) {
      const paymentDate = addYears(firstPayment, year)
      const age = ageOn(birth, paymentDate)
      const payment = Math.min(balance, paymentForYear(balance, age))
      const endingBalance = Math.max(0, balance * (1 + inputs.expectedReturn) - payment)
      projections.push({
        yearNumber: year + 1,
        calendarYear: paymentDate.year,
        age,
        startingBalance: Math.round(balance),
        annualPayment: Math.round(payment),
        endingBalance: Math.round(endingBalance),
      })
      balance = endingBalance
    }
    const annualPayment = projections.length === 0 ? 0 : projections[0].annualPayment
    return { method, annualPayment, monthlyPayment: annualPayment / 12, projections }
  }

  const rmd = build('rmd', (balance, age) => balance / seppSingleLifeFactor(age))
  const amortizationPayment = amortizedPayment(inputs.accountBalance, inputs.interestRate, lifeExpectancyFactor)
  const amortization = build('amortization', () => amortizationPayment)
  const annuitization: SeppMethodResult = inputs.annuityFactor !== null && inputs.annuityFactor > 0
    ? build('annuitization', () => inputs.accountBalance / inputs.annuityFactor!)
    : { method: 'annuitization', annualPayment: null, monthlyPayment: null, projections: [] }

  return {
    startingAge,
    lifeExpectancyFactor,
    requiredEndDate: formatIsoDate(requiredEndDate),
    requiredYears,
    maximumInterestRate: inputs.maximumInterestRate,
    rmd,
    amortization,
    annuitization,
  }
}

export function seppResultFor(result: SeppResult, method: SeppMethod): SeppMethodResult {
  switch (method) {
    case 'rmd':
      return result.rmd
    case 'amortization':
      return result.amortization
    case 'annuitization':
      return result.annuitization
  }
}
