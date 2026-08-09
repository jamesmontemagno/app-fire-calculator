using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyFireNumber.Core.Calculators;
using MyFireNumber.Services;

namespace MyFireNumber.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly INavigationService navigationService;

    public HomeViewModel(ICalculatorCatalog catalog, INavigationService navigationService)
    {
        FeaturedCalculators = catalog.All.Take(3).ToArray();
        this.navigationService = navigationService;
    }

    public IReadOnlyList<CalculatorDefinition> FeaturedCalculators { get; }

    [RelayCommand]
    private Task OpenCalculatorAsync(CalculatorDefinition definition)
    {
        return navigationService.GoToAsync($"calculator?calculatorId={Uri.EscapeDataString(definition.Id)}");
    }

    [RelayCommand]
    private Task BrowseCalculatorsAsync()
    {
        return navigationService.GoToAsync("//calculators");
    }
}