using CommunityToolkit.Mvvm.Input;
using MyFireNumber.Services;

namespace MyFireNumber.ViewModels;

public partial class SettingsViewModel
{
    private readonly IOnboardingService onboardingService;
    private readonly INavigationService navigationService;

    public SettingsViewModel(IOnboardingService onboardingService, INavigationService navigationService)
    {
        this.onboardingService = onboardingService;
        this.navigationService = navigationService;
    }

    [RelayCommand]
    private async Task RetakeQuizAsync()
    {
        onboardingService.Reset();
        await navigationService.GoToAsync("quiz");
    }
}