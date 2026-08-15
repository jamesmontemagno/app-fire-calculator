using System.Globalization;

namespace MyFireNumber.Core.Presentation;

/// <summary>
/// One recurring currency input: the canonical amount, the period it is displayed in, and the text
/// the entry is currently showing.
/// </summary>
/// <remarks>
/// <para><b>Why this type exists and the web has no counterpart.</b> On web the stored value is a
/// number and the field text is derived from it. In this app the view models hold <i>text</i> as the
/// source of truth and parse it to build a draft, so a display period would have nowhere lossless to
/// live: annual 50000 shown as monthly rounds to <c>"4166.67"</c>, and parsing that back gives
/// 50000.04. This type supplies the canonical numeric shadow the view models lack, so toggling the
/// display never edits the value.</para>
/// <para><b>The entry echo is the reason <see cref="CurrencyPeriodMath.ResolveEditedAmount"/> is
/// still needed even though <see cref="Text"/> is always written from
/// <see cref="StoredValue"/>.</b> The MAUI <c>Entry</c> binds <c>Mode=TwoWay</c>, so assigning
/// <see cref="Text"/> fires the binding back through the setter and the rounded string is
/// re-submitted as if the user had typed it. <see cref="TrySetText"/> recognises an amount that
/// matches what is already on screen to the cent and keeps the stored value untouched, so the round
/// trip is exactly lossless however many times it happens.</para>
/// <para>Text is written only when the value is loaded or the period changes. It is never rewritten
/// while the user types, which is why there is no counterpart to the web helper
/// <c>formatTypedAmount</c>: typing <c>"1234."</c> leaves <c>"1234."</c> on screen.</para>
/// </remarks>
public sealed class PeriodicAmountField
{
    private readonly IFormatProvider formatProvider;

    public PeriodicAmountField(
        CurrencyPeriod storedPeriod,
        CurrencyPeriod displayPeriod = CurrencyPeriod.Annual,
        IFormatProvider? formatProvider = null)
    {
        StoredPeriod = storedPeriod.Validated(nameof(storedPeriod));
        DisplayPeriod = displayPeriod.Validated(nameof(displayPeriod));
        this.formatProvider = formatProvider ?? CultureInfo.CurrentCulture;
        Text = FormatFromStoredValue();
        HasValidText = true;
    }

    /// <summary>The period <see cref="StoredValue"/> is expressed in. Fixed for the field's life.</summary>
    public CurrencyPeriod StoredPeriod { get; }

    /// <summary>The period <see cref="Text"/> is expressed in.</summary>
    public CurrencyPeriod DisplayPeriod { get; private set; }

    /// <summary>The canonical amount, in <see cref="StoredPeriod"/>. This is what calculations use.</summary>
    public double StoredValue { get; private set; }

    /// <summary>What the entry shows. Left exactly as typed while the user is editing.</summary>
    public string Text { get; private set; }

    /// <summary>
    /// False when <see cref="Text"/> is not a non-negative number, matching the existing
    /// <c>TryParseNonNegative</c> rule the calculators validate with. <see cref="StoredValue"/> keeps
    /// its last good value so a half-typed entry does not destroy it.
    /// </summary>
    public bool HasValidText { get; private set; }

    /// <summary>Load a canonical amount, e.g. when a draft or saved plan is applied.</summary>
    public void SetStoredValue(double value)
    {
        StoredValue = double.IsFinite(value) ? value : 0;
        Text = FormatFromStoredValue();
        HasValidText = true;
    }

    /// <summary>
    /// Accept text from the entry — either typed by the user or echoed back by the two-way binding.
    /// </summary>
    /// <returns><c>true</c> when the text parsed to a non-negative amount.</returns>
    public bool TrySetText(string? raw)
    {
        Text = raw ?? string.Empty;

        if (!double.TryParse(Text, NumberStyles.Number, formatProvider, out var typed)
            || !double.IsFinite(typed)
            || typed < 0)
        {
            HasValidText = false;
            return false;
        }

        HasValidText = true;
        StoredValue = CurrencyPeriodMath.ResolveEditedAmount(typed, StoredValue, DisplayPeriod, StoredPeriod);
        return true;
    }

    /// <summary>Switch which period the amount is shown in. The stored amount does not move.</summary>
    /// <remarks>
    /// <see cref="DisplayPeriod"/> is updated before the text is rewritten. The other order would
    /// hand the new text to the binding while the field still claims the old period, so the echo
    /// through <see cref="TrySetText"/> would read a monthly figure as an annual one and multiply the
    /// stored value by 144.
    /// </remarks>
    public void SetDisplayPeriod(CurrencyPeriod period)
    {
        period.Validated(nameof(period));
        if (period == DisplayPeriod)
        {
            return;
        }

        DisplayPeriod = period;
        // Any half-typed text belongs to the period the user was looking at, so it is replaced rather
        // than reinterpreted. Web does the same by clearing its draft when the period changes.
        Text = FormatFromStoredValue();
        HasValidText = true;
    }

    private string FormatFromStoredValue()
    {
        return CurrencyPeriodMath.Format(
            CurrencyPeriodMath.Convert(StoredValue, StoredPeriod, DisplayPeriod),
            formatProvider);
    }
}
