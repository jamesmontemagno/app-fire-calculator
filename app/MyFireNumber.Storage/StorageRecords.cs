using MyFireNumber.Core.Profile;
using MyFireNumber.Core.Calculations;

namespace MyFireNumber.Storage;

public sealed record DraftRecord(
    string CalculatorId,
    int PayloadVersion,
    string PayloadJson,
    DateTime UpdatedAtUtc,
    ScenarioDataMode DataMode = ScenarioDataMode.Standalone,
    long? ProfileRevision = null);

public sealed record PlanRecord(
    string Id,
    string CalculatorId,
    string Name,
    int PayloadVersion,
    string PayloadJson,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    ScenarioDataMode DataMode = ScenarioDataMode.Standalone,
    long? ProfileRevision = null);

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
    DateTime ExportedAtUtc,
    IReadOnlyList<DraftRecord> Drafts,
    IReadOnlyList<PlanRecord> Plans,
    IReadOnlyList<CalculatorPreferenceRecord> CalculatorPreferences,
    IReadOnlyList<RecentActivityRecord> RecentActivity,
    IReadOnlyList<CorruptPayloadRecord> CorruptPayloads)
{
    /// <summary>
    /// The stored profile, or <see cref="FinancialProfile.Empty"/> for an archive written before
    /// profiles existed. Restoring an empty profile still clears the destination profile so an
    /// import never blends two people's data.
    /// </summary>
    public FinancialProfile Profile { get; init; } = FinancialProfile.Empty;

    public IReadOnlyList<RetirementAccount> ProfileAccounts { get; init; } = [];

    public IReadOnlyList<RetirementIncomeSource> ProfileIncome { get; init; } = [];

    public IReadOnlyList<RetirementExpense> ProfileExpenses { get; init; } = [];

    public IReadOnlyList<DebtItem> ProfileDebts { get; init; } = [];
}

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

public interface IProfileRepository
{
    Task<FinancialProfile> GetAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(FinancialProfile profile, CancellationToken cancellationToken = default);
}

public interface IProfileAccountRepository
{
    Task<IReadOnlyList<RetirementAccount>> ListAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(RetirementAccount account, CancellationToken cancellationToken = default);

    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}

public interface IProfileIncomeRepository
{
    Task<IReadOnlyList<RetirementIncomeSource>> ListAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(RetirementIncomeSource income, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}

public interface IProfileExpenseRepository
{
    Task<IReadOnlyList<RetirementExpense>> ListAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(RetirementExpense expense, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}

public interface IProfileDebtRepository
{
    Task<IReadOnlyList<DebtItem>> ListAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(DebtItem debt, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}

public interface IProfileFinancialSnapshotRepository
{
    Task<ProfileFinancialSnapshot> GetAsync(CancellationToken cancellationToken = default);
}