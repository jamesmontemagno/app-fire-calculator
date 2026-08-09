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