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
    public async Task ImportAsync_RejectsUnsupportedVersionBeforeMutation()
    {
        var database = new LocalDatabase(databasePath);
        var repository = new SqliteDraftRepository(database);
        var now = new DateTime(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc);
        await repository.SaveAsync(new DraftRecord("standard-fire", 1, "{}", now));

        var invalidArchive = (await database.ExportAsync()) with { Version = 99 };

        await Assert.ThrowsAsync<InvalidDataException>(() => database.ImportAsync(invalidArchive));
        Assert.NotNull(await repository.GetAsync("standard-fire"));
    }
}
