namespace MyFireNumber.Core.Presentation;

/// <summary>One recurring currency input on a calculator, and the period it is stored in.</summary>
/// <param name="Key">
/// Stable identifier, deliberately the same string the web calculator uses for the parameter
/// (<c>annualContribution</c>, <c>healthcareMonthlyPremium</c>, …). Sharing the vocabulary means the
/// cross-platform inventory in <c>shared/parity/periodic-fields.json</c> can be compared directly,
/// with no name translation table for the two sides to drift apart in.
/// </param>
/// <param name="StoredPeriod">
/// The period the canonical value is held in. Annual for everything except the healthcare premium.
/// </param>
public sealed record PeriodicFieldDefinition(string Key, CurrencyPeriod StoredPeriod);

/// <summary>
/// Which fields on each calculator follow the monthly/annual display toggle.
/// </summary>
/// <remarks>
/// <para>Recurring amounts only. Balances and one-off amounts — current savings, portfolio value,
/// starting amount — are not periodic, because "your portfolio, monthly" means nothing.</para>
/// <para><c>withdrawal-rate</c> and <c>debt-payoff</c> declare an empty list rather than being
/// omitted. Omission and "deliberately none" would otherwise be the same state, and a calculator
/// added later would look like it had already been considered.</para>
/// </remarks>
public static class PeriodicFieldCatalog
{
    public const string AnnualContribution = "annualContribution";
    public const string AnnualIncome = "annualIncome";
    public const string AnnualExpenses = "annualExpenses";
    public const string PartTimeIncome = "partTimeIncome";
    public const string HealthcareMonthlyPremium = "healthcareMonthlyPremium";
    public const string HealthcareAnnualDeductible = "healthcareAnnualDeductible";
    public const string HealthcareAnnualOutOfPocket = "healthcareAnnualOutOfPocket";

    private static readonly IReadOnlyList<PeriodicFieldDefinition> FireFamily =
    [
        new(AnnualContribution, CurrencyPeriod.Annual),
        new(AnnualIncome, CurrencyPeriod.Annual),
        new(AnnualExpenses, CurrencyPeriod.Annual)
    ];

    private static readonly Dictionary<string, IReadOnlyList<PeriodicFieldDefinition>> Definitions =
        new(StringComparer.Ordinal)
        {
            ["standard-fire"] = FireFamily,
            ["lean-fire"] = FireFamily,
            ["fat-fire"] = FireFamily,
            ["coast-fire"] =
            [
                new(AnnualContribution, CurrencyPeriod.Annual),
                new(AnnualExpenses, CurrencyPeriod.Annual)
            ],
            ["barista-fire"] =
            [
                new(AnnualContribution, CurrencyPeriod.Annual),
                new(AnnualExpenses, CurrencyPeriod.Annual),
                new(PartTimeIncome, CurrencyPeriod.Annual)
            ],
            ["reverse-fire"] =
            [
                new(AnnualExpenses, CurrencyPeriod.Annual)
            ],
            ["savings-rate"] =
            [
                // The contribution is governed by ContributionFrequency, which changes the arithmetic,
                // so it is not a display-period field. Only income is.
                new(AnnualIncome, CurrencyPeriod.Annual)
            ],
            ["healthcare-gap"] =
            [
                // The one canonically monthly amount in the app. A mechanism that assumed everything
                // was annual would show $600/mo as $50/mo and be wrong by 144x after a round trip.
                new(HealthcareMonthlyPremium, CurrencyPeriod.Monthly),
                new(HealthcareAnnualDeductible, CurrencyPeriod.Annual),
                new(HealthcareAnnualOutOfPocket, CurrencyPeriod.Annual)
            ],
            ["retirement-cash-flow"] =
            [
                new(AnnualExpenses, CurrencyPeriod.Annual)
            ],
            // Every currency input here is a portfolio balance or a rate, so there is nothing
            // recurring to restate. Web gives this calculator no toggle either.
            ["withdrawal-rate"] = [],
            // The payoff model steps monthly, so the budget, extra payment, and per-debt minimum are
            // genuinely monthly inputs rather than monthly views of an annual amount. Changing the
            // number would change the schedule.
            ["debt-payoff"] = [],
            // Account balance is a point-in-time value and every payment amount is a result.
            ["sepp-72t"] = [],
            // The annual conversion is a tax-year strategy input, not a display-period preference.
            ["roth-conversion"] = []
        };

    /// <summary>Every calculator ID this catalog covers.</summary>
    public static IReadOnlyCollection<string> CalculatorIds => Definitions.Keys;

    /// <summary>The periodic fields for a calculator, possibly empty.</summary>
    /// <exception cref="KeyNotFoundException">The calculator has no declaration at all.</exception>
    public static IReadOnlyList<PeriodicFieldDefinition> For(string calculatorId)
    {
        return Definitions.TryGetValue(calculatorId, out var fields)
            ? fields
            : throw new KeyNotFoundException(
                $"No periodic field declaration for calculator '{calculatorId}'. Declare its recurring "
                + "currency fields, or declare an empty list if it has none.");
    }
}
