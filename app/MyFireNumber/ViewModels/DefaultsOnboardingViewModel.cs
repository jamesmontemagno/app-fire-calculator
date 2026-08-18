using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Profile;
using MyFireNumber.Services;
using MyFireNumber.Storage;
using System.Globalization;

namespace MyFireNumber.ViewModels;

public partial class DefaultsOnboardingViewModel : ObservableObject
{
    private const double RoundingTolerance = 0.000001;
    private readonly ICalculatorDefaultsService calculatorDefaultsService;
    private readonly ICurrencyPreferencesService currencyPreferencesService;
    private readonly ILocalDateProvider localDateProvider;
    private readonly INavigationService navigationService;
    private readonly IProfileExpenseRepository profileExpenseRepository;
    private readonly IProfileIncomeRepository profileIncomeRepository;
    private readonly IProfileService profileService;

    public DefaultsOnboardingViewModel(
        ICalculatorDefaultsService calculatorDefaultsService,
        ICurrencyPreferencesService currencyPreferencesService,
        ILocalDateProvider localDateProvider,
        INavigationService navigationService,
        IProfileExpenseRepository profileExpenseRepository,
        IProfileIncomeRepository profileIncomeRepository,
        IProfileService profileService)
    {
        this.calculatorDefaultsService = calculatorDefaultsService;
        this.currencyPreferencesService = currencyPreferencesService;
        this.localDateProvider = localDateProvider;
        this.navigationService = navigationService;
        this.profileExpenseRepository = profileExpenseRepository;
        this.profileIncomeRepository = profileIncomeRepository;
        this.profileService = profileService;

        var defaults = calculatorDefaultsService.Current;
        var profile = profileService.Current;
        annualIncome = defaults.AnnualIncome;
        annualExpenses = defaults.AnnualExpenses;
        displayName = profile.DisplayName ?? string.Empty;
        householdSize = profile.HouseholdSize ?? 1;

        // A saved birth date wins. Otherwise the picker opens on the saved default age so the wheel
        // starts somewhere plausible instead of today.
        hasBirthDate = profile.BirthDate is not null;
        birthDate = (profile.BirthDate ?? localDateProvider.Today.AddYears(-defaults.CurrentAge))
            .ToDateTime(TimeOnly.MinValue);
    }

    [ObservableProperty]
    private string displayName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HouseholdSizeText))]
    private double householdSize;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DerivedAgeText))]
    private DateTime birthDate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DerivedAgeText))]
    [NotifyPropertyChangedFor(nameof(HasNoBirthDate))]
    private bool hasBirthDate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AnnualIncomeText))]
    private double annualIncome;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AnnualExpensesText))]
    private double annualExpenses;

    public bool HasNoBirthDate => !HasBirthDate;
    public string HouseholdSizeText => HouseholdSize <= 1 ? "Just me" : $"{HouseholdSize:0} people";
    public string AnnualIncomeText => currencyPreferencesService.Format(AnnualIncome);
    public string AnnualExpensesText => currencyPreferencesService.Format(AnnualExpenses);
    public double MaximumAnnualIncome => 1_000_000;
    public double MaximumAnnualExpenses => 500_000;
    public double MaximumHouseholdSize => 10;

    /// <summary>Keeps the picker from offering a date the age math cannot evaluate.</summary>
    public DateTime MaximumBirthDate => localDateProvider.Today.ToDateTime(TimeOnly.MinValue);

    public string DerivedAgeText => HasBirthDate
        ? $"That makes you {DerivedAge.ToString(CultureInfo.CurrentCulture)} today, and your age stays current as time passes."
        : $"Skip this and calculators start from age {calculatorDefaultsService.Current.CurrentAge.ToString(CultureInfo.CurrentCulture)} instead.";

    private int DerivedAge
    {
        get
        {
            var selected = DateOnly.FromDateTime(BirthDate);
            return selected > localDateProvider.Today
                ? 0
                : ProfileAgeCalculator.AgeOn(selected, localDateProvider.Today);
        }
    }

    partial void OnHouseholdSizeChanged(double value) =>
        RoundSliderValue(value, rounded => HouseholdSize = rounded, 1);

    partial void OnAnnualIncomeChanged(double value) =>
        RoundSliderValue(value, rounded => AnnualIncome = rounded, 1_000);

    partial void OnAnnualExpensesChanged(double value) =>
        RoundSliderValue(value, rounded => AnnualExpenses = rounded, 1_000);

    [RelayCommand]
    private void UseBirthDate() => HasBirthDate = true;

    [RelayCommand]
    private void SkipBirthDate() => HasBirthDate = false;

    [RelayCommand]
    private async Task SaveAndContinueAsync()
    {
        var selectedBirthDate = HasBirthDate ? DateOnly.FromDateTime(BirthDate) : (DateOnly?)null;

        // A future date the picker should have prevented still must not reach the profile, because
        // age derivation cannot evaluate it.
        if (selectedBirthDate > localDateProvider.Today)
        {
            selectedBirthDate = localDateProvider.Today;
        }

        var defaults = calculatorDefaultsService.Current;
        var currentAge = selectedBirthDate is DateOnly birth
            ? ProfileAgeCalculator.AgeOn(birth, localDateProvider.Today)
            : defaults.CurrentAge;

        calculatorDefaultsService.Save(defaults with
        {
            CurrentAge = currentAge,
            RetirementAge = Math.Max(defaults.RetirementAge, currentAge),
            AnnualIncome = AnnualIncome,
            AnnualExpenses = AnnualExpenses
        });

        try
        {
            await profileService.SaveAsync(profileService.Current with
            {
                DisplayName = string.IsNullOrWhiteSpace(DisplayName) ? null : DisplayName.Trim(),
                HouseholdSize = (int)HouseholdSize,
                BirthDate = selectedBirthDate,
                AnnualIncome = AnnualIncome,
                AnnualExpenses = AnnualExpenses
            });

            await CreateMissingCashFlowItemsAsync(currentAge, defaults.RetirementAge);
            await profileService.NotifyExternalChangeAsync();
        }
        catch (Exception)
        {
            // Onboarding must never dead-end on a storage failure. The calculator defaults saved
            // above still apply, and Profile can be completed later.
        }

        await navigationService.GoToAsync("onboarding-timeline");
    }

    private async Task CreateMissingCashFlowItemsAsync(int currentAge, double retirementAge)
    {
        var startAge = Math.Clamp(currentAge, 18, 100);
        var endAge = Math.Clamp(
            Math.Max(startAge, (int)Math.Round(retirementAge)),
            startAge,
            100);

        if (AnnualIncome > 0 && (await profileIncomeRepository.ListAsync()).Count == 0)
        {
            await profileIncomeRepository.SaveAsync(new RetirementIncomeSource(
                Guid.NewGuid().ToString("N"),
                "Current after-tax income",
                AnnualIncome,
                startAge,
                endAge,
                0,
                true,
                0));
        }

        if (AnnualExpenses > 0 && (await profileExpenseRepository.ListAsync()).Count == 0)
        {
            await profileExpenseRepository.SaveAsync(new RetirementExpense(
                Guid.NewGuid().ToString("N"),
                "Current expenses",
                AnnualExpenses,
                startAge));
        }
    }

    [RelayCommand]
    private Task SkipAsync() => navigationService.GoToAsync("onboarding-timeline");

    private static void RoundSliderValue(double value, Action<double> update, double increment)
    {
        var rounded = Math.Round(value / increment) * increment;
        if (Math.Abs(value - rounded) > RoundingTolerance)
        {
            update(rounded);
        }
    }
}
