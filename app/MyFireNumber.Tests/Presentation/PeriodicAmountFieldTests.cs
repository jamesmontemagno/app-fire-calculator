using System.Globalization;

using MyFireNumber.Core.Presentation;

namespace MyFireNumber.Tests.Presentation;

/// <summary>
/// Pins the lossless round trip behind the monthly/annual display toggle.
///
/// <para><b>Read <see cref="ToggleWithBindingEcho"/> before anything else in this file.</b> Because
/// <see cref="PeriodicAmountField.Text"/> is always written from the canonical value, simply calling
/// <see cref="PeriodicAmountField.SetDisplayPeriod"/> in a loop cannot drift — such a test passes
/// with <c>ResolveEditedAmount</c> deleted, and would be a guard whose blind spot is exactly the
/// thing it claims to check.</para>
///
/// <para>The drift is real only because the MAUI <c>Entry</c> binds <c>Mode=TwoWay</c>: assigning
/// <c>Text</c> fires the binding back through the setter, so the rounded string is resubmitted as if
/// typed. Every round-trip test here simulates that echo, and
/// <see cref="The_binding_echo_is_what_gives_the_round_trip_tests_teeth"/> computes what the
/// unguarded path would have stored to prove the echo is load-bearing rather than decorative.</para>
/// </summary>
public class PeriodicAmountFieldTests
{
    private static PeriodicAmountField Field(
        CurrencyPeriod storedPeriod,
        double storedValue,
        CurrencyPeriod displayPeriod = CurrencyPeriod.Annual)
    {
        var field = new PeriodicAmountField(storedPeriod, displayPeriod, CultureInfo.InvariantCulture);
        field.SetStoredValue(storedValue);
        return field;
    }

    /// <summary>
    /// Switch period the way the running app does: the view model assigns the new text to the bound
    /// property, and the two-way binding immediately hands that same text back to the setter.
    /// Dropping the second line is what makes a round-trip test vacuous.
    /// </summary>
    private static void ToggleWithBindingEcho(PeriodicAmountField field, CurrencyPeriod period)
    {
        field.SetDisplayPeriod(period);
        field.TrySetText(field.Text);
    }

    private static long Bits(double value) => BitConverter.DoubleToInt64Bits(value);

    /// <summary>
    /// Asserts the stored value has not moved at all. The double comparison runs first purely so a
    /// failure reads as "Expected: 50000  Actual: 50000.04" instead of two raw bit patterns; the bit
    /// comparison is what makes "unmoved" mean bit-for-bit rather than "close enough".
    /// </summary>
    private static void AssertUnmoved(double expected, PeriodicAmountField field)
    {
        Assert.Equal(expected, field.StoredValue);
        Assert.Equal(Bits(expected), Bits(field.StoredValue));
    }

