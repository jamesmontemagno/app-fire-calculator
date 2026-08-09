using MyFireNumber.Core.Calculations;

namespace MyFireNumber.Tests.Calculations;

public class FinancialCalculatorTests
{
    private static readonly FireInputs DefaultInputs = new(
        CurrentAge: 30,
        RetirementAge: 55,
        CurrentSavings: 100_000,
        AnnualContribution: 24_000,
        AnnualIncome: 72_000,
        ExpectedReturn: 0.07,
        InflationRate: 0.03,
        WithdrawalRate: 0.04,
        AnnualExpenses: 48_000,
        ProjectionStartYear: 2026);

    [Fact]
    public void FutureValue_MatchesWebFormula()
    {
        var result = FinancialCalculator.FutureValue(100_000, 24_000, 0.07, 5);

        Assert.Equal(278_272.90931, result, 5);
    }

    [Fact]
    public void YearsToTarget_WithZeroReturnAndNoContribution_IsUnreachable()
    {
        var result = FinancialCalculator.YearsToTarget(10_000, 0, 0, 100_000);

        Assert.True(double.IsPositiveInfinity(result));
    }

    [Fact]
    public void YearsToTarget_WhenAlreadyFunded_IsZero()
    {
        Assert.Equal(0, FinancialCalculator.YearsToTarget(100_000, 0, 0.07, 100_000));
    }

    [Fact]
    public void StandardFire_MatchesWebDefaults()
    {
        var result = FinancialCalculator.CalculateStandardFire(DefaultInputs);

        Assert.Equal(1_200_000, result.FireNumber);
        Assert.Equal(24.4, result.YearsToFire);
        Assert.Equal(54.4, result.FireAge);
        Assert.Equal(462_932, result.CoastFireNumber);
        Assert.Equal(1d / 3d, result.SavingsRate, 10);
        Assert.Equal(2_000, result.MonthlyContribution);
        Assert.Equal(36, result.Projections.Count);
        Assert.Equal(new ProjectionPoint(30, 2026, 100_000, 100_000, 100_000, 100_000), result.Projections[0]);
    }

    [Fact]
    public void CoastFire_MatchesWebDefaults()
    {
        var result = FinancialCalculator.CalculateCoastFire(DefaultInputs);

        Assert.Equal(462_932, result.CoastNumber);
        Assert.Equal(10.7, result.YearsToCoast);
        Assert.False(result.AlreadyCoasting);
        Assert.Equal(1_200_000, result.FireNumber);
        Assert.Equal(36, result.Projections.Count);
        Assert.Equal(36, result.ProjectionsWithContributions.Count);
    }

    [Theory]
    [InlineData(40_000, true, false)]
    [InlineData(100_000, false, true)]
    public void LeanAndFatThresholds_MatchWebRules(double annualExpenses, bool expectedLean, bool expectedFat)
    {
        var inputs = DefaultInputs with { AnnualExpenses = annualExpenses };

        Assert.Equal(expectedLean, FinancialCalculator.CalculateLeanFire(inputs).IsLean);
        Assert.Equal(expectedFat, FinancialCalculator.CalculateFatFire(inputs).IsFat);
    }

    [Fact]
    public void BaristaFire_ReducesRequiredPortfolioByPartTimeIncome()
    {
        var result = FinancialCalculator.CalculateBaristaFire(DefaultInputs, 20_000);

        Assert.Equal(700_000, result.BaristaNumber);
        Assert.Equal(1_200_000, result.FullFireNumber);
        Assert.Equal(500_000, result.SavingsFromPartTime);
    }

    [Fact]
    public void BaristaFire_WhenPartTimeIncomeCoversExpenses_HasZeroTarget()
    {
        var result = FinancialCalculator.CalculateBaristaFire(DefaultInputs, DefaultInputs.AnnualExpenses);

        Assert.Equal(0, result.BaristaNumber);
        Assert.Equal(0, result.YearsToBaristaFire);
    }

    [Fact]
    public void BaristaFireDraft_DefaultMatchesWebPartTimeIncome()
    {
        var draft = BaristaFireDraft.Default;
        var result = FinancialCalculator.CalculateBaristaFire(draft.ToFireInputs(2026), draft.PartTimeAnnualIncome);

        Assert.Equal(20_000, draft.PartTimeAnnualIncome);
        Assert.Equal(700_000, result.BaristaNumber);
        Assert.Equal(1_200_000, result.FullFireNumber);
    }

