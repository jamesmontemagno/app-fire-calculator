namespace MyFireNumber.Core.Presentation;

/// <summary>
/// Remembers which calculators the user opened the advanced assumptions section on, for the
/// lifetime of one app run.
/// </summary>
/// <remarks>
/// <para>Calculator pages are registered <c>AddTransient</c> and reached through Shell routes, so
/// every navigation builds a brand-new page and a brand-new expander. The disclosure state cannot
/// live on the page: by the time the user comes back, the object that knew it has been collected.
/// This is the thing that outlives the page.</para>
/// <para>Deliberately in memory and deliberately not in SQLite next to the drafts. Collapsed on
/// first visit is load-bearing — it is what keeps the results reachable without a long scroll on a
/// phone — so a section opened once should not still be open on a relaunch weeks later. Within one
/// run, reopening a calculator you just expanded should not forget what you did.</para>
/// </remarks>
public interface IAdvancedAssumptionsSessionState
{
    /// <summary>Whether <paramref name="calculatorId"/> was left expanded earlier in this app run.</summary>
    bool IsExpanded(string calculatorId);

    /// <summary>Records the disclosure state the user just chose for <paramref name="calculatorId"/>.</summary>
    void SetExpanded(string calculatorId, bool isExpanded);
}

/// <inheritdoc cref="IAdvancedAssumptionsSessionState" />
public sealed class AdvancedAssumptionsSessionState : IAdvancedAssumptionsSessionState
{
    /// <summary>
    /// Only the expanded ids are held, so the set is bounded by the number of calculators the user
    /// actually expanded — never more than the catalog — and it stores short strings rather than
    /// any reference back to a page or view model.
    /// </summary>
    private readonly HashSet<string> expandedCalculatorIds = new(StringComparer.Ordinal);

    private readonly object gate = new();

    public bool IsExpanded(string calculatorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calculatorId);

        lock (gate)
        {
            return expandedCalculatorIds.Contains(calculatorId);
        }
    }

    public void SetExpanded(string calculatorId, bool isExpanded)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calculatorId);

        lock (gate)
        {
            if (isExpanded)
            {
                expandedCalculatorIds.Add(calculatorId);
            }
            else
            {
                expandedCalculatorIds.Remove(calculatorId);
            }
        }
    }
}
