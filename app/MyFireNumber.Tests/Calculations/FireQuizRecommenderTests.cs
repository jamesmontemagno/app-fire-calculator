using MyFireNumber.Core.Calculations;

namespace MyFireNumber.Tests.Calculations;

public sealed class FireQuizRecommenderTests
{
    public static TheoryData<FireQuizAnswers, string> RecommendationCases => new()
    {
        { Answers(lifestyle: FireQuizLifestyle.Minimal), "lean-fire" },
        { Answers(lifestyle: FireQuizLifestyle.Luxury), "fat-fire" },
        { Answers(work: FireQuizWorkPreference.PartTime), "barista-fire" },
        { Answers(work: FireQuizWorkPreference.Coast, timeline: FireQuizTimeline.TwentyPlusYears), "coast-fire" },
        { Answers(timeline: FireQuizTimeline.WithinFiveYears, goal: FireQuizPrimaryGoal.RetireEarly), "reverse-fire" },
        { Answers(goal: FireQuizPrimaryGoal.FinancialSecurity), "standard-fire" },
        {
            Answers(
                lifestyle: FireQuizLifestyle.NotSure,
                work: FireQuizWorkPreference.NotSure,
                timeline: FireQuizTimeline.NotSure,
                goal: FireQuizPrimaryGoal.NotSure),
            "standard-fire"
        }
    };

    [Theory]
    [MemberData(nameof(RecommendationCases))]
    public void Recommend_MatchesRankedPathContract(FireQuizAnswers answers, string expectedCalculatorId)
    {
        var recommendation = FireQuizRecommender.Recommend(answers);

        Assert.Equal(expectedCalculatorId, recommendation.Primary.CalculatorId);
        Assert.NotEmpty(recommendation.Primary.Benefits);
        Assert.Equal(2, recommendation.Alternatives.Count);
    }

    private static FireQuizAnswers Answers(
        double expenses = 60_000,
        FireQuizLifestyle lifestyle = FireQuizLifestyle.Moderate,
        FireQuizWorkPreference work = FireQuizWorkPreference.QuitCompletely,
        FireQuizTimeline timeline = FireQuizTimeline.TenToTwentyYears,
        FireQuizPrimaryGoal goal = FireQuizPrimaryGoal.Flexibility)
    {
        return new FireQuizAnswers(
            lifestyle,
            work,
            timeline,
            goal,
            CurrentSavings: 200_000,
            AnnualIncome: 100_000,
            AnnualExpenses: expenses);
    }
}
