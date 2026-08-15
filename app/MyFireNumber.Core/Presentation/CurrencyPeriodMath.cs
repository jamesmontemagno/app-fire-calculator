namespace MyFireNumber.Core.Presentation;

/// <summary>
/// Conversion and formatting for the monthly/annual display period. Mirrors the behaviour of
/// <c>web/src/utils/currencyPeriod.ts</c>.
/// </summary>
/// <remarks>
/// Nothing here changes a calculation. Callers convert a canonical stored amount for display and
/// convert an edited display amount back; the value handed to
/// <see cref="Calculations.FinancialCalculator"/> is always the canonical one.
/// </remarks>
public static class CurrencyPeriodMath
{
    public const int MonthsPerYear = 12;

    /// <summary>
    /// Convert an amount between periods. Exact for equal periods, so it is safe to call
    /// unconditionally.
    /// </summary>
    public static double Convert(double value, CurrencyPeriod from, CurrencyPeriod to)
    {
        from.Validated(nameof(from));
        to.Validated(nameof(to));

        if (from == to)
        {
            return value;
        }

        return to == CurrencyPeriod.Monthly
            ? value / MonthsPerYear
            : value * MonthsPerYear;
    }

    /// <summary>True when two amounts are indistinguishable once rounded to whole cents.</summary>
    public static bool IsSameToCent(double a, double b)
    {
        if (!double.IsFinite(a) || !double.IsFinite(b))
        {
            return false;
        }

        return RoundHalfUp(a * 100) == RoundHalfUp(b * 100);
    }

    /// <summary>
    /// Round half **up** (toward positive infinity), matching JavaScript's <c>Math.round</c>.
    /// </summary>
    /// <remarks>
    /// <para>This is deliberately not <see cref="Math.Round(double)"/> and deliberately not
    /// <see cref="MidpointRounding.AwayFromZero"/>. The web side of this feature rounds with
    /// <c>Math.round</c>, and the three rules disagree in different places:</para>
    /// <code>
    /// value    JS Math.round    C# default (banker's)    C# AwayFromZero
    ///   0.5          1                   0                     1
    ///   2.5          3                   2                     3
    ///   4.5          5                   4                     5
    ///  -0.5         -0                  -0                    -1
    ///  -1.5         -1                  -2                    -2
    /// </code>
    /// <para>The default disagrees on <b>positive</b> midpoints — every even boundary — which is
    /// exactly where currency lives: half a cent on a positive amount. <c>AwayFromZero</c> matches
    /// on positives and diverges on negatives. Pairing C# <c>AwayFromZero</c> with JS
    /// <c>Math.round</c> is what shipped as issue #63, so this repo has already paid for getting it
    /// wrong once.</para>
    /// <para>Implemented by comparing the fraction rather than as <c>Math.Floor(x + 0.5)</c>: the
    /// latter disagrees with JS at <c>0.49999999999999994</c>, where the addition itself rounds up to
    /// exactly 1.</para>
    /// </remarks>
    public static double RoundHalfUp(double value)
    {
        var floor = Math.Floor(value);
        return value - floor >= 0.5 ? floor + 1 : floor;
    }

    /// <summary>
    /// Resolve what a field edit should store.
    /// </summary>
    /// <remarks>
    /// The displayed figure is rounded for readability, so converting it straight back would drift:
    /// $50,000/yr shows as $4,166.67/mo, and $4,166.67 x 12 is $50,000.04. Whenever the amount that
    /// was typed is the same (to the cent) as the amount already on screen, the user did not actually
    /// change anything, so the stored value is returned untouched and the round trip is exactly
    /// lossless. A genuine edit converts normally.
    /// </remarks>
    public static double ResolveEditedAmount(
        double typedDisplayAmount,
        double storedValue,
        CurrencyPeriod displayPeriod,
        CurrencyPeriod storedPeriod)
    {
        var currentDisplayAmount = Convert(storedValue, storedPeriod, displayPeriod);
        return IsSameToCent(typedDisplayAmount, currentDisplayAmount)
            ? storedValue
            : Convert(typedDisplayAmount, displayPeriod, storedPeriod);
    }

    /// <summary>
    /// Format an amount for a currency entry. Cents are shown only when the amount has them, so
    /// annual figures stay clean while monthly conversions keep the precision needed to edit them
    /// accurately.
    /// </summary>
    /// <remarks>
    /// <c>"0.##"</c> is the format the calculator entries already use, so the display period does not
    /// change how any amount is written. It is also why this type has no counterpart to the web
    /// helper <c>formatTypedAmount</c>: that exists purely to stop comma grouping from eating a
    /// trailing <c>"."</c> mid-keystroke, and nothing here groups digits or rewrites text while the
    /// user is typing.
    /// </remarks>
    public static string Format(double value, IFormatProvider formatProvider)
    {
        return double.IsFinite(value) ? value.ToString("0.##", formatProvider) : "0";
    }

    /// <summary>Short suffix shown inside the entry, e.g. <c>/mo</c>.</summary>
    public static string Suffix(CurrencyPeriod period)
    {
        return period.Validated(nameof(period)) == CurrencyPeriod.Monthly ? "/mo" : "/yr";
    }

    /// <summary>Long qualifier appended to a field label, e.g. <c>per month</c>.</summary>
    public static string Qualifier(CurrencyPeriod period)
    {
        return period.Validated(nameof(period)) == CurrencyPeriod.Monthly ? "per month" : "per year";
    }
}
