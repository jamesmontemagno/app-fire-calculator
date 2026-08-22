namespace MyFireNumber.Core.Calculations;

public sealed record RothConversionInputs(
    int CurrentAge,
    int StartYear,
    double TraditionalBalance,
    double RothBalance,
    double AnnualConversion,
    int ConversionYears,
    double ExpectedReturn,
    double EstimatedTaxRate);

public sealed record RothConversionProjectionPoint(
    int YearNumber,
    int CalendarYear,
    int Age,
    double StartingTraditionalBalance,
    double Conversion,
    double EstimatedTaxes,
    double EndingTraditionalBalance,
    double EndingRothBalance,
    double NewlyAccessiblePrincipal,
    double CumulativeAccessiblePrincipal);

public sealed record RothConversionResult(
    double TotalConverted,
    double TotalEstimatedTaxes,
    int? FirstAccessibleYear,
    double EndingTraditionalBalance,
    double EndingRothBalance,
    IReadOnlyList<RothConversionProjectionPoint> Projections);

public static class RothConversionCalculator
{
    public const int ConversionWaitingPeriodYears = 5;

    public static RothConversionResult Calculate(RothConversionInputs inputs)
    {
        Validate(inputs);

        var traditionalBalance = inputs.TraditionalBalance;
        var rothBalance = inputs.RothBalance;
        var convertedByYear = new Dictionary<int, double>();
        var projections = new List<RothConversionProjectionPoint>(
            inputs.ConversionYears + ConversionWaitingPeriodYears);
        var totalConverted = 0d;
        var totalTaxes = 0d;
        var accessiblePrincipal = 0d;
        int? firstAccessibleYear = null;

        for (var index = 0; index < inputs.ConversionYears + ConversionWaitingPeriodYears; index++)
        {
            var calendarYear = inputs.StartYear + index;
            var startingTraditionalBalance = traditionalBalance;
            traditionalBalance *= 1 + inputs.ExpectedReturn;
            rothBalance *= 1 + inputs.ExpectedReturn;

            var conversion = index < inputs.ConversionYears
                ? Math.Min(inputs.AnnualConversion, traditionalBalance)
                : 0;
            traditionalBalance -= conversion;
            rothBalance += conversion;

            if (conversion > 0)
            {
                convertedByYear[calendarYear] = conversion;
                totalConverted += conversion;
                totalTaxes += conversion * inputs.EstimatedTaxRate;
            }

            var sourceYear = calendarYear - ConversionWaitingPeriodYears;
            var newlyAccessible = convertedByYear.GetValueOrDefault(sourceYear);
            accessiblePrincipal += newlyAccessible;
            if (newlyAccessible > 0 && firstAccessibleYear is null)
            {
                firstAccessibleYear = calendarYear;
            }

            projections.Add(new(
                index + 1,
                calendarYear,
                inputs.CurrentAge + index,
                Round(startingTraditionalBalance),
                Round(conversion),
                Round(conversion * inputs.EstimatedTaxRate),
                Round(traditionalBalance),
                Round(rothBalance),
                Round(newlyAccessible),
                Round(accessiblePrincipal)));
        }

        return new(
            Round(totalConverted),
            Round(totalTaxes),
            firstAccessibleYear,
            Round(traditionalBalance),
            Round(rothBalance),
            projections);
    }

    private static void Validate(RothConversionInputs inputs)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(inputs.CurrentAge);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(inputs.CurrentAge, 120);
        ArgumentOutOfRangeException.ThrowIfLessThan(inputs.StartYear, 1900);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(inputs.StartYear, 2200);
        ArgumentOutOfRangeException.ThrowIfNegative(inputs.TraditionalBalance);
        ArgumentOutOfRangeException.ThrowIfNegative(inputs.RothBalance);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(inputs.AnnualConversion);
        ArgumentOutOfRangeException.ThrowIfLessThan(inputs.ConversionYears, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(inputs.ConversionYears, 50);
        ArgumentOutOfRangeException.ThrowIfLessThan(inputs.ExpectedReturn, -1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(inputs.ExpectedReturn, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(inputs.EstimatedTaxRate);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(inputs.EstimatedTaxRate, 1);
    }

    private static double Round(double value) =>
        Math.Round(Math.Max(0, value), MidpointRounding.AwayFromZero);
}
