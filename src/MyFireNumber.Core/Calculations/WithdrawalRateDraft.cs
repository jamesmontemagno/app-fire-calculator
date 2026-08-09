namespace MyFireNumber.Core.Calculations;

public sealed record WithdrawalRateDraft(
    double PortfolioValue,
    double WithdrawalRate,
    double ExpectedReturn,
    double InflationRate,
    int RetirementYears)
{
    public const int PayloadVersion = 1;

    public static WithdrawalRateDraft Default { get; } = new(
        PortfolioValue: 1_000_000,
        WithdrawalRate: 0.04,
        ExpectedReturn: 0.07,
        InflationRate: 0.03,
        RetirementYears: 30);
}