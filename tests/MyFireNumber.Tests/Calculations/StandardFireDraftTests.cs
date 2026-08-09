using MyFireNumber.Core.Calculations;

namespace MyFireNumber.Tests.Calculations;

public sealed class StandardFireDraftTests
{
    [Fact]
    public void Default_ToFireInputsMatchesWebDefaults()
    {
        var inputs = StandardFireDraft.Default.ToFireInputs(2026);

        Assert.Equal(30, inputs.CurrentAge);
        Assert.Equal(55, inputs.RetirementAge);
        Assert.Equal(100_000, inputs.CurrentSavings);
        Assert.Equal(24_000, inputs.AnnualContribution);
        Assert.Equal(72_000, inputs.AnnualIncome);
        Assert.Equal(0.07, inputs.ExpectedReturn);
        Assert.Equal(0.03, inputs.InflationRate);
        Assert.Equal(0.04, inputs.WithdrawalRate);
        Assert.Equal(48_000, inputs.AnnualExpenses);
        Assert.Equal(2026, inputs.ProjectionStartYear);
    }

    [Fact]
    public void CoastDefault_ToFireInputsMatchesWebDefaults()
    {
        var inputs = CoastFireDraft.Default.ToFireInputs(2026);

        Assert.Equal(30, inputs.CurrentAge);
        Assert.Equal(55, inputs.RetirementAge);
        Assert.Equal(100_000, inputs.CurrentSavings);
        Assert.Equal(24_000, inputs.AnnualContribution);
        Assert.Equal(0, inputs.AnnualIncome);
        Assert.Equal(0.07, inputs.ExpectedReturn);
        Assert.Equal(0.03, inputs.InflationRate);
        Assert.Equal(0.04, inputs.WithdrawalRate);
        Assert.Equal(48_000, inputs.AnnualExpenses);
        Assert.Equal(2026, inputs.ProjectionStartYear);
    }

    [Fact]
    public void LeanDraft_PreservesEnteredExpensesAndCapsCalculationAtLeanThreshold()
    {
        var draft = LeanFireDraft.Default;
        var inputs = draft.ToFireInputs(2026);

        Assert.Equal(48_000, draft.AnnualExpenses);
        Assert.False(draft.IsWithinLeanThreshold);
        Assert.Equal(FinancialCalculator.LeanFireThreshold, inputs.AnnualExpenses);
        Assert.Equal(72_000, inputs.AnnualIncome);
        Assert.Equal(2026, inputs.ProjectionStartYear);
    }

    [Fact]
    public void FatDraft_PreservesEnteredExpensesWithoutCappingCalculation()
    {
        var draft = FatFireDraft.Default with { AnnualExpenses = 125_000 };
        var inputs = draft.ToFireInputs(2026);

        Assert.True(draft.IsWithinFatThreshold);
        Assert.Equal(125_000, inputs.AnnualExpenses);
        Assert.Equal(72_000, inputs.AnnualIncome);
        Assert.Equal(2026, inputs.ProjectionStartYear);
    }

    [Fact]
    public void ReverseDraft_MapsWebDefaultsToTargetRetirementCalculation()
    {
        var inputs = ReverseFireDraft.Default.ToFireInputs(2026);

        Assert.Equal(30, inputs.CurrentAge);
        Assert.Equal(55, inputs.RetirementAge);
        Assert.Equal(100_000, inputs.CurrentSavings);
        Assert.Equal(0, inputs.AnnualContribution);
        Assert.Equal(48_000, inputs.AnnualExpenses);
        Assert.Equal(2026, inputs.ProjectionStartYear);
    }

    [Fact]
    public void WithdrawalRateDraft_MatchesWebDefaults()
    {
        var draft = WithdrawalRateDraft.Default;

        Assert.Equal(1_000_000, draft.PortfolioValue);
        Assert.Equal(0.04, draft.WithdrawalRate);
        Assert.Equal(0.07, draft.ExpectedReturn);
        Assert.Equal(0.03, draft.InflationRate);
        Assert.Equal(30, draft.RetirementYears);
    }

    [Fact]
    public void SavingsInvestmentDraft_MapsMonthlyWebDefaultsToGrowthInputs()
    {
        var inputs = SavingsInvestmentDraft.Default.ToInputs(2026);

        Assert.Equal(100_000, inputs.StartingAmount);
        Assert.Equal(500, inputs.ContributionAmount);
        Assert.Equal(ContributionFrequency.Monthly, inputs.ContributionFrequency);
        Assert.Equal(30, inputs.YearsInvesting);
        Assert.Equal(75_000, inputs.AnnualIncome);
        Assert.Equal(30, inputs.CurrentAge);
        Assert.Equal(2026, inputs.ProjectionStartYear);
    }

    [Fact]
    public void HealthcareGapDraft_MapsWebDefaultsToGapInputs()
    {
        var inputs = HealthcareGapDraft.Default.ToInputs(2026);

        Assert.Equal(30, inputs.CurrentAge);
        Assert.Equal(55, inputs.EarlyRetirementAge);
        Assert.Equal(65, inputs.MedicareAge);
        Assert.Equal(600, inputs.MonthlyPremium);
        Assert.Equal(2_500, inputs.AnnualDeductible);
        Assert.Equal(2_000, inputs.AnnualOutOfPocket);
        Assert.Equal(0.03, inputs.InflationRate);
        Assert.Equal(2026, inputs.ProjectionStartYear);
    }

    [Fact]
    public void DebtPayoffDraft_MatchesWebDefaults()
    {
        var draft = DebtPayoffDraft.Default;

        Assert.Empty(draft.Debts);
        Assert.Equal(1_000, draft.MonthlyBudget);
        Assert.Equal(0, draft.ExtraPayment);
        Assert.Equal(36, draft.TargetMonths);
        Assert.Equal(DebtPayoffMode.FixedBudget, draft.Mode);
        Assert.Equal(DebtPayoffStrategy.Snowball, draft.Strategy);
    }

    [Theory]
    [InlineData("Conservative", 12_000, 80_000, 0.15)]
    [InlineData("Moderate", 24_000, 96_000, 0.25)]
    [InlineData("Aggressive", 48_000, 96_000, 0.50)]
    [InlineData("Fat FIRE", 100_000, 250_000, 0.40)]
    public void Presets_ContainCompleteDraftsWithTheStatedSavingsRate(
        string name,
        double annualContribution,
        double annualIncome,
        double expectedSavingsRate)
    {
        var preset = Assert.Single(StandardFirePreset.All, preset => preset.Name == name);

        Assert.Equal(annualContribution, preset.Draft.AnnualContribution);
        Assert.Equal(annualIncome, preset.Draft.AnnualIncome);
        Assert.Equal(expectedSavingsRate, preset.Draft.AnnualContribution / preset.Draft.AnnualIncome);
        Assert.InRange(preset.Draft.ExpectedReturn, 0, 0.20);
        Assert.InRange(preset.Draft.InflationRate, 0, 0.10);
        Assert.InRange(preset.Draft.WithdrawalRate, 0.02, 0.06);
    }
}