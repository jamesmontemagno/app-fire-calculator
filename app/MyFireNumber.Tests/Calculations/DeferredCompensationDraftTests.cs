using MyFireNumber.Core.Calculations;
using System.Text.Json;

namespace MyFireNumber.Tests.Calculations;

public sealed class DeferredCompensationDraftTests
{
    [Fact]
    public void Default_StartsWithSavingsAndTraditionalAccounts()
    {
        var draft = DeferredCompensationDraft.Default;
        var inputs = draft.ToInputs(2026);

        Assert.Equal(45, inputs.CurrentAge);
        Assert.Equal(55, inputs.SemiRetirementAge);
        Assert.Equal(90, inputs.PlanThroughAge);
        Assert.Equal(80_000, inputs.AnnualExpenses);
        Assert.Equal(0.03, inputs.InflationRate);
        Assert.Equal(2, inputs.Accounts.Count);
        Assert.Equal("Savings", inputs.Accounts[0].Name);
        Assert.Equal(RetirementAccountType.Savings, inputs.Accounts[0].Type);
        Assert.Equal("401(k)", inputs.Accounts[1].Name);
        Assert.Equal(RetirementAccountType.Traditional, inputs.Accounts[1].Type);
        Assert.Single(inputs.IncomeSources);
        Assert.True(inputs.WithdrawOnlyAfterRetirement);
        Assert.False(inputs.ReinvestSurplus);
    }

    [Fact]
    public void JsonRoundTrip_PreservesCustomCollections()
    {
        var draft = DeferredCompensationDraft.Default with
        {
            Accounts =
            [
                new RetirementAccount("hsa", "HSA", RetirementAccountType.Hsa, 50_000, 4_000, 0.06, 65, 0.04, 1)
            ],
            IncomeSources =
            [
                new RetirementIncomeSource("pension", "Pension", 25_000, 62, 90, 0.02, false, 0.15)
            ],
            AdditionalExpenses =
            [
                new RetirementExpense("travel", "Travel", 10_000, 60)
            ]
        };

        var restored = JsonSerializer.Deserialize<DeferredCompensationDraft>(JsonSerializer.Serialize(draft));

        Assert.NotNull(restored);
        Assert.Equal("HSA", Assert.Single(restored!.Accounts).Name);
        Assert.Equal("Pension", Assert.Single(restored.IncomeSources).Name);
        Assert.Equal("Travel", Assert.Single(restored.AdditionalExpenses).Name);
    }
}