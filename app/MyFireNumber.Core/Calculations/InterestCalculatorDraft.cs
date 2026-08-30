namespace MyFireNumber.Core.Calculations;

public sealed record InterestCalculatorDraft(
    double StartingBalance,
    double MonthlyContribution,
    double AnnualInterestRate,
    int Years)
{
    public const int PayloadVersion = 1;

    public static InterestCalculatorDraft Default { get; } = new(
        StartingBalance: 10_000,
        MonthlyContribution: 250,
        AnnualInterestRate: 0.05,
        Years: 10);

    public InterestCalculatorInputs ToInputs() =>
        new(StartingBalance, MonthlyContribution, AnnualInterestRate, Years);
}
