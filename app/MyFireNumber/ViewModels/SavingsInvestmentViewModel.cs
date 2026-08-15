using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyFireNumber.Core.Calculations;
using MyFireNumber.Services;
using SkiaSharp;
using System.Globalization;

namespace MyFireNumber.ViewModels;

public sealed partial class SavingsInvestmentViewModel : CalculatorViewModelBase<SavingsInvestmentDraft>
{
    private readonly ISavingsInvestmentExportService exportService;

    public SavingsInvestmentViewModel(
        CalculatorViewModelServices services,
        ISavingsInvestmentExportService exportService)
        : base(services)
    {
        this.exportService = exportService;
    }

    [ObservableProperty]
    private string currentAgeText = string.Empty;

    [ObservableProperty]
    private string yearsInvestingText = string.Empty;

    [ObservableProperty]
    private string startingAmountText = string.Empty;

    [ObservableProperty]
    private string investmentContributionText = string.Empty;

    [ObservableProperty]
    private string investmentAnnualIncomeText = string.Empty;

    [ObservableProperty]
    private string expectedReturnText = string.Empty;

    [ObservableProperty]
    private string inflationRateText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMonthlyContribution))]
    [NotifyPropertyChangedFor(nameof(IsYearlyContribution))]
    [NotifyPropertyChangedFor(nameof(ContributionAmountHeader))]
    private ContributionFrequency contributionFrequency = ContributionFrequency.Monthly;

    [ObservableProperty]
    private string investmentSavingsRateText = string.Empty;

    [ObservableProperty]
    private string investmentAnnualContributionText = string.Empty;

    [ObservableProperty]
    private string investmentFinalBalanceText = string.Empty;

    [ObservableProperty]
    private string investmentInflationAdjustedText = string.Empty;

    [ObservableProperty]
    private string investmentGrowthText = string.Empty;

    [ObservableProperty]
    private string investmentCategoryText = string.Empty;

    [ObservableProperty]
    private string investmentProjectionSummary = string.Empty;

    public bool IsMonthlyContribution => ContributionFrequency == ContributionFrequency.Monthly;

    public bool IsYearlyContribution => ContributionFrequency == ContributionFrequency.Yearly;

    /// <summary>
    /// Names the period on the contribution field so the amount can never be read as the wrong cadence.
    /// </summary>
    public string ContributionAmountHeader => IsMonthlyContribution
        ? "Monthly contribution amount"
        : "Yearly contribution amount";

    protected override string CalculatorId => "savings-rate";

    protected override int DraftPayloadVersion => SavingsInvestmentDraft.PayloadVersion;

    protected override SavingsInvestmentDraft DefaultDraft => CalculatorDefaults.SavingsInvestment;

    protected override string DefaultPlanName => "My Investment Plan";

    protected override string ExportSuccessMessage => "Your Savings & Investment Rate workbook is ready to share.";

    protected override string ExportFailureMessage => "The Savings & Investment Rate workbook could not be created locally.";

    partial void OnCurrentAgeTextChanged(string value) => OnDraftInputChanged();
    partial void OnYearsInvestingTextChanged(string value) => OnDraftInputChanged();
    partial void OnStartingAmountTextChanged(string value) => OnDraftInputChanged();
    partial void OnInvestmentContributionTextChanged(string value) => OnDraftInputChanged();
    partial void OnInvestmentAnnualIncomeTextChanged(string value) => OnDraftInputChanged();
    partial void OnExpectedReturnTextChanged(string value) => OnDraftInputChanged();
    partial void OnInflationRateTextChanged(string value) => OnDraftInputChanged();
    partial void OnContributionFrequencyChanged(ContributionFrequency value) => OnDraftInputChanged();

    [RelayCommand]
    private void SetContributionFrequency(string frequency)
    {
        ContributionFrequency = string.Equals(frequency, "yearly", StringComparison.OrdinalIgnoreCase)
            ? ContributionFrequency.Yearly
            : ContributionFrequency.Monthly;
    }

    protected override void ApplyDraft(SavingsInvestmentDraft draft)
    {
        StartingAmountText = FormatNumber(draft.StartingAmount);
        InvestmentContributionText = FormatNumber(draft.ContributionAmount);
        ContributionFrequency = draft.ContributionFrequency;
        YearsInvestingText = draft.YearsInvesting.ToString(CultureInfo.CurrentCulture);
        ExpectedReturnText = FormatNumber(draft.ExpectedReturn * 100);
        InflationRateText = FormatNumber(draft.InflationRate * 100);
        InvestmentAnnualIncomeText = FormatNumber(draft.AnnualIncome);
        CurrentAgeText = draft.CurrentAge.ToString(CultureInfo.CurrentCulture);
    }

    protected override bool TryBuildDraft(out SavingsInvestmentDraft draft)
    {
        draft = DefaultDraft;
        if (!TryParseWholeNumber(CurrentAgeText, out var currentAge) || currentAge is < 18 or > 100)
        {
            ValidationMessage = "Enter a current age from 18 to 100.";
            return false;
        }

        if (!TryParseWholeNumber(YearsInvestingText, out var yearsInvesting) || yearsInvesting is < 1 or > 60)
        {
            ValidationMessage = "Enter an investment timeline from 1 to 60 years.";
            return false;
        }

        if (!TryParseNonNegative(StartingAmountText, out var startingAmount)
            || !TryParseNonNegative(InvestmentContributionText, out var contributionAmount)
            || !TryParseNonNegative(InvestmentAnnualIncomeText, out var annualIncome))
        {
            ValidationMessage = "Enter zero or a positive amount for each dollar value.";
            return false;
        }

        if (!TryParsePercentage(ExpectedReturnText, 0, 15, out var expectedReturn)
            || !TryParsePercentage(InflationRateText, 0, 10, out var inflationRate))
        {
            ValidationMessage = "Expected return must be 0% to 15% and inflation 0% to 10%.";
            return false;
        }

        draft = new SavingsInvestmentDraft(startingAmount, contributionAmount, ContributionFrequency, yearsInvesting, expectedReturn, inflationRate, annualIncome, currentAge);
        return true;
    }

    protected override void Recalculate(SavingsInvestmentDraft draft)
    {
        var result = FinancialCalculator.CalculateInvestmentGrowth(draft.ToInputs());
        InvestmentSavingsRateText = result.SavingsRate.ToString("P1", CultureInfo.CurrentCulture);
        InvestmentAnnualContributionText = FormatCurrency(result.AnnualContribution);
        InvestmentFinalBalanceText = FormatCurrency(result.FinalNominalBalance);
        InvestmentInflationAdjustedText = FormatCurrency(result.FinalInflationAdjustedBalance);
        InvestmentGrowthText = FormatCurrency(result.TotalGrowth);
        InvestmentCategoryText = result.SavingsCategory;
        InvestmentProjectionSummary = $"After {draft.YearsInvesting} years, your portfolio is projected to reach {FormatCurrency(result.FinalNominalBalance)}; that is {FormatCurrency(result.FinalInflationAdjustedBalance)} in today's dollars.";
        UpdateProjectionChart(result);
    }

    protected override async Task ShareAsync(SavingsInvestmentDraft draft)
    {
        await exportService.ShareAsync(draft, FinancialCalculator.CalculateInvestmentGrowth(draft.ToInputs()));
    }

    private void UpdateProjectionChart(InvestmentGrowthResult result)
    {
        ProjectionSeries =
        [
            CreateProjectionSeries("Portfolio", result.Projections.Select(point => point.Portfolio), new SKColor(72, 93, 165)),
            CreateProjectionSeries("Today's dollars", result.Projections.Select(point => point.InflationAdjusted), new SKColor(84, 112, 104)),
            CreateProjectionSeries("Total invested", result.Projections.Select(point => point.TotalContributions), new SKColor(201, 119, 39))
        ];
        ProjectionXAxes =
        [
            CreateLabelledAxis("Age", result.Projections.Select(point => point.Age.ToString("0", CultureInfo.CurrentCulture)))
        ];
        ProjectionChartDescription = $"Investment growth projection from age {result.Projections[0].Age:0} through age {result.Projections[^1].Age:0}. "
            + $"The projected final portfolio is {FormatCurrency(result.FinalNominalBalance)}.";
    }
}
