// ============================================
// FIRE Calculator - Core Calculation Functions
// ============================================

/**
 * How contributions behave over time.
 *
 * - `inflation`: the contribution keeps a constant purchasing power, so the nominal amount paid at the
 *   end of year k is `annualContribution * (1 + inflationRate)^k`. This is the model the closed-form
 *   solver assumes, and it is the app default.
 * - `flat`: the same nominal amount is contributed every year, so its purchasing power erodes.
 */
export type ContributionGrowth = 'inflation' | 'flat'

export const DEFAULT_CONTRIBUTION_GROWTH: ContributionGrowth = 'inflation'

export interface FIREInputs {
  currentAge: number
  retirementAge?: number
  currentSavings: number
  annualContribution: number
  annualIncome: number // net annual income for savings rate calculation
  expectedReturn: number // as decimal, e.g., 0.07 for 7%
  inflationRate: number // as decimal
  withdrawalRate: number // as decimal, e.g., 0.04 for 4%
  annualExpenses: number
  contributionGrowth?: ContributionGrowth
}

export interface ProjectionPoint {
  age: number
  year: number
  portfolio: number
  contributions: number
  totalContributions: number
  inflationAdjusted: number
}

export interface StandardFIREResult {
  fireNumber: number
  yearsToFIRE: number
  fireAge: number
  projections: ProjectionPoint[]
  savingsRate: number
  monthlyContribution: number
  coastFireNumber: number
  retirementGoal: RetirementGoalAssessment
}

export interface RetirementGoalAssessment {
  targetRetirementAge: number
  calculatedFireAge: number
  targetAgeGap: number
  isOnTrack: boolean
  message: string
}

export interface CoastFIREResult {
  coastNumber: number
  yearsToCoast: number
  alreadyCoasting: boolean
  fireNumber: number
  projections: ProjectionPoint[]
  projectionsWithContributions: ProjectionPoint[]
}

export interface LeanFIREResult extends StandardFIREResult {
  isLean: boolean
  leanThreshold: number
}

export interface FatFIREResult extends StandardFIREResult {
  isFat: boolean
  fatThreshold: number
}

export interface BaristaFIREResult {
  baristaNumber: number
  fullFireNumber: number
  yearsToBaristaFIRE: number
  partTimeIncomeNeeded: number
  projections: ProjectionPoint[]
  savingsFromPartTime: number
}

export interface WithdrawalResult {
  portfolioLongevity: number // full years funded with a positive balance remaining, capped at retirementYears
  /**
   * Share of the retirement horizon funded by this single deterministic projection
   * (portfolioLongevity / retirementYears, capped at 1). This is NOT a probability of
   * success: it comes from one fixed-return path, not from historical or Monte Carlo
   * sequence-of-returns simulation.
   */
  horizonFundedRatio: number
  annualWithdrawal: number
  monthlyWithdrawal: number
  endingBalance: number
  withdrawalProjections: { year: number; balance: number; withdrawal: number }[]
  rateAnalysis: { rate: number; years: number; endBalance: number }[]
}

// ============================================
// Helper Functions
// ============================================

/**
 * Calculate future value with regular contributions
 * FV = PV(1+r)^n + PMT * (((1+r)^n - 1) / r)
 */
export function futureValue(
  presentValue: number,
  annualContribution: number,
  rate: number,
  years: number
): number {
  if (rate === 0) {
    return presentValue + annualContribution * years
  }
  const compoundFactor = Math.pow(1 + rate, years)
  return presentValue * compoundFactor + annualContribution * ((compoundFactor - 1) / rate)
}

/**
 * Calculate present value needed for a future target
 * PV = FV / (1+r)^n
 */
export function presentValue(futureVal: number, rate: number, years: number): number {
  if (years <= 0) return futureVal
  return futureVal / Math.pow(1 + rate, years)
}

/**
 * Real (inflation-adjusted) return: r_real = (1 + r_nominal) / (1 + i) - 1
 */
export function realReturn(expectedReturn: number, inflationRate: number): number {
  return (1 + expectedReturn) / (1 + inflationRate) - 1
}

const MAX_PROJECTION_YEARS = 100

/**
 * Nominal contribution paid at the end of year `year` (1-based).
 *
 * With `inflation` growth the contribution keeps a constant purchasing power, which is what makes the
 * deflated projection identical to the closed-form solution used for the headline FIRE age.
 */
export function contributionForYear(
  annualContribution: number,
  inflationRate: number,
  year: number,
  contributionGrowth: ContributionGrowth = DEFAULT_CONTRIBUTION_GROWTH
): number {
  return contributionGrowth === 'inflation'
    ? annualContribution * Math.pow(1 + inflationRate, year)
    : annualContribution
}

/**
 * Balance in today's dollars after `years` of flat nominal contributions.
 * Matches the year-by-year projection exactly at whole years, and interpolates between them.
 */
function flatContributionRealBalance(
  presentVal: number,
  annualContribution: number,
  expectedReturn: number,
  inflationRate: number,
  years: number
): number {
  const compoundFactor = Math.pow(1 + expectedReturn, years)
  const nominal = expectedReturn === 0
    ? presentVal + annualContribution * years
    : presentVal * compoundFactor + annualContribution * ((compoundFactor - 1) / expectedReturn)
  return nominal / Math.pow(1 + inflationRate, years)
}

