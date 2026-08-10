using CommunityToolkit.Mvvm.ComponentModel;
using MyFireNumber.Core.Calculations;
using MyFireNumber.Services;
using SkiaSharp;
using System.Globalization;

namespace MyFireNumber.ViewModels;

public sealed partial class HealthcareGapViewModel : CalculatorViewModelBase<HealthcareGapDraft>
{
    private readonly IHealthcareGapExportService exportService;

    public HealthcareGapViewModel(
        CalculatorViewModelServices services,
        IHealthcareGapExportService exportService)
        : base(services)
    {
        this.exportService = exportService;
    }

    [ObservableProperty]
    private string healthcareCurrentAgeText = string.Empty;

    [ObservableProperty]
    private string earlyRetirementAgeText = string.Empty;

    [ObservableProperty]
    private string medicareAgeText = string.Empty;

    [ObservableProperty]
    private string monthlyPremiumText = string.Empty;

    [ObservableProperty]
    private string annualDeductibleText = string.Empty;

    [ObservableProperty]
    private string annualOutOfPocketText = string.Empty;

    [ObservableProperty]
    private string inflationRateText = string.Empty;

    [ObservableProperty]
    private string healthcareGapYearsText = string.Empty;

    [ObservableProperty]
    private string healthcareAnnualCostText = string.Empty;

    [ObservableProperty]
    private string healthcareTotalCostText = string.Empty;

    [ObservableProperty]
    private string healthcareAverageAnnualCostText = string.Empty;

    [ObservableProperty]
    private string healthcareSubsidyEstimateText = string.Empty;

    [ObservableProperty]
    private string healthcareProjectionSummary = string.Empty;

    protected override string CalculatorId => "healthcare-gap";

    protected override int DraftPayloadVersion => HealthcareGapDraft.PayloadVersion;

    protected override HealthcareGapDraft DefaultDraft => CalculatorDefaults.HealthcareGap;

    protected override string DefaultPlanName => "My Healthcare Gap Plan";

    protected override string ExportSuccessMessage => "Your Healthcare Gap workbook is ready to share.";

    protected override string ExportFailureMessage => "The Healthcare Gap workbook could not be created locally.";

    partial void OnHealthcareCurrentAgeTextChanged(string value) => OnDraftInputChanged();
    partial void OnEarlyRetirementAgeTextChanged(string value) => OnDraftInputChanged();
    partial void OnMedicareAgeTextChanged(string value) => OnDraftInputChanged();
    partial void OnMonthlyPremiumTextChanged(string value) => OnDraftInputChanged();
    partial void OnAnnualDeductibleTextChanged(string value) => OnDraftInputChanged();
    partial void OnAnnualOutOfPocketTextChanged(string value) => OnDraftInputChanged();
    partial void OnInflationRateTextChanged(string value) => OnDraftInputChanged();

    protected override void ApplyDraft(HealthcareGapDraft draft)
    {
        HealthcareCurrentAgeText = draft.CurrentAge.ToString(CultureInfo.CurrentCulture);
        EarlyRetirementAgeText = draft.EarlyRetirementAge.ToString(CultureInfo.CurrentCulture);
        MedicareAgeText = draft.MedicareAge.ToString(CultureInfo.CurrentCulture);
        MonthlyPremiumText = FormatNumber(draft.MonthlyPremium);
        AnnualDeductibleText = FormatNumber(draft.AnnualDeductible);
        AnnualOutOfPocketText = FormatNumber(draft.AnnualOutOfPocket);
        InflationRateText = FormatNumber(draft.InflationRate * 100);
    }

