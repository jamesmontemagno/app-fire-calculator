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

/// <summary>
/// A single account's balance as captured by a monthly check-in. The name and type are copied at
/// check-in time (not looked up live) so history keeps reading correctly even after the source
/// account is renamed, re-typed, or deleted.
/// </summary>
public sealed record AccountBalanceEntry(
    string AccountId,
    string Name,
    RetirementAccountType Type,
    double Balance);

/// <summary>A single debt's balance as captured by a monthly check-in. See <see cref="AccountBalanceEntry"/>.</summary>
public sealed record DebtBalanceEntry(
    string DebtId,
    string Name,
    double Balance);

/// <summary>
/// A timestamped, entirely local snapshot produced by the guided monthly check-in. Every field is
/// copied at the moment the check-in is saved, so the snapshot never changes even as the live
/// Accounts data it was based on is edited or deleted later.
/// </summary>
public sealed record FinancialCheckIn(
    string Id,
    DateTime CompletedAtUtc,
    IReadOnlyList<AccountBalanceEntry> Accounts,
    IReadOnlyList<DebtBalanceEntry> Debts,
    double AnnualIncome,
    double AnnualExpenses)
{
    public double TotalAssets => Accounts.Sum(account => account.Balance);
    public double TotalDebts => Debts.Sum(debt => debt.Balance);
    public double NetWorth => TotalAssets - TotalDebts;
    public double AnnualCashFlow => AnnualIncome - AnnualExpenses;
}
