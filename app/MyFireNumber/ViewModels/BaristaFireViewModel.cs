using CommunityToolkit.Mvvm.ComponentModel;
using MyFireNumber.Core.Calculations;
using MyFireNumber.Services;
using SkiaSharp;
using System.Globalization;

namespace MyFireNumber.ViewModels;

public sealed partial class BaristaFireViewModel : CalculatorViewModelBase<BaristaFireDraft>
{
    private readonly IBaristaFireExportService exportService;

    public BaristaFireViewModel(
        CalculatorViewModelServices services,
        IBaristaFireExportService exportService)
        : base(services)
    {
        this.exportService = exportService;
    }

    [ObservableProperty]
    private string currentAgeText = string.Empty;

    [ObservableProperty]
    private string currentSavingsText = string.Empty;

    [ObservableProperty]
    private string annualContributionText = string.Empty;

    [ObservableProperty]
    private string annualExpensesText = string.Empty;

    [ObservableProperty]
    private string partTimeAnnualIncomeText = string.Empty;

    [ObservableProperty]
    private string expectedReturnText = string.Empty;

    [ObservableProperty]
    private string inflationRateText = string.Empty;

    [ObservableProperty]
    private string withdrawalRateText = string.Empty;

    [ObservableProperty]
    private string baristaNumberText = string.Empty;

    [ObservableProperty]
    private string fullFireNumberText = string.Empty;

    [ObservableProperty]
    private string baristaYearsText = string.Empty;

    [ObservableProperty]
    private string baristaReductionText = string.Empty;

    [ObservableProperty]
    private string baristaProgressDescription = string.Empty;

    [ObservableProperty]
    private string baristaProjectionSummary = string.Empty;

    protected override string CalculatorId => "barista-fire";

    protected override int DraftPayloadVersion => BaristaFireDraft.PayloadVersion;

    protected override BaristaFireDraft DefaultDraft => CalculatorDefaults.BaristaFire;

    protected override string DefaultPlanName => "My Barista FIRE Plan";

    protected override string ExportSuccessMessage => "Your Barista FIRE workbook is ready to share.";

    protected override string ExportFailureMessage => "The Barista FIRE workbook could not be created locally.";

    partial void OnCurrentAgeTextChanged(string value) => OnDraftInputChanged();
    partial void OnCurrentSavingsTextChanged(string value) => OnDraftInputChanged();
    partial void OnAnnualContributionTextChanged(string value) => OnDraftInputChanged();
    partial void OnAnnualExpensesTextChanged(string value) => OnDraftInputChanged();
    partial void OnPartTimeAnnualIncomeTextChanged(string value) => OnDraftInputChanged();
    partial void OnExpectedReturnTextChanged(string value) => OnDraftInputChanged();
    partial void OnInflationRateTextChanged(string value) => OnDraftInputChanged();
    partial void OnWithdrawalRateTextChanged(string value) => OnDraftInputChanged();

    protected override void ApplyDraft(BaristaFireDraft draft)
    {
        CurrentAgeText = draft.CurrentAge.ToString(CultureInfo.CurrentCulture);
        CurrentSavingsText = FormatNumber(draft.CurrentSavings);
        AnnualContributionText = FormatNumber(draft.AnnualContribution);
        AnnualExpensesText = FormatNumber(draft.AnnualExpenses);
        PartTimeAnnualIncomeText = FormatNumber(draft.PartTimeAnnualIncome);
        ExpectedReturnText = FormatNumber(draft.ExpectedReturn * 100);
        InflationRateText = FormatNumber(draft.InflationRate * 100);
        WithdrawalRateText = FormatNumber(draft.WithdrawalRate * 100);
    }

    protected override bool TryBuildDraft(out BaristaFireDraft draft)
    {
        draft = DefaultDraft;
        if (!TryParseWholeNumber(CurrentAgeText, out var currentAge) || currentAge is < 18 or > 100)
        {
            ValidationMessage = "Enter a current age from 18 to 100.";
            return false;
        }

        if (!TryParseNonNegative(CurrentSavingsText, out var currentSavings)
            || !TryParseNonNegative(AnnualContributionText, out var annualContribution)
            || !TryParseNonNegative(AnnualExpensesText, out var annualExpenses)
            || !TryParseNonNegative(PartTimeAnnualIncomeText, out var partTimeIncome))
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

        draft = new BaristaFireDraft(currentAge, currentSavings, annualContribution, expectedReturn, inflationRate, withdrawalRate, annualExpenses, partTimeIncome);
        return true;
    }

    protected override void Recalculate(BaristaFireDraft draft)
    {
        var result = FinancialCalculator.CalculateBaristaFire(draft.ToFireInputs(), draft.PartTimeAnnualIncome);
        BaristaNumberText = FormatCurrency(result.BaristaNumber);
        FullFireNumberText = FormatCurrency(result.FullFireNumber);
        BaristaYearsText = double.IsPositiveInfinity(result.YearsToBaristaFire)
            ? "Not reachable with these inputs"
            : $"{result.YearsToBaristaFire:N1} years";
        BaristaReductionText = $"{FormatCurrency(result.SavingsFromPartTime)} less needed with part-time income.";
        var progress = result.BaristaNumber <= 0 ? 0 : Math.Clamp(draft.CurrentSavings / result.BaristaNumber, 0, 1);
        BaristaProgressDescription = $"{progress:P0} of your Barista FIRE Number is currently funded.";
        BaristaProjectionSummary = double.IsPositiveInfinity(result.YearsToBaristaFire)
            ? "The current contribution and return assumptions do not reach the Barista FIRE Number."
            : $"At the current assumptions, your portfolio is projected to reach {FormatCurrency(result.BaristaNumber)} in approximately {result.YearsToBaristaFire:N1} years.";
        UpdateProjectionChart(result);
    }

    protected override async Task ShareAsync(BaristaFireDraft draft)
    {
        await exportService.ShareAsync(
            draft,
            FinancialCalculator.CalculateBaristaFire(draft.ToFireInputs(), draft.PartTimeAnnualIncome));
    }

    private void UpdateProjectionChart(BaristaFireResult result)
    {
        ProjectionSeries =
        [
            CreateProjectionSeries("Portfolio", result.Projections.Select(point => point.Portfolio), new SKColor(154, 92, 23)),
            CreateProjectionSeries("Today's dollars", result.Projections.Select(point => point.InflationAdjusted), new SKColor(84, 112, 104)),
            CreateProjectionSeries("Barista FIRE target", Enumerable.Repeat(result.BaristaNumber, result.Projections.Count), new SKColor(201, 119, 39)),
            CreateProjectionSeries("Full FIRE target", Enumerable.Repeat(result.FullFireNumber, result.Projections.Count), new SKColor(75, 90, 84))
        ];
        ProjectionXAxes =
        [
            CreateLabelledAxis("Age", result.Projections.Select(point => point.Age.ToString("0", CultureInfo.CurrentCulture)))
        ];
        ProjectionChartDescription = $"Barista FIRE portfolio projection from age {result.Projections[0].Age:0} through age {result.Projections[^1].Age:0}. "
            + $"The Barista FIRE target is {FormatCurrency(result.BaristaNumber)} and the full FIRE target is {FormatCurrency(result.FullFireNumber)}.";
    }
}
