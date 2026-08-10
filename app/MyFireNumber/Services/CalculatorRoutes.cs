namespace MyFireNumber.Services;

/// <summary>
/// Builds Shell routes for calculators. Calculators that have been split into
/// their own page get a dedicated route; the rest fall back to the shared
/// <c>calculator</c> detail route until they are migrated.
/// </summary>
public static class CalculatorRoutes
{
    /// <summary>Calculator IDs that own a dedicated page and view model.</summary>
    public static IReadOnlySet<string> DedicatedRouteCalculatorIds { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "withdrawal-rate",
        "savings-rate",
        "healthcare-gap"
    };

    public static string Build(
        string calculatorId,
        string? planId = null,
        bool returnHomeAfterSave = false,
        string routePrefix = "")
    {
        var hasDedicatedRoute = DedicatedRouteCalculatorIds.Contains(calculatorId);
        var route = hasDedicatedRoute
            ? $"{routePrefix}{calculatorId}"
            : $"{routePrefix}calculator?calculatorId={Uri.EscapeDataString(calculatorId)}";

        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(planId))
        {
            query.Add($"planId={Uri.EscapeDataString(planId)}");
        }

        if (returnHomeAfterSave)
        {
            query.Add("returnHomeAfterSave=true");
        }

        if (query.Count == 0)
        {
            return route;
        }

        var separator = hasDedicatedRoute ? '?' : '&';
        return $"{route}{separator}{string.Join('&', query)}";
    }
}
