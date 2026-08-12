using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyFireNumber.Services;

namespace MyFireNumber.ViewModels;

public partial class WithdrawalRateOnboardingViewModel : ObservableObject
{
    private readonly ICalculatorDefaultsService calculatorDefaultsService;
    private readonly INavigationService navigationService;

    public WithdrawalRateOnboardingViewModel(
        ICalculatorDefaultsService calculatorDefaultsService,
        INavigationService navigationService)
    {
        this.calculatorDefaultsService = calculatorDefaultsService;
        this.navigationService = navigationService;
        withdrawalRatePercent = calculatorDefaultsService.Current.WithdrawalRate * 100;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WithdrawalRateText))]
    private double withdrawalRatePercent;

    public string WithdrawalRateText => $"{WithdrawalRatePercent:0.0}%";

    partial void OnWithdrawalRatePercentChanged(double value)
    {
        var rounded = Math.Round(value, 1);
        if (Math.Abs(value - rounded) > double.Epsilon)
        {
            WithdrawalRatePercent = rounded;
        }
    }

    [RelayCommand]
    private async Task SaveAndContinueAsync()
    {
        calculatorDefaultsService.Save(calculatorDefaultsService.Current with
        {
            WithdrawalRate = WithdrawalRatePercent / 100
        });
        await navigationService.GoToAsync("../onboarding-choice");
    }

    [RelayCommand]
    private Task SkipAsync() => navigationService.GoToAsync("../onboarding-choice");
}
