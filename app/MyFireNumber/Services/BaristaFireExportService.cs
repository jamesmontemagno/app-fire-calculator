using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Exports;

namespace MyFireNumber.Services;

public interface IBaristaFireExportService
{
    Task ShareAsync(BaristaFireDraft draft, BaristaFireResult result, CancellationToken cancellationToken = default);
}

public sealed class BaristaFireExportService : IBaristaFireExportService
{
    private readonly IDisplayPeriodPreferencesService displayPeriodPreferences;

    public BaristaFireExportService(IDisplayPeriodPreferencesService displayPeriodPreferences)
    {
        this.displayPeriodPreferences = displayPeriodPreferences;
    }

    public async Task ShareAsync(BaristaFireDraft draft, BaristaFireResult result, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var filePath = Path.Combine(
            FileSystem.CacheDirectory,
            $"barista-fire-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.xlsx");
        BaristaFireWorkbook.Create(filePath, draft, result, displayPeriodPreferences.Get("barista-fire"), DateTimeOffset.UtcNow);

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Share Barista FIRE workbook",
            File = new ShareFile(filePath)
        });
    }
}