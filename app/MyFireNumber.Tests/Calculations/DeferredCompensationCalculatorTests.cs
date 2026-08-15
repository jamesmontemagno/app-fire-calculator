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
    public void PortfolioWithdrawal_ExceedsTheAccountWithdrawalRateRatherThanReportingAShortfall()
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

        // Year one: a 10% rate on 10,000 releases exactly the 1,000 needed, so the policy is not
        // exceeded and nothing about this year changes.
        Assert.Equal(9_000, result.Projections[0].TotalBalance);
        Assert.Equal(1_000, result.Projections[0].PortfolioWithdrawals);
        Assert.Equal(0, result.Projections[0].Surplus);
        Assert.Equal(0, result.Projections[0].PolicyExcessWithdrawals);

        // Year two flipped for issue #56. 10% of the reduced 9,000 balance is only 900, and the plan
        // used to stop there: withdraw 900, report a 100 shortfall, and carry 8,100 forward. It now
        // takes the remaining 100 as well, so the year is funded and the 100 is disclosed as having
        // been taken above the stated rate.
        //   TotalBalance 8,100 -> 8,000; PortfolioWithdrawals 900 -> 1,000; Surplus -100 -> 0;
        //   FundedYears 1 -> 2. The 100 figure itself is unchanged, but it now means the opposite:
        //   it was the amount the rate held back, and it is now the amount the rate was exceeded by.
        Assert.Equal(8_000, result.Projections[1].TotalBalance);
        Assert.Equal(1_000, result.Projections[1].PortfolioWithdrawals);
        Assert.Equal(0, result.Projections[1].Surplus);
        Assert.Equal(2, result.FundedYears);
        Assert.Equal(100, result.Projections[1].PolicyExcessWithdrawals);
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

    /// <summary>
    /// Issue #56. The per-account withdrawal rate is a spending policy, not a hard limit: a year the
    /// policy alone cannot cover withdraws beyond it, bounded by what the reachable accounts hold.
    /// These mirror the TypeScript cases in
    /// <c>web/src/utils/__tests__/deferredCompensation.test.ts</c> one for one, because the two
    /// engines have to agree on this to the dollar.
    /// </summary>
    [Fact]
    public void CapFlex_CountsAYearAsFundedWhenExceedingThePolicyIsWhatCoveredIt()
    {
        // The distinction the whole change turns on: whether to exceed the policy is decided against
        // the shortfall *before* flexing, but the funded/short verdict is read from the surplus
        // *after*. A 4% rate releases 4,000 against a 10,000 need, so this year is short on the
        // first pass and covered on the second, and the disclosure records that it took a breach.
        var account = new RetirementAccount(
            "taxable", "Taxable brokerage", RetirementAccountType.Taxable, 100_000, 0, 0, 50, 0.04, 1, 0);
        var result = DeferredCompensationCalculator.Calculate(
            Inputs(account, annualExpenses: 10_000, planThroughAge: 59));

        Assert.Equal(10_000, result.Projections[0].Withdrawals["taxable"]);
        Assert.Equal(0, result.Projections[0].Surplus);
        Assert.Equal(6_000, result.Projections[0].PolicyExcessWithdrawals);

        // 100,000 covers ten 10,000 years, so 50-59 are all funded. Under the old hard cap none were.
        Assert.Equal(10, result.FundedYears);
        Assert.Null(result.FirstShortfallAge);
        Assert.Equal(0, result.EndingBalance);
    }

    [Fact]
    public void CapFlex_LeavesThePolicyAloneWhenItAlreadyCoversTheYear()
    {
        // 4% of 100,000 is 4,000 against a 1,000 need, so the need binds, not the cap. The gate asks
        // the same shortfall question the headline verdict asks, and gets "no", so nothing flexes.
        var account = new RetirementAccount(
            "taxable", "Taxable brokerage", RetirementAccountType.Taxable, 100_000, 0, 0, 50, 0.04, 1, 0);
        var result = DeferredCompensationCalculator.Calculate(
            Inputs(account, annualExpenses: 1_000, planThroughAge: 50));

        Assert.Equal(1_000, result.Projections[0].Withdrawals["taxable"]);
        Assert.Equal(0, result.Projections[0].PolicyExcessWithdrawals);
        Assert.Equal(99_000, result.EndingBalance);
    }

    [Fact]
    public void CapFlex_WillNotReachAnAccountBeforeItsAvailabilityAge()
    {
        // Exceeding a spending policy is a choice the plan gets to make; opening a locked account is
        // not. Only the 20,000 account is reachable at 50, so the year is short by 30,000 even
        // though 1,020,000 exists on paper, and the locked balance is untouched afterwards.
        var open = new RetirementAccount(
            "open", "Brokerage", RetirementAccountType.Taxable, 20_000, 0, 0, 50, 0.04, 1, 0);
        var locked = new RetirementAccount(
            "locked", "401(k)", RetirementAccountType.Traditional, 1_000_000, 0, 0, 60, 0.04, 1, 0);
        var inputs = Inputs(open, annualExpenses: 50_000, planThroughAge: 51) with
        {
            Accounts = [open, locked],
        };
        var result = DeferredCompensationCalculator.Calculate(inputs);

        Assert.Equal(20_000, result.Projections[0].Withdrawals["open"]);
        Assert.Equal(0, result.Projections[0].Withdrawals.GetValueOrDefault("locked"));
        Assert.Equal(-30_000, result.Projections[0].Surplus);
        Assert.Equal(1_000_000, result.Projections[1].Balances["locked"]);
    }

    [Fact]
    public void CapFlex_StopsAtTheBalanceRatherThanOverdrawingIt()
    {
        var account = new RetirementAccount(
            "taxable", "Taxable brokerage", RetirementAccountType.Taxable, 30_000, 0, 0, 50, 0.04, 1, 0);
        var result = DeferredCompensationCalculator.Calculate(
            Inputs(account, annualExpenses: 100_000, planThroughAge: 52));

        Assert.Equal(30_000, result.Projections[0].Withdrawals["taxable"]);
        Assert.Equal(-70_000, result.Projections[0].Surplus);

        // Exactly zero, not a floating-point residue, and never negative in a later year.
        Assert.Equal(0, result.Projections[1].TotalBalance);
        Assert.Equal(0, result.Projections[2].TotalBalance);
        Assert.Equal(0, result.EndingBalance);
    }

    [Fact]
    public void CapFlex_ProratesAcrossReachableAccountsByBalance()
    {
        // Both accounts give up the same fraction of their balance, which is what keeps the result
        // independent of the order the accounts happen to sit in. Net capacity is
        // 50,000 + 40,000 * 0.75 = 80,000, so a 10,000 need scales every balance by 0.125: 6,250
        // gross from the untaxed account and 5,000 from the taxed one, of which 1,250 is tax.
        // Both rates are 0 so the policy pass contributes nothing and this isolates the flex.
        var taxFree = new RetirementAccount(
            "taxfree", "Roth", RetirementAccountType.Roth, 50_000, 0, 0, 50, 0, 1, 0);
        var taxed = new RetirementAccount(
            "taxed", "401(k)", RetirementAccountType.Traditional, 40_000, 0, 0, 50, 0, 1, 0.25);
        var inputs = Inputs(taxFree, annualExpenses: 10_000, planThroughAge: 50) with
        {
            Accounts = [taxFree, taxed],
        };
        var result = DeferredCompensationCalculator.Calculate(inputs);

        Assert.Equal(6_250, result.Projections[0].Withdrawals["taxfree"]);
        Assert.Equal(5_000, result.Projections[0].Withdrawals["taxed"]);
        Assert.Equal(1_250, result.Projections[0].WithdrawalTaxes);
        Assert.Equal(0, result.Projections[0].Surplus);

        // Taxable-first would have taken the whole 10,000 from the untaxed account and ended on
        // 80,000. Proration ends on 78,750, which is what makes the ordering choice testable.
        Assert.Equal(78_750, result.EndingBalance);
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
