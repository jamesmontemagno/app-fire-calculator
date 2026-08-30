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
/// Wraps a LiveCharts Cartesian chart with a pinned readout that is updated when the chart is pressed.
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

    public static readonly BindableProperty ValueFormatProperty = BindableProperty.Create(
        nameof(ValueFormat),
        typeof(string),
        typeof(InteractiveCartesianChartView),
        "C0");

    private readonly CartesianChart chart;
    private readonly Label detailLabel;

    public InteractiveCartesianChartView()
    {
        chart = new CartesianChart
        {
            HeightRequest = ChartHeight
        };

        // LiveCharts handles native touch on its own platform view, so MAUI gesture recognizers on the
        // chart never fire on mobile. The chart's own pointer command is the reliable input source.
        chart.PressedCommand = new Command<PointerCommandArgs>(OnChartPressed);

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
        if (args is null)
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

        var pointerX = args.PointerPosition.X;
        var dataX = TryScalePointerToData(pointerX);

        var primaryValues = seriesValues[0].Values;
        var selectedIndex = GetNearestIndex(primaryValues, dataX, pointerX);
        var lines = new List<string> { GetXAxisLabel(selectedIndex, primaryValues) };

        foreach (var (series, values) in seriesValues)
        {
            var index = GetNearestIndex(values, dataX, pointerX);
            if (TryFormatValue(values[index]) is not { } value)
            {
                continue;
            }

            lines.Add($"{series.Name}: {value}");
        }
        detailLabel.Text = string.Join(Environment.NewLine, lines);
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
        detailLabel.Text = EmptySelectionText;
    }
}
