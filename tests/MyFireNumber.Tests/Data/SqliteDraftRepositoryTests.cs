using MyFireNumber.Storage;

namespace MyFireNumber.Tests.Data;

public sealed class SqliteDraftRepositoryTests : IAsyncLifetime
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
    public async Task SaveAsync_UpsertsAndRoundTripsDraft()
    {
        var repository = new SqliteDraftRepository(new LocalDatabase(databasePath));
        var updatedAt = new DateTime(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc);
        var original = new DraftRecord("standard-fire", 1, "{\"currentAge\":30}", updatedAt);

        await repository.SaveAsync(original);
        await repository.SaveAsync(original with { PayloadJson = "{\"currentAge\":31}" });

        var restored = await repository.GetAsync("standard-fire");

        Assert.NotNull(restored);
        Assert.Equal("standard-fire", restored.CalculatorId);
        Assert.Equal(1, restored.PayloadVersion);
        Assert.Equal("{\"currentAge\":31}", restored.PayloadJson);
        Assert.Equal(updatedAt, restored.UpdatedAtUtc);
    }

    [Fact]
    public async Task ClearAsync_RemovesAllLocalData()
    {
        var database = new LocalDatabase(databasePath);
        var drafts = new SqliteDraftRepository(database);
        var plans = new SqlitePlanRepository(database);
        var preferences = new SqliteCalculatorPreferencesRepository(database);
        var activity = new SqliteRecentActivityRepository(database);
        var corruptPayloads = new SqliteCorruptPayloadRepository(database);
        var now = new DateTime(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc);
        var draft = new DraftRecord("standard-fire", 1, "{}", now);

        await drafts.SaveAsync(draft);
        await plans.SaveAsync(new PlanRecord("plan", "standard-fire", "Plan", 1, "{}", now, now));
        await preferences.SaveAsync(new CalculatorPreferenceRecord("standard-fire", false, 0));
        await activity.TrackAsync(new RecentActivityRecord(RecentActivityKind.Calculator, "standard-fire", now));
        await corruptPayloads.QuarantineDraftAsync(draft);

        await database.ClearAsync();

        Assert.Null(await drafts.GetAsync("standard-fire"));
        Assert.Empty(await plans.ListAsync());
        Assert.Empty(await preferences.ListAsync());
        Assert.Empty(await activity.ListAsync(RecentActivityKind.Calculator, 1));
        Assert.Empty(await corruptPayloads.ListAsync());
    }
}