    protected override bool TryBuildDraft(out HealthcareGapDraft draft)
    {
        draft = DefaultDraft;
        if (!TryParseWholeNumber(HealthcareCurrentAgeText, out var currentAge) || currentAge is < 18 or > 100)
        {
            ValidationMessage = "Enter a current age from 18 to 100.";
            return false;
        }

        if (!TryParseWholeNumber(EarlyRetirementAgeText, out var earlyRetirementAge)
            || earlyRetirementAge < currentAge
            || earlyRetirementAge > 100)
        {
            ValidationMessage = "Enter a retirement age from your current age through 100.";
            return false;
        }

        if (!TryParseWholeNumber(MedicareAgeText, out var medicareAge)
            || medicareAge < earlyRetirementAge
            || medicareAge > 100)
        {
            ValidationMessage = "Enter a Medicare age from retirement age through 100.";
            return false;
        }

        if (!TryParseNonNegative(MonthlyPremiumText, out var monthlyPremium)
            || !TryParseNonNegative(AnnualDeductibleText, out var annualDeductible)
            || !TryParseNonNegative(AnnualOutOfPocketText, out var annualOutOfPocket))
        {
            ValidationMessage = "Enter zero or a positive amount for each healthcare cost.";
            return false;
        }

        if (!TryParsePercentage(InflationRateText, 0, 10, out var inflationRate))
        {
            ValidationMessage = "Enter an inflation rate from 0% to 10%.";
            return false;
        }

        draft = new HealthcareGapDraft(currentAge, earlyRetirementAge, medicareAge, monthlyPremium, annualDeductible, annualOutOfPocket, inflationRate);
        return true;
    }

    protected override void Recalculate(HealthcareGapDraft draft)
    {
        var result = FinancialCalculator.CalculateHealthcareGap(draft.ToInputs());
        HealthcareGapYearsText = $"{result.GapYears} years";
        HealthcareAnnualCostText = FormatCurrency(result.AnnualCost);
        HealthcareTotalCostText = FormatCurrency(result.TotalCost);
        HealthcareAverageAnnualCostText = FormatCurrency(result.AverageAnnualCost);
        HealthcareSubsidyEstimateText = $"$30k income: {FormatCurrency(result.EstimatedSubsidy30k)}  |  $50k: {FormatCurrency(result.EstimatedSubsidy50k)}  |  $75k: {FormatCurrency(result.EstimatedSubsidy75k)}";
        HealthcareProjectionSummary = result.GapYears == 0
            ? "Your retirement age is at or beyond Medicare eligibility, so no pre-Medicare coverage gap is projected."
            : $"From age {draft.EarlyRetirementAge} to {draft.MedicareAge}, estimated healthcare costs total {FormatCurrency(result.TotalCost)} before Medicare eligibility.";
        UpdateProjectionChart(result);
    }

    protected override async Task ShareAsync(HealthcareGapDraft draft)
    {
        await exportService.ShareAsync(draft, FinancialCalculator.CalculateHealthcareGap(draft.ToInputs()));
    }

    private void UpdateProjectionChart(HealthcareGapResult result)
    {
        ProjectionSeries =
        [
            CreateProjectionSeries("Total cost", result.YearlyBreakdown.Select(year => year.Cost), new SKColor(190, 81, 66)),
            CreateProjectionSeries("Premium", result.YearlyBreakdown.Select(year => year.Premium), new SKColor(72, 93, 165)),
            CreateProjectionSeries("Deductible and out-of-pocket", result.YearlyBreakdown.Select(year => year.Deductible + year.OutOfPocket), new SKColor(201, 119, 39))
        ];
        ProjectionXAxes =
        [
            CreateLabelledAxis("Age", result.YearlyBreakdown.Select(year => year.Age.ToString("0", CultureInfo.CurrentCulture)))
        ];
        ProjectionChartDescription = result.GapYears == 0
            ? "No pre-Medicare healthcare coverage gap is projected."
            : $"Pre-Medicare healthcare costs from age {result.YearlyBreakdown[0].Age:0} through age {result.YearlyBreakdown[^1].Age:0}. Total estimated cost is {FormatCurrency(result.TotalCost)}.";
    }
}