/**
 * Calculate years to reach a target with contributions
 * Solves for n in: FV = PV(1+r)^n + PMT * (((1+r)^n - 1) / r)
 * Uses closed-form solution: n = ln((PMT + target*r) / (PMT + PV*r)) / ln(1+r)
 */
export function yearsToTarget(
  presentVal: number,
  annualContribution: number,
  rate: number,
  target: number
): number {
  if (presentVal >= target) return 0
  if (rate === 0) {
    if (annualContribution <= 0) return Infinity
    return (target - presentVal) / annualContribution
  }
  
  // Try closed-form solution for fractional years
  // n = ln((PMT + FV*r) / (PMT + PV*r)) / ln(1+r)
  const numerator = annualContribution + target * rate
  const denominator = annualContribution + presentVal * rate
  
  // Check if the target is reachable (denominator must be positive and numerator > denominator)
  if (denominator <= 0 || numerator <= denominator) {
    // Fall back to iterative approach if closed-form doesn't work
    let years = 0
    let current = presentVal
    const maxYears = MAX_PROJECTION_YEARS
    
    while (current < target && years < maxYears) {
      current = current * (1 + rate) + annualContribution
      years++
    }
    
    return years >= maxYears ? Infinity : years
  }
  
  const years = Math.log(numerator / denominator) / Math.log(1 + rate)
  
  // Sanity check - if result is negative or too large, use iterative
  if (years < 0 || years > MAX_PROJECTION_YEARS) {
    return Infinity
  }
  
  return years
}

/**
 * Years until the portfolio reaches a target expressed in today's dollars.
 *
 * This is the single source of truth for every headline FIRE age, and it is solved against the same
 * path that `generateProjections()` draws, so the chart crossing always equals the headline number.
 *
 * - `inflation` growth: closed form at the real return, because a constant-real contribution compounds
 *   at the real rate.
 * - `flat` growth: the closed form does not apply, so the deflated flat-contribution path is solved
 *   numerically (bracket scan, then bisection) for the same fractional-year precision.
 */
export function yearsToFIRETarget(
  presentVal: number,
  annualContribution: number,
  expectedReturn: number,
  inflationRate: number,
  target: number,
  contributionGrowth: ContributionGrowth = DEFAULT_CONTRIBUTION_GROWTH
): number {
  if (presentVal >= target) return 0

  if (contributionGrowth === 'inflation') {
    return yearsToTarget(presentVal, annualContribution, realReturn(expectedReturn, inflationRate), target)
  }

  const balanceAt = (years: number) =>
    flatContributionRealBalance(presentVal, annualContribution, expectedReturn, inflationRate, years)

  let lower = 0
  let upper = -1
  for (let year = 1; year <= MAX_PROJECTION_YEARS; year++) {
    if (balanceAt(year) >= target) {
      upper = year
      lower = year - 1
      break
    }
  }

  if (upper < 0) return Infinity

  for (let iteration = 0; iteration < 60; iteration++) {
    const midpoint = (lower + upper) / 2
    if (balanceAt(midpoint) >= target) {
      upper = midpoint
    } else {
      lower = midpoint
    }
  }

  return (lower + upper) / 2
}

/**
 * Generate projection points over time.
 *
 * `portfolio` is in future (nominal) dollars and `inflationAdjusted` is the same portfolio expressed in
 * today's dollars. `annualContribution` is stated in today's dollars; with the default `inflation`
 * growth the nominal amount contributed each year rises to preserve its purchasing power.
 */
export function generateProjections(
  currentAge: number,
  currentSavings: number,
  annualContribution: number,
  expectedReturn: number,
  inflationRate: number,
  years: number,
  contributionGrowth: ContributionGrowth = DEFAULT_CONTRIBUTION_GROWTH
): ProjectionPoint[] {
  const projections: ProjectionPoint[] = []
  let portfolio = currentSavings
  let totalContributions = currentSavings
  const currentYear = new Date().getFullYear()

  for (let i = 0; i <= years; i++) {
    const inflationAdjusted = portfolio / Math.pow(1 + inflationRate, i)
    const contribution = contributionForYear(annualContribution, inflationRate, i, contributionGrowth)

    projections.push({
      age: currentAge + i,
      year: currentYear + i,
      portfolio: Math.round(portfolio),
      contributions: i === 0 ? currentSavings : contribution,
      totalContributions: Math.round(totalContributions),
      inflationAdjusted: Math.round(inflationAdjusted),
    })

    const nextContribution = contributionForYear(annualContribution, inflationRate, i + 1, contributionGrowth)
    portfolio = portfolio * (1 + expectedReturn) + nextContribution
    totalContributions += nextContribution
  }

  return projections
}

/**
 * Format currency for display
 */
export function formatCurrency(value: number): string {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(value)
}

/**
 * Format percentage for display
 */
export function formatPercent(value: number): string {
  return `${(value * 100).toFixed(1)}%`
}

// ============================================
// Standard FIRE Calculator
// ============================================

