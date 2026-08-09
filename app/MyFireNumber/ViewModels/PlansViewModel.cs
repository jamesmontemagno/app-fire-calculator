using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyFireNumber.Core.Calculators;
using MyFireNumber.Services;
using MyFireNumber.Storage;

namespace MyFireNumber.ViewModels;

public sealed record PlanListItem(string Id, string CalculatorId, string Name, string CalculatorTitle, string UpdatedDescription);

public partial class PlansViewModel : ObservableObject
{
    private readonly IPlanRepository planRepository;
    private readonly ICalculatorCatalog catalog;
    private readonly IConfirmationService confirmationService;
    private readonly INavigationService navigationService;
    private readonly IPlanNamePromptService planNamePromptService;

    public PlansViewModel(
        IPlanRepository planRepository,
        ICalculatorCatalog catalog,
        INavigationService navigationService,
        IConfirmationService confirmationService,
        IPlanNamePromptService planNamePromptService)
    {
        this.planRepository = planRepository;
        this.catalog = catalog;
        this.navigationService = navigationService;
        this.confirmationService = confirmationService;
        this.planNamePromptService = planNamePromptService;
    }

    public ObservableCollection<PlanListItem> Plans { get; } = [];

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    public bool HasPlans => Plans.Count > 0;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var records = await planRepository.ListAsync();
            Plans.Clear();
            foreach (var record in records)
            {
                var title = catalog.GetRequired(record.CalculatorId).Title;
                Plans.Add(new PlanListItem(
                    record.Id,
                    record.CalculatorId,
                    record.Name,
                    title,
                    $"Updated {record.UpdatedAtUtc.ToLocalTime():g}"));
            }

            OnPropertyChanged(nameof(HasPlans));
        }
        catch (Exception)
        {
            ErrorMessage = "Saved plans could not be loaded right now.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private Task OpenPlanAsync(PlanListItem plan)
    {
        var route = $"calculator?calculatorId={Uri.EscapeDataString(plan.CalculatorId)}&planId={Uri.EscapeDataString(plan.Id)}";
        return navigationService.GoToAsync(route);
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
        }
    }

    [RelayCommand]
    private async Task DeletePlanAsync(PlanListItem plan)
    {
        var confirmed = await confirmationService.ConfirmAsync(
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
            Plans.Remove(plan);
            OnPropertyChanged(nameof(HasPlans));
        }
        catch (Exception)
        {
            ErrorMessage = "This saved plan could not be deleted right now.";
        }
    }
}