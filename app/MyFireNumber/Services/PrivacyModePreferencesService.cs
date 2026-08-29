namespace MyFireNumber.Services;

/// <summary>
/// Remembers whether the Home dashboard and Accounts overview mask their dollar figures behind a
/// placeholder, plus the global "privacy mode on startup" setting from Settings.
/// </summary>
/// <remarks>
/// Home and Accounts each keep their own on/off state so a user can, for example, leave Accounts
/// masked while Home stays visible. Both default to off — the whole point of this feature is that
/// privacy is opt-in unless the user turns on the global startup override in Settings. When that
/// override is on, <see cref="ApplyStartupOverride"/> forces both back to on every time the app
/// launches, regardless of what the user left them as when they last closed the app; during the
/// running session the two toggles behave independently again until the next launch.
/// </remarks>
public interface IPrivacyModePreferencesService
{
    bool HomePrivacyEnabled { get; set; }

    bool AccountsPrivacyEnabled { get; set; }

    bool PrivacyModeOnStartup { get; set; }

    /// <summary>
    /// Forces <see cref="HomePrivacyEnabled"/> and <see cref="AccountsPrivacyEnabled"/> back on when
    /// <see cref="PrivacyModeOnStartup"/> is enabled. Call once per app launch, before either page
    /// reads its toggle state.
    /// </summary>
    void ApplyStartupOverride();
}

public sealed class PrivacyModePreferencesService : IPrivacyModePreferencesService
{
    private const string HomePrivacyKey = "privacy-home-enabled";
    private const string AccountsPrivacyKey = "privacy-accounts-enabled";
    private const string PrivacyOnStartupKey = "privacy-on-startup";

    public bool HomePrivacyEnabled
    {
        get => Preferences.Default.Get(HomePrivacyKey, false);
        set => Preferences.Default.Set(HomePrivacyKey, value);
    }

    public bool AccountsPrivacyEnabled
    {
        get => Preferences.Default.Get(AccountsPrivacyKey, false);
        set => Preferences.Default.Set(AccountsPrivacyKey, value);
    }

    public bool PrivacyModeOnStartup
    {
        get => Preferences.Default.Get(PrivacyOnStartupKey, false);
        set => Preferences.Default.Set(PrivacyOnStartupKey, value);
    }

    public void ApplyStartupOverride()
    {
        if (!PrivacyModeOnStartup)
        {
            return;
        }

        HomePrivacyEnabled = true;
        AccountsPrivacyEnabled = true;
    }
}
