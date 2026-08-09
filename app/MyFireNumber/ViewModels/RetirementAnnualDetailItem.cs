using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyFireNumber.ViewModels;

public partial class RetirementAnnualDetailItem(
    string title,
    string balanceText,
    string incomeText,
    string expensesText,
    string surplusText,
    string incomeBreakdown,
    string expenseBreakdown,
    string accountBalanceBreakdown) : ObservableObject
{
    public string Title { get; } = title;
    public string BalanceText { get; } = balanceText;
    public string IncomeText { get; } = incomeText;
    public string ExpensesText { get; } = expensesText;
    public string SurplusText { get; } = surplusText;
    public string IncomeBreakdown { get; } = incomeBreakdown;
    public string ExpenseBreakdown { get; } = expenseBreakdown;
    public string AccountBalanceBreakdown { get; } = accountBalanceBreakdown;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpansionGlyph), nameof(ExpansionDescription))]
    private bool isExpanded;

    public string ExpansionGlyph => IsExpanded ? "\uf077" : "\uf078";
    public string ExpansionDescription => IsExpanded ? $"Collapse {Title}" : $"Expand {Title}";

    [RelayCommand]
    private void Toggle()
    {
        IsExpanded = !IsExpanded;
    }
}
