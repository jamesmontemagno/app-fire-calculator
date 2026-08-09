using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Exports;

namespace MyFireNumber.Services;

public interface IDebtPayoffExportService
{
    Task ShareAsync(DebtPayoffDraft draft, DebtPayoffResult result, CancellationToken cancellationToken = default);
}

public sealed class DebtPayoffExportService : IDebtPayoffExportService
{
    public async Task ShareAsync(DebtPayoffDraft draft, DebtPayoffResult result, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var filePath = Path.Combine(FileSystem.CacheDirectory, $"debt-payoff-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.xlsx");
        DebtPayoffWorkbook.Create(filePath, draft, result, DateTimeOffset.UtcNow);
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Share Debt Payoff workbook",
            File = new ShareFile(filePath)
        });
    }
}