using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Exports;

namespace MyFireNumber.Services;

public interface IStandardFireExportService
{
    Task ShareAsync(StandardFireDraft draft, StandardFireResult result, CancellationToken cancellationToken = default);
}

public sealed class StandardFireExportService : IStandardFireExportService
{
    public async Task ShareAsync(StandardFireDraft draft, StandardFireResult result, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var filePath = Path.Combine(
            FileSystem.CacheDirectory,
            $"standard-fire-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.xlsx");
        StandardFireWorkbook.Create(filePath, draft, result, DateTimeOffset.UtcNow);

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Share Standard FIRE workbook",
            File = new ShareFile(filePath)
        });
    }
}