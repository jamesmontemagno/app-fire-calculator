using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Profile;
using MyFireNumber.Storage;

namespace MyFireNumber.Tests.Data;

public sealed class SqliteFinancialCheckInRepositoryTests : IAsyncLifetime
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"financial-check-in-{Guid.NewGuid():N}.db3");

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        if (File.Exists(databasePath)) File.Delete(databasePath);
        return Task.CompletedTask;
    }

    private static FinancialCheckIn CreateCheckIn(string id, DateTime completedAtUtc) => new(
        id,
        completedAtUtc,
        [new AccountBalanceEntry("401k", "401(k)", RetirementAccountType.Traditional, 100_000)],
        [new DebtBalanceEntry("card", "Card", 2_000)],
        90_000,
        60_000);

    private static void AssertCheckInsMatch(FinancialCheckIn expected, FinancialCheckIn actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.CompletedAtUtc, actual.CompletedAtUtc);
        Assert.Equal(expected.AnnualIncome, actual.AnnualIncome);
        Assert.Equal(expected.AnnualExpenses, actual.AnnualExpenses);
        Assert.Equal(expected.Accounts, actual.Accounts);
        Assert.Equal(expected.Debts, actual.Debts);
        Assert.Equal(expected.Assets, actual.Assets);
    }

    [Fact]
    public async Task SaveAsync_RoundTripsAssetValuesAndCountsThemInNetWorth()
    {
        var repository = new SqliteFinancialCheckInRepository(new LocalDatabase(databasePath));
        var checkIn = new FinancialCheckIn(
            "with-assets",
            new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            [new AccountBalanceEntry("401k", "401(k)", RetirementAccountType.Traditional, 100_000)],
            [new DebtBalanceEntry("mortgage", "Mortgage", 300_000)],
            90_000,
            60_000)
        {
            Assets =
            [
                new AssetValueEntry("house", "Our house", PropertyAssetType.Home, 450_000),
                new AssetValueEntry("car", "Car", PropertyAssetType.Vehicle, 22_000, IncludeInNetWorth: false)
            ]
        };

        await repository.SaveAsync(checkIn);

        var saved = Assert.Single(await repository.ListAsync());
        AssertCheckInsMatch(checkIn, saved);
        Assert.Equal(100_000, saved.TotalAccountBalance);
        Assert.Equal(450_000, saved.TotalAssetValue);
        Assert.Equal(550_000, saved.TotalAssets);
        Assert.Equal(250_000, saved.NetWorth);
    }

    [Fact]
    public async Task SaveAsync_RejectsANegativeAssetValue()
    {
        var repository = new SqliteFinancialCheckInRepository(new LocalDatabase(databasePath));
        var invalid = new FinancialCheckIn("bad-asset", DateTime.UtcNow, [], [], 0, 0)
        {
            Assets = [new AssetValueEntry("house", "Our house", PropertyAssetType.Home, -1)]
        };

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => repository.SaveAsync(invalid));
    }

    [Fact]
    public async Task ListAsync_TreatsACheckInSavedWithoutAssetsAsHavingNone()
    {
        var repository = new SqliteFinancialCheckInRepository(new LocalDatabase(databasePath));
        await repository.SaveAsync(CreateCheckIn("legacy", new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)));

        var saved = Assert.Single(await repository.ListAsync());
        Assert.Empty(saved.Assets);
        Assert.Equal(saved.TotalAccountBalance, saved.TotalAssets);
    }

    [Fact]
    public async Task SaveAsync_ThenListAsync_RoundTripsAllFieldsAndOrdersOldestFirst()
    {
        var repository = new SqliteFinancialCheckInRepository(new LocalDatabase(databasePath));
        var older = CreateCheckIn("first", new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var newer = CreateCheckIn("second", new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc));

        await repository.SaveAsync(newer);
        await repository.SaveAsync(older);

        var results = await repository.ListAsync();
        Assert.Equal(2, results.Count);
        AssertCheckInsMatch(older, results[0]);
        AssertCheckInsMatch(newer, results[1]);
    }

    [Fact]
    public async Task GetLatestAsync_ReturnsTheMostRecentlyCompletedCheckIn()
    {
        var repository = new SqliteFinancialCheckInRepository(new LocalDatabase(databasePath));
        await repository.SaveAsync(CreateCheckIn("first", new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        var newest = CreateCheckIn("second", new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        await repository.SaveAsync(newest);

        var latest = await repository.GetLatestAsync();
        Assert.NotNull(latest);
        AssertCheckInsMatch(newest, latest);
    }

    [Fact]
    public async Task GetLatestAsync_ReturnsNull_WhenNoCheckInHasBeenSaved()
    {
        var repository = new SqliteFinancialCheckInRepository(new LocalDatabase(databasePath));

        Assert.Null(await repository.GetLatestAsync());
    }

    [Fact]
    public async Task SaveAsync_PreservesTheAccountTypeAndLabelsRecordedAtCheckInTime()
    {
        // A check-in snapshot must keep the label/type as it was that day, even if the live account is
        // later renamed or retyped, so historical charts don't silently rewrite the past.
        var repository = new SqliteFinancialCheckInRepository(new LocalDatabase(databasePath));
        var checkIn = CreateCheckIn("first", new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        await repository.SaveAsync(checkIn);

        var saved = Assert.Single(await repository.ListAsync());
        var account = Assert.Single(saved.Accounts);
        Assert.Equal("401(k)", account.Name);
        Assert.Equal(RetirementAccountType.Traditional, account.Type);
    }

    [Theory]
    [InlineData(-1, 60_000)]
    [InlineData(90_000, -1)]
    public async Task SaveAsync_RejectsNegativeAnnualTotals(double annualIncome, double annualExpenses)
    {
        var repository = new SqliteFinancialCheckInRepository(new LocalDatabase(databasePath));
        var invalid = new FinancialCheckIn("bad", DateTime.UtcNow, [], [], annualIncome, annualExpenses);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => repository.SaveAsync(invalid));
    }

    [Fact]
    public async Task SaveAsync_RejectsANegativeAccountOrDebtBalance()
    {
        var repository = new SqliteFinancialCheckInRepository(new LocalDatabase(databasePath));
        var negativeAccount = new FinancialCheckIn(
            "bad-account",
            DateTime.UtcNow,
            [new AccountBalanceEntry("acct", "Account", RetirementAccountType.Roth, -1)],
            [],
            0,
            0);
        var negativeDebt = new FinancialCheckIn(
            "bad-debt",
            DateTime.UtcNow,
            [],
            [new DebtBalanceEntry("debt", "Debt", -1)],
            0,
            0);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => repository.SaveAsync(negativeAccount));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => repository.SaveAsync(negativeDebt));
    }

    [Fact]
    public async Task SaveAsync_UsesInsertOrReplace_SoResavingTheSameIdOverwritesTheEntity()
    {
        // The repository doc comment says a check-in is immutable once saved (callers always mint a
        // fresh id), but the underlying storage call is InsertOrReplace; this pins that mechanic so a
        // future accidental id collision overwrites rather than throwing or duplicating rows.
        var repository = new SqliteFinancialCheckInRepository(new LocalDatabase(databasePath));
        await repository.SaveAsync(CreateCheckIn("same-id", new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        await repository.SaveAsync(CreateCheckIn("same-id", new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)) with { AnnualIncome = 120_000 });

        var results = await repository.ListAsync();
        var single = Assert.Single(results);
        Assert.Equal(120_000, single.AnnualIncome);
    }
}
