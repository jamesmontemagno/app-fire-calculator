using MyFireNumber.Core.Calculations;

namespace MyFireNumber.Tests.Calculations;

/// <summary>
/// Cross-platform agreement on the sign and rounding of <c>Surplus</c> — the fix for issue #63.
///
/// <para><b>What was wrong.</b> <c>Surplus</c> is the only signed field on a projection point; every
/// other value goes through a helper that clamps at zero first. The web mirror rounded it with bare
/// <c>Math.round</c>, which rounds half toward +Infinity, while this side used
/// <c>MidpointRounding.AwayFromZero</c>. The two agree on positives and away from midpoints and
/// disagree at an exact negative half-integer:</para>
/// <code>
/// JS  Math.round(-2.5)                              === -2
/// C#  Math.Round(-2.5, MidpointRounding.AwayFromZero) == -3
/// </code>
///
/// <para><b>Why it was severe.</b> Both platforms then classified shortfall from that ROUNDED value.
/// <c>Math.round(-0.5)</c> is <c>-0</c> and <c>-0 &lt; 0</c> is <c>false</c>, so web reported a
/// fifty-cent shortfall as a fully funded plan that never falls short, while this side reported
/// failure at the first retirement age. Same inputs, opposite answers to "does my plan work".</para>
///
/// <para><b>What changed.</b> Web now rounds signed money away from zero, matching this side, so the
/// assertions below are unchanged from when they pinned the divergence — the web values moved to meet
/// them. Separately, the verdict on both platforms now reads the unrounded surplus through an explicit
/// half-dollar tolerance, so no display rounding rule can move a headline boolean again.</para>
///
/// <para>These inputs drive the raw surplus to exact midpoints. Both operands are exactly
/// representable in binary64, so the values below are deterministic, not floating-point noise. The
/// same scenarios are pinned as shared agreement in <c>shared/parity/fire-parity-cases.json</c>
/// (the <c>deferred-*</c> cases) and mirrored in
/// <c>web/src/utils/__tests__/deferredCompensation.test.ts</c>.</para>
/// </summary>
public class SurplusRoundingParityTests
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
    public void NegativeMidpoint_RoundsAwayFromZero_AndWebNowAgrees()
    {
        // 10,000 - 10,002.50 = -2.50 exactly. Web reported -2 before #63 and reports -3 now.
        var result = FlatShortfall(10_002.5);

        Assert.Equal(-3, result.Projections[0].Surplus);
        Assert.Equal(-3, result.FirstYearSurplus);
    }

    [Fact]
    public void HalfDollarShortfall_ReportsAShortfall_OnBothPlatforms()
    {
        /*
         * This was the consequential half. Before #63 the same inputs produced:
         *
         *                        web       MAUI
         *   surplus               -0         -1
         *   fundedYears            3          0
         *   firstShortfallAge   null         60
         *   yearsFullyCovered      3          0
         *
         * The MAUI column is the behaviour that survived, so these assertions are unchanged; web now
         * produces the same four values. -0.50 <= -0.50, so every year of this plan is short and none
         * of them counts as covered.
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
        // Math.round(2.5) === 3 and MidpointRounding.AwayFromZero also gives 3. The modes differed
        // only on negatives, which is why nothing caught this until the audit.
        var result = FlatShortfall(9_997.5);

        Assert.Equal(3, result.Projections[0].Surplus);
        Assert.Equal(3, result.FundedYears);
        Assert.Null(result.FirstShortfallAge);
        Assert.Equal(3, result.YearsFullyCovered);
    }

    [Fact]
    public void AwayFromMidpoint_AgreesWithWeb()
    {
        // -2.4 rounds to -2 under both modes; the divergence needed an exact half.
        var result = FlatShortfall(10_002.4);

        Assert.Equal(-2, result.Projections[0].Surplus);
    }

    [Fact]
    public void SubHalfDollarShortfall_IsNotNegativeZero_AndCountsAsFunded()
    {
        /*
         * The third symptom of #63, and the one that was misattributed to web. A raw surplus of -0.40
         * rounds to NEGATIVE zero under AwayFromZero. `-0.0 < 0` is false, so the year was already
         * classified as funded — but `(-0.0).ToString("C0")` renders "-$0", and
         * RetirementCashFlowViewModel formats point.Surplus directly, so the annual detail row showed
         * a negative surplus for a year the headline called funded. Web's table renders
         * `Math.abs(surplus)` behind a `>= 0` test, so it showed a green "+$0" for the same inputs —
         * an undocumented display divergence in its own right.
         *
         * RoundSigned now normalises negative zero away, so both platforms display "$0" here.
         */
        var result = FlatShortfall(10_000.4);

        Assert.Equal(0, result.Projections[0].Surplus);
        Assert.False(double.IsNegative(result.Projections[0].Surplus));
        Assert.False(double.IsNegative(result.FirstYearSurplus));

        // -0.40 > -0.50, so this is inside the tolerance and the plan still reads as funded. That is
        // the documented cost of a whole-dollar calculator: a sub-fifty-cent gap is not a shortfall.
        Assert.Equal(3, result.FundedYears);
        Assert.Null(result.FirstShortfallAge);
    }
}