/**
 * Calculate Standard FIRE metrics using the 4% rule (25x annual expenses)
 * 
 * The Standard FIRE approach calculates:
 * 1. FIRE Number = Annual Expenses / Safe Withdrawal Rate (typically 4%)
 * 2. Years to FIRE based on current savings, contributions, and expected returns
 * 3. Coast FIRE number (amount needed now to reach FIRE through growth alone)
 * 
 * Mathematical formulas:
 * - FIRE Number: FN = E / w (where E = annual expenses, w = withdrawal rate)
 * - Years to FIRE: Solved using logarithmic time-value-of-money equation
 * - Real return: r_real = (1 + r_nominal) / (1 + i) - 1 (adjusts for inflation)
 * 
 * Based on the Trinity Study which found a 4% withdrawal rate has historically
 * been safe for 30+ year retirements with a balanced stock/bond portfolio.
 * 
 * @param inputs - Calculator parameters including age, savings, expenses, rates
 * @returns FIRE metrics including target number, years to FIRE, and projections
 * 
 * @example
 * calculateStandardFIRE({
 *   currentAge: 30,
 *   retirementAge: 55,
 *   currentSavings: 100000,
 *   annualContribution: 24000,
 *   expectedReturn: 0.07,    // 7% nominal return
 *   inflationRate: 0.03,     // 3% inflation
 *   withdrawalRate: 0.04,    // 4% safe withdrawal rate
 *   annualExpenses: 48000
 * })
 * // Returns: { fireNumber: 1200000, yearsToFIRE: 21.5, ... }
 */
export function calculateStandardFIRE(inputs: FIREInputs): StandardFIREResult {
  const { 
    currentAge, 
    currentSavings, 
    annualContribution, 
    annualIncome,
    expectedReturn, 
    inflationRate,
    withdrawalRate, 
    annualExpenses,
    contributionGrowth = DEFAULT_CONTRIBUTION_GROWTH,
  } = inputs

  // FIRE Number = Annual Expenses / Withdrawal Rate
  const fireNumber = annualExpenses / withdrawalRate

  // Real return (adjusted for inflation)
  const realReturnRate = realReturn(expectedReturn, inflationRate)

  // Years to reach FIRE number, solved against the same path the projections draw
  const yearsToFIRE = yearsToFIRETarget(
    currentSavings,
    annualContribution,
    expectedReturn,
    inflationRate,
    fireNumber,
    contributionGrowth
  )
  const fireAge = currentAge + yearsToFIRE

  // Coast FIRE Number (amount needed now to coast to FIRE at target retirement age)
  const yearsToRetirement = Math.max(0, (inputs.retirementAge ?? fireAge) - currentAge)
  const coastFireNumber = presentValue(fireNumber, realReturnRate, yearsToRetirement)
  const roundedFireAge = Math.round(fireAge * 10) / 10
  const targetRetirementAge = inputs.retirementAge ?? roundedFireAge
  const targetAgeGap = roundedFireAge - targetRetirementAge
  const isOnTrack = Number.isFinite(roundedFireAge) && targetAgeGap <= 0
  const retirementGoalMessage = !Number.isFinite(roundedFireAge)
    ? 'Off track: FIRE is not reachable with the current assumptions.'
    : targetAgeGap < 0
      ? `On track: projected to reach FIRE ${Math.abs(targetAgeGap).toFixed(1)} years before your target retirement age.`
      : targetAgeGap > 0
        ? `Off track: projected to reach FIRE ${targetAgeGap.toFixed(1)} years after your target retirement age.`
        : 'On track: projected to reach FIRE at your target retirement age.'

  // Calculate savings rate based on annual income
  const savingsRate = annualIncome > 0 ? annualContribution / annualIncome : 0

  // Generate projections
  const projectionYears = Math.min(Math.ceil(yearsToFIRE) + 10, 50)
  const projections = generateProjections(
    currentAge,
    currentSavings,
    annualContribution,
    expectedReturn,
    inflationRate,
    projectionYears,
    contributionGrowth
  )

  return {
    fireNumber: Math.round(fireNumber),
    yearsToFIRE: Math.round(yearsToFIRE * 10) / 10,
    fireAge: roundedFireAge,
    projections,
    savingsRate,
    monthlyContribution: annualContribution / 12,
    coastFireNumber: Math.round(coastFireNumber),
    retirementGoal: {
      targetRetirementAge,
      calculatedFireAge: roundedFireAge,
      targetAgeGap,
      isOnTrack,
      message: retirementGoalMessage,
    },
  }
}

// ============================================
// Coast FIRE Calculator
// ============================================

