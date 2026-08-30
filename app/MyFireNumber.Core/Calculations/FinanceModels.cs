namespace MyFireNumber.Core.Calculations;

public sealed record FireInputs(
    double CurrentAge,
    double RetirementAge,
    double CurrentSavings,
    double AnnualContribution,
    double AnnualIncome,
    double ExpectedReturn,
    double InflationRate,
    double WithdrawalRate,
    double AnnualExpenses,
    int ProjectionStartYear = 0,
    ContributionGrowth ContributionGrowth = ContributionGrowth.Inflation);

/// <summary>
/// How contributions behave over time.
/// <para><see cref="Inflation"/>: the contribution keeps a constant purchasing power, so the nominal
/// amount paid at the end of year k is <c>annualContribution * (1 + inflationRate)^k</c>. This is the
/// model the closed-form solver assumes, and it is the app default.</para>
/// <para><see cref="Flat"/>: the same nominal amount is contributed every year, so its purchasing power
/// erodes.</para>
/// </summary>
public enum ContributionGrowth
{
    Inflation,
    Flat
}

public sealed record ProjectionPoint(
    double Age,
    int Year,
    double Portfolio,
    double Contributions,
    double TotalContributions,
    double InflationAdjusted);

public sealed record RetirementGoalAssessment(
    double TargetRetirementAge,
    double CalculatedFireAge)
{
    public static RetirementGoalAssessment Unavailable { get; } = new(double.NaN, double.NaN);

    // A positive gap means the calculated FIRE age is after the target age.
    public double TargetAgeGap => CalculatedFireAge - TargetRetirementAge;

    public bool IsOnTrack => double.IsFinite(CalculatedFireAge) && TargetAgeGap <= 0;

    public string Message => double.IsNaN(TargetRetirementAge) || double.IsNaN(CalculatedFireAge)
        ? "Retirement goal assessment is unavailable."
        : double.IsPositiveInfinity(CalculatedFireAge)
            ? "Off track: FIRE is not reachable with the current assumptions."
            : TargetAgeGap < 0
                ? $"On track: projected to reach FIRE {Math.Abs(TargetAgeGap):N1} years before your target retirement age."
                : TargetAgeGap > 0
                    ? $"Off track: projected to reach FIRE {TargetAgeGap:N1} years after your target retirement age."
                    : "On track: projected to reach FIRE at your target retirement age.";
}

public sealed record StandardFireResult(
    double FireNumber,
    double YearsToFire,
    double FireAge,
    IReadOnlyList<ProjectionPoint> Projections,
    double SavingsRate,
    double MonthlyContribution,
    double CoastFireNumber)
{
    public RetirementGoalAssessment RetirementGoal { get; init; } = RetirementGoalAssessment.Unavailable;
}

public sealed record CoastFireResult(
    double CoastNumber,
    double YearsToCoast,
    bool AlreadyCoasting,
    double FireNumber,
    IReadOnlyList<ProjectionPoint> Projections,
    IReadOnlyList<ProjectionPoint> ProjectionsWithContributions);

public sealed record LeanFireResult(StandardFireResult Standard, bool IsLean, double LeanThreshold)
{
    public RetirementGoalAssessment RetirementGoal => Standard.RetirementGoal;
}

public sealed record FatFireResult(StandardFireResult Standard, bool IsFat, double FatThreshold)
{
    public RetirementGoalAssessment RetirementGoal => Standard.RetirementGoal;
}

public sealed record BaristaFireResult(
    double BaristaNumber,
    double FullFireNumber,
    double YearsToBaristaFire,
    double PartTimeIncomeNeeded,
    IReadOnlyList<ProjectionPoint> Projections,
    double SavingsFromPartTime);

public sealed record WithdrawalProjection(double Year, double Balance, double Withdrawal);

public sealed record WithdrawalRateAnalysis(double Rate, double Years, double EndBalance);

public sealed record WithdrawalResult(
    double PortfolioLongevity,
    // Share of the retirement horizon funded by this single deterministic projection
    // (PortfolioLongevity / RetirementYears, capped at 1). Not a probability of success.
    double HorizonFundedRatio,
    double AnnualWithdrawal,
    double MonthlyWithdrawal,
    double EndingBalance,
    IReadOnlyList<WithdrawalProjection> WithdrawalProjections,
    IReadOnlyList<WithdrawalRateAnalysis> RateAnalysis);

