using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyFireNumber.Core.Profile;
using MyFireNumber.Services;

namespace MyFireNumber.ViewModels;

public partial class TimelineOnboardingViewModel : ObservableObject
{
    private const double RoundingTolerance = 0.000001;
    private const int MinimumRetirementAge = 30;
    private const int HighestRetirementAge = 90;
    private readonly ICalculatorDefaultsService calculatorDefaultsService;
    private readonly ILocalDateProvider localDateProvider;
    private readonly INavigationService navigationService;
    private readonly IProfileService profileService;

    public TimelineOnboardingViewModel(
        ICalculatorDefaultsService calculatorDefaultsService,
        ILocalDateProvider localDateProvider,
        INavigationService navigationService,
        IProfileService profileService)
    {
        this.calculatorDefaultsService = calculatorDefaultsService;
        this.localDateProvider = localDateProvider;
        this.navigationService = navigationService;
        this.profileService = profileService;

        var defaults = calculatorDefaultsService.Current;
        var profile = profileService.Current;
        currentAge = profileService.DerivedCurrentAge ?? defaults.CurrentAge;
        ageRange = ProfileAgeCalculator.RetirementAgeRange(currentAge, MinimumRetirementAge, HighestRetirementAge);
        retirementAge = Math.Clamp(
            (double)(profileService.DerivedTargetRetirementAge ?? defaults.RetirementAge),
            ageRange.Minimum,
            ageRange.Maximum);
        hasPhasedRetirement = profile.PhasedRetirementDate is not null;
        phasedRetirementAge = Math.Clamp(
            (double)(profileService.DerivedPhasedRetirementAge ?? (int)retirementAge),
            ageRange.Minimum,
            retirementAge);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RetirementAgeText))]
    [NotifyPropertyChangedFor(nameof(TimelineSummary))]
    private double retirementAge;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PhasedRetirementAgeText))]
    [NotifyPropertyChangedFor(nameof(TimelineSummary))]
    private double phasedRetirementAge;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TimelineSummary))]
    [NotifyPropertyChangedFor(nameof(HasNoPhasedRetirement))]
    private bool hasPhasedRetirement;

    private readonly int currentAge;
    private readonly (int Minimum, int Maximum) ageRange;

    public bool HasNoPhasedRetirement => !HasPhasedRetirement;

    /// <summary>
    /// Never exceeds <see cref="MaximumRetirementAge"/>. Someone already older than the slider's top
    /// value would otherwise produce a minimum above the maximum, which the slider cannot represent.
    /// </summary>
    public double MinimumAllowedRetirementAge => ageRange.Minimum;

    public double MaximumRetirementAge => ageRange.Maximum;
    public string RetirementAgeText => $"{RetirementAge:0}";
    public string PhasedRetirementAgeText => $"{PhasedRetirementAge:0}";
    public bool HasBirthDate => profileService.Current.BirthDate is not null;

    public string TimelineSummary
    {
        get
        {
            if (!HasBirthDate)
            {
                return "Add a birth date in Profile to turn these ages into dates. Until then they are saved as calculator starting points.";
            }

            var yearsAway = Math.Max(0, (int)RetirementAge - currentAge);
            var target = $"You plan to retire fully at {RetirementAge:0}, about {yearsAway} years from now.";
            return HasPhasedRetirement
                ? $"{target} You expect to step back to part-time work at {PhasedRetirementAge:0}."
                : target;
        }
    }

    partial void OnRetirementAgeChanged(double value)
    {
        RoundSliderValue(value, rounded => RetirementAge = rounded, 1);

        // Full retirement can never come before the phased step, so pull the phased age down with it
        // rather than saving a pair the profile would reject.
        if (PhasedRetirementAge > RetirementAge)
        {
            PhasedRetirementAge = RetirementAge;
        }
    }

    partial void OnPhasedRetirementAgeChanged(double value)
    {
        RoundSliderValue(value, rounded => PhasedRetirementAge = rounded, 1);
        if (PhasedRetirementAge > RetirementAge)
        {
            PhasedRetirementAge = RetirementAge;
        }
    }

    [RelayCommand]
    private void AddPhasedRetirement() => HasPhasedRetirement = true;

    [RelayCommand]
    private void RemovePhasedRetirement() => HasPhasedRetirement = false;

    [RelayCommand]
    private async Task SaveAndContinueAsync()
    {
        calculatorDefaultsService.Save(calculatorDefaultsService.Current with
        {
            RetirementAge = (int)RetirementAge
        });

        // Ages only become profile dates once a birth date anchors them.
        if (profileService.Current.BirthDate is DateOnly birthDate)
        {
            try
            {
                await profileService.SaveAsync(profileService.Current with
                {
                    TargetRetirementDate = ProfileAgeCalculator.DateAtAge(birthDate, (int)RetirementAge),
                    PhasedRetirementDate = HasPhasedRetirement
                        ? ProfileAgeCalculator.DateAtAge(birthDate, (int)PhasedRetirementAge)
                        : null
                });
            }
            catch (Exception)
            {
                // Onboarding must never dead-end. The calculator defaults above already carry the
                // chosen retirement age, and Profile can be completed later.
            }
        }

        await navigationService.GoToAsync("onboarding-withdrawal-rate");
    }

    [RelayCommand]
    private Task SkipAsync() => navigationService.GoToAsync("onboarding-withdrawal-rate");

    private static void RoundSliderValue(double value, Action<double> update, double increment)
    {
        var rounded = Math.Round(value / increment) * increment;
        if (Math.Abs(value - rounded) > RoundingTolerance)
        {
            update(rounded);
        }
    }
}
