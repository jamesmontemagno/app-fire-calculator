using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyFireNumber.Core.Calculations;
using System.Globalization;

namespace MyFireNumber.ViewModels;

public sealed partial class RetirementAccountEditorItem : ObservableObject
{
    public IReadOnlyList<RetirementAccountType> AccountTypes { get; } = Enum.GetValues<RetirementAccountType>();

    [ObservableProperty]
    private string id = Guid.NewGuid().ToString("N");

    [ObservableProperty]
    private string name = "New account";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UsesPayoutSchedule))]
    [NotifyPropertyChangedFor(nameof(UsesWithdrawalRate))]
    private RetirementAccountType type = RetirementAccountType.Traditional;

    [ObservableProperty]
    private string balanceText = "0";

    [ObservableProperty]
    private string annualContributionText = "0";

    [ObservableProperty]
    private string annualReturnText = "5";

    [ObservableProperty]
    private string availableAgeText = "59";

    [ObservableProperty]
    private string withdrawalRateText = "4";

    [ObservableProperty]
    private string payoutYearsText = "5";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpansionGlyph))]
    private bool isExpanded;

    public string ExpansionGlyph => IsExpanded ? "\uf078" : "\uf054";
    public bool UsesPayoutSchedule => Type == RetirementAccountType.Deferred;
    public bool UsesWithdrawalRate => !UsesPayoutSchedule;

    public event EventHandler? Changed;

    public static RetirementAccountEditorItem FromAccount(RetirementAccount account) => new()
    {
        Id = account.Id,
        Name = account.Name,
        Type = account.Type,
        BalanceText = Format(account.Balance),
        AnnualContributionText = Format(account.AnnualContribution),
        AnnualReturnText = Format(account.AnnualReturn * 100),
        AvailableAgeText = account.AvailableAge.ToString(CultureInfo.CurrentCulture),
        WithdrawalRateText = Format(account.WithdrawalRate * 100),
        PayoutYearsText = account.PayoutYears.ToString(CultureInfo.CurrentCulture)
    };

    public bool TryCreateAccount(out RetirementAccount account, out string validationError)
    {
        account = new RetirementAccount(Id, Name.Trim(), Type, 0, 0, 0, 0, 0, 0);
        validationError = string.Empty;
        var withdrawalRate = 0d;
        var payoutYears = 1;
        if (string.IsNullOrWhiteSpace(account.Name))
        {
            validationError = "Name is required.";
            return false;
        }

        if (!TryNonNegative(BalanceText, out var balance))
        {
            validationError = "Balance must be a number of zero or more.";
            return false;
        }

        if (!TryNonNegative(AnnualContributionText, out var contribution))
        {
            validationError = "Annual contribution must be a number of zero or more.";
            return false;
        }

        if (!TryPercentage(AnnualReturnText, -100, 100, out var annualReturn))
        {
            validationError = "Expected annual return must be between -100% and 100%.";
            return false;
        }

        if (!TryAge(AvailableAgeText, out var availableAge))
        {
            validationError = "Available age must be a whole number from 18 to 100.";
            return false;
        }

        if (UsesPayoutSchedule)
        {
            if (!int.TryParse(PayoutYearsText, NumberStyles.Integer, CultureInfo.CurrentCulture, out payoutYears)
                || payoutYears is < 1 or > 50)
            {
                validationError = "Payout years must be a whole number from 1 to 50.";
                return false;
            }
        }
        else if (!TryPercentage(WithdrawalRateText, 0, 100, out withdrawalRate))
        {
            validationError = "Withdrawal rate must be between 0% and 100%.";
            return false;
        }

        account = new RetirementAccount(
            Id,
            Name.Trim(),
            Type,
            balance,
            contribution,
            annualReturn,
            availableAge,
            withdrawalRate,
            payoutYears);
        return true;
    }

    partial void OnNameChanged(string value) => RaiseChanged();
    partial void OnTypeChanged(RetirementAccountType value) => RaiseChanged();
    partial void OnBalanceTextChanged(string value) => RaiseChanged();
    partial void OnAnnualContributionTextChanged(string value) => RaiseChanged();
    partial void OnAnnualReturnTextChanged(string value) => RaiseChanged();
    partial void OnAvailableAgeTextChanged(string value) => RaiseChanged();
    partial void OnWithdrawalRateTextChanged(string value) => RaiseChanged();
    partial void OnPayoutYearsTextChanged(string value) => RaiseChanged();

    [RelayCommand]
    private void ToggleExpanded() => IsExpanded = !IsExpanded;

    private void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
    private static string Format(double value) => value.ToString("0.##", CultureInfo.CurrentCulture);
    private static bool TryNonNegative(string text, out double value) =>
        double.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value) && value >= 0;
    private static bool TryAge(string text, out int value) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out value) && value is >= 18 and <= 100;
    private static bool TryPercentage(string text, double minimum, double maximum, out double value)
    {
        value = 0;
        return double.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var percent)
            && percent >= minimum
            && percent <= maximum
            && (value = percent / 100) >= -1;
    }
}

public sealed partial class RetirementIncomeEditorItem : ObservableObject
{
    [ObservableProperty]
    private string id = Guid.NewGuid().ToString("N");

    [ObservableProperty]
    private string name = "New income";

    [ObservableProperty]
    private string annualAmountText = "0";

    [ObservableProperty]
    private string startAgeText = "55";

    [ObservableProperty]
    private string endAgeText = "65";

