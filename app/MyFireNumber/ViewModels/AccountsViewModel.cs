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

/// <summary>
/// Owns every reusable financial inventory item (accounts, income, expenses, debts) that linked
/// calculators read, plus the overview totals, freshness, and guided check-in/history entry points
/// built on top of them. This used to live on <see cref="ProfileViewModel"/>; Profile now keeps only
/// personal identity, timeline, and planning-assumption inputs.
/// </summary>
public sealed partial class AccountsViewModel(
    IProfileService profileService,
    IProfileAccountRepository profileAccountRepository,
    IProfileIncomeRepository profileIncomeRepository,
    IProfileExpenseRepository profileExpenseRepository,
    IProfileDebtRepository profileDebtRepository,
    IFinancialCheckInRepository checkInRepository,
    ICalculatorDefaultsService calculatorDefaultsService,
    ICurrencyPreferencesService currencyPreferencesService,
    ILocalDateProvider localDateProvider,
    INavigationService navigationService,
    IConfirmationService confirmationService,
    IRetirementCashFlowPromptService promptService) : ObservableObject
{
    private bool isLoaded;
    private bool isTrackingCollections;
    private long loadedDataRevision = -1;
    private IReadOnlyList<FinancialCheckIn> allCheckIns = [];

    public ObservableCollection<RetirementAccountEditorItem> Accounts { get; } = [];
    public ObservableCollection<RetirementIncomeEditorItem> Income { get; } = [];
    public ObservableCollection<RetirementExpenseEditorItem> Expenses { get; } = [];
    public ObservableCollection<DebtEditorItem> Debts { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValidationMessage))]
    private string validationMessage = string.Empty;
    [ObservableProperty] private string statusMessage = string.Empty;

    [ObservableProperty] private string accountsSummary = "No accounts yet.";
    [ObservableProperty] private string incomeSummary = "No income yet.";
    [ObservableProperty] private string expensesSummary = "No expenses yet.";
    [ObservableProperty] private string debtsSummary = "No debts yet.";

    // Overview totals. Always computed from the live, current inventory — not from check-in history,
    // which exists only to show trends and freshness over time.
    [ObservableProperty] private string totalAssetsText = "$0";
    [ObservableProperty] private string totalDebtsText = "$0";
    [ObservableProperty] private string netWorthText = "$0";
    [ObservableProperty] private string annualIncomeTotalText = "$0";
    [ObservableProperty] private string annualExpensesTotalText = "$0";
    [ObservableProperty] private string annualCashFlowText = "$0";
    [ObservableProperty] private string annualContributionsText = "$0";

    [ObservableProperty] private bool hasCompletedCheckIn;
    [ObservableProperty] private string lastCheckInText = "You haven't completed a monthly update yet.";
    [ObservableProperty] private string nextCheckInText = string.Empty;
    [ObservableProperty] private bool isCheckInOverdue;
    [ObservableProperty] private string netWorthChangeText = string.Empty;
    [ObservableProperty] private bool hasNetWorthChange;
    [ObservableProperty] private bool isLoading = true;

    public bool HasValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);

    /// <summary>
    /// Drops the loaded state so the next appearance re-reads storage. Used after a monthly check-in
    /// completes (new balances and a new snapshot were saved directly, bypassing this view model), and
    /// after reset/import replace the underlying tables.
    /// </summary>
    public void Invalidate() => isLoaded = false;

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            await LoadCoreAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadCoreAsync()
    {
        if (isLoaded && loadedDataRevision == profileService.DataRevision)
        {
            return;
        }

        TrackInventoryCollections();

        await profileService.LoadAsync();

        DetachInventoryItemHandlers();
        Accounts.Clear();
        Income.Clear();
        Expenses.Clear();
        Debts.Clear();

        var accounts = await profileAccountRepository.ListAsync();
        var income = await profileIncomeRepository.ListAsync();
        var expenses = await profileExpenseRepository.ListAsync();
        var debts = await profileDebtRepository.ListAsync();

        foreach (var account in accounts)
        {
            Accounts.Add(RetirementAccountEditorItem.FromAccount(account));
        }

        foreach (var item in income)
        {
            Income.Add(RetirementIncomeEditorItem.FromIncome(item));
        }

        foreach (var item in expenses)
        {
            Expenses.Add(RetirementExpenseEditorItem.FromExpense(item));
        }

        foreach (var item in debts)
        {
            Debts.Add(DebtEditorItem.FromDebt(item));
        }

        UpdateInventorySummaries();
        await UpdateFreshnessAsync();
        ValidationMessage = string.Empty;
        StatusMessage = string.Empty;
        loadedDataRevision = profileService.DataRevision;
        isLoaded = true;
    }

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
    }

    private void OnExpenseCollectionChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        UpdateItemHandlers<RetirementExpenseEditorItem>(eventArgs, OnInventoryItemChanged);
        UpdateInventorySummaries();
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

    private void DetachInventoryItemHandlers()
    {
        foreach (var item in Accounts) item.Changed -= OnInventoryItemChanged;
        foreach (var item in Income) item.Changed -= OnInventoryItemChanged;
        foreach (var item in Expenses) item.Changed -= OnInventoryItemChanged;
        foreach (var item in Debts) item.Changed -= OnInventoryItemChanged;
    }

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
        var extraPayments = Debts.Sum(item => ParseAmount(item.ExtraMonthlyPaymentText));
        DebtsSummary = Debts.Count == 0
            ? "No debts yet."
            : $"{CountLabel(Debts.Count, "debt")} | {FormatCurrency(debtBalance)} balance | {FormatCurrency(minimumPayments + extraPayments)}/mo current payments";

        TotalAssetsText = FormatCurrency(accountBalance);
        TotalDebtsText = FormatCurrency(debtBalance);
        NetWorthText = FormatCurrency(accountBalance - debtBalance);
        AnnualIncomeTotalText = FormatCurrency(annualIncome);
        AnnualExpensesTotalText = FormatCurrency(annualExpenses);
        AnnualCashFlowText = FormatCurrency(annualIncome - annualExpenses);
        AnnualContributionsText = FormatCurrency(contributions);
    }

    /// <summary>
    /// Cross-references saved check-ins against the live inventory so each account/debt shows when it
    /// was last confirmed, and the overview shows overall freshness plus the change in net worth since
    /// the last completed check-in.
    /// </summary>
    private async Task UpdateFreshnessAsync()
    {
        var checkIns = await checkInRepository.ListAsync();
        allCheckIns = checkIns;
        var now = DateTime.UtcNow;

        var latest = checkIns.Count > 0 ? checkIns[^1] : null;
        HasCompletedCheckIn = latest is not null;
        if (latest is null)
        {
            LastCheckInText = "You haven't completed a monthly update yet.";
            NextCheckInText = string.Empty;
            IsCheckInOverdue = false;
            HasNetWorthChange = false;
            NetWorthChangeText = string.Empty;
        }
        else
        {
            var days = CheckInSchedule.DaysSince(latest.CompletedAtUtc, now);
            LastCheckInText = days == 0 ? "Last updated today." : $"Last updated {CountLabel(days, "day")} ago.";
            var status = CheckInSchedule.Classify(latest.CompletedAtUtc, now);
            IsCheckInOverdue = status == FreshnessStatus.Overdue;
            var dueDate = CheckInSchedule.NextDueUtc(latest.CompletedAtUtc);
            NextCheckInText = status == FreshnessStatus.Overdue
                ? $"Overdue since {dueDate:MMM d, yyyy}."
                : $"Next update due {dueDate:MMM d, yyyy}.";

            var currentNetWorth = Accounts.Sum(item => ParseAmount(item.BalanceText)) - Debts.Sum(item => ParseAmount(item.BalanceText));
            var change = currentNetWorth - latest.NetWorth;
            HasNetWorthChange = true;
            NetWorthChangeText = change switch
            {
                > 0 => $"Up {FormatCurrency(change)} since your last update.",
                < 0 => $"Down {FormatCurrency(Math.Abs(change))} since your last update.",
                _ => "No change since your last update."
            };
        }

        // Latest confirming check-in per account/debt id, scanning newest-first so the first match wins.
        var latestAccountConfirmation = new Dictionary<string, DateTime>(StringComparer.Ordinal);
        var latestDebtConfirmation = new Dictionary<string, DateTime>(StringComparer.Ordinal);
        for (var index = checkIns.Count - 1; index >= 0; index--)
        {
            var checkIn = checkIns[index];
            foreach (var account in checkIn.Accounts)
            {
                latestAccountConfirmation.TryAdd(account.AccountId, checkIn.CompletedAtUtc);
            }

            foreach (var debt in checkIn.Debts)
            {
                latestDebtConfirmation.TryAdd(debt.DebtId, checkIn.CompletedAtUtc);
            }
        }

        foreach (var account in Accounts)
        {
            ApplyFreshness(account, latestAccountConfirmation.GetValueOrDefault(account.Id), now,
                (text, overdue) => { account.FreshnessText = text; account.IsOverdue = overdue; });
        }

        foreach (var debt in Debts)
        {
            ApplyFreshness(debt, latestDebtConfirmation.GetValueOrDefault(debt.Id), now,
                (text, overdue) => { debt.FreshnessText = text; debt.IsOverdue = overdue; });
        }
    }

    private static void ApplyFreshness<TItem>(
        TItem item,
        DateTime confirmedAtUtc,
        DateTime now,
        Action<string, bool> apply)
    {
        if (confirmedAtUtc == default)
        {
            apply("Never confirmed", false);
            return;
        }

        var status = CheckInSchedule.Classify(confirmedAtUtc, now);
        var days = CheckInSchedule.DaysSince(confirmedAtUtc, now);
        var text = days == 0 ? "Confirmed today" : $"Confirmed {CountLabel(days, "day")} ago";
        apply(text, status == FreshnessStatus.Overdue);
    }

    private static string CountLabel(int count, string singular) =>
        $"{count} {(count == 1 ? singular : $"{singular}s")}";

    private static double ParseAmount(string text) =>
        double.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var amount) ? amount : 0;

    private string FormatCurrency(double amount) => currencyPreferencesService.Format(amount);

    [RelayCommand]
    private async Task SaveAsync()
    {
        // Each inventory category is validated and persisted exactly once, independently of the
        // others, so a validation failure in one leaves nothing else half-saved.
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
                ValidationMessage = $"Debt {item.Name}: enter a name, positive balance and minimum payment, non-negative extra payment, and a rate from 0% to 100%.";
                return;
            }

            debts.Add(debt);
        }

        foreach (var account in accounts) await profileAccountRepository.SaveAsync(account);
        foreach (var item in income) await profileIncomeRepository.SaveAsync(item);
        foreach (var item in expenses) await profileExpenseRepository.SaveAsync(item);
        foreach (var debt in debts) await profileDebtRepository.SaveAsync(debt);

        UpdateInventorySummaries();
        await UpdateFreshnessAsync();
        ValidationMessage = string.Empty;
        StatusMessage = "Accounts saved on this device.";
    }

    [RelayCommand]
    private async Task AddAccountAsync()
    {
        var type = await promptService.ChooseAccountTypeAsync();
        if (type is null)
        {
            return;
        }

        var expectedReturn = calculatorDefaultsService.Current.ExpectedReturn;
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
            EndAgeText = Math.Max(startAge, 90).ToString(CultureInfo.CurrentCulture),
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
            $"Delete \"{name}\" from your Accounts?",
            "Delete",
            "Cancel");

    private (int StartAge, int EndAge) DefaultRetirementAgeRange()
    {
        var today = localDateProvider.Today;
        var profile = profileService.Current;
        var startAge = profile.BirthDate is DateOnly birth
            ? ProfileAgeCalculator.AgeOn(birth, today)
            : 45;
        var endAge = profile.BirthDate is DateOnly birthDate && profile.TargetRetirementDate is DateOnly target
            ? ProfileAgeCalculator.AgeOn(birthDate, target)
            : 65;
        return (Math.Clamp(startAge, 18, 100), Math.Clamp(Math.Max(startAge, endAge), 18, 100));
    }

    [RelayCommand]
    private Task StartCheckInAsync() => navigationService.GoToAsync("accounts-check-in");

    [RelayCommand]
    private Task ViewHistoryAsync() => navigationService.GoToAsync("accounts-history");

    [RelayCommand]
    private Task ViewAccountDetailAsync(RetirementAccountEditorItem? account)
    {
        if (account is null)
        {
            return Task.CompletedTask;
        }

        var history = allCheckIns
            .SelectMany(checkIn => checkIn.Accounts
                .Where(entry => entry.AccountId == account.Id)
                .Select(entry => new AccountItemHistoryPoint(checkIn.CompletedAtUtc, entry.Balance)))
            .OrderBy(point => point.CompletedAtUtc)
            .ToArray();

        var args = new AccountItemDetailArgs(
            account.Id,
            account.Name,
            account.Type.ToString(),
            IsDebt: false,
            ParseAmount(account.BalanceText),
            account.FreshnessText,
            account.IsOverdue,
            history);

        return navigationService.GoToAsync("account-item-detail", new Dictionary<string, object> { ["details"] = args });
    }

    [RelayCommand]
    private Task ViewDebtDetailAsync(DebtEditorItem? debt)
    {
        if (debt is null)
        {
            return Task.CompletedTask;
        }

        var history = allCheckIns
            .SelectMany(checkIn => checkIn.Debts
                .Where(entry => entry.DebtId == debt.Id)
                .Select(entry => new AccountItemHistoryPoint(checkIn.CompletedAtUtc, entry.Balance)))
            .OrderBy(point => point.CompletedAtUtc)
            .ToArray();

        var args = new AccountItemDetailArgs(
            debt.Id,
            debt.Name,
            "Debt",
            IsDebt: true,
            ParseAmount(debt.BalanceText),
            debt.FreshnessText,
            debt.IsOverdue,
            history);

        return navigationService.GoToAsync("account-item-detail", new Dictionary<string, object> { ["details"] = args });
    }
}
