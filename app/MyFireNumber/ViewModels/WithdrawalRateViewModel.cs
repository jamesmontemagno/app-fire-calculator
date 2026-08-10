using CommunityToolkit.Mvvm.ComponentModel;
using MyFireNumber.Core.Calculations;
using MyFireNumber.Services;
using SkiaSharp;
using System.Globalization;

namespace MyFireNumber.ViewModels;

public sealed partial class WithdrawalRateViewModel : CalculatorViewModelBase<WithdrawalRateDraft>
{
    private readonly IWithdrawalRateExportService exportService;

    public WithdrawalRateViewModel(
        CalculatorViewModelServices services,
        IWithdrawalRateExportService exportService)
        : base(services)
    {
        this.exportService = exportService;
    }

    [ObservableProperty]
    private string portfolioValueText = string.Empty;

    [ObservableProperty]
    private string withdrawalRateText = string.Empty;

    [ObservableProperty]
    private string expectedReturnText = string.Empty;

    [ObservableProperty]
    private string inflationRateText = string.Empty;

    [ObservableProperty]
    private string retirementYearsText = string.Empty;

    [ObservableProperty]
    private string withdrawalAnnualText = string.Empty;

    [ObservableProperty]
    private string withdrawalMonthlyText = string.Empty;

    [ObservableProperty]
    private string withdrawalLongevityText = string.Empty;

    [ObservableProperty]
    private string withdrawalSuccessText = string.Empty;

    [ObservableProperty]
    private string withdrawalStatusText = string.Empty;

    [ObservableProperty]
    private string withdrawalRateAnalysisText = string.Empty;

    protected override string CalculatorId => "withdrawal-rate";

    protected override int DraftPayloadVersion => WithdrawalRateDraft.PayloadVersion;

    protected override WithdrawalRateDraft DefaultDraft => CalculatorDefaults.WithdrawalRate;

    protected override string DefaultPlanName => "My Withdrawal Plan";

    protected override string ExportSuccessMessage => "Your Withdrawal Rate workbook is ready to share.";

    protected override string ExportFailureMessage => "The Withdrawal Rate workbook could not be created locally.";

    partial void OnPortfolioValueTextChanged(string value) => OnDraftInputChanged();
    partial void OnWithdrawalRateTextChanged(string value) => OnDraftInputChanged();
    partial void OnExpectedReturnTextChanged(string value) => OnDraftInputChanged();
    partial void OnInflationRateTextChanged(string value) => OnDraftInputChanged();
    partial void OnRetirementYearsTextChanged(string value) => OnDraftInputChanged();

    protected override void ApplyDraft(WithdrawalRateDraft draft)
    {
        PortfolioValueText = FormatNumber(draft.PortfolioValue);
        WithdrawalRateText = FormatNumber(draft.WithdrawalRate * 100);
        ExpectedReturnText = FormatNumber(draft.ExpectedReturn * 100);
        InflationRateText = FormatNumber(draft.InflationRate * 100);
        RetirementYearsText = draft.RetirementYears.ToString(CultureInfo.CurrentCulture);
    }

    protected override bool TryBuildDraft(out WithdrawalRateDraft draft)
    {
        draft = DefaultDraft;
        if (!TryParseNonNegative(PortfolioValueText, out var portfolioValue) || portfolioValue <= 0)
        {
            ValidationMessage = "Enter a portfolio value greater than zero.";
            return false;
        }

        if (!TryParseWholeNumber(RetirementYearsText, out var retirementYears) || retirementYears is < 10 or > 60)
        {
            ValidationMessage = "Enter a retirement duration from 10 to 60 years.";
            return false;
        }

        if (!TryParsePercentage(WithdrawalRateText, 2, 8, out var withdrawalRate)
            || !TryParsePercentage(ExpectedReturnText, 0, 15, out var expectedReturn)
            || !TryParsePercentage(InflationRateText, 0, 10, out var inflationRate))
        {
            ValidationMessage = "Withdrawal rate must be 2% to 8%, return 0% to 15%, and inflation 0% to 10%.";
            return false;
        }

        draft = new WithdrawalRateDraft(portfolioValue, withdrawalRate, expectedReturn, inflationRate, retirementYears);
        return true;
    }

    protected override void Recalculate(WithdrawalRateDraft draft)
    {
        var result = Calculate(draft);
        WithdrawalAnnualText = FormatCurrency(result.AnnualWithdrawal);
        WithdrawalMonthlyText = FormatCurrency(result.MonthlyWithdrawal);
        WithdrawalLongevityText = result.PortfolioLongevity >= draft.RetirementYears
            ? $"{draft.RetirementYears}+ years"
            : $"{result.PortfolioLongevity:0} years";
        WithdrawalSuccessText = result.SuccessRate.ToString("P0", CultureInfo.CurrentCulture);
        WithdrawalStatusText = result.PortfolioLongevity >= draft.RetirementYears
            ? "Your withdrawal rate is sustainable for this goal."
            : "Your portfolio may run out before this goal.";
        WithdrawalRateAnalysisText = string.Join("  ", result.RateAnalysis.Select(analysis =>
            $"{analysis.Rate:P1}: {(analysis.Years >= draft.RetirementYears ? "Sustainable" : $"{analysis.Years:0} years")}"));
        UpdateProjectionChart(result);
    }

    protected override async Task ShareAsync(WithdrawalRateDraft draft)
    {
        await exportService.ShareAsync(draft, Calculate(draft));
    }

    private static WithdrawalResult Calculate(WithdrawalRateDraft draft)
    {
        return FinancialCalculator.CalculateWithdrawal(
            draft.PortfolioValue,
            draft.WithdrawalRate,
            draft.ExpectedReturn,
            draft.InflationRate,
            draft.RetirementYears);
    }

    private void UpdateProjectionChart(WithdrawalResult result)
    {
        ProjectionSeries =
        [
            CreateProjectionSeries("Portfolio balance", result.WithdrawalProjections.Select(point => point.Balance), new SKColor(42, 121, 160)),
            CreateProjectionSeries("Annual withdrawal", result.WithdrawalProjections.Select(point => point.Withdrawal), new SKColor(201, 119, 39))
        ];
        ProjectionXAxes =
        [
            CreateLabelledAxis(
                "Retirement year",
                result.WithdrawalProjections.Select(point => point.Year.ToString("0", CultureInfo.CurrentCulture)))
        ];
        ProjectionChartDescription = $"Withdrawal projection through retirement year {result.WithdrawalProjections[^1].Year:0}. "
            + $"The ending portfolio balance is {FormatCurrency(result.EndingBalance)}.";
    }
}
