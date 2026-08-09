using CommunityToolkit.Mvvm.ComponentModel;
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

    public bool TryCreateAccount(out RetirementAccount account)
    {
        account = new RetirementAccount(Id, Name.Trim(), Type, 0, 0, 0, 0, 0, 0);
        if (string.IsNullOrWhiteSpace(account.Name)
            || !TryNonNegative(BalanceText, out var balance)
            || !TryNonNegative(AnnualContributionText, out var contribution)
            || !TryPercentage(AnnualReturnText, -100, 100, out var annualReturn)
            || !TryAge(AvailableAgeText, out var availableAge)
            || !TryPercentage(WithdrawalRateText, 0, 100, out var withdrawalRate)
            || !int.TryParse(PayoutYearsText, NumberStyles.Integer, CultureInfo.CurrentCulture, out var payoutYears)
            || payoutYears is < 1 or > 50)
        {
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
    private bool isAfterTax = true;

    [ObservableProperty]
    private string taxRateText = "0";

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

    public bool TryCreateIncome(out RetirementIncomeSource income)
    {
        income = new RetirementIncomeSource(Id, Name.Trim(), 0, 0, 0, 0, IsAfterTax, 0);
        if (string.IsNullOrWhiteSpace(income.Name)
            || !TryNonNegative(AnnualAmountText, out var amount)
            || !TryAge(StartAgeText, out var startAge)
            || !TryAge(EndAgeText, out var endAge)
            || endAge < startAge
            || !TryPercentage(AnnualGrowthText, -100, 100, out var annualGrowth)
            || !TryPercentage(TaxRateText, 0, 100, out var taxRate))
        {
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

    public event EventHandler? Changed;

    public static RetirementExpenseEditorItem FromExpense(RetirementExpense expense) => new()
    {
        Id = expense.Id,
        Name = expense.Name,
        AnnualAmountText = expense.AnnualAmount.ToString("0.##", CultureInfo.CurrentCulture),
        StartAgeText = expense.StartAge.ToString(CultureInfo.CurrentCulture)
    };

    public bool TryCreateExpense(out RetirementExpense expense)
    {
        expense = new RetirementExpense(Id, Name.Trim(), 0, 0);
        if (string.IsNullOrWhiteSpace(expense.Name)
            || !double.TryParse(AnnualAmountText, NumberStyles.Number, CultureInfo.CurrentCulture, out var amount)
            || amount < 0
            || !int.TryParse(StartAgeText, NumberStyles.Integer, CultureInfo.CurrentCulture, out var startAge)
            || startAge is < 18 or > 100)
        {
            return false;
        }

        expense = new RetirementExpense(Id, Name.Trim(), amount, startAge);
        return true;
    }

    partial void OnNameChanged(string value) => RaiseChanged();
    partial void OnAnnualAmountTextChanged(string value) => RaiseChanged();
    partial void OnStartAgeTextChanged(string value) => RaiseChanged();

    private void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
