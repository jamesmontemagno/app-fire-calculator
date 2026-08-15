namespace MyFireNumber.Core.Presentation;

/// <summary>Shared labels for recurring currency inputs and their exports.</summary>
public static class RecurringAmountLabels
{
    public const string RetirementSpending = "Retirement spending (today’s dollars)";

    public static string RetirementSpendingFor(CurrencyPeriod displayPeriod) =>
        $"{RetirementSpending} ({CurrencyPeriodMath.Qualifier(displayPeriod)})";
}
