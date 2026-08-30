using System.Text.RegularExpressions;

using MyFireNumber.Core.Calculators;

namespace MyFireNumber.Tests.Calculations;

/// <summary>
/// Pins every shipped calculator to a native definition — a Shell route registered under the
/// calculator's ID and pointing at a page type — discovered from <c>app/MyFireNumber/AppShell.xaml.cs</c>.
///
/// <para><b>Why the expected set is discovered rather than listed.</b> This test used to open with
/// <c>Assert.Equal(11, catalog.All.Count)</c>. A cardinality check cannot do the job the test's name
/// claims. Adding a twelfth calculator failed it as "expected 11, got 12", which reads as
/// <i>the test needs updating</i>, so the natural fix was to bump the literal — a rubber stamp that
/// checked nothing about the new calculator. Worse, it could not see a swap at all: drop one
/// calculator, add another, and the count is still 11 and still green with the catalog materially
/// changed. An assertion that is satisfied by editing a number is not a guard.</para>
///
/// <para><b>Why the route table is the oracle.</b> <c>MyFireNumber.Tests</c> cannot reference the MAUI
/// single-project, so the route table is read as text rather than invoked — the same shape as
/// <see cref="Presentation.SharedPeriodicFieldInventoryTests"/> reading <c>shared/parity</c>. It is the
/// right oracle because it is the only place a catalog entry becomes reachable natively: a calculator
/// listed in the catalog with no registered route is a dead tile in the app. Nothing here is
/// enumerated, so a calculator added to one side and forgotten on the other fails on its own.</para>
///
/// <para><b>What this deliberately does not assert.</b> The reverse direction — that every registered
/// route is a calculator — is not checkable, because <c>quiz</c>, <c>welcome</c>, the onboarding
/// routes and <c>retirement-annual-details</c> are legitimately routes without catalog entries, and
/// nothing in the registration distinguishes them.</para>
/// </summary>
public class CalculatorCatalogTests
{
    private static readonly ICalculatorCatalog Catalog = new CalculatorCatalog();
    private static readonly IReadOnlyDictionary<string, string> NativeRoutes = LoadNativeRoutes();

    [Fact]
    public void Every_shipped_calculator_has_a_native_page_route()
    {
        foreach (var definition in Catalog.All)
        {
            Assert.True(
                NativeRoutes.ContainsKey(definition.Id),
                $"Calculator '{definition.Id}' is in the catalog but no Shell route is registered for it in " +
                "app/MyFireNumber/AppShell.xaml.cs, so it is unreachable in the native app. Add " +
                $"Routing.RegisterRoute(\"{definition.Id}\", typeof(SomePage)); alongside the others.");
        }
    }

    [Fact]
    public void Every_shipped_calculator_definition_is_well_formed()
    {
        Assert.Equal(Catalog.All.Count, Catalog.All.Select(definition => definition.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.All(Catalog.All, definition => Assert.False(string.IsNullOrWhiteSpace(definition.Title)));
        Assert.All(Catalog.All, definition => Assert.False(string.IsNullOrWhiteSpace(definition.IconGlyph)));
        Assert.All(Catalog.All, definition => Assert.InRange(definition.Summary.Length, 40, 120));
        Assert.All(Catalog.All, definition => Assert.Same(definition, Catalog.GetRequired(definition.Id)));
    }

    [Fact]
    public void Neither_the_catalog_nor_the_discovered_route_table_is_silently_empty()
    {
        // Guards the guard. Every assertion above is a "for each" over one of these two sets, and a
        // "for each" over an empty set passes. Emptying the catalog, or a regex that silently stopped
        // matching after the route table was reformatted, would turn this file green while checking
        // nothing. Both facts are therefore asserted directly, each pinned by a named member so a
        // non-empty set of the wrong things cannot stand in.
        Assert.NotEmpty(Catalog.All);
        Assert.NotEmpty(NativeRoutes);
        Assert.Contains(Catalog.All, definition => definition.Id == "standard-fire");
        Assert.Equal("FireNumberPage", NativeRoutes["standard-fire"]);
    }

    [Fact]
    public void GetRequired_RejectsUnknownCalculatorIds()
    {
        Assert.Throws<KeyNotFoundException>(() => Catalog.GetRequired("not-a-calculator"));
    }

    [Fact]
    public void Standalone_only_calculators_do_not_support_linked_profiles()
    {
        Assert.False(Catalog.GetRequired("interest-calculator").SupportsLinkedProfile);
        Assert.False(Catalog.GetRequired("withdrawal-rate").SupportsLinkedProfile);
    }

    private static IReadOnlyDictionary<string, string> LoadNativeRoutes()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "NativeRoutes", "AppShell.xaml.cs");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Native route table not found at '{path}'. It is copied from app/MyFireNumber by MyFireNumber.Tests.csproj.",
                path);
        }

        var registrations = Regex.Matches(
            File.ReadAllText(path),
            """Routing\.RegisterRoute\s*\(\s*"(?<route>[^"]+)"\s*,\s*typeof\(\s*(?<page>[\w.]+)\s*\)\s*\)""");

        return registrations.ToDictionary(
            registration => registration.Groups["route"].Value,
            registration => registration.Groups["page"].Value.Split('.')[^1],
            StringComparer.Ordinal);
    }
}
