using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyFireNumber.Core.Calculators;
using MyFireNumber.Services;
using MyFireNumber.Storage;

namespace MyFireNumber.ViewModels;

public sealed record PlanListItem(
    string Id,
    string CalculatorId,
    string Name,
    string CalculatorTitle,
    DateTime UpdatedAtUtc,
    string UpdatedDescription);

public enum PlanSortOrder
{
    RecentlyUpdated,
    Name,
    Calculator
}

public sealed record PlanSortOption(PlanSortOrder Order, string Name);

public partial class PlansViewModel : ObservableObject
{
    private readonly IPlanRepository planRepository;
    private readonly IAppBehaviorPreferencesService behaviorPreferencesService;
    private readonly ICalculatorCatalog catalog;
    private readonly IConfirmationService confirmationService;
    private readonly IErrorPresentationService errorPresentationService;
    private readonly INavigationService navigationService;
    private readonly IPlanNamePromptService planNamePromptService;
    private readonly IRecentActivityRepository recentActivityRepository;
    private readonly List<PlanListItem> allPlans = [];

    public PlansViewModel(
        IPlanRepository planRepository,
        IAppBehaviorPreferencesService behaviorPreferencesService,
        ICalculatorCatalog catalog,
        INavigationService navigationService,
        IConfirmationService confirmationService,
        IErrorPresentationService errorPresentationService,
        IPlanNamePromptService planNamePromptService,
        IRecentActivityRepository recentActivityRepository)
    {
        this.planRepository = planRepository;
        this.behaviorPreferencesService = behaviorPreferencesService;
        this.catalog = catalog;
        this.navigationService = navigationService;
        this.confirmationService = confirmationService;
        this.errorPresentationService = errorPresentationService;
        this.planNamePromptService = planNamePromptService;
        this.recentActivityRepository = recentActivityRepository;
        SortOptions.Add(new PlanSortOption(PlanSortOrder.RecentlyUpdated, "Recently updated"));
        SortOptions.Add(new PlanSortOption(PlanSortOrder.Name, "Plan name"));
        SortOptions.Add(new PlanSortOption(PlanSortOrder.Calculator, "Calculator"));
        SelectedSortOption = SortOptions[0];
    }

    public ObservableCollection<PlanListItem> Plans { get; } = [];
    public ObservableCollection<PlanSortOption> SortOptions { get; } = [];

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private PlanSortOption? selectedSortOption;

    public bool HasPlans => Plans.Count > 0;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var records = await planRepository.ListAsync();
            allPlans.Clear();
            foreach (var record in records)
            {
                var title = catalog.GetRequired(record.CalculatorId).Title;
                allPlans.Add(new PlanListItem(
                    record.Id,
                    record.CalculatorId,
                    record.Name,
                    title,
                    record.UpdatedAtUtc,
                    $"Updated {record.UpdatedAtUtc.ToLocalTime():g}"));
            }

            ApplyFilters();
        }
        catch (Exception)
        {
            ErrorMessage = "Saved plans could not be loaded right now.";
            await errorPresentationService.ShowAsync("Plans unavailable", ErrorMessage);
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();

    partial void OnSelectedSortOptionChanged(PlanSortOption? value) => ApplyFilters();

    [RelayCommand]
    private async Task OpenPlanAsync(PlanListItem plan)
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
        var route = $"calculator?calculatorId={Uri.EscapeDataString(plan.CalculatorId)}&planId={Uri.EscapeDataString(plan.Id)}";
        await navigationService.GoToAsync(route);
    }

    [RelayCommand]
    private async Task RenamePlanAsync(PlanListItem plan)
    {
        var name = await planNamePromptService.PromptAsync(
            "Rename saved plan",
            "Enter a new name for this scenario.",
            plan.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        try
        {
            var record = await planRepository.GetAsync(plan.Id);
            if (record is null)
            {
                ErrorMessage = "This saved plan could not be found.";
                return;
            }

            await planRepository.SaveAsync(record with
            {
                Name = name.Trim(),
                UpdatedAtUtc = DateTime.UtcNow
            });
            await LoadAsync();
        }
        catch (Exception)
        {
            ErrorMessage = "This saved plan could not be renamed right now.";
            await errorPresentationService.ShowAsync("Rename unavailable", ErrorMessage);
        }
    }

    [RelayCommand]
    private async Task DuplicatePlanAsync(PlanListItem plan)
    {
        var name = await planNamePromptService.PromptAsync(
            "Duplicate saved plan",
            "Enter a name for the copied scenario.",
            $"{plan.Name} copy");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        try
        {
            var record = await planRepository.GetAsync(plan.Id);
            if (record is null)
            {
                ErrorMessage = "This saved plan could not be found.";
                return;
            }

            var now = DateTime.UtcNow;
            await planRepository.SaveAsync(record with
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = name.Trim(),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
            await LoadAsync();
        }
        catch (Exception)
        {
            ErrorMessage = "This saved plan could not be duplicated right now.";
            await errorPresentationService.ShowAsync("Duplicate unavailable", ErrorMessage);
        }
    }

    [RelayCommand]
    private async Task DeletePlanAsync(PlanListItem plan)
    {
        var confirmed = !behaviorPreferencesService.Current.ConfirmPlanDeletion
            || await confirmationService.ConfirmAsync(
                "Delete saved plan?",
                $"Delete \"{plan.Name}\"? Your current calculator draft will not be changed.",
                "Delete",
                "Cancel");
        if (!confirmed)
        {
            return;
        }

        try
        {
            await planRepository.DeleteAsync(plan.Id);
            behaviorPreferencesService.PerformHaptic();
            await LoadAsync();
        }
        catch (Exception)
        {
            ErrorMessage = "This saved plan could not be deleted right now.";
            await errorPresentationService.ShowAsync("Delete unavailable", ErrorMessage);
        }
    }

    private void ApplyFilters()
    {
        var search = SearchText.Trim();
        var filteredPlans = allPlans.Where(plan =>
            search.Length == 0 || plan.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase));
        filteredPlans = SelectedSortOption?.Order switch
        {
            PlanSortOrder.Name => filteredPlans
                .OrderBy(plan => plan.Name, StringComparer.CurrentCultureIgnoreCase)
                .ThenByDescending(plan => plan.UpdatedAtUtc),
            PlanSortOrder.Calculator => filteredPlans
                .OrderBy(plan => plan.CalculatorTitle, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(plan => plan.Name, StringComparer.CurrentCultureIgnoreCase),
            _ => filteredPlans.OrderByDescending(plan => plan.UpdatedAtUtc)
        };

        Plans.Clear();
        foreach (var plan in filteredPlans)
        {
            Plans.Add(plan);
        }

        OnPropertyChanged(nameof(HasPlans));
    }
}