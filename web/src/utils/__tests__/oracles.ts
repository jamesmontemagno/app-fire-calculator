/**
 * Independent analytic oracles.
 *
 * ============================ READ THIS BEFORE EDITING ============================
 * Nothing in this file may import from `../calculations` or `../deferredCompensation`.
 * Every function here is derived from the underlying financial algebra and is written to be
 * read and checked against a textbook, not against the shipped implementation. That is the whole
 * point: a test whose expectation came out of the code under test is a screenshot, not a test.
 *
 * Two of the bugs the cross-platform audit fixed (#45, #46) shipped precisely because the
 * behaviour looked intentional — nothing independent contradicted it.
 * =================================================================================
 */

/** Real (inflation-adjusted) return. Fisher relation: (1 + rho)(1 + i) = (1 + r). */
export function realRate(nominalReturn: number, inflationRate: number): number {
  return (1 + nominalReturn) / (1 + inflationRate) - 1
}

/**
 * Balance after `n` years in TODAY'S dollars, closed form.
 *
 *   P_n / (1+i)^n = PV(1+rho)^n + C((1+rho)^n - 1)/rho,     rho = (1+r)/(1+i) - 1
 *
 * This is an identity, not an approximation, and it holds only under the app's convention that a
 * contribution entered in today's dollars is actually paid as `C(1+i)^k` nominal at the end of
 * year k. `nominalEscalatingSeries` below simulates that convention from scratch; the two agreeing
 * to floating-point noise is the proof the identity applies.
 *
 * The `rho === 0` branch is the degenerate case r === i, where the annuity factor
 * ((1+rho)^n - 1)/rho is 0/0 and the limit is simply n.
 */
export function realBalanceClosedForm(
  presentValue: number,
  annualContribution: number,
  nominalReturn: number,
  inflationRate: number,
  years: number,
): number {
  const rho = realRate(nominalReturn, inflationRate)
  if (rho === 0) return presentValue + annualContribution * years
  const growth = Math.pow(1 + rho, years)
  return presentValue * growth + annualContribution * ((growth - 1) / rho)
}

/**
 * Nominal balances year by year under inflation-escalating contributions, simulated directly from
 * the recurrence rather than from any closed form:
 *
 *   B_0 = PV,   B_k = B_{k-1}(1 + r) + C(1 + i)^k
 *
 * Returns `[B_0, B_1, ... B_years]`.
 */
export function nominalEscalatingSeries(
  presentValue: number,
  annualContribution: number,
  nominalReturn: number,
  inflationRate: number,
  years: number,
): number[] {
  const series = [presentValue]
  let balance = presentValue
  for (let k = 1; k <= years; k++) {
    balance = balance * (1 + nominalReturn) + annualContribution * Math.pow(1 + inflationRate, k)
    series.push(balance)
  }
  return series
}

/**
 * Nominal balances under FLAT contributions: the same nominal amount every year, so its purchasing
 * power erodes.
 *
 *   B_0 = PV,   B_k = B_{k-1}(1 + r) + C
 *
 * No closed form in today's dollars exists for this model, which is why the shipped code solves it
 * numerically. Simulating it here gives an oracle that owes the shipped solver nothing.
 */
export function nominalFlatSeries(
  presentValue: number,
  annualContribution: number,
  nominalReturn: number,
  years: number,
): number[] {
  const series = [presentValue]
  let balance = presentValue
  for (let k = 1; k <= years; k++) {
    balance = balance * (1 + nominalReturn) + annualContribution
    series.push(balance)
  }
  return series
}

/** Deflate a nominal amount observed at year `n` back into today's dollars. */
export function deflate(nominal: number, inflationRate: number, years: number): number {
  return nominal / Math.pow(1 + inflationRate, years)
}

/**
 * Years to grow `presentValue` to `target` at rate `rate` with level payment `payment`, in closed
 * form. Solve FV = PV(1+g)^n + C((1+g)^n - 1)/g for n:
 *
 *   (C + FV*g) / (C + PV*g) = (1+g)^n    =>    n = ln((C + FV*g)/(C + PV*g)) / ln(1+g)
 *
 * Returns a fractional year count. `null` when the target is unreachable (the ratio is <= 1, i.e.
 * the balance converges below the target).
 */
