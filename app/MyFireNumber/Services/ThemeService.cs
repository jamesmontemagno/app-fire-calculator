namespace MyFireNumber.Services;

public enum ThemePreference
{
    System,
    Light,
    Dark
}

public interface IThemeService
{
    ThemePreference Preference { get; }

    void Apply(ThemePreference preference);
}

public sealed class ThemeService : IThemeService
{
    private const string PreferenceKey = "theme-preference";

    public ThemePreference Preference
    {
        get
        {
            var storedValue = Preferences.Default.Get(PreferenceKey, ThemePreference.System.ToString());
            return Enum.TryParse<ThemePreference>(storedValue, ignoreCase: true, out var preference)
                ? preference
                : ThemePreference.System;
        }
    }

    public void Apply(ThemePreference preference)
    {
        Preferences.Default.Set(PreferenceKey, preference.ToString());
        Application.Current!.UserAppTheme = preference switch
        {
            ThemePreference.Light => AppTheme.Light,
            ThemePreference.Dark => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
    }
}
