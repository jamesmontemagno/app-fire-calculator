using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyFireNumber.Core.Calculators;
using MyFireNumber.Services;
using MyFireNumber.Storage;

namespace MyFireNumber.ViewModels;

public partial class CalculatorCatalogViewModel : ObservableObject
{
    private readonly ICalculatorCatalog catalog;
    private readonly INavigationService navigationService;
    private readonly IRecentActivityRepository recentActivityRepository;

    public CalculatorCatalogViewModel(
        ICalculatorCatalog catalog,
        INavigationService navigationService,
        IRecentActivityRepository recentActivityRepository)
    {
        this.catalog = catalog;
        this.navigationService = navigationService;
        this.recentActivityRepository = recentActivityRepository;
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
    private async Task OpenCalculatorAsync(CalculatorDefinition definition)
    {
        await recentActivityRepository.TrackAsync(new RecentActivityRecord(
            RecentActivityKind.Calculator,
            definition.Id,
            DateTime.UtcNow));
        await navigationService.GoToAsync($"calculator?calculatorId={Uri.EscapeDataString(definition.Id)}");
    }
}