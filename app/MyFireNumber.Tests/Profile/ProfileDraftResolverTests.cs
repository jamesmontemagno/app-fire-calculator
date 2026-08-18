using MyFireNumber.Core.Calculations;
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
            income: [Income("salary", 120_000)],
            expenses: [Expense("living", 48_000)]);

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
            income: [Income("salary", 100_000)],
            expenses: [Expense("living", 40_000)]);
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
            income: [Income("salary", 100_000)],
            expenses: [Expense("living", 40_000)]);

        var result = ProfileDraftResolver.Resolve(StandardFireDraft.Default, snapshot, new DateOnly(2026, 1, 1));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("account", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Resolve_MapsProfileDebts()
    {
        var snapshot = Snapshot(debts: [new DebtItem("card", "Card", 5_000, 0.2, 150, 50)]);
        var source = DebtPayoffDraft.Default with { MonthlyBudget = 1_000, ExtraPayment = 25 };

        var result = ProfileDraftResolver.Resolve(source, snapshot, new DateOnly(2026, 1, 1));

        Assert.True(result.IsValid);
        Assert.Collection(result.Draft.Debts, debt => Assert.Equal("card", debt.Id));
        Assert.Equal(200, result.Draft.MonthlyBudget);
        Assert.Equal(25, result.Draft.ExtraPayment);
    }

    [Fact]
    public void Resolve_UsesItemisedIncomeAndExpenses()
    {
        var snapshot = Snapshot(
            accounts: [Account("one", 100_000, 10_000)],
            income: [Income("salary", 100_000)],
            expenses: [Expense("living", 40_000)]) with
        {
            Profile = FinancialProfile.Empty with { AnnualIncome = 55_000, AnnualExpenses = 22_000 }
        };

        var result = ProfileDraftResolver.Resolve(StandardFireDraft.Default, snapshot, new DateOnly(2026, 1, 1));

        Assert.True(result.IsValid);
        Assert.Equal(100_000, result.Draft.AnnualIncome);
        Assert.Equal(40_000, result.Draft.AnnualExpenses);
    }

    [Fact]
    public void Resolve_IgnoresLegacyHouseholdFiguresWhenNothingIsItemised()
    {
        var snapshot = Snapshot(accounts: [Account("one", 100_000, 10_000)]) with
        {
            Profile = FinancialProfile.Empty with { AnnualIncome = 55_000, AnnualExpenses = 22_000 }
        };

        var result = ProfileDraftResolver.Resolve(StandardFireDraft.Default, snapshot, new DateOnly(2026, 1, 1));

        Assert.False(result.IsValid);
        Assert.Equal(StandardFireDraft.Default.AnnualIncome, result.Draft.AnnualIncome);
        Assert.Equal(StandardFireDraft.Default.AnnualExpenses, result.Draft.AnnualExpenses);
        Assert.Contains(result.Errors, error => error.Contains("income", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, error => error.Contains("expense", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Resolve_BlocksOnlyWhenNeitherSourceHasIncome()
    {
        var snapshot = Snapshot(
            accounts: [Account("one", 100_000, 10_000)],
            expenses: [Expense("living", 40_000)]);

        var result = ProfileDraftResolver.Resolve(StandardFireDraft.Default, snapshot, new DateOnly(2026, 1, 1));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("income", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EffectiveValues_MatchWhatTheResolverApplies()
    {
        var itemised = Snapshot(
            income: [Income("a", 12_000)]) with
        {
            Profile = FinancialProfile.Empty with { AnnualIncome = 99_000 }
        };
        var householdOnly = Snapshot() with
        {
            Profile = FinancialProfile.Empty with { AnnualIncome = 99_000 }
        };

        Assert.Equal(12_000, itemised.EffectiveAnnualIncome);
        Assert.True(itemised.IsIncomeItemised);
        Assert.Null(householdOnly.EffectiveAnnualIncome);
        Assert.False(householdOnly.IsIncomeItemised);
        Assert.Null(Snapshot().EffectiveAnnualIncome);
    }

    [Fact]
    public void Resolve_MapsCompleteRetirementInventoryWithoutDoubleCountingExpenses()
    {
        var account = Account("brokerage", 100_000, 5_000);
        var income = Income("pension", 24_000);
        var expense = Expense("healthcare", 12_000);
        var snapshot = Snapshot(accounts: [account], income: [income], expenses: [expense]) with
        {
            Profile = FinancialProfile.Empty with
            {
                BirthDate = new DateOnly(1980, 1, 1),
                PhasedRetirementDate = new DateOnly(2035, 1, 1),
                AnnualExpenses = 50_000
            }
        };

        var result = ProfileDraftResolver.Resolve(
            DeferredCompensationDraft.Default,
            snapshot,
            new DateOnly(2026, 1, 1));

        Assert.True(result.IsValid);
        Assert.Equal(0, result.Draft.AnnualExpenses);
        Assert.Equal([account], result.Draft.Accounts);
        Assert.Equal([income], result.Draft.IncomeSources);
        Assert.Equal([expense], result.Draft.AdditionalExpenses);
    }

    private static ProfileFinancialSnapshot Snapshot(
        IReadOnlyList<RetirementAccount>? accounts = null,
        IReadOnlyList<RetirementIncomeSource>? income = null,
        IReadOnlyList<RetirementExpense>? expenses = null,
        IReadOnlyList<DebtItem>? debts = null) => new(
        FinancialProfile.Empty,
        accounts ?? [],
        income ?? [],
        expenses ?? [],
        debts ?? [],
        1);

    private static RetirementAccount Account(string id, double balance, double contribution) => new(
        id, id, RetirementAccountType.Taxable, balance, contribution, 0.07, 18, 0.04, 1, 0);

    private static RetirementIncomeSource Income(string id, double annualAmount) => new(
        id, id, annualAmount, 55, 65, 0, true, 0);

    private static RetirementExpense Expense(string id, double annualAmount) => new(
        id, id, annualAmount, 55, 90);
}
