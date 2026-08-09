namespace MyFireNumber.Tests;

public class FinancialValueConventionTests
{
    [Fact]
    public void SevenPercent_IsStoredAsDecimal()
    {
        const decimal storedRate = 0.07m;

        Assert.Equal(7m, storedRate * 100m);
    }
}
