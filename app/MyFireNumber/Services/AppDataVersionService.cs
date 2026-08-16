namespace MyFireNumber.Services;

public interface IAppDataVersionService
{
    void EnsureCurrentVersion();

    /// <summary>
    /// Records the current data version without touching stored data. Reset and import clear every
    /// preference, so they must call this to avoid the startup check re-running its one-time wipe.
    /// </summary>
    void MarkCurrentVersion();
}

public sealed class AppDataVersionService : IAppDataVersionService
{
    private const string VersionKey = "app-data-version";
    private const int CurrentVersion = 3;

    // Pre-release database identities only. The app has not shipped, so these files are deliberately
    // removed rather than migrated; the current v3 file is never in this list.
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

        // Only a device that actually carries pre-release data needs the cleanup. A fresh install
        // has nothing to remove, so it just records the marker and keeps any settings intact.
        if (TryRemoveLegacyDatabases())
        {
            TryClearPreferences();
        }

        MarkCurrentVersion();
    }

    public void MarkCurrentVersion()
    {
        try
        {
            Preferences.Default.Set(VersionKey, CurrentVersion);
        }
        catch (Exception)
        {
            // A device that cannot persist the marker still runs; the check simply repeats next launch.
        }
    }

    private static bool TryRemoveLegacyDatabases()
    {
        var removedAny = false;
        foreach (var fileName in LegacyDatabaseFiles)
        {
            try
            {
                var path = Path.Combine(FileSystem.AppDataDirectory, fileName);
                if (!File.Exists(path))
                {
                    continue;
                }

                File.Delete(path);
                removedAny = true;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // A locked or restricted file must not stop startup. The stale file is unused by the
                // current database identity, so leaving it behind is safe.
                removedAny = true;
            }
        }

        return removedAny;
    }

    private static void TryClearPreferences()
    {
        try
        {
            Preferences.Default.Clear();
        }
        catch (Exception)
        {
            // Best effort: preferences that survive are still readable by the current app version.
        }
    }
}
