using MyFireNumber.Core.Profile;
using MyFireNumber.Storage;

namespace MyFireNumber.Services;

public interface IProfileScenarioResolver
{
    Task<ProfileResolution<TDraft>> ResolveAsync<TDraft>(
        TDraft draft,
        CancellationToken cancellationToken = default)
        where TDraft : class;

    Task<bool> HasCompatibleDataAsync(string calculatorId, CancellationToken cancellationToken = default);
}

public sealed class ProfileScenarioResolver(
    IProfileFinancialSnapshotRepository snapshotRepository,
    ILocalDateProvider localDateProvider) : IProfileScenarioResolver
{
    public async Task<ProfileResolution<TDraft>> ResolveAsync<TDraft>(
        TDraft draft,
        CancellationToken cancellationToken = default)
        where TDraft : class
    {
        var snapshot = await snapshotRepository.GetAsync(cancellationToken);
        return ProfileDraftResolver.Resolve(draft, snapshot, localDateProvider.Today);
    }

    public async Task<bool> HasCompatibleDataAsync(string calculatorId, CancellationToken cancellationToken = default)
    {
        var snapshot = await snapshotRepository.GetAsync(cancellationToken);
        return calculatorId switch
        {
            "debt-payoff" => snapshot.Debts.Count > 0,
            "healthcare-gap" => snapshot.Profile.BirthDate is not null,
            "sepp-72t" =>
                snapshot.Profile.BirthDate is not null &&
                snapshot.Accounts.Any(account =>
                    account.Type is MyFireNumber.Core.Calculations.RetirementAccountType.Traditional
                        or MyFireNumber.Core.Calculations.RetirementAccountType.Roth),
            "withdrawal-rate" => false,
            "retirement-cash-flow" =>
                snapshot.Profile.BirthDate is not null &&
                snapshot.Profile.PhasedRetirementDate is not null &&
                snapshot.Accounts.Count > 0 &&
                snapshot.EffectiveAnnualExpenses is not null,
            _ => snapshot.Accounts.Count > 0 || snapshot.Income.Count > 0 || snapshot.Expenses.Count > 0
        };
    }
}

public interface IScenarioModePromptService
{
    Task<ScenarioDataMode?> ChooseAsync(string calculatorTitle);
}

public sealed class ScenarioModePromptService : IScenarioModePromptService
{
    public async Task<ScenarioDataMode?> ChooseAsync(string calculatorTitle)
    {
        var choice = await Shell.Current.DisplayActionSheetAsync(
            $"Start {calculatorTitle}",
            "Cancel",
            null,
            "Standalone snapshot",
            "Linked Profile");
        return choice switch
        {
            "Linked Profile" => ScenarioDataMode.LinkedProfile,
            "Standalone snapshot" => ScenarioDataMode.Standalone,
            _ => null
        };
    }
}
