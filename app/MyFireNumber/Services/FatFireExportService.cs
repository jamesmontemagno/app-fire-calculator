using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Exports;

namespace MyFireNumber.Services;

public interface IFatFireExportService
{
    Task ShareAsync(FatFireDraft draft, StandardFireResult result, CancellationToken cancellationToken = default);
}

public sealed class FatFireExportService : IFatFireExportService
{
    public async Task ShareAsync(FatFireDraft draft, StandardFireResult result, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var filePath = Path.Combine(
            FileSystem.CacheDirectory,
            $"fat-fire-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.xlsx");
        StandardFireWorkbook.CreateFat(filePath, draft, result, DateTimeOffset.UtcNow);

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Share Fat FIRE workbook",
            File = new ShareFile(filePath)
        });
    }
}