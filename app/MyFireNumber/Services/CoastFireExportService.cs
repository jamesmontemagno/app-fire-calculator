using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Exports;

namespace MyFireNumber.Services;

public interface ICoastFireExportService
{
    Task ShareAsync(CoastFireDraft draft, CoastFireResult result, CancellationToken cancellationToken = default);
}

public sealed class CoastFireExportService : ICoastFireExportService
{
    private readonly IDisplayPeriodPreferencesService displayPeriodPreferences;

    public CoastFireExportService(IDisplayPeriodPreferencesService displayPeriodPreferences)
    {
        this.displayPeriodPreferences = displayPeriodPreferences;
    }

    public async Task ShareAsync(CoastFireDraft draft, CoastFireResult result, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var filePath = Path.Combine(
            FileSystem.CacheDirectory,
            $"coast-fire-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.xlsx");
        CoastFireWorkbook.Create(filePath, draft, result, displayPeriodPreferences.Get("coast-fire"), DateTimeOffset.UtcNow);

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Share Coast FIRE workbook",
            File = new ShareFile(filePath)
        });
    }
}