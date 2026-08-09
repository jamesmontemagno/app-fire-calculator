using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Exports;

namespace MyFireNumber.Services;

public interface IWithdrawalRateExportService
{
    Task ShareAsync(WithdrawalRateDraft draft, WithdrawalResult result, CancellationToken cancellationToken = default);
}

public sealed class WithdrawalRateExportService : IWithdrawalRateExportService
{
    public async Task ShareAsync(WithdrawalRateDraft draft, WithdrawalResult result, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var filePath = Path.Combine(
            FileSystem.CacheDirectory,
            $"withdrawal-rate-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.xlsx");
        WithdrawalRateWorkbook.Create(filePath, draft, result, DateTimeOffset.UtcNow);

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Share Withdrawal Rate workbook",
            File = new ShareFile(filePath)
        });
    }
}