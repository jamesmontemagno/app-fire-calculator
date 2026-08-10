using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyFireNumber.Services;

namespace MyFireNumber.ViewModels;

public partial class DefaultsOnboardingViewModel : ObservableObject
{
    private readonly ICalculatorDefaultsService calculatorDefaultsService;
    private readonly INavigationService navigationService;

    public DefaultsOnboardingViewModel(
        ICalculatorDefaultsService calculatorDefaultsService,
        INavigationService navigationService)
    {
        this.calculatorDefaultsService = calculatorDefaultsService;
        this.navigationService = navigationService;

        var defaults = calculatorDefaultsService.Current;
        expectedReturnPercent = defaults.ExpectedReturn * 100;
        inflationRatePercent = defaults.InflationRate * 100;
        withdrawalRatePercent = defaults.WithdrawalRate * 100;
        currentAge = defaults.CurrentAge;
        retirementAge = defaults.RetirementAge;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpectedReturnText))]
    private double expectedReturnPercent;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InflationRateText))]
    private double inflationRatePercent;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WithdrawalRateText))]
    private double withdrawalRatePercent;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentAgeText), nameof(MinimumRetirementAge))]
    private double currentAge;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RetirementAgeText))]
    private double retirementAge;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValidationMessage))]
    private string validationMessage = string.Empty;

    public string ExpectedReturnText => $"{ExpectedReturnPercent:0.0}%";
    public string InflationRateText => $"{InflationRatePercent:0.0}%";
    public string WithdrawalRateText => $"{WithdrawalRatePercent:0.0}%";
    public string CurrentAgeText => $"{CurrentAge:0}";
    public string RetirementAgeText => $"{RetirementAge:0}";
    public double MinimumRetirementAge => CurrentAge + 1;
    public bool HasValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);

    partial void OnExpectedReturnPercentChanged(double value) =>
        RoundSliderValue(value, rounded => ExpectedReturnPercent = rounded, 1);

    partial void OnInflationRatePercentChanged(double value) =>
        RoundSliderValue(value, rounded => InflationRatePercent = rounded, 1);

    partial void OnWithdrawalRatePercentChanged(double value) =>
        RoundSliderValue(value, rounded => WithdrawalRatePercent = rounded, 1);

    partial void OnCurrentAgeChanged(double value)
    {
        RoundSliderValue(value, rounded => CurrentAge = rounded, 0);
        if (RetirementAge <= CurrentAge)
        {
            RetirementAge = Math.Min(100, CurrentAge + 1);
        }

        ValidationMessage = string.Empty;
    }

    partial void OnRetirementAgeChanged(double value)
    {
        RoundSliderValue(value, rounded => RetirementAge = rounded, 0);
        ValidationMessage = string.Empty;
    }

    [RelayCommand]
    private async Task SaveAndContinueAsync()
    {
        if (RetirementAge <= CurrentAge)
        {
            ValidationMessage = "Retirement age must be later than your current age.";
            return;
        }

        calculatorDefaultsService.Save(new CalculatorDefaults(
            ExpectedReturnPercent / 100,
            InflationRatePercent / 100,
            WithdrawalRatePercent / 100,
            (int)CurrentAge,
            (int)RetirementAge));

        await navigationService.GoToAsync("../onboarding-choice");
    }

    [RelayCommand]
    private Task SkipAsync() => navigationService.GoToAsync("../onboarding-choice");

    private static void RoundSliderValue(double value, Action<double> update, int digits)
    {
        var rounded = Math.Round(value, digits);
        if (Math.Abs(value - rounded) > double.Epsilon)
        {
            update(rounded);
        }
    }
}
