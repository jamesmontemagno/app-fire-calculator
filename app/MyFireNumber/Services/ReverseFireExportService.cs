using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Exports;

namespace MyFireNumber.Services;

public interface IReverseFireExportService
{
    Task ShareAsync(ReverseFireDraft draft, ReverseFireResult result, CancellationToken cancellationToken = default);
}

public sealed class ReverseFireExportService : IReverseFireExportService
{
    private readonly IDisplayPeriodPreferencesService displayPeriodPreferences;

    public ReverseFireExportService(IDisplayPeriodPreferencesService displayPeriodPreferences)
    {
        this.displayPeriodPreferences = displayPeriodPreferences;
    }

    public async Task ShareAsync(ReverseFireDraft draft, ReverseFireResult result, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var filePath = Path.Combine(
            FileSystem.CacheDirectory,
            $"reverse-fire-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.xlsx");
        ReverseFireWorkbook.Create(filePath, draft, result, displayPeriodPreferences.Get("reverse-fire"), DateTimeOffset.UtcNow);

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Share Reverse FIRE workbook",
            File = new ShareFile(filePath)
        });
    }
}