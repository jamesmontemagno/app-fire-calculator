using MyFireNumber.Core.Calculations;
using MyFireNumber.Storage;
using SQLite;

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
        var expenseItem = new RetirementExpense("home", "Housing", 30_000, 45);
        var debtItem = new DebtItem("card", "Card", 5_000, 0.2, 150);
        await income.SaveAsync(incomeItem);
        await expenses.SaveAsync(expenseItem);
        await debts.SaveAsync(debtItem);

        Assert.Equal(incomeItem, Assert.Single(await income.ListAsync()));
        Assert.Equal(expenseItem, Assert.Single(await expenses.ListAsync()));
        Assert.Equal(debtItem, Assert.Single(await debts.ListAsync()));
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
    }

    [Fact]
    public async Task SchemaSix_ClearsOnlyLegacyIncomeAndExpenses()
    {
        var connection = new SQLiteAsyncConnection(databasePath);
        await connection.ExecuteAsync("CREATE TABLE schema_metadata (Key TEXT PRIMARY KEY, Value TEXT NOT NULL)");
        await connection.ExecuteAsync("INSERT INTO schema_metadata (Key, Value) VALUES ('schema-version', '5')");
        await connection.ExecuteAsync("CREATE TABLE profile_income (Id TEXT PRIMARY KEY, Name TEXT NOT NULL, Amount REAL, Period TEXT NOT NULL, Category TEXT)");
        await connection.ExecuteAsync("CREATE TABLE profile_expenses (Id TEXT PRIMARY KEY, Name TEXT NOT NULL, Amount REAL, Period TEXT NOT NULL, Category TEXT)");
        await connection.ExecuteAsync("CREATE TABLE profile_debts (Id TEXT PRIMARY KEY, Name TEXT NOT NULL, Balance REAL, Rate REAL, MinimumPayment REAL)");
        await connection.ExecuteAsync("INSERT INTO profile_income VALUES ('salary', 'Salary', 1000, 'Monthly', 'Work')");
        await connection.ExecuteAsync("INSERT INTO profile_expenses VALUES ('home', 'Housing', 1000, 'Monthly', 'Home')");
        await connection.ExecuteAsync("INSERT INTO profile_debts VALUES ('card', 'Card', 5000, 0.2, 150)");
        await connection.CloseAsync();

        var database = new LocalDatabase(databasePath);

        Assert.Empty(await new SqliteProfileIncomeRepository(database).ListAsync());
        Assert.Empty(await new SqliteProfileExpenseRepository(database).ListAsync());
        Assert.Equal("card", Assert.Single(await new SqliteProfileDebtRepository(database).ListAsync()).Id);
    }
}
