using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyFireNumber.Core.Calculators;
using MyFireNumber.Core.Profile;
using MyFireNumber.Services;
using MyFireNumber.Storage;

namespace MyFireNumber.ViewModels;

public sealed class CalculatorGroup(string name, string iconGlyph, IEnumerable<CalculatorDefinition> calculators)
    : List<CalculatorDefinition>(calculators)
{
    public string Name { get; } = name;
    public string IconGlyph { get; } = iconGlyph;
}

public partial class CalculatorCatalogViewModel : ObservableObject
{
    private static readonly IReadOnlyDictionary<string, string> Categories =
        new Dictionary<string, string>
        {
            ["standard-fire"] = "FIRE",
            ["coast-fire"] = "FIRE",
            ["lean-fire"] = "FIRE",
            ["fat-fire"] = "FIRE",
            ["barista-fire"] = "FIRE",
            ["reverse-fire"] = "FIRE",
            ["withdrawal-rate"] = "Finance",
            ["savings-rate"] = "Finance",
            ["debt-payoff"] = "Finance",
            ["healthcare-gap"] = "Finance",
            ["sepp-72t"] = "Finance",
            ["roth-conversion"] = "Finance",
            ["interest-calculator"] = "Cash Flow",
            ["retirement-cash-flow"] = "Cash Flow"
        };
    private static readonly (string Name, string IconGlyph)[] CategoryOrder =
    [
        ("FIRE", "\uf06d"),
        ("Finance", "\uf201"),
        ("Cash Flow", "\uf53d")
    ];
    private readonly ICalculatorCatalog catalog;
    private readonly INavigationService navigationService;
    private readonly IRecentActivityRepository recentActivityRepository;
    private readonly IProfileScenarioResolver profileScenarioResolver;
    private readonly IScenarioModePromptService scenarioModePromptService;

    public CalculatorCatalogViewModel(
        ICalculatorCatalog catalog,
        INavigationService navigationService,
        IRecentActivityRepository recentActivityRepository,
        IProfileScenarioResolver profileScenarioResolver,
        IScenarioModePromptService scenarioModePromptService)
    {
        this.catalog = catalog;
        this.navigationService = navigationService;
        this.recentActivityRepository = recentActivityRepository;
        this.profileScenarioResolver = profileScenarioResolver;
        this.scenarioModePromptService = scenarioModePromptService;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredCalculatorGroups))]
    private string searchText = string.Empty;

    public IReadOnlyList<CalculatorGroup> FilteredCalculatorGroups
    {
        get
        {
            var matches = catalog.All
                .Where(definition => string.IsNullOrWhiteSpace(SearchText)
                    || definition.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                    || definition.Summary.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            return CategoryOrder
                .Select(category => new CalculatorGroup(
                    category.Name,
                    category.IconGlyph,
                    matches.Where(definition => Categories[definition.Id] == category.Name)))
                .Where(group => group.Count > 0)
                .ToArray();
        }
    }

    [RelayCommand]
    private async Task OpenCalculatorAsync(CalculatorDefinition definition)
    {
        var canChooseProfileLink = definition.Id == "retirement-cash-flow"
            || await profileScenarioResolver.HasCompatibleDataAsync(definition.Id);
        var dataMode = canChooseProfileLink
            ? await scenarioModePromptService.ChooseAsync(definition.Title)
            : ScenarioDataMode.Standalone;
        if (dataMode is null)
        {
            return;
        }

        await recentActivityRepository.TrackAsync(new RecentActivityRecord(
            RecentActivityKind.Calculator,
            definition.Id,
            DateTime.UtcNow));
        await navigationService.GoToAsync(CalculatorRoutes.Build(definition.Id, dataMode: dataMode));
    }
}