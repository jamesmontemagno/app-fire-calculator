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
    /// back and proves nothing about the sign. <see cref="double.IsNegative(double)"/> is the C#
    /// counterpart to web's <c>Object.is</c> and is the only assertion here that can see it.</para>
    /// <para>This implementation returns <b>positive</b> zero: <c>Math.Floor(-0.5)</c> is
    /// <c>-1.0</c>, and IEEE-754 addition of exact opposites yields <c>+0.0</c> under
    /// round-to-nearest, so the <c>-0</c> path is never taken. So it is one step *safer* than the JS
    /// original here rather than a match, and this test pins that rather than the JS bit pattern.</para>
    /// <para><b>This test used to also assert on <c>Format(result)</c>, and that assertion is what
    /// hid issue #91.</b> <c>result</c> is always positive zero, so <c>Format</c> was called but
    /// never once handed a negative value — the suite looked as though it covered <c>Format</c>'s
    /// sign handling while proving only that <c>Format(+0.0)</c> is <c>"0"</c>. That misreading
    /// became the premise of #87 ("MAUI's <c>Format</c> deliberately never emits -0"), which was
    /// false. The two are different guarantees and are now tested apart: this one is about what
    /// <c>RoundHalfUp</c> <i>produces</i>;
    /// <see cref="Format_normalises_a_negative_zero_it_is_actually_handed"/> is about what
    /// <c>Format</c> does with one it is <i>given</i>. Conflating them is what let the defect
    /// survive two sessions.</para>
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

    /// <summary>
    /// Issue #91: a negative whose magnitude the format string rounds away renders <b>unsigned</b>.
    ///
    /// <para>These three rows were added by PR #90 asserting <c>"-0"</c>, to record MAUI's
    /// divergence from web as measured fact rather than fix it out of scope. They are inverted here
    /// rather than deleted, so the suite keeps naming the exact inputs that used to be wrong.</para>
    /// <para>#87 was filed on the premise that <c>Format</c> "deliberately never emits -0". That was
    /// false: <c>"0.##"</c> keeps the sign under IEEE-compliant formatting while dropping the
    /// magnitude, exactly as <c>Intl.NumberFormat</c> does. The safety established in #77 belongs to
    /// <see cref="CurrencyPeriodMath.RoundHalfUp"/>, which never <i>produces</i> a negative zero, and
    /// was mistaken for a property of <c>Format</c> — see
    /// <see cref="RoundHalfUp_does_not_produce_negative_zero"/> for how the old test made that
    /// misreading look verified.</para>
    /// <para>Only the first row is a negative zero. <c>-0.001</c> and <c>-1e-7</c> are ordinary
    /// negative numbers, so a fix written against <c>double.IsNegative(value) &amp;&amp; value == 0</c>
    /// — the C# analogue of <c>Object.is(value, -0)</c> — would leave them rendering a signed zero.
    /// That is why both platforms normalise the <i>rendered text</i> instead of the input.</para>
    /// </summary>
    [Theory]
    [InlineData(-0.0, "0")]
    [InlineData(-0.001, "0")]
    [InlineData(-1e-7, "0")]
    public void Format_renders_a_rounded_away_negative_unsigned(double value, string expected)
    {
        // Guards the table against going vacuous. -0.0 == 0.0, so if the sign were lost passing
        // through the InlineData attribute the first row would silently become a duplicate of
        // Format(0.0) and would pass no matter what Format did with a real negative zero.
        Assert.True(
            double.IsNegative(value),
            $"{value:R} reached the test unsigned, so this row proves nothing about negatives.");

        Assert.Equal(expected, CurrencyPeriodMath.Format(value, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// <see cref="CurrencyPeriodMath.Format"/> handed a genuine negative zero — the single case the
    /// suite appeared to cover before #91 and never actually exercised.
    /// </summary>
    [Fact]
    public void Format_normalises_a_negative_zero_it_is_actually_handed()
    {
        const double negativeZero = -0.0;

        // Without this the assertion below is worthless: an unsigned zero would satisfy it too.
        Assert.True(double.IsNegative(negativeZero));

        Assert.Equal("0", CurrencyPeriodMath.Format(negativeZero, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// The negative sign is culture data, not the character <c>'-'</c>.
    ///
    /// <para>Every other row in this file formats under <see cref="CultureInfo.InvariantCulture"/>,
    /// but production does not: <c>PeriodicAmountField</c> defaults its provider to
    /// <see cref="CultureInfo.CurrentCulture"/>, so the sign a user actually sees is whatever their
    /// device locale prescribes. On .NET 10, 99 of the 1063 available cultures render a negative
    /// with something other than U+002D HYPHEN-MINUS:</para>
    /// <code>
    /// sv-SE   U+2212           MINUS SIGN
    /// fi-FI   U+2212           MINUS SIGN
    /// et-EE   U+2212           MINUS SIGN
    /// ar-EG   U+061C U+002D    ARABIC LETTER MARK + hyphen    (two chars)
    /// fa-IR   U+200E U+2212    LEFT-TO-RIGHT MARK + MINUS     (two chars, no hyphen at all)
    /// </code>
    /// <para>A fix built on a literal <c>"-"</c> — <c>StartsWith("-")</c>, <c>TrimStart('-')</c>, or
    /// <c>Substring(1)</c> — passes every Invariant-culture assertion in this suite and still leaves
    /// the defect in place for all five. Worse for the two-character signs: the default
    /// <c>StartsWith(string)</c> overload is culture-sensitive and treats U+200E and U+061C as
    /// ignorable, so it can report a match and then <c>Substring(1)</c> strips only the mark and
    /// hands back <c>"-0"</c> unchanged. These rows exist so that shape of fix cannot ship green.
    /// </para>
    /// </summary>
    [Theory]
    [InlineData("sv-SE")]
    [InlineData("fi-FI")]
    [InlineData("et-EE")]
    [InlineData("ar-EG")]
    [InlineData("fa-IR")]
    public void Format_normalises_a_signed_zero_under_a_culture_whose_negative_sign_is_not_a_hyphen(
        string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);

        // The row only means anything while this holds. If ICU data ever changes it, or the suite is
        // run in invariant globalization mode, this fails loudly rather than quietly degrading into
        // another copy of the Invariant test above.
        Assert.NotEqual("-", culture.NumberFormat.NegativeSign);

        Assert.Equal("0", CurrencyPeriodMath.Format(-0.0, culture));
        Assert.Equal("0", CurrencyPeriodMath.Format(-0.001, culture));
        Assert.Equal("0", CurrencyPeriodMath.Format(-1e-7, culture));
    }

    /// <summary>
    /// The contract itself, free of any ICU data on the build machine: whatever the provider says its
    /// negative sign is, that is what a rendering of nothing but zeros is stripped of.
    ///
    /// <para>The sign here is deliberately two characters and contains no hyphen, so
    /// <c>StartsWith("-")</c> never fires and <c>Substring(1)</c> would leave <c>"~0"</c> behind. The
    /// decimal separator is changed too, so nothing can pass by assuming <c>'.'</c>. The last two
    /// rows keep the fix from being over-broad in the same breath.</para>
    /// </summary>
    [Fact]
    public void Format_takes_the_negative_sign_from_the_format_provider()
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NegativeSign = "~~";
        culture.NumberFormat.NumberDecimalSeparator = "|";

        Assert.Equal("0", CurrencyPeriodMath.Format(-0.0, culture));
        Assert.Equal("0", CurrencyPeriodMath.Format(-0.001, culture));
        Assert.Equal("~~0|01", CurrencyPeriodMath.Format(-0.005, culture));
        Assert.Equal("~~1234|5", CurrencyPeriodMath.Format(-1_234.5, culture));
        Assert.Equal("0|5", CurrencyPeriodMath.Format(0.5, culture));
    }

    /// <summary>
    /// Guards the rows above: they are three different numbers, and only the first is negative zero.
    ///
    /// <para><c>double.IsNegative</c> is the C# counterpart to web's <c>Object.is</c> here.
    /// <c>-0.0 == 0.0</c> is <c>true</c> in both languages, so an equality assertion cannot see the
    /// sign at all — the blind spot that let #87 be written with a false premise and #91 survive
    /// undetected. The other two rows are ordinary negative numbers whose magnitude the format string
    /// rounds away while the sign survives, so a fix written against negative zero alone would miss
    /// them on either platform.</para>
    /// </summary>
    [Fact]
    public void The_rounded_away_zero_rows_are_genuinely_different_inputs()
    {
        const double negativeZero = -0.0;

        Assert.True(double.IsNegative(negativeZero));
        Assert.False(double.IsNegative(0.0));
        // The blind spot itself: this passes, and proves nothing about the sign.
        Assert.Equal(0.0, negativeZero);

        Assert.False(negativeZero < 0);
        Assert.True(-0.001 < 0);
        Assert.True(-1e-7 < 0);
    }

    /// <summary>
    /// A negative large enough to survive the rounding keeps its sign, on both platforms. Half a cent
    /// is the first magnitude that still shows, so it is the boundary worth pinning: a tidier-looking
    /// clamp such as <c>Math.Round(value * 100) == 0</c> would swallow this row instead, turning a
    /// real amount into an unsigned zero. The fix has to be neither narrower nor wider than "the
    /// rendering is nothing but zeros".
    /// </summary>
    [Theory]
    [InlineData(-0.005, "-0.01")]
    [InlineData(-0.01, "-0.01")]
    [InlineData(-1_234.5, "-1234.5")]
    [InlineData(-50_000, "-50000")]
    public void Format_keeps_the_sign_of_a_negative_that_survives_the_rounding(double value, string expected)
    {
        Assert.Equal(expected, CurrencyPeriodMath.Format(value, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// The same boundary under a culture that changes both the sign and the decimal separator, so
    /// "do not over-correct" is pinned everywhere "normalise the signed zero" is.
    /// </summary>
    [Fact]
    public void Format_keeps_a_surviving_negative_under_a_non_hyphen_culture()
    {
        var culture = CultureInfo.GetCultureInfo("sv-SE");

        // U+2212 MINUS SIGN, then a comma decimal separator.
        Assert.Equal("\u22120,01", CurrencyPeriodMath.Format(-0.005, culture));
        Assert.Equal("\u22121234,5", CurrencyPeriodMath.Format(-1_234.5, culture));
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
