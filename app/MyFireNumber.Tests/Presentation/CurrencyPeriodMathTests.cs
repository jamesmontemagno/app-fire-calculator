using System.Globalization;

using MyFireNumber.Core.Presentation;

namespace MyFireNumber.Tests.Presentation;

/// <summary>
/// Pins the arithmetic behind the monthly/annual display toggle against
/// <c>web/src/utils/currencyPeriod.ts</c>.
///
/// <para>The web module has no test file of its own, so these are the only tests either platform has
/// for this behaviour. If the two implementations ever disagree, web's suite cannot say which one is
/// right — which is a reason to be explicit here about what the expected values are derived from
/// rather than copied from.</para>
/// </summary>
public class CurrencyPeriodMathTests
{
    [Theory]
    [InlineData(50_000, CurrencyPeriod.Annual, CurrencyPeriod.Monthly, 50_000d / 12)]
    [InlineData(600, CurrencyPeriod.Monthly, CurrencyPeriod.Annual, 7_200)]
    [InlineData(1_234.56, CurrencyPeriod.Annual, CurrencyPeriod.Annual, 1_234.56)]
    [InlineData(1_234.56, CurrencyPeriod.Monthly, CurrencyPeriod.Monthly, 1_234.56)]
    [InlineData(0, CurrencyPeriod.Annual, CurrencyPeriod.Monthly, 0)]
    public void Convert_scales_by_twelve_between_periods(
        double value,
        CurrencyPeriod from,
        CurrencyPeriod to,
        double expected)
    {
        Assert.Equal(expected, CurrencyPeriodMath.Convert(value, from, to));
    }

    [Fact]
    public void Convert_between_equal_periods_is_bit_for_bit_identity()
    {
        // Called unconditionally by the display path, so "same period" must not introduce error of
        // its own on a value that cannot be represented exactly.
        const double awkward = 1 / 3d;

        Assert.Equal(awkward, CurrencyPeriodMath.Convert(awkward, CurrencyPeriod.Annual, CurrencyPeriod.Annual));
    }

