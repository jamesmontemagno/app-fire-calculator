namespace MyFireNumber.Core.Calculations;

public enum FireQuizLifestyle
{
    Minimal,
    Moderate,
    Comfortable,
    Luxury
}

public enum FireQuizWorkPreference
{
    QuitCompletely,
    PartTime,
    Coast,
    Flexible
}

public enum FireQuizPrimaryGoal
{
    RetireEarly,
    FinancialSecurity,
    MaintainLifestyle,
    Flexibility
}

public sealed record FireQuizAnswers(
    int CurrentAge,
    int RetirementAge,
    double CurrentSavings,
    double AnnualIncome,
    double AnnualExpenses,
    FireQuizLifestyle Lifestyle,
    FireQuizWorkPreference WorkPreference,
    FireQuizPrimaryGoal PrimaryGoal);

public sealed record FireQuizRecommendation(
    string CalculatorId,
    string Title,
    string Reason,
    string Description,
    IReadOnlyList<string> Benefits);

public static class FireQuizRecommender
{
    public static FireQuizRecommendation Recommend(FireQuizAnswers answers)
    {
        var yearsToFire = answers.RetirementAge - answers.CurrentAge;

        if (answers.Lifestyle == FireQuizLifestyle.Minimal
            || (answers.AnnualExpenses < 40_000 && answers.PrimaryGoal == FireQuizPrimaryGoal.RetireEarly))
        {
            return Create(
                "lean-fire",
                "Lean FIRE",
                "Your minimalist lifestyle and lower expenses make Lean FIRE achievable.",
                "Reach financial independence faster through intentional spending and a smaller portfolio target.",
                "Retire earlier than traditional FIRE",
                "Need less total savings",
                "Build intentional spending habits");
        }

        if (answers.Lifestyle == FireQuizLifestyle.Luxury
            || (answers.AnnualExpenses >= 100_000 && answers.PrimaryGoal == FireQuizPrimaryGoal.MaintainLifestyle))
        {
            return Create(
                "fat-fire",
                "Fat FIRE",
                "Your desired lifestyle without compromise aligns with Fat FIRE.",
                "Build a larger portfolio target for travel, flexibility, and additional margin.",
                "Maintain your planned lifestyle",
                "Create a larger market buffer",
                "Preserve room for travel and family goals");
        }

        if (answers.WorkPreference == FireQuizWorkPreference.PartTime
            || (yearsToFire < 10 && answers.PrimaryGoal == FireQuizPrimaryGoal.Flexibility))
        {
            return Create(
                "barista-fire",
                "Barista FIRE",
                "Your interest in part-time work makes Barista FIRE a useful stepping stone.",
                "Blend portfolio withdrawals with part-time income to leave full-time work sooner.",
                "Leave full-time work earlier",
                "Cover part of your expenses with earned income",
                "Keep flexibility and social connection");
        }

        if (answers.WorkPreference == FireQuizWorkPreference.Coast
            || (answers.CurrentAge < 35 && yearsToFire > 20))
        {
            return Create(
                "coast-fire",
                "Coast FIRE",
                "Your age and timeline make compound growth a strategic part of the plan.",
                "Invest aggressively now, then let existing savings grow toward retirement while work covers current expenses.",
                "Front-load retirement savings",
                "Let compound growth do more of the work",
                "Create room for lower-stress work");
        }

        if (answers.PrimaryGoal == FireQuizPrimaryGoal.RetireEarly && yearsToFire < 15)
        {
            return Create(
                "reverse-fire",
                "Reverse FIRE",
                "Your specific retirement date calls for a targeted savings plan.",
                "Work backward from your target age to calculate the contribution required to get there.",
                "Get a clear savings target",
                "Compare timeline and contribution tradeoffs",
                "Track a deadline-driven goal");
        }

        if (answers.PrimaryGoal == FireQuizPrimaryGoal.FinancialSecurity)
        {
            return Create(
                "savings-rate",
                "Savings and Investment Rate",
                "Understanding your savings rate is a strong foundation for financial security.",
                "See how income, contributions, and time combine to grow your invested assets.",
                "Measure your current savings rate",
                "See the impact of saving more",
                "Build a flexible foundation for any FIRE path");
        }

        return Create(
            "standard-fire",
            "Standard FIRE",
            "The classic FIRE approach fits your balanced goals and timeline.",
            "Use the traditional portfolio target and withdrawal-rate framework as a practical starting point.",
            "Use a time-tested planning framework",
            "Balance lifestyle and retirement timing",
            "Adjust assumptions as your plan changes");
    }

    private static FireQuizRecommendation Create(
        string calculatorId,
        string title,
        string reason,
        string description,
        params string[] benefits)
    {
        return new FireQuizRecommendation(calculatorId, title, reason, description, benefits);
    }
}
