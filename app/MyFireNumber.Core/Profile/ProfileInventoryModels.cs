using MyFireNumber.Core.Calculations;

namespace MyFireNumber.Core.Profile;

public enum ScenarioDataMode
{
    Standalone,
    LinkedProfile
}

public sealed record ProfileFinancialSnapshot(
    FinancialProfile Profile,
    IReadOnlyList<RetirementAccount> Accounts,
    IReadOnlyList<RetirementIncomeSource> Income,
    IReadOnlyList<RetirementExpense> Expenses,
    IReadOnlyList<DebtItem> Debts,
    long Revision)
{
    public double TotalAccountBalance => Accounts.Sum(account => account.Balance);
    public double TotalAnnualContributions => Accounts.Sum(account => account.AnnualContribution);
    public double TotalAnnualIncome => Income.Sum(item => item.AnnualAmount);
    public double TotalAnnualExpenses => Expenses.Sum(item => item.AnnualAmount);

    /// <summary>The itemized Profile income calculators should use, or null when none is entered.</summary>
    public double? EffectiveAnnualIncome =>
        Income.Count > 0 ? TotalAnnualIncome : null;

    /// <summary>The itemized Profile expenses calculators should use, or null when none are entered.</summary>
    public double? EffectiveAnnualExpenses =>
        Expenses.Count > 0 ? TotalAnnualExpenses : null;

    /// <summary>Whether the Profile contains itemized income.</summary>
    public bool IsIncomeItemised => Income.Count > 0;

    /// <summary>Whether the Profile contains itemized expenses.</summary>
    public bool AreExpensesItemised => Expenses.Count > 0;
}

public sealed record ProfileResolution<TDraft>(
    TDraft Draft,
    long ProfileRevision,
    IReadOnlyList<string> Sources,
    IReadOnlyList<string> Errors)
{
    public bool IsValid => Errors.Count == 0;
}