    [Fact]
    public void Withdrawal_MatchesWebInflationAdjustedDrawdown()
    {
        var result = FinancialCalculator.CalculateWithdrawal(1_000_000, 0.04, 0.07, 0.03, 30);

        Assert.Equal(30, result.PortfolioLongevity);
        Assert.Equal(1, result.SuccessRate);
        Assert.Equal(40_000, result.AnnualWithdrawal);
        Assert.Equal(2_427_262, result.EndingBalance);
        Assert.Equal(new WithdrawalProjection(30, 2_427_262, 97_090), result.WithdrawalProjections[^1]);
    }

    [Fact]
    public void SnowballPayoff_AppliesMinimumThenRemainingBudget()
    {
        var debt = new DebtItem("card", "Credit card", 1_000, 0, 100);

        var result = FinancialCalculator.CalculateSnowballPayoff([debt], 500);

        Assert.Equal(2, result.TotalMonths);
        Assert.Equal(0, result.TotalInterest);
        Assert.Equal(["Credit card"], result.PayoffOrder);
        Assert.Equal(0, result.Projections[^1].TotalBalance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void DebtTimeline_WithInvalidTarget_ReturnsNoResult(int targetMonths)
    {
        var debt = new DebtItem("card", "Credit card", 1_000, 0.20, 100);

        Assert.Null(FinancialCalculator.CalculateDebtPayoffByTimeline([debt], targetMonths, useSnowball: true));
    }

    [Fact]
    public void ReverseFire_MatchesWebDefaultRequirement()
    {
        var result = FinancialCalculator.CalculateReverseFire(DefaultInputs);

        Assert.Equal(1_200_000, result.FireNumber);
        Assert.Equal(25, result.YearsToFire);
        Assert.Equal(22_946.80027, result.RequiredAnnualSavings, 5);
        Assert.Equal(1_912.23336, result.RequiredMonthlySavings, 5);
        Assert.False(result.AlreadyAchievable);
        Assert.Equal(259_217, result.CurrentWillGrowTo);
    }

    [Fact]
    public void InvestmentGrowth_MatchesWebAnnualContributionModel()
    {
        var result = FinancialCalculator.CalculateInvestmentGrowth(new InvestmentGrowthInputs(
            StartingAmount: 100_000,
            ContributionAmount: 500,
            ContributionFrequency: ContributionFrequency.Monthly,
            YearsInvesting: 30,
            ExpectedReturn: 0.07,
            InflationRate: 0.03,
            AnnualIncome: 75_000,
            ProjectionStartYear: 2026));

        Assert.Equal(0.08, result.SavingsRate, 10);
        Assert.Equal(6_000, result.AnnualContribution);
        Assert.Equal(1_327_990.22221, result.FinalNominalBalance, 5);
        Assert.Equal("Below Average", result.SavingsCategory);
        Assert.Equal(31, result.Projections.Count);
        Assert.Equal(new InvestmentProjectionPoint(30, 2026, 0, 100_000, 100_000, 100_000, 0), result.Projections[0]);
    }

    [Fact]
    public void HealthcareGap_MatchesWebInflationAndSubsidyRules()
    {
        var result = FinancialCalculator.CalculateHealthcareGap(new HealthcareGapInputs(
            CurrentAge: 30,
            EarlyRetirementAge: 55,
            MedicareAge: 65,
            MonthlyPremium: 600,
            AnnualDeductible: 2_500,
            AnnualOutOfPocket: 2_000,
            InflationRate: 0.03,
            ProjectionStartYear: 2026));

        Assert.Equal(10, result.GapYears);
        Assert.Equal(11_700, result.AnnualCost);
        Assert.Equal(134_127, result.TotalCost);
        Assert.Equal(13_413, result.AverageAnnualCost);
        Assert.Equal(5_850, result.EstimatedSubsidy30k);
        Assert.Equal(3_510, result.EstimatedSubsidy50k);
        Assert.Equal(1_755, result.EstimatedSubsidy75k);
        Assert.Equal(new HealthcareYear(64, 2060, 15_266, 9_394, 3_262, 2_610), result.YearlyBreakdown[^1]);
    }

    [Fact]
    public void HealthcareGap_WhenRetirementStartsAtMedicare_HasNoGap()
    {
        var result = FinancialCalculator.CalculateHealthcareGap(new HealthcareGapInputs(
            CurrentAge: 64,
            EarlyRetirementAge: 65,
            MedicareAge: 65,
            MonthlyPremium: 600,
            AnnualDeductible: 2_500,
            AnnualOutOfPocket: 2_000,
            InflationRate: 0.03,
            ProjectionStartYear: 2026));

        Assert.Equal(0, result.GapYears);
        Assert.Equal(0, result.TotalCost);
        Assert.Empty(result.YearlyBreakdown);
    }
}