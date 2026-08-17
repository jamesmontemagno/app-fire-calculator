namespace MyFireNumber.Views.Controls;

public sealed class SteppedSlider : Slider
{
    public static readonly BindableProperty StepSizeProperty = BindableProperty.Create(
        nameof(StepSize),
        typeof(double),
        typeof(SteppedSlider),
        1d,
        validateValue: static (_, value) => (double)value > 0);

    private bool isSnapping;

    public SteppedSlider()
    {
        ValueChanged += OnValueChanged;
    }

    public double StepSize
    {
        get => (double)GetValue(StepSizeProperty);
        set => SetValue(StepSizeProperty, value);
    }

    private void OnValueChanged(object? sender, ValueChangedEventArgs eventArgs)
    {
        if (isSnapping)
        {
            return;
        }

        var stepCount = Math.Round((eventArgs.NewValue - Minimum) / StepSize, MidpointRounding.AwayFromZero);
        var snappedValue = Math.Clamp(Minimum + (stepCount * StepSize), Minimum, Maximum);
        if (Math.Abs(snappedValue - eventArgs.NewValue) < 0.000001)
        {
            return;
        }

        isSnapping = true;
        Value = snappedValue;
        isSnapping = false;
    }
}
