using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyFireNumber.Services;

namespace MyFireNumber.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IOnboardingService onboardingService;
    private readonly INavigationService navigationService;
    private readonly IThemeService themeService;

    public SettingsViewModel(
        IOnboardingService onboardingService,
        INavigationService navigationService,
        IThemeService themeService)
    {
        this.onboardingService = onboardingService;
        this.navigationService = navigationService;
        this.themeService = themeService;
        selectedTheme = themeService.Preference;
    }

    public IReadOnlyList<ThemePreference> ThemeOptions { get; } = Enum.GetValues<ThemePreference>();

    [ObservableProperty]
    private ThemePreference selectedTheme;

    partial void OnSelectedThemeChanged(ThemePreference value) => themeService.Apply(value);

    [RelayCommand]
    private async Task RetakeQuizAsync()
    {
        onboardingService.Reset();
        await navigationService.GoToAsync("quiz");
    }
}