using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyFireNumber.Core.Profile;
using MyFireNumber.Services;
using System.Globalization;

namespace MyFireNumber.ViewModels;

/// <summary>
/// Personal identity, household, timeline, and planning-assumption inputs. Reusable financial
/// inventory (accounts, income, expenses, debts) moved to <see cref="AccountsViewModel"/> and the
/// Accounts tab, which every linked calculator still reads from unchanged.
/// </summary>
public sealed partial class ProfileViewModel(
    IProfileService profileService,
    ICalculatorDefaultsService calculatorDefaultsService,
    ILocalDateProvider localDateProvider,
    INavigationService navigationService) : ObservableObject
{
    private bool isLoaded;
    private long loadedDataRevision = -1;

    [ObservableProperty]
    public partial string DisplayName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string HouseholdName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string HouseholdSizeText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DateTime BirthDate { get; set; } = DateTime.Today.AddYears(-30);

    [ObservableProperty]
    public partial DateTime PhasedRetirementDate { get; set; } = DateTime.Today.AddYears(25);

    [ObservableProperty]
    public partial DateTime TargetRetirementDate { get; set; } = DateTime.Today.AddYears(30);

    [ObservableProperty]
    public partial bool HasBirthDate { get; set; }

    [ObservableProperty]
    public partial bool HasPhasedRetirementDate { get; set; }

    [ObservableProperty]
    public partial bool HasTargetRetirementDate { get; set; }

    [ObservableProperty]
    public partial string ValidationMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    // Planning assumptions. These live with the profile rather than in Settings because they are
    // personal planning inputs, not app preferences, and every new calculator starts from them.
    [ObservableProperty]
    public partial string ExpectedReturnText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string InflationRateText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string WithdrawalRateText { get; set; } = string.Empty;

    /// <summary>The page heading, personalized once the profile has a name.</summary>
    [ObservableProperty]
    public partial string HeaderTitle { get; set; } = "Profile";

    public bool HasValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);
    public bool HasNoBirthDate => !HasBirthDate;
    public bool HasNoPhasedRetirementDate => !HasPhasedRetirementDate;
    public bool HasNoTargetRetirementDate => !HasTargetRetirementDate;

    /// <summary>Keeps the birth-date picker from offering a future date the age math cannot use.</summary>
    public DateTime MaximumBirthDate => localDateProvider.Today.ToDateTime(TimeOnly.MinValue);
    public bool IsProfileComplete => HasBirthDate && HasTargetRetirementDate;
    public string CompletionText => IsProfileComplete
        ? "Your personal details are set. Add accounts, income, and expenses on the Accounts tab to personalize new calculations."
        : "Add your birth date and target retirement date. Then visit the Accounts tab to add income and expenses.";

    partial void OnValidationMessageChanged(string value) => OnPropertyChanged(nameof(HasValidationMessage));
    partial void OnHasBirthDateChanged(bool value)
    {
        OnPropertyChanged(nameof(HasNoBirthDate));
        NotifyCompletionChanged();
    }

    partial void OnHasPhasedRetirementDateChanged(bool value) => OnPropertyChanged(nameof(HasNoPhasedRetirementDate));

    partial void OnHasTargetRetirementDateChanged(bool value)
    {
        OnPropertyChanged(nameof(HasNoTargetRetirementDate));
        NotifyCompletionChanged();
    }

    /// <summary>
    /// <see cref="CompletionText"/> is derived from <see cref="IsProfileComplete"/>, so both have to
    /// be raised together or the completion card keeps stale guidance until the next save.
    /// </summary>
    private void NotifyCompletionChanged()
    {
        OnPropertyChanged(nameof(IsProfileComplete));
        OnPropertyChanged(nameof(CompletionText));
    }

    /// <summary>
    /// Drops the loaded state so the next appearance re-reads storage. Reset and import replace the
    /// profile tables underneath this singleton view model, and without this the editor would keep
    /// -- and re-save -- data the user just deleted.
    /// </summary>
    public void Invalidate() => isLoaded = false;

    public async Task LoadAsync()
    {
        if (isLoaded && loadedDataRevision == profileService.DataRevision)
        {
            return;
        }

        await profileService.LoadAsync();
        ApplyProfile(profileService.Current);
        ApplyAssumptions();

        ValidationMessage = string.Empty;
        StatusMessage = string.Empty;
        loadedDataRevision = profileService.DataRevision;
        isLoaded = true;
    }

    private void ApplyAssumptions()
    {
        var defaults = calculatorDefaultsService.Current;
        ExpectedReturnText = (defaults.ExpectedReturn * 100).ToString("0.##", CultureInfo.CurrentCulture);
        InflationRateText = (defaults.InflationRate * 100).ToString("0.##", CultureInfo.CurrentCulture);
        WithdrawalRateText = (defaults.WithdrawalRate * 100).ToString("0.##", CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Validates the planning assumptions without writing anything, so an invalid entry is caught
    /// before any part of the profile is persisted.
    /// </summary>
    private bool TryReadAssumptions(out (double ExpectedReturn, double InflationRate, double WithdrawalRate) assumptions)
    {
        assumptions = default;
        if (!TryPercent(ExpectedReturnText, 0, 15, out var expectedReturn) ||
            !TryPercent(InflationRateText, 0, 10, out var inflationRate) ||
            !TryPercent(WithdrawalRateText, 2, 6, out var withdrawalRate))
        {
            ValidationMessage = "Enter an expected return of 0% to 15%, inflation of 0% to 10%, and a withdrawal rate of 2% to 6%.";
            return false;
        }

        assumptions = (expectedReturn, inflationRate, withdrawalRate);
        return true;
    }

    /// <summary>
    /// Persists the assumptions. Runs after the profile is saved and re-reads
    /// <see cref="ICalculatorDefaultsService.Current"/>, which resolves age, income, and spending
    /// from the profile, so the stored fallbacks mirror the values just saved rather than the
    /// previous ones.
    /// </summary>
    private void SaveAssumptions((double ExpectedReturn, double InflationRate, double WithdrawalRate) assumptions)
    {
        calculatorDefaultsService.Save(calculatorDefaultsService.Current with
        {
            ExpectedReturn = assumptions.ExpectedReturn,
            InflationRate = assumptions.InflationRate,
            WithdrawalRate = assumptions.WithdrawalRate
        });
    }

    private static bool TryPercent(string text, double minimum, double maximum, out double value)
    {
        value = 0;
        if (!double.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var percent) ||
            percent < minimum ||
            percent > maximum)
        {
            return false;
        }

        value = percent / 100;
        return true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!TryCreateProfile(out var profile) || !TryReadAssumptions(out var assumptions))
        {
            return;
        }

        await profileService.SaveAsync(profile);

        // Written last so the stored fallbacks mirror the profile that was just saved.
        SaveAssumptions(assumptions);

        ValidationMessage = string.Empty;
        StatusMessage = "Profile saved on this device.";
        HeaderTitle = FirstNonEmpty(profile.HouseholdName, profile.DisplayName) ?? "Profile";
        NotifyCompletionChanged();
    }

    [RelayCommand]
    private Task OpenSettingsAsync() => navigationService.GoToAsync("settings");

    [RelayCommand]
    private void SetBirthDate() => HasBirthDate = true;

    [RelayCommand]
    private void ClearBirthDate() => HasBirthDate = false;

    [RelayCommand]
    private void SetPhasedRetirementDate() => HasPhasedRetirementDate = true;

    [RelayCommand]
    private void ClearPhasedRetirementDate() => HasPhasedRetirementDate = false;

    [RelayCommand]
    private void SetTargetRetirementDate() => HasTargetRetirementDate = true;

    [RelayCommand]
    private void ClearTargetRetirementDate() => HasTargetRetirementDate = false;

    private bool TryCreateProfile(out FinancialProfile profile)
    {
        profile = FinancialProfile.Empty;
        if (!TryOptionalPositiveInt(HouseholdSizeText, out var householdSize))
        {
            ValidationMessage = "Household size must be a whole number.";
            return false;
        }

        profile = new FinancialProfile(
            DisplayName,
            HouseholdName,
            householdSize,
            HasBirthDate ? DateOnly.FromDateTime(BirthDate) : null,
            HasPhasedRetirementDate ? DateOnly.FromDateTime(PhasedRetirementDate) : null,
            HasTargetRetirementDate ? DateOnly.FromDateTime(TargetRetirementDate) : null,
            null,
            null);

        if (!ProfileAgeCalculator.TryValidate(profile, localDateProvider.Today, out var validationError))
        {
            ValidationMessage = validationError;
            return false;
        }

        return true;
    }

    private void ApplyProfile(FinancialProfile profile)
    {
        DisplayName = profile.DisplayName ?? string.Empty;
        HouseholdName = profile.HouseholdName ?? string.Empty;
        HouseholdSizeText = profile.HouseholdSize?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
        HasBirthDate = profile.BirthDate is not null;
        HasPhasedRetirementDate = profile.PhasedRetirementDate is not null;
        HasTargetRetirementDate = profile.TargetRetirementDate is not null;
        if (profile.BirthDate is DateOnly birth) BirthDate = birth.ToDateTime(TimeOnly.MinValue);
        if (profile.PhasedRetirementDate is DateOnly phased) PhasedRetirementDate = phased.ToDateTime(TimeOnly.MinValue);
        if (profile.TargetRetirementDate is DateOnly target) TargetRetirementDate = target.ToDateTime(TimeOnly.MinValue);

        // Prefer the household label, then the person's name, so a shared plan reads correctly.
        HeaderTitle = FirstNonEmpty(profile.HouseholdName, profile.DisplayName) ?? "Profile";
        NotifyCompletionChanged();
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();

    private static bool TryOptionalPositiveInt(string value, out int? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out var parsed) || parsed < 1)
        {
            return false;
        }

        result = parsed;
        return true;
    }
}