    [Theory]
    [InlineData(50_000)]
    [InlineData(48_000)]
    [InlineData(24_000)]
    [InlineData(72_000)]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(0.01)]
    [InlineData(100)]
    [InlineData(12_345.67)]
    [InlineData(1_000_000)]
    [InlineData(33_333.33)]
    public void Toggling_repeatedly_never_moves_an_annual_stored_value(double stored)
    {
        var field = Field(CurrencyPeriod.Annual, stored);

        for (var i = 0; i < 50; i++)
        {
            ToggleWithBindingEcho(field, CurrencyPeriod.Monthly);
            AssertUnmoved(stored, field);

            ToggleWithBindingEcho(field, CurrencyPeriod.Annual);
            AssertUnmoved(stored, field);
        }

        AssertUnmoved(stored, field);
    }

    [Theory]
    [InlineData(600)]
    [InlineData(1_234.5678)]
    [InlineData(0)]
    [InlineData(2_999.99)]
    public void Toggling_repeatedly_never_moves_a_monthly_stored_value(double stored)
    {
        var field = Field(CurrencyPeriod.Monthly, stored);

        for (var i = 0; i < 50; i++)
        {
            ToggleWithBindingEcho(field, CurrencyPeriod.Annual);
            AssertUnmoved(stored, field);

            ToggleWithBindingEcho(field, CurrencyPeriod.Monthly);
            AssertUnmoved(stored, field);
        }

        AssertUnmoved(stored, field);
    }

    /// <summary>
    /// Proves the echo in <see cref="ToggleWithBindingEcho"/> is load-bearing, by computing what a
    /// field without <c>ResolveEditedAmount</c> would have stored from the very same text. If those
    /// two numbers were equal, every round-trip test above would pass no matter what the production
    /// code did.
    /// </summary>
    [Fact]
    public void The_binding_echo_is_what_gives_the_round_trip_tests_teeth()
    {
        var field = Field(CurrencyPeriod.Annual, 50_000);
        field.SetDisplayPeriod(CurrencyPeriod.Monthly);

        Assert.Equal("4166.67", field.Text);

        var unguarded = CurrencyPeriodMath.Convert(
            double.Parse(field.Text, CultureInfo.InvariantCulture),
            CurrencyPeriod.Monthly,
            CurrencyPeriod.Annual);

        // What a straight conversion would have stored: not 50000.
        Assert.Equal(50_000.04, unguarded, 6);
        Assert.NotEqual(Bits(50_000), Bits(unguarded));

        // What the field actually stores from that identical text.
        field.TrySetText(field.Text);
        AssertUnmoved(50_000, field);
    }

    [Fact]
    public void Display_period_changes_before_the_text_is_rewritten()
    {
        // Rewriting first would leave "50000" on screen while the field already claimed monthly, and
        // the echo would then read an annual figure as monthly and store 600000.
        var field = Field(CurrencyPeriod.Annual, 50_000);

        field.SetDisplayPeriod(CurrencyPeriod.Monthly);

        Assert.Equal(CurrencyPeriod.Monthly, field.DisplayPeriod);
        Assert.Equal("4166.67", field.Text);

        field.TrySetText(field.Text);
        Assert.Equal(50_000, field.StoredValue);
    }

    [Fact]
    public void A_monthly_stored_field_shows_twelve_times_the_stored_amount_when_annual()
    {
        // The healthcare premium is the one canonically monthly field. A mechanism that assumed
        // every stored value was annual would show $600/mo as $50/mo here.
        var field = Field(CurrencyPeriod.Monthly, 600);

        Assert.Equal(CurrencyPeriod.Monthly, field.StoredPeriod);
        Assert.Equal("7200", field.Text);

        field.SetDisplayPeriod(CurrencyPeriod.Monthly);
        Assert.Equal("600", field.Text);
        Assert.Equal(600, field.StoredValue);
    }

    [Fact]
    public void Editing_a_monthly_stored_field_while_annual_stores_the_monthly_amount()
    {
        var field = Field(CurrencyPeriod.Monthly, 600);

        Assert.True(field.TrySetText("9600"));

        Assert.Equal(800, field.StoredValue);
    }

    [Fact]
    public void Editing_an_annual_stored_field_while_monthly_stores_the_annual_amount()
    {
        var field = Field(CurrencyPeriod.Annual, 50_000, CurrencyPeriod.Monthly);

        Assert.True(field.TrySetText("5000"));

        Assert.Equal(60_000, field.StoredValue);
    }

    [Fact]
    public void An_edit_survives_a_toggle_round_trip()
    {
        var field = Field(CurrencyPeriod.Annual, 50_000, CurrencyPeriod.Monthly);
        field.TrySetText("5000");

        ToggleWithBindingEcho(field, CurrencyPeriod.Annual);
        Assert.Equal("60000", field.Text);

        ToggleWithBindingEcho(field, CurrencyPeriod.Monthly);
        Assert.Equal("5000", field.Text);
        Assert.Equal(60_000, field.StoredValue);
    }

    /// <summary>
    /// Text is left exactly as typed, which is why there is no counterpart to the web helper
    /// <c>formatTypedAmount</c>. Rewriting it here is what would eat the trailing separator.
    /// </summary>
    [Theory]
    [InlineData("1234.", 1234)]
    [InlineData("1234.5", 1234.5)]
    [InlineData("1234.50", 1234.5)]
    [InlineData("0", 0)]
    [InlineData("0.0", 0)]
    public void Typed_text_is_kept_verbatim_while_the_parsed_amount_is_stored(string typed, double expected)
    {
        var field = Field(CurrencyPeriod.Annual, 0);

        Assert.True(field.TrySetText(typed));

        Assert.Equal(typed, field.Text);
        Assert.Equal(expected, field.StoredValue);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData(".")]
    [InlineData("-5")]
    [InlineData(null)]
    public void Unusable_text_is_flagged_without_destroying_the_stored_amount(string? typed)
    {
        var field = Field(CurrencyPeriod.Annual, 48_000);

        Assert.False(field.TrySetText(typed));

        Assert.False(field.HasValidText);
        Assert.Equal(48_000, field.StoredValue);
        Assert.Equal(typed ?? string.Empty, field.Text);
    }

    [Fact]
    public void Changing_period_replaces_unusable_text_with_the_stored_amount()
    {
        // Text the field could not parse belongs to the period the user was looking at. Leaving it on
        // screen after a toggle would label an annual figure as monthly.
        var field = Field(CurrencyPeriod.Annual, 48_000);
        Assert.False(field.TrySetText("48,0x"));

        field.SetDisplayPeriod(CurrencyPeriod.Monthly);

        Assert.True(field.HasValidText);
        Assert.Equal("4000", field.Text);
        Assert.Equal(48_000, field.StoredValue);
    }

    [Fact]
    public void Setting_the_same_period_twice_is_a_no_op()
    {
        var field = Field(CurrencyPeriod.Annual, 48_000);
        field.TrySetText("48000.");

        field.SetDisplayPeriod(CurrencyPeriod.Annual);

        Assert.Equal("48000.", field.Text);
    }

    [Fact]
    public void Loading_a_stored_amount_rewrites_the_text_in_the_current_display_period()
    {
        var field = Field(CurrencyPeriod.Annual, 0, CurrencyPeriod.Monthly);

        field.SetStoredValue(60_000);

        Assert.Equal("5000", field.Text);
        Assert.True(field.HasValidText);
    }

    [Fact]
    public void A_non_finite_stored_amount_falls_back_to_zero()
    {
        var field = Field(CurrencyPeriod.Annual, 0);

        field.SetStoredValue(double.NaN);

        Assert.Equal(0, field.StoredValue);
        Assert.Equal("0", field.Text);
    }
}