public sealed record DebtItem(
    string Id,
    string Name,
    double Balance,
    double Rate,
    double MinimumPayment,
    double ExtraMonthlyPayment = 0);

public sealed record DebtBalance(string Name, double Balance);

public sealed record DebtPayoffMonth(
    int Month,
    double TotalBalance,
    double PrincipalPaid,
    double InterestPaid,
    double CumulativePrincipal,
    double CumulativeInterest,
    IReadOnlyList<string> DebtsPaidOff,
    IReadOnlyList<DebtBalance> DebtsRemaining);

public sealed record DebtMilestone(int Month, string DebtName);

public sealed record DebtPayoffResult(
    int TotalMonths,
    double TotalInterest,
    double TotalPrincipal,
    double MonthlyPayment,
    IReadOnlyList<DebtPayoffMonth> Projections,
    IReadOnlyList<string> PayoffOrder,
    IReadOnlyList<DebtMilestone> DebtMilestones);

public sealed record DebtTimelineResult(double RequiredPayment, DebtPayoffResult Result);

public sealed record ReverseFireResult(
    double FireNumber,
    double YearsToFire,
    double RequiredAnnualSavings,
    double RequiredMonthlySavings,
    IReadOnlyList<ProjectionPoint> Projections,
    bool AlreadyAchievable,
    double CurrentWillGrowTo);

public enum ContributionFrequency
{
    Monthly,
    Yearly
}

public sealed record InvestmentGrowthInputs(
    double StartingAmount,
    double ContributionAmount,
    ContributionFrequency ContributionFrequency,
    int YearsInvesting,
    double ExpectedReturn,
    double InflationRate,
    double AnnualIncome,
    double CurrentAge = 30,
    int ProjectionStartYear = 0,
    ContributionGrowth ContributionGrowth = ContributionGrowth.Inflation);

public sealed record InvestmentProjectionPoint(
    double Age,
    int Year,
    int YearNumber,
    double Portfolio,
    double InflationAdjusted,
    double TotalContributions,
    double Contributions);

public sealed record InvestmentGrowthResult(
    double SavingsRate,
    double AnnualContribution,
    double MonthlyContribution,
    double FinalNominalBalance,
    double FinalInflationAdjustedBalance,
    double TotalInvested,
    double TotalGrowth,
    double InflationImpact,
    IReadOnlyList<InvestmentProjectionPoint> Projections,
    string SavingsCategory);

public sealed record InterestCalculatorInputs(
    double StartingBalance,
    double MonthlyContribution,
    double AnnualInterestRate,
    int Years);

public sealed record InterestProjectionPoint(
    int Year,
    double Balance,
    double TotalContributions,
    double InterestEarned);

public sealed record InterestCalculatorResult(
    double EndingBalance,
    double TotalContributions,
    double InterestEarned,
    double EffectiveAnnualYield,
    IReadOnlyList<InterestProjectionPoint> Projections);

public sealed record HealthcareGapInputs(
    int CurrentAge,
    int EarlyRetirementAge,
    int MedicareAge,
    double MonthlyPremium,
    double AnnualDeductible,
    double AnnualOutOfPocket,
    double InflationRate,
    int ProjectionStartYear = 0);

public sealed record HealthcareYear(
    int Age,
    int Year,
    double Cost,
    double Premium,
    double Deductible,
    double OutOfPocket);

public sealed record HealthcareGapResult(
    int GapYears,
    double AnnualCost,
    double TotalCost,
    double AverageAnnualCost,
    IReadOnlyList<HealthcareYear> YearlyBreakdown);

public enum RetirementAccountType
{
    Deferred,
    Traditional,
    Roth,
    Taxable,
    Savings,
    Hsa,
    Other
}

public static class RetirementTaxDefaults
{
    /// <summary>
    /// The flat rate this app already assumes for ordinary income. It matches the default tax rate
    /// used for retirement income sources so the two sides of the ledger share one assumption.
    /// </summary>
    public const double OrdinaryIncomeTaxRate = 0.25;

