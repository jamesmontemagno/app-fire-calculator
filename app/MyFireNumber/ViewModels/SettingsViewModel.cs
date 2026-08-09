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
        this.appResetService = appResetService;
        this.appDataTransferService = appDataTransferService;
        this.preferencesRepository = preferencesRepository;
        this.confirmationService = confirmationService;
        this.externalLinkService = externalLinkService;
        this.errorPresentationService = errorPresentationService;
        this.themeService = themeService;
        selectedTheme = themeService.Preference;
        LoadCalculatorDefaults();
    }

    public IReadOnlyList<ThemePreference> ThemeOptions { get; } = Enum.GetValues<ThemePreference>();
    public ObservableCollection<CalculatorPreferenceItem> CalculatorPreferences { get; } = [];

    [ObservableProperty]
    private ThemePreference selectedTheme;

    [ObservableProperty]
    private string expectedReturnPercent = string.Empty;

    [ObservableProperty]
    private string inflationRatePercent = string.Empty;

    [ObservableProperty]
    private string withdrawalRatePercent = string.Empty;

    [ObservableProperty]
    private string defaultCurrentAge = string.Empty;

    [ObservableProperty]
    private string defaultRetirementAge = string.Empty;

    [ObservableProperty]
    private string defaultsStatus = string.Empty;

    partial void OnSelectedThemeChanged(ThemePreference value) => themeService.Apply(value);

    [RelayCommand]
    private void SetTheme(ThemePreference preference) => SelectedTheme = preference;

    [RelayCommand]
    private void SaveCalculatorDefaults()
    {
        if (!TryParsePercent(ExpectedReturnPercent, out var expectedReturn)
            || !TryParsePercent(InflationRatePercent, out var inflationRate, allowZero: true)
            || !TryParsePercent(WithdrawalRatePercent, out var withdrawalRate)
            || !int.TryParse(DefaultCurrentAge, NumberStyles.Integer, CultureInfo.CurrentCulture, out var currentAge)
            || !int.TryParse(DefaultRetirementAge, NumberStyles.Integer, CultureInfo.CurrentCulture, out var retirementAge)
            || currentAge is < 18 or > 100
            || retirementAge <= currentAge
            || retirementAge > 100)
        {
            DefaultsStatus = "Enter percentages from 0 to 100 and ages from 18 to 100. Retirement age must be later.";
            return;
        }

        calculatorDefaultsService.Save(new CalculatorDefaults(
            expectedReturn,
            inflationRate,
            withdrawalRate,
            currentAge,
            retirementAge));
        DefaultsStatus = "Saved. These assumptions apply only to new calculators.";
    }

    public async Task LoadAsync()
    {
        LoadCalculatorDefaults();
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

    private void LoadCalculatorDefaults()
    {
        var defaults = calculatorDefaultsService.Current;
        ExpectedReturnPercent = (defaults.ExpectedReturn * 100).ToString("0.##", CultureInfo.CurrentCulture);
        InflationRatePercent = (defaults.InflationRate * 100).ToString("0.##", CultureInfo.CurrentCulture);
        WithdrawalRatePercent = (defaults.WithdrawalRate * 100).ToString("0.##", CultureInfo.CurrentCulture);
        DefaultCurrentAge = defaults.CurrentAge.ToString(CultureInfo.CurrentCulture);
        DefaultRetirementAge = defaults.RetirementAge.ToString(CultureInfo.CurrentCulture);
        DefaultsStatus = string.Empty;
    }

    private static bool TryParsePercent(string text, out double value, bool allowZero = false)
    {
        if (double.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var percent)
            && percent < 100
            && (allowZero ? percent >= 0 : percent > 0))
        {
            value = percent / 100;
            return true;
        }

        value = 0;
        return false;
    }
}