using SQLite;

namespace MyFireNumber.Storage;

public sealed class LocalDatabase
{
    private const string SchemaVersionKey = "schema-version";
    private const int CurrentSchemaVersion = 3;

    private readonly SQLiteAsyncConnection connection;
    private readonly SemaphoreSlim initializationLock = new(1, 1);
    private bool isInitialized;

    public LocalDatabase(string databasePath)
    {
        connection = new SQLiteAsyncConnection(databasePath);
    }

    internal SQLiteAsyncConnection Connection => connection;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (isInitialized)
        {
            return;
        }

        await initializationLock.WaitAsync(cancellationToken);
        try
        {
            if (isInitialized)
            {
                return;
            }

            await connection.CreateTableAsync<SchemaMetadataEntity>();

            var schemaVersion = await connection.Table<SchemaMetadataEntity>()
                .Where(entry => entry.Key == SchemaVersionKey)
                .FirstOrDefaultAsync();

            if (schemaVersion is null)
            {
                await CreateCurrentSchemaAsync();
                await connection.InsertAsync(new SchemaMetadataEntity
                {
                    Key = SchemaVersionKey,
                    Value = CurrentSchemaVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)
                });
            }
            else if (!int.TryParse(schemaVersion.Value, out var storedVersion) || storedVersion > CurrentSchemaVersion)
            {
                throw new InvalidOperationException("The local data schema is not supported by this version of My Fire Number.");
            }
            else
            {
                await ApplyMigrationsAsync(storedVersion, schemaVersion);
            }

            isInitialized = true;
        }
        finally
        {
            initializationLock.Release();
        }
    }

    private async Task CreateCurrentSchemaAsync()
    {
        await connection.CreateTableAsync<DraftEntity>();
        await connection.CreateTableAsync<PlanEntity>();
        await connection.CreateTableAsync<CalculatorPreferenceEntity>();
        await connection.CreateTableAsync<RecentActivityEntity>();
        await connection.CreateTableAsync<CorruptPayloadEntity>();
    }

    private async Task ApplyMigrationsAsync(int storedVersion, SchemaMetadataEntity schemaVersion)
    {
        if (storedVersion < 1)
        {
            await connection.CreateTableAsync<DraftEntity>();
            await connection.CreateTableAsync<PlanEntity>();
            await connection.CreateTableAsync<CalculatorPreferenceEntity>();
        }

        if (storedVersion < 2)
        {
            await connection.CreateTableAsync<RecentActivityEntity>();
        }

        if (storedVersion < 3)
        {
            await connection.CreateTableAsync<CorruptPayloadEntity>();
        }

        if (storedVersion != CurrentSchemaVersion)
        {
            schemaVersion.Value = CurrentSchemaVersion.ToString(System.Globalization.CultureInfo.InvariantCulture);
            await connection.UpdateAsync(schemaVersion);
        }
    }
}

[Table("drafts")]
internal sealed class DraftEntity
{
    [PrimaryKey]
    public string CalculatorId { get; set; } = string.Empty;

    [NotNull]
    public int PayloadVersion { get; set; }

    [NotNull]
    public string PayloadJson { get; set; } = string.Empty;

    [NotNull]
    public string UpdatedAtUtc { get; set; } = string.Empty;
}

[Table("plans")]
internal sealed class PlanEntity
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;

    [NotNull]
    public string CalculatorId { get; set; } = string.Empty;

    [NotNull]
    public string Name { get; set; } = string.Empty;

    [NotNull]
    public int PayloadVersion { get; set; }

    [NotNull]
    public string PayloadJson { get; set; } = string.Empty;

    [NotNull]
    public string CreatedAtUtc { get; set; } = string.Empty;

    [NotNull]
    public string UpdatedAtUtc { get; set; } = string.Empty;
}

[Table("calculator_preferences")]
internal sealed class CalculatorPreferenceEntity
{
    [PrimaryKey]
    public string CalculatorId { get; set; } = string.Empty;

    [NotNull]
    public bool IsVisible { get; set; }

    [NotNull]
    public int SortOrder { get; set; }
}

[Table("recent_activity")]
internal sealed class RecentActivityEntity
{
    [PrimaryKey]
    public string Key { get; set; } = string.Empty;

    [NotNull]
    public string Kind { get; set; } = string.Empty;

    [NotNull]
    public string ItemId { get; set; } = string.Empty;

    [NotNull]
    public string LastOpenedAtUtc { get; set; } = string.Empty;
}

[Table("schema_metadata")]
internal sealed class SchemaMetadataEntity
{
    [PrimaryKey]
    public string Key { get; set; } = string.Empty;

    [NotNull]
    public string Value { get; set; } = string.Empty;
}

[Table("corrupt_payloads")]
internal sealed class CorruptPayloadEntity
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;

    [NotNull]
    public string SourceKind { get; set; } = string.Empty;

    [NotNull]
    public string SourceId { get; set; } = string.Empty;

    [NotNull]
    public string CalculatorId { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    [NotNull]
    public int PayloadVersion { get; set; }

    [NotNull]
    public string PayloadJson { get; set; } = string.Empty;

    public string? OriginalCreatedAtUtc { get; set; }

    [NotNull]
    public string OriginalUpdatedAtUtc { get; set; } = string.Empty;

    [NotNull]
    public string QuarantinedAtUtc { get; set; } = string.Empty;
}