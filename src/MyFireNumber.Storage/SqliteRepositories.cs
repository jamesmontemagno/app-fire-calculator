using System.Globalization;
using SQLite;

namespace MyFireNumber.Storage;

public sealed class SqliteDraftRepository(LocalDatabase database) : IDraftRepository
{
    public async Task<DraftRecord?> GetAsync(string calculatorId, CancellationToken cancellationToken = default)
    {
        await database.InitializeAsync(cancellationToken);
        var entity = await database.Connection.FindAsync<DraftEntity>(calculatorId);
        return entity is null ? null : ToRecord(entity);
    }

    public async Task SaveAsync(DraftRecord draft, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(draft.CalculatorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(draft.PayloadJson);

        await database.InitializeAsync(cancellationToken);
        await database.Connection.InsertOrReplaceAsync(new DraftEntity
        {
            CalculatorId = draft.CalculatorId,
            PayloadVersion = draft.PayloadVersion,
            PayloadJson = draft.PayloadJson,
            UpdatedAtUtc = draft.UpdatedAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)
        });
    }

    public async Task DeleteAsync(string calculatorId, CancellationToken cancellationToken = default)
    {
        await database.InitializeAsync(cancellationToken);
        await database.Connection.DeleteAsync<DraftEntity>(calculatorId);
    }

    private static DraftRecord ToRecord(DraftEntity entity)
    {
        return new DraftRecord(
            entity.CalculatorId,
            entity.PayloadVersion,
            entity.PayloadJson,
            DateTime.Parse(entity.UpdatedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
    }
}

public sealed class SqlitePlanRepository(LocalDatabase database) : IPlanRepository
{
    public async Task<IReadOnlyList<PlanRecord>> ListAsync(string? calculatorId = null, CancellationToken cancellationToken = default)
    {
        await database.InitializeAsync(cancellationToken);
        var query = database.Connection.Table<PlanEntity>();
        var entities = string.IsNullOrWhiteSpace(calculatorId)
            ? await query.ToListAsync()
            : await query.Where(plan => plan.CalculatorId == calculatorId).ToListAsync();

        return entities
            .Select(ToRecord)
            .OrderByDescending(plan => plan.UpdatedAtUtc)
            .ToArray();
    }

    public async Task<PlanRecord?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        await database.InitializeAsync(cancellationToken);
        var entity = await database.Connection.FindAsync<PlanEntity>(id);
        return entity is null ? null : ToRecord(entity);
    }

    public async Task SaveAsync(PlanRecord plan, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plan.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(plan.CalculatorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(plan.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(plan.PayloadJson);

        await database.InitializeAsync(cancellationToken);
        await database.Connection.InsertOrReplaceAsync(new PlanEntity
        {
            Id = plan.Id,
            CalculatorId = plan.CalculatorId,
            Name = plan.Name.Trim(),
            PayloadVersion = plan.PayloadVersion,
            PayloadJson = plan.PayloadJson,
            CreatedAtUtc = plan.CreatedAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            UpdatedAtUtc = plan.UpdatedAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)
        });
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        await database.InitializeAsync(cancellationToken);
        await database.Connection.DeleteAsync<PlanEntity>(id);
    }

    private static PlanRecord ToRecord(PlanEntity entity)
    {
        return new PlanRecord(
            entity.Id,
            entity.CalculatorId,
            entity.Name,
            entity.PayloadVersion,
            entity.PayloadJson,
            DateTime.Parse(entity.CreatedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            DateTime.Parse(entity.UpdatedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
    }
}

public sealed class SqliteCalculatorPreferencesRepository(LocalDatabase database) : ICalculatorPreferencesRepository
{
    public async Task<IReadOnlyList<CalculatorPreferenceRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        await database.InitializeAsync(cancellationToken);
        var preferences = await database.Connection.Table<CalculatorPreferenceEntity>().ToListAsync();
        return preferences
            .Select(preference => new CalculatorPreferenceRecord(preference.CalculatorId, preference.IsVisible, preference.SortOrder))
            .OrderBy(preference => preference.SortOrder)
            .ToArray();
    }

    public async Task SaveAsync(CalculatorPreferenceRecord preference, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(preference.CalculatorId);

        await database.InitializeAsync(cancellationToken);
        await database.Connection.InsertOrReplaceAsync(new CalculatorPreferenceEntity
        {
            CalculatorId = preference.CalculatorId,
            IsVisible = preference.IsVisible,
            SortOrder = preference.SortOrder
        });
    }
}