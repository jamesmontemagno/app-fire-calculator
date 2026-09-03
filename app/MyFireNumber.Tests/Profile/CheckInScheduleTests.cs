using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Profile;

namespace MyFireNumber.Tests.Profile;

public sealed class CheckInScheduleTests
{
    private static FinancialCheckIn CheckIn(string id, double netWorth)
    {
        var accountBalance = Math.Max(netWorth, 0);
        var debtBalance = Math.Max(-netWorth, 0);
        return new FinancialCheckIn(
            id,
            DateTime.UtcNow,
            [new AccountBalanceEntry("account", "Account", RetirementAccountType.Traditional, accountBalance)],
            [new DebtBalanceEntry("debt", "Debt", debtBalance)],
            0,
            0);
    }

    [Fact]
    public void Classify_ReturnsNever_WhenNoCheckInHasEverCompleted()
    {
        Assert.Equal(FreshnessStatus.Never, CheckInSchedule.Classify(null, DateTime.UtcNow));
    }

    [Theory]
    [InlineData(0, FreshnessStatus.UpToDate)]
    [InlineData(15, FreshnessStatus.UpToDate)]
    [InlineData(22, FreshnessStatus.UpToDate)]
    [InlineData(23, FreshnessStatus.DueSoon)]
    [InlineData(29, FreshnessStatus.DueSoon)]
    [InlineData(30, FreshnessStatus.DueSoon)]
    [InlineData(31, FreshnessStatus.Overdue)]
    [InlineData(60, FreshnessStatus.Overdue)]
    public void Classify_UsesThirtyDayCadenceWithASevenDayDueSoonWindow(int daysAgo, FreshnessStatus expected)
    {
        var now = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        var lastCompleted = now.AddDays(-daysAgo);

        Assert.Equal(expected, CheckInSchedule.Classify(lastCompleted, now));
    }

    [Fact]
    public void NextDueUtc_IsThirtyDaysAfterTheLastCompletedCheckIn()
    {
        var lastCompleted = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        Assert.Equal(new DateTime(2025, 1, 31, 0, 0, 0, DateTimeKind.Utc), CheckInSchedule.NextDueUtc(lastCompleted));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(29, 29)]
    public void DaysSince_ReturnsWholeCalendarDaysElapsed(int daysAgo, int expected)
    {
        var now = new DateTime(2025, 6, 15, 18, 30, 0, DateTimeKind.Utc);
        var lastCompleted = now.AddDays(-daysAgo);

        Assert.Equal(expected, CheckInSchedule.DaysSince(lastCompleted, now));
    }

    [Fact]
    public void DaysSince_NeverReturnsNegative_WhenTheCheckInIsInTheFuture()
    {
        var now = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        var futureCheckIn = now.AddDays(5);

        Assert.Equal(0, CheckInSchedule.DaysSince(futureCheckIn, now));
    }

    [Fact]
    public void CompareNetWorth_ReturnsNoTrend_WhenTheFirstCheckInMatchesLiveData()
    {
        var result = CheckInTrend.CompareNetWorth(100_000, [CheckIn("first", 100_000)]);

        Assert.Null(result);
    }

    [Fact]
    public void CompareNetWorth_UsesLatestCheckInForUnsavedLiveChanges()
    {
        var result = CheckInTrend.CompareNetWorth(
            125_000,
            [CheckIn("first", 90_000), CheckIn("latest", 100_000)]);

        Assert.Equal(new NetWorthComparison(25_000, NetWorthComparisonPeriod.LastUpdate), result);
    }

    [Fact]
    public void CompareNetWorth_UsesPreviousCheckInAfterCompletingAnUpdate()
    {
        var result = CheckInTrend.CompareNetWorth(
            125_000,
            [CheckIn("first", 100_000), CheckIn("latest", 125_000)]);

        Assert.Equal(new NetWorthComparison(25_000, NetWorthComparisonPeriod.PreviousUpdate), result);
    }
}
