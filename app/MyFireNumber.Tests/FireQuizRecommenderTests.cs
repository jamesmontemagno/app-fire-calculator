using MyFireNumber.Core.Calculations;

namespace MyFireNumber.Tests;

public class FireQuizRecommenderTests
{
    public static TheoryData<string, FireQuizAnswers> PathProfiles => new()
    {
        {
            "standard-fire",
            new(
                FireQuizLifestyle.Moderate,
                FireQuizWorkPreference.QuitCompletely,
                FireQuizTimeline.TenToTwentyYears,
                FireQuizPrimaryGoal.FinancialSecurity)
        },
        {
            "lean-fire",
            new(
                FireQuizLifestyle.Minimal,
                FireQuizWorkPreference.QuitCompletely,
                FireQuizTimeline.FiveToTenYears,
                FireQuizPrimaryGoal.RetireEarly,
                AnnualExpenses: 35_000)
        },
        {
            "fat-fire",
            new(
                FireQuizLifestyle.Luxury,
                FireQuizWorkPreference.QuitCompletely,
                FireQuizTimeline.TenToTwentyYears,
                FireQuizPrimaryGoal.MaintainLifestyle,
                AnnualExpenses: 120_000)
        },
        {
            "barista-fire",
            new(
                FireQuizLifestyle.Moderate,
                FireQuizWorkPreference.PartTime,
                FireQuizTimeline.WithinFiveYears,
                FireQuizPrimaryGoal.Flexibility)
        },
        {
            "coast-fire",
            new(
                FireQuizLifestyle.Moderate,
                FireQuizWorkPreference.Coast,
                FireQuizTimeline.TwentyPlusYears,
                FireQuizPrimaryGoal.Flexibility)
        },
        {
            "reverse-fire",
            new(
                FireQuizLifestyle.Comfortable,
                FireQuizWorkPreference.QuitCompletely,
                FireQuizTimeline.WithinFiveYears,
                FireQuizPrimaryGoal.RetireEarly)
        }
    };

    [Theory]
    [MemberData(nameof(PathProfiles))]
    public void Recommend_AllowsEveryFirePathToRankFirst(string expectedCalculatorId, FireQuizAnswers answers)
    {
        var recommendation = FireQuizRecommender.Recommend(answers);

        Assert.Equal(expectedCalculatorId, recommendation.Primary.CalculatorId);
        Assert.Equal(2, recommendation.Alternatives.Count);
    }

    [Fact]
    public void Recommend_AllUnknownAnswersReturnsBalancedLowConfidenceStart()
    {
        var answers = new FireQuizAnswers(
            FireQuizLifestyle.NotSure,
            FireQuizWorkPreference.NotSure,
            FireQuizTimeline.NotSure,
            FireQuizPrimaryGoal.NotSure);

        var recommendation = FireQuizRecommender.Recommend(answers);

        Assert.Equal("standard-fire", recommendation.Primary.CalculatorId);
        Assert.Equal(FireQuizConfidence.Low, recommendation.Confidence);
        Assert.Contains("balanced-start", recommendation.Primary.ReasonIds);
    }

    [Fact]
    public void Recommend_UsesStableTieBreaking()
    {
        var answers = new FireQuizAnswers(
            FireQuizLifestyle.NotSure,
            FireQuizWorkPreference.Flexible,
            FireQuizTimeline.NotSure,
            FireQuizPrimaryGoal.NotSure);

        var recommendation = FireQuizRecommender.Recommend(answers);

        Assert.Equal("standard-fire", recommendation.Primary.CalculatorId);
        Assert.Equal("coast-fire", recommendation.Alternatives[0].CalculatorId);
        Assert.Equal("barista-fire", recommendation.Alternatives[1].CalculatorId);
    }
}
