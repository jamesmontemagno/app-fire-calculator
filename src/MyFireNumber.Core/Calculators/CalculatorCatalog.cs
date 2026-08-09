namespace MyFireNumber.Core.Calculators;

public sealed record CalculatorDefinition(string Id, string Title, string Summary, string Route);

public interface ICalculatorCatalog
{
    IReadOnlyList<CalculatorDefinition> All { get; }

    CalculatorDefinition GetRequired(string id);
}

public sealed class CalculatorCatalog : ICalculatorCatalog
{
    private static readonly IReadOnlyList<CalculatorDefinition> Definitions =
    [
        new("standard-fire", "Standard FIRE", "Plan the portfolio needed to cover annual spending.", "calculator/standard-fire"),
        new("coast-fire", "Coast FIRE", "Find the balance that can grow to your retirement target.", "calculator/coast-fire"),
        new("lean-fire", "Lean FIRE", "Explore a lower-expense path to financial independence.", "calculator/lean-fire"),
        new("fat-fire", "Fat FIRE", "Model a higher-spending financial independence target.", "calculator/fat-fire"),
        new("barista-fire", "Barista FIRE", "See how part-time income can lower your target portfolio.", "calculator/barista-fire"),
        new("reverse-fire", "Reverse FIRE", "Work backward from your target retirement age.", "calculator/reverse-fire"),
        new("withdrawal-rate", "Withdrawal Rate", "Test portfolio longevity through retirement withdrawals.", "calculator/withdrawal-rate"),
        new("savings-rate", "Savings & Investment Rate", "Project consistent investing and savings rate over time.", "calculator/savings-rate"),
        new("debt-payoff", "Debt Payoff", "Compare snowball and avalanche repayment plans.", "calculator/debt-payoff"),
        new("healthcare-gap", "Healthcare Gap", "Estimate costs before Medicare eligibility.", "calculator/healthcare-gap"),
        new("retirement-cash-flow", "Retirement Cash Flow", "Coordinate accounts, income, expenses, and withdrawals.", "calculator/retirement-cash-flow")
    ];

    public IReadOnlyList<CalculatorDefinition> All => Definitions;

    public CalculatorDefinition GetRequired(string id)
    {
        return Definitions.FirstOrDefault(definition => definition.Id == id)
            ?? throw new KeyNotFoundException($"No calculator is registered with ID '{id}'.");
    }
}