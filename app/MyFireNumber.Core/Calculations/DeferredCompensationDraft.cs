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
        Accounts: [],
        IncomeSources: [],
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