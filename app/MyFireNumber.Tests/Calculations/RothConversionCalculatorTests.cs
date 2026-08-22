using MyFireNumber.Core.Calculations;

namespace MyFireNumber.Tests.Calculations;

public sealed class RothConversionCalculatorTests
{
    private static readonly RothConversionInputs Inputs = new(
        CurrentAge: 45,
        StartYear: 2026,
        TraditionalBalance: 500_000,
        RothBalance: 50_000,
        AnnualConversion: 40_000,
        ConversionYears: 3,
        ExpectedReturn: 0,
        EstimatedTaxRate: 0.20);

    [Fact]
    public void Calculate_BuildsFiveTaxYearConversionLadder()
    {
        var result = RothConversionCalculator.Calculate(Inputs);

        Assert.Equal(120_000, result.TotalConverted);
        Assert.Equal(24_000, result.TotalEstimatedTaxes);
        Assert.Equal(2031, result.FirstAccessibleYear);
        Assert.Equal(380_000, result.EndingTraditionalBalance);
        Assert.Equal(170_000, result.EndingRothBalance);
        Assert.Equal(8, result.Projections.Count);
        Assert.Equal(40_000, result.Projections[5].NewlyAccessiblePrincipal);
        Assert.Equal(120_000, result.Projections[^1].CumulativeAccessiblePrincipal);
    }

    [Fact]
    public void Calculate_LimitsConversionToRemainingTraditionalBalance()
    {
        var result = RothConversionCalculator.Calculate(Inputs with
        {
            TraditionalBalance = 60_000,
            AnnualConversion = 40_000
        });

        Assert.Equal(60_000, result.TotalConverted);
        Assert.Equal(0, result.EndingTraditionalBalance);
        Assert.Equal(2, result.Projections.Count(point => point.Conversion > 0));
    }

    [Fact]
    public void Calculate_GrowsBalancesBeforeEachYearsConversion()
    {
        var result = RothConversionCalculator.Calculate(Inputs with
        {
            TraditionalBalance = 100_000,
            RothBalance = 0,
            AnnualConversion = 10_000,
            ConversionYears = 1,
            ExpectedReturn = 0.10
        });

        Assert.Equal(100_000, result.Projections[0].EndingTraditionalBalance);
        Assert.Equal(10_000, result.Projections[0].EndingRothBalance);
        Assert.Equal(161_051, result.EndingTraditionalBalance);
        Assert.Equal(16_105, result.EndingRothBalance);
    }

    [Fact]
    public void Calculate_RejectsInvalidRatesAndDurations()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RothConversionCalculator.Calculate(Inputs with { EstimatedTaxRate = 1.01 }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RothConversionCalculator.Calculate(Inputs with { ConversionYears = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RothConversionCalculator.Calculate(Inputs with { AnnualConversion = 0 }));
    }
}
