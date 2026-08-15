using CommunityToolkit.Mvvm.ComponentModel;
using MyFireNumber.Core.Calculations;
using MyFireNumber.Services;
using SkiaSharp;
using System.Globalization;

namespace MyFireNumber.ViewModels;

public sealed partial class CoastFireViewModel : CalculatorViewModelBase<CoastFireDraft>
{
    private readonly ICoastFireExportService exportService;

    public CoastFireViewModel(
        CalculatorViewModelServices services,
        ICoastFireExportService exportService)
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
    private string annualContributionText = string.Empty;

    [ObservableProperty]
    private string annualExpensesText = string.Empty;

    [ObservableProperty]
    private string expectedReturnText = string.Empty;

    [ObservableProperty]
    private string inflationRateText = string.Empty;

    [ObservableProperty]
    private string withdrawalRateText = string.Empty;

    [ObservableProperty]
    private string coastNumberText = string.Empty;

    [ObservableProperty]
    private string fullFireNumberText = string.Empty;

    [ObservableProperty]
    private string yearsToCoastText = string.Empty;

    [ObservableProperty]
    private string coastStatusText = string.Empty;

    [ObservableProperty]
    private string coastProgressDescription = string.Empty;

    [ObservableProperty]
    private string coastProjectionSummary = string.Empty;

    protected override string CalculatorId => "coast-fire";

    protected override int DraftPayloadVersion => CoastFireDraft.PayloadVersion;

    protected override CoastFireDraft DefaultDraft => CalculatorDefaults.CoastFire;

    protected override string DefaultPlanName => "My Coast FIRE Plan";

    protected override string ExportSuccessMessage => "Your Coast FIRE workbook is ready to share.";

    protected override string ExportFailureMessage => "The Coast FIRE workbook could not be created locally.";

    partial void OnCurrentAgeTextChanged(string value) => OnDraftInputChanged();
    partial void OnRetirementAgeTextChanged(string value) => OnDraftInputChanged();
    partial void OnCurrentSavingsTextChanged(string value) => OnDraftInputChanged();
    partial void OnAnnualContributionTextChanged(string value) => OnDraftInputChanged();
    partial void OnAnnualExpensesTextChanged(string value) => OnDraftInputChanged();
    partial void OnExpectedReturnTextChanged(string value) => OnDraftInputChanged();
    partial void OnInflationRateTextChanged(string value) => OnDraftInputChanged();
    partial void OnWithdrawalRateTextChanged(string value) => OnDraftInputChanged();

    protected override void ApplyDraft(CoastFireDraft draft)
    {
        CurrentAgeText = draft.CurrentAge.ToString(CultureInfo.CurrentCulture);
        RetirementAgeText = draft.RetirementAge.ToString(CultureInfo.CurrentCulture);
        CurrentSavingsText = FormatNumber(draft.CurrentSavings);
        AnnualContributionText = FormatNumber(draft.AnnualContribution);
        AnnualExpensesText = FormatNumber(draft.AnnualExpenses);
        ExpectedReturnText = FormatNumber(draft.ExpectedReturn * 100);
        InflationRateText = FormatNumber(draft.InflationRate * 100);
        WithdrawalRateText = FormatNumber(draft.WithdrawalRate * 100);
    }

    protected override bool TryBuildDraft(out CoastFireDraft draft)
    {
        draft = DefaultDraft;
        if (!TryParseWholeNumber(CurrentAgeText, out var currentAge) || currentAge is < 18 or > 100)
        {
            ValidationMessage = "Enter a current age from 18 to 100.";
            return false;
        }

        if (!TryParseWholeNumber(RetirementAgeText, out var retirementAge) || retirementAge < currentAge || retirementAge > 100)
        {
            ValidationMessage = "Enter a retirement age from your current age through 100.";
            return false;
        }

        if (!TryParseNonNegative(CurrentSavingsText, out var currentSavings)
            || !TryParseNonNegative(AnnualContributionText, out var annualContribution)
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

        draft = new CoastFireDraft(
            currentAge,
            retirementAge,
            currentSavings,
            annualContribution,
            expectedReturn,
            inflationRate,
            withdrawalRate,
            annualExpenses);
        return true;
    }

    protected override void Recalculate(CoastFireDraft draft)
    {
        var result = FinancialCalculator.CalculateCoastFire(draft.ToFireInputs());
        CoastNumberText = FormatCurrency(result.CoastNumber);
        FullFireNumberText = FormatCurrency(result.FireNumber);
        var coastReachable = double.IsFinite(result.YearsToCoast);
        YearsToCoastText = result.AlreadyCoasting
            ? "Already coasting"
            : coastReachable ? $"{result.YearsToCoast:N1} years" : "Not reachable with these inputs";
        CoastStatusText = result.AlreadyCoasting
            ? "You're already Coast FIRE!"
            : coastReachable
                ? $"{result.YearsToCoast:N1} years to Coast FIRE"
                : "The current contribution and return assumptions do not reach the Coast FIRE Number.";
        var progress = result.CoastNumber <= 0 ? 0 : Math.Clamp(draft.CurrentSavings / result.CoastNumber, 0, 1);
        CoastProgressDescription = $"{progress:P0} of your Coast FIRE Number is currently funded.";
        CoastProjectionSummary = $"By age {result.Projections[^1].Age:0}, coasting projects {FormatCurrency(result.Projections[^1].Portfolio)} and continuing contributions projects {FormatCurrency(result.ProjectionsWithContributions[^1].Portfolio)}.";
        UpdateProjectionChart(result);
    }

    protected override async Task ShareAsync(CoastFireDraft draft)
    {
        await exportService.ShareAsync(draft, FinancialCalculator.CalculateCoastFire(draft.ToFireInputs()));
    }

    private void UpdateProjectionChart(CoastFireResult result)
    {
        ProjectionSeries =
        [
            CreateProjectionSeries("Coast", result.Projections.Select(point => point.Portfolio), new SKColor(51, 117, 171)),
            CreateProjectionSeries("Continue contributing", result.ProjectionsWithContributions.Select(point => point.Portfolio), new SKColor(95, 86, 189)),
            CreateProjectionSeries("Full FIRE target", Enumerable.Repeat(result.FireNumber, result.Projections.Count), new SKColor(201, 119, 39))
        ];
        ProjectionXAxes =
        [
            CreateLabelledAxis("Age", result.Projections.Select(point => point.Age.ToString("0", CultureInfo.CurrentCulture)))
        ];
        ProjectionChartDescription = $"Coast FIRE comparison from age {result.Projections[0].Age:0} through age {result.Projections[^1].Age:0}. "
            + $"The Coast path has no further contributions, while the comparison path continues contributions. The full FIRE target is {FormatCurrency(result.FireNumber)}.";
    }
}
