using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyFireNumber.Services;

namespace MyFireNumber.ViewModels;

public partial class WelcomeViewModel : ObservableObject
{
    private readonly IOnboardingService onboardingService;
    private readonly INavigationService navigationService;

    public WelcomeViewModel(
        IOnboardingService onboardingService,
        INavigationService navigationService)
    {
        this.onboardingService = onboardingService;
        this.navigationService = navigationService;
    }

    [RelayCommand]
    private async Task GetStartedAsync()
    {
        onboardingService.MarkWelcomeSeen();
        await navigationService.GoToAsync("onboarding-defaults");
    }
}
