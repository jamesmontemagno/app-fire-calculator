using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Exports;

namespace MyFireNumber.Services;

public interface IDeferredCompensationExportService
{
    Task ShareAsync(DeferredCompensationDraft draft, DeferredCompensationResult result, CancellationToken cancellationToken = default);
}

public sealed class DeferredCompensationExportService : IDeferredCompensationExportService
{
    private readonly IDisplayPeriodPreferencesService displayPeriodPreferences;

    public DeferredCompensationExportService(IDisplayPeriodPreferencesService displayPeriodPreferences)
    {
        this.displayPeriodPreferences = displayPeriodPreferences;
    }

    public async Task ShareAsync(DeferredCompensationDraft draft, DeferredCompensationResult result, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var filePath = Path.Combine(FileSystem.CacheDirectory, $"retirement-cash-flow-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.xlsx");
        DeferredCompensationWorkbook.Create(filePath, draft, result, displayPeriodPreferences.Get("retirement-cash-flow"), DateTimeOffset.UtcNow);
        await Share.Default.RequestAsync(new ShareFileRequest { Title = "Share Retirement Cash Flow workbook", File = new ShareFile(filePath) });
    }
}