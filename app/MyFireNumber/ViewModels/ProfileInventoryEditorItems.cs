using CommunityToolkit.Mvvm.ComponentModel;
using MyFireNumber.Core.Presentation;
using MyFireNumber.Core.Profile;
using System.Globalization;

namespace MyFireNumber.ViewModels;

public sealed partial class ProfileRecurringEditorItem : ObservableObject
{
    public IReadOnlyList<CurrencyPeriod> Periods { get; } = Enum.GetValues<CurrencyPeriod>();

    [ObservableProperty] private string id = Guid.NewGuid().ToString("N");
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string amountText = "0";
    [ObservableProperty] private CurrencyPeriod period = CurrencyPeriod.Monthly;
    [ObservableProperty] private string category = string.Empty;

    public bool TryGetAmount(out double amount) =>
        double.TryParse(AmountText, NumberStyles.Number, CultureInfo.CurrentCulture, out amount) && amount >= 0;

    public static ProfileRecurringEditorItem FromIncome(ProfileIncome item) => new()
    {
        Id = item.Id, Name = item.Name, AmountText = item.Amount.ToString("0.##", CultureInfo.CurrentCulture),
        Period = item.Period, Category = item.Category ?? string.Empty
    };

    public static ProfileRecurringEditorItem FromExpense(ProfileExpense item) => new()
    {
        Id = item.Id, Name = item.Name, AmountText = item.Amount.ToString("0.##", CultureInfo.CurrentCulture),
        Period = item.Period, Category = item.Category ?? string.Empty
    };
}

public sealed partial class ProfileDebtEditorItem : ObservableObject
{
    [ObservableProperty] private string id = Guid.NewGuid().ToString("N");
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string balanceText = "0";
    [ObservableProperty] private string rateText = "0";
    [ObservableProperty] private string minimumPaymentText = "0";

    public bool TryCreate(out ProfileDebt debt)
    {
        debt = new ProfileDebt(Id, Name.Trim(), 0, 0, 0);
        if (string.IsNullOrWhiteSpace(Name) ||
            !TryNonNegative(BalanceText, out var balance) ||
            !TryNonNegative(RateText, out var rate) ||
            !TryNonNegative(MinimumPaymentText, out var minimum))
        {
            return false;
        }

        debt = new ProfileDebt(Id, Name.Trim(), balance, rate / 100, minimum);
        return true;
    }

    public static ProfileDebtEditorItem FromDebt(ProfileDebt debt) => new()
    {
        Id = debt.Id,
        Name = debt.Name,
        BalanceText = debt.Balance.ToString("0.##", CultureInfo.CurrentCulture),
        RateText = (debt.Rate * 100).ToString("0.##", CultureInfo.CurrentCulture),
        MinimumPaymentText = debt.MinimumPayment.ToString("0.##", CultureInfo.CurrentCulture)
    };

    private static bool TryNonNegative(string text, out double value) =>
        double.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value) && value >= 0;
}
