namespace MyFireNumber.Core.Presentation;

/// <summary>
/// The period a currency amount is <b>displayed</b> in.
/// </summary>
/// <remarks>
/// <para>This is a presentation concern only, and it lives in <c>Presentation</c> rather than
/// <c>Calculations</c> for that reason. Every recurring amount is stored in one canonical period
/// (annual, except the healthcare premium which is canonically monthly) and every calculation runs on
/// that canonical value. Showing an annual amount as a monthly one divides by 12 at the display edge
/// and nothing else: it never introduces intra-year compounding and never changes the math.</para>
/// <para><b>Not to be confused with <see cref="Calculations.ContributionFrequency"/>.</b> That one is
/// a semantic input frequency that genuinely changes the arithmetic — it selects between
/// <c>amount * 12</c> and <c>amount</c> for the Savings &amp; Investment Rate calculator, and it is
/// persisted in <see cref="Calculations.SavingsInvestmentDraft"/> because it is a calculation input.
/// A <see cref="CurrencyPeriod"/> never reaches a draft, a workbook, or
/// <see cref="Calculations.FinancialCalculator"/>.</para>
/// <para>Mirrors <c>web/src/utils/currencyPeriod.ts</c>.</para>
/// </remarks>
public enum CurrencyPeriod
{
    Annual,
    Monthly
}

public static class CurrencyPeriodExtensions
{
    /// <summary>
    /// Throws unless <paramref name="period"/> is a declared member.
    /// </summary>
    /// <remarks>
    /// An enum does not make an out-of-range value unrepresentable in C#: <c>(CurrencyPeriod)99</c>
    /// compiles and carries through any <c>switch</c> that has a <c>default</c> arm. Every entry point
    /// in this namespace therefore validates explicitly and throws, rather than quietly treating an
    /// unknown value as <see cref="CurrencyPeriod.Annual"/> — a silent fallback would turn a caller's
    /// bug into a plausible-looking wrong number on screen.
    /// </remarks>
    public static CurrencyPeriod Validated(this CurrencyPeriod period, string paramName)
    {
        if (!Enum.IsDefined(period))
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                period,
                $"'{(int)period}' is not a declared {nameof(CurrencyPeriod)}.");
        }

        return period;
    }
}
