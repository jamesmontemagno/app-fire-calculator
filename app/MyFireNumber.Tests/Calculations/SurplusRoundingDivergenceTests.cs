using MyFireNumber.Core.Calculations;

namespace MyFireNumber.Tests.Calculations;

/// <summary>
/// KNOWN CROSS-PLATFORM DIVERGENCE — tracked in issue #63. Not fixed here; this PR is test-only.
///
/// <para>The web engine rounds the deferred-compensation <c>surplus</c> with bare
/// <c>Math.round</c> (deferredCompensation.ts:284), while this side uses
/// <c>Math.Round(value, MidpointRounding.AwayFromZero)</c>. <c>surplus</c> is the only signed field —
/// every other field goes through a helper that clamps at zero first, so no other value is
/// exposed.</para>
///
/// <para>The two modes agree on positives and away from midpoints, and disagree at an exact negative
/// half-integer:</para>
/// <code>
/// JS  Math.round(-2.5)                              === -2
/// C#  Math.Round(-2.5, MidpointRounding.AwayFromZero) == -3
/// </code>
///
/// <para>The inputs below drive the raw surplus to exactly -2.50 and exactly -0.50. Both operands
/// are exactly representable in binary64, so these are deterministic midpoints rather than
/// floating-point noise.</para>
///
/// <para>These assert the CURRENT C# VALUES, and the matching web pins live in
/// <c>web/src/utils/__tests__/deferredCompensation.test.ts</c>. They are deliberately NOT in
/// <c>shared/parity/fire-parity-cases.json</c>: that fixture asserts agreement between the two
/// platforms, and there is none to assert here. Adding a case would have forced a choice of which
/// platform is correct, which is a behaviour change and belongs in the fix for #63. When that fix
/// lands, these tests SHOULD fail — that is the signal.</para>
/// </summary>
public class SurplusRoundingDivergenceTests
{
    /// <summary>
    /// A flat, after-tax $10,000 pension with zero inflation and no accounts, so the surplus is
    /// exactly <c>10,000 - annualExpenses</c> with no compounding in the way.
    /// </summary>
    private static DeferredCompensationResult FlatShortfall(double annualExpenses) =>
        DeferredCompensationCalculator.Calculate(new DeferredCompensationInputs(
            CurrentAge: 60,
            SemiRetirementAge: 60,
            PlanThroughAge: 62,
            AnnualExpenses: annualExpenses,
            InflationRate: 0,
            Accounts: [],
            IncomeSources: [new RetirementIncomeSource("pension", "Pension", 10_000, 0, 200, 0, true, 0)],
            AdditionalExpenses: [],
            WithdrawOnlyAfterRetirement: false,
            ReinvestSurplus: false,
            CurrentYear: 2025));

    [Fact]
    public void NegativeMidpoint_RoundsAwayFromZero_WhereWebRoundsTowardIt()
    {
        // 10,000 - 10,002.50 = -2.50 exactly. The web engine reports -2 for this same input.
        var result = FlatShortfall(10_002.5);

        Assert.Equal(-3, result.Projections[0].Surplus);
        Assert.Equal(-3, result.FirstYearSurplus);
    }

    [Fact]
    public void HalfDollarShortfall_ReportsAShortfall_WhereWebReportsAFullyFundedPlan()
    {
        /*
         * This is the consequential half. The shortfall predicate reads the ROUNDED surplus
         * (DeferredCompensationCalculator.cs:155), so the rounding mode decides a categorical
         * outcome rather than a display cent.
         *
         * This side rounds -0.50 to -1, which is < 0, so the year counts as a shortfall. The web
         * engine gets Math.round(-0.5) === -0, and -0 < 0 is false, so it counts the same year as
         * fully funded. Same inputs, opposite answers to "does my plan work":
         *
         *                        web       MAUI
         *   surplus               -0         -1
         *   fundedYears            3          0
         *   firstShortfallAge   null         60
         *   yearsFullyCovered      3          0
         */
        var result = FlatShortfall(10_000.5);

        Assert.Equal(-1, result.Projections[0].Surplus);
        Assert.Equal(0, result.FundedYears);
        Assert.Equal(60, result.FirstShortfallAge);
        Assert.Equal(0, result.YearsFullyCovered);
    }

    [Fact]
    public void PositiveMidpoint_AgreesWithWeb_WhichIsWhyThisWentUnnoticed()
    {
        // Math.round(2.5) === 3 and MidpointRounding.AwayFromZero also gives 3. The modes differ
        // only on negatives.
        var result = FlatShortfall(9_997.5);

        Assert.Equal(3, result.Projections[0].Surplus);
    }

    [Fact]
    public void AwayFromMidpoint_AgreesWithWeb()
    {
        // -2.4 rounds to -2 under both modes; the divergence needs an exact half.
        var result = FlatShortfall(10_002.4);

        Assert.Equal(-2, result.Projections[0].Surplus);
    }
}
