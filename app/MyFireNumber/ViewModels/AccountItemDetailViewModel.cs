using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using MyFireNumber.Services;
using SkiaSharp;
using System.Globalization;

namespace MyFireNumber.ViewModels;

/// <summary>
/// Shows one account's or debt's current value alongside its check-in history: a line chart over a
/// selectable time range plus a plain-language summary of the change, minimum, and maximum. Populated
/// entirely from the <see cref="AccountItemDetailArgs"/> navigation payload built by
/// <see cref="AccountsViewModel"/> - no separate storage read.
/// </summary>
public sealed partial class AccountItemDetailViewModel(
    ICurrencyPreferencesService currencyPreferencesService,
    IAppBehaviorPreferencesService behaviorPreferencesService) : ObservableObject, IQueryAttributable
{
    private static readonly SKColor LineColor = new(51, 117, 171);

    private IReadOnlyList<AccountItemHistoryPoint> allHistory = [];

    [ObservableProperty] private string itemName = string.Empty;
    [ObservableProperty] private string itemTypeLabel = string.Empty;
    [ObservableProperty] private bool isDebt;
    [ObservableProperty] private string currentBalanceText = string.Empty;
    [ObservableProperty] private string freshnessText = string.Empty;
    [ObservableProperty] private bool isOverdue;

    [ObservableProperty] private HistoryRange selectedRange = HistoryRange.OneYear;

    [ObservableProperty] private bool hasHistory;
    [ObservableProperty] private bool hasNoHistory = true;
    [ObservableProperty] private bool hasTrendData;

    [ObservableProperty] private IReadOnlyList<ISeries> series = [];
    [ObservableProperty] private Axis[] xAxes = [];
    [ObservableProperty] private string summaryText = string.Empty;
    [ObservableProperty] private string rangeDescription = string.Empty;

    public TimeSpan ChartAnimationsSpeed => behaviorPreferencesService.Current.ReduceMotion
        ? TimeSpan.Zero
        : TimeSpan.FromMilliseconds(800);

    partial void OnSelectedRangeChanged(HistoryRange value) => UpdateChart();

    partial void OnHasHistoryChanged(bool value) => HasNoHistory = !value;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("details", out var value) || value is not AccountItemDetailArgs args)
        {
            return;
        }

        ItemName = args.ItemName;
        ItemTypeLabel = args.ItemTypeLabel;
        IsDebt = args.IsDebt;
        CurrentBalanceText = currencyPreferencesService.Format(args.CurrentBalance);
        FreshnessText = args.FreshnessText;
        IsOverdue = args.IsOverdue;
        allHistory = args.History;
        HasHistory = allHistory.Count > 0;
        UpdateChart();
    }

    [RelayCommand]
    private void SetRange(HistoryRange range) => SelectedRange = range;

    private void UpdateChart()
    {
        var filtered = FilterByRange(allHistory, SelectedRange);
        HasTrendData = filtered.Count > 0;

        if (filtered.Count == 0)
        {
            Series = [];
            XAxes = [];
            SummaryText = "No check-ins yet in this time range.";
            RangeDescription = SummaryText;
            return;
        }

        var points = filtered.Select(point => new DateTimePoint(point.CompletedAtUtc, point.Balance)).ToArray();
        Series = [CreateLineSeries(points)];
        XAxes = [CreateDateTimeAxis(SelectedRange)];

        var first = filtered[0];
        var last = filtered[^1];
        var min = filtered.Min(point => point.Balance);
        var max = filtered.Max(point => point.Balance);
        var change = last.Balance - first.Balance;

        SummaryText = filtered.Count == 1
            ? $"One check-in on {first.CompletedAtUtc:MMM d, yyyy}: {FormatCurrency(first.Balance)}."
            : change switch
            {
                > 0 => $"Up {FormatCurrency(change)} from {FormatCurrency(first.Balance)} on {first.CompletedAtUtc:MMM d, yyyy} to {FormatCurrency(last.Balance)} on {last.CompletedAtUtc:MMM d, yyyy}.",
                < 0 => $"Down {FormatCurrency(Math.Abs(change))} from {FormatCurrency(first.Balance)} on {first.CompletedAtUtc:MMM d, yyyy} to {FormatCurrency(last.Balance)} on {last.CompletedAtUtc:MMM d, yyyy}.",
                _ => $"Unchanged at {FormatCurrency(last.Balance)} from {first.CompletedAtUtc:MMM d, yyyy} to {last.CompletedAtUtc:MMM d, yyyy}."
            };

        RangeDescription = $"Range low {FormatCurrency(min)}, range high {FormatCurrency(max)}.";
    }

    private static IReadOnlyList<AccountItemHistoryPoint> FilterByRange(
        IReadOnlyList<AccountItemHistoryPoint> history,
        HistoryRange range)
    {
        if (range == HistoryRange.All)
        {
            return history;
        }

        var cutoff = range switch
        {
            HistoryRange.ThreeMonths => DateTime.UtcNow.AddMonths(-3),
            HistoryRange.SixMonths => DateTime.UtcNow.AddMonths(-6),
            _ => DateTime.UtcNow.AddYears(-1)
        };

        return [.. history.Where(point => point.CompletedAtUtc >= cutoff)];
    }

    private LineSeries<DateTimePoint> CreateLineSeries(DateTimePoint[] points) => new()
    {
        Name = ItemName,
        Values = points,
        GeometrySize = 6,
        Fill = null,
        Stroke = new SolidColorPaint(LineColor) { StrokeThickness = 3 },
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

    private string FormatCurrency(double amount) => currencyPreferencesService.Format(amount);
}
