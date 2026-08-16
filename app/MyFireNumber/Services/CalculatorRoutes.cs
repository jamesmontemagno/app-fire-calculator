namespace MyFireNumber.Services;

/// <summary>
/// Builds Shell routes for calculators. Every calculator owns a route named after
/// its ID. The Standard, Lean, and Fat FIRE variants share one page, so their
/// routes also carry the calculator ID so the page can pick the right view model.
/// </summary>
public static class CalculatorRoutes
{
    /// <summary>Calculator IDs served by the shared FIRE Number page.</summary>
    public static IReadOnlySet<string> SharedFireNumberCalculatorIds { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "standard-fire",
        "lean-fire",
        "fat-fire"
    };

    public static string Build(
        string calculatorId,
        string? planId = null,
        bool returnHomeAfterSave = false,
        string routePrefix = "",
        MyFireNumber.Core.Profile.ScenarioDataMode? dataMode = null)
    {
        var query = new List<string>();
        if (SharedFireNumberCalculatorIds.Contains(calculatorId))
        {
            query.Add($"calculatorId={Uri.EscapeDataString(calculatorId)}");
        }

        if (!string.IsNullOrWhiteSpace(planId))
        {
            query.Add($"planId={Uri.EscapeDataString(planId)}");
        }

        if (returnHomeAfterSave)
        {
            query.Add("returnHomeAfterSave=true");
        }

        if (dataMode is not null)
        {
            query.Add($"dataMode={dataMode}");
        }

        var route = $"{routePrefix}{calculatorId}";
        return query.Count == 0
            ? route
            : $"{route}?{string.Join('&', query)}";
    }
}
