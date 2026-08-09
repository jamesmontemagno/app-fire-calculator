using System.Diagnostics;

namespace MyFireNumber.Services;

public interface ITemporaryExportCleanupService
{
    void RemoveStaleFiles();
}

public sealed class TemporaryExportCleanupService : ITemporaryExportCleanupService
{
    private static readonly TimeSpan MaximumAge = TimeSpan.FromDays(1);

    public void RemoveStaleFiles()
    {
        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(FileSystem.CacheDirectory).ToArray();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine("Temporary export cleanup could not inspect the app cache.");
            return;
        }

        var cutoff = DateTime.UtcNow - MaximumAge;
        foreach (var file in files.Where(IsTemporaryExport))
        {
            try
            {
                if (File.GetLastWriteTimeUtc(file) < cutoff)
                {
                    File.Delete(file);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Debug.WriteLine("A stale temporary export could not be removed.");
            }
        }
    }

    private static bool IsTemporaryExport(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        return fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
            || (fileName.StartsWith("my-fire-number-backup-", StringComparison.Ordinal)
                && fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
    }
}
