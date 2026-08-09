using MyFireNumber.Core.Calculations;

namespace MyFireNumber.Tests.Calculations;

public sealed class FireQuizRecommenderTests
{
    public static TheoryData<FireQuizAnswers, string> RecommendationCases => new()
    {
        { Answers(lifestyle: FireQuizLifestyle.Minimal), "lean-fire" },
        { Answers(lifestyle: FireQuizLifestyle.Luxury), "fat-fire" },
        { Answers(work: FireQuizWorkPreference.PartTime), "barista-fire" },
        { Answers(currentAge: 25, retirementAge: 60, work: FireQuizWorkPreference.Flexible), "coast-fire" },
        { Answers(currentAge: 45, retirementAge: 55, goal: FireQuizPrimaryGoal.RetireEarly), "reverse-fire" },
        { Answers(goal: FireQuizPrimaryGoal.FinancialSecurity), "savings-rate" },
        { Answers(), "standard-fire" }
    };

    [Theory]
    [MemberData(nameof(RecommendationCases))]
    public void Recommend_MatchesWebRulePriority(FireQuizAnswers answers, string expectedCalculatorId)
    {
        var recommendation = FireQuizRecommender.Recommend(answers);

        Assert.Equal(expectedCalculatorId, recommendation.CalculatorId);
        Assert.NotEmpty(recommendation.Benefits);
    }

    private static FireQuizAnswers Answers(
        int currentAge = 40,
        int retirementAge = 60,
        double expenses = 60_000,
        FireQuizLifestyle lifestyle = FireQuizLifestyle.Moderate,
        FireQuizWorkPreference work = FireQuizWorkPreference.QuitCompletely,
        FireQuizPrimaryGoal goal = FireQuizPrimaryGoal.Flexibility)
    {
        return new FireQuizAnswers(
            currentAge,
            retirementAge,
            CurrentSavings: 200_000,
            AnnualIncome: 100_000,
            expenses,
            lifestyle,
            work,
            goal);
    }
}
