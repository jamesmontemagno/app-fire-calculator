using System.Text.Json;

using MyFireNumber.Core.Calculators;
using MyFireNumber.Core.Presentation;

namespace MyFireNumber.Tests.Presentation;

/// <summary>
/// Pins <see cref="PeriodicFieldCatalog"/> to <c>shared/parity/periodic-fields.json</c>, the same file
/// the web suite reads.
///
/// <para><b>Why this exists.</b> No unit test in this project can prove a XAML <c>Entry</c> is bound to
/// a period-aware field: <c>MyFireNumber.Tests</c> cannot reference the MAUI single-project, so a page
/// wired on one platform and forgotten on the other is invisible to both suites. Comparing each side
/// against one shared artifact makes that omission fail on the platform that has it wrong instead of
/// leaving it to review.</para>
///
/// <para><b>What is deliberately not here.</b> No conversion arithmetic. A case asserting
/// <c>50000 / 12 = 4166.67</c> would restate the implementation rather than check it, which is the
/// screenshot test <c>shared/parity/README.md</c> forbids. Conversion behaviour is pinned in
/// <see cref="CurrencyPeriodMathTests"/> and <see cref="PeriodicAmountFieldTests"/> against a
/// lossless-round-trip invariant, which is a property the implementation could actually fail.</para>
/// </summary>
public class SharedPeriodicFieldInventoryTests
{
    private static readonly ICalculatorCatalog Calculators = new CalculatorCatalog();
    private static readonly IReadOnlyList<SharedCalculator> Shared = LoadShared();

    [Fact]
    public void The_shared_inventory_covers_exactly_the_shipped_calculators()
    {
        var shipped = Calculators.All.Select(definition => definition.Id).OrderBy(id => id, StringComparer.Ordinal);
        var declared = Shared.Select(calculator => calculator.Id).OrderBy(id => id, StringComparer.Ordinal);

        Assert.Equal(shipped, declared);
    }

    [Fact]
    public void Every_calculator_declares_the_shared_periodic_fields_in_the_shared_periods()
    {
        foreach (var calculator in Shared)
        {
            var expected = calculator.Fields
                .Select(field => $"{field.Key}:{field.StoredPeriod}")
                .OrderBy(entry => entry, StringComparer.Ordinal);

            var actual = PeriodicFieldCatalog.For(calculator.Id)
                .Select(field => $"{field.Key}:{Serialize(field.StoredPeriod)}")
                .OrderBy(entry => entry, StringComparer.Ordinal);

            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void The_shared_inventory_is_not_silently_empty()
    {
        // Guards the guard. If the artifact failed to load, or its shape changed so every field list
        // parsed as empty, the comparison above would pass against a catalog that had also been
        // emptied -- two blank lists agreeing is indistinguishable from success. These are the two
        // facts that make the comparison meaningful, so they are asserted directly.
        Assert.Contains(Shared, calculator => calculator.Fields.Count > 0);
        Assert.Contains(
            Shared.SelectMany(calculator => calculator.Fields),
            field => field.StoredPeriod == "monthly");
    }

    [Fact]
    public void Every_shared_stored_period_is_one_this_app_can_represent()
    {
        var allowed = Enum.GetValues<CurrencyPeriod>().Select(Serialize).ToHashSet(StringComparer.Ordinal);

        foreach (var field in Shared.SelectMany(calculator => calculator.Fields))
        {
            Assert.Contains(field.StoredPeriod, allowed);
        }
    }

    private static string Serialize(CurrencyPeriod period) =>
        period.Validated(nameof(period)) == CurrencyPeriod.Monthly ? "monthly" : "annual";

    private static IReadOnlyList<SharedCalculator> LoadShared()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "SharedParity", "periodic-fields.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Shared periodic field inventory not found at '{path}'. It is copied from shared/parity by MyFireNumber.Tests.csproj.",
                path);
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));

        return document.RootElement.GetProperty("calculators").EnumerateArray()
            .Select(calculator => new SharedCalculator(
                calculator.GetProperty("id").GetString()!,
                calculator.GetProperty("fields").EnumerateArray()
                    .Select(field => new SharedField(
                        field.GetProperty("key").GetString()!,
                        field.GetProperty("storedPeriod").GetString()!))
                    .ToList()))
            .ToList();
    }

    private sealed record SharedCalculator(string Id, IReadOnlyList<SharedField> Fields);

    private sealed record SharedField(string Key, string StoredPeriod);
}
