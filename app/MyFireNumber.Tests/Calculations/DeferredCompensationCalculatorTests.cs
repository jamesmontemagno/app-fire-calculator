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
            2);
        var inputs = new DeferredCompensationInputs(
            CurrentAge: 50,
            SemiRetirementAge: 50,
            PlanThroughAge: 51,
            AnnualExpenses: 5_000,
            InflationRate: 0,
            Accounts: [account],
            IncomeSources: [],
            AdditionalExpenses: [],
            WithdrawOnlyAfterRetirement: true,
            ReinvestSurplus: false,
            CurrentYear: 2026);

        var result = DeferredCompensationCalculator.Calculate(inputs);

        Assert.Equal(10_000, result.CurrentBalance);
        Assert.Equal(5_000, result.BalanceAtSemiRetirement);
        Assert.Equal(5_000, result.FirstYearIncome);
        Assert.Equal(0, result.FirstYearSurplus);
        Assert.Equal(0, result.EndingBalance);
        Assert.Equal(2, result.FundedYears);
        Assert.Equal(5_000, result.Projections[0].Withdrawals["deferred"]);
        Assert.Equal(5_000, result.Projections[1].Withdrawals["deferred"]);
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
            1);
        var inputs = new DeferredCompensationInputs(
            CurrentAge: 50,
            SemiRetirementAge: 50,
            PlanThroughAge: 51,
            AnnualExpenses: 1_000,
            InflationRate: 0,
            Accounts: [account],
            IncomeSources: [],
            AdditionalExpenses: [],
            WithdrawOnlyAfterRetirement: true,
            ReinvestSurplus: false,
            CurrentYear: 2026);

        var result = DeferredCompensationCalculator.Calculate(inputs);

        Assert.Equal(9_000, result.Projections[0].TotalBalance);
        Assert.Equal(1_000, result.Projections[0].PortfolioWithdrawals);
        Assert.Equal(0, result.Projections[0].Surplus);
        Assert.Equal(8_100, result.Projections[1].TotalBalance);
        Assert.Equal(900, result.Projections[1].PortfolioWithdrawals);
        Assert.Equal(-100, result.Projections[1].Surplus);
        Assert.Equal(1, result.FundedYears);
    }
}