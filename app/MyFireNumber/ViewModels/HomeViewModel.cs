using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyFireNumber.Core.Calculators;
using MyFireNumber.Services;
using MyFireNumber.Storage;
using System.Collections.ObjectModel;

namespace MyFireNumber.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly ICalculatorCatalog catalog;
    private readonly INavigationService navigationService;
    private readonly ICalculatorPreferencesRepository preferencesRepository;

    public HomeViewModel(
        ICalculatorCatalog catalog,
        INavigationService navigationService,
        ICalculatorPreferencesRepository preferencesRepository)
    {
        this.catalog = catalog;
        this.navigationService = navigationService;
        this.preferencesRepository = preferencesRepository;
    }

    public ObservableCollection<CalculatorDefinition> FeaturedCalculators { get; } = [];

    public async Task LoadAsync()
    {
        var preferences = await preferencesRepository.ListAsync();
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
    }

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