    /// <summary>
    /// An enum does not stop an out-of-range value from existing in C#. <c>(CurrencyPeriod)99</c>
    /// compiles, and a <c>switch</c> with a <c>default</c> arm would silently treat it as annual.
    /// Every entry point throws instead.
    /// </summary>
    [Fact]
    public void Undefined_period_values_throw_rather_than_defaulting_to_annual()
    {
        const CurrencyPeriod undefinedPeriod = (CurrencyPeriod)99;

        Assert.Throws<ArgumentOutOfRangeException>(
            () => CurrencyPeriodMath.Convert(1, undefinedPeriod, CurrencyPeriod.Annual));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CurrencyPeriodMath.Convert(1, CurrencyPeriod.Annual, undefinedPeriod));
        Assert.Throws<ArgumentOutOfRangeException>(() => CurrencyPeriodMath.Suffix(undefinedPeriod));
        Assert.Throws<ArgumentOutOfRangeException>(() => CurrencyPeriodMath.Qualifier(undefinedPeriod));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PeriodicAmountField(undefinedPeriod));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PeriodicAmountField(CurrencyPeriod.Annual).SetDisplayPeriod(undefinedPeriod));
    }

    /// <summary>
    /// Midpoint rounding, pinned in <b>both</b> directions.
    ///
    /// <para>Expected values are JavaScript's <c>Math.round</c>, because that is what the web module
    /// rounds cents with. Neither C# default applies:</para>
    /// <code>
    /// value    expected (JS)    C# Math.Round    C# AwayFromZero
    ///   0.5          1                0                1
    ///   2.5          3                2                3
    ///   4.5          5                4                5
    ///  -0.5         -0               -0               -1
    ///  -1.5         -1               -2               -2
    /// </code>
    /// <para>The positive rows are the ones that matter most: C#'s default disagrees at every even
    /// boundary, and half a cent on a positive amount is exactly where currency lives. A table that
    /// only pinned negatives would still fail a naive default via -1.5, but it would leave the
    /// likeliest real divergence untested and imply the risk is a negative-number edge case. Issue
    /// #63 shipped this bug class in this repo already.</para>
    /// <para>The -0.5 row is written as <c>0</c> because <c>-0.0 == 0.0</c> makes the two
    /// indistinguishable to <c>Assert.Equal</c>; the sign is pinned separately in
    /// <see cref="RoundHalfUp_does_not_produce_negative_zero"/>.</para>
    /// </summary>
    [Theory]
    [InlineData(0.5, 1)]
    [InlineData(1.5, 2)]
    [InlineData(2.5, 3)]
    [InlineData(3.5, 4)]
    [InlineData(4.5, 5)]
    [InlineData(-0.5, 0)]
    [InlineData(-1.5, -1)]
    [InlineData(-2.5, -2)]
    [InlineData(0.4, 0)]
    [InlineData(0.6, 1)]
    [InlineData(-0.6, -1)]
    [InlineData(0.49999999999999994, 0)]
    public void RoundHalfUp_matches_javascript_Math_round(double value, double expected)
    {
        Assert.Equal(expected, CurrencyPeriodMath.RoundHalfUp(value));
    }

    /// <summary>
    /// The -0.5 row above cannot see the sign of its own result, so it is pinned separately.
    ///
    /// <para>JavaScript's <c>Math.round(-0.5)</c> is <c>-0</c>, not <c>0</c>. In C# — as in JS —
    /// <c>-0.0 == 0.0</c> is <c>true</c>, so <c>Assert.Equal(0, ...)</c> passes whichever zero comes
    /// back and proves nothing about the sign. That matters because a negative zero reaching
    /// <see cref="CurrencyPeriodMath.Format"/> would render as the string <c>"-0"</c> in an entry.
    /// </para>
    /// <para>This implementation returns <b>positive</b> zero: <c>Math.Floor(-0.5)</c> is
    /// <c>-1.0</c>, and IEEE-754 addition of exact opposites yields <c>+0.0</c> under
    /// round-to-nearest, so the <c>-0</c> path is never taken. So it is one step *safer* than the JS
    /// original here rather than a match, and this test pins that rather than the JS bit pattern —
    /// picking the behaviour that cannot surface "-0" in the UI.</para>
    /// <para>All three negative rows are defensive only. <c>RoundHalfUp</c> is reachable from the
    /// field solely through <c>IsSameToCent</c> &lt;- <c>ResolveEditedAmount</c> &lt;-
    /// <c>PeriodicAmountField.TrySetText</c>, which rejects <c>typed &lt; 0</c> before it converts,
    /// so no negative amount reaches this helper from a currency entry. They are pinned because the
    /// helper is public Core surface, not because a user can produce them.</para>
    /// </summary>
    [Fact]
    public void RoundHalfUp_does_not_produce_negative_zero()
    {
        var result = CurrencyPeriodMath.RoundHalfUp(-0.5);

        Assert.Equal(0, result);
        Assert.False(double.IsNegative(result), "-0.5 rounded to negative zero, which formats as \"-0\".");
        Assert.Equal("0", CurrencyPeriodMath.Format(result, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Pins the guard that makes the negative rows above unreachable in practice, so that if the
    /// guard is ever removed this suite says so rather than the negative rows quietly becoming live.
    /// </summary>
    [Theory]
    [InlineData("-1")]
    [InlineData("-0.5")]
    [InlineData("-50000")]
    public void A_negative_amount_never_reaches_the_rounding_helper(string typed)
    {
        var field = new PeriodicAmountField(
            CurrencyPeriod.Annual,
            CurrencyPeriod.Annual,
            CultureInfo.InvariantCulture);
        field.SetStoredValue(50_000);

        Assert.False(field.TrySetText(typed));
        Assert.Equal(50_000, field.StoredValue);
    }

    [Theory]
    [InlineData(4_166.67, 50_000d / 12, true)]
    [InlineData(4_166.67, 4_166.67, true)]
    [InlineData(4_166.67, 4_166.68, false)]
    [InlineData(0, 0.004, true)]
    [InlineData(0, 0.006, false)]
    public void IsSameToCent_compares_whole_cents(double a, double b, bool expected)
    {
        Assert.Equal(expected, CurrencyPeriodMath.IsSameToCent(a, b));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void IsSameToCent_rejects_non_finite_amounts(double value)
    {
        Assert.False(CurrencyPeriodMath.IsSameToCent(value, value));
        Assert.False(CurrencyPeriodMath.IsSameToCent(value, 1));
    }

    /// <summary>
    /// Re-entering the figure already on screen stores nothing new, so the rounding done for
    /// readability never leaks back into the value.
    /// </summary>
    [Fact]
    public void ResolveEditedAmount_treats_the_displayed_figure_as_no_edit()
    {
        // 50000 / 12 = 4166.666..., displayed as 4166.67. Converting that back gives 50000.04.
        var resolved = CurrencyPeriodMath.ResolveEditedAmount(
            typedDisplayAmount: 4_166.67,
            storedValue: 50_000,
            displayPeriod: CurrencyPeriod.Monthly,
            storedPeriod: CurrencyPeriod.Annual);

        Assert.Equal(50_000, resolved);
        // The drift the guard prevents: the displayed figure converted straight back is not 50000.
        Assert.NotEqual(50_000, 4_166.67 * 12);
        Assert.Equal(50_000.04, 4_166.67 * 12, 6);
    }

    [Fact]
    public void ResolveEditedAmount_converts_a_genuine_edit()
    {
        var resolved = CurrencyPeriodMath.ResolveEditedAmount(
            typedDisplayAmount: 5_000,
            storedValue: 50_000,
            displayPeriod: CurrencyPeriod.Monthly,
            storedPeriod: CurrencyPeriod.Annual);

        Assert.Equal(60_000, resolved);
    }

    [Fact]
    public void ResolveEditedAmount_converts_a_genuine_edit_on_a_monthly_stored_field()
    {
        // Healthcare premium: stored monthly, edited while displayed annually.
        var resolved = CurrencyPeriodMath.ResolveEditedAmount(
            typedDisplayAmount: 9_600,
            storedValue: 600,
            displayPeriod: CurrencyPeriod.Annual,
            storedPeriod: CurrencyPeriod.Monthly);

        Assert.Equal(800, resolved);
    }

    [Theory]
    [InlineData(50_000, "50000")]
    [InlineData(50_000d / 12, "4166.67")]
    [InlineData(0, "0")]
    [InlineData(1_234.5, "1234.5")]
    [InlineData(double.NaN, "0")]
    public void Format_shows_cents_only_when_there_are_cents(double value, string expected)
    {
        Assert.Equal(expected, CurrencyPeriodMath.Format(value, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Suffix_and_qualifier_name_the_period()
    {
        Assert.Equal("/yr", CurrencyPeriodMath.Suffix(CurrencyPeriod.Annual));
        Assert.Equal("/mo", CurrencyPeriodMath.Suffix(CurrencyPeriod.Monthly));
        Assert.Equal("per year", CurrencyPeriodMath.Qualifier(CurrencyPeriod.Annual));
        Assert.Equal("per month", CurrencyPeriodMath.Qualifier(CurrencyPeriod.Monthly));
    }
}
