using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Profile;
using MyFireNumber.Storage;

namespace MyFireNumber.Tests.Data;

public sealed class LocalDataArchiveRepositoryTests : IAsyncLifetime
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"my-fire-number-{Guid.NewGuid():N}.db3");

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }

        return Task.CompletedTask;
    }

    [Fact]
    public async Task ExportAndImport_RoundTripsLocalData()
    {
        var source = new LocalDatabase(databasePath);
        var now = new DateTime(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc);
        await new SqliteDraftRepository(source).SaveAsync(new DraftRecord("standard-fire", 1, "{}", now));
        await new SqlitePlanRepository(source).SaveAsync(new PlanRecord("plan", "standard-fire", "Plan", 1, "{}", now, now));
        await new SqliteCalculatorPreferencesRepository(source).SaveAsync(new CalculatorPreferenceRecord("standard-fire", false, 2));
        await new SqliteRecentActivityRepository(source).TrackAsync(new RecentActivityRecord(RecentActivityKind.Plan, "plan", now));

        var archive = await source.ExportAsync();
        await source.ClearAsync();
        await source.ImportAsync(archive);

        Assert.NotNull(await new SqliteDraftRepository(source).GetAsync("standard-fire"));
        Assert.NotNull(await new SqlitePlanRepository(source).GetAsync("plan"));
        Assert.Single(await new SqliteCalculatorPreferencesRepository(source).ListAsync());
        Assert.Single(await new SqliteRecentActivityRepository(source).ListAsync(RecentActivityKind.Plan, 1));
    }

    [Fact]
    public async Task ExportAndImport_RoundTripsProfileAndInventory()
    {
        var database = new LocalDatabase(databasePath);
        var profile = new FinancialProfile("Alex", "Alex household", 2, new DateOnly(1990, 8, 16), null, new DateOnly(2045, 8, 16), 120_000, 72_000);
        await new SqliteProfileRepository(database).SaveAsync(profile);
        await new SqliteProfileAccountRepository(database).SaveAsync(
            new RetirementAccount("brokerage", "Brokerage", RetirementAccountType.Taxable, 50_000, 6_000, 0.07, 18, 0.04, 1, 0.15));
        await new SqliteProfileIncomeRepository(database).SaveAsync(
            new RetirementIncomeSource("salary", "Salary", 120_000, 45, 65, 0.02, false, 0.25));
        await new SqliteProfileExpenseRepository(database).SaveAsync(
            new RetirementExpense("housing", "Housing", 30_000, 45, 75));
        await new SqliteProfileDebtRepository(database).SaveAsync(
            new DebtItem("card", "Card", 5_000, 0.2, 150, 75));

        var archive = await database.ExportAsync();
        await database.ClearAsync();
        await database.ImportAsync(archive);

        Assert.Equal(profile, await new SqliteProfileRepository(database).GetAsync());
        Assert.Single(await new SqliteProfileAccountRepository(database).ListAsync());
        Assert.Single(await new SqliteProfileIncomeRepository(database).ListAsync());
        Assert.Single(await new SqliteProfileExpenseRepository(database).ListAsync());
        Assert.Equal(75, Assert.Single(await new SqliteProfileDebtRepository(database).ListAsync()).ExtraMonthlyPayment);
    }

    [Fact]
    public async Task ImportAsync_ReplacesExistingProfileWhenArchiveHasNone()
    {
        var database = new LocalDatabase(databasePath);
        var archive = await database.ExportAsync();

        await new SqliteProfileRepository(database).SaveAsync(FinancialProfile.Empty with { DisplayName = "Existing" });
        await new SqliteProfileDebtRepository(database).SaveAsync(new DebtItem("card", "Card", 5_000, 0.2, 150));

        await database.ImportAsync(archive);

        Assert.Equal(FinancialProfile.Empty, await new SqliteProfileRepository(database).GetAsync());
        Assert.Empty(await new SqliteProfileDebtRepository(database).ListAsync());
    }

    [Fact]
    public async Task ImportAsync_RejectsInvalidProfileInventory()
    {
        var database = new LocalDatabase(databasePath);
        var archive = (await database.ExportAsync()) with
        {
            ProfileDebts = [new DebtItem("card", "Card", -1, 0.2, 150)]
        };

        await Assert.ThrowsAsync<InvalidDataException>(() => database.ImportAsync(archive));
    }
}
