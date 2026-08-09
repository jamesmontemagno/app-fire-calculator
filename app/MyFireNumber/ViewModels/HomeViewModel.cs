using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyFireNumber.Core.Calculators;
using MyFireNumber.Services;
using MyFireNumber.Storage;
using System.Collections.ObjectModel;

namespace MyFireNumber.ViewModels;

public sealed record RecentPlanItem(string Id, string CalculatorId, string Name, string CalculatorTitle);

public partial class HomeViewModel : ObservableObject
{
    private readonly ICalculatorCatalog catalog;
    private readonly INavigationService navigationService;
    private readonly ICalculatorPreferencesRepository preferencesRepository;
    private readonly IPlanRepository planRepository;
    private readonly IRecentActivityRepository recentActivityRepository;

    public HomeViewModel(
        ICalculatorCatalog catalog,
        INavigationService navigationService,
        ICalculatorPreferencesRepository preferencesRepository,
        IPlanRepository planRepository,
        IRecentActivityRepository recentActivityRepository)
    {
        this.catalog = catalog;
        this.navigationService = navigationService;
        this.preferencesRepository = preferencesRepository;
        this.planRepository = planRepository;
        this.recentActivityRepository = recentActivityRepository;
    }

    public ObservableCollection<CalculatorDefinition> FeaturedCalculators { get; } = [];
    public ObservableCollection<CalculatorDefinition> RecentCalculators { get; } = [];
    public ObservableCollection<RecentPlanItem> RecentPlans { get; } = [];

    public bool HasRecentCalculators => RecentCalculators.Count > 0;
    public bool HasRecentPlans => RecentPlans.Count > 0;

    public async Task LoadAsync()
    {
        var preferencesTask = preferencesRepository.ListAsync();
        var recentCalculatorsTask = recentActivityRepository.ListAsync(RecentActivityKind.Calculator, 3);
        var recentPlansTask = recentActivityRepository.ListAsync(RecentActivityKind.Plan, 3);
        var plansTask = planRepository.ListAsync();
        await Task.WhenAll(preferencesTask, recentCalculatorsTask, recentPlansTask, plansTask);

        var preferences = await preferencesTask;
        var preferencesByCalculator = preferences.ToDictionary(preference => preference.CalculatorId);
        var visibleCalculators = catalog.All
            .Select((definition, defaultOrder) => new
            {
                Definition = definition,
                Preference = preferencesByCalculator.GetValueOrDefault(
                    definition.Id,
                    new CalculatorPreferenceRecord(definition.Id, true, defaultOrder))
            })
            .Where(item => item.Preference.IsVisible)
            .OrderBy(item => item.Preference.SortOrder)
            .Select(item => item.Definition);

        FeaturedCalculators.Clear();
        foreach (var calculator in visibleCalculators)
        {
            FeaturedCalculators.Add(calculator);
        }

        RecentCalculators.Clear();
        foreach (var activity in await recentCalculatorsTask)
        {
            var calculator = catalog.All.FirstOrDefault(definition => definition.Id == activity.ItemId);
            if (calculator is not null)
            {
                RecentCalculators.Add(calculator);
            }
        }

        var plansById = (await plansTask).ToDictionary(plan => plan.Id);
        RecentPlans.Clear();
        foreach (var activity in await recentPlansTask)
        {
            if (plansById.TryGetValue(activity.ItemId, out var plan))
            {
                RecentPlans.Add(new RecentPlanItem(
                    plan.Id,
                    plan.CalculatorId,
                    plan.Name,
                    catalog.GetRequired(plan.CalculatorId).Title));
            }
        }

        OnPropertyChanged(nameof(HasRecentCalculators));
        OnPropertyChanged(nameof(HasRecentPlans));
    }

    [RelayCommand]
    private async Task OpenCalculatorAsync(CalculatorDefinition definition)
    {
        await recentActivityRepository.TrackAsync(new RecentActivityRecord(
            RecentActivityKind.Calculator,
            definition.Id,
            DateTime.UtcNow));
        await navigationService.GoToAsync($"calculator?calculatorId={Uri.EscapeDataString(definition.Id)}");
    }

    [RelayCommand]
    private async Task OpenPlanAsync(RecentPlanItem plan)
    {
        var now = DateTime.UtcNow;
        await recentActivityRepository.TrackAsync(new RecentActivityRecord(
            RecentActivityKind.Calculator,
            plan.CalculatorId,
            now));
        await recentActivityRepository.TrackAsync(new RecentActivityRecord(
            RecentActivityKind.Plan,
            plan.Id,
            now));
        await navigationService.GoToAsync(
            $"calculator?calculatorId={Uri.EscapeDataString(plan.CalculatorId)}&planId={Uri.EscapeDataString(plan.Id)}");
    }

    [RelayCommand]
    private Task BrowseCalculatorsAsync()
    {
        return navigationService.GoToAsync("//calculators");
    }
}