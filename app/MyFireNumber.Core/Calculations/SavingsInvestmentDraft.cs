namespace MyFireNumber.Core.Calculations;

public sealed record SavingsInvestmentDraft(
    double StartingAmount,
    double ContributionAmount,
    ContributionFrequency ContributionFrequency,
    int YearsInvesting,
    double ExpectedReturn,
    double InflationRate,
    double AnnualIncome,
    int CurrentAge)
{
    public const int PayloadVersion = 1;

    public static SavingsInvestmentDraft Default { get; } = new(
        StartingAmount: 100_000,
        ContributionAmount: 500,
        ContributionFrequency: ContributionFrequency.Monthly,
        YearsInvesting: 30,
        ExpectedReturn: 0.07,
        InflationRate: 0.03,
        AnnualIncome: 75_000,
        CurrentAge: 30);

    public InvestmentGrowthInputs ToInputs(int projectionStartYear = 0)
    {
        return new InvestmentGrowthInputs(
            StartingAmount,
            ContributionAmount,
            ContributionFrequency,
            YearsInvesting,
            ExpectedReturn,
            InflationRate,
            AnnualIncome,
            CurrentAge,
            projectionStartYear);
    }
}