/**
 * Calculate Coast FIRE - the point where you can stop contributing and let compound
 * interest carry you to your FIRE goal by target retirement age
 * 
 * Coast FIRE asks: "How much do I need saved NOW so that I don't need to save another
 * penny, and compound growth alone will get me to my FIRE number by retirement?"
 * 
 * Mathematical formula:
 * - Coast Number = FIRE Number / (1 + r)^years_remaining
 * - This is the present value (PV) calculation discounting future FIRE number
 * 
 * Two scenarios calculated:
 * 1. Continue contributing: Shows accelerated path to Coast FIRE
 * 2. Stop contributing now: Shows natural growth trajectory
 * 
 * @param currentAge - Current age in years
 * @param targetRetirementAge - Desired retirement age
 * @param currentSavings - Current portfolio value
 * @param annualContribution - Annual savings amount (used for accelerated scenario)
 * @param expectedReturn - Expected annual return (decimal, e.g., 0.07 for 7%)
 * @param inflationRate - Expected inflation rate (decimal)
 * @param annualExpenses - Annual living expenses in retirement
 * @param withdrawalRate - Safe withdrawal rate (typically 0.04)
 * @param contributionGrowth - Whether contributions escalate with inflation or stay flat
 * @returns Coast FIRE metrics including coast number, years to reach it, and projections
 *
 * @example
 * // annualExpenses comes BEFORE withdrawalRate. Passing them the other way round is silent —
 * // both are numbers — and yields a plausible-looking but entirely wrong result.
 * calculateCoastFIRE(30, 55, 100000, 24000, 0.07, 0.03, 48000, 0.04)
 * // If you have $100k at 30, you need ~$466k to "coast" to $1.2M by 55
 */
export function calculateCoastFIRE(
  currentAge: number,
  targetRetirementAge: number,
  currentSavings: number,
  annualContribution: number,
  expectedReturn: number,
  inflationRate: number,
  annualExpenses: number,
  withdrawalRate: number,
  contributionGrowth: ContributionGrowth = DEFAULT_CONTRIBUTION_GROWTH
): CoastFIREResult {
  // FIRE number at retirement
  const fireNumber = annualExpenses / withdrawalRate
  
  // Years until target retirement
  const yearsToRetirement = Math.max(0, targetRetirementAge - currentAge)
  
  // Real return
  const realReturnRate = realReturn(expectedReturn, inflationRate)
  
  // Coast number = what you need NOW to reach FIRE number at retirement without contributions
  const coastNumber = presentValue(fireNumber, realReturnRate, yearsToRetirement)
  
  // Are we already coasting?
  const alreadyCoasting = currentSavings >= coastNumber
  
  // Years to reach coast number (with contributions)
  const yearsToCoast = alreadyCoasting
    ? 0
    : yearsToFIRETarget(
        currentSavings,
        annualContribution,
        expectedReturn,
        inflationRate,
        coastNumber,
        contributionGrowth
      )
  
  // Projections without contributions (coast scenario)
  const projections = generateProjections(
    currentAge,
    currentSavings,
    0, // No contributions
    expectedReturn,
    inflationRate,
    yearsToRetirement + 10,
    contributionGrowth
  )
  
  // Projections with contributions (for comparison)
  const projectionsWithContributions = generateProjections(
    currentAge,
    currentSavings,
    annualContribution,
    expectedReturn,
    inflationRate,
    yearsToRetirement + 10,
    contributionGrowth
  )

  return {
    coastNumber: Math.round(coastNumber),
    yearsToCoast: Math.round(yearsToCoast * 10) / 10,
    alreadyCoasting,
    fireNumber: Math.round(fireNumber),
    projections,
    projectionsWithContributions,
  }
}

// ============================================
// Lean FIRE Calculator
// ============================================

const LEAN_FIRE_THRESHOLD = 40000 // $40k/year max for lean FIRE

export function calculateLeanFIRE(inputs: FIREInputs): LeanFIREResult {
  const standardResult = calculateStandardFIRE(inputs)
  
  return {
    ...standardResult,
    isLean: inputs.annualExpenses <= LEAN_FIRE_THRESHOLD,
    leanThreshold: LEAN_FIRE_THRESHOLD,
  }
}

// ============================================
// Fat FIRE Calculator
// ============================================

const FAT_FIRE_THRESHOLD = 100000 // $100k/year min for fat FIRE

export function calculateFatFIRE(inputs: FIREInputs): FatFIREResult {
  const standardResult = calculateStandardFIRE(inputs)
  
  return {
    ...standardResult,
    isFat: inputs.annualExpenses >= FAT_FIRE_THRESHOLD,
    fatThreshold: FAT_FIRE_THRESHOLD,
  }
}

// ============================================
// Barista FIRE Calculator
// ============================================

export function calculateBaristaFIRE(
  currentAge: number,
  currentSavings: number,
  annualContribution: number,
  expectedReturn: number,
  inflationRate: number,
  annualExpenses: number,
  withdrawalRate: number,
  partTimeAnnualIncome: number,
  contributionGrowth: ContributionGrowth = DEFAULT_CONTRIBUTION_GROWTH
): BaristaFIREResult {
  // Full FIRE number (without part-time income)
  const fullFireNumber = annualExpenses / withdrawalRate
  
  // Expenses that portfolio needs to cover = total expenses - part-time income
  const portfolioExpenses = Math.max(0, annualExpenses - partTimeAnnualIncome)
  
  // Barista FIRE number = reduced expenses / withdrawal rate
  const baristaNumber = portfolioExpenses / withdrawalRate
  
  // Years to reach Barista FIRE
  const yearsToBaristaFIRE = yearsToFIRETarget(
    currentSavings,
    annualContribution,
    expectedReturn,
    inflationRate,
    baristaNumber,
    contributionGrowth
  )
  
  // How much the part-time work saves in required portfolio
  const savingsFromPartTime = fullFireNumber - baristaNumber
  
  // Generate projections
  const projectionYears = Math.min(Math.ceil(yearsToBaristaFIRE) + 10, 50)
  const projections = generateProjections(
    currentAge,
    currentSavings,
    annualContribution,
    expectedReturn,
    inflationRate,
    projectionYears,
    contributionGrowth
  )

  return {
    baristaNumber: Math.round(baristaNumber),
    fullFireNumber: Math.round(fullFireNumber),
    yearsToBaristaFIRE: Math.round(yearsToBaristaFIRE * 10) / 10,
    partTimeIncomeNeeded: partTimeAnnualIncome,
    projections,
    savingsFromPartTime: Math.round(savingsFromPartTime),
  }
}

