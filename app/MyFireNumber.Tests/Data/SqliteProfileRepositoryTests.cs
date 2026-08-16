using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Profile;
using MyFireNumber.Storage;

namespace MyFireNumber.Tests.Data;

public sealed class SqliteProfileRepositoryTests : IAsyncLifetime
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"my-fire-number-profile-{Guid.NewGuid():N}.db3");

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
    public async Task SaveAsync_RoundTripsProfileAndAccounts()
    {
        var database = new LocalDatabase(databasePath);
        var profileRepository = new SqliteProfileRepository(database);
        var accountRepository = new SqliteProfileAccountRepository(database);
        var profile = new FinancialProfile(
            "Alex",
            "Alex household",
            2,
            new DateOnly(1990, 8, 16),
            new DateOnly(2040, 8, 16),
            new DateOnly(2045, 8, 16),
            120_000,
            72_000);
        var account = new ProfileAccount(
            "brokerage",
            "Brokerage",
            RetirementAccountType.Taxable,
            50_000,
            6_000,
            0.07,
            18,
            0.04,
            1,
            0.15);

        await profileRepository.SaveAsync(profile);
        await accountRepository.SaveAsync(account);

        Assert.Equal(profile, await profileRepository.GetAsync());
        Assert.Equal([account], await accountRepository.ListAsync());
    }

    [Fact]
    public async Task ClearAsync_RemovesProfileAndAccounts()
    {
        var database = new LocalDatabase(databasePath);
        var profileRepository = new SqliteProfileRepository(database);
        var accountRepository = new SqliteProfileAccountRepository(database);
        await profileRepository.SaveAsync(FinancialProfile.Empty with { DisplayName = "Alex" });
        await accountRepository.SaveAsync(new ProfileAccount(
            "cash",
            "Cash",
            RetirementAccountType.Savings,
            1,
            0,
            0,
            18,
            0.04,
            1,
            0));

        await database.ClearAsync();

        Assert.Equal(FinancialProfile.Empty, await profileRepository.GetAsync());
        Assert.Empty(await accountRepository.ListAsync());
    }
}
