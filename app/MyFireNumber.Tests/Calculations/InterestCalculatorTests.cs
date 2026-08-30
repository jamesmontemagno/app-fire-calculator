using MyFireNumber.Core.Calculations;

namespace MyFireNumber.Tests.Calculations;

public sealed class InterestCalculatorTests
{
    [Fact]
    public void CalculateInterest_CompoundsMonthlyAndAddsContributionsAtMonthEnd()
    {
        var result = FinancialCalculator.CalculateInterest(
            new InterestCalculatorInputs(10_000, 250, 0.05, 10));

        Assert.Equal(55_291, result.EndingBalance, 2);
        Assert.Equal(40_000, result.TotalContributions);
        Assert.Equal(15_291, result.InterestEarned, 2);
        Assert.Equal(0.051162, result.EffectiveAnnualYield, 6);
        Assert.Equal(11, result.Projections.Count);
    }

    [Fact]
    public void CalculateInterest_AtZeroRate_ReturnsContributionsOnly()
    {
        var result = FinancialCalculator.CalculateInterest(
            new InterestCalculatorInputs(1_000, 100, 0, 2));

        Assert.Equal(3_400, result.EndingBalance);
        Assert.Equal(0, result.InterestEarned);
        Assert.Equal(0, result.EffectiveAnnualYield);
    }
}
