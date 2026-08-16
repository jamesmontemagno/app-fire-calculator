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

    Task LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(FinancialProfile profile, CancellationToken cancellationToken = default);

    int? DerivedCurrentAge { get; }

    int? DerivedPhasedRetirementAge { get; }

    int? DerivedTargetRetirementAge { get; }
}

public sealed class ProfileService(
    IProfileRepository profileRepository,
    ILocalDateProvider localDateProvider) : IProfileService
{
    private FinancialProfile current = FinancialProfile.Empty;

    public FinancialProfile Current => current;

    public int? DerivedCurrentAge => current.BirthDate is DateOnly birthDate
        ? ProfileAgeCalculator.AgeOn(birthDate, localDateProvider.Today)
        : null;

    public int? DerivedPhasedRetirementAge => DeriveAge(current.PhasedRetirementDate);

    public int? DerivedTargetRetirementAge => DeriveAge(current.TargetRetirementDate);

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        current = await profileRepository.GetAsync(cancellationToken);
    }

    public async Task SaveAsync(FinancialProfile profile, CancellationToken cancellationToken = default)
    {
        await profileRepository.SaveAsync(profile, cancellationToken);
        current = profile;
    }

    private int? DeriveAge(DateOnly? date) => current.BirthDate is DateOnly birthDate && date is DateOnly targetDate
        ? ProfileAgeCalculator.AgeOn(birthDate, targetDate)
        : null;
}
