namespace MyFireNumber.Core.Calculations;

public sealed record HealthcareGapDraft(
    int CurrentAge,
    int EarlyRetirementAge,
    int MedicareAge,
    double MonthlyPremium,
    double AnnualDeductible,
    double AnnualOutOfPocket,
    double InflationRate)
{
    public const int PayloadVersion = 1;

    public static HealthcareGapDraft Default { get; } = new(
        CurrentAge: 30,
        EarlyRetirementAge: 55,
        MedicareAge: 65,
        MonthlyPremium: 600,
        AnnualDeductible: 2_500,
        AnnualOutOfPocket: 2_000,
        InflationRate: 0.03);

    public HealthcareGapInputs ToInputs(int projectionStartYear = 0)
    {
        return new HealthcareGapInputs(
            CurrentAge,
            EarlyRetirementAge,
            MedicareAge,
            MonthlyPremium,
            AnnualDeductible,
            AnnualOutOfPocket,
            InflationRate,
            projectionStartYear);
    }
}