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
}
