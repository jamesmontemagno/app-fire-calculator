using System.Collections;
using System.Globalization;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Maui;

namespace MyFireNumber.Views.Controls;

/// <summary>
/// Wraps a LiveCharts Cartesian chart with a pinned readout that is updated after a tap completes.
/// </summary>
public sealed class InteractiveCartesianChartView : ContentView
{
    private const string EmptySelectionText = "Tap the graph to pin exact values.";

    public static readonly BindableProperty SeriesProperty = BindableProperty.Create(
        nameof(Series),
        typeof(IReadOnlyList<ISeries>),
        typeof(InteractiveCartesianChartView),
        Array.Empty<ISeries>(),
        propertyChanged: (bindable, _, newValue) =>
        {
            var view = (InteractiveCartesianChartView)bindable;
            view.chart.Series = (IReadOnlyList<ISeries>?)newValue ?? Array.Empty<ISeries>();
            view.ClearSelection();
        });

    public static readonly BindableProperty XAxesProperty = BindableProperty.Create(
        nameof(XAxes),
        typeof(Axis[]),
        typeof(InteractiveCartesianChartView),
        Array.Empty<Axis>(),
        propertyChanged: (bindable, _, newValue) =>
        {
            var view = (InteractiveCartesianChartView)bindable;
            view.chart.XAxes = (Axis[]?)newValue ?? Array.Empty<Axis>();
            view.ClearSelection();
        });

    public static readonly BindableProperty AnimationsSpeedProperty = BindableProperty.Create(
        nameof(AnimationsSpeed),
        typeof(TimeSpan),
        typeof(InteractiveCartesianChartView),
        TimeSpan.FromMilliseconds(800),
        propertyChanged: (bindable, _, newValue) =>
        {
            ((InteractiveCartesianChartView)bindable).chart.AnimationsSpeed = (TimeSpan)newValue;
        });

    public static readonly BindableProperty ChartHeightProperty = BindableProperty.Create(
        nameof(ChartHeight),
        typeof(double),
        typeof(InteractiveCartesianChartView),
        300d,
        propertyChanged: (bindable, _, newValue) =>
        {
            ((InteractiveCartesianChartView)bindable).chart.HeightRequest = (double)newValue;
        });

    private readonly CartesianChart chart;
    private readonly Label detailLabel;

    public InteractiveCartesianChartView()
    {
        chart = new CartesianChart
        {
            HeightRequest = ChartHeight
        };

        detailLabel = new Label
        {
            LineBreakMode = LineBreakMode.WordWrap,
            Text = EmptySelectionText
        };
        detailLabel.SetDynamicResource(StyleProperty, "CalculatorSupportingText");

        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += OnChartTapped;
        chart.GestureRecognizers.Add(tapGesture);

        var layout = new VerticalStackLayout
        {
            Children =
            {
                chart,
                detailLabel
            }
        };
        layout.SetDynamicResource(VerticalStackLayout.SpacingProperty, "SpaceSm");
        Content = layout;
    }

    public IReadOnlyList<ISeries> Series
    {
        get => (IReadOnlyList<ISeries>)GetValue(SeriesProperty);
        set => SetValue(SeriesProperty, value);
    }

    public Axis[] XAxes
    {
        get => (Axis[])GetValue(XAxesProperty);
        set => SetValue(XAxesProperty, value);
    }

    public TimeSpan AnimationsSpeed
    {
        get => (TimeSpan)GetValue(AnimationsSpeedProperty);
        set => SetValue(AnimationsSpeedProperty, value);
    }

    public double ChartHeight
    {
        get => (double)GetValue(ChartHeightProperty);
        set => SetValue(ChartHeightProperty, value);
    }

    private void OnChartTapped(object? sender, TappedEventArgs args)
    {
        var position = args.GetPosition(chart);
        if (position is null)
        {
            return;
        }

        var seriesValues = Series
            .Select(series => (Series: series, Values: ToValueList(series.Values)))
            .Where(series => series.Values.Count > 0)
            .ToArray();

        if (seriesValues.Length == 0)
        {
            ClearSelection();
            return;
        }

        var pointCount = seriesValues.Max(series => series.Values.Count);
        var selectedIndex = GetNearestIndex(position.Value.X, pointCount);
        var label = GetXAxisLabel(selectedIndex, seriesValues);
        var lines = new List<string> { label };

        foreach (var (series, values) in seriesValues)
        {
            if (selectedIndex >= values.Count || TryFormatValue(values[selectedIndex]) is not { } value)
            {
                continue;
            }

            lines.Add($"{series.Name}: {value}");
        }

        detailLabel.Text = string.Join(Environment.NewLine, lines);
        SemanticProperties.SetDescription(this, detailLabel.Text);
    }

    private int GetNearestIndex(double pointerX, int pointCount)
    {
        if (pointCount <= 1 || chart.Width <= 0)
        {
            return 0;
        }

        var drawMarginLocation = chart.CoreChart.DrawMarginLocation;
        var drawMarginSize = chart.CoreChart.DrawMarginSize;
        var plotLeft = drawMarginLocation.X;
        var plotWidth = drawMarginSize.Width > 0
            ? drawMarginSize.Width
            : Math.Max(1, chart.Width);

        var density = DeviceDisplay.Current.MainDisplayInfo.Density;
        var scaledPointerX = pointerX * (density > 0 ? density : 1);
        var ratio = Math.Clamp((scaledPointerX - plotLeft) / plotWidth, 0, 1);
        return (int)Math.Round(ratio * (pointCount - 1), MidpointRounding.AwayFromZero);
    }

    private string GetXAxisLabel(int selectedIndex, IEnumerable<(ISeries Series, IReadOnlyList<object?> Values)> seriesValues)
    {
        var firstAxis = XAxes.FirstOrDefault();
        var labels = firstAxis?.Labels;
        if (labels is not null && selectedIndex < labels.Count)
        {
            var axisName = firstAxis?.Name;
            return string.IsNullOrWhiteSpace(axisName)
                ? labels[selectedIndex]
                : $"{axisName} {labels[selectedIndex]}";
        }

        var datePoint = seriesValues
            .Select(series => selectedIndex < series.Values.Count ? series.Values[selectedIndex] : null)
            .OfType<DateTimePoint>()
            .FirstOrDefault();

        return datePoint is not null
            ? datePoint.DateTime.ToString("MMM d, yyyy", CultureInfo.CurrentCulture)
            : $"Point {selectedIndex + 1}";
    }

    private static IReadOnlyList<object?> ToValueList(IEnumerable? values)
    {
        if (values is null)
        {
            return [];
        }

        return values.Cast<object?>().ToArray();
    }

    private static string? TryFormatValue(object? value)
    {
        var number = value switch
        {
            double doubleValue => doubleValue,
            float floatValue => floatValue,
            decimal decimalValue => (double)decimalValue,
            int intValue => intValue,
            long longValue => longValue,
            DateTimePoint { Value: { } pointValue } => pointValue,
            ObservablePoint { Y: { } pointValue } => pointValue,
            _ => (double?)null
        };

        return number?.ToString("C0", CultureInfo.CurrentCulture);
    }

    private void ClearSelection()
    {
        detailLabel.Text = EmptySelectionText;
        SemanticProperties.SetDescription(this, EmptySelectionText);
    }
}
