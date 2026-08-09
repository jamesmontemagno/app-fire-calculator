namespace MyFireNumber.Core.Calculations;

public enum DebtPayoffMode
{
    FixedBudget,
    TargetTimeline
}

public enum DebtPayoffStrategy
{
    Snowball,
    Avalanche
}

public sealed record DebtPayoffDraft(
    IReadOnlyList<DebtItem> Debts,
    double MonthlyBudget,
    double ExtraPayment,
    int TargetMonths,
    DebtPayoffMode Mode,
    DebtPayoffStrategy Strategy)
{
    public const int PayloadVersion = 1;

    public static DebtPayoffDraft Default { get; } = new(
        Debts: [],
        MonthlyBudget: 1_000,
        ExtraPayment: 0,
        TargetMonths: 36,
        Mode: DebtPayoffMode.FixedBudget,
        Strategy: DebtPayoffStrategy.Snowball);
}