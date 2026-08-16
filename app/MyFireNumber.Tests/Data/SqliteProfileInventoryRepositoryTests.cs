using MyFireNumber.Core.Presentation;
using MyFireNumber.Core.Profile;
using MyFireNumber.Storage;

namespace MyFireNumber.Tests.Data;

public sealed class SqliteProfileInventoryRepositoryTests : IAsyncLifetime
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"profile-inventory-{Guid.NewGuid():N}.db3");

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        if (File.Exists(databasePath)) File.Delete(databasePath);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Repositories_RoundTripIncomeExpensesAndDebts()
    {
        var database = new LocalDatabase(databasePath);
        var income = new SqliteProfileIncomeRepository(database);
        var expenses = new SqliteProfileExpenseRepository(database);
        var debts = new SqliteProfileDebtRepository(database);

        await income.SaveAsync(new ProfileIncome("salary", "Salary", 10_000, CurrencyPeriod.Monthly, "Work"));
        await expenses.SaveAsync(new ProfileExpense("home", "Housing", 2_500, CurrencyPeriod.Monthly, "Home"));
        await debts.SaveAsync(new ProfileDebt("card", "Card", 5_000, 0.2, 150));

        Assert.Single(await income.ListAsync());
        Assert.Single(await expenses.ListAsync());
        Assert.Single(await debts.ListAsync());
    }

    [Fact]
    public async Task SaveAsync_RejectsNegativeDebtValues()
    {
        var repository = new SqliteProfileDebtRepository(new LocalDatabase(databasePath));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => repository.SaveAsync(new ProfileDebt("card", "Card", -1, 0.2, 150)));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => repository.SaveAsync(new ProfileDebt("card", "Card", 100, -0.2, 150)));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => repository.SaveAsync(new ProfileDebt("card", "Card", 100, 0.2, -150)));
    }
}
