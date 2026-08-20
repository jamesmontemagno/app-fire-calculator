using MyFireNumber.Core.Profile;

namespace MyFireNumber.Tests.Profile;

public sealed class CheckInScheduleTests
{
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
}
