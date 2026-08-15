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
        Assert.Equal(55, result.RetirementGoal.TargetRetirementAge);
        Assert.Equal(-0.6, result.RetirementGoal.TargetAgeGap, 10);
        Assert.True(result.RetirementGoal.IsOnTrack);
        Assert.Contains("On track", result.RetirementGoal.Message);

        // Contributions are stated in today's dollars and escalate with inflation.
        Assert.Equal(24_720, result.Projections[1].Contributions, 5);
        AssertHeadlineMatchesCrossing(result.Projections, result.FireNumber, result.YearsToFire);
    }

    [Fact]
    public void StandardFire_WithFlatContributions_SolvesTheFlatNominalPath()
    {
        var result = FinancialCalculator.CalculateStandardFire(DefaultInputs with { ContributionGrowth = ContributionGrowth.Flat });

        Assert.Equal(1_200_000, result.FireNumber);
        Assert.Equal(29.2, result.YearsToFire);
        Assert.Equal(59.2, result.FireAge);

        // Flat mode never escalates the contribution.
        Assert.Equal(24_000, result.Projections[1].Contributions, 5);
        Assert.Equal(24_000, result.Projections[10].Contributions, 5);
        AssertHeadlineMatchesCrossing(result.Projections, result.FireNumber, result.YearsToFire);
    }

    [Theory]
    [InlineData(ContributionGrowth.Inflation)]
    [InlineData(ContributionGrowth.Flat)]
    public void CoastFire_HeadlineMatchesProjectionCrossing(ContributionGrowth growth)
    {
        var result = FinancialCalculator.CalculateCoastFire(DefaultInputs with { ContributionGrowth = growth });

        AssertHeadlineMatchesCrossing(result.ProjectionsWithContributions, result.CoastNumber, result.YearsToCoast);
    }

    [Theory]
    [InlineData(ContributionGrowth.Inflation)]
    [InlineData(ContributionGrowth.Flat)]
    public void BaristaFire_HeadlineMatchesProjectionCrossing(ContributionGrowth growth)
    {
        var result = FinancialCalculator.CalculateBaristaFire(DefaultInputs with { ContributionGrowth = growth }, partTimeAnnualIncome: 20_000);

        AssertHeadlineMatchesCrossing(result.Projections, result.BaristaNumber, result.YearsToBaristaFire);
    }

    [Fact]
    public void StandardFire_RetirementAgeChangesGoalAssessmentWithoutChangingFireMath()
    {
        var earlierTarget = FinancialCalculator.CalculateStandardFire(DefaultInputs with { RetirementAge = 54 });
        var laterTarget = FinancialCalculator.CalculateStandardFire(DefaultInputs with { RetirementAge = 56 });

        Assert.Equal(earlierTarget.FireNumber, laterTarget.FireNumber);
        Assert.Equal(earlierTarget.YearsToFire, laterTarget.YearsToFire);
        Assert.Equal(earlierTarget.FireAge, laterTarget.FireAge);

        Assert.Equal(0.4, earlierTarget.RetirementGoal.TargetAgeGap, 10);
        Assert.False(earlierTarget.RetirementGoal.IsOnTrack);
        Assert.Contains("Off track", earlierTarget.RetirementGoal.Message);

        Assert.Equal(-1.6, laterTarget.RetirementGoal.TargetAgeGap, 10);
        Assert.True(laterTarget.RetirementGoal.IsOnTrack);
        Assert.Contains("On track", laterTarget.RetirementGoal.Message);
    }

    [Fact]
    public void StandardFire_WhenUnreachable_AssessesTheRetirementGoalAsOffTrack()
    {
        var result = FinancialCalculator.CalculateStandardFire(DefaultInputs with
        {
            CurrentSavings = 0,
            AnnualContribution = 0,
            ExpectedReturn = 0,
            InflationRate = 0
        });

        Assert.True(double.IsPositiveInfinity(result.FireAge));
        Assert.True(double.IsPositiveInfinity(result.RetirementGoal.CalculatedFireAge));
        Assert.False(result.RetirementGoal.IsOnTrack);
        Assert.Equal("Off track: FIRE is not reachable with the current assumptions.", result.RetirementGoal.Message);
    }

    [Fact]
    public void StandardFireResult_SevenArgumentConstructorProvidesAnUnavailableGoalAssessment()
    {
        var result = new StandardFireResult(1, 2, 3, [], 4, 5, 6);

        Assert.False(result.RetirementGoal.IsOnTrack);
        Assert.Equal("Retirement goal assessment is unavailable.", result.RetirementGoal.Message);
    }

    [Fact]
    public void LeanAndFatFire_ExposeTheRetirementGoalAssessment()
    {
        var inputs = DefaultInputs with { RetirementAge = 54 };
        var lean = FinancialCalculator.CalculateLeanFire(inputs);
        var fat = FinancialCalculator.CalculateFatFire(inputs);

        Assert.Equal(lean.Standard.RetirementGoal, lean.RetirementGoal);
        Assert.Equal(fat.Standard.RetirementGoal, fat.RetirementGoal);
        Assert.False(lean.RetirementGoal.IsOnTrack);
        Assert.False(fat.RetirementGoal.IsOnTrack);
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
        Assert.Equal(1, result.HorizonFundedRatio);
        Assert.Equal(40_000, result.AnnualWithdrawal);
        Assert.Equal(2_427_262, result.EndingBalance);
        Assert.Equal(new WithdrawalProjection(30, 2_427_262, 97_090), result.WithdrawalProjections[^1]);
    }

    [Fact]
    public void Withdrawal_RateAnalysisUsesSameYearConventionAsHeadline()
    {
        const double portfolioValue = 1_000_000;
        const double rate = 0.05;

        // A 5% start rate on a 2% real return depletes the portfolio, so both the headline and the
        // comparison row report a partial horizon and must land on the same year.
        var headline = FinancialCalculator.CalculateWithdrawal(portfolioValue, rate, 0.05, 0.03, 60);
        var comparison = headline.RateAnalysis.Single(analysis => analysis.Rate == rate);

        Assert.True(headline.PortfolioLongevity < 60);
        Assert.Equal(headline.PortfolioLongevity, comparison.Years);
    }

    [Fact]
    public void Withdrawal_WhenPortfolioIsEmpty_ReportsZeroYearsFunded()
    {
        var result = FinancialCalculator.CalculateWithdrawal(0, 0.04, 0.07, 0.03, 30);

        Assert.Equal(0, result.PortfolioLongevity);
        Assert.Equal(0, result.HorizonFundedRatio);
        Assert.All(result.RateAnalysis, analysis => Assert.Equal(0, analysis.Years));
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
    public void SnowballPayoff_WithInterest_ChargesInterestOncePerMonth()
    {
        var debt = new DebtItem("card", "Credit card", 10_000, 0.20, 250);

        var result = FinancialCalculator.CalculateSnowballPayoff([debt], 500);

        Assert.Equal(25, result.TotalMonths);
        Assert.Equal(2_266, result.TotalInterest);
        Assert.Equal(10_000, result.TotalPrincipal);

        // $10,000 at 20% APR accrues $166.67 in month one, not $333.33.
        var firstMonth = result.Projections[0];
        Assert.Equal(167, firstMonth.InterestPaid);
        Assert.Equal(333, firstMonth.PrincipalPaid);
        Assert.Equal(9_667, firstMonth.TotalBalance);

        Assert.Equal(0, result.Projections[^1].TotalBalance);
        Assert.Equal(10_000, result.Projections[^1].CumulativePrincipal);
    }

    [Fact]
    public void AvalanchePayoff_NeverCostsMoreInterestThanSnowball()
    {
        DebtItem[] debts =
        [
            new("small", "Small balance", 2_000, 0.06, 50),
            new("big", "High rate", 8_000, 0.22, 200)
        ];

        var snowball = FinancialCalculator.CalculateSnowballPayoff(debts, 600);
        var avalanche = FinancialCalculator.CalculateAvalanchePayoff(debts, 600);

        Assert.True(
            avalanche.TotalInterest <= snowball.TotalInterest,
            $"Avalanche interest {avalanche.TotalInterest} should not exceed snowball interest {snowball.TotalInterest}.");

        Assert.Equal(20, snowball.TotalMonths);
        Assert.Equal(1_930, snowball.TotalInterest);
        Assert.Equal(["Small balance", "High rate"], snowball.PayoffOrder);

        Assert.Equal(20, avalanche.TotalMonths);
        Assert.Equal(1_543, avalanche.TotalInterest);
        Assert.Equal(["High rate", "Small balance"], avalanche.PayoffOrder);
    }

    [Fact]
    public void DebtPayoff_WithBudgetBelowMinimums_DoesNotSpendMoreThanBudget()
    {
        DebtItem[] debts =
        [
            new("small", "Small balance", 2_000, 0.06, 50),
            new("big", "High rate", 8_000, 0.22, 200)
        ];

        var result = FinancialCalculator.CalculateSnowballPayoff(debts, 100);

        foreach (var month in result.Projections)
        {
            Assert.True(
                month.PrincipalPaid + month.InterestPaid <= 100,
                $"Month {month.Month} spent {month.PrincipalPaid + month.InterestPaid} against a $100 budget.");
        }

        // A budget that cannot cover minimums never retires the debt instead of silently overpaying.
        Assert.Equal(600, result.TotalMonths);
        Assert.DoesNotContain("High rate", result.PayoffOrder);
        Assert.True(result.Projections[^1].TotalBalance > 8_000);
    }

    [Fact]
    public void DebtTimeline_UsesCorrectedInterestWhenSolvingForPayment()
    {
        var debt = new DebtItem("card", "Credit card", 10_000, 0.20, 250);

        var timeline = FinancialCalculator.CalculateDebtPayoffByTimeline([debt], 24, useSnowball: true);

        Assert.NotNull(timeline);
        Assert.Equal(509, timeline.RequiredPayment);
        Assert.True(timeline.Result.TotalMonths <= 24);
        Assert.Equal(2_212, timeline.Result.TotalInterest);
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

        // The whole job of this calculator: the recommended savings must actually land on the
        // FIRE number in today's dollars at the retirement age it was solved for.
        Assert.Equal(1_200_000, result.Projections[25].InflationAdjusted, 0);
    }

    [Fact]
    public void ReverseFire_WithFlatContributions_StillLandsOnTarget()
    {
        var result = FinancialCalculator.CalculateReverseFire(DefaultInputs with { ContributionGrowth = ContributionGrowth.Flat });

        Assert.Equal(1_200_000, result.FireNumber);
        Assert.Equal(25, result.YearsToFire);

        // Flat contributions must be larger, because the later ones buy less.
        Assert.Equal(31_143.40269, result.RequiredAnnualSavings, 5);
        Assert.Equal(2_595.28356, result.RequiredMonthlySavings, 5);
        Assert.True(result.RequiredAnnualSavings > 22_946.80027);
        Assert.Equal(1_200_000, result.Projections[25].InflationAdjusted, 0);
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

        // Contributions are stated in today's dollars and escalate with inflation, so the
        // nominal balance is higher than the old flat-contribution model produced.
        Assert.Equal(1_562_306.85656, result.FinalNominalBalance, 5);

        // Previously unasserted, which is why the real/nominal mix in issue #47 shipped.
        // This is now purely the nominal balance deflated by inflation.
        Assert.Equal(643_649.73920, result.FinalInflationAdjustedBalance, 5);
        Assert.Equal(
            result.FinalNominalBalance / Math.Pow(1.03, 30),
            result.FinalInflationAdjustedBalance,
            8);
        Assert.Equal(result.FinalNominalBalance - result.FinalInflationAdjustedBalance, result.InflationImpact, 8);
        Assert.Equal(394_016.06907, result.TotalInvested, 5);

        Assert.Equal("Below Average", result.SavingsCategory);
        Assert.Equal(31, result.Projections.Count);
        Assert.Equal(new InvestmentProjectionPoint(30, 2026, 0, 100_000, 100_000, 100_000, 0), result.Projections[0]);
        Assert.Equal(6_180, result.Projections[1].Contributions, 5);
    }

    [Fact]
    public void InvestmentGrowth_WithFlatContributions_DeflatesTheFlatNominalPath()
    {
        var result = FinancialCalculator.CalculateInvestmentGrowth(new InvestmentGrowthInputs(
            StartingAmount: 100_000,
            ContributionAmount: 500,
            ContributionFrequency: ContributionFrequency.Monthly,
            YearsInvesting: 30,
            ExpectedReturn: 0.07,
            InflationRate: 0.03,
            AnnualIncome: 75_000,
            ProjectionStartYear: 2026,
            ContributionGrowth: ContributionGrowth.Flat));

        // The nominal balance is the value this calculator shipped with.
        Assert.Equal(1_327_990.22221, result.FinalNominalBalance, 5);

        // The old code reported 643,649.74 here by growing a second balance at the real rate
        // while feeding it undiscounted contributions. Deflating the flat path gives this.
        Assert.Equal(547_114.38832, result.FinalInflationAdjustedBalance, 5);
        Assert.Equal(280_000, result.TotalInvested, 5);
        Assert.Equal(6_000, result.Projections[1].Contributions, 5);
        Assert.Equal(6_000, result.Projections[30].Contributions, 5);
    }

    /// <summary>
    /// Web and mobile must produce identical numbers. These values were generated from
    /// <c>web/src/utils/calculations.ts</c> for a scenario that shares no defaults with the
    /// other tests, so a drift in either implementation shows up here.
    /// </summary>
    [Theory]
    [InlineData(ContributionGrowth.Inflation, 35.1, 77.1, 1_396_066d, 895_110d, 28_073.85692, 63_449.60981, 1_396_066.33193, 895_110.13921, 663_028.13367)]
    [InlineData(ContributionGrowth.Flat, 40.1, 82.1, 1_269_887d, 814_208d, 18_000d, 77_841.18596, 1_269_886.53410, 814_207.95440, 574_000d)]
    public void WebParity_NonDefaultScenario(
        ContributionGrowth growth,
        double expectedYearsToFire,
        double expectedFireAge,
        double expectedYear18Portfolio,
        double expectedYear18InflationAdjusted,
        double expectedYear18Contribution,
        double expectedReverseRequirement,
        double expectedGrowthNominal,
        double expectedGrowthInflationAdjusted,
        double expectedGrowthInvested)
    {
        var inputs = new FireInputs(
            CurrentAge: 42,
            RetirementAge: 60,
            CurrentSavings: 250_000,
            AnnualContribution: 18_000,
            AnnualIncome: 95_000,
            ExpectedReturn: 0.06,
            InflationRate: 0.025,
            WithdrawalRate: 0.035,
            AnnualExpenses: 70_000,
            ProjectionStartYear: 2026,
            ContributionGrowth: growth);

        var standard = FinancialCalculator.CalculateStandardFire(inputs);
        Assert.Equal(2_000_000, standard.FireNumber);
        Assert.Equal(1_092_833, standard.CoastFireNumber);
        Assert.Equal(expectedYearsToFire, standard.YearsToFire);
        Assert.Equal(expectedFireAge, standard.FireAge);
        Assert.Equal(expectedYear18Portfolio, standard.Projections[18].Portfolio);
        Assert.Equal(expectedYear18InflationAdjusted, standard.Projections[18].InflationAdjusted);
        Assert.Equal(expectedYear18Contribution, standard.Projections[18].Contributions, 5);
        AssertHeadlineMatchesCrossing(standard.Projections, standard.FireNumber, standard.YearsToFire);

        var reverse = FinancialCalculator.CalculateReverseFire(inputs);
        Assert.Equal(expectedReverseRequirement, reverse.RequiredAnnualSavings, 5);
        Assert.Equal(457_526, reverse.CurrentWillGrowTo);

        var growthResult = FinancialCalculator.CalculateInvestmentGrowth(new InvestmentGrowthInputs(
            StartingAmount: 250_000,
            ContributionAmount: 1_500,
            ContributionFrequency: ContributionFrequency.Monthly,
            YearsInvesting: 18,
            ExpectedReturn: 0.06,
            InflationRate: 0.025,
            AnnualIncome: 95_000,
            ProjectionStartYear: 2026,
            CurrentAge: 42,
            ContributionGrowth: growth));

        Assert.Equal(expectedGrowthNominal, growthResult.FinalNominalBalance, 5);
        Assert.Equal(expectedGrowthInflationAdjusted, growthResult.FinalInflationAdjustedBalance, 5);
        Assert.Equal(expectedGrowthInvested, growthResult.TotalInvested, 5);
    }

    /// <summary>
    /// The invariant behind issues #46 and #47: the headline "you reach it in N years" number must
    /// be the same year at which the drawn projection crosses the target in today's dollars.
    /// </summary>
    private static void AssertHeadlineMatchesCrossing(IReadOnlyList<ProjectionPoint> projections, double target, double headlineYears)
    {
        Assert.True(double.IsFinite(headlineYears), "Headline years must be finite for this assertion.");

        var before = (int)Math.Floor(headlineYears);
        var after = (int)Math.Ceiling(headlineYears);
        Assert.True(after < projections.Count, $"Projection is too short to contain the crossing at year {headlineYears}.");

        if (before == after)
        {
            Assert.True(projections[after].InflationAdjusted >= target - 1);
            return;
        }

        Assert.True(
            projections[before].InflationAdjusted < target,
            $"Year {before} already reached the target ({projections[before].InflationAdjusted} >= {target}), so the headline is late.");
        Assert.True(
            projections[after].InflationAdjusted >= target,
            $"Year {after} has not reached the target ({projections[after].InflationAdjusted} < {target}), so the headline is early.");
    }

    [Fact]
    public void HealthcareGap_MatchesWebInflationRules()
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