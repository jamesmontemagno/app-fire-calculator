using MyFireNumber.Core.Calculators;

namespace MyFireNumber.Tests.Calculations;

public class CalculatorCatalogTests
{
    [Fact]
    public void AllWebCalculators_HaveStableNativeDefinitions()
    {
        ICalculatorCatalog catalog = new CalculatorCatalog();

        Assert.Equal(11, catalog.All.Count);
        Assert.Equal(catalog.All.Count, catalog.All.Select(definition => definition.Id).Distinct().Count());
        Assert.All(catalog.All, definition => Assert.Equal($"calculator/{definition.Id}", definition.Route));
        Assert.All(catalog.All, definition => Assert.False(string.IsNullOrWhiteSpace(definition.IconGlyph)));
        Assert.All(catalog.All, definition => Assert.InRange(definition.Summary.Length, 40, 120));
    }

    [Fact]
    public void GetRequired_RejectsUnknownCalculatorIds()
    {
        ICalculatorCatalog catalog = new CalculatorCatalog();

        Assert.Throws<KeyNotFoundException>(() => catalog.GetRequired("not-a-calculator"));
    }
}