export function yearsToTargetClosedForm(
  presentValue: number,
  payment: number,
  rate: number,
  target: number,
): number | null {
  if (presentValue >= target) return 0
  if (rate === 0) return payment > 0 ? (target - presentValue) / payment : null
  const numerator = payment + target * rate
  const denominator = payment + presentValue * rate
  if (denominator <= 0 || numerator <= denominator) return null
  return Math.log(numerator / denominator) / Math.log(1 + rate)
}

/**
 * The balance a declining-real-return plan converges to.
 *
 * When rho < 0 the recurrence B_{k+1} = B_k(1 + rho) + C has a stable fixed point at
 * B* = -C/rho. Any target above B* is mathematically unreachable no matter how long you wait, so
 * `Infinity` is the correct answer rather than a bug to be papered over.
 */
export function realFixedPoint(annualContribution: number, rho: number): number {
  return -annualContribution / rho
}

/**
 * Months to clear a single loan with level payment `payment`, standard amortization closed form.
 *
 *   n = -ln(1 - P*m/A) / ln(1 + m),    m = APR/12
 *
 * The lender charges interest once per period before the payment lands, so the real month count is
 * ceil(n). Returns `null` when the payment never covers the first month's interest (P*m >= A), in
 * which case the balance grows without bound.
 */
export function monthsToPayOffClosedForm(principal: number, apr: number, payment: number): number | null {
  const monthlyRate = apr / 12
  if (monthlyRate === 0) return Math.ceil(principal / payment)
  if (principal * monthlyRate >= payment) return null
  return -Math.log(1 - (principal * monthlyRate) / payment) / Math.log(1 + monthlyRate)
}

/**
 * Total interest paid clearing a single loan, simulated from the recurrence:
 * interest accrues once, then the payment is applied.
 *
 *   B <- B(1 + m);  pay min(A, B)
 *
 * Charging interest more than once per month is exactly the #45 defect, so this oracle is written
 * to accrue it exactly once and nowhere else.
 */
export function amortize(principal: number, apr: number, payment: number, maxMonths = 600): {
  months: number
  totalInterest: number
  firstMonthInterest: number
} {
  const monthlyRate = apr / 12
  let balance = principal
  let totalInterest = 0
  let firstMonthInterest = 0
  let months = 0
  while (balance > 0 && months < maxMonths) {
    months++
    const interest = balance * monthlyRate
    if (months === 1) firstMonthInterest = interest
    balance += interest
    totalInterest += interest
    balance -= Math.min(payment, balance)
  }
  return { months, totalInterest, firstMonthInterest }
}

/**
 * Sum of `n` payments starting at `amount` and inflating at `i`, closed form geometric series:
 *
 *   S = A * ((1+i)^n - 1) / i          (and A*n when i = 0)
 */
export function inflatingSum(amount: number, inflationRate: number, years: number): number {
  if (years <= 0) return 0
  if (inflationRate === 0) return amount * years
  return (amount * (Math.pow(1 + inflationRate, years) - 1)) / inflationRate
}

/**
 * Retirement drawdown balances, simulated from the recurrence:
 *
 *   B_0 = P,   B_k = B_{k-1}(1 + r) - W_0(1 + i)^{k-1}
 *
 * The withdrawal taken during year k is the one set at the START of that year, which is why the
 * inflation exponent is k-1 and not k. Returns `[B_0, B_1, ...]` for `steps` steps.
 */
export function drawdownSeries(
  portfolio: number,
  initialWithdrawal: number,
  nominalReturn: number,
  inflationRate: number,
  steps: number,
): number[] {
  const series = [portfolio]
  let balance = portfolio
  for (let k = 1; k <= steps; k++) {
    balance = balance * (1 + nominalReturn) - initialWithdrawal * Math.pow(1 + inflationRate, k - 1)
    series.push(balance)
  }
  return series
}
