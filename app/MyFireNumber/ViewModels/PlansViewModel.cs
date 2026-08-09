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

    public PlansViewModel(
        IPlanRepository planRepository,
        ICalculatorCatalog catalog,
        INavigationService navigationService,
        IConfirmationService confirmationService)
    {
        this.planRepository = planRepository;
        this.catalog = catalog;
        this.navigationService = navigationService;
        this.confirmationService = confirmationService;
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