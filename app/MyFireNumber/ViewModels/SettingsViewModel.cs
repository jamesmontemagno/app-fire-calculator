using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyFireNumber.Core.Calculators;
using MyFireNumber.Services;
using MyFireNumber.Storage;
using System.Collections.ObjectModel;

namespace MyFireNumber.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IOnboardingService onboardingService;
    private readonly INavigationService navigationService;
    private readonly ICalculatorCatalog catalog;
    private readonly IAppResetService appResetService;
    private readonly IConfirmationService confirmationService;
    private readonly IExternalLinkService externalLinkService;
    private readonly ICalculatorPreferencesRepository preferencesRepository;
    private readonly IThemeService themeService;

    public SettingsViewModel(
        IOnboardingService onboardingService,
        INavigationService navigationService,
        ICalculatorCatalog catalog,
        IAppResetService appResetService,
        ICalculatorPreferencesRepository preferencesRepository,
        IConfirmationService confirmationService,
        IExternalLinkService externalLinkService,
        IThemeService themeService)
    {
        this.onboardingService = onboardingService;
        this.navigationService = navigationService;
        this.catalog = catalog;
        this.appResetService = appResetService;
        this.preferencesRepository = preferencesRepository;
        this.confirmationService = confirmationService;
        this.externalLinkService = externalLinkService;
        this.themeService = themeService;
        selectedTheme = themeService.Preference;
    }

    public IReadOnlyList<ThemePreference> ThemeOptions { get; } = Enum.GetValues<ThemePreference>();
    public ObservableCollection<CalculatorPreferenceItem> CalculatorPreferences { get; } = [];

    [ObservableProperty]
    private ThemePreference selectedTheme;

    partial void OnSelectedThemeChanged(ThemePreference value) => themeService.Apply(value);

    [RelayCommand]
    private void SetTheme(ThemePreference preference) => SelectedTheme = preference;

    public async Task LoadAsync()
    {
        var storedPreferences = await preferencesRepository.ListAsync();
        var preferencesByCalculator = storedPreferences.ToDictionary(preference => preference.CalculatorId);
        CalculatorPreferences.Clear();
        foreach (var calculator in catalog.All.Select((definition, index) => new
        {
            Definition = definition,
            Preference = preferencesByCalculator.GetValueOrDefault(
                definition.Id,
                new CalculatorPreferenceRecord(definition.Id, true, index))
        }).OrderBy(item => item.Preference.SortOrder))
        {
            CalculatorPreferences.Add(new CalculatorPreferenceItem(
                calculator.Definition.Id,
                calculator.Definition.Title,
                calculator.Preference.IsVisible,
                calculator.Preference.SortOrder));
        }
    }

    [RelayCommand]
    private async Task ToggleCalculatorVisibilityAsync(CalculatorPreferenceItem item)
    {
        item.IsVisible = !item.IsVisible;
        await SavePreferenceAsync(item);
    }

    [RelayCommand]
    private async Task MoveCalculatorUpAsync(CalculatorPreferenceItem item)
    {
        var index = CalculatorPreferences.IndexOf(item);
        if (index <= 0)
        {
            return;
        }

        await MoveCalculatorAsync(index, index - 1);
    }

    [RelayCommand]
    private async Task MoveCalculatorDownAsync(CalculatorPreferenceItem item)
    {
        var index = CalculatorPreferences.IndexOf(item);
        if (index < 0 || index >= CalculatorPreferences.Count - 1)
        {
            return;
        }

        await MoveCalculatorAsync(index, index + 1);
    }

    [RelayCommand]
    private async Task ResetCalculatorPreferencesAsync()
    {
        var confirmed = await confirmationService.ConfirmAsync(
            "Reset Home calculators?",
            "Show every calculator and restore the default Home order?",
            "Reset",
            "Cancel");
        if (!confirmed)
        {
            return;
        }

        foreach (var calculator in catalog.All.Select((definition, index) => new { definition, index }))
        {
            await preferencesRepository.SaveAsync(new CalculatorPreferenceRecord(
                calculator.definition.Id,
                true,
                calculator.index));
        }

        await LoadAsync();
    }

    [RelayCommand]
    private async Task RetakeQuizAsync()
    {
        onboardingService.Reset();
        await navigationService.GoToAsync("quiz");
    }

    [RelayCommand]
    private Task OpenTermsAsync() => externalLinkService.OpenTermsAsync();

    [RelayCommand]
    private Task OpenPrivacyAsync() => externalLinkService.OpenPrivacyAsync();

    [RelayCommand]
    private async Task ResetAppAsync()
    {
        var confirmed = await confirmationService.ConfirmAsync(
            "Delete all app data?",
            "This permanently deletes your local drafts, plans, calculator settings, and quiz progress. You will start again with the FIRE Quiz.",
            "Delete and reset",
            "Cancel");
        if (!confirmed)
        {
            return;
        }

        await appResetService.ResetAsync();
        CalculatorPreferences.Clear();
        await navigationService.GoToAsync("//home");
        await navigationService.GoToAsync("quiz");
    }

    private async Task MoveCalculatorAsync(int oldIndex, int newIndex)
    {
        CalculatorPreferences.Move(oldIndex, newIndex);
        for (var index = 0; index < CalculatorPreferences.Count; index++)
        {
            var item = CalculatorPreferences[index];
            if (item.SortOrder != index)
            {
                item.SortOrder = index;
                await SavePreferenceAsync(item);
            }
        }
    }

    private Task SavePreferenceAsync(CalculatorPreferenceItem item)
    {
        return preferencesRepository.SaveAsync(new CalculatorPreferenceRecord(
            item.CalculatorId,
            item.IsVisible,
            item.SortOrder));
    }
}