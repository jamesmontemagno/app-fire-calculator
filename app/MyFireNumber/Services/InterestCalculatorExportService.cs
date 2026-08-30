using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Exports;

namespace MyFireNumber.Services;

public interface IInterestCalculatorExportService
{
    Task ShareAsync(InterestCalculatorDraft draft, InterestCalculatorResult result, CancellationToken cancellationToken = default);
}

public sealed class InterestCalculatorExportService : IInterestCalculatorExportService
{
    public async Task ShareAsync(InterestCalculatorDraft draft, InterestCalculatorResult result, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var filePath = Path.Combine(FileSystem.CacheDirectory, $"interest-calculator-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.xlsx");
        InterestCalculatorWorkbook.Create(filePath, draft, result, DateTimeOffset.UtcNow);
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Share Interest Calculator workbook",
            File = new ShareFile(filePath)
        });
    }
}
