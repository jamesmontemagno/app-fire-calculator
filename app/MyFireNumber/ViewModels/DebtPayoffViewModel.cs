using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using MyFireNumber.Core.Calculations;
using MyFireNumber.Services;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;

namespace MyFireNumber.ViewModels;

public sealed partial class DebtPayoffViewModel : CalculatorViewModelBase<DebtPayoffDraft>
{
    private readonly IDebtPayoffExportService exportService;

    public DebtPayoffViewModel(
        CalculatorViewModelServices services,
        IDebtPayoffExportService exportService)
        : base(services)
    {
        this.exportService = exportService;
        DebtItems.CollectionChanged += OnDebtItemsChanged;
    }

    public ObservableCollection<DebtEditorItem> DebtItems { get; } = [];

    [ObservableProperty]
    private string debtMonthlyBudgetText = string.Empty;

    [ObservableProperty]
    private string debtExtraPaymentText = string.Empty;

    [ObservableProperty]
    private string debtTargetMonthsText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFixedDebtPayoff), nameof(IsTargetDebtPayoff))]
    private DebtPayoffMode debtPayoffMode = DebtPayoffMode.FixedBudget;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSnowballStrategy), nameof(IsAvalancheStrategy))]
    private DebtPayoffStrategy debtPayoffStrategy = DebtPayoffStrategy.Snowball;

    [ObservableProperty]
    private string debtTotalText = string.Empty;

    [ObservableProperty]
    private string debtMinimumPaymentsText = string.Empty;

    [ObservableProperty]
    private string debtPayoffTimeText = string.Empty;

    [ObservableProperty]
    private string debtInterestText = string.Empty;

    [ObservableProperty]
    private string debtPaymentText = string.Empty;

    [ObservableProperty]
    private string debtStrategySummary = string.Empty;

    [ObservableProperty]
    private string debtSnowballComparisonText = string.Empty;

    [ObservableProperty]
    private string debtAvalancheComparisonText = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<ISeries> debtBreakdownSeries = [];

    [ObservableProperty]
    private string debtBreakdownDescription = string.Empty;

    [ObservableProperty]
    private string debtBreakdownSummary = string.Empty;

    public bool IsFixedDebtPayoff => DebtPayoffMode == DebtPayoffMode.FixedBudget;

    public bool IsTargetDebtPayoff => DebtPayoffMode == DebtPayoffMode.TargetTimeline;
    public bool IsSnowballStrategy => DebtPayoffStrategy == DebtPayoffStrategy.Snowball;
    public bool IsAvalancheStrategy => DebtPayoffStrategy == DebtPayoffStrategy.Avalanche;

    protected override string CalculatorId => "debt-payoff";

    protected override int DraftPayloadVersion => DebtPayoffDraft.PayloadVersion;

    protected override DebtPayoffDraft DefaultDraft => DebtPayoffDraft.Default;

    protected override string DefaultPlanName => "My Debt Payoff Plan";

    protected override string ExportSuccessMessage => "Your Debt Payoff workbook is ready to share.";

    protected override string ExportFailureMessage => "The Debt Payoff workbook could not be created locally.";

    partial void OnDebtMonthlyBudgetTextChanged(string value) => OnDraftInputChanged();
    partial void OnDebtExtraPaymentTextChanged(string value) => OnDraftInputChanged();
    partial void OnDebtTargetMonthsTextChanged(string value) => OnDraftInputChanged();
    partial void OnDebtPayoffModeChanged(DebtPayoffMode value) => OnDraftInputChanged();
    partial void OnDebtPayoffStrategyChanged(DebtPayoffStrategy value) => OnDraftInputChanged();

    [RelayCommand]
    private void AddDebt()
    {
        if (IsLinkedProfile)
        {
            return;
        }

        DebtItems.Add(new DebtEditorItem
        {
            Name = "New debt",
            BalanceText = "1000",
            RateText = "19.99",
            MinimumPaymentText = "50"
        });
    }

    [RelayCommand]
    private void RemoveDebt(DebtEditorItem? debt)
    {
        if (!IsLinkedProfile && debt is not null)
        {
            DebtItems.Remove(debt);
        }
    }

    [RelayCommand]
    private void SetDebtStrategy(string strategy)
    {
        DebtPayoffStrategy = string.Equals(strategy, "avalanche", StringComparison.OrdinalIgnoreCase)
            ? DebtPayoffStrategy.Avalanche
            : DebtPayoffStrategy.Snowball;
    }

    [RelayCommand]
    private void SetDebtPayoffMode(string mode)
    {
        DebtPayoffMode = string.Equals(mode, "target", StringComparison.OrdinalIgnoreCase)
            ? DebtPayoffMode.TargetTimeline
            : DebtPayoffMode.FixedBudget;
    }

    protected override void ApplyDraft(DebtPayoffDraft draft)
    {
        ReplaceDebtItems(draft.Debts);
        DebtMonthlyBudgetText = FormatNumber(draft.MonthlyBudget);
        DebtExtraPaymentText = FormatNumber(draft.ExtraPayment);
        DebtTargetMonthsText = draft.TargetMonths.ToString(CultureInfo.CurrentCulture);
        DebtPayoffMode = draft.Mode;
        DebtPayoffStrategy = draft.Strategy;
    }

    protected override bool TryBuildDraft(out DebtPayoffDraft draft)
    {
        draft = DebtPayoffDraft.Default;
        var debts = new List<DebtItem>();
        foreach (var debtItem in DebtItems)
        {
            if (!debtItem.TryCreateDebt(out var debt))
            {
                ValidationMessage = "Every debt needs a name, positive balance, interest rate, and minimum payment.";
                return false;
            }

            debts.Add(debt);
        }

        if (debts.Count == 0)
        {
            ValidationMessage = "Add at least one debt to calculate a payoff strategy.";
            return false;
        }

        if (!TryParseNonNegative(DebtMonthlyBudgetText, out var monthlyBudget)
            || !TryParseNonNegative(DebtExtraPaymentText, out var extraPayment)
            || !TryParseWholeNumber(DebtTargetMonthsText, out var targetMonths)
            || targetMonths is < 1 or > 360)
        {
            ValidationMessage = "Enter positive debt payments and a payoff timeline from 1 to 360 months.";
            return false;
        }

        draft = new DebtPayoffDraft(debts, monthlyBudget, extraPayment, targetMonths, DebtPayoffMode, DebtPayoffStrategy);
        return true;
    }

    protected override void Recalculate(DebtPayoffDraft draft)
    {
        var totalMinimumPayments = draft.Debts.Sum(debt => debt.MinimumPayment);
        if (!TryCalculate(draft, totalMinimumPayments, out var result))
        {
            return;
        }

        ValidationMessage = string.Empty;
        DebtTotalText = FormatCurrency(draft.Debts.Sum(debt => debt.Balance));
        DebtMinimumPaymentsText = FormatCurrency(totalMinimumPayments);
        DebtPayoffTimeText = $"{result.TotalMonths} months";
        DebtInterestText = FormatCurrency(result.TotalInterest);
        DebtPaymentText = FormatCurrency(result.MonthlyPayment);
        DebtStrategySummary = draft.Strategy == DebtPayoffStrategy.Snowball
            ? $"Snowball pays the smallest balance first. Your payoff order is {string.Join(", ", result.PayoffOrder)}."
            : $"Avalanche pays the highest interest rate first. Your payoff order is {string.Join(", ", result.PayoffOrder)}.";
        var comparisonPayment = result.MonthlyPayment;
        var snowball = FinancialCalculator.CalculateSnowballPayoff(draft.Debts, comparisonPayment);
        var avalanche = FinancialCalculator.CalculateAvalanchePayoff(draft.Debts, comparisonPayment);
        DebtSnowballComparisonText = $"{snowball.TotalMonths} months, {FormatCurrency(snowball.TotalInterest)} interest";
        DebtAvalancheComparisonText = $"{avalanche.TotalMonths} months, {FormatCurrency(avalanche.TotalInterest)} interest";
        UpdateProjectionChart(result);
        UpdateBreakdownChart(result);
    }

    protected override async Task ShareAsync(DebtPayoffDraft draft)
    {
        var totalMinimumPayments = draft.Debts.Sum(debt => debt.MinimumPayment);
        DebtPayoffResult result;
        if (draft.Mode == DebtPayoffMode.TargetTimeline)
        {
            result = FinancialCalculator.CalculateDebtPayoffByTimeline(
                draft.Debts,
                draft.TargetMonths,
                draft.Strategy == DebtPayoffStrategy.Snowball,
                draft.ExtraPayment)?.Result
                ?? throw new InvalidOperationException("A payoff timeline could not be calculated for these debts.");
        }
        else
        {
            if (draft.MonthlyBudget < totalMinimumPayments)
            {
                throw new InvalidOperationException("Monthly budget must cover minimum payments.");
            }

            result = draft.Strategy == DebtPayoffStrategy.Snowball
                ? FinancialCalculator.CalculateSnowballPayoff(draft.Debts, draft.MonthlyBudget, draft.ExtraPayment)
                : FinancialCalculator.CalculateAvalanchePayoff(draft.Debts, draft.MonthlyBudget, draft.ExtraPayment);
        }

        await exportService.ShareAsync(draft, result);
    }

    private bool TryCalculate(DebtPayoffDraft draft, double totalMinimumPayments, out DebtPayoffResult result)
    {
        if (draft.Mode == DebtPayoffMode.TargetTimeline)
        {
            var timeline = FinancialCalculator.CalculateDebtPayoffByTimeline(
                draft.Debts,
                draft.TargetMonths,
                draft.Strategy == DebtPayoffStrategy.Snowball,
                draft.ExtraPayment);
            if (timeline is null)
            {
                ValidationMessage = "A payoff timeline could not be calculated for these debts.";
                result = default!;
                return false;
            }

            result = timeline.Result;
            return true;
        }

        if (draft.MonthlyBudget < totalMinimumPayments)
        {
            ValidationMessage = $"Monthly budget must be at least {FormatCurrency(totalMinimumPayments)} to cover minimum payments.";
            result = default!;
            return false;
        }

        result = draft.Strategy == DebtPayoffStrategy.Snowball
            ? FinancialCalculator.CalculateSnowballPayoff(draft.Debts, draft.MonthlyBudget, draft.ExtraPayment)
            : FinancialCalculator.CalculateAvalanchePayoff(draft.Debts, draft.MonthlyBudget, draft.ExtraPayment);
        return true;
    }

    private void UpdateProjectionChart(DebtPayoffResult result)
    {
        ProjectionSeries =
        [
            CreateProjectionSeries("Debt balance", result.Projections.Select(month => month.TotalBalance), new SKColor(190, 81, 66))
        ];
        ProjectionXAxes =
        [
            CreateLabelledAxis(
                "Month",
                result.Projections
                    .Where((_, index) => index % Math.Max(1, result.Projections.Count / 6) == 0)
                    .Select(month => month.Month.ToString(CultureInfo.CurrentCulture)))
        ];
        ProjectionChartDescription = $"Debt balance projection over {result.TotalMonths} months. Total interest is {FormatCurrency(result.TotalInterest)}.";
    }

    private void UpdateBreakdownChart(DebtPayoffResult result)
    {
        DebtBreakdownSeries =
        [
            CreateStackedAreaSeries("Principal paid", result.Projections.Select(month => month.CumulativePrincipal), new SKColor(16, 185, 129)),
            CreateStackedAreaSeries("Interest paid", result.Projections.Select(month => month.CumulativeInterest), new SKColor(239, 68, 68))
        ];
        DebtBreakdownDescription = $"Cumulative principal and interest paid over {result.TotalMonths} months.";
        DebtBreakdownSummary = $"Across the payoff plan, {FormatCurrency(result.TotalPrincipal)} goes to principal and {FormatCurrency(result.TotalInterest)} goes to interest.";
    }

    private void OnDebtItemsChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        if (eventArgs.OldItems is not null)
        {
            foreach (DebtEditorItem debt in eventArgs.OldItems)
            {
                debt.Changed -= OnDebtItemChanged;
            }
        }

        if (eventArgs.NewItems is not null)
        {
            foreach (DebtEditorItem debt in eventArgs.NewItems)
            {
                debt.Changed += OnDebtItemChanged;
            }
        }

        OnDraftInputChanged();
    }

    private void OnDebtItemChanged(object? sender, EventArgs eventArgs) => OnDraftInputChanged();

    private void ReplaceDebtItems(IReadOnlyList<DebtItem> debts)
    {
        DebtItems.Clear();
        foreach (var debt in debts)
        {
            var editor = DebtEditorItem.FromDebt(debt);
            editor.IsReadOnly = IsLinkedProfile;
            DebtItems.Add(editor);
        }
    }
}