// ============================================
// Withdrawal Rate Calculator
// ============================================

/** Horizon used by the withdrawal-rate comparison table before a plan is treated as open-ended. */
const RATE_ANALYSIS_MAX_YEARS = 50

/**
 * Calculate portfolio longevity and withdrawal sustainability
 * 
 * Tests how long a portfolio will last given:
 * - An initial withdrawal amount (as % of portfolio)
 * - Annual withdrawals adjusted for inflation
 * - Portfolio growth at expected return rate
 * 
 * This models the retirement drawdown phase, answering:
 * "Will my money last through retirement?"
 * 
 * Mathematical model:
 * - Each year: Balance = Balance × (1 + r) - Withdrawal
 * - Withdrawal increases annually: W_n = W_0 × (1 + inflation)^n
 * - Portfolio fails when Balance <= 0
 * 
 * The 4% rule historically provided 95%+ success over 30-year periods,
 * but actual safe rates depend on:
 * - Asset allocation (stocks vs bonds)
 * - Sequence of returns risk
 * - Retirement time horizon
 * - Flexibility to reduce spending in bad years
 *
 * This function projects a single deterministic path at a fixed return. It does not run
 * historical or Monte Carlo simulations, so it cannot produce a probability of success.
 *
 * Year convention: both `portfolioLongevity` and each `rateAnalysis[].years` count the
 * full years funded while a positive balance remained, so the headline and the comparison
 * table always agree for the same rate.
 * 
 * @param portfolioValue - Starting portfolio balance
 * @param withdrawalRate - Initial withdrawal rate (decimal, e.g., 0.04 for 4%)
 * @param expectedReturn - Annual portfolio return (nominal, not inflation-adjusted)
 * @param inflationRate - Expected inflation for withdrawal adjustments
 * @param retirementYears - Expected retirement duration in years
 * @returns Analysis including years portfolio lasts, ending balance, and rate comparisons
 * 
 * @example
 * calculateWithdrawal(1000000, 0.04, 0.07, 0.03, 30)
 * // Tests if $1M portfolio with 4% withdrawal lasts 30 years at 7% return
 */
export function calculateWithdrawal(
  portfolioValue: number,
  withdrawalRate: number,
  expectedReturn: number,
  inflationRate: number,
  retirementYears: number
): WithdrawalResult {
  const annualWithdrawal = portfolioValue * withdrawalRate
  const monthlyWithdrawal = annualWithdrawal / 12
  
  // Use nominal return with inflation-adjusted withdrawals
  // This properly models: portfolio grows at nominal rate, withdrawals increase with inflation
  const nominalReturn = expectedReturn
  
  // Calculate portfolio longevity
  let balance = portfolioValue
  let year = 0
  const withdrawalProjections: { year: number; balance: number; withdrawal: number }[] = []
  let adjustedWithdrawal = annualWithdrawal
  
  while (balance > 0 && year <= retirementYears) {
    withdrawalProjections.push({
      year,
      balance: Math.round(balance),
      withdrawal: Math.round(adjustedWithdrawal),
    })
    
    balance = balance * (1 + nominalReturn) - adjustedWithdrawal
    adjustedWithdrawal *= (1 + inflationRate) // Adjust withdrawal for inflation
    year++
  }
  
  // Years fully funded: the last projection year that still ended with a positive balance.
  const portfolioLongevity = Math.max(0, year - 1)
  const endingBalance = Math.max(0, withdrawalProjections[withdrawalProjections.length - 1]?.balance || 0)

  // Deterministic coverage of the horizon on this single fixed-return path.
  // This is not a success probability; modeling that would require sequence-of-returns simulation.
  const horizonFundedRatio = retirementYears <= 0 || portfolioLongevity >= retirementYears
    ? 1
    : portfolioLongevity / retirementYears

  // Analyze different withdrawal rates using the same "years fully funded" convention
  // as portfolioLongevity so the headline and this table cannot disagree.
  const rates = [0.03, 0.035, 0.04, 0.045, 0.05]
  const rateAnalysis = rates.map(rate => {
    let bal = portfolioValue
    let yr = 0
    let withdrawal = portfolioValue * rate

    while (bal > 0 && yr < RATE_ANALYSIS_MAX_YEARS) {
      bal = bal * (1 + nominalReturn) - withdrawal
      withdrawal *= (1 + inflationRate)
      yr++
    }

    return {
      rate,
      years: Math.max(0, bal > 0 ? yr : yr - 1),
      endBalance: Math.max(0, Math.round(bal)),
    }
  })

  return {
    portfolioLongevity,
    horizonFundedRatio,
    annualWithdrawal: Math.round(annualWithdrawal),
    monthlyWithdrawal: Math.round(monthlyWithdrawal),
    endingBalance,
    withdrawalProjections,
    rateAnalysis,
  }
}

