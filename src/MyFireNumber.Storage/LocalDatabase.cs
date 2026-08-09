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

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        await connection.RunInTransactionAsync(database =>
        {
            database.DeleteAll<DraftEntity>();
            database.DeleteAll<PlanEntity>();
            database.DeleteAll<CalculatorPreferenceEntity>();
            database.DeleteAll<RecentActivityEntity>();
            database.DeleteAll<CorruptPayloadEntity>();
        });
    }

    public async Task<LocalDataArchive> ExportAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        var drafts = await connection.Table<DraftEntity>().ToListAsync();
        var plans = await connection.Table<PlanEntity>().ToListAsync();
        var preferences = await connection.Table<CalculatorPreferenceEntity>().ToListAsync();
        var activity = await connection.Table<RecentActivityEntity>().ToListAsync();
        var corruptPayloads = await connection.Table<CorruptPayloadEntity>().ToListAsync();

        return new LocalDataArchive(
            1,
            DateTime.UtcNow,
            drafts.Select(ToRecord).ToArray(),
            plans.Select(ToRecord).ToArray(),
            preferences.Select(item => new CalculatorPreferenceRecord(item.CalculatorId, item.IsVisible, item.SortOrder)).ToArray(),
            activity.Select(ToRecord).ToArray(),
            corruptPayloads.Select(ToRecord).ToArray());
    }

    public async Task ImportAsync(LocalDataArchive archive, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(archive);
        if (archive.Version != 1)
        {
            throw new InvalidDataException($"Archive version {archive.Version} is not supported.");
        }

        ValidateArchive(archive);
        await InitializeAsync(cancellationToken);
        await connection.RunInTransactionAsync(database =>
        {
            database.DeleteAll<DraftEntity>();
            database.DeleteAll<PlanEntity>();
            database.DeleteAll<CalculatorPreferenceEntity>();
            database.DeleteAll<RecentActivityEntity>();
            database.DeleteAll<CorruptPayloadEntity>();

            database.InsertAll(archive.Drafts.Select(ToEntity));
            database.InsertAll(archive.Plans.Select(ToEntity));
            database.InsertAll(archive.CalculatorPreferences.Select(item => new CalculatorPreferenceEntity
            {
                CalculatorId = item.CalculatorId,
                IsVisible = item.IsVisible,
                SortOrder = item.SortOrder
            }));
            database.InsertAll(archive.RecentActivity.Select(ToEntity));
            database.InsertAll(archive.CorruptPayloads.Select(ToEntity));
        });
    }

    private static void ValidateArchive(LocalDataArchive archive)
    {
        if (archive.Drafts is null ||
            archive.Plans is null ||
            archive.CalculatorPreferences is null ||
            archive.RecentActivity is null ||
            archive.CorruptPayloads is null)
        {
            throw new InvalidDataException("The archive is missing required local data.");
        }

        if (archive.Drafts.Any(item => string.IsNullOrWhiteSpace(item.CalculatorId) || string.IsNullOrWhiteSpace(item.PayloadJson)) ||
            archive.Plans.Any(item => string.IsNullOrWhiteSpace(item.Id) || string.IsNullOrWhiteSpace(item.CalculatorId) || string.IsNullOrWhiteSpace(item.Name) || string.IsNullOrWhiteSpace(item.PayloadJson)) ||
            archive.CalculatorPreferences.Any(item => string.IsNullOrWhiteSpace(item.CalculatorId) || item.SortOrder < 0) ||
            archive.RecentActivity.Any(item => string.IsNullOrWhiteSpace(item.ItemId)) ||
            archive.CorruptPayloads.Any(item => string.IsNullOrWhiteSpace(item.Id) || string.IsNullOrWhiteSpace(item.SourceId) || string.IsNullOrWhiteSpace(item.CalculatorId) || string.IsNullOrWhiteSpace(item.PayloadJson)))
        {
            throw new InvalidDataException("The archive contains invalid local data.");
        }
    }

    private static DraftRecord ToRecord(DraftEntity item) => new(
        item.CalculatorId,
        item.PayloadVersion,
        item.PayloadJson,
        ParseDate(item.UpdatedAtUtc));

    private static PlanRecord ToRecord(PlanEntity item) => new(
        item.Id,
        item.CalculatorId,
        item.Name,
        item.PayloadVersion,
        item.PayloadJson,
        ParseDate(item.CreatedAtUtc),
        ParseDate(item.UpdatedAtUtc));

    private static RecentActivityRecord ToRecord(RecentActivityEntity item) => new(
        Enum.Parse<RecentActivityKind>(item.Kind),
        item.ItemId,
        ParseDate(item.LastOpenedAtUtc));

    private static CorruptPayloadRecord ToRecord(CorruptPayloadEntity item) => new(
        item.Id,
        Enum.Parse<CorruptPayloadSourceKind>(item.SourceKind),
        item.SourceId,
        item.CalculatorId,
        item.DisplayName,
        item.PayloadVersion,
        item.PayloadJson,
        item.OriginalCreatedAtUtc is null ? null : ParseDate(item.OriginalCreatedAtUtc),
        ParseDate(item.OriginalUpdatedAtUtc),
        ParseDate(item.QuarantinedAtUtc));

    private static DraftEntity ToEntity(DraftRecord item) => new()
    {
        CalculatorId = item.CalculatorId,
        PayloadVersion = item.PayloadVersion,
        PayloadJson = item.PayloadJson,
        UpdatedAtUtc = FormatDate(item.UpdatedAtUtc)
    };

    private static PlanEntity ToEntity(PlanRecord item) => new()
    {
        Id = item.Id,
        CalculatorId = item.CalculatorId,
        Name = item.Name,
        PayloadVersion = item.PayloadVersion,
        PayloadJson = item.PayloadJson,
        CreatedAtUtc = FormatDate(item.CreatedAtUtc),
        UpdatedAtUtc = FormatDate(item.UpdatedAtUtc)
    };

    private static RecentActivityEntity ToEntity(RecentActivityRecord item) => new()
    {
        Key = $"{item.Kind}:{item.ItemId}",
        Kind = item.Kind.ToString(),
        ItemId = item.ItemId,
        LastOpenedAtUtc = FormatDate(item.LastOpenedAtUtc)
    };

    private static CorruptPayloadEntity ToEntity(CorruptPayloadRecord item) => new()
    {
        Id = item.Id,
        SourceKind = item.SourceKind.ToString(),
        SourceId = item.SourceId,
        CalculatorId = item.CalculatorId,
        DisplayName = item.DisplayName,
        PayloadVersion = item.PayloadVersion,
        PayloadJson = item.PayloadJson,
        OriginalCreatedAtUtc = item.OriginalCreatedAtUtc is null ? null : FormatDate(item.OriginalCreatedAtUtc.Value),
        OriginalUpdatedAtUtc = FormatDate(item.OriginalUpdatedAtUtc),
        QuarantinedAtUtc = FormatDate(item.QuarantinedAtUtc)
    };

    private static DateTime ParseDate(string value) => DateTime.Parse(
        value,
        System.Globalization.CultureInfo.InvariantCulture,
        System.Globalization.DateTimeStyles.RoundtripKind);

    private static string FormatDate(DateTime value) => value
        .ToUniversalTime()
        .ToString("O", System.Globalization.CultureInfo.InvariantCulture);

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