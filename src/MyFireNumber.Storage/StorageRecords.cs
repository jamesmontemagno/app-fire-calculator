namespace MyFireNumber.Storage;

public sealed record DraftRecord(
    string CalculatorId,
    int PayloadVersion,
    string PayloadJson,
    DateTime UpdatedAtUtc);

public sealed record PlanRecord(
    string Id,
    string CalculatorId,
    string Name,
    int PayloadVersion,
    string PayloadJson,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CalculatorPreferenceRecord(
    string CalculatorId,
    bool IsVisible,
    int SortOrder);

public enum RecentActivityKind
{
    Calculator,
    Plan
}

public sealed record RecentActivityRecord(
    RecentActivityKind Kind,
    string ItemId,
    DateTime LastOpenedAtUtc);

public enum CorruptPayloadSourceKind
{
    Draft,
    Plan
}

public sealed record CorruptPayloadRecord(
    string Id,
    CorruptPayloadSourceKind SourceKind,
    string SourceId,
    string CalculatorId,
    string? DisplayName,
    int PayloadVersion,
    string PayloadJson,
    DateTime? OriginalCreatedAtUtc,
    DateTime OriginalUpdatedAtUtc,
    DateTime QuarantinedAtUtc);

public sealed record LocalDataArchive(
    int Version,
    DateTime ExportedAtUtc,
    IReadOnlyList<DraftRecord> Drafts,
    IReadOnlyList<PlanRecord> Plans,
    IReadOnlyList<CalculatorPreferenceRecord> CalculatorPreferences,
    IReadOnlyList<RecentActivityRecord> RecentActivity,
    IReadOnlyList<CorruptPayloadRecord> CorruptPayloads);

public interface ILocalDataArchiveRepository
{
    Task<LocalDataArchive> ExportAsync(CancellationToken cancellationToken = default);

    Task ImportAsync(LocalDataArchive archive, CancellationToken cancellationToken = default);
}

public interface IDraftRepository
{
    Task<DraftRecord?> GetAsync(string calculatorId, CancellationToken cancellationToken = default);

    Task SaveAsync(DraftRecord draft, CancellationToken cancellationToken = default);

    Task DeleteAsync(string calculatorId, CancellationToken cancellationToken = default);
}

public interface IPlanRepository
{
    Task<IReadOnlyList<PlanRecord>> ListAsync(string? calculatorId = null, CancellationToken cancellationToken = default);

    Task<PlanRecord?> GetAsync(string id, CancellationToken cancellationToken = default);

    Task SaveAsync(PlanRecord plan, CancellationToken cancellationToken = default);

    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}

public interface ICalculatorPreferencesRepository
{
    Task<IReadOnlyList<CalculatorPreferenceRecord>> ListAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(CalculatorPreferenceRecord preference, CancellationToken cancellationToken = default);
}

public interface IRecentActivityRepository
{
    Task<IReadOnlyList<RecentActivityRecord>> ListAsync(
        RecentActivityKind kind,
        int limit,
        CancellationToken cancellationToken = default);

    Task TrackAsync(RecentActivityRecord activity, CancellationToken cancellationToken = default);
}

public interface ICorruptPayloadRepository
{
    Task<IReadOnlyList<CorruptPayloadRecord>> ListAsync(CancellationToken cancellationToken = default);

    Task QuarantineDraftAsync(DraftRecord draft, CancellationToken cancellationToken = default);

    Task QuarantinePlanAsync(PlanRecord plan, CancellationToken cancellationToken = default);
}