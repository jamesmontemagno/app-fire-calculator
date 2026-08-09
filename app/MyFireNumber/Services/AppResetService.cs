using MyFireNumber.Storage;

namespace MyFireNumber.Services;

public interface IAppResetService
{
    Task ResetAsync();
}

public sealed class AppResetService(LocalDatabase database, IThemeService themeService) : IAppResetService
{
    public async Task ResetAsync()
    {
        await database.ClearAsync();
        Preferences.Default.Clear();
        themeService.Apply(ThemePreference.System);
    }
}