// ============================================
// Debt Payoff Calculator
// ============================================

export interface DebtItem {
  id: string
  name: string
  balance: number
  rate: number // Annual rate as decimal, e.g., 0.18 for 18%
  minPayment: number
}

export interface DebtPayoffMonth {
  month: number
  totalBalance: number
  principalPaid: number
  interestPaid: number
  cumulativePrincipal: number
  cumulativeInterest: number
  debtsPaidOff: string[] // Names of debts paid off this month
  debtsRemaining: { name: string; balance: number }[]
}

export interface DebtPayoffResult {
  totalMonths: number
  totalInterest: number
  totalPrincipal: number
  monthlyPayment: number
  projections: DebtPayoffMonth[]
  payoffOrder: string[] // Order in which debts are paid off
  debtMilestones: { month: number; debtName: string }[]
}

/**
 * Calculate debt payoff using Snowball method (smallest balance first)
 */
export function calculateSnowballPayoff(
  debts: DebtItem[],
  totalMonthlyPayment: number,
  extraPayment: number = 0
): DebtPayoffResult {
  // Sort debts by balance (smallest first)
  const sortedDebts = [...debts].sort((a, b) => a.balance - b.balance)
  return calculateDebtPayoff(sortedDebts, totalMonthlyPayment, extraPayment)
}

/**
 * Calculate debt payoff using Avalanche method (highest interest rate first)
 */
export function calculateAvalanchePayoff(
  debts: DebtItem[],
  totalMonthlyPayment: number,
  extraPayment: number = 0
): DebtPayoffResult {
  // Sort debts by interest rate (highest first)
  const sortedDebts = [...debts].sort((a, b) => b.rate - a.rate)
  return calculateDebtPayoff(sortedDebts, totalMonthlyPayment, extraPayment)
}

/**
 * Core debt payoff calculation logic.
 *
 * Each month interest accrues exactly once per debt before any payment is applied, then the
 * available budget pays minimums in priority order and any remainder goes to the highest
 * priority debt as pure principal. Payments never exceed the available budget, so a budget
 * that cannot cover the minimums results in growing balances instead of silent overpayment.
 */
function calculateDebtPayoff(
  sortedDebts: DebtItem[],
  totalMonthlyPayment: number,
  extraPayment: number = 0
): DebtPayoffResult {
  // Clone debts to track balances
  const remainingDebts = sortedDebts.map(d => ({ ...d, currentBalance: d.balance }))
  
  const projections: DebtPayoffMonth[] = []
  const payoffOrder: string[] = []
  const debtMilestones: { month: number; debtName: string }[] = []
  
  let month = 0
  let cumulativePrincipal = 0
  let cumulativeInterest = 0
  const totalPrincipal = sortedDebts.reduce((sum, d) => sum + d.balance, 0)
  
  const availablePayment = totalMonthlyPayment + extraPayment
  
  while (remainingDebts.some(d => d.currentBalance > 0) && month < 600) { // Max 50 years
    month++
    
    let monthlyBudget = availablePayment
    let monthPayments = 0
    let monthInterest = 0
    const paidOffThisMonth: string[] = []
    
    const markPaidOff = (debt: { name: string; currentBalance: number }) => {
      if (debt.currentBalance > 0) return
      debt.currentBalance = 0
      if (paidOffThisMonth.includes(debt.name)) return
      paidOffThisMonth.push(debt.name)
      payoffOrder.push(debt.name)
      debtMilestones.push({ month, debtName: debt.name })
    }
    
    // 1. Accrue interest exactly once per debt, before any payment is applied
    for (const debt of remainingDebts) {
      if (debt.currentBalance <= 0) continue
      
      const interestCharge = debt.currentBalance * (debt.rate / 12)
      debt.currentBalance += interestCharge
      monthInterest += interestCharge
    }
    
    // 2. Pay minimums in priority order, never spending more than the available budget
    for (const debt of remainingDebts) {
      if (debt.currentBalance <= 0 || monthlyBudget <= 0) continue
      
      const payment = Math.min(debt.minPayment, debt.currentBalance, monthlyBudget)
      debt.currentBalance -= payment
      monthlyBudget -= payment
      monthPayments += payment
      markPaidOff(debt)
    }
    
    // 3. Apply any remaining budget to the highest priority debt as pure principal
    while (monthlyBudget > 0) {
      const targetDebt = remainingDebts.find(d => d.currentBalance > 0)
      if (!targetDebt) break
      
      const payment = Math.min(monthlyBudget, targetDebt.currentBalance)
      targetDebt.currentBalance -= payment
      monthlyBudget -= payment
      monthPayments += payment
      markPaidOff(targetDebt)
    }
    
    // Balances already include this month's interest, so principal is what is left of the payments
    const monthPrincipal = monthPayments - monthInterest
    
    cumulativePrincipal += monthPrincipal
    cumulativeInterest += monthInterest
    
    const totalBalance = remainingDebts.reduce((sum, d) => sum + d.currentBalance, 0)
    
    projections.push({
      month,
      totalBalance: Math.round(totalBalance),
      principalPaid: Math.round(monthPrincipal),
      interestPaid: Math.round(monthInterest),
      cumulativePrincipal: Math.round(cumulativePrincipal),
      cumulativeInterest: Math.round(cumulativeInterest),
      debtsPaidOff: paidOffThisMonth,
      debtsRemaining: remainingDebts
        .filter(d => d.currentBalance > 0)
        .map(d => ({ name: d.name, balance: Math.round(d.currentBalance) })),
    })
    
    // Break if all debts are paid
    if (totalBalance <= 0) break
  }
  
  return {
    totalMonths: month,
    totalInterest: Math.round(cumulativeInterest),
    totalPrincipal: Math.round(totalPrincipal),
    monthlyPayment: totalMonthlyPayment + extraPayment,
    projections,
    payoffOrder,
    debtMilestones,
  }
}

