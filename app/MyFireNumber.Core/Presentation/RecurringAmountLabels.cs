namespace MyFireNumber.Core.Presentation;

/// <summary>Shared labels for recurring currency inputs and their exports.</summary>
public static class RecurringAmountLabels
{
    public const string RetirementSpending = "Expenses";

    public static string RetirementSpendingFor(CurrencyPeriod displayPeriod) =>
        $"{RetirementSpending} ({CurrencyPeriodMath.Qualifier(displayPeriod)})";
}
