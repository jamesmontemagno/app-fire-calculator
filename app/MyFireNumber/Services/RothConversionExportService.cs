using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Exports;

namespace MyFireNumber.Services;

public interface IRothConversionExportService
{
    Task ShareAsync(
        RothConversionDraft draft,
        RothConversionResult result,
        CancellationToken cancellationToken = default);
}

public sealed class RothConversionExportService : IRothConversionExportService
{
    public async Task ShareAsync(
        RothConversionDraft draft,
        RothConversionResult result,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var filePath = Path.Combine(
            FileSystem.CacheDirectory,
            $"roth-conversion-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.xlsx");
        RothConversionWorkbook.Create(filePath, draft, result, DateTimeOffset.UtcNow);
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Share Roth conversion strategy workbook",
            File = new ShareFile(filePath)
        });
    }
}
