using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Presentation;
using MyFireNumber.Core.Profile;
using MyFireNumber.Services;
using MyFireNumber.Storage;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;

namespace MyFireNumber.ViewModels;

public sealed partial class ProfileViewModel(
    IProfileService profileService,
    IProfileAccountRepository profileAccountRepository,
    IProfileIncomeRepository profileIncomeRepository,
    IProfileExpenseRepository profileExpenseRepository,
    IProfileDebtRepository profileDebtRepository,
    ICalculatorDefaultsService calculatorDefaultsService,
    ILocalDateProvider localDateProvider,
    INavigationService navigationService) : ObservableObject
{
    private bool isLoaded;
    private bool isTrackingCollections;
    private long loadedDataRevision = -1;

    public ObservableCollection<RetirementAccountEditorItem> Accounts { get; } = [];
    public ObservableCollection<ProfileRecurringEditorItem> Income { get; } = [];
    public ObservableCollection<ProfileRecurringEditorItem> Expenses { get; } = [];
    public ObservableCollection<ProfileDebtEditorItem> Debts { get; } = [];
    [ObservableProperty] private string displayName = string.Empty;
    [ObservableProperty] private string householdName = string.Empty;
    [ObservableProperty] private string householdSizeText = string.Empty;
    [ObservableProperty] private string annualIncomeText = string.Empty;
    [ObservableProperty] private string annualExpensesText = string.Empty;
    [ObservableProperty] private DateTime birthDate = DateTime.Today.AddYears(-30);
    [ObservableProperty] private DateTime phasedRetirementDate = DateTime.Today.AddYears(25);
    [ObservableProperty] private DateTime targetRetirementDate = DateTime.Today.AddYears(30);
    [ObservableProperty] private bool hasBirthDate;
    [ObservableProperty] private bool hasPhasedRetirementDate;
    [ObservableProperty] private bool hasTargetRetirementDate;
    [ObservableProperty] private string validationMessage = string.Empty;
    [ObservableProperty] private string statusMessage = string.Empty;

    // Planning assumptions. These live with the profile rather than in Settings because they are
    // personal planning inputs, not app preferences, and every new calculator starts from them.
    [ObservableProperty] private string expectedReturnText = string.Empty;
    [ObservableProperty] private string inflationRateText = string.Empty;
    [ObservableProperty] private string withdrawalRateText = string.Empty;

    /// <summary>The page heading, personalized once the profile has a name.</summary>
    [ObservableProperty] private string headerTitle = "Profile";

    public bool HasValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);
    public bool HasNoBirthDate => !HasBirthDate;
    public bool HasNoPhasedRetirementDate => !HasPhasedRetirementDate;
    public bool HasNoTargetRetirementDate => !HasTargetRetirementDate;

    /// <summary>
    /// Explains which figure calculators will actually use, because the itemised list silently wins
    /// over the single household figure and a user editing both deserves to see that.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasIncomeOverride))]
    private string incomeOverrideText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasExpenseOverride))]
    private string expenseOverrideText = string.Empty;

    public bool HasIncomeOverride => !string.IsNullOrWhiteSpace(IncomeOverrideText);
    public bool HasExpenseOverride => !string.IsNullOrWhiteSpace(ExpenseOverrideText);

    /// <summary>Keeps the birth-date picker from offering a future date the age math cannot use.</summary>
    public DateTime MaximumBirthDate => localDateProvider.Today.ToDateTime(TimeOnly.MinValue);
    public bool IsProfileComplete => HasBirthDate && HasTargetRetirementDate &&
        !string.IsNullOrWhiteSpace(AnnualIncomeText) && !string.IsNullOrWhiteSpace(AnnualExpensesText);
    public string CompletionText => IsProfileComplete
        ? "Your essential planning details are set."
        : "Add your birth date, target retirement date, income, and spending to personalize new calculations.";

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
    partial void OnAnnualIncomeTextChanged(string value) => NotifyCompletionChanged();
    partial void OnAnnualExpensesTextChanged(string value) => NotifyCompletionChanged();

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
    /// profile tables underneath this singleton view model, and without this the editor collections
    /// would keep -- and re-save -- data the user just deleted.
    /// </summary>
    public void Invalidate() => isLoaded = false;

    /// <summary>
    /// Subscribes once so the override notices follow the lists, including edits to an existing
    /// item's amount or period, not just additions and removals.
    /// </summary>
    private void TrackRecurringCollections()
    {
        if (isTrackingCollections)
        {
            return;
        }

        isTrackingCollections = true;
        Income.CollectionChanged += OnRecurringCollectionChanged;
        Expenses.CollectionChanged += OnRecurringCollectionChanged;
    }

    private void OnRecurringCollectionChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        foreach (var item in eventArgs.OldItems?.OfType<ProfileRecurringEditorItem>() ?? [])
        {
            item.PropertyChanged -= OnRecurringItemChanged;
        }

        foreach (var item in eventArgs.NewItems?.OfType<ProfileRecurringEditorItem>() ?? [])
        {
            item.PropertyChanged += OnRecurringItemChanged;
        }

        UpdateOverrideNotices();
    }

    private void OnRecurringItemChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName is nameof(ProfileRecurringEditorItem.AmountText)
            or nameof(ProfileRecurringEditorItem.Period))
        {
            UpdateOverrideNotices();
        }
    }

    /// <summary>
    /// Recomputes the notices that tell the user the itemised lists are overriding the single
    /// household figures. Kept live so adding a first income item immediately explains why the
    /// household number above it stopped mattering.
    /// </summary>
    private void UpdateOverrideNotices()
    {
        IncomeOverrideText = BuildOverrideText(Income, "income");
        ExpenseOverrideText = BuildOverrideText(Expenses, "spending");
    }

    private static string BuildOverrideText(
        IReadOnlyCollection<ProfileRecurringEditorItem> items,
        string noun)
    {
        if (items.Count == 0)
        {
            return string.Empty;
        }

        var total = items.Sum(item => item.TryGetAmount(out var amount)
            ? CurrencyPeriodMath.Convert(amount, item.Period, CurrencyPeriod.Annual)
            : 0);
        var label = items.Count == 1 ? "entry" : "entries";
        return $"Your {items.Count} {noun} {label} total {total.ToString("C0", CultureInfo.CurrentCulture)} a year, and calculators use that instead of the figure above.";
    }

    public async Task LoadAsync()
    {
        if (isLoaded && loadedDataRevision == profileService.DataRevision)
        {
            return;
        }

        TrackRecurringCollections();

        await profileService.LoadAsync();
        ApplyProfile(profileService.Current);
        ApplyAssumptions();

        Accounts.Clear();
        Income.Clear();
        Expenses.Clear();
        Debts.Clear();

        foreach (var account in await profileAccountRepository.ListAsync())
        {
            Accounts.Add(ToEditor(account));
        }

        foreach (var item in await profileIncomeRepository.ListAsync())
        {
            Income.Add(ProfileRecurringEditorItem.FromIncome(item));
        }

        foreach (var item in await profileExpenseRepository.ListAsync())
        {
            Expenses.Add(ProfileRecurringEditorItem.FromExpense(item));
        }

        foreach (var item in await profileDebtRepository.ListAsync())
        {
            Debts.Add(ProfileDebtEditorItem.FromDebt(item));
        }

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

        // Each inventory category is validated and persisted exactly once, independently of the
        // others. Nesting these loops inside the account loop saved every item once per account and
        // skipped them entirely for a profile with no accounts.
        var accounts = new List<ProfileAccount>(Accounts.Count);
        foreach (var editor in Accounts)
        {
            if (!editor.TryCreateAccount(out var account, out var error))
            {
                editor.IsExpanded = true;
                ValidationMessage = $"Account {editor.Name}: {error}";
                return;
            }

            accounts.Add(new ProfileAccount(
                account.Id,
                account.Name,
                account.Type,
                account.Balance,
                account.AnnualContribution,
                account.AnnualReturn,
                account.AvailableAge,
                account.WithdrawalRate,
                account.PayoutYears,
                account.EffectiveWithdrawalTaxRate));
        }

        var income = new List<ProfileIncome>(Income.Count);
        foreach (var item in Income)
        {
            if (string.IsNullOrWhiteSpace(item.Name) || !item.TryGetAmount(out var amount))
            {
                ValidationMessage = "Each income item needs a name and non-negative amount.";
                return;
            }

            income.Add(new ProfileIncome(item.Id, item.Name, amount, item.Period, item.Category));
        }

        var expenses = new List<ProfileExpense>(Expenses.Count);
        foreach (var item in Expenses)
        {
            if (string.IsNullOrWhiteSpace(item.Name) || !item.TryGetAmount(out var amount))
            {
                ValidationMessage = "Each expense needs a name and non-negative amount.";
                return;
            }

            expenses.Add(new ProfileExpense(item.Id, item.Name, amount, item.Period, item.Category));
        }

        var debts = new List<ProfileDebt>(Debts.Count);
        foreach (var item in Debts)
        {
            if (!item.TryCreate(out var debt, out var debtError))
            {
                ValidationMessage = $"Debt {item.Name}: {debtError}";
                return;
            }

            debts.Add(debt);
        }

        // Everything is validated before anything is written, so a late failure cannot leave the
        // profile half-saved.
        foreach (var account in accounts)
        {
            await profileAccountRepository.SaveAsync(account);
        }

        foreach (var item in income)
        {
            await profileIncomeRepository.SaveAsync(item);
        }

        foreach (var item in expenses)
        {
            await profileExpenseRepository.SaveAsync(item);
        }

        foreach (var debt in debts)
        {
            await profileDebtRepository.SaveAsync(debt);
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
    private void AddAccount()
    {
        Accounts.Add(new RetirementAccountEditorItem
        {
            Name = "New account",
            IsExpanded = true
        });
    }

    [RelayCommand] private void AddIncome() => Income.Add(new ProfileRecurringEditorItem { Name = "New income" });
    [RelayCommand] private void AddExpense() => Expenses.Add(new ProfileRecurringEditorItem { Name = "New expense" });
    [RelayCommand] private void AddDebt() => Debts.Add(new ProfileDebtEditorItem { Name = "New debt" });

    [RelayCommand]
    private async Task RemoveIncomeAsync(ProfileRecurringEditorItem? item)
    {
        if (item is null) return;
        Income.Remove(item);
        await profileIncomeRepository.DeleteAsync(item.Id);
    }

    [RelayCommand]
    private async Task RemoveExpenseAsync(ProfileRecurringEditorItem? item)
    {
        if (item is null) return;
        Expenses.Remove(item);
        await profileExpenseRepository.DeleteAsync(item.Id);
    }

    [RelayCommand]
    private async Task RemoveDebtAsync(ProfileDebtEditorItem? item)
    {
        if (item is null) return;
        Debts.Remove(item);
        await profileDebtRepository.DeleteAsync(item.Id);
    }

    [RelayCommand]
    private async Task RemoveAccountAsync(RetirementAccountEditorItem? account)
    {
        if (account is null)
        {
            return;
        }

        Accounts.Remove(account);
        await profileAccountRepository.DeleteAsync(account.Id);
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
        if (!TryOptionalPositiveInt(HouseholdSizeText, out var householdSize) ||
            !TryOptionalNonNegative(AnnualIncomeText, out var annualIncome) ||
            !TryOptionalNonNegative(AnnualExpensesText, out var annualExpenses))
        {
            ValidationMessage = "Household size must be a whole number, and income and spending must be zero or more.";
            return false;
        }

        profile = new FinancialProfile(
            DisplayName,
            HouseholdName,
            householdSize,
            HasBirthDate ? DateOnly.FromDateTime(BirthDate) : null,
            HasPhasedRetirementDate ? DateOnly.FromDateTime(PhasedRetirementDate) : null,
            HasTargetRetirementDate ? DateOnly.FromDateTime(TargetRetirementDate) : null,
            annualIncome,
            annualExpenses);

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
        AnnualIncomeText = profile.AnnualIncome?.ToString("0.##", CultureInfo.CurrentCulture) ?? string.Empty;
        AnnualExpensesText = profile.AnnualExpenses?.ToString("0.##", CultureInfo.CurrentCulture) ?? string.Empty;
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

    private static RetirementAccountEditorItem ToEditor(ProfileAccount account) =>
        RetirementAccountEditorItem.FromAccount(new RetirementAccount(
            account.Id,
            account.Name,
            account.Type,
            account.Balance,
            account.AnnualContribution,
            account.AnnualReturn,
            account.AvailableAge,
            account.WithdrawalRate,
            account.PayoutYears,
            account.EffectiveWithdrawalTaxRate));

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

    private static bool TryOptionalNonNegative(string value, out double? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!double.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed) || parsed < 0)
        {
            return false;
        }

        result = parsed;
        return true;
    }
}
