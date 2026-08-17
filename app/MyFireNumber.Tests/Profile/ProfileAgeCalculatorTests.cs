using MyFireNumber.Core.Profile;

namespace MyFireNumber.Tests.Profile;

public sealed class ProfileAgeCalculatorTests
{
    [Theory]
    [InlineData(1990, 8, 16, 2026, 8, 16, 36)]
    [InlineData(1990, 8, 17, 2026, 8, 16, 35)]
    [InlineData(2000, 2, 29, 2025, 2, 27, 24)]
    [InlineData(2000, 2, 29, 2025, 2, 28, 25)]
    public void AgeOn_UsesCalendarBirthdayBoundaries(
        int birthYear,
        int birthMonth,
        int birthDay,
        int dateYear,
        int dateMonth,
        int dateDay,
        int expectedAge)
    {
        var age = ProfileAgeCalculator.AgeOn(
            new DateOnly(birthYear, birthMonth, birthDay),
            new DateOnly(dateYear, dateMonth, dateDay));

        Assert.Equal(expectedAge, age);
    }

    [Fact]
    public void TryValidate_RejectsRetirementBeforeBirthDate()
    {
        var profile = FinancialProfile.Empty with
        {
            BirthDate = new DateOnly(1990, 1, 1),
            TargetRetirementDate = new DateOnly(1989, 12, 31)
        };

        var valid = ProfileAgeCalculator.TryValidate(profile, out var message);

        Assert.False(valid);
        Assert.Contains("after the birth date", message);
    }

    [Fact]
    public void TryValidate_RejectsTargetBeforePhasedRetirement()
    {
        var profile = FinancialProfile.Empty with
        {
            BirthDate = new DateOnly(1990, 1, 1),
            PhasedRetirementDate = new DateOnly(2045, 1, 1),
            TargetRetirementDate = new DateOnly(2044, 1, 1)
        };

        var valid = ProfileAgeCalculator.TryValidate(profile, out var message);

        Assert.False(valid);
        Assert.Contains("on or after", message);
    }

    [Fact]
    public void TryValidate_RejectsBirthDateInTheFuture()
    {
        var today = new DateOnly(2026, 8, 16);
        var profile = FinancialProfile.Empty with { BirthDate = today.AddDays(1) };

        var valid = ProfileAgeCalculator.TryValidate(profile, today, out var message);

        Assert.False(valid);
        Assert.Contains("future", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryValidate_AcceptsBirthDateOfToday()
    {
        var today = new DateOnly(2026, 8, 16);
        var profile = FinancialProfile.Empty with { BirthDate = today };

        Assert.True(ProfileAgeCalculator.TryValidate(profile, today, out _));
    }

    [Theory]
    [InlineData(1990, 8, 16, 55, 2045, 8, 16)]
    [InlineData(1990, 12, 31, 65, 2055, 12, 31)]
    [InlineData(2000, 2, 29, 55, 2055, 2, 28)]
    [InlineData(2000, 2, 29, 60, 2060, 2, 29)]
    public void DateAtAge_ReturnsBirthdayInTheTargetYear(
        int birthYear,
        int birthMonth,
        int birthDay,
        int age,
        int expectedYear,
        int expectedMonth,
        int expectedDay)
    {
        var date = ProfileAgeCalculator.DateAtAge(new DateOnly(birthYear, birthMonth, birthDay), age);

        Assert.Equal(new DateOnly(expectedYear, expectedMonth, expectedDay), date);
    }

    [Fact]
    public void DateAtAge_RoundTripsThroughAgeOn()
    {
        var birthDate = new DateOnly(1988, 3, 7);

        var retirementDate = ProfileAgeCalculator.DateAtAge(birthDate, 62);

        Assert.Equal(62, ProfileAgeCalculator.AgeOn(birthDate, retirementDate));
    }

    [Theory]
    [InlineData(30, 30, 90)]
    [InlineData(20, 30, 90)]
    [InlineData(64, 64, 90)]
    public void RetirementAgeRange_StartsAtTheLaterOfTheFloorAndCurrentAge(
        int currentAge,
        int expectedMinimum,
        int expectedMaximum)
    {
        var range = ProfileAgeCalculator.RetirementAgeRange(currentAge, 30, 90);

        Assert.Equal(expectedMinimum, range.Minimum);
        Assert.Equal(expectedMaximum, range.Maximum);
    }

    [Fact]
    public void RetirementAgeRange_NeverInvertsForSomeoneOlderThanTheCeiling()
    {
        var range = ProfileAgeCalculator.RetirementAgeRange(105, 30, 90);

        Assert.Equal(90, range.Minimum);
        Assert.Equal(90, range.Maximum);
        Assert.True(range.Minimum <= range.Maximum);
    }
}
