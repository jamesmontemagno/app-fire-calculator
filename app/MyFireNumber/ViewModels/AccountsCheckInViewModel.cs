using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Profile;
using MyFireNumber.Services;
using MyFireNumber.Storage;
using System.Collections.ObjectModel;
using System.Globalization;

namespace MyFireNumber.ViewModels;

/// <summary>Which page of the guided monthly check-in is currently shown.</summary>
public enum CheckInStep
{
    Accounts,
    Debts,
    IncomeAndExpenses,
    Confirm
}

/// <summary>
/// Guided monthly check-in wizard: confirm or update account and debt balances, review income and
/// expenses, then save. Nothing is persisted -- no updated balances and no
/// <see cref="FinancialCheckIn"/> snapshot -- until <see cref="CompleteCommand"/> succeeds, so
/// backing out at any earlier step leaves the saved data untouched.
/// </summary>
public sealed partial class AccountsCheckInViewModel(
    IProfileAccountRepository profileAccountRepository,
    IProfileIncomeRepository profileIncomeRepository,
    IProfileExpenseRepository profileExpenseRepository,
    IProfileDebtRepository profileDebtRepository,
    IFinancialCheckInRepository checkInRepository,
    ICurrencyPreferencesService currencyPreferencesService,
    INavigationService navigationService,
    AccountsViewModel accountsViewModel) : ObservableObject
{
    // The order steps are presented in. Steps whose collection is empty are skipped automatically
    // (e.g. a household with no debts never sees a "Debts" page), but Confirm always shows.
    private static readonly CheckInStep[] StepOrder =
    [
        CheckInStep.Accounts,
        CheckInStep.Debts,
        CheckInStep.IncomeAndExpenses,
        CheckInStep.Confirm
    ];

    public ObservableCollection<RetirementAccountEditorItem> Accounts { get; } = [];
    public ObservableCollection<DebtEditorItem> Debts { get; } = [];
    public ObservableCollection<RetirementIncomeEditorItem> Income { get; } = [];
    public ObservableCollection<RetirementExpenseEditorItem> Expenses { get; } = [];

    [ObservableProperty] private int stepIndex;
    [ObservableProperty] private string validationMessage = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CompleteCommand))]
    [NotifyPropertyChangedFor(nameof(IsNotSaving))]
    private bool isSaving;

    [ObservableProperty] private string totalAssetsText = "$0";
    [ObservableProperty] private string totalDebtsText = "$0";
    [ObservableProperty] private string netWorthText = "$0";
    [ObservableProperty] private string annualIncomeText = "$0";
    [ObservableProperty] private string annualExpensesText = "$0";
    [ObservableProperty] private string annualCashFlowText = "$0";

    public bool HasValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);
    public bool IsNotSaving => !IsSaving;

    public CheckInStep CurrentStep => StepIndex >= 0 && StepIndex < StepOrder.Length ? StepOrder[StepIndex] : CheckInStep.Confirm;
    public bool IsAccountsStep => CurrentStep == CheckInStep.Accounts;
    public bool IsDebtsStep => CurrentStep == CheckInStep.Debts;
    public bool IsIncomeAndExpensesStep => CurrentStep == CheckInStep.IncomeAndExpenses;
    public bool IsConfirmStep => CurrentStep == CheckInStep.Confirm;
    public bool IsNotConfirmStep => !IsConfirmStep;
    public bool HasIncome => Income.Count > 0;
    public bool HasExpenses => Expenses.Count > 0;
    public bool CanGoBack => StepIndex > 0;
    public string NextButtonText => IsConfirmStep ? "Complete check-in" : "Continue";
    public string ProgressText => $"Step {StepIndex + 1} of {StepOrder.Length}: {StepTitle}";
    public double ProgressFraction => (StepIndex + 1) / (double)StepOrder.Length;

    public string StepTitle => CurrentStep switch
    {
        CheckInStep.Accounts => "Confirm account balances",
        CheckInStep.Debts => "Confirm debt balances",
        CheckInStep.IncomeAndExpenses => "Review income and expenses",
        _ => "Confirm and save"
    };

    partial void OnValidationMessageChanged(string value) => OnPropertyChanged(nameof(HasValidationMessage));
    partial void OnStepIndexChanged(int value)
    {
        OnPropertyChanged(nameof(CurrentStep));
        OnPropertyChanged(nameof(IsAccountsStep));
        OnPropertyChanged(nameof(IsDebtsStep));
        OnPropertyChanged(nameof(IsIncomeAndExpensesStep));
        OnPropertyChanged(nameof(IsConfirmStep));
        OnPropertyChanged(nameof(IsNotConfirmStep));
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(NextButtonText));
        OnPropertyChanged(nameof(StepTitle));
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(ProgressFraction));
        ValidationMessage = string.Empty;
    }

    /// <summary>
    /// Loads fresh working copies every time the wizard is opened, prefilled with the current saved
    /// balances, so an abandoned check-in never leaves stale data around for next time.
    /// </summary>
    public async Task LoadAsync()
    {
        Accounts.Clear();
        Debts.Clear();
        Income.Clear();
        Expenses.Clear();

        foreach (var account in await profileAccountRepository.ListAsync())
        {
            Accounts.Add(RetirementAccountEditorItem.FromAccount(account));
        }

        foreach (var debt in await profileDebtRepository.ListAsync())
        {
            Debts.Add(DebtEditorItem.FromDebt(debt));
        }

        foreach (var income in await profileIncomeRepository.ListAsync())
        {
            Income.Add(RetirementIncomeEditorItem.FromIncome(income));
        }

        foreach (var expense in await profileExpenseRepository.ListAsync())
        {
            Expenses.Add(RetirementExpenseEditorItem.FromExpense(expense));
        }

        OnPropertyChanged(nameof(HasIncome));
        OnPropertyChanged(nameof(HasExpenses));

        StepIndex = 0;
        SkipEmptyStepsForward();
        ValidationMessage = string.Empty;
        UpdateConfirmationTotals();
    }

    [RelayCommand]
    private void Next()
    {
        if (!ValidateCurrentStep())
        {
            return;
        }

        if (IsConfirmStep)
        {
            return;
        }

        StepIndex++;
        SkipEmptyStepsForward();
        UpdateConfirmationTotals();
    }

    [RelayCommand]
    private void Back()
    {
        if (!CanGoBack)
        {
            return;
        }

        StepIndex--;
        SkipEmptyStepsBackward();
    }

    [RelayCommand]
    private Task CancelAsync() => IsSaving ? Task.CompletedTask : navigationService.GoToAsync("..");

    [RelayCommand(CanExecute = nameof(CanComplete))]
    private async Task CompleteAsync()
    {
        if (!ValidateCurrentStep() || IsSaving)
        {
            return;
        }

        IsSaving = true;
        try
        {
            var accounts = new List<RetirementAccount>(Accounts.Count);
            foreach (var editor in Accounts)
            {
                if (!editor.TryCreateAccount(out var account, out var error))
                {
                    ValidationMessage = $"Account {editor.Name}: {error}";
                    return;
                }

                accounts.Add(account);
            }

            var debts = new List<DebtItem>(Debts.Count);
            foreach (var editor in Debts)
            {
                if (!editor.TryCreateDebt(out var debt))
                {
                    ValidationMessage = $"Debt {editor.Name}: enter a valid balance.";
                    return;
                }

                debts.Add(debt);
            }

            var income = new List<RetirementIncomeSource>(Income.Count);
            foreach (var editor in Income)
            {
                if (!editor.TryCreateIncome(out var source, out var incomeError))
                {
                    ValidationMessage = $"Income {editor.Name}: {incomeError}";
                    return;
                }

                income.Add(source);
            }

            var expenses = new List<RetirementExpense>(Expenses.Count);
            foreach (var editor in Expenses)
            {
                if (!editor.TryCreateExpense(out var expense, out var expenseError))
                {
                    ValidationMessage = $"Expense {editor.Name}: {expenseError}";
                    return;
                }

                expenses.Add(expense);
            }

            foreach (var account in accounts) await profileAccountRepository.SaveAsync(account);
            foreach (var debt in debts) await profileDebtRepository.SaveAsync(debt);
            foreach (var source in income) await profileIncomeRepository.SaveAsync(source);
            foreach (var expense in expenses) await profileExpenseRepository.SaveAsync(expense);

            var checkIn = new FinancialCheckIn(
                Guid.NewGuid().ToString("N"),
                DateTime.UtcNow,
                [.. accounts.Select(a => new AccountBalanceEntry(a.Id, a.Name, a.Type, a.Balance))],
                [.. debts.Select(d => new DebtBalanceEntry(d.Id, d.Name, d.Balance))],
                income.Sum(i => i.AnnualAmount),
                expenses.Sum(e => e.AnnualAmount));

            await checkInRepository.SaveAsync(checkIn);

            // The Accounts tab is a long-lived singleton; invalidate it so the next time it appears it
            // reloads the balances and freshness this check-in just saved instead of showing stale data.
            accountsViewModel.Invalidate();

            await navigationService.GoToAsync("..");
        }
        finally
        {
            IsSaving = false;
        }
    }

    private bool CanComplete() => !IsSaving;

    private void UpdateConfirmationTotals()
    {
        var assets = Accounts.Sum(item => ParseAmount(item.BalanceText));
        var debts = Debts.Sum(item => ParseAmount(item.BalanceText));
        var income = Income.Sum(item => ParseAmount(item.AnnualAmountText));
        var expenses = Expenses.Sum(item => ParseAmount(item.AnnualAmountText));

        TotalAssetsText = FormatCurrency(assets);
        TotalDebtsText = FormatCurrency(debts);
        NetWorthText = FormatCurrency(assets - debts);
        AnnualIncomeText = FormatCurrency(income);
        AnnualExpensesText = FormatCurrency(expenses);
        AnnualCashFlowText = FormatCurrency(income - expenses);
    }

    private bool ValidateCurrentStep()
    {
        ValidationMessage = string.Empty;
        switch (CurrentStep)
        {
            case CheckInStep.Accounts:
                foreach (var account in Accounts)
                {
                    if (!TryParseNonNegative(account.BalanceText, out _))
                    {
                        ValidationMessage = $"Enter a valid balance for {account.Name}.";
                        return false;
                    }
                }

                return true;

            case CheckInStep.Debts:
                foreach (var debt in Debts)
                {
                    if (!TryParseNonNegative(debt.BalanceText, out _))
                    {
                        ValidationMessage = $"Enter a valid balance for {debt.Name}.";
                        return false;
                    }
                }

                return true;

            case CheckInStep.IncomeAndExpenses:
                foreach (var item in Income)
                {
                    if (!TryParseNonNegative(item.AnnualAmountText, out _))
                    {
                        ValidationMessage = $"Enter a valid annual amount for {item.Name}.";
                        return false;
                    }
                }

                foreach (var item in Expenses)
                {
                    if (!TryParseNonNegative(item.AnnualAmountText, out _))
                    {
                        ValidationMessage = $"Enter a valid annual amount for {item.Name}.";
                        return false;
                    }
                }

                return true;

            default:
                return true;
        }
    }

    /// <summary>An empty step (e.g. no debts entered yet) has nothing to confirm, so skip it.</summary>
    private void SkipEmptyStepsForward()
    {
        while (StepIndex < StepOrder.Length - 1 && IsStepEmpty(StepOrder[StepIndex]))
        {
            StepIndex++;
        }
    }

    private void SkipEmptyStepsBackward()
    {
        while (StepIndex > 0 && IsStepEmpty(StepOrder[StepIndex]))
        {
            StepIndex--;
        }
    }

    private bool IsStepEmpty(CheckInStep step) => step switch
    {
        CheckInStep.Accounts => Accounts.Count == 0,
        CheckInStep.Debts => Debts.Count == 0,
        CheckInStep.IncomeAndExpenses => Income.Count == 0 && Expenses.Count == 0,
        _ => false
    };

    private static bool TryParseNonNegative(string text, out double value) =>
        double.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value) && value >= 0;

    private static double ParseAmount(string text) =>
        double.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var amount) ? amount : 0;

    private string FormatCurrency(double amount) => currencyPreferencesService.Format(amount);
}
