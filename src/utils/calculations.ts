// ============================================
// FIRE Calculator - Core Calculation Functions
// ============================================

export interface FIREInputs {
  currentAge: number
  retirementAge: number
  currentSavings: number
  annualContribution: number
  expectedReturn: number // as decimal, e.g., 0.07 for 7%
  inflationRate: number // as decimal
  withdrawalRate: number // as decimal, e.g., 0.04 for 4%
  annualExpenses: number
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
  portfolioLongevity: number // years the portfolio lasts
  successRate: number // based on historical simulations
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
    const maxYears = 100
    
    while (current < target && years < maxYears) {
      current = current * (1 + rate) + annualContribution
      years++
    }
    
    return years >= maxYears ? Infinity : years
  }
  
  const years = Math.log(numerator / denominator) / Math.log(1 + rate)
  
  // Sanity check - if result is negative or too large, use iterative
  if (years < 0 || years > 100) {
    return Infinity
  }
  
  return years
}

/**
 * Generate projection points over time
 */
export function generateProjections(
  currentAge: number,
  currentSavings: number,
  annualContribution: number,
  expectedReturn: number,
  inflationRate: number,
  years: number
): ProjectionPoint[] {
  const projections: ProjectionPoint[] = []
  let portfolio = currentSavings
  let totalContributions = currentSavings
  const currentYear = new Date().getFullYear()

  for (let i = 0; i <= years; i++) {
    const inflationAdjusted = portfolio / Math.pow(1 + inflationRate, i)
    
    projections.push({
      age: currentAge + i,
      year: currentYear + i,
      portfolio: Math.round(portfolio),
      contributions: i === 0 ? currentSavings : annualContribution,
      totalContributions: Math.round(totalContributions),
      inflationAdjusted: Math.round(inflationAdjusted),
    })

    portfolio = portfolio * (1 + expectedReturn) + annualContribution
    totalContributions += annualContribution
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

export function calculateStandardFIRE(inputs: FIREInputs): StandardFIREResult {
  const { 
    currentAge, 
    currentSavings, 
    annualContribution, 
    expectedReturn, 
    inflationRate,
    withdrawalRate, 
    annualExpenses 
  } = inputs

  // FIRE Number = Annual Expenses / Withdrawal Rate
  const fireNumber = annualExpenses / withdrawalRate

  // Real return (adjusted for inflation)
  const realReturn = (1 + expectedReturn) / (1 + inflationRate) - 1

  // Years to reach FIRE number
  const yearsToFIRE = yearsToTarget(currentSavings, annualContribution, realReturn, fireNumber)
  const fireAge = currentAge + yearsToFIRE

  // Coast FIRE Number (amount needed now to coast to FIRE at target retirement age)
  const yearsToRetirement = Math.max(0, inputs.retirementAge - currentAge)
  const coastFireNumber = presentValue(fireNumber, realReturn, yearsToRetirement)

  // Calculate savings rate (assumes income = contributions + expenses)
  const estimatedIncome = annualContribution + annualExpenses
  const savingsRate = estimatedIncome > 0 ? annualContribution / estimatedIncome : 0

  // Generate projections
  const projectionYears = Math.min(Math.ceil(yearsToFIRE) + 10, 50)
  const projections = generateProjections(
    currentAge,
    currentSavings,
    annualContribution,
    expectedReturn,
    inflationRate,
    projectionYears
  )

  return {
    fireNumber: Math.round(fireNumber),
    yearsToFIRE: Math.round(yearsToFIRE * 10) / 10,
    fireAge: Math.round(fireAge * 10) / 10,
    projections,
    savingsRate,
    monthlyContribution: annualContribution / 12,
    coastFireNumber: Math.round(coastFireNumber),
  }
}

// ============================================
// Coast FIRE Calculator
// ============================================

export function calculateCoastFIRE(
  currentAge: number,
  targetRetirementAge: number,
  currentSavings: number,
  annualContribution: number,
  expectedReturn: number,
  inflationRate: number,
  annualExpenses: number,
  withdrawalRate: number
): CoastFIREResult {
  // FIRE number at retirement
  const fireNumber = annualExpenses / withdrawalRate
  
  // Years until target retirement
  const yearsToRetirement = Math.max(0, targetRetirementAge - currentAge)
  
  // Real return
  const realReturn = (1 + expectedReturn) / (1 + inflationRate) - 1
  
  // Coast number = what you need NOW to reach FIRE number at retirement without contributions
  const coastNumber = presentValue(fireNumber, realReturn, yearsToRetirement)
  
  // Are we already coasting?
  const alreadyCoasting = currentSavings >= coastNumber
  
  // Years to reach coast number (with contributions)
  const yearsToCoast = alreadyCoasting ? 0 : yearsToTarget(currentSavings, annualContribution, realReturn, coastNumber)
  
  // Projections without contributions (coast scenario)
  const projections = generateProjections(
    currentAge,
    currentSavings,
    0, // No contributions
    expectedReturn,
    inflationRate,
    yearsToRetirement + 10
  )
  
  // Projections with contributions (for comparison)
  const projectionsWithContributions = generateProjections(
    currentAge,
    currentSavings,
    annualContribution,
    expectedReturn,
    inflationRate,
    yearsToRetirement + 10
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
  partTimeAnnualIncome: number
): BaristaFIREResult {
  // Full FIRE number (without part-time income)
  const fullFireNumber = annualExpenses / withdrawalRate
  
  // Expenses that portfolio needs to cover = total expenses - part-time income
  const portfolioExpenses = Math.max(0, annualExpenses - partTimeAnnualIncome)
  
  // Barista FIRE number = reduced expenses / withdrawal rate
  const baristaNumber = portfolioExpenses / withdrawalRate
  
  // Real return
  const realReturn = (1 + expectedReturn) / (1 + inflationRate) - 1
  
  // Years to reach Barista FIRE
  const yearsToBaristaFIRE = yearsToTarget(currentSavings, annualContribution, realReturn, baristaNumber)
  
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
    projectionYears
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
  
  const portfolioLongevity = year - 1
  const endingBalance = Math.max(0, withdrawalProjections[withdrawalProjections.length - 1]?.balance || 0)
  
  // Calculate goal achievement rate (simplified - based on whether portfolio lasts through retirement)
  const successRate = portfolioLongevity >= retirementYears ? 1 : portfolioLongevity / retirementYears
  
  // Analyze different withdrawal rates
  const rates = [0.03, 0.035, 0.04, 0.045, 0.05]
  const rateAnalysis = rates.map(rate => {
    let bal = portfolioValue
    let yr = 0
    let withdrawal = portfolioValue * rate
    
    while (bal > 0 && yr < 50) {
      bal = bal * (1 + nominalReturn) - withdrawal
      withdrawal *= (1 + inflationRate)
      yr++
    }
    
    return {
      rate,
      years: yr,
      endBalance: Math.max(0, Math.round(bal)),
    }
  })

  return {
    portfolioLongevity,
    successRate,
    annualWithdrawal: Math.round(annualWithdrawal),
    monthlyWithdrawal: Math.round(monthlyWithdrawal),
    endingBalance,
    withdrawalProjections,
    rateAnalysis,
  }
}
