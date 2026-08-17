using MyFireNumber.Core.Profile;
using MyFireNumber.Storage;

namespace MyFireNumber.Services;

public interface ILocalDateProvider
{
    DateOnly Today { get; }
}

public sealed class LocalDateProvider : ILocalDateProvider
{
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
}

public interface IProfileService
{
    FinancialProfile Current { get; }

    /// <summary>
    /// Increments whenever the profile is replaced from outside the Profile editor, such as a data
    /// reset or backup import, so cached editor state knows it must reload.
    /// </summary>
    long DataRevision { get; }

    /// <summary>
    /// Annual income for new scenarios, applying the same itemised-wins-over-household rule that
    /// linked plans use, so both routes agree. Null when the user has provided neither.
    /// </summary>
    double? EffectiveAnnualIncome { get; }

    /// <inheritdoc cref="EffectiveAnnualIncome"/>
    double? EffectiveAnnualExpenses { get; }

    Task LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(FinancialProfile profile, CancellationToken cancellationToken = default);

    /// <summary>Reloads the profile and signals that any cached editor state is stale.</summary>
    Task NotifyExternalChangeAsync(CancellationToken cancellationToken = default);

    int? DerivedCurrentAge { get; }

    int? DerivedPhasedRetirementAge { get; }

    int? DerivedTargetRetirementAge { get; }
}

public sealed class ProfileService(
    IProfileFinancialSnapshotRepository snapshotRepository,
    IProfileRepository profileRepository,
    ILocalDateProvider localDateProvider) : IProfileService
{
    private FinancialProfile current = FinancialProfile.Empty;
    private double? effectiveAnnualIncome;
    private double? effectiveAnnualExpenses;
    private long dataRevision;

    public FinancialProfile Current => current;

    public long DataRevision => Interlocked.Read(ref dataRevision);

    public double? EffectiveAnnualIncome => effectiveAnnualIncome;

    public double? EffectiveAnnualExpenses => effectiveAnnualExpenses;

    public int? DerivedCurrentAge => DeriveAge(localDateProvider.Today);

    public int? DerivedPhasedRetirementAge => DeriveAge(current.PhasedRetirementDate);

    public int? DerivedTargetRetirementAge => DeriveAge(current.TargetRetirementDate);

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await snapshotRepository.GetAsync(cancellationToken);
        current = snapshot.Profile;
        effectiveAnnualIncome = snapshot.EffectiveAnnualIncome;
        effectiveAnnualExpenses = snapshot.EffectiveAnnualExpenses;
    }

    public async Task SaveAsync(FinancialProfile profile, CancellationToken cancellationToken = default)
    {
        await profileRepository.SaveAsync(profile, cancellationToken);
        current = profile;

        // Re-read the inventory so a saved household figure does not keep overriding, or being
        // overridden by, a stale itemised total.
        await LoadAsync(cancellationToken);
    }

    public async Task NotifyExternalChangeAsync(CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref dataRevision);
        await LoadAsync(cancellationToken);
    }

    // Returns null rather than throwing for a date that precedes the birth date. Validation rejects
    // those profiles, but imported or otherwise unexpected data must not crash age derivation on a
    // startup or calculator-default path.
    private int? DeriveAge(DateOnly? date) =>
        current.BirthDate is DateOnly birthDate && date is DateOnly targetDate && targetDate >= birthDate
            ? ProfileAgeCalculator.AgeOn(birthDate, targetDate)
            : null;
}
