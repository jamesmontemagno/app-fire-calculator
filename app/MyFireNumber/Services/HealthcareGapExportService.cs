using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Exports;

namespace MyFireNumber.Services;

public interface IHealthcareGapExportService
{
    Task ShareAsync(HealthcareGapDraft draft, HealthcareGapResult result, CancellationToken cancellationToken = default);
}

public sealed class HealthcareGapExportService : IHealthcareGapExportService
{
    public async Task ShareAsync(HealthcareGapDraft draft, HealthcareGapResult result, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var filePath = Path.Combine(
            FileSystem.CacheDirectory,
            $"healthcare-gap-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.xlsx");
        HealthcareGapWorkbook.Create(filePath, draft, result, DateTimeOffset.UtcNow);

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Share Healthcare Gap workbook",
            File = new ShareFile(filePath)
        });
    }
}