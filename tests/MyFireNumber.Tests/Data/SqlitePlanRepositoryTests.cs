using MyFireNumber.Storage;

namespace MyFireNumber.Tests.Data;

public sealed class SqlitePlanRepositoryTests : IAsyncLifetime
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"my-fire-number-{Guid.NewGuid():N}.db3");

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }

        return Task.CompletedTask;
    }

    [Fact]
    public async Task ListAsync_ReturnsMatchingPlansNewestFirst()
    {
        var repository = new SqlitePlanRepository(new LocalDatabase(databasePath));
        var createdAt = new DateTime(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc);
        await repository.SaveAsync(new PlanRecord("older", "standard-fire", "Older", 1, "{}", createdAt, createdAt));
        await repository.SaveAsync(new PlanRecord("newer", "standard-fire", "Newer", 1, "{}", createdAt, createdAt.AddMinutes(1)));
        await repository.SaveAsync(new PlanRecord("coast", "coast-fire", "Coast", 1, "{}", createdAt, createdAt.AddMinutes(2)));

        var plans = await repository.ListAsync("standard-fire");

        Assert.Collection(
            plans,
            plan => Assert.Equal("newer", plan.Id),
            plan => Assert.Equal("older", plan.Id));
    }

    [Fact]
    public async Task SaveAsync_UpsertsCalculatorPreference()
    {
        var repository = new SqliteCalculatorPreferencesRepository(new LocalDatabase(databasePath));

        await repository.SaveAsync(new CalculatorPreferenceRecord("standard-fire", true, 1));
        await repository.SaveAsync(new CalculatorPreferenceRecord("standard-fire", false, 3));

        var preferences = await repository.ListAsync();

        var preference = Assert.Single(preferences);
        Assert.False(preference.IsVisible);
        Assert.Equal(3, preference.SortOrder);
    }
}