    [ObservableProperty]
    private string annualGrowthText = "0";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RequiresTaxRate))]
    private bool isAfterTax = true;

    [ObservableProperty]
    private string taxRateText = "0";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpansionGlyph))]
    private bool isExpanded;

    public string ExpansionGlyph => IsExpanded ? "\uf078" : "\uf054";
    public bool RequiresTaxRate => !IsAfterTax;

    public event EventHandler? Changed;

    public static RetirementIncomeEditorItem FromIncome(RetirementIncomeSource income) => new()
    {
        Id = income.Id,
        Name = income.Name,
        AnnualAmountText = Format(income.AnnualAmount),
        StartAgeText = income.StartAge.ToString(CultureInfo.CurrentCulture),
        EndAgeText = income.EndAge.ToString(CultureInfo.CurrentCulture),
        AnnualGrowthText = Format(income.AnnualGrowth * 100),
        IsAfterTax = income.IsAfterTax,
        TaxRateText = Format(income.TaxRate * 100)
    };

    public bool TryCreateIncome(out RetirementIncomeSource income, out string validationError)
    {
        income = new RetirementIncomeSource(Id, Name.Trim(), 0, 0, 0, 0, IsAfterTax, 0);
        validationError = string.Empty;
        if (string.IsNullOrWhiteSpace(income.Name))
        {
            validationError = "Name is required.";
            return false;
        }

        if (!TryNonNegative(AnnualAmountText, out var amount))
        {
            validationError = "Annual amount must be a number of zero or more.";
            return false;
        }

        if (!TryAge(StartAgeText, out var startAge))
        {
            validationError = "Start age must be a whole number from 18 to 100.";
            return false;
        }

        if (!TryAge(EndAgeText, out var endAge))
        {
            validationError = "End age must be a whole number from 18 to 100.";
            return false;
        }

        if (endAge < startAge)
        {
            validationError = "End age must be the same as or later than start age.";
            return false;
        }

        if (!TryPercentage(AnnualGrowthText, -100, 100, out var annualGrowth))
        {
            validationError = "Annual growth must be between -100% and 100%.";
            return false;
        }

        var taxRate = 0d;
        if (RequiresTaxRate && !TryPercentage(TaxRateText, 0, 100, out taxRate))
        {
            validationError = "Tax rate must be between 0% and 100%.";
            return false;
        }

        income = new RetirementIncomeSource(Id, Name.Trim(), amount, startAge, endAge, annualGrowth, IsAfterTax, taxRate);
        return true;
    }

    partial void OnNameChanged(string value) => RaiseChanged();
    partial void OnAnnualAmountTextChanged(string value) => RaiseChanged();
    partial void OnStartAgeTextChanged(string value) => RaiseChanged();
    partial void OnEndAgeTextChanged(string value) => RaiseChanged();
    partial void OnAnnualGrowthTextChanged(string value) => RaiseChanged();
    partial void OnIsAfterTaxChanged(bool value) => RaiseChanged();
    partial void OnTaxRateTextChanged(string value) => RaiseChanged();

    [RelayCommand]
    private void ToggleExpanded() => IsExpanded = !IsExpanded;

    private void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
    private static string Format(double value) => value.ToString("0.##", CultureInfo.CurrentCulture);
    private static bool TryNonNegative(string text, out double value) =>
        double.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value) && value >= 0;
    private static bool TryAge(string text, out int value) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out value) && value is >= 18 and <= 100;
    private static bool TryPercentage(string text, double minimum, double maximum, out double value)
    {
        value = 0;
        return double.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var percent)
            && percent >= minimum
            && percent <= maximum
            && (value = percent / 100) >= -1;
    }
}

public sealed partial class RetirementExpenseEditorItem : ObservableObject
{
    [ObservableProperty]
    private string id = Guid.NewGuid().ToString("N");

    [ObservableProperty]
    private string name = "New expense";

    [ObservableProperty]
    private string annualAmountText = "0";

    [ObservableProperty]
    private string startAgeText = "55";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpansionGlyph))]
    private bool isExpanded;

    public string ExpansionGlyph => IsExpanded ? "\uf078" : "\uf054";

    public event EventHandler? Changed;

    public static RetirementExpenseEditorItem FromExpense(RetirementExpense expense) => new()
    {
        Id = expense.Id,
        Name = expense.Name,
        AnnualAmountText = expense.AnnualAmount.ToString("0.##", CultureInfo.CurrentCulture),
        StartAgeText = expense.StartAge.ToString(CultureInfo.CurrentCulture)
    };

    public bool TryCreateExpense(out RetirementExpense expense, out string validationError)
    {
        expense = new RetirementExpense(Id, Name.Trim(), 0, 0);
        validationError = string.Empty;
        if (string.IsNullOrWhiteSpace(expense.Name))
        {
            validationError = "Name is required.";
            return false;
        }

        if (!double.TryParse(AnnualAmountText, NumberStyles.Number, CultureInfo.CurrentCulture, out var amount)
            || amount < 0)
        {
            validationError = "Annual amount must be a number of zero or more.";
            return false;
        }

        if (!int.TryParse(StartAgeText, NumberStyles.Integer, CultureInfo.CurrentCulture, out var startAge)
            || startAge is < 18 or > 100)
        {
            validationError = "Start age must be a whole number from 18 to 100.";
            return false;
        }

        expense = new RetirementExpense(Id, Name.Trim(), amount, startAge);
        return true;
    }

    partial void OnNameChanged(string value) => RaiseChanged();
    partial void OnAnnualAmountTextChanged(string value) => RaiseChanged();
    partial void OnStartAgeTextChanged(string value) => RaiseChanged();

    [RelayCommand]
    private void ToggleExpanded() => IsExpanded = !IsExpanded;

    private void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
