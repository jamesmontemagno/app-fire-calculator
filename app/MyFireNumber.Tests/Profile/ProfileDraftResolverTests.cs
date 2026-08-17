using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Presentation;
using MyFireNumber.Core.Profile;

namespace MyFireNumber.Tests.Profile;

public sealed class ProfileDraftResolverTests
{
    [Fact]
    public void Resolve_MapsLiveProfileAggregatesIntoStandardFire()
    {
        var snapshot = Snapshot(
            accounts:
            [
                Account("one", 100_000, 10_000),
                Account("two", 50_000, 5_000)
            ],
            income: [new ProfileIncome("salary", "Salary", 10_000, CurrencyPeriod.Monthly, null)],
            expenses: [new ProfileExpense("living", "Living", 4_000, CurrencyPeriod.Monthly, null)]);

        var result = ProfileDraftResolver.Resolve(
            StandardFireDraft.Default,
            snapshot,
            new DateOnly(2026, 8, 16));

        Assert.True(result.IsValid);
        Assert.Equal(150_000, result.Draft.CurrentSavings);
        Assert.Equal(15_000, result.Draft.AnnualContribution);
        Assert.Equal(120_000, result.Draft.AnnualIncome);
        Assert.Equal(48_000, result.Draft.AnnualExpenses);
    }

    [Fact]
    public void Resolve_UsesCurrentCategoryAfterItemsAreDeleted()
    {
        var before = Snapshot(
            accounts: [Account("one", 100_000, 10_000), Account("two", 50_000, 5_000)],
            income: [new ProfileIncome("salary", "Salary", 100_000, CurrencyPeriod.Annual, null)],
            expenses: [new ProfileExpense("living", "Living", 40_000, CurrencyPeriod.Annual, null)]);
        var after = before with { Accounts = [before.Accounts[0]], Revision = 2 };

        var first = ProfileDraftResolver.Resolve(StandardFireDraft.Default, before, new DateOnly(2026, 1, 1));
        var second = ProfileDraftResolver.Resolve(StandardFireDraft.Default, after, new DateOnly(2026, 1, 1));

        Assert.Equal(150_000, first.Draft.CurrentSavings);
        Assert.Equal(100_000, second.Draft.CurrentSavings);
    }

    [Fact]
    public void Resolve_BlocksWhenRequiredCategoryIsEmpty()
    {
        var snapshot = Snapshot(
            accounts: [],
            income: [new ProfileIncome("salary", "Salary", 100_000, CurrencyPeriod.Annual, null)],
            expenses: [new ProfileExpense("living", "Living", 40_000, CurrencyPeriod.Annual, null)]);

        var result = ProfileDraftResolver.Resolve(StandardFireDraft.Default, snapshot, new DateOnly(2026, 1, 1));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("account", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Resolve_MapsProfileDebts()
    {
        var snapshot = Snapshot(debts: [new ProfileDebt("card", "Card", 5_000, 0.2, 150)]);

        var result = ProfileDraftResolver.Resolve(DebtPayoffDraft.Default, snapshot, new DateOnly(2026, 1, 1));

        Assert.True(result.IsValid);
        Assert.Collection(result.Draft.Debts, debt => Assert.Equal("card", debt.Id));
    }

    [Fact]
    public void Resolve_PrefersItemisedIncomeOverTheHouseholdFigure()
    {
        var snapshot = Snapshot(
            accounts: [Account("one", 100_000, 10_000)],
            income: [new ProfileIncome("salary", "Salary", 100_000, CurrencyPeriod.Annual, null)],
            expenses: [new ProfileExpense("living", "Living", 40_000, CurrencyPeriod.Annual, null)]) with
        {
            Profile = FinancialProfile.Empty with { AnnualIncome = 55_000, AnnualExpenses = 22_000 }
        };

        var result = ProfileDraftResolver.Resolve(StandardFireDraft.Default, snapshot, new DateOnly(2026, 1, 1));

        Assert.True(result.IsValid);
        Assert.Equal(100_000, result.Draft.AnnualIncome);
        Assert.Equal(40_000, result.Draft.AnnualExpenses);
    }

    [Fact]
    public void Resolve_FallsBackToTheHouseholdFigureWhenNothingIsItemised()
    {
        var snapshot = Snapshot(accounts: [Account("one", 100_000, 10_000)]) with
        {
            Profile = FinancialProfile.Empty with { AnnualIncome = 55_000, AnnualExpenses = 22_000 }
        };

        var result = ProfileDraftResolver.Resolve(StandardFireDraft.Default, snapshot, new DateOnly(2026, 1, 1));

        Assert.True(result.IsValid);
        Assert.Equal(55_000, result.Draft.AnnualIncome);
        Assert.Equal(22_000, result.Draft.AnnualExpenses);
    }

    [Fact]
    public void Resolve_BlocksOnlyWhenNeitherSourceHasIncome()
    {
        var snapshot = Snapshot(
            accounts: [Account("one", 100_000, 10_000)],
            expenses: [new ProfileExpense("living", "Living", 40_000, CurrencyPeriod.Annual, null)]);

        var result = ProfileDraftResolver.Resolve(StandardFireDraft.Default, snapshot, new DateOnly(2026, 1, 1));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("income", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EffectiveValues_MatchWhatTheResolverApplies()
    {
        var itemised = Snapshot(
            income: [new ProfileIncome("a", "A", 1_000, CurrencyPeriod.Monthly, null)]) with
        {
            Profile = FinancialProfile.Empty with { AnnualIncome = 99_000 }
        };
        var householdOnly = Snapshot() with
        {
            Profile = FinancialProfile.Empty with { AnnualIncome = 99_000 }
        };

        Assert.Equal(12_000, itemised.EffectiveAnnualIncome);
        Assert.True(itemised.IsIncomeItemised);
        Assert.Equal(99_000, householdOnly.EffectiveAnnualIncome);
        Assert.False(householdOnly.IsIncomeItemised);
        Assert.Null(Snapshot().EffectiveAnnualIncome);
    }

    private static ProfileFinancialSnapshot Snapshot(
        IReadOnlyList<ProfileAccount>? accounts = null,
        IReadOnlyList<ProfileIncome>? income = null,
        IReadOnlyList<ProfileExpense>? expenses = null,
        IReadOnlyList<ProfileDebt>? debts = null) => new(
        FinancialProfile.Empty,
        accounts ?? [],
        income ?? [],
        expenses ?? [],
        debts ?? [],
        1);

    private static ProfileAccount Account(string id, double balance, double contribution) => new(
        id, id, RetirementAccountType.Taxable, balance, contribution, 0.07, 18, 0.04, 1, 0);
}
