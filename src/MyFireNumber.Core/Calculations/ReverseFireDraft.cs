namespace MyFireNumber.Core.Calculations;

public sealed record ReverseFireDraft(
    int CurrentAge,
    int TargetRetirementAge,
    double CurrentSavings,
    double ExpectedReturn,
    double InflationRate,
    double WithdrawalRate,
    double AnnualExpenses)
{
    public const int PayloadVersion = 1;

    public static ReverseFireDraft Default { get; } = new(
        CurrentAge: 30,
        TargetRetirementAge: 55,
        CurrentSavings: 100_000,
        ExpectedReturn: 0.07,
        InflationRate: 0.03,
        WithdrawalRate: 0.04,
        AnnualExpenses: 48_000);

    public FireInputs ToFireInputs(int projectionStartYear = 0)
    {
        return new FireInputs(
            CurrentAge,
            TargetRetirementAge,
            CurrentSavings,
            AnnualContribution: 0,
            AnnualIncome: 0,
            ExpectedReturn,
            InflationRate,
            WithdrawalRate,
            AnnualExpenses,
            projectionStartYear);
    }
}