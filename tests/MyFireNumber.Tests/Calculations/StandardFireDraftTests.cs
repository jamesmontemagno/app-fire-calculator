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
}