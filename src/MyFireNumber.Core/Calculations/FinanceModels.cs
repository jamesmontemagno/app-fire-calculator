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
    int ProjectionStartYear = 0);

public sealed record ProjectionPoint(
    double Age,
    int Year,
    double Portfolio,
    double Contributions,
    double TotalContributions,
    double InflationAdjusted);

public sealed record StandardFireResult(
    double FireNumber,
    double YearsToFire,
    double FireAge,
    IReadOnlyList<ProjectionPoint> Projections,
    double SavingsRate,
    double MonthlyContribution,
    double CoastFireNumber);

public sealed record CoastFireResult(
    double CoastNumber,
    double YearsToCoast,
    bool AlreadyCoasting,
    double FireNumber,
    IReadOnlyList<ProjectionPoint> Projections,
    IReadOnlyList<ProjectionPoint> ProjectionsWithContributions);

public sealed record LeanFireResult(StandardFireResult Standard, bool IsLean, double LeanThreshold);

public sealed record FatFireResult(StandardFireResult Standard, bool IsFat, double FatThreshold);

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
    double SuccessRate,
    double AnnualWithdrawal,
    double MonthlyWithdrawal,
    double EndingBalance,
    IReadOnlyList<WithdrawalProjection> WithdrawalProjections,
    IReadOnlyList<WithdrawalRateAnalysis> RateAnalysis);

public sealed record DebtItem(string Id, string Name, double Balance, double Rate, double MinimumPayment);

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
    int ProjectionStartYear = 0);

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
    IReadOnlyList<HealthcareYear> YearlyBreakdown,
    double EstimatedSubsidy30k,
    double EstimatedSubsidy50k,
    double EstimatedSubsidy75k);

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

public sealed record RetirementAccount(
    string Id,
    string Name,
    RetirementAccountType Type,
    double Balance,
    double AnnualContribution,
    double AnnualReturn,
    int AvailableAge,
    double WithdrawalRate,
    int PayoutYears);

public sealed record RetirementIncomeSource(
    string Id,
    string Name,
    double AnnualAmount,
    int StartAge,
    int EndAge,
    double AnnualGrowth,
    bool IsAfterTax,
    double TaxRate);

public sealed record RetirementExpense(string Id, string Name, double AnnualAmount, int StartAge);

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
    IReadOnlyDictionary<string, double> ExpensesByItem);

public sealed record DeferredCompensationResult(
    IReadOnlyList<RetirementCashFlowPoint> Projections,
    double CurrentBalance,
    double BalanceAtSemiRetirement,
    double FirstYearIncome,
    double FirstYearSurplus,
    double EndingBalance,
    int FundedYears);