using MyFireNumber.Storage;

namespace MyFireNumber.Services;

public interface IAppResetService
{
    Task ResetAsync();
}

public sealed class AppResetService(
    LocalDatabase database,
    IThemeService themeService,
    IProfileService profileService,
    IAppDataVersionService appDataVersionService) : IAppResetService
{
    public async Task ResetAsync()
    {
        await database.ClearAsync();
        Preferences.Default.Clear();

        // Clearing preferences also removes the app-data version marker. Without restoring it the
        // next launch would treat this device as un-versioned and wipe preferences a second time,
        // discarding whatever the user set after the reset.
        appDataVersionService.MarkCurrentVersion();
        themeService.Apply(ThemePreference.System);
        await profileService.NotifyExternalChangeAsync();
    }
}
