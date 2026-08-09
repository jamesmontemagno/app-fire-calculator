namespace MyFireNumber.Core.Calculations;

public sealed record CoastFireDraft(
    int CurrentAge,
    int RetirementAge,
    double CurrentSavings,
    double AnnualContribution,
    double ExpectedReturn,
    double InflationRate,
    double WithdrawalRate,
    double AnnualExpenses)
{
    public const int PayloadVersion = 1;

    public static CoastFireDraft Default { get; } = new(
        CurrentAge: 30,
        RetirementAge: 55,
        CurrentSavings: 100_000,
        AnnualContribution: 24_000,
        ExpectedReturn: 0.07,
        InflationRate: 0.03,
        WithdrawalRate: 0.04,
        AnnualExpenses: 48_000);

    public FireInputs ToFireInputs(int projectionStartYear = 0)
    {
        return new FireInputs(
            CurrentAge,
            RetirementAge,
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