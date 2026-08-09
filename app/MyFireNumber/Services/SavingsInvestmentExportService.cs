using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Exports;

namespace MyFireNumber.Services;

public interface ISavingsInvestmentExportService
{
    Task ShareAsync(SavingsInvestmentDraft draft, InvestmentGrowthResult result, CancellationToken cancellationToken = default);
}

public sealed class SavingsInvestmentExportService : ISavingsInvestmentExportService
{
    public async Task ShareAsync(SavingsInvestmentDraft draft, InvestmentGrowthResult result, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var filePath = Path.Combine(
            FileSystem.CacheDirectory,
            $"savings-investment-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.xlsx");
        SavingsInvestmentWorkbook.Create(filePath, draft, result, DateTimeOffset.UtcNow);

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Share Savings & Investment Rate workbook",
            File = new ShareFile(filePath)
        });
    }
}