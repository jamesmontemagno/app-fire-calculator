using MyFireNumber.Storage;

namespace MyFireNumber.Tests.Data;

public sealed class SqliteRecentActivityRepositoryTests : IAsyncLifetime
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"my-fire-number-recents-{Guid.NewGuid():N}.db3");

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
    public async Task TrackAsync_MovesExistingItemToFrontAndKeepsKindsSeparate()
    {
        var repository = new SqliteRecentActivityRepository(new LocalDatabase(databasePath));
        var now = new DateTime(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc);

        await repository.TrackAsync(new RecentActivityRecord(RecentActivityKind.Calculator, "standard-fire", now));
        await repository.TrackAsync(new RecentActivityRecord(RecentActivityKind.Calculator, "coast-fire", now.AddMinutes(1)));
        await repository.TrackAsync(new RecentActivityRecord(RecentActivityKind.Plan, "plan-1", now.AddMinutes(2)));
        await repository.TrackAsync(new RecentActivityRecord(RecentActivityKind.Calculator, "standard-fire", now.AddMinutes(3)));

        var calculators = await repository.ListAsync(RecentActivityKind.Calculator, 3);
        var plans = await repository.ListAsync(RecentActivityKind.Plan, 3);

        Assert.Collection(
            calculators,
            item => Assert.Equal("standard-fire", item.ItemId),
            item => Assert.Equal("coast-fire", item.ItemId));
        Assert.Collection(plans, item => Assert.Equal("plan-1", item.ItemId));
    }
}
