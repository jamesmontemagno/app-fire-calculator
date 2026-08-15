using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyFireNumber.Services;

namespace MyFireNumber.ViewModels;

public partial class DefaultsOnboardingViewModel : ObservableObject
{
    private const double RoundingTolerance = 0.000001;
    private readonly ICalculatorDefaultsService calculatorDefaultsService;
    private readonly ICurrencyPreferencesService currencyPreferencesService;
    private readonly INavigationService navigationService;

    public DefaultsOnboardingViewModel(
        ICalculatorDefaultsService calculatorDefaultsService,
        ICurrencyPreferencesService currencyPreferencesService,
        INavigationService navigationService)
    {
        this.calculatorDefaultsService = calculatorDefaultsService;
        this.currencyPreferencesService = currencyPreferencesService;
        this.navigationService = navigationService;

        var defaults = calculatorDefaultsService.Current;
        currentAge = defaults.CurrentAge;
        annualIncome = defaults.AnnualIncome;
        annualExpenses = defaults.AnnualExpenses;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentAgeText))]
    private double currentAge;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AnnualIncomeText))]
    private double annualIncome;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AnnualExpensesText))]
    private double annualExpenses;

    public string CurrentAgeText => $"{CurrentAge:0}";
    public string AnnualIncomeText => currencyPreferencesService.Format(AnnualIncome);
    public string AnnualExpensesText => currencyPreferencesService.Format(AnnualExpenses);
    public double MaximumAnnualIncome => 1_000_000;
    public double MaximumAnnualExpenses => 500_000;

    partial void OnCurrentAgeChanged(double value) =>
        RoundSliderValue(value, rounded => CurrentAge = rounded, 1);

    partial void OnAnnualIncomeChanged(double value) =>
        RoundSliderValue(value, rounded => AnnualIncome = rounded, 1_000);

    partial void OnAnnualExpensesChanged(double value) =>
        RoundSliderValue(value, rounded => AnnualExpenses = rounded, 1_000);

    [RelayCommand]
    private async Task SaveAndContinueAsync()
    {
        var defaults = calculatorDefaultsService.Current;
        calculatorDefaultsService.Save(defaults with
        {
            CurrentAge = (int)CurrentAge,
            RetirementAge = Math.Max(defaults.RetirementAge, (int)CurrentAge),
            AnnualIncome = AnnualIncome,
            AnnualExpenses = AnnualExpenses
        });

        await navigationService.GoToAsync("../onboarding-withdrawal-rate");
    }

    [RelayCommand]
    private Task SkipAsync() => navigationService.GoToAsync("../onboarding-withdrawal-rate");

    private static void RoundSliderValue(double value, Action<double> update, double increment)
    {
        var rounded = Math.Round(value / increment) * increment;
        if (Math.Abs(value - rounded) > RoundingTolerance)
        {
            update(rounded);
        }
    }
}
