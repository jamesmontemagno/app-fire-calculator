using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyFireNumber.Core.Calculations;
using System.Globalization;

namespace MyFireNumber.ViewModels;

public sealed partial class DebtEditorItem : ObservableObject
{
    [ObservableProperty]
    private string id = Guid.NewGuid().ToString("N");

    [ObservableProperty]
    private string name = "Debt";

    [ObservableProperty]
    private string balanceText = string.Empty;

    [ObservableProperty]
    private string rateText = string.Empty;

    [ObservableProperty]
    private string minimumPaymentText = string.Empty;

    [ObservableProperty]
    private string extraMonthlyPaymentText = "0";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditable))]
    private bool isReadOnly;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpansionGlyph))]
    private bool isExpanded;

    /// <summary>Set by the Accounts overview after loading check-in history; not persisted.</summary>
    [ObservableProperty]
    private string freshnessText = "Never confirmed";

    [ObservableProperty]
    private bool isOverdue;

    public bool IsEditable => !IsReadOnly;
    public string ExpansionGlyph => IsExpanded ? "\uf078" : "\uf054";

    public event EventHandler? Changed;

    public static DebtEditorItem FromDebt(DebtItem debt)
    {
        return new DebtEditorItem
        {
            Id = debt.Id,
            Name = debt.Name,
            BalanceText = debt.Balance.ToString("0.##", CultureInfo.CurrentCulture),
            RateText = (debt.Rate * 100).ToString("0.##", CultureInfo.CurrentCulture),
            MinimumPaymentText = debt.MinimumPayment.ToString("0.##", CultureInfo.CurrentCulture),
            ExtraMonthlyPaymentText = debt.ExtraMonthlyPayment.ToString("0.##", CultureInfo.CurrentCulture)
        };
    }

    public bool TryCreateDebt(out DebtItem debt)
    {
        debt = new DebtItem(Id, Name.Trim(), 0, 0, 0, 0);
        return !string.IsNullOrWhiteSpace(debt.Name)
            && TryParseNonNegative(BalanceText, out var balance)
            && balance > 0
            && TryParsePercentage(RateText, out var rate)
            && TryParseNonNegative(MinimumPaymentText, out var minimumPayment)
            && minimumPayment > 0
            && TryParseNonNegative(ExtraMonthlyPaymentText, out var extraMonthlyPayment)
            && SetDebt(out debt, balance, rate, minimumPayment, extraMonthlyPayment);
    }

    partial void OnNameChanged(string value) => Changed?.Invoke(this, EventArgs.Empty);
    partial void OnBalanceTextChanged(string value) => Changed?.Invoke(this, EventArgs.Empty);
    partial void OnRateTextChanged(string value) => Changed?.Invoke(this, EventArgs.Empty);
    partial void OnMinimumPaymentTextChanged(string value) => Changed?.Invoke(this, EventArgs.Empty);
    partial void OnExtraMonthlyPaymentTextChanged(string value) => Changed?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void ToggleExpanded() => IsExpanded = !IsExpanded;

    private bool SetDebt(
        out DebtItem debt,
        double balance,
        double rate,
        double minimumPayment,
        double extraMonthlyPayment)
    {
        debt = new DebtItem(Id, Name.Trim(), balance, rate, minimumPayment, extraMonthlyPayment);
        return true;
    }

    private static bool TryParseNonNegative(string value, out double number)
    {
        return double.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out number) && number >= 0;
    }

    private static bool TryParsePercentage(string value, out double percentage)
    {
        percentage = 0;
        return double.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out var percent)
            && percent is >= 0 and <= 100
            && (percentage = percent / 100) >= 0;
    }
}