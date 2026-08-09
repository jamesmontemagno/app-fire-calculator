using CommunityToolkit.Mvvm.ComponentModel;
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

    public event EventHandler? Changed;

    public static DebtEditorItem FromDebt(DebtItem debt)
    {
        return new DebtEditorItem
        {
            Id = debt.Id,
            Name = debt.Name,
            BalanceText = debt.Balance.ToString("0.##", CultureInfo.CurrentCulture),
            RateText = (debt.Rate * 100).ToString("0.##", CultureInfo.CurrentCulture),
            MinimumPaymentText = debt.MinimumPayment.ToString("0.##", CultureInfo.CurrentCulture)
        };
    }

    public bool TryCreateDebt(out DebtItem debt)
    {
        debt = new DebtItem(Id, Name.Trim(), 0, 0, 0);
        return !string.IsNullOrWhiteSpace(debt.Name)
            && TryParseNonNegative(BalanceText, out var balance)
            && balance > 0
            && TryParsePercentage(RateText, out var rate)
            && TryParseNonNegative(MinimumPaymentText, out var minimumPayment)
            && minimumPayment > 0
            && SetDebt(out debt, balance, rate, minimumPayment);
    }

    partial void OnNameChanged(string value) => Changed?.Invoke(this, EventArgs.Empty);
    partial void OnBalanceTextChanged(string value) => Changed?.Invoke(this, EventArgs.Empty);
    partial void OnRateTextChanged(string value) => Changed?.Invoke(this, EventArgs.Empty);
    partial void OnMinimumPaymentTextChanged(string value) => Changed?.Invoke(this, EventArgs.Empty);

    private bool SetDebt(out DebtItem debt, double balance, double rate, double minimumPayment)
    {
        debt = new DebtItem(Id, Name.Trim(), balance, rate, minimumPayment);
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