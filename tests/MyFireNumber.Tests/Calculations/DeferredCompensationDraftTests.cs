using MyFireNumber.Core.Calculations;

namespace MyFireNumber.Tests.Calculations;

public sealed class DeferredCompensationDraftTests
{
    [Fact]
    public void Default_MapsWebScenarioToCoreInputs()
    {
        var draft = DeferredCompensationDraft.Default;
        var inputs = draft.ToInputs(2026);

        Assert.Equal(45, inputs.CurrentAge);
        Assert.Equal(55, inputs.SemiRetirementAge);
        Assert.Equal(90, inputs.PlanThroughAge);
        Assert.Equal(80_000, inputs.AnnualExpenses);
        Assert.Equal(0.03, inputs.InflationRate);
        Assert.Equal(2, inputs.Accounts.Count);
        Assert.Equal("Deferred Compensation", inputs.Accounts[0].Name);
        Assert.Equal(RetirementAccountType.Deferred, inputs.Accounts[0].Type);
        Assert.Single(inputs.IncomeSources);
        Assert.True(inputs.WithdrawOnlyAfterRetirement);
        Assert.True(inputs.ReinvestSurplus);
    }
}