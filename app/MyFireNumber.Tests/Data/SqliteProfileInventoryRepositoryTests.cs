using MyFireNumber.Core.Calculations;
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

        var incomeItem = new RetirementIncomeSource("salary", "Salary", 120_000, 45, 65, 0.02, false, 0.25);
        var expenseItem = new RetirementExpense("home", "Housing", 30_000, 45, 75);
        var debtItem = new DebtItem("card", "Card", 5_000, 0.2, 150, 75);
        await income.SaveAsync(incomeItem);
        await expenses.SaveAsync(expenseItem);
        await debts.SaveAsync(debtItem);

        Assert.Equal(incomeItem, Assert.Single(await income.ListAsync()));
        Assert.Equal(expenseItem, Assert.Single(await expenses.ListAsync()));
        Assert.Equal(debtItem, Assert.Single(await debts.ListAsync()));
    }

    [Fact]
    public async Task ExpenseSaveAsync_RejectsAnEndAgeBeforeTheStartAge()
    {
        var repository = new SqliteProfileExpenseRepository(new LocalDatabase(databasePath));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => repository.SaveAsync(new RetirementExpense("home", "Housing", 30_000, 75, 74)));
    }

    [Fact]
    public async Task SaveAsync_RejectsNegativeDebtValues()
    {
        var repository = new SqliteProfileDebtRepository(new LocalDatabase(databasePath));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => repository.SaveAsync(new DebtItem("card", "Card", -1, 0.2, 150)));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => repository.SaveAsync(new DebtItem("card", "Card", 100, -0.2, 150)));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => repository.SaveAsync(new DebtItem("card", "Card", 100, 0.2, -150)));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => repository.SaveAsync(new DebtItem("card", "Card", 100, 0.2, 150, -25)));
    }
}
