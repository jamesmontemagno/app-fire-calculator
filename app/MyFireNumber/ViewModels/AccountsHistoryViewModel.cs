using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Profile;
using MyFireNumber.Services;
using MyFireNumber.Storage;
using SkiaSharp;
using System.Globalization;

namespace MyFireNumber.ViewModels;

/// <summary>How far back the history charts look. "All" is unbounded.</summary>
public enum HistoryRange
{
    ThreeMonths,
    SixMonths,
    OneYear,
    All
}

/// <summary>
/// Charts built from saved <see cref="FinancialCheckIn"/> snapshots: net worth trend, assets vs
/// debts, per-account trends, current allocation, income vs expenses, and cash flow. Every "over
/// time" chart is empty until at least one check-in exists, and shows a plain-language empty state
/// instead of an empty plot.
/// </summary>
public sealed partial class AccountsHistoryViewModel(
    IFinancialCheckInRepository checkInRepository,
    IProfileAccountRepository profileAccountRepository,
    ICurrencyPreferencesService currencyPreferencesService,
    IAppBehaviorPreferencesService behaviorPreferencesService) : ObservableObject
{
    // Matches the palette RetirementCashFlowViewModel uses for its per-account bucket chart, so an
    // account's color reads consistently between the linked calculator and this history view.
    private static readonly SKColor[] AccountColors =
    [
        new(139, 92, 246),
        new(14, 165, 233),
        new(20, 184, 166),
        new(245, 158, 11),
        new(236, 72, 153),
        new(132, 204, 22),
        new(249, 115, 22),
        new(99, 102, 241)
    ];

    private static readonly SKColor AssetsColor = new(43, 111, 83);
    private static readonly SKColor DebtsColor = new(190, 81, 66);
    private static readonly SKColor NetWorthColor = new(51, 117, 171);
    private static readonly SKColor IncomeColor = new(72, 93, 165);
    private static readonly SKColor ExpensesColor = new(201, 119, 39);
    private static readonly SKColor CashFlowColor = new(20, 184, 166);

    private IReadOnlyList<FinancialCheckIn> allCheckIns = [];
    private IReadOnlyList<RetirementAccount> currentAccounts = [];
    private bool isLoaded;

    [ObservableProperty] private HistoryRange selectedRange = HistoryRange.OneYear;

    [ObservableProperty] private bool hasHistory;
    [ObservableProperty] private bool hasNoHistory = true;
    [ObservableProperty] private bool hasTrendData;
    [ObservableProperty] private bool hasAccountTrendData;
    [ObservableProperty] private bool hasAllocationData;

    [ObservableProperty] private IReadOnlyList<ISeries> netWorthSeries = [];
    [ObservableProperty] private Axis[] netWorthXAxes = [];
    [ObservableProperty] private string netWorthDescription = string.Empty;

    [ObservableProperty] private IReadOnlyList<ISeries> assetsVsDebtsSeries = [];
    [ObservableProperty] private Axis[] assetsVsDebtsXAxes = [];
    [ObservableProperty] private string assetsVsDebtsDescription = string.Empty;

    [ObservableProperty] private IReadOnlyList<ISeries> accountTrendsSeries = [];
    [ObservableProperty] private Axis[] accountTrendsXAxes = [];
    [ObservableProperty] private string accountTrendsDescription = string.Empty;

    [ObservableProperty] private IReadOnlyList<ISeries> allocationSeries = [];
    [ObservableProperty] private string allocationDescription = string.Empty;

    [ObservableProperty] private IReadOnlyList<ISeries> incomeVsExpensesSeries = [];
    [ObservableProperty] private Axis[] incomeVsExpensesXAxes = [];
    [ObservableProperty] private string incomeVsExpensesDescription = string.Empty;

    [ObservableProperty] private IReadOnlyList<ISeries> cashFlowSeries = [];
    [ObservableProperty] private Axis[] cashFlowXAxes = [];
    [ObservableProperty] private string cashFlowDescription = string.Empty;

    public TimeSpan ChartAnimationsSpeed => behaviorPreferencesService.Current.ReduceMotion
        ? TimeSpan.Zero
        : TimeSpan.FromMilliseconds(800);

    partial void OnSelectedRangeChanged(HistoryRange value) => UpdateCharts();

    partial void OnHasHistoryChanged(bool value) => HasNoHistory = !value;

    public async Task LoadAsync()
    {
        if (isLoaded)
        {
            return;
        }

        allCheckIns = await checkInRepository.ListAsync();
        currentAccounts = await profileAccountRepository.ListAsync();
        HasHistory = allCheckIns.Count > 0;
        UpdateCharts();
        isLoaded = true;
    }

    [RelayCommand]
    private void SetRange(HistoryRange range) => SelectedRange = range;

    private void UpdateCharts()
    {
        var filtered = FilterByRange(allCheckIns, SelectedRange);
        HasTrendData = filtered.Count > 0;

        UpdateNetWorthChart(filtered);
        UpdateAssetsVsDebtsChart(filtered);
        UpdateAccountTrendsChart(filtered);
        UpdateAllocationChart();
        UpdateIncomeVsExpensesChart(filtered);
        UpdateCashFlowChart(filtered);
    }

    private static IReadOnlyList<FinancialCheckIn> FilterByRange(IReadOnlyList<FinancialCheckIn> checkIns, HistoryRange range)
    {
        if (range == HistoryRange.All)
        {
            return checkIns;
        }

        var cutoff = range switch
        {
            HistoryRange.ThreeMonths => DateTime.UtcNow.AddMonths(-3),
            HistoryRange.SixMonths => DateTime.UtcNow.AddMonths(-6),
            _ => DateTime.UtcNow.AddYears(-1)
        };

        return [.. checkIns.Where(checkIn => checkIn.CompletedAtUtc >= cutoff)];
    }

    private void UpdateNetWorthChart(IReadOnlyList<FinancialCheckIn> checkIns)
    {
        if (checkIns.Count == 0)
        {
            NetWorthSeries = [];
            NetWorthXAxes = [];
            NetWorthDescription = "No check-ins yet in this time range.";
            return;
        }

        var points = checkIns.Select(checkIn => new DateTimePoint(checkIn.CompletedAtUtc, checkIn.NetWorth)).ToArray();
        NetWorthSeries = [CreateLineSeries("Net worth", points, NetWorthColor)];
        NetWorthXAxes = [CreateDateTimeAxis(SelectedRange)];
        var first = checkIns[0];
        var last = checkIns[^1];
        NetWorthDescription = checkIns.Count == 1
            ? $"One check-in on {first.CompletedAtUtc:MMM d, yyyy}: net worth {FormatCurrency(first.NetWorth)}."
            : $"Net worth from {FormatCurrency(first.NetWorth)} on {first.CompletedAtUtc:MMM d, yyyy} to {FormatCurrency(last.NetWorth)} on {last.CompletedAtUtc:MMM d, yyyy}.";
    }

    private void UpdateAssetsVsDebtsChart(IReadOnlyList<FinancialCheckIn> checkIns)
    {
        if (checkIns.Count == 0)
        {
            AssetsVsDebtsSeries = [];
            AssetsVsDebtsXAxes = [];
            AssetsVsDebtsDescription = "No check-ins yet in this time range.";
            return;
        }

        var assetPoints = checkIns.Select(checkIn => new DateTimePoint(checkIn.CompletedAtUtc, checkIn.TotalAssets)).ToArray();
        var debtPoints = checkIns.Select(checkIn => new DateTimePoint(checkIn.CompletedAtUtc, checkIn.TotalDebts)).ToArray();
        AssetsVsDebtsSeries =
        [
            CreateLineSeries("Assets", assetPoints, AssetsColor),
            CreateLineSeries("Debts", debtPoints, DebtsColor)
        ];
        AssetsVsDebtsXAxes = [CreateDateTimeAxis(SelectedRange)];
        var last = checkIns[^1];
        AssetsVsDebtsDescription = $"As of {last.CompletedAtUtc:MMM d, yyyy}, assets are {FormatCurrency(last.TotalAssets)} and debts are {FormatCurrency(last.TotalDebts)}.";
    }

    private void UpdateAccountTrendsChart(IReadOnlyList<FinancialCheckIn> checkIns)
    {
        // Each account keeps its own line even if it was missing from some check-ins (never
        // confirmed that month, or added later); the label uses the most recent name recorded so a
        // rename doesn't split one account's history into two legend entries.
        var accountIds = checkIns
            .SelectMany(checkIn => checkIn.Accounts)
            .Select(account => account.AccountId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        HasAccountTrendData = accountIds.Length > 0;
        if (accountIds.Length == 0)
        {
            AccountTrendsSeries = [];
            AccountTrendsXAxes = [];
            AccountTrendsDescription = "No account balances recorded yet in this time range.";
            return;
        }

        var series = new List<ISeries>(accountIds.Length);
        for (var index = 0; index < accountIds.Length; index++)
        {
            var accountId = accountIds[index];
            var entries = checkIns
                .SelectMany(checkIn => checkIn.Accounts.Where(account => account.AccountId == accountId)
                    .Select(account => (checkIn.CompletedAtUtc, account.Name, account.Balance)))
                .OrderBy(entry => entry.CompletedAtUtc)
                .ToArray();

            if (entries.Length == 0)
            {
                continue;
            }

            var points = entries.Select(entry => new DateTimePoint(entry.CompletedAtUtc, entry.Balance)).ToArray();
            var label = entries[^1].Name;
            series.Add(CreateLineSeries(label, points, AccountColors[index % AccountColors.Length]));
        }

        AccountTrendsSeries = series;
        AccountTrendsXAxes = [CreateDateTimeAxis(SelectedRange)];
        AccountTrendsDescription = $"Balance history for {CountLabel(accountIds.Length, "account")} tracked by monthly check-ins.";
    }

    private void UpdateAllocationChart()
    {
        // Allocation is a point-in-time concept, so it always reflects the accounts as they stand
        // today rather than a historical check-in, even while the other charts respect the range picker.
        var groups = currentAccounts
            .GroupBy(account => account.Type)
            .Select(group => (Type: group.Key, Balance: group.Sum(account => account.Balance)))
            .Where(group => group.Balance > 0)
            .OrderByDescending(group => group.Balance)
            .ToArray();

        HasAllocationData = groups.Length > 0;
        if (groups.Length == 0)
        {
            AllocationSeries = [];
            AllocationDescription = "Add an account balance to see how your assets are allocated.";
            return;
        }

        AllocationSeries = groups
            .Select((group, index) => (ISeries)new PieSeries<double>
            {
                Name = group.Type.ToString(),
                Values = [group.Balance],
                Fill = new SolidColorPaint(AccountColors[index % AccountColors.Length]),
                ToolTipLabelFormatter = point => $"{group.Type}: {FormatCurrency(point.Coordinate.PrimaryValue)}"
            })
            .ToArray();

        var total = groups.Sum(group => group.Balance);
        AllocationDescription = string.Join(", ", groups.Select(group =>
            $"{group.Type} {FormatCurrency(group.Balance)} ({group.Balance / total:P0})"));
    }

    private void UpdateIncomeVsExpensesChart(IReadOnlyList<FinancialCheckIn> checkIns)
    {
        if (checkIns.Count == 0)
        {
            IncomeVsExpensesSeries = [];
            IncomeVsExpensesXAxes = [];
            IncomeVsExpensesDescription = "No check-ins yet in this time range.";
            return;
        }

        var incomePoints = checkIns.Select(checkIn => new DateTimePoint(checkIn.CompletedAtUtc, checkIn.AnnualIncome)).ToArray();
        var expensePoints = checkIns.Select(checkIn => new DateTimePoint(checkIn.CompletedAtUtc, checkIn.AnnualExpenses)).ToArray();
        IncomeVsExpensesSeries =
        [
            CreateLineSeries("Annual income", incomePoints, IncomeColor),
            CreateLineSeries("Annual expenses", expensePoints, ExpensesColor)
        ];
        IncomeVsExpensesXAxes = [CreateDateTimeAxis(SelectedRange)];
        var last = checkIns[^1];
        IncomeVsExpensesDescription = $"As of {last.CompletedAtUtc:MMM d, yyyy}, annual income is {FormatCurrency(last.AnnualIncome)} and annual expenses are {FormatCurrency(last.AnnualExpenses)}.";
    }

    private void UpdateCashFlowChart(IReadOnlyList<FinancialCheckIn> checkIns)
    {
        if (checkIns.Count == 0)
        {
            CashFlowSeries = [];
            CashFlowXAxes = [];
            CashFlowDescription = "No check-ins yet in this time range.";
            return;
        }

        var points = checkIns.Select(checkIn => new DateTimePoint(checkIn.CompletedAtUtc, checkIn.AnnualCashFlow)).ToArray();
        CashFlowSeries = [CreateLineSeries("Annual cash flow", points, CashFlowColor)];
        CashFlowXAxes = [CreateDateTimeAxis(SelectedRange)];
        var last = checkIns[^1];
        CashFlowDescription = $"As of {last.CompletedAtUtc:MMM d, yyyy}, annual cash flow is {FormatCurrency(last.AnnualCashFlow)}.";
    }

    private LineSeries<DateTimePoint> CreateLineSeries(string name, DateTimePoint[] points, SKColor color) => new()
    {
        Name = name,
        Values = points,
        GeometrySize = 6,
        Fill = null,
        Stroke = new SolidColorPaint(color) { StrokeThickness = 3 },
        YToolTipLabelFormatter = point => FormatCurrency(point.Coordinate.PrimaryValue)
    };

    private static Axis CreateDateTimeAxis(HistoryRange range)
    {
        var unit = range switch
        {
            HistoryRange.ThreeMonths => TimeSpan.FromDays(7),
            HistoryRange.SixMonths => TimeSpan.FromDays(14),
            _ => TimeSpan.FromDays(30)
        };

        return new DateTimeAxis(unit, date => date.ToString("MMM d", CultureInfo.CurrentCulture))
        {
            LabelsRotation = 0,
            TextSize = 10
        };
    }

    private static string CountLabel(int count, string singular) =>
        $"{count} {(count == 1 ? singular : $"{singular}s")}";

    private string FormatCurrency(double amount) => currencyPreferencesService.Format(amount);
}
