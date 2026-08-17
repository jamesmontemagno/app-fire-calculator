using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Profile;
using MyFireNumber.Services;
using MyFireNumber.Storage;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
    INavigationService navigationService,
    IConfirmationService confirmationService,
    IRetirementCashFlowPromptService promptService) : ObservableObject
{
    private bool isLoaded;
    private bool isTrackingCollections;
    private long loadedDataRevision = -1;

    public ObservableCollection<RetirementAccountEditorItem> Accounts { get; } = [];
    public ObservableCollection<RetirementIncomeEditorItem> Income { get; } = [];
    public ObservableCollection<RetirementExpenseEditorItem> Expenses { get; } = [];
    public ObservableCollection<DebtEditorItem> Debts { get; } = [];
    [ObservableProperty] private string displayName = string.Empty;
    [ObservableProperty] private string householdName = string.Empty;
    [ObservableProperty] private string householdSizeText = string.Empty;
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

    [ObservableProperty] private string accountsSummary = "No accounts yet.";
    [ObservableProperty] private string incomeSummary = "No income yet.";
    [ObservableProperty] private string expensesSummary = "No expenses yet.";
    [ObservableProperty] private string debtsSummary = "No debts yet.";

    /// <summary>Keeps the birth-date picker from offering a future date the age math cannot use.</summary>
    public DateTime MaximumBirthDate => localDateProvider.Today.ToDateTime(TimeOnly.MinValue);
    public bool IsProfileComplete => HasBirthDate && HasTargetRetirementDate &&
        Income.Count > 0 && Expenses.Count > 0;
    public string CompletionText => IsProfileComplete
        ? "Your essential planning details are set."
        : "Add your birth date, target retirement date, income, and expenses to personalize new calculations.";

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
    /// profile tables underneath this singleton view model, and without this the editor collections
    /// would keep -- and re-save -- data the user just deleted.
    /// </summary>
    public void Invalidate() => isLoaded = false;

    /// <summary>
    /// Subscribes once so the override notices follow the lists, including edits to an existing
    /// item's amount or period, not just additions and removals.
    /// </summary>
    private void TrackInventoryCollections()
    {
        if (isTrackingCollections)
        {
            return;
        }

        isTrackingCollections = true;
        Accounts.CollectionChanged += OnAccountsCollectionChanged;
        Income.CollectionChanged += OnIncomeCollectionChanged;
        Expenses.CollectionChanged += OnExpenseCollectionChanged;
        Debts.CollectionChanged += OnDebtsCollectionChanged;
    }

    private void OnAccountsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        UpdateItemHandlers<RetirementAccountEditorItem>(eventArgs, OnInventoryItemChanged);
        UpdateInventorySummaries();
    }

    private void OnIncomeCollectionChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        UpdateItemHandlers<RetirementIncomeEditorItem>(eventArgs, OnInventoryItemChanged);
        UpdateInventorySummaries();
        NotifyCompletionChanged();
    }

    private void OnExpenseCollectionChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        UpdateItemHandlers<RetirementExpenseEditorItem>(eventArgs, OnInventoryItemChanged);
        UpdateInventorySummaries();
        NotifyCompletionChanged();
    }

    private void OnDebtsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        UpdateItemHandlers<DebtEditorItem>(eventArgs, OnInventoryItemChanged);
        UpdateInventorySummaries();
    }

    private static void UpdateItemHandlers<TItem>(
        NotifyCollectionChangedEventArgs eventArgs,
        EventHandler handler)
        where TItem : class
    {
        foreach (var item in eventArgs.OldItems?.OfType<TItem>() ?? [])
        {
            switch (item)
            {
                case RetirementAccountEditorItem account: account.Changed -= handler; break;
                case RetirementIncomeEditorItem income: income.Changed -= handler; break;
                case RetirementExpenseEditorItem expense: expense.Changed -= handler; break;
                case DebtEditorItem debt: debt.Changed -= handler; break;
            }
        }

        foreach (var item in eventArgs.NewItems?.OfType<TItem>() ?? [])
        {
            switch (item)
            {
                case RetirementAccountEditorItem account: account.Changed += handler; break;
                case RetirementIncomeEditorItem income: income.Changed += handler; break;
                case RetirementExpenseEditorItem expense: expense.Changed += handler; break;
                case DebtEditorItem debt: debt.Changed += handler; break;
            }
        }
    }

    private void OnInventoryItemChanged(object? sender, EventArgs eventArgs) => UpdateInventorySummaries();

    private void UpdateInventorySummaries()
    {
        var accountBalance = Accounts.Sum(item => ParseAmount(item.BalanceText));
        var contributions = Accounts.Sum(item => ParseAmount(item.AnnualContributionText));
        AccountsSummary = Accounts.Count == 0
            ? "No accounts yet."
            : $"{CountLabel(Accounts.Count, "account")} | {FormatCurrency(accountBalance)} balance | {FormatCurrency(contributions)}/yr contributions";

        var annualIncome = Income.Sum(item => ParseAmount(item.AnnualAmountText));
        IncomeSummary = Income.Count == 0
            ? "No income yet."
            : $"{CountLabel(Income.Count, "source")} | {FormatCurrency(annualIncome)}/yr total";

        var annualExpenses = Expenses.Sum(item => ParseAmount(item.AnnualAmountText));
        ExpensesSummary = Expenses.Count == 0
            ? "No expenses yet."
            : $"{CountLabel(Expenses.Count, "expense")} | {FormatCurrency(annualExpenses)}/yr total";

        var debtBalance = Debts.Sum(item => ParseAmount(item.BalanceText));
        var minimumPayments = Debts.Sum(item => ParseAmount(item.MinimumPaymentText));
        DebtsSummary = Debts.Count == 0
            ? "No debts yet."
            : $"{CountLabel(Debts.Count, "debt")} | {FormatCurrency(debtBalance)} balance | {FormatCurrency(minimumPayments)}/mo minimum";
    }

    private static string CountLabel(int count, string singular) =>
        $"{count} {(count == 1 ? singular : $"{singular}s")}";

    private static double ParseAmount(string text) =>
        double.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var amount) ? amount : 0;

    private static string FormatCurrency(double amount) =>
        amount.ToString("C0", CultureInfo.CurrentCulture);

    public async Task LoadAsync()
    {
        if (isLoaded && loadedDataRevision == profileService.DataRevision)
        {
            return;
        }

        TrackInventoryCollections();

        await profileService.LoadAsync();
        ApplyProfile(profileService.Current);
        ApplyAssumptions();

        DetachInventoryItemHandlers();
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
            Income.Add(RetirementIncomeEditorItem.FromIncome(item));
        }

        foreach (var item in await profileExpenseRepository.ListAsync())
        {
            Expenses.Add(RetirementExpenseEditorItem.FromExpense(item));
        }

        foreach (var item in await profileDebtRepository.ListAsync())
        {
            Debts.Add(DebtEditorItem.FromDebt(item));
        }

        UpdateInventorySummaries();
        ValidationMessage = string.Empty;
        StatusMessage = string.Empty;
        loadedDataRevision = profileService.DataRevision;
        isLoaded = true;
    }

    private void DetachInventoryItemHandlers()
    {
        foreach (var item in Accounts)
        {
            item.Changed -= OnInventoryItemChanged;
        }

        foreach (var item in Income)
        {
            item.Changed -= OnInventoryItemChanged;
        }

        foreach (var item in Expenses)
        {
            item.Changed -= OnInventoryItemChanged;
        }

        foreach (var item in Debts)
        {
            item.Changed -= OnInventoryItemChanged;
        }
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
        var accounts = new List<RetirementAccount>(Accounts.Count);
        foreach (var editor in Accounts)
        {
            if (!editor.TryCreateAccount(out var account, out var error))
            {
                editor.IsExpanded = true;
                ValidationMessage = $"Account {editor.Name}: {error}";
                return;
            }

            accounts.Add(account);
        }

        var income = new List<RetirementIncomeSource>(Income.Count);
        foreach (var item in Income)
        {
            if (!item.TryCreateIncome(out var source, out var incomeError))
            {
                item.IsExpanded = true;
                ValidationMessage = $"Income {item.Name}: {incomeError}";
                return;
            }

            income.Add(source);
        }

        var expenses = new List<RetirementExpense>(Expenses.Count);
        foreach (var item in Expenses)
        {
            if (!item.TryCreateExpense(out var expense, out var expenseError))
            {
                item.IsExpanded = true;
                ValidationMessage = $"Expense {item.Name}: {expenseError}";
                return;
            }

            expenses.Add(expense);
        }

        var debts = new List<DebtItem>(Debts.Count);
        foreach (var item in Debts)
        {
            if (!item.TryCreateDebt(out var debt))
            {
                item.IsExpanded = true;
                ValidationMessage = $"Debt {item.Name}: enter a name, positive balance and minimum payment, and a rate from 0% to 100%.";
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
    private async Task AddAccountAsync()
    {
        var type = await promptService.ChooseAccountTypeAsync();
        if (type is null)
        {
            return;
        }

        var expectedReturn = TryPercent(ExpectedReturnText, 0, 15, out var parsedReturn)
            ? parsedReturn
            : 0.07;
        Accounts.Add(RetirementAccountEditorItem.CreateNew(type.Value, expectedReturn));
    }

    [RelayCommand]
    private void AddIncome()
    {
        var (startAge, endAge) = DefaultRetirementAgeRange();
        Income.Add(new RetirementIncomeEditorItem
        {
            Name = "New income",
            StartAgeText = startAge.ToString(CultureInfo.CurrentCulture),
            EndAgeText = endAge.ToString(CultureInfo.CurrentCulture),
            IsExpanded = true
        });
    }

    [RelayCommand]
    private void AddExpense()
    {
        var (startAge, _) = DefaultRetirementAgeRange();
        Expenses.Add(new RetirementExpenseEditorItem
        {
            Name = "New expense",
            StartAgeText = startAge.ToString(CultureInfo.CurrentCulture),
            IsExpanded = true
        });
    }

    [RelayCommand] private void AddDebt() => Debts.Add(new DebtEditorItem { Name = "New debt", IsExpanded = true });

    [RelayCommand]
    private async Task RemoveIncomeAsync(RetirementIncomeEditorItem? item)
    {
        if (item is null || !await ConfirmDeleteAsync("income", item.Name)) return;
        Income.Remove(item);
        await profileIncomeRepository.DeleteAsync(item.Id);
    }

    [RelayCommand]
    private async Task RemoveExpenseAsync(RetirementExpenseEditorItem? item)
    {
        if (item is null || !await ConfirmDeleteAsync("expense", item.Name)) return;
        Expenses.Remove(item);
        await profileExpenseRepository.DeleteAsync(item.Id);
    }

    [RelayCommand]
    private async Task RemoveDebtAsync(DebtEditorItem? item)
    {
        if (item is null || !await ConfirmDeleteAsync("debt", item.Name)) return;
        Debts.Remove(item);
        await profileDebtRepository.DeleteAsync(item.Id);
    }

    [RelayCommand]
    private async Task RemoveAccountAsync(RetirementAccountEditorItem? account)
    {
        if (account is null || !await ConfirmDeleteAsync("account", account.Name))
        {
            return;
        }

        Accounts.Remove(account);
        await profileAccountRepository.DeleteAsync(account.Id);
    }

    private Task<bool> ConfirmDeleteAsync(string itemType, string name) =>
        confirmationService.ConfirmAsync(
            $"Delete {itemType}?",
            $"Delete \"{name}\" from your Profile?",
            "Delete",
            "Cancel");

    private (int StartAge, int EndAge) DefaultRetirementAgeRange()
    {
        var today = localDateProvider.Today;
        var startAge = HasBirthDate
            ? ProfileAgeCalculator.AgeOn(DateOnly.FromDateTime(BirthDate), today)
            : 45;
        var endAge = HasBirthDate && HasTargetRetirementDate
            ? ProfileAgeCalculator.AgeOn(
                DateOnly.FromDateTime(BirthDate),
                DateOnly.FromDateTime(TargetRetirementDate))
            : 65;
        return (Math.Clamp(startAge, 18, 100), Math.Clamp(Math.Max(startAge, endAge), 18, 100));
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

    private static RetirementAccountEditorItem ToEditor(RetirementAccount account) =>
        RetirementAccountEditorItem.FromAccount(account);

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
