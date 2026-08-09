namespace MyFireNumber.Core.Calculations;

public sealed record BaristaFireDraft(
    int CurrentAge,
    double CurrentSavings,
    double AnnualContribution,
    double ExpectedReturn,
    double InflationRate,
    double WithdrawalRate,
    double AnnualExpenses,
    double PartTimeAnnualIncome)
{
    public const int PayloadVersion = 1;

    public static BaristaFireDraft Default { get; } = new(
        CurrentAge: 30,
        CurrentSavings: 100_000,
        AnnualContribution: 24_000,
        ExpectedReturn: 0.07,
        InflationRate: 0.03,
        WithdrawalRate: 0.04,
        AnnualExpenses: 48_000,
        PartTimeAnnualIncome: 20_000);

    public FireInputs ToFireInputs(int projectionStartYear = 0)
    {
        return new FireInputs(
            CurrentAge,
            RetirementAge: CurrentAge,
            CurrentSavings,
            AnnualContribution,
            AnnualIncome: 0,
            ExpectedReturn,
            InflationRate,
            WithdrawalRate,
            AnnualExpenses,
            projectionStartYear);
    }
}