    /// <summary>
    /// Withdrawals from tax-deferred accounts are ordinary income. Roth and HSA withdrawals are
    /// genuinely tax-free. Taxable and savings accounts default to zero because only the gain or
    /// interest portion is taxable and this model tracks no cost basis, so taxing the full
    /// withdrawal would overstate it.
    /// </summary>
    public static double WithdrawalTaxRateFor(RetirementAccountType type) =>
        type is RetirementAccountType.Deferred or RetirementAccountType.Traditional
            ? OrdinaryIncomeTaxRate
            : 0;
}

public sealed record RetirementAccount(
    string Id,
    string Name,
    RetirementAccountType Type,
    double Balance,
    double AnnualContribution,
    double AnnualReturn,
    int AvailableAge,
    double WithdrawalRate,
    int PayoutYears,
    double? WithdrawalTaxRate = null)
{
    /// <summary>
    /// Drafts saved before withdrawal tax existed deserialize with no rate, so they resolve to the
    /// type-driven default rather than silently becoming tax-free.
    /// </summary>
    public double EffectiveWithdrawalTaxRate =>
        Math.Clamp(WithdrawalTaxRate ?? RetirementTaxDefaults.WithdrawalTaxRateFor(Type), 0, 1);
}

public sealed record RetirementIncomeSource(
    string Id,
    string Name,
    double AnnualAmount,
    int StartAge,
    int EndAge,
    double AnnualGrowth,
    bool IsAfterTax,
    double TaxRate);

public sealed record RetirementExpense(
    string Id,
    string Name,
    double AnnualAmount,
    int StartAge,
    int EndAge);

public sealed record DeferredCompensationInputs(
    int CurrentAge,
    int SemiRetirementAge,
    int PlanThroughAge,
    double AnnualExpenses,
    double InflationRate,
    IReadOnlyList<RetirementAccount> Accounts,
    IReadOnlyList<RetirementIncomeSource> IncomeSources,
    IReadOnlyList<RetirementExpense> AdditionalExpenses,
    bool WithdrawOnlyAfterRetirement,
    bool ReinvestSurplus,
    int CurrentYear = 0);

/// <summary>
/// One projected retirement year. Currency fields are rounded to whole dollars for display —
/// <c>Surplus</c> away from zero, everything else clamped at zero first.
///
/// <para><c>Surplus</c> is a presentation value and must not be used to decide whether a year is
/// short. <see cref="DeferredCompensationCalculator"/> keeps that verdict on the unrounded surplus;
/// see issue #63, where classifying from the rounded field made web and MAUI give opposite answers to
/// "is my plan funded" for the same inputs.</para>
///
/// <para><c>PolicyExcessWithdrawals</c> is the gross withdrawn *beyond* the per-account
/// withdrawal-rate limits to keep the year funded. Zero means the stated rates already covered the
/// year; non-zero means the plan only stays funded by spending faster than the policy the user
/// entered. See issue #56.</para>
/// </summary>
public sealed record RetirementCashFlowPoint(
    int Age,
    int Year,
    double TotalBalance,
    double OutsideIncome,
    double DeferredIncome,
    double PortfolioWithdrawals,
    double TotalIncome,
    double Expenses,
    double Surplus,
    IReadOnlyDictionary<string, double> Withdrawals,
    IReadOnlyDictionary<string, double> Balances,
    IReadOnlyDictionary<string, double> IncomeBySource,
    double CoreExpenses,
    double AdditionalExpenses,
    IReadOnlyDictionary<string, double> ExpensesByItem,
    double WithdrawalTaxes,
    double PolicyExcessWithdrawals);

public sealed record DeferredCompensationResult(
    IReadOnlyList<RetirementCashFlowPoint> Projections,
    double CurrentBalance,
    double BalanceAtSemiRetirement,
    double FirstYearIncome,
    double FirstYearSurplus,
    double EndingBalance,
    int FundedYears,
    int YearsFullyCovered,
    int? FirstShortfallAge,
    int RetirementYears);