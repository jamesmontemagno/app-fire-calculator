using MyFireNumber.Core.Calculations;

namespace MyFireNumber.Tests.Calculations;

public class DeferredCompensationCalculatorTests
{
    [Fact]
    public void DeferredAccount_IsDistributedAcrossConfiguredPayoutYears()
    {
        var account = new RetirementAccount(
            "deferred",
            "Deferred compensation",
            RetirementAccountType.Deferred,
            10_000,
            0,
            0.05,
            50,
            0,
            2,
            0);
        var result = DeferredCompensationCalculator.Calculate(Inputs(account, annualExpenses: 5_000, planThroughAge: 51));

        Assert.Equal(10_000, result.CurrentBalance);
        Assert.Equal(5_000, result.BalanceAtSemiRetirement);
        Assert.Equal(5_000, result.FirstYearIncome);
        Assert.Equal(0, result.FirstYearSurplus);
        Assert.Equal(0, result.EndingBalance);
        Assert.Equal(2, result.FundedYears);
        Assert.Equal(5_000, result.Projections[0].Withdrawals["deferred"]);

        // The undistributed 5,000 earns 5% during payout, so the final installment is larger than
        // the first and the account still lands at exactly zero.
        Assert.Equal(5_250, result.Projections[1].Withdrawals["deferred"]);
        Assert.Equal(0, result.Projections[1].Balances["deferred"]);
    }

    [Fact]
    public void PortfolioWithdrawal_RespectsAccountWithdrawalRateAndTracksFundingGap()
    {
        var account = new RetirementAccount(
            "taxable",
            "Taxable brokerage",
            RetirementAccountType.Taxable,
            10_000,
            0,
            0,
            50,
            0.1,
            1,
            0);
        var result = DeferredCompensationCalculator.Calculate(Inputs(account, annualExpenses: 1_000, planThroughAge: 51));

        Assert.Equal(9_000, result.Projections[0].TotalBalance);
        Assert.Equal(1_000, result.Projections[0].PortfolioWithdrawals);
        Assert.Equal(0, result.Projections[0].Surplus);
        Assert.Equal(8_100, result.Projections[1].TotalBalance);
        Assert.Equal(900, result.Projections[1].PortfolioWithdrawals);
        Assert.Equal(-100, result.Projections[1].Surplus);
        Assert.Equal(1, result.FundedYears);

        // The balance could have covered the whole 1,000; the self-imposed rate blocked 100 of it.
        Assert.Equal(100, result.Projections[1].PolicyLimitedWithdrawals);
        Assert.Equal(0, result.Projections[0].PolicyLimitedWithdrawals);
    }

    [Fact]
    public void Contributions_EscalateWithInflationLikeExpenses()
    {
        var account = new RetirementAccount("savings", "Savings", RetirementAccountType.Savings, 0, 1_000, 0, 40, 0, 1, 0);
        var inputs = new DeferredCompensationInputs(
            CurrentAge: 40,
            SemiRetirementAge: 42,
            PlanThroughAge: 42,
            AnnualExpenses: 0,
            InflationRate: 0.1,
            Accounts: [account],
            IncomeSources: [],
            AdditionalExpenses: [],
            WithdrawOnlyAfterRetirement: true,
            ReinvestSurplus: false,
            CurrentYear: 2026);

        var result = DeferredCompensationCalculator.Calculate(inputs);

        // The entered 1,000 is today's dollars, so the single contribution year pays 1,000 * 1.1.
        // A flat contribution would leave 1,000 here.
        Assert.Equal(1_100, result.BalanceAtSemiRetirement);
    }

    [Fact]
    public void TaxableWithdrawal_IsGrossedUpSoSpendableAmountCoversTheGap()
    {
        var account = new RetirementAccount("401k", "401(k)", RetirementAccountType.Traditional, 100_000, 0, 0, 50, 1, 1, 0.25);
        var result = DeferredCompensationCalculator.Calculate(Inputs(account, annualExpenses: 10_000, planThroughAge: 50));
        var point = result.Projections[0];

        Assert.Equal(13_333, point.Withdrawals["401k"]);
        Assert.Equal(10_000, point.PortfolioWithdrawals);
        Assert.Equal(3_333, point.WithdrawalTaxes);
        Assert.Equal(0, point.Surplus);
        Assert.Equal(86_667, point.TotalBalance);
    }

