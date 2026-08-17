using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpansionGlyph))]
    private bool isExpanded;

    public string ExpansionGlyph => IsExpanded ? "\uf078" : "\uf054";

    [RelayCommand]
    private void ToggleExpanded() => IsExpanded = !IsExpanded;

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
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpansionGlyph))]
    private bool isExpanded;

    public string ExpansionGlyph => IsExpanded ? "\uf078" : "\uf054";

    [RelayCommand]
    private void ToggleExpanded() => IsExpanded = !IsExpanded;

    /// <summary>
    /// Applies the same constraints as <see cref="DebtEditorItem.TryCreateDebt"/>: a positive balance
    /// and payment and a 0-100% rate. Anything looser would let the profile store debts that a linked
    /// Debt Payoff scenario immediately rejects.
    /// </summary>
    public bool TryCreate(out ProfileDebt debt, out string validationError)
    {
        debt = new ProfileDebt(Id, Name.Trim(), 0, 0, 0);
        validationError = string.Empty;

        if (string.IsNullOrWhiteSpace(Name))
        {
            validationError = "Name is required.";
            return false;
        }

        if (!TryParse(BalanceText, out var balance) || balance <= 0)
        {
            validationError = "Balance must be greater than zero.";
            return false;
        }

        if (!TryParse(RateText, out var rate) || rate is < 0 or > 100)
        {
            validationError = "Interest rate must be between 0% and 100%.";
            return false;
        }

        if (!TryParse(MinimumPaymentText, out var minimum) || minimum <= 0)
        {
            validationError = "Minimum payment must be greater than zero.";
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

    private static bool TryParse(string text, out double value) =>
        double.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value);
}
