using System.Collections;
using System.Globalization;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel.Events;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Maui;

namespace MyFireNumber.Views.Controls;

/// <summary>
/// Wraps a LiveCharts Cartesian chart with a pinned readout that is updated when the chart is pressed
/// and while the pointer is dragged across the plot.
/// </summary>
public sealed class InteractiveCartesianChartView : ContentView
{
    private const string EmptySelectionText = "Tap or drag across the graph to pin exact values.";

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

    public static readonly BindableProperty ValueFormatProperty = BindableProperty.Create(
        nameof(ValueFormat),
        typeof(string),
        typeof(InteractiveCartesianChartView),
        "C0");

    private readonly CartesianChart chart;
    private readonly Label detailLabel;
    private (ISeries Series, IReadOnlyList<object?> Values)[]? cachedSeriesValues;
    private bool isPointerDown;
    private int selectedIndex = -1;

    public InteractiveCartesianChartView()
    {
        chart = new CartesianChart
        {
            HeightRequest = ChartHeight
        };

        // LiveCharts handles native touch on its own platform view, so MAUI gesture recognizers on the
        // chart never fire on mobile. The chart's own pointer commands are the reliable input source.
        // Pressed pins the first value, Moved keeps it in sync while the finger drags across the plot.
        chart.PressedCommand = new Command<PointerCommandArgs>(OnChartPressed);
        chart.MovedCommand = new Command<PointerCommandArgs>(OnChartMoved);
        chart.ReleasedCommand = new Command<PointerCommandArgs>(OnChartReleased);

        detailLabel = new Label
        {
            LineBreakMode = LineBreakMode.WordWrap,
            Text = EmptySelectionText
        };
        detailLabel.SetDynamicResource(StyleProperty, "CalculatorSupportingText");

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

    public string ValueFormat
    {
        get => (string)GetValue(ValueFormatProperty);
        set => SetValue(ValueFormatProperty, value);
    }

    private void OnChartPressed(PointerCommandArgs? args)
    {
        isPointerDown = true;
        // Series values can be replaced or mutated between gestures, so snapshot them per gesture.
        cachedSeriesValues = null;
        UpdateSelection(args, force: true);
    }

    private void OnChartMoved(PointerCommandArgs? args)
    {
        // Moved also fires for mouse hover on desktop; only scrub while the pointer is held down so the
        // pinned readout stays where the user placed it.
        if (!isPointerDown)
        {
            return;
        }

        UpdateSelection(args, force: false);
    }

    private void OnChartReleased(PointerCommandArgs? args)
    {
        isPointerDown = false;
        cachedSeriesValues = null;
    }

    private void UpdateSelection(PointerCommandArgs? args, bool force)
    {
        if (args is null)
        {
            return;
        }

        var seriesValues = GetSeriesValues();
        if (seriesValues.Length == 0)
        {
            ClearSelection();
            return;
        }

        var pointerX = args.PointerPosition.X;
        var dataX = TryScalePointerToData(pointerX);

        var primaryValues = seriesValues[0].Values;
        var index = GetNearestIndex(primaryValues, dataX, pointerX);
        if (!force && index == selectedIndex)
        {
            return;
        }

        selectedIndex = index;
        var lines = new List<string> { GetXAxisLabel(index, primaryValues) };

        foreach (var (series, values) in seriesValues)
        {
            var seriesIndex = GetNearestIndex(values, dataX, pointerX);
            if (TryFormatValue(values[seriesIndex]) is not { } value)
            {
                continue;
            }

            lines.Add($"{series.Name}: {value}");
        }

        detailLabel.Text = string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Materializes the series values once per interaction so dragging does not re-enumerate every
    /// series on each pointer move.
    /// </summary>
    private (ISeries Series, IReadOnlyList<object?> Values)[] GetSeriesValues()
    {
        return cachedSeriesValues ??= Series
            .Select(series => (Series: series, Values: ToValueList(series.Values)))
            .Where(series => series.Values.Count > 0)
            .ToArray();
    }

    /// <summary>
    /// Converts the pointer position (in device independent units, the same units LiveCharts uses for
    /// the draw margin) into a value on the x axis, or <c>null</c> when the chart has not been measured.
    /// </summary>
    private double? TryScalePointerToData(double pointerX)
    {
        if (chart.Width <= 0 || chart.CoreChart.DrawMarginSize.Width <= 0)
        {
            return null;
        }

        try
        {
            return chart.ScalePixelsToData(new LvcPointD(pointerX, 0)).X;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private int GetNearestIndex(IReadOnlyList<object?> values, double? dataX, double pointerX)
    {
        if (values.Count <= 1)
        {
            return 0;
        }

        if (dataX is not { } targetX)
        {
            return GetNearestIndexFromPointer(pointerX, values.Count);
        }

        var nearestIndex = 0;
        var nearestDistance = double.MaxValue;
        for (var index = 0; index < values.Count; index++)
        {
            var distance = Math.Abs(GetPointX(values[index], index) - targetX);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = index;
            }
        }

        return nearestIndex;
    }

    private static double GetPointX(object? value, int index) => value switch
    {
        DateTimePoint datePoint => datePoint.DateTime.Ticks,
        ObservablePoint { X: { } x } => x,
        _ => index
    };

    private int GetNearestIndexFromPointer(double pointerX, int pointCount)
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

        var ratio = Math.Clamp((pointerX - plotLeft) / plotWidth, 0, 1);
        return (int)Math.Round(ratio * (pointCount - 1), MidpointRounding.AwayFromZero);
    }

    private string GetXAxisLabel(int selectedIndex, IReadOnlyList<object?> values)
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

        return selectedIndex < values.Count && values[selectedIndex] is DateTimePoint datePoint
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

    private string? TryFormatValue(object? value)
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

        try
        {
            return number?.ToString(ValueFormat, CultureInfo.CurrentCulture);
        }
        catch (FormatException)
        {
            return number?.ToString("0.##", CultureInfo.CurrentCulture);
        }
    }

    private void ClearSelection()
    {
        cachedSeriesValues = null;
        selectedIndex = -1;
        detailLabel.Text = EmptySelectionText;
    }
}
