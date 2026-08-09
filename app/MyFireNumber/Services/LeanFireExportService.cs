using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Exports;

namespace MyFireNumber.Services;

public interface ILeanFireExportService
{
    Task ShareAsync(LeanFireDraft draft, StandardFireResult result, CancellationToken cancellationToken = default);
}

public sealed class LeanFireExportService : ILeanFireExportService
{
    public async Task ShareAsync(LeanFireDraft draft, StandardFireResult result, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var filePath = Path.Combine(
            FileSystem.CacheDirectory,
            $"lean-fire-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.xlsx");
        StandardFireWorkbook.CreateLean(filePath, draft, result, DateTimeOffset.UtcNow);

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Share Lean FIRE workbook",
            File = new ShareFile(filePath)
        });
    }
}