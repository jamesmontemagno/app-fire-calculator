using System.Globalization;

namespace MyFireNumber.Services;

public interface ICurrencyPreferencesService
{
    IReadOnlyList<string> Options { get; }

    string SelectedOption { get; }

    void Save(string option);

    string Format(double value);
}

public sealed class CurrencyPreferencesService : ICurrencyPreferencesService
{
    public const string DeviceRegion = "Device region";
    private const string PreferenceKey = "currency-option";
    private static readonly IReadOnlyDictionary<string, string> CurrencyCultures =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["USD — US Dollar"] = "en-US",
            ["CAD — Canadian Dollar"] = "en-CA",
            ["EUR — Euro"] = "de-DE",
            ["GBP — British Pound"] = "en-GB",
            ["AUD — Australian Dollar"] = "en-AU",
            ["JPY — Japanese Yen"] = "ja-JP"
        };

    public IReadOnlyList<string> Options { get; } = [DeviceRegion, .. CurrencyCultures.Keys];

    public string SelectedOption
    {
        get
        {
            var selected = Preferences.Default.Get(PreferenceKey, DeviceRegion);
            return selected == DeviceRegion || CurrencyCultures.ContainsKey(selected)
                ? selected
                : DeviceRegion;
        }
    }

    public void Save(string option)
    {
        Preferences.Default.Set(
            PreferenceKey,
            option == DeviceRegion || CurrencyCultures.ContainsKey(option) ? option : DeviceRegion);
    }

    public string Format(double value)
    {
        if (SelectedOption == DeviceRegion)
        {
            return value.ToString("C0", CultureInfo.CurrentCulture);
        }

        var currencyCulture = CultureInfo.GetCultureInfo(CurrencyCultures[SelectedOption]);
        var numberFormat = (NumberFormatInfo)CultureInfo.CurrentCulture.NumberFormat.Clone();
        numberFormat.CurrencySymbol = currencyCulture.NumberFormat.CurrencySymbol;
        numberFormat.CurrencyDecimalDigits = 0;
        return value.ToString("C0", numberFormat);
    }
}