    [Fact]
    public void RothWithdrawal_DefaultsToTaxFree()
    {
        var account = new RetirementAccount("roth", "Roth IRA", RetirementAccountType.Roth, 100_000, 0, 0, 50, 1, 1);
        var result = DeferredCompensationCalculator.Calculate(Inputs(account, annualExpenses: 10_000, planThroughAge: 50));
        var point = result.Projections[0];

        Assert.Equal(0, account.EffectiveWithdrawalTaxRate);
        Assert.Equal(10_000, point.Withdrawals["roth"]);
        Assert.Equal(0, point.WithdrawalTaxes);
    }

    [Fact]
    public void MissingWithdrawalTaxRate_ResolvesToTheAccountTypeDefault()
    {
        // Drafts saved before this field existed deserialize with a null rate. They must resolve to
        // the type default rather than silently becoming tax-free.
        var legacy = new RetirementAccount("401k", "401(k)", RetirementAccountType.Traditional, 100_000, 0, 0, 50, 1, 1);

        Assert.Null(legacy.WithdrawalTaxRate);
        Assert.Equal(0.25, legacy.EffectiveWithdrawalTaxRate);
        Assert.Equal(0.25, new RetirementAccount("d", "d", RetirementAccountType.Deferred, 0, 0, 0, 50, 0, 1).EffectiveWithdrawalTaxRate);
        Assert.Equal(0, new RetirementAccount("t", "t", RetirementAccountType.Taxable, 0, 0, 0, 50, 0, 1).EffectiveWithdrawalTaxRate);
        Assert.Equal(0, new RetirementAccount("h", "h", RetirementAccountType.Hsa, 0, 0, 0, 50, 0, 1).EffectiveWithdrawalTaxRate);
    }

    [Fact]
    public void FundedYears_CountsConsecutiveYearsAndReportsTheFirstShortfallAge()
    {
        var inputs = new DeferredCompensationInputs(
            CurrentAge: 60,
            SemiRetirementAge: 60,
            PlanThroughAge: 62,
            AnnualExpenses: 10_000,
            InflationRate: 0,
            Accounts: [],
            IncomeSources:
            [
                new RetirementIncomeSource("early", "Early", 5_000, 60, 61, 0, true, 0),
                new RetirementIncomeSource("later", "Later", 20_000, 62, 120, 0, true, 0)
            ],
            AdditionalExpenses: [],
            WithdrawOnlyAfterRetirement: true,
            ReinvestSurplus: false,
            CurrentYear: 2026);

        var result = DeferredCompensationCalculator.Calculate(inputs);

        // Ages 60 and 61 fall short; 62 is covered. A plain count of covered years reports 1 and
        // reads as a duration, which is the defect. The funded span is zero.
        Assert.Equal(0, result.FundedYears);
        Assert.Equal(1, result.YearsFullyCovered);
        Assert.Equal(60, result.FirstShortfallAge);
        Assert.Equal(3, result.RetirementYears);
    }

    [Fact]
    public void RetirementYears_CountsOnlyYearsAtOrAfterRetirement()
    {
        var inputs = new DeferredCompensationInputs(
            CurrentAge: 45,
            SemiRetirementAge: 55,
            PlanThroughAge: 90,
            AnnualExpenses: 1,
            InflationRate: 0,
            Accounts: [],
            IncomeSources: [new RetirementIncomeSource("i", "Income", 1_000, 45, 120, 0, true, 0)],
            AdditionalExpenses: [],
            WithdrawOnlyAfterRetirement: true,
            ReinvestSurplus: false,
            CurrentYear: 2026);

        var result = DeferredCompensationCalculator.Calculate(inputs);

        // The projection array spans 46 ages but only 36 of them are retirement years.
        Assert.Equal(46, result.Projections.Count);
        Assert.Equal(36, result.RetirementYears);
        Assert.Equal(36, result.FundedYears);
        Assert.Null(result.FirstShortfallAge);
    }

    private static DeferredCompensationInputs Inputs(RetirementAccount account, double annualExpenses, int planThroughAge) => new(
        CurrentAge: 50,
        SemiRetirementAge: 50,
        PlanThroughAge: planThroughAge,
        AnnualExpenses: annualExpenses,
        InflationRate: 0,
        Accounts: [account],
        IncomeSources: [],
        AdditionalExpenses: [],
        WithdrawOnlyAfterRetirement: true,
        ReinvestSurplus: false,
        CurrentYear: 2026);
}
