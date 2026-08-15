using CommunityToolkit.Mvvm.ComponentModel;
using MyFireNumber.Core.Calculations;
using MyFireNumber.Services;
using SkiaSharp;
using System.Globalization;

namespace MyFireNumber.ViewModels;

public sealed partial class ReverseFireViewModel : CalculatorViewModelBase<ReverseFireDraft>
{
    private readonly IReverseFireExportService exportService;

    public ReverseFireViewModel(
        CalculatorViewModelServices services,
        IReverseFireExportService exportService)
        : base(services)
    {
        this.exportService = exportService;
    }

    [ObservableProperty]
    private string currentAgeText = string.Empty;

    [ObservableProperty]
    private string retirementAgeText = string.Empty;

    [ObservableProperty]
    private string currentSavingsText = string.Empty;

    [ObservableProperty]
    private string annualExpensesText = string.Empty;

    [ObservableProperty]
    private string expectedReturnText = string.Empty;

    [ObservableProperty]
    private string inflationRateText = string.Empty;

    [ObservableProperty]
    private string withdrawalRateText = string.Empty;

    [ObservableProperty]
    private string reverseRequiredAnnualSavingsText = string.Empty;

    [ObservableProperty]
    private string reverseRequiredMonthlySavingsText = string.Empty;

    [ObservableProperty]
    private string reverseYearsText = string.Empty;

    [ObservableProperty]
    private string reverseCurrentGrowthText = string.Empty;

    [ObservableProperty]
    private string reverseStatusText = string.Empty;

    [ObservableProperty]
    private string reverseProjectionSummary = string.Empty;

    protected override string CalculatorId => "reverse-fire";

    protected override int DraftPayloadVersion => ReverseFireDraft.PayloadVersion;

    protected override ReverseFireDraft DefaultDraft => CalculatorDefaults.ReverseFire;

    protected override string DefaultPlanName => "My Reverse FIRE Plan";

    protected override string ExportSuccessMessage => "Your Reverse FIRE workbook is ready to share.";

    protected override string ExportFailureMessage => "The Reverse FIRE workbook could not be created locally.";

    partial void OnCurrentAgeTextChanged(string value) => OnDraftInputChanged();
    partial void OnRetirementAgeTextChanged(string value) => OnDraftInputChanged();
    partial void OnCurrentSavingsTextChanged(string value) => OnDraftInputChanged();
    partial void OnAnnualExpensesTextChanged(string value) => OnDraftInputChanged();
    partial void OnExpectedReturnTextChanged(string value) => OnDraftInputChanged();
    partial void OnInflationRateTextChanged(string value) => OnDraftInputChanged();
    partial void OnWithdrawalRateTextChanged(string value) => OnDraftInputChanged();

    protected override void ApplyDraft(ReverseFireDraft draft)
    {
        CurrentAgeText = draft.CurrentAge.ToString(CultureInfo.CurrentCulture);
        RetirementAgeText = draft.TargetRetirementAge.ToString(CultureInfo.CurrentCulture);
        CurrentSavingsText = FormatNumber(draft.CurrentSavings);
        AnnualExpensesText = FormatNumber(draft.AnnualExpenses);
        ExpectedReturnText = FormatNumber(draft.ExpectedReturn * 100);
        InflationRateText = FormatNumber(draft.InflationRate * 100);
        WithdrawalRateText = FormatNumber(draft.WithdrawalRate * 100);
    }

    protected override bool TryBuildDraft(out ReverseFireDraft draft)
    {
        draft = DefaultDraft;
        if (!TryParseWholeNumber(CurrentAgeText, out var currentAge) || currentAge is < 18 or > 100)
        {
            ValidationMessage = "Enter a current age from 18 to 100.";
            return false;
        }

        // Retiring at your current age is a valid scenario to model, so equality is allowed
        // here and on every other calculator across both platforms.
        if (!TryParseWholeNumber(RetirementAgeText, out var targetRetirementAge)
            || targetRetirementAge < currentAge
            || targetRetirementAge > 100)
        {
            ValidationMessage = "Enter a target FIRE age from your current age through 100.";
            return false;
        }

        if (!TryParseNonNegative(CurrentSavingsText, out var currentSavings)
            || !TryParseNonNegative(AnnualExpensesText, out var annualExpenses))
        {
            ValidationMessage = "Enter zero or a positive amount for each dollar value.";
            return false;
        }

        if (!TryParsePercentage(ExpectedReturnText, 0, 15, out var expectedReturn)
            || !TryParsePercentage(InflationRateText, 0, 10, out var inflationRate)
            || !TryParsePercentage(WithdrawalRateText, 2, 6, out var withdrawalRate))
        {
            ValidationMessage = "Expected return must be 0% to 15%, inflation 0% to 10%, and withdrawal rate 2% to 6%.";
            return false;
        }

        draft = new ReverseFireDraft(currentAge, targetRetirementAge, currentSavings, expectedReturn, inflationRate, withdrawalRate, annualExpenses);
        return true;
    }

    protected override void Recalculate(ReverseFireDraft draft)
    {
        var result = FinancialCalculator.CalculateReverseFire(draft.ToFireInputs());
        ReverseRequiredAnnualSavingsText = FormatCurrency(result.RequiredAnnualSavings);
        ReverseRequiredMonthlySavingsText = FormatCurrency(result.RequiredMonthlySavings);
        ReverseYearsText = $"{result.YearsToFire:0} years";
        ReverseCurrentGrowthText = FormatCurrency(result.CurrentWillGrowTo);
        ReverseStatusText = result.AlreadyAchievable
            ? "You're already on track!"
            : $"To FIRE by age {draft.TargetRetirementAge}, you need to save {FormatCurrency(result.RequiredMonthlySavings)} per month.";
        ReverseProjectionSummary = $"Your current savings are projected to grow to {FormatCurrency(result.CurrentWillGrowTo)} by age {draft.TargetRetirementAge}; the target portfolio is {FormatCurrency(result.FireNumber)}.";
        UpdateProjectionChart(result);
    }

    protected override async Task ShareAsync(ReverseFireDraft draft)
    {
        await exportService.ShareAsync(draft, FinancialCalculator.CalculateReverseFire(draft.ToFireInputs()));
    }

    private void UpdateProjectionChart(ReverseFireResult result)
    {
        ProjectionSeries =
        [
            CreateProjectionSeries("Portfolio", result.Projections.Select(point => point.Portfolio), new SKColor(29, 119, 130)),
            CreateProjectionSeries("Today's dollars", result.Projections.Select(point => point.InflationAdjusted), new SKColor(84, 112, 104)),
            CreateProjectionSeries("FIRE target", Enumerable.Repeat(result.FireNumber, result.Projections.Count), new SKColor(201, 119, 39))
        ];
        ProjectionXAxes =
        [
            CreateLabelledAxis("Age", result.Projections.Select(point => point.Age.ToString("0", CultureInfo.CurrentCulture)))
        ];
        ProjectionChartDescription = $"Reverse FIRE projection from age {result.Projections[0].Age:0} through age {result.Projections[^1].Age:0}. "
            + $"The required-saving path targets {FormatCurrency(result.FireNumber)}.";
    }
}
