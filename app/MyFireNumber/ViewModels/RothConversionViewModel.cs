using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using MyFireNumber.Core.Calculations;
using MyFireNumber.Services;
using SkiaSharp;

namespace MyFireNumber.ViewModels;

public sealed partial class RothConversionViewModel : CalculatorViewModelBase<RothConversionDraft>
{
    private readonly IRothConversionExportService exportService;

    public RothConversionViewModel(
        CalculatorViewModelServices services,
        IRothConversionExportService exportService)
        : base(services)
    {
        this.exportService = exportService;
    }

    [ObservableProperty] private string currentAgeText = string.Empty;
    [ObservableProperty] private string startYearText = string.Empty;
    [ObservableProperty] private string traditionalBalanceText = string.Empty;
    [ObservableProperty] private string rothBalanceText = string.Empty;
    [ObservableProperty] private string annualConversionText = string.Empty;
    [ObservableProperty] private string conversionYearsText = string.Empty;
    [ObservableProperty] private string expectedReturnText = string.Empty;
    [ObservableProperty] private string estimatedTaxRateText = string.Empty;

    [ObservableProperty] private string totalConvertedText = string.Empty;
    [ObservableProperty] private string totalTaxesText = string.Empty;
    [ObservableProperty] private string firstAccessibleYearText = string.Empty;
    [ObservableProperty] private string endingTraditionalBalanceText = string.Empty;
    [ObservableProperty] private string endingRothBalanceText = string.Empty;
    [ObservableProperty] private string projectionSummary = string.Empty;

    protected override string CalculatorId => "roth-conversion";
    protected override int DraftPayloadVersion => RothConversionDraft.PayloadVersion;
    protected override RothConversionDraft DefaultDraft => CalculatorDefaults.RothConversion;
    protected override string DefaultPlanName => "My Roth Conversion Strategy";
    protected override string ExportSuccessMessage => "Your Roth conversion workbook is ready to share.";
    protected override string ExportFailureMessage => "The Roth conversion workbook could not be created locally.";

    partial void OnCurrentAgeTextChanged(string value) => OnDraftInputChanged();
    partial void OnStartYearTextChanged(string value) => OnDraftInputChanged();
    partial void OnTraditionalBalanceTextChanged(string value) => OnDraftInputChanged();
    partial void OnRothBalanceTextChanged(string value) => OnDraftInputChanged();
    partial void OnAnnualConversionTextChanged(string value) => OnDraftInputChanged();
    partial void OnConversionYearsTextChanged(string value) => OnDraftInputChanged();
    partial void OnExpectedReturnTextChanged(string value) => OnDraftInputChanged();
    partial void OnEstimatedTaxRateTextChanged(string value) => OnDraftInputChanged();

    protected override void ApplyDraft(RothConversionDraft draft)
    {
        CurrentAgeText = draft.CurrentAge.ToString(CultureInfo.CurrentCulture);
        StartYearText = draft.StartYear.ToString(CultureInfo.CurrentCulture);
        TraditionalBalanceText = FormatNumber(draft.TraditionalBalance);
        RothBalanceText = FormatNumber(draft.RothBalance);
        AnnualConversionText = FormatNumber(draft.AnnualConversion);
        ConversionYearsText = draft.ConversionYears.ToString(CultureInfo.CurrentCulture);
        ExpectedReturnText = FormatNumber(draft.ExpectedReturn * 100);
        EstimatedTaxRateText = FormatNumber(draft.EstimatedTaxRate * 100);
    }

    protected override bool TryBuildDraft(out RothConversionDraft draft)
    {
        draft = DefaultDraft;
        if (!TryParseWholeNumber(CurrentAgeText, out var currentAge) || currentAge is < 18 or > 100)
        {
            ValidationMessage = "Enter a current age from 18 to 100.";
            return false;
        }

        if (!TryParseWholeNumber(StartYearText, out var startYear) || startYear is < 1900 or > 2200)
        {
            ValidationMessage = "Enter a first conversion year from 1900 to 2200.";
            return false;
        }

        if (!TryParseNonNegative(TraditionalBalanceText, out var traditionalBalance)
            || !TryParseNonNegative(RothBalanceText, out var rothBalance))
        {
            ValidationMessage = "Enter zero or a positive amount for each account balance.";
            return false;
        }

        if (!double.TryParse(AnnualConversionText, NumberStyles.Number, CultureInfo.CurrentCulture, out var annualConversion)
            || annualConversion <= 0)
        {
            ValidationMessage = "Enter an annual conversion amount greater than zero.";
            return false;
        }

        if (!TryParseWholeNumber(ConversionYearsText, out var conversionYears)
            || conversionYears is < 1 or > 50)
        {
            ValidationMessage = "Enter a conversion period from 1 to 50 years.";
            return false;
        }

        if (!TryParsePercentage(ExpectedReturnText, -100, 100, out var expectedReturn))
        {
            ValidationMessage = "Enter an expected return from -100% to 100%.";
            return false;
        }

        if (!TryParsePercentage(EstimatedTaxRateText, 0, 100, out var estimatedTaxRate))
        {
            ValidationMessage = "Enter an estimated conversion tax rate from 0% to 100%.";
            return false;
        }

        draft = new(
            currentAge,
            startYear,
            traditionalBalance,
            rothBalance,
            annualConversion,
            conversionYears,
            expectedReturn,
            estimatedTaxRate);
        return true;
    }

    protected override void Recalculate(RothConversionDraft draft)
    {
        var result = RothConversionCalculator.Calculate(draft.ToInputs());
        TotalConvertedText = FormatCurrency(result.TotalConverted);
        TotalTaxesText = FormatCurrency(result.TotalEstimatedTaxes);
        FirstAccessibleYearText = result.FirstAccessibleYear?.ToString(CultureInfo.CurrentCulture) ?? "Not available";
        EndingTraditionalBalanceText = FormatCurrency(result.EndingTraditionalBalance);
        EndingRothBalanceText = FormatCurrency(result.EndingRothBalance);
        ProjectionSummary =
            $"{FormatCurrency(result.TotalConverted)} is planned for conversion over {draft.ConversionYears} years. " +
            $"Each year's converted principal is shown as accessible after five tax years.";

        ProjectionSeries =
        [
            CreateProjectionSeries(
                "Traditional balance",
                result.Projections.Select(point => point.EndingTraditionalBalance),
                new SKColor(72, 93, 165)),
            CreateProjectionSeries(
                "Roth balance",
                result.Projections.Select(point => point.EndingRothBalance),
                new SKColor(43, 111, 83))
        ];
        ProjectionXAxes =
        [
            CreateLabelledAxis(
                "Year",
                result.Projections.Select(point => point.CalendarYear.ToString(CultureInfo.CurrentCulture)))
        ];
        ProjectionChartDescription =
            $"Traditional and Roth account balance projection from {draft.StartYear} through {result.Projections[^1].CalendarYear}.";
    }

    protected override Task ShareAsync(RothConversionDraft draft) =>
        exportService.ShareAsync(draft, RothConversionCalculator.Calculate(draft.ToInputs()));
}
