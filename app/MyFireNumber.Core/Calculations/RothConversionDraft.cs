namespace MyFireNumber.Core.Calculations;

public sealed record RothConversionDraft(
    int CurrentAge,
    int StartYear,
    double TraditionalBalance,
    double RothBalance,
    double AnnualConversion,
    int ConversionYears,
    double ExpectedReturn,
    double EstimatedTaxRate)
{
    public const int PayloadVersion = 1;

    public static RothConversionDraft Default
    {
        get
        {
            var today = DateTime.Today;
            return new(
                CurrentAge: 45,
                StartYear: today.Year,
                TraditionalBalance: 750_000,
                RothBalance: 100_000,
                AnnualConversion: 50_000,
                ConversionYears: 10,
                ExpectedReturn: 0.05,
                EstimatedTaxRate: 0.22);
        }
    }

    public RothConversionInputs ToInputs() => new(
        CurrentAge,
        StartYear,
        TraditionalBalance,
        RothBalance,
        AnnualConversion,
        ConversionYears,
        ExpectedReturn,
        EstimatedTaxRate);
}
