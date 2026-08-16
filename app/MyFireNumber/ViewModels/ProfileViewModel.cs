using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Profile;
using MyFireNumber.Services;
using MyFireNumber.Storage;
using System.Collections.ObjectModel;
using System.Globalization;

namespace MyFireNumber.ViewModels;

public sealed partial class ProfileViewModel(
    IProfileService profileService,
    IProfileAccountRepository profileAccountRepository,
    IProfileIncomeRepository profileIncomeRepository,
    IProfileExpenseRepository profileExpenseRepository,
    IProfileDebtRepository profileDebtRepository,
    INavigationService navigationService) : ObservableObject
{
    private bool isLoaded;

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

    public bool HasValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);
    public bool HasNoBirthDate => !HasBirthDate;
    public bool HasNoPhasedRetirementDate => !HasPhasedRetirementDate;
    public bool HasNoTargetRetirementDate => !HasTargetRetirementDate;
    public bool IsProfileComplete => HasBirthDate && HasTargetRetirementDate &&
        !string.IsNullOrWhiteSpace(AnnualIncomeText) && !string.IsNullOrWhiteSpace(AnnualExpensesText);
    public string CompletionText => IsProfileComplete
        ? "Your essential planning details are set."
        : "Add your birth date, target retirement date, income, and spending to personalize new calculations.";

    partial void OnValidationMessageChanged(string value) => OnPropertyChanged(nameof(HasValidationMessage));
    partial void OnHasBirthDateChanged(bool value)
    {
        OnPropertyChanged(nameof(HasNoBirthDate));
        OnPropertyChanged(nameof(IsProfileComplete));
    }

    partial void OnHasPhasedRetirementDateChanged(bool value) => OnPropertyChanged(nameof(HasNoPhasedRetirementDate));

    partial void OnHasTargetRetirementDateChanged(bool value)
    {
        OnPropertyChanged(nameof(HasNoTargetRetirementDate));
        OnPropertyChanged(nameof(IsProfileComplete));
    }
    partial void OnAnnualIncomeTextChanged(string value) => OnPropertyChanged(nameof(IsProfileComplete));
    partial void OnAnnualExpensesTextChanged(string value) => OnPropertyChanged(nameof(IsProfileComplete));

    public async Task LoadAsync()
    {
        if (isLoaded)
        {
            return;
        }

        await profileService.LoadAsync();
        ApplyProfile(profileService.Current);
        var accounts = await profileAccountRepository.ListAsync();
        Accounts.Clear();
        foreach (var account in accounts)
        {
            Accounts.Add(ToEditor(account));
        }
        foreach (var item in await profileIncomeRepository.ListAsync()) Income.Add(ProfileRecurringEditorItem.FromIncome(item));
        foreach (var item in await profileExpenseRepository.ListAsync()) Expenses.Add(ProfileRecurringEditorItem.FromExpense(item));
        foreach (var item in await profileDebtRepository.ListAsync()) Debts.Add(ProfileDebtEditorItem.FromDebt(item));

        isLoaded = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!TryCreateProfile(out var profile))
        {
            return;
        }

        foreach (var editor in Accounts)
        {
            if (!editor.TryCreateAccount(out var account, out var error))
            {
                editor.IsExpanded = true;
                ValidationMessage = $"Account {editor.Name}: {error}";
                return;
            }

            foreach (var item in Income)
            {
                if (string.IsNullOrWhiteSpace(item.Name) || !item.TryGetAmount(out var amount))
                {
                    ValidationMessage = "Each income item needs a name and non-negative amount.";
                    return;
                }
                await profileIncomeRepository.SaveAsync(new ProfileIncome(item.Id, item.Name, amount, item.Period, item.Category));
            }
            foreach (var item in Expenses)
            {
                if (string.IsNullOrWhiteSpace(item.Name) || !item.TryGetAmount(out var amount))
                {
                    ValidationMessage = "Each expense needs a name and non-negative amount.";
                    return;
                }
                await profileExpenseRepository.SaveAsync(new ProfileExpense(item.Id, item.Name, amount, item.Period, item.Category));
            }
            foreach (var item in Debts)
            {
                if (!item.TryCreate(out var debt))
                {
                    ValidationMessage = "Each debt needs a name and valid non-negative values.";
                    return;
                }
                await profileDebtRepository.SaveAsync(debt);
            }

            await profileAccountRepository.SaveAsync(new ProfileAccount(
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

        await profileService.SaveAsync(profile);
        ValidationMessage = string.Empty;
        StatusMessage = "Profile saved on this device.";
        OnPropertyChanged(nameof(IsProfileComplete));
        OnPropertyChanged(nameof(CompletionText));
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

        if (!ProfileAgeCalculator.TryValidate(profile, out var validationError))
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
        OnPropertyChanged(nameof(CompletionText));
    }

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
