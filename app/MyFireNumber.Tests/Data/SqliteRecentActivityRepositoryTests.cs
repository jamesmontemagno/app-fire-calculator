using MyFireNumber.Storage;
using SQLite;

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

    [Fact]
    public async Task InitializeAsync_UpgradesVersionOneDatabaseForRecentActivity()
    {
        var legacyConnection = new SQLiteAsyncConnection(databasePath);
        await legacyConnection.ExecuteAsync(
            "CREATE TABLE schema_metadata (Key TEXT PRIMARY KEY NOT NULL, Value TEXT NOT NULL)");
        await legacyConnection.ExecuteAsync(
            "INSERT INTO schema_metadata (Key, Value) VALUES ('schema-version', '1')");
        await legacyConnection.CloseAsync();

        var repository = new SqliteRecentActivityRepository(new LocalDatabase(databasePath));
        await repository.TrackAsync(new RecentActivityRecord(
            RecentActivityKind.Calculator,
            "standard-fire",
            DateTime.UtcNow));

        var activities = await repository.ListAsync(RecentActivityKind.Calculator, 3);
        var inspectionConnection = new SQLiteAsyncConnection(databasePath);
        var schemaVersion = await inspectionConnection.ExecuteScalarAsync<string>(
            "SELECT Value FROM schema_metadata WHERE Key = 'schema-version'");
        await inspectionConnection.CloseAsync();

        Assert.Equal("3", schemaVersion);
        Assert.Collection(activities, activity => Assert.Equal("standard-fire", activity.ItemId));
    }
}
