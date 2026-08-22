using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Exports;

namespace MyFireNumber.Services;

public interface ISeppExportService
{
    Task ShareAsync(SeppDraft draft, SeppResult result, CancellationToken cancellationToken = default);
}

public sealed class SeppExportService : ISeppExportService
{
    public async Task ShareAsync(SeppDraft draft, SeppResult result, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var filePath = Path.Combine(
            FileSystem.CacheDirectory,
            $"sepp-72t-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.xlsx");
        SeppWorkbook.Create(filePath, draft, result, DateTimeOffset.UtcNow);
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Share 72(t) / SEPP workbook",
            File = new ShareFile(filePath)
        });
    }
}