/**
 * Calculate required monthly payment to pay off debts in target months
 */
export function calculateDebtPayoffByTimeline(
  debts: DebtItem[],
  targetMonths: number,
  strategy: 'snowball' | 'avalanche',
  extraPayment: number = 0
): { requiredPayment: number; result: DebtPayoffResult } | null {
  if (targetMonths <= 0 || debts.length === 0) return null
  
  // Binary search for required payment
  const totalBalance = debts.reduce((sum, d) => sum + d.balance, 0)
  const totalMinPayment = debts.reduce((sum, d) => sum + d.minPayment, 0)
  
  let minPayment = totalMinPayment
  let maxPayment = totalBalance // Upper bound
  let requiredPayment = minPayment
  let result: DebtPayoffResult | null = null
  
  // Binary search with max 30 iterations
  for (let i = 0; i < 30; i++) {
    const testPayment = (minPayment + maxPayment) / 2
    const testResult = strategy === 'snowball' 
      ? calculateSnowballPayoff(debts, testPayment, extraPayment)
      : calculateAvalanchePayoff(debts, testPayment, extraPayment)
    
    if (testResult.totalMonths <= targetMonths) {
      requiredPayment = testPayment
      result = testResult
      maxPayment = testPayment
    } else {
      minPayment = testPayment
    }
    
    // If we're close enough, break
    if (Math.abs(maxPayment - minPayment) < 1) break
  }
  
  return result ? { requiredPayment: Math.round(requiredPayment), result } : null
}

// ============================================
// Reverse FIRE Calculator
// ============================================

export interface ReverseFIREResult {
  fireNumber: number
  yearsToFIRE: number
  requiredAnnualSavings: number
  requiredMonthlySavings: number
  projections: ProjectionPoint[]
  alreadyAchievable: boolean
  currentWillGrowTo: number
}

/**
 * Work backwards from a retirement age to the savings required each year.
 *
 * The FIRE number is expressed in today's dollars, so the required saving is solved against the same
 * deflated path `generateProjections()` draws. With the default `inflation` growth the answer is the
 * contribution in today's dollars; with `flat` growth it is a fixed nominal amount.
 */
export function calculateReverseFIRE(
  currentAge: number,
  targetRetirementAge: number,
  currentSavings: number,
  annualExpenses: number,
  expectedReturn: number,
  inflationRate: number,
  withdrawalRate: number,
  contributionGrowth: ContributionGrowth = DEFAULT_CONTRIBUTION_GROWTH
): ReverseFIREResult {
  const yearsToFIRE = Math.max(1, targetRetirementAge - currentAge)
  const fireNumber = annualExpenses / withdrawalRate
  const realReturnRate = realReturn(expectedReturn, inflationRate)

  // Existing savings deflate the same way in both models, so this is always in today's dollars.
  const futureValueOfCurrent = currentSavings * Math.pow(1 + realReturnRate, yearsToFIRE)

  let requiredAnnualSavings: number
  if (futureValueOfCurrent >= fireNumber) {
    requiredAnnualSavings = 0
  } else if (contributionGrowth === 'inflation') {
    const compoundFactor = Math.pow(1 + realReturnRate, yearsToFIRE)
    requiredAnnualSavings = realReturnRate === 0
      ? (fireNumber - currentSavings) / yearsToFIRE
      : (fireNumber - futureValueOfCurrent) * realReturnRate / (compoundFactor - 1)
  } else {
    // Flat nominal contributions: solve the deflated flat path for a constant nominal payment.
    const nominalTarget = fireNumber * Math.pow(1 + inflationRate, yearsToFIRE)
    const compoundFactor = Math.pow(1 + expectedReturn, yearsToFIRE)
    const nominalValueOfCurrent = currentSavings * compoundFactor
    requiredAnnualSavings = expectedReturn === 0
      ? (nominalTarget - nominalValueOfCurrent) / yearsToFIRE
      : (nominalTarget - nominalValueOfCurrent) * expectedReturn / (compoundFactor - 1)
  }

  const safeAnnualSavings = Math.max(0, requiredAnnualSavings)

  return {
    fireNumber,
    yearsToFIRE,
    requiredAnnualSavings: safeAnnualSavings,
    requiredMonthlySavings: safeAnnualSavings / 12,
    projections: generateProjections(
      currentAge,
      currentSavings,
      safeAnnualSavings,
      expectedReturn,
      inflationRate,
      yearsToFIRE + 10,
      contributionGrowth
    ),
    alreadyAchievable: futureValueOfCurrent >= fireNumber,
    currentWillGrowTo: Math.round(futureValueOfCurrent),
  }
}

// ============================================
// Savings & Investment Growth Calculator
// ============================================

export interface InvestmentProjectionPoint {
  age: number
  year: number
  portfolio: number
  inflationAdjusted: number
  totalContributions: number
  contributions: number
}

export interface InvestmentGrowthResult {
  savingsRate: number
  annualContribution: number
  monthlyContribution: number
  finalNominalBalance: number
  finalInflationAdjustedBalance: number
  totalInvested: number
  totalGrowth: number
  inflationImpact: number
  projections: InvestmentProjectionPoint[]
}

/**
 * Project a repeatable contribution plan.
 *
 * Both series describe one plan: `portfolio` is in future dollars and `inflationAdjusted` is that same
 * balance deflated to today's dollars. The contribution is stated in today's dollars, so with the
 * default `inflation` growth the nominal amount invested rises each year to hold its purchasing power.
 */
export function calculateInvestmentGrowth(
  startingAmount: number,
  contributionAmount: number,
  contributionFrequency: 'monthly' | 'yearly',
  yearsInvesting: number,
  expectedReturn: number,
  inflationRate: number,
  annualIncome: number,
  currentAge: number,
  contributionGrowth: ContributionGrowth = DEFAULT_CONTRIBUTION_GROWTH
): InvestmentGrowthResult {
  const annualContribution = contributionFrequency === 'monthly' ? contributionAmount * 12 : contributionAmount
  const savingsRate = annualIncome > 0 ? annualContribution / annualIncome : 0
  const projections: InvestmentProjectionPoint[] = []
  const currentYear = new Date().getFullYear()

  let nominalBalance = startingAmount
  let totalContributions = startingAmount

  projections.push({
    age: currentAge,
    year: currentYear,
    portfolio: Math.round(nominalBalance),
    inflationAdjusted: Math.round(nominalBalance),
    totalContributions: Math.round(totalContributions),
    contributions: 0,
  })

  for (let year = 1; year <= yearsInvesting; year += 1) {
    const contribution = contributionForYear(annualContribution, inflationRate, year, contributionGrowth)
    nominalBalance = nominalBalance * (1 + expectedReturn) + contribution
    totalContributions += contribution

    projections.push({
      age: currentAge + year,
      year: currentYear + year,
      portfolio: Math.round(nominalBalance),
      inflationAdjusted: Math.round(nominalBalance / Math.pow(1 + inflationRate, year)),
      totalContributions: Math.round(totalContributions),
      contributions: contribution,
    })
  }

  const finalInflationAdjustedBalance = nominalBalance / Math.pow(1 + inflationRate, yearsInvesting)

  return {
    savingsRate,
    annualContribution,
    monthlyContribution: annualContribution / 12,
    finalNominalBalance: nominalBalance,
    finalInflationAdjustedBalance,
    totalInvested: totalContributions,
    totalGrowth: nominalBalance - totalContributions,
    inflationImpact: nominalBalance - finalInflationAdjustedBalance,
    projections,
  }
}

// ============================================
// Healthcare Gap Calculator
// ============================================

export const MEDICARE_AGE = 65

export interface HealthcareYear {
  age: number
  year: number
  cost: number
  premium: number
  deductible: number
  outOfPocket: number
}

export interface HealthcareGapResult {
  gapYears: number
  annualCost: number
  totalCost: number
  avgAnnualCost: number
  yearlyBreakdown: HealthcareYear[]
}

/**
 * Cost of self-funded healthcare between early retirement and Medicare eligibility.
 * Costs are stated in today's dollars and inflated year by year.
 */
export function calculateHealthcareGap(
  currentAge: number,
  earlyRetirementAge: number,
  monthlyPremium: number,
  annualDeductible: number,
  annualOutOfPocket: number,
  inflationRate: number
): HealthcareGapResult {
  const gapYears = Math.max(0, MEDICARE_AGE - earlyRetirementAge)
  const annualCost = monthlyPremium * 12 + annualDeductible + annualOutOfPocket
  const currentYear = new Date().getFullYear()
  const yearlyBreakdown: HealthcareYear[] = []
  let totalCost = 0

  for (let index = 0; index < gapYears; index += 1) {
    const multiplier = Math.pow(1 + inflationRate, index)
    const cost = annualCost * multiplier
    totalCost += cost
    yearlyBreakdown.push({
      age: earlyRetirementAge + index,
      year: currentYear + earlyRetirementAge - currentAge + index,
      cost: Math.round(cost),
      premium: Math.round(monthlyPremium * 12 * multiplier),
      deductible: Math.round(annualDeductible * multiplier),
      outOfPocket: Math.round(annualOutOfPocket * multiplier),
    })
  }

  return {
    gapYears,
    annualCost,
    totalCost: Math.round(totalCost),
    avgAnnualCost: gapYears > 0 ? Math.round(totalCost / gapYears) : 0,
    yearlyBreakdown,
  }
}
