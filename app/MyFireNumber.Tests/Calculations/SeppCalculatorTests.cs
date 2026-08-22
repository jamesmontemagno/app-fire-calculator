using MyFireNumber.Core.Calculations;

namespace MyFireNumber.Tests.Calculations;

public sealed class SeppCalculatorTests
{
    private static readonly SeppInputs Inputs = new(
        AccountBalance: 500_000,
        ExpectedReturn: 0.05,
        BirthDate: new DateOnly(1976, 8, 22),
        FirstPaymentDate: new DateOnly(2026, 8, 22),
        InterestRate: 0.05,
        MaximumInterestRate: 0.0522,
        AnnuityFactor: 16.2,
        Method: SeppMethod.FixedAmortization);

    [Theory]
    [InlineData(18, 67.0)]
    [InlineData(50, 36.2)]
    [InlineData(59, 28.0)]
    public void SingleLifeFactor_UsesPost2022IrsTable(int age, double expected)
    {
        Assert.Equal(expected, SeppCalculator.SingleLifeFactor(age));
    }

    [Fact]
    public void MaximumRate_UsesGreaterOfFivePercentOrOneHundredTwentyPercentAfr()
    {
        Assert.Equal(0.05, SeppCalculator.MaximumPermittedInterestRate(0.03), 10);
        Assert.Equal(0.0522, SeppCalculator.MaximumPermittedInterestRate(0.0435), 10);
    }

    [Fact]
    public void Calculate_ComparesAllThreeMethods()
    {
        var result = SeppCalculator.Calculate(Inputs);

        Assert.Equal(50, result.StartingAge);
        Assert.Equal(36.2, result.LifeExpectancyFactor);
        Assert.Equal(new DateOnly(2036, 2, 22), result.RequiredEndDate);
        Assert.Equal(10, result.RequiredYears);
        Assert.Equal(13_812, result.Rmd.AnnualPayment);
        Assert.Equal(30_156, result.Amortization.AnnualPayment);
        Assert.Equal(30_864, result.Annuitization.AnnualPayment);
        Assert.Equal(10, result.Amortization.Projections.Count);
    }

    [Fact]
    public void Calculate_UsesFiveYearsWhenParticipantIsAlreadyNearFiftyNineAndAHalf()
    {
        var result = SeppCalculator.Calculate(Inputs with
        {
            BirthDate = new DateOnly(1967, 10, 1)
        });

        Assert.Equal(new DateOnly(2031, 8, 22), result.RequiredEndDate);
        Assert.Equal(5, result.RequiredYears);
        Assert.Equal(5, result.Rmd.Projections[^1].YearNumber);
        Assert.Equal(62, result.Rmd.Projections[^1].Age);
    }

    [Fact]
    public void Calculate_RejectsInterestRateAboveUserSuppliedIrsLimit()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SeppCalculator.Calculate(Inputs with { InterestRate = 0.0523 }));
    }

    [Fact]
    public void Calculate_AllowsMissingAnnuityFactorWhenAnotherMethodIsSelected()
    {
        var result = SeppCalculator.Calculate(Inputs with { AnnuityFactor = null });

        Assert.Null(result.Annuitization.AnnualPayment);
        Assert.Empty(result.Annuitization.Projections);
    }

    [Fact]
    public void Calculate_RequiresAnnuityFactorWhenAnnuitizationIsSelected()
    {
        Assert.Throws<ArgumentException>(() =>
            SeppCalculator.Calculate(Inputs with
            {
                AnnuityFactor = null,
                Method = SeppMethod.FixedAnnuitization
            }));
    }
}
