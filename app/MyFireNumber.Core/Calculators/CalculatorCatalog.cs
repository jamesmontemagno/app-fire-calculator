namespace MyFireNumber.Core.Calculators;

public sealed record CalculatorDefinition(string Id, string Title, string Summary, string IconGlyph);

public interface ICalculatorCatalog
{
    IReadOnlyList<CalculatorDefinition> All { get; }

    CalculatorDefinition GetRequired(string id);
}

public sealed class CalculatorCatalog : ICalculatorCatalog
{
    private static readonly IReadOnlyList<CalculatorDefinition> Definitions =
    [
        new("standard-fire", "Standard FIRE", "Calculate your full financial independence target with the classic 25x expenses rule.", "\uf06d"),
        new("coast-fire", "Coast FIRE", "Find what you need invested now so compound growth can carry you to retirement.", "\uf5ca"),
        new("lean-fire", "Lean FIRE", "Reach financial independence sooner with a lower-cost, minimalist lifestyle.", "\uf4d8"),
        new("fat-fire", "Fat FIRE", "Plan for financial independence while maintaining a comfortable lifestyle.", "\uf51e"),
        new("barista-fire", "Barista FIRE", "Blend part-time income with portfolio withdrawals to leave full-time work earlier.", "\uf0f4"),
        new("reverse-fire", "Reverse FIRE", "Choose a target retirement age and find the savings needed to reach it.", "\uf2f9"),
        new("withdrawal-rate", "Withdrawal Rate", "Test how long your portfolio may last while funding retirement spending.", "\uf4c0"),
        new("savings-rate", "Savings & Investment Rate", "See how recurring contributions and compound growth can build wealth over time.", "\uf4c4"),
        new("debt-payoff", "Debt Payoff", "Compare Snowball and Avalanche strategies for eliminating multiple debts.", "\uf09d"),
        new("healthcare-gap", "Healthcare Gap", "Estimate healthcare costs between early retirement and Medicare eligibility.", "\uf0fa"),
        new("retirement-cash-flow", "Retirement Cash Flow", "Coordinate accounts, income, expenses, and withdrawals across retirement.", "\uf1da")
    ];

    public IReadOnlyList<CalculatorDefinition> All => Definitions;

    public CalculatorDefinition GetRequired(string id)
    {
        return Definitions.FirstOrDefault(definition => definition.Id == id)
            ?? throw new KeyNotFoundException($"No calculator is registered with ID '{id}'.");
    }
}