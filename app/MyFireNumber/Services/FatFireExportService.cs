using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Exports;

namespace MyFireNumber.Services;

public interface IFatFireExportService
{
    Task ShareAsync(FatFireDraft draft, StandardFireResult result, CancellationToken cancellationToken = default);
}

public sealed class FatFireExportService : IFatFireExportService
{
    private readonly IDisplayPeriodPreferencesService displayPeriodPreferences;

    public FatFireExportService(IDisplayPeriodPreferencesService displayPeriodPreferences)
    {
        this.displayPeriodPreferences = displayPeriodPreferences;
    }

    public async Task ShareAsync(FatFireDraft draft, StandardFireResult result, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var filePath = Path.Combine(
            FileSystem.CacheDirectory,
            $"fat-fire-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.xlsx");
        StandardFireWorkbook.CreateFat(filePath, draft, result, displayPeriodPreferences.Get("fat-fire"), DateTimeOffset.UtcNow);

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Share Fat FIRE workbook",
            File = new ShareFile(filePath)
        });
    }
}