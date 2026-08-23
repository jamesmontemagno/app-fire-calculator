// ============================================
// Roth Conversion Strategy Calculator
// Mirrors app/MyFireNumber.Core/Calculations/RothConversionCalculator.cs
// ============================================

export const ROTH_CONVERSION_WAITING_PERIOD_YEARS = 5

/** Whole-year approximation of the 59½ penalty threshold used by the annual model. */
export const ROTH_PENALTY_FREE_AGE = 60

export interface RothConversionInputs {
  currentAge: number
  startYear: number
  traditionalBalance: number
  rothBalance: number
  annualConversion: number
  conversionYears: number
  expectedReturn: number
  estimatedTaxRate: number
}

export interface RothConversionProjectionPoint {
  yearNumber: number
  calendarYear: number
  age: number
  startingTraditionalBalance: number
  conversion: number
  estimatedTaxes: number
  endingTraditionalBalance: number
  endingRothBalance: number
  newlyAccessiblePrincipal: number
  cumulativeAccessiblePrincipal: number
}

export interface RothConversionResult {
  totalConverted: number
  totalEstimatedTaxes: number
  firstAccessibleYear: number | null
  endingTraditionalBalance: number
  endingRothBalance: number
  projections: RothConversionProjectionPoint[]
}

function round(value: number): number {
  return Math.round(Math.max(0, value))
}

/** A human-readable reason the inputs cannot be calculated, or null when they can. */
export function validateRothConversionInputs(inputs: RothConversionInputs): string | null {
  if (inputs.currentAge < 0 || inputs.currentAge > 120) return 'Enter a current age from 0 to 120.'
  if (inputs.startYear < 1900 || inputs.startYear > 2200) return 'Enter a first conversion year from 1900 to 2200.'
  if (inputs.traditionalBalance < 0 || inputs.rothBalance < 0) {
    return 'Enter zero or a positive amount for each account balance.'
  }
  if (!(inputs.annualConversion > 0)) return 'Enter an annual conversion amount greater than zero.'
  if (inputs.conversionYears < 1 || inputs.conversionYears > 50) return 'Enter a conversion period from 1 to 50 years.'
  if (inputs.expectedReturn < -1 || inputs.expectedReturn > 1) return 'Enter an expected return from -100% to 100%.'
  if (inputs.estimatedTaxRate < 0 || inputs.estimatedTaxRate > 1) {
    return 'Enter an estimated conversion tax rate from 0% to 100%.'
  }
  return null
}

export function calculateRothConversion(inputs: RothConversionInputs): RothConversionResult {
  const problem = validateRothConversionInputs(inputs)
  if (problem) throw new RangeError(problem)

  let traditionalBalance = inputs.traditionalBalance
  let rothBalance = inputs.rothBalance
  const convertedByYear = new Map<number, number>()
  const accessibleConversionYears = new Set<number>()
  const projections: RothConversionProjectionPoint[] = []
  let totalConverted = 0
  let totalTaxes = 0
  let accessiblePrincipal = 0
  let firstAccessibleYear: number | null = null

  const horizon = inputs.conversionYears + ROTH_CONVERSION_WAITING_PERIOD_YEARS
  for (let index = 0; index < horizon; index += 1) {
    const calendarYear = inputs.startYear + index
    const startingTraditionalBalance = traditionalBalance

    // Both balances grow before the year's conversion is moved across.
    traditionalBalance *= 1 + inputs.expectedReturn
    rothBalance *= 1 + inputs.expectedReturn

    const conversion = index < inputs.conversionYears
      ? Math.min(inputs.annualConversion, traditionalBalance)
      : 0
    traditionalBalance -= conversion
    rothBalance += conversion

    if (conversion > 0) {
      convertedByYear.set(calendarYear, conversion)
      totalConverted += conversion
      totalTaxes += conversion * inputs.estimatedTaxRate
    }

    // Each conversion's principal is reachable without the additional 10% tax once its own
    // five-tax-year clock has run, or once the owner reaches the penalty-free age.
    const age = inputs.currentAge + index
    let newlyAccessible = 0
    for (const [sourceYear, amount] of convertedByYear) {
      if (accessibleConversionYears.has(sourceYear)) continue
      if (calendarYear >= sourceYear + ROTH_CONVERSION_WAITING_PERIOD_YEARS || age >= ROTH_PENALTY_FREE_AGE) {
        accessibleConversionYears.add(sourceYear)
        newlyAccessible += amount
      }
    }
    accessiblePrincipal += newlyAccessible
    if (newlyAccessible > 0 && firstAccessibleYear === null) {
      firstAccessibleYear = calendarYear
    }

    projections.push({
      yearNumber: index + 1,
      calendarYear,
      age,
      startingTraditionalBalance: round(startingTraditionalBalance),
      conversion: round(conversion),
      estimatedTaxes: round(conversion * inputs.estimatedTaxRate),
      endingTraditionalBalance: round(traditionalBalance),
      endingRothBalance: round(rothBalance),
      newlyAccessiblePrincipal: round(newlyAccessible),
      cumulativeAccessiblePrincipal: round(accessiblePrincipal),
    })
  }

  return {
    totalConverted: round(totalConverted),
    totalEstimatedTaxes: round(totalTaxes),
    firstAccessibleYear,
    endingTraditionalBalance: round(traditionalBalance),
    endingRothBalance: round(rothBalance),
    projections,
  }
}
