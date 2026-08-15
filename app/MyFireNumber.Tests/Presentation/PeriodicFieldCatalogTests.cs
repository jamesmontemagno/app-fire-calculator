using System.Globalization;

using MyFireNumber.Core.Calculators;
using MyFireNumber.Core.Presentation;

namespace MyFireNumber.Tests.Presentation;

/// <summary>
/// Structural checks on which calculators declare recurring currency fields.
///
/// <para>Nothing here asserts a count. <c>Assert.Equal(11, …)</c> would pass the day a twelfth
/// calculator was added and forgotten, which is the failure mode these checks exist to prevent. The
/// covered set is compared against <see cref="CalculatorCatalog"/> instead, so a new calculator turns
/// this file red until someone decides — in writing — whether it has periodic fields.</para>
/// </summary>
public class PeriodicFieldCatalogTests
{
    private static readonly ICalculatorCatalog Calculators = new CalculatorCatalog();

    [Fact]
    public void Every_calculator_declares_its_periodic_fields()
    {
        var shipped = Calculators.All.Select(definition => definition.Id).OrderBy(id => id, StringComparer.Ordinal);
        var declared = PeriodicFieldCatalog.CalculatorIds.OrderBy(id => id, StringComparer.Ordinal);

        Assert.Equal(shipped, declared);
    }

    [Theory]
    [InlineData("withdrawal-rate")]
    [InlineData("debt-payoff")]
    public void Calculators_without_recurring_amounts_declare_an_empty_list(string calculatorId)
    {
        // Declared and empty, not absent. Absence would be indistinguishable from an oversight.
        Assert.Empty(PeriodicFieldCatalog.For(calculatorId));
    }

    [Fact]
    public void The_healthcare_premium_is_the_only_field_stored_monthly()
    {
        var monthly = PeriodicFieldCatalog.CalculatorIds
            .SelectMany(id => PeriodicFieldCatalog.For(id).Select(field => (id, field)))
            .Where(entry => entry.field.StoredPeriod == CurrencyPeriod.Monthly)
            .Select(entry => $"{entry.id}/{entry.field.Key}")
            .OrderBy(name => name, StringComparer.Ordinal);

        Assert.Equal(["healthcare-gap/healthcareMonthlyPremium"], monthly);
    }

    [Fact]
    public void No_calculator_declares_the_same_field_twice()
    {
        foreach (var calculatorId in PeriodicFieldCatalog.CalculatorIds)
        {
            var keys = PeriodicFieldCatalog.For(calculatorId).Select(field => field.Key).ToList();

            Assert.Equal(keys.Count, keys.Distinct(StringComparer.Ordinal).Count());
        }
    }

    [Fact]
    public void A_field_key_always_means_the_same_stored_period_everywhere_it_appears()
    {
        // The three FIRE variants share a field vocabulary. A key that meant annual on one screen and
        // monthly on another would make the shared inventory ambiguous.
        var periodsByKey = PeriodicFieldCatalog.CalculatorIds
            .SelectMany(PeriodicFieldCatalog.For)
            .GroupBy(field => field.Key, StringComparer.Ordinal);

        foreach (var group in periodsByKey)
        {
            Assert.Single(group.Select(field => field.StoredPeriod).Distinct());
        }
    }

    [Fact]
    public void An_unknown_calculator_throws_rather_than_reporting_no_periodic_fields()
    {
        // Returning an empty list would let a typo'd ID look like a calculator with nothing to toggle.
        Assert.Throws<KeyNotFoundException>(() => PeriodicFieldCatalog.For("not-a-calculator"));
    }

    /// <summary>
    /// Exercises the declared stored period through a real field rather than only reading it back as a
    /// string. Without this, flipping the healthcare premium to annual would break only an inventory
    /// listing — the declaration has to be load-bearing for arithmetic or it is just documentation.
    /// </summary>
    [Fact]
    public void A_600_dollar_healthcare_premium_shows_as_7200_a_year()
    {
        var premium = PeriodicFieldCatalog.For("healthcare-gap")
            .Single(field => field.Key == PeriodicFieldCatalog.HealthcareMonthlyPremium);

        var field = new PeriodicAmountField(
            premium.StoredPeriod,
            CurrencyPeriod.Monthly,
            CultureInfo.InvariantCulture);
        field.SetStoredValue(600);

        Assert.Equal("600", field.Text);

        field.SetDisplayPeriod(CurrencyPeriod.Annual);

        // Read the premium as annual and it is 7200. If the catalog claimed the stored value was
        // already annual, this would read 50 — the same 144x error web would make.
        Assert.Equal("7200", field.Text);
        Assert.Equal(600, field.StoredValue);
    }

    /// <summary>
    /// The mirror of the above for an annual-canonical field, so the two stored periods are pinned
    /// against each other rather than each in isolation.
    /// </summary>
    [Fact]
    public void A_60000_dollar_annual_expense_shows_as_5000_a_month()
    {
        var expenses = PeriodicFieldCatalog.For("healthcare-gap")
            .Single(field => field.Key == PeriodicFieldCatalog.HealthcareAnnualDeductible);

        var field = new PeriodicAmountField(
            expenses.StoredPeriod,
            CurrencyPeriod.Annual,
            CultureInfo.InvariantCulture);
        field.SetStoredValue(60_000);

        field.SetDisplayPeriod(CurrencyPeriod.Monthly);

        Assert.Equal("5000", field.Text);
        Assert.Equal(60_000, field.StoredValue);
    }
}
