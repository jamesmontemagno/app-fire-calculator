using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Profile;
using MyFireNumber.Storage;

namespace MyFireNumber.Tests.Data;

public sealed class SqliteProfileAssetRepositoryTests : IAsyncLifetime
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"profile-assets-{Guid.NewGuid():N}.db3");

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        if (File.Exists(databasePath)) File.Delete(databasePath);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task SaveAsync_RoundTripsEveryField()
    {
        var repository = new SqliteProfileAssetRepository(new LocalDatabase(databasePath));
        var asset = new PropertyAsset("house", "Our house", PropertyAssetType.Home, 450_000, 380_000);

        await repository.SaveAsync(asset);

        Assert.Equal(asset, Assert.Single(await repository.ListAsync()));
    }

    [Fact]
    public async Task SaveAsync_KeepsAnAssetExcludedFromNetWorth()
    {
        var repository = new SqliteProfileAssetRepository(new LocalDatabase(databasePath));
        await repository.SaveAsync(new PropertyAsset("car", "Car", PropertyAssetType.Vehicle, 22_000, 34_000, IncludeInNetWorth: false));

        var saved = Assert.Single(await repository.ListAsync());
        Assert.False(saved.IncludeInNetWorth);
        Assert.Equal(0, saved.NetWorthValue);
        Assert.Equal(-12_000, saved.ValueChange);
    }

    [Theory]
    [InlineData(-1d, 0d)]
    [InlineData(0d, -1d)]
    public async Task SaveAsync_RejectsNegativeValues(double currentValue, double purchaseValue)
    {
        var repository = new SqliteProfileAssetRepository(new LocalDatabase(databasePath));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => repository.SaveAsync(new PropertyAsset("house", "Our house", PropertyAssetType.Home, currentValue, purchaseValue)));
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheAsset()
    {
        var repository = new SqliteProfileAssetRepository(new LocalDatabase(databasePath));
        await repository.SaveAsync(new PropertyAsset("land", "Lake lot", PropertyAssetType.Land, 60_000, 45_000));

        await repository.DeleteAsync("land");

        Assert.Empty(await repository.ListAsync());
    }

    [Fact]
    public async Task SnapshotRepository_ReportsAssetsSeparatelyFromInvestableBalances()
    {
        // FIRE projections may only spend investment balances, so a house must never inflate the
        // account total the calculators read; it only shows up in net worth.
        var database = new LocalDatabase(databasePath);
        var accounts = new SqliteProfileAccountRepository(database);
        var assets = new SqliteProfileAssetRepository(database);
        var debts = new SqliteProfileDebtRepository(database);
        await accounts.SaveAsync(new RetirementAccount("401k", "401(k)", RetirementAccountType.Traditional, 250_000, 20_000, 0.07, 60, 0.04, 30));
        await assets.SaveAsync(new PropertyAsset("house", "Our house", PropertyAssetType.Home, 450_000, 380_000));
        await assets.SaveAsync(new PropertyAsset("car", "Car", PropertyAssetType.Vehicle, 22_000, 34_000, IncludeInNetWorth: false));
        await debts.SaveAsync(new DebtItem("mortgage", "Mortgage", 300_000, 0.055, 2_100));

        var snapshot = await new SqliteProfileFinancialSnapshotRepository(
            new SqliteProfileRepository(database),
            accounts,
            new SqliteProfileIncomeRepository(database),
            new SqliteProfileExpenseRepository(database),
            debts,
            assets).GetAsync();

        Assert.Equal(250_000, snapshot.TotalAccountBalance);
        Assert.Equal(450_000, snapshot.TotalAssetValue);
        Assert.Equal(300_000, snapshot.TotalDebtBalance);
        Assert.Equal(400_000, snapshot.NetWorth);
    }
}
