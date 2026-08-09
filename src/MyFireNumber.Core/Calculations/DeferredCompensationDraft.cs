namespace MyFireNumber.Core.Calculations;

public sealed record DeferredCompensationDraft(
    int CurrentAge,
    int SemiRetirementAge,
    int PlanThroughAge,
    double AnnualExpenses,
    double InflationRate,
    IReadOnlyList<RetirementAccount> Accounts,
    IReadOnlyList<RetirementIncomeSource> IncomeSources,
    IReadOnlyList<RetirementExpense> AdditionalExpenses,
    bool WithdrawOnlyAfterRetirement,
    bool ReinvestSurplus)
{
    public const int PayloadVersion = 1;

    public static DeferredCompensationDraft Default { get; } = new(
        CurrentAge: 45,
        SemiRetirementAge: 55,
        PlanThroughAge: 90,
        AnnualExpenses: 80_000,
        InflationRate: 0.03,
        Accounts:
        [
            new RetirementAccount("deferred-comp", "Deferred Compensation", RetirementAccountType.Deferred, 300_000, 0, 0.05, 55, 0, 5),
            new RetirementAccount("401k", "401(k)", RetirementAccountType.Traditional, 500_000, 23_500, 0.07, 60, 0.04, 1)
        ],
        IncomeSources:
        [
            new RetirementIncomeSource("part-time-income", "Part-time income", 20_000, 55, 65, 0, true, 0.25)
        ],
        AdditionalExpenses: [],
        WithdrawOnlyAfterRetirement: true,
        ReinvestSurplus: true);

    public DeferredCompensationInputs ToInputs(int currentYear = 0) => new(
        CurrentAge,
        SemiRetirementAge,
        PlanThroughAge,
        AnnualExpenses,
        InflationRate,
        Accounts,
        IncomeSources,
        AdditionalExpenses,
        WithdrawOnlyAfterRetirement,
        ReinvestSurplus,
        currentYear);
}