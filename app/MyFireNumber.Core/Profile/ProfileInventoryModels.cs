using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Presentation;

namespace MyFireNumber.Core.Profile;

public enum ScenarioDataMode
{
    Standalone,
    LinkedProfile
}

public sealed record ProfileIncome(
    string Id,
    string Name,
    double Amount,
    CurrencyPeriod Period,
    string? Category)
{
    public double AnnualAmount => CurrencyPeriodMath.Convert(Amount, Period, CurrencyPeriod.Annual);
}

public sealed record ProfileExpense(
    string Id,
    string Name,
    double Amount,
    CurrencyPeriod Period,
    string? Category)
{
    public double AnnualAmount => CurrencyPeriodMath.Convert(Amount, Period, CurrencyPeriod.Annual);
}

public sealed record ProfileDebt(
    string Id,
    string Name,
    double Balance,
    double Rate,
    double MinimumPayment)
{
    public DebtItem ToDebtItem() => new(Id, Name, Balance, Rate, MinimumPayment);
}

public sealed record ProfileFinancialSnapshot(
    FinancialProfile Profile,
    IReadOnlyList<ProfileAccount> Accounts,
    IReadOnlyList<ProfileIncome> Income,
    IReadOnlyList<ProfileExpense> Expenses,
    IReadOnlyList<ProfileDebt> Debts,
    long Revision)
{
    public double TotalAccountBalance => Accounts.Sum(account => account.Balance);
    public double TotalAnnualContributions => Accounts.Sum(account => account.AnnualContribution);
    public double TotalAnnualIncome => Income.Sum(item => item.AnnualAmount);
    public double TotalAnnualExpenses => Expenses.Sum(item => item.AnnualAmount);
}

public sealed record ProfileResolution<TDraft>(
    TDraft Draft,
    long ProfileRevision,
    IReadOnlyList<string> Sources,
    IReadOnlyList<string> Errors)
{
    public bool IsValid => Errors.Count == 0;
}
