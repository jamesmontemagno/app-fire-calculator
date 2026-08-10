using CommunityToolkit.Mvvm.ComponentModel;
using MyFireNumber.Core.Calculations;
using MyFireNumber.Services;
using SkiaSharp;
using System.Globalization;

namespace MyFireNumber.ViewModels;

/// <summary>
/// Shared inputs, results, and projection charting for the FIRE Number family
/// (Standard, Lean, and Fat FIRE). Each variant only supplies its own draft type,
/// calculation, and export.
/// </summary>
public abstract partial class FireNumberViewModelBase<TDraft> : CalculatorViewModelBase<TDraft>, IFireNumberViewModel
    where TDraft : class
{
    protected FireNumberViewModelBase(CalculatorViewModelServices services)
        : base(services)
    {
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
    private string annualIncomeText = string.Empty;

    [ObservableProperty]
    private string annualExpensesText = string.Empty;

    [ObservableProperty]
    private string expectedReturnText = string.Empty;

    [ObservableProperty]
    private string inflationRateText = string.Empty;

    [ObservableProperty]
    private string withdrawalRateText = string.Empty;

    [ObservableProperty]
    private string fireNumberText = string.Empty;

    [ObservableProperty]
    private string yearsToFireText = string.Empty;

    [ObservableProperty]
    private string fireAgeText = string.Empty;

    [ObservableProperty]
    private string savingsRateText = string.Empty;

    [ObservableProperty]
    private string monthlyContributionText = string.Empty;

    [ObservableProperty]
    private string progressDescription = string.Empty;

    [ObservableProperty]
    private double progressToFire;

    [ObservableProperty]
    private string projectionSummary = string.Empty;

    [ObservableProperty]
    private string leanStatusText = string.Empty;

    [ObservableProperty]
    private string leanGuidanceText = string.Empty;

    [ObservableProperty]
    private string fatStatusText = string.Empty;

    [ObservableProperty]
    private string fatGuidanceText = string.Empty;

    [ObservableProperty]
    private StandardFirePreset? selectedPreset;

    public IReadOnlyList<StandardFirePreset> StandardFirePresets => StandardFirePreset.All;

    public virtual bool IsStandardFire => false;

    public virtual bool IsLeanFire => false;

    public virtual bool IsFatFire => false;

    public abstract string FireNumberLabel { get; }

    public abstract string YearsToFireLabel { get; }

    public abstract string OutlookTitle { get; }

    public abstract string ProjectionTitle { get; }

    public string PlanNamePlaceholder => DefaultPlanName;

    public abstract string PlanNameDescription { get; }

    public abstract string ExportDescription { get; }

    partial void OnCurrentAgeTextChanged(string value) => OnDraftInputChanged();
    partial void OnRetirementAgeTextChanged(string value) => OnDraftInputChanged();
    partial void OnCurrentSavingsTextChanged(string value) => OnDraftInputChanged();
    partial void OnAnnualContributionTextChanged(string value) => OnDraftInputChanged();
    partial void OnAnnualIncomeTextChanged(string value) => OnDraftInputChanged();
    partial void OnAnnualExpensesTextChanged(string value) => OnDraftInputChanged();
    partial void OnExpectedReturnTextChanged(string value) => OnDraftInputChanged();
    partial void OnInflationRateTextChanged(string value) => OnDraftInputChanged();
    partial void OnWithdrawalRateTextChanged(string value) => OnDraftInputChanged();

    partial void OnSelectedPresetChanged(StandardFirePreset? value)
    {
        if (value is not null && IsStandardFire)
        {
            ValidationMessage = string.Empty;
            LoadInputs(FromStandard(value.Draft));
        }
    }

    protected override void OnDraftInputChanged()
    {
        if (!IsApplyingDraft)
        {
            SelectedPreset = null;
        }

        base.OnDraftInputChanged();
    }

    /// <summary>Converts the shared Standard FIRE shape into this variant's draft.</summary>
    protected abstract TDraft FromStandard(StandardFireDraft draft);

    /// <summary>Projects this variant's draft onto the shared Standard FIRE shape.</summary>
    protected abstract StandardFireDraft ToStandard(TDraft draft);

    /// <summary>Runs this variant's calculation and publishes any variant-only messaging.</summary>
    protected abstract StandardFireResult CalculateResult(TDraft draft);

    protected sealed override void ApplyDraft(TDraft draft)
    {
        var standard = ToStandard(draft);
        CurrentAgeText = standard.CurrentAge.ToString(CultureInfo.CurrentCulture);
        RetirementAgeText = standard.RetirementAge.ToString(CultureInfo.CurrentCulture);
        CurrentSavingsText = FormatNumber(standard.CurrentSavings);
        AnnualContributionText = FormatNumber(standard.AnnualContribution);
        AnnualIncomeText = FormatNumber(standard.AnnualIncome);
        AnnualExpensesText = FormatNumber(standard.AnnualExpenses);
        ExpectedReturnText = FormatNumber(standard.ExpectedReturn * 100);
        InflationRateText = FormatNumber(standard.InflationRate * 100);
        WithdrawalRateText = FormatNumber(standard.WithdrawalRate * 100);
    }

    protected sealed override bool TryBuildDraft(out TDraft draft)
    {
        draft = DefaultDraft;
        if (!TryBuildStandardDraft(out var standard))
        {
            return false;
        }

        draft = FromStandard(standard);
        return true;
    }

    protected sealed override void Recalculate(TDraft draft)
    {
        var standard = ToStandard(draft);
        var result = CalculateResult(draft);
        ValidationMessage = string.Empty;
        FireNumberText = FormatCurrency(result.FireNumber);
        YearsToFireText = double.IsPositiveInfinity(result.YearsToFire)
            ? "Not reachable with these inputs"
            : $"{result.YearsToFire:N1} years";
        FireAgeText = double.IsPositiveInfinity(result.FireAge) ? "--" : $"Age {result.FireAge:N1}";
        SavingsRateText = $"{result.SavingsRate:P1}";
        MonthlyContributionText = FormatCurrency(result.MonthlyContribution);
        ProgressToFire = result.FireNumber <= 0 ? 0 : Math.Clamp(standard.CurrentSavings / result.FireNumber, 0, 1);
        ProgressDescription = $"{ProgressToFire:P0} of your {FireNumberLabel} is currently funded.";
        ProjectionSummary = BuildProjectionSummary(result);
        UpdateProjectionChart(result);
    }

    protected virtual string BuildProjectionSummary(StandardFireResult result)
    {
        return $"At the current assumptions, your portfolio is projected to reach {FormatCurrency(result.FireNumber)} in approximately {result.YearsToFire:N1} years.";
    }

    private bool TryBuildStandardDraft(out StandardFireDraft draft)
    {
        draft = CalculatorDefaults.StandardFire;
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
            || !TryParseNonNegative(AnnualIncomeText, out var annualIncome)
            || !TryParseNonNegative(AnnualExpensesText, out var annualExpenses))
        {
            ValidationMessage = "Enter zero or a positive amount for each dollar value.";
            return false;
        }

        if (!TryParsePercentage(ExpectedReturnText, 0, 20, out var expectedReturn)
            || !TryParsePercentage(InflationRateText, 0, 10, out var inflationRate)
            || !TryParsePercentage(WithdrawalRateText, 2, 6, out var withdrawalRate))
        {
            ValidationMessage = "Expected return must be 0% to 20%, inflation 0% to 10%, and withdrawal rate 2% to 6%.";
            return false;
        }

        draft = new StandardFireDraft(
            currentAge,
            retirementAge,
            currentSavings,
            annualContribution,
            annualIncome,
            expectedReturn,
            inflationRate,
            withdrawalRate,
            annualExpenses);
        return true;
    }

    private void UpdateProjectionChart(StandardFireResult result)
    {
        ProjectionSeries =
        [
            CreateProjectionSeries("Portfolio", result.Projections.Select(point => point.Portfolio), new SKColor(47, 107, 87)),
            CreateProjectionSeries("Today's dollars", result.Projections.Select(point => point.InflationAdjusted), new SKColor(84, 112, 104)),
            CreateProjectionSeries("FIRE target", Enumerable.Repeat(result.FireNumber, result.Projections.Count), new SKColor(201, 119, 39))
        ];
        ProjectionXAxes =
        [
            CreateLabelledAxis("Age", result.Projections.Select(point => point.Age.ToString("0", CultureInfo.CurrentCulture)))
        ];
        ProjectionChartDescription = $"Portfolio projection from age {result.Projections[0].Age:0} through age {result.Projections[^1].Age:0}. "
            + $"The portfolio starts at {FormatCurrency(result.Projections[0].Portfolio)} and the FIRE target is {FormatCurrency(result.FireNumber)}.";
    }
}
