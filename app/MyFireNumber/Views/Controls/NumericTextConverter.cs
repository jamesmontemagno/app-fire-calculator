using System.Globalization;

namespace MyFireNumber.Views.Controls;

public sealed class NumericTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is string text && double.TryParse(text, NumberStyles.Number, culture, out var number)
            ? number
            : 0d;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is double number
            ? number.ToString("0.##", culture)
            : string.Empty;
    }
}
