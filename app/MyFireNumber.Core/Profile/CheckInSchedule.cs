namespace MyFireNumber.Core.Profile;

/// <summary>How fresh a piece of check-in data is relative to the monthly cadence.</summary>
public enum FreshnessStatus
{
    /// <summary>No check-in has ever confirmed this data.</summary>
    Never,

    /// <summary>Confirmed within the current cadence window.</summary>
    UpToDate,

    /// <summary>Approaching the end of the cadence window; a reminder is appropriate.</summary>
    DueSoon,

    /// <summary>Past the cadence window and past due for a new check-in.</summary>
    Overdue
}

/// <summary>
/// The shared monthly cadence used to judge whether Accounts data is fresh. Kept in one place so the
/// Home dashboard, the Accounts overview, and per-item freshness chips can never disagree about what
/// counts as "due soon" versus "overdue".
/// </summary>
public static class CheckInSchedule
{
    /// <summary>Nominal number of days between check-ins.</summary>
    public const int IntervalDays = 30;

    /// <summary>Days before the interval elapses that data is flagged as due soon rather than up to date.</summary>
    public const int DueSoonWindowDays = 7;

    /// <summary>The date the next check-in should happen, based on the last completed one.</summary>
    public static DateTime NextDueUtc(DateTime lastCompletedAtUtc) => lastCompletedAtUtc.AddDays(IntervalDays);

    /// <summary>
    /// Classifies freshness as of <paramref name="nowUtc"/>. Returns <see cref="FreshnessStatus.Never"/>
    /// when <paramref name="lastCompletedAtUtc"/> is null, meaning nothing has ever confirmed this data.
    /// </summary>
    public static FreshnessStatus Classify(DateTime? lastCompletedAtUtc, DateTime nowUtc)
    {
        if (lastCompletedAtUtc is not DateTime lastCompleted)
        {
            return FreshnessStatus.Never;
        }

        var dueDate = NextDueUtc(lastCompleted);
        if (nowUtc > dueDate)
        {
            return FreshnessStatus.Overdue;
        }

        return nowUtc >= dueDate.AddDays(-DueSoonWindowDays)
            ? FreshnessStatus.DueSoon
            : FreshnessStatus.UpToDate;
    }

    /// <summary>Whole days between <paramref name="lastCompletedAtUtc"/> and <paramref name="nowUtc"/>.</summary>
    public static int DaysSince(DateTime lastCompletedAtUtc, DateTime nowUtc) =>
        Math.Max(0, (int)(nowUtc.Date - lastCompletedAtUtc.Date).TotalDays);
}

public enum NetWorthComparisonPeriod
{
    LastUpdate,
    PreviousUpdate
}

public readonly record struct NetWorthComparison(
    double Change,
    NetWorthComparisonPeriod Period);

public static class CheckInTrend
{
    /// <summary>
    /// Compares unsaved live changes with the latest check-in. Immediately after a check-in, when
    /// live net worth matches that snapshot, compares the latest check-in with the preceding one.
    /// </summary>
    public static NetWorthComparison? CompareNetWorth(
        double currentNetWorth,
        IReadOnlyList<FinancialCheckIn> checkIns)
    {
        if (checkIns.Count == 0)
        {
            return null;
        }

        var latest = checkIns[^1];
        if (!MatchesDisplayedCurrency(currentNetWorth, latest.NetWorth))
        {
            return new NetWorthComparison(
                currentNetWorth - latest.NetWorth,
                NetWorthComparisonPeriod.LastUpdate);
        }

        if (checkIns.Count == 1)
        {
            return null;
        }

        return new NetWorthComparison(
            latest.NetWorth - checkIns[^2].NetWorth,
            NetWorthComparisonPeriod.PreviousUpdate);
    }

    private static bool MatchesDisplayedCurrency(double left, double right) =>
        Math.Round(left, 2, MidpointRounding.AwayFromZero)
        == Math.Round(right, 2, MidpointRounding.AwayFromZero);
}
