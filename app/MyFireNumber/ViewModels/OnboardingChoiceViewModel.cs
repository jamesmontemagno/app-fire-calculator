using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyFireNumber.Services;
using MyFireNumber.Storage;

namespace MyFireNumber.ViewModels;

public partial class OnboardingChoiceViewModel : ObservableObject
{
    private readonly IOnboardingService onboardingService;
    private readonly INavigationService navigationService;
    private readonly IRecentActivityRepository recentActivityRepository;

    public OnboardingChoiceViewModel(
        IOnboardingService onboardingService,
        INavigationService navigationService,
        IRecentActivityRepository recentActivityRepository)
    {
        this.onboardingService = onboardingService;
        this.navigationService = navigationService;
        this.recentActivityRepository = recentActivityRepository;
    }

    [RelayCommand]
    private Task TakeQuizAsync() => navigationService.GoToAsync("quiz");

    [RelayCommand]
    private async Task StartStandardFireAsync()
    {
        onboardingService.SetRecommendation("standard-fire");
        onboardingService.Complete();
        await recentActivityRepository.TrackAsync(new RecentActivityRecord(
            RecentActivityKind.Calculator,
            "standard-fire",
            DateTime.UtcNow));
        await navigationService.GoToAsync("//home");
        await navigationService.GoToAsync(CalculatorRoutes.Build("standard-fire"));
    }

    [RelayCommand]
    private async Task ExploreOnMyOwnAsync()
    {
        onboardingService.Complete();
        await navigationService.GoToAsync("//home");
    }
}
