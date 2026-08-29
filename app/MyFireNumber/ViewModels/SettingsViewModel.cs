using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyFireNumber.Core.Calculators;
using MyFireNumber.Services;
using MyFireNumber.Storage;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;

namespace MyFireNumber.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IOnboardingService onboardingService;
    private readonly INavigationService navigationService;
    private readonly ICalculatorCatalog catalog;
    private readonly ICalculatorDefaultsService calculatorDefaultsService;
    private readonly IAppBehaviorPreferencesService behaviorPreferencesService;
    private readonly IPrivacyModePreferencesService privacyModePreferencesService;
    private readonly ICurrencyPreferencesService currencyPreferencesService;
    private readonly IAppResetService appResetService;
    private readonly IAppDataTransferService appDataTransferService;
    private readonly IConfirmationService confirmationService;
    private readonly IExternalLinkService externalLinkService;
    private readonly IErrorPresentationService errorPresentationService;
    private readonly ICalculatorPreferencesRepository preferencesRepository;
    private readonly IThemeService themeService;

    public SettingsViewModel(
        IOnboardingService onboardingService,
        INavigationService navigationService,
        ICalculatorCatalog catalog,
        ICalculatorDefaultsService calculatorDefaultsService,
        IAppBehaviorPreferencesService behaviorPreferencesService,
        IPrivacyModePreferencesService privacyModePreferencesService,
        ICurrencyPreferencesService currencyPreferencesService,
        IAppResetService appResetService,
        IAppDataTransferService appDataTransferService,
        ICalculatorPreferencesRepository preferencesRepository,
        IConfirmationService confirmationService,
        IExternalLinkService externalLinkService,
        IErrorPresentationService errorPresentationService,
        IThemeService themeService)
    {
        this.onboardingService = onboardingService;
        this.navigationService = navigationService;
        this.catalog = catalog;
        this.calculatorDefaultsService = calculatorDefaultsService;
        this.behaviorPreferencesService = behaviorPreferencesService;
        this.privacyModePreferencesService = privacyModePreferencesService;
        this.currencyPreferencesService = currencyPreferencesService;
        this.appResetService = appResetService;
        this.appDataTransferService = appDataTransferService;
        this.preferencesRepository = preferencesRepository;
        this.confirmationService = confirmationService;
        this.externalLinkService = externalLinkService;
        this.errorPresentationService = errorPresentationService;
        this.themeService = themeService;
        selectedTheme = themeService.Preference;
        var behavior = behaviorPreferencesService.Current;
        selectedLaunchDestination = behavior.LaunchDestination;
        restoreDrafts = behavior.RestoreDrafts;
        confirmPlanDeletion = behavior.ConfirmPlanDeletion;
        hapticsEnabled = behavior.Haptics;
        reduceMotion = behavior.ReduceMotion;
        highContrast = behavior.HighContrast;
        showRecommendedBooks = behavior.ShowRecommendedBooks;
        selectedCurrencyOption = currencyPreferencesService.SelectedOption;
        privacyModeOnStartup = privacyModePreferencesService.PrivacyModeOnStartup;
    }

    public IReadOnlyList<ThemePreference> ThemeOptions { get; } = Enum.GetValues<ThemePreference>();
    public IReadOnlyList<LaunchDestination> LaunchOptions { get; } = Enum.GetValues<LaunchDestination>();
    public IReadOnlyList<string> CurrencyOptions => currencyPreferencesService.Options;
    public ObservableCollection<CalculatorPreferenceItem> CalculatorPreferences { get; } = [];

    [ObservableProperty]
    private ThemePreference selectedTheme;












    [ObservableProperty]
    private string selectedCurrencyOption = CurrencyPreferencesService.DeviceRegion;

    partial void OnSelectedCurrencyOptionChanged(string value) => currencyPreferencesService.Save(value);

    partial void OnSelectedThemeChanged(ThemePreference value) =>
        themeService.Apply(HighContrast ? ThemePreference.Dark : value);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LaunchDestinationDescription))]
    private LaunchDestination selectedLaunchDestination;

    [ObservableProperty]
    private bool restoreDrafts;

    [ObservableProperty]
    private bool confirmPlanDeletion;

    [ObservableProperty]
    private bool hapticsEnabled;

    [ObservableProperty]
    private bool reduceMotion;

    [ObservableProperty]
    private bool highContrast;

    [ObservableProperty]
    private bool showRecommendedBooks;

    /// <summary>
    /// Global override: when on, Home and Accounts privacy toggles are forced back on every time the
    /// app launches, regardless of what the user last left them as. Applied once per launch in
    /// <c>App.xaml.cs</c>; toggling it here only takes effect on the next launch, not immediately.
    /// </summary>
    [ObservableProperty]
    private bool privacyModeOnStartup;

    partial void OnPrivacyModeOnStartupChanged(bool value) => privacyModePreferencesService.PrivacyModeOnStartup = value;

    public string LaunchDestinationDescription => $"{SelectedLaunchDestination} will open after onboarding.";








    partial void OnSelectedLaunchDestinationChanged(LaunchDestination value) => SaveBehaviorPreferences();
    partial void OnRestoreDraftsChanged(bool value) => SaveBehaviorPreferences();
    partial void OnConfirmPlanDeletionChanged(bool value) => SaveBehaviorPreferences();
    partial void OnHapticsEnabledChanged(bool value) => SaveBehaviorPreferences();
    partial void OnReduceMotionChanged(bool value) => SaveBehaviorPreferences();
    partial void OnShowRecommendedBooksChanged(bool value) => SaveBehaviorPreferences();

    partial void OnHighContrastChanged(bool value)
    {
        SaveBehaviorPreferences();
        themeService.Apply(value ? ThemePreference.Dark : SelectedTheme);
    }

    [RelayCommand]
    private void SetTheme(ThemePreference preference) => SelectedTheme = preference;

    [RelayCommand]
    private void SetLaunchDestination(LaunchDestination destination) => SelectedLaunchDestination = destination;

    /// <summary>
    /// Sends the user to Profile, which now owns their age, retirement date, income, spending, and
    /// planning assumptions. Settings keeps only app-level preferences such as currency and theme.
    /// </summary>
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
    private async Task ExportDataAsync()
    {
        try
        {
            await appDataTransferService.ExportAsync();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            await errorPresentationService.ShowAsync("Couldn’t export data", "The backup file could not be created. Please try again.");
        }
    }

    [RelayCommand]
    private async Task ImportDataAsync()
    {
        var confirmed = await confirmationService.ConfirmAsync(
            "Replace local app data?",
            "Importing a backup replaces the drafts, plans, activity, and settings currently stored on this device.",
            "Choose backup",
            "Cancel");
        if (!confirmed)
        {
            return;
        }

        try
        {
            if (await appDataTransferService.PickAndImportAsync())
            {
                await LoadAsync();
                await navigationService.GoToAsync("//home");
            }
        }
        catch (Exception exception) when (exception is InvalidDataException or JsonException or IOException or UnauthorizedAccessException)
        {
            await errorPresentationService.ShowAsync(
                "Couldn’t import backup",
                "The selected file is not a valid My Fire # backup. Your existing data was not changed.");
        }
    }

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
        await navigationService.GoToAsync("onboarding-defaults");
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


    private void SaveBehaviorPreferences()
    {
        behaviorPreferencesService.Save(new AppBehaviorPreferences(
            SelectedLaunchDestination,
            RestoreDrafts,
            ConfirmPlanDeletion,
            HapticsEnabled,
            ReduceMotion,
            HighContrast,
            ShowRecommendedBooks));
    }
}