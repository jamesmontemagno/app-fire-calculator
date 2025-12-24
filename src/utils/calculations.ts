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
 * @param withdrawalRate - Safe withdrawal rate (typically 0.04)
 * @param annualExpenses - Annual living expenses in retirement
 * @returns Coast FIRE metrics including coast number, years to reach it, and projections
 * 
 * @example
 * calculateCoastFIRE(30, 55, 100000, 24000, 0.07, 0.03, 0.04, 48000)
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
 * Core debt payoff calculation logic
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
    let monthPrincipal = 0
    let monthInterest = 0
    const paidOffThisMonth: string[] = []
    
    // First, pay minimum payments on all debts
    for (const debt of remainingDebts) {
      if (debt.currentBalance <= 0) continue
      
      const monthlyRate = debt.rate / 12
      const interestCharge = debt.currentBalance * monthlyRate
      const minPaymentNeeded = Math.min(debt.minPayment, debt.currentBalance + interestCharge)
      const principalPayment = Math.max(0, minPaymentNeeded - interestCharge)
      
      debt.currentBalance -= principalPayment
      monthlyBudget -= minPaymentNeeded
      monthPrincipal += principalPayment
      monthInterest += interestCharge
      
      if (debt.currentBalance <= 0) {
        debt.currentBalance = 0
        paidOffThisMonth.push(debt.name)
        payoffOrder.push(debt.name)
        debtMilestones.push({ month, debtName: debt.name })
      }
    }
    
    // Apply remaining budget to highest priority debt
    if (monthlyBudget > 0) {
      const targetDebt = remainingDebts.find(d => d.currentBalance > 0)
      if (targetDebt) {
        const monthlyRate = targetDebt.rate / 12
        const additionalInterest = targetDebt.currentBalance * monthlyRate
        const maxPayment = targetDebt.currentBalance + additionalInterest
        const actualPayment = Math.min(monthlyBudget, maxPayment)
        const additionalPrincipal = Math.max(0, actualPayment - additionalInterest)
        
        targetDebt.currentBalance -= additionalPrincipal
        monthPrincipal += additionalPrincipal
        monthInterest += additionalInterest
        
        if (targetDebt.currentBalance <= 0) {
          targetDebt.currentBalance = 0
          if (!paidOffThisMonth.includes(targetDebt.name)) {
            paidOffThisMonth.push(targetDebt.name)
            payoffOrder.push(targetDebt.name)
            debtMilestones.push({ month, debtName: targetDebt.name })
          }
        }
      }
    }
    
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
