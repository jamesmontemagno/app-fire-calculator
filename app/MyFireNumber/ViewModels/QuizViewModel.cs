using CommunityToolkit.Mvvm.Input;
using MyFireNumber.Services;

namespace MyFireNumber.ViewModels;

public partial class QuizViewModel
{
    private readonly IOnboardingService onboardingService;
    private readonly INavigationService navigationService;

    public QuizViewModel(IOnboardingService onboardingService, INavigationService navigationService)
    {
        this.onboardingService = onboardingService;
        this.navigationService = navigationService;
    }

    [RelayCommand]
    private async Task SkipAsync()
    {
        onboardingService.Complete();
        await navigationService.GoToAsync("//home");
    }
}