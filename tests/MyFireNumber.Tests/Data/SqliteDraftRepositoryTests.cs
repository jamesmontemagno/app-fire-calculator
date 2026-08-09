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
}