using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using MyFireNumber.Core.Calculations;
using MyFireNumber.Services;
using SkiaSharp;

namespace MyFireNumber.ViewModels;

public sealed partial class InterestCalculatorViewModel : CalculatorViewModelBase<InterestCalculatorDraft>
{
    private readonly IInterestCalculatorExportService exportService;

    public InterestCalculatorViewModel(CalculatorViewModelServices services, IInterestCalculatorExportService exportService)
        : base(services)
    {
        this.exportService = exportService;
    }

    [ObservableProperty]
    public partial string StartingBalanceText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string MonthlyContributionText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AnnualInterestRateText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string YearsText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EndingBalanceText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TotalContributionsText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string InterestEarnedText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EffectiveAnnualYieldText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ProjectionSummary { get; set; } = string.Empty;

    protected override string CalculatorId => "interest-calculator";
    protected override int DraftPayloadVersion => InterestCalculatorDraft.PayloadVersion;
    protected override InterestCalculatorDraft DefaultDraft => CalculatorDefaults.InterestCalculator;
    protected override string DefaultPlanName => "My Interest Plan";
    protected override string ExportSuccessMessage => "Your Interest Calculator workbook is ready to share.";
    protected override string ExportFailureMessage => "The Interest Calculator workbook could not be created locally.";

    partial void OnStartingBalanceTextChanged(string value) => OnDraftInputChanged();
    partial void OnMonthlyContributionTextChanged(string value) => OnDraftInputChanged();
    partial void OnAnnualInterestRateTextChanged(string value) => OnDraftInputChanged();
    partial void OnYearsTextChanged(string value) => OnDraftInputChanged();

    protected override void ApplyDraft(InterestCalculatorDraft draft)
    {
        StartingBalanceText = FormatNumber(draft.StartingBalance);
        MonthlyContributionText = FormatNumber(draft.MonthlyContribution);
        AnnualInterestRateText = FormatNumber(draft.AnnualInterestRate * 100);
        YearsText = draft.Years.ToString(CultureInfo.CurrentCulture);
    }

    protected override bool TryBuildDraft(out InterestCalculatorDraft draft)
    {
        draft = DefaultDraft;
        if (!TryParseNonNegative(StartingBalanceText, out var startingBalance)
            || !TryParseNonNegative(MonthlyContributionText, out var monthlyContribution))
        {
            ValidationMessage = "Enter zero or a positive amount for each dollar value.";
            return false;
        }

        if (!TryParsePercentage(AnnualInterestRateText, 0, 50, out var annualInterestRate))
        {
            ValidationMessage = "Enter an annual interest rate from 0% to 50%.";
            return false;
        }

        if (!TryParseWholeNumber(YearsText, out var years) || years is < 1 or > 60)
        {
            ValidationMessage = "Enter a time period from 1 to 60 years.";
            return false;
        }

        draft = new(startingBalance, monthlyContribution, annualInterestRate, years);
        return true;
    }

    protected override void Recalculate(InterestCalculatorDraft draft)
    {
        var result = FinancialCalculator.CalculateInterest(draft.ToInputs());
        EndingBalanceText = FormatCurrency(result.EndingBalance);
        TotalContributionsText = FormatCurrency(result.TotalContributions);
        InterestEarnedText = FormatCurrency(result.InterestEarned);
        EffectiveAnnualYieldText = result.EffectiveAnnualYield.ToString("P2", CultureInfo.CurrentCulture);
        ProjectionSummary = $"After {draft.Years} years, the projected balance is {FormatCurrency(result.EndingBalance)}, including {FormatCurrency(result.InterestEarned)} in interest.";
        ProjectionSeries =
        [
            CreateProjectionSeries("Balance", result.Projections.Select(point => point.Balance), new SKColor(42, 121, 160)),
            CreateProjectionSeries("Contributions", result.Projections.Select(point => point.TotalContributions), new SKColor(201, 119, 39))
        ];
        ProjectionXAxes =
        [
            CreateLabelledAxis("Year", result.Projections.Select(point => point.Year.ToString(CultureInfo.CurrentCulture)))
        ];
        ProjectionChartDescription = $"Interest projection through year {draft.Years}. The ending balance is {FormatCurrency(result.EndingBalance)}.";
    }

    protected override Task ShareAsync(InterestCalculatorDraft draft) =>
        exportService.ShareAsync(draft, FinancialCalculator.CalculateInterest(draft.ToInputs()));
}
