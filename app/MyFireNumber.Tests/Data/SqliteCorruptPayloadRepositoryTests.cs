using MyFireNumber.Storage;
using SQLite;

namespace MyFireNumber.Tests.Data;

public sealed class SqliteCorruptPayloadRepositoryTests : IAsyncLifetime
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"my-fire-number-recovery-{Guid.NewGuid():N}.db3");

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
    public async Task QuarantineDraftAsync_PreservesPayloadAndRemovesActiveDraft()
    {
        var database = new LocalDatabase(databasePath);
        var drafts = new SqliteDraftRepository(database);
        var recovery = new SqliteCorruptPayloadRepository(database);
        var updatedAt = new DateTime(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc);
        var draft = new DraftRecord("standard-fire", 1, "{not-json", updatedAt);
        await drafts.SaveAsync(draft);

        await recovery.QuarantineDraftAsync(draft);

        Assert.Null(await drafts.GetAsync("standard-fire"));
        var quarantined = Assert.Single(await recovery.ListAsync());
        Assert.Equal(CorruptPayloadSourceKind.Draft, quarantined.SourceKind);
        Assert.Equal("standard-fire", quarantined.SourceId);
        Assert.Equal("{not-json", quarantined.PayloadJson);
        Assert.Equal(updatedAt, quarantined.OriginalUpdatedAtUtc);
    }

    [Fact]
    public async Task QuarantinePlanAsync_PreservesMetadataAndRemovesActivePlan()
    {
        var database = new LocalDatabase(databasePath);
        var plans = new SqlitePlanRepository(database);
        var recovery = new SqliteCorruptPayloadRepository(database);
        var createdAt = new DateTime(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc);
        var plan = new PlanRecord("plan-1", "coast-fire", "Recovery plan", 1, "{not-json", createdAt, createdAt.AddMinutes(1));
        await plans.SaveAsync(plan);

        await recovery.QuarantinePlanAsync(plan);

        Assert.Null(await plans.GetAsync("plan-1"));
        var quarantined = Assert.Single(await recovery.ListAsync());
        Assert.Equal(CorruptPayloadSourceKind.Plan, quarantined.SourceKind);
        Assert.Equal("Recovery plan", quarantined.DisplayName);
        Assert.Equal("{not-json", quarantined.PayloadJson);
        Assert.Equal(createdAt, quarantined.OriginalCreatedAtUtc);
    }

    [Fact]
    public async Task InitializeAsync_UpgradesVersionTwoDatabaseForRecoveryStorage()
    {
        var legacyConnection = new SQLiteAsyncConnection(databasePath);
        await legacyConnection.ExecuteAsync(
            "CREATE TABLE schema_metadata (Key TEXT PRIMARY KEY NOT NULL, Value TEXT NOT NULL)");
        await legacyConnection.ExecuteAsync(
            "INSERT INTO schema_metadata (Key, Value) VALUES ('schema-version', '2')");
        await legacyConnection.CloseAsync();

        var recovery = new SqliteCorruptPayloadRepository(new LocalDatabase(databasePath));
        var payloads = await recovery.ListAsync();

        var inspectionConnection = new SQLiteAsyncConnection(databasePath);
        var schemaVersion = await inspectionConnection.ExecuteScalarAsync<string>(
            "SELECT Value FROM schema_metadata WHERE Key = 'schema-version'");
        await inspectionConnection.CloseAsync();

        Assert.Empty(payloads);
        Assert.Equal("6", schemaVersion);
    }
}
