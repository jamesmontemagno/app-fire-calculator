using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyFireNumber.Core.Calculators;
using MyFireNumber.Services;

namespace MyFireNumber.ViewModels;

public partial class CalculatorCatalogViewModel : ObservableObject
{
    private readonly ICalculatorCatalog catalog;
    private readonly INavigationService navigationService;

    public CalculatorCatalogViewModel(ICalculatorCatalog catalog, INavigationService navigationService)
    {
        this.catalog = catalog;
        this.navigationService = navigationService;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredCalculators))]
    private string searchText = string.Empty;

    public IReadOnlyList<CalculatorDefinition> FilteredCalculators => catalog.All
        .Where(definition => string.IsNullOrWhiteSpace(SearchText)
            || definition.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || definition.Summary.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
        .ToArray();

    [RelayCommand]
    private Task OpenCalculatorAsync(CalculatorDefinition definition)
    {
        return navigationService.GoToAsync($"calculator?calculatorId={Uri.EscapeDataString(definition.Id)}");
    }
}