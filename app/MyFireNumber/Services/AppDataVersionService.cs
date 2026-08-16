namespace MyFireNumber.Services;

public interface IAppDataVersionService
{
    void EnsureCurrentVersion();
}

public sealed class AppDataVersionService : IAppDataVersionService
{
    private const string VersionKey = "app-data-version";
    private const int CurrentVersion = 3;
    private static readonly string[] LegacyDatabaseFiles =
    [
        "my-fire-number.db3",
        "my-fire-number.db3-wal",
        "my-fire-number.db3-shm",
        "my-fire-number-v2.db3",
        "my-fire-number-v2.db3-wal",
        "my-fire-number-v2.db3-shm"
    ];

    public void EnsureCurrentVersion()
    {
        if (Preferences.Default.Get(VersionKey, 0) == CurrentVersion)
        {
            return;
        }

        Preferences.Default.Clear();
        foreach (var fileName in LegacyDatabaseFiles)
        {
            var path = Path.Combine(FileSystem.AppDataDirectory, fileName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        Preferences.Default.Set(VersionKey, CurrentVersion);
    }
}
