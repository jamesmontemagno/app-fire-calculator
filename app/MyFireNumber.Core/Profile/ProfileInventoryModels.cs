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

    /// <summary>
    /// The income every calculator should use. Itemised entries win when the user has added any,
    /// because they are the more specific answer; the single household figure from onboarding is the
    /// fallback. Null when neither has been provided.
    /// <para>This precedence has to live in one place: linked plans read the itemised totals while
    /// new scenarios read the household figure, so without a shared rule the same profile produces
    /// two different numbers depending on how the scenario was created.</para>
    /// </summary>
    public double? EffectiveAnnualIncome =>
        Income.Count > 0 ? TotalAnnualIncome : Profile.AnnualIncome;

    /// <inheritdoc cref="EffectiveAnnualIncome"/>
    public double? EffectiveAnnualExpenses =>
        Expenses.Count > 0 ? TotalAnnualExpenses : Profile.AnnualExpenses;

    /// <summary>Whether itemised entries are overriding the single household income figure.</summary>
    public bool IsIncomeItemised => Income.Count > 0;

    /// <inheritdoc cref="IsIncomeItemised"/>
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
