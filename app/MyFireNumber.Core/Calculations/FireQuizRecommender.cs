namespace MyFireNumber.Core.Calculations;

public enum FireQuizLifestyle
{
    Minimal,
    Moderate,
    Comfortable,
    Luxury,
    NotSure
}

public enum FireQuizWorkPreference
{
    QuitCompletely,
    PartTime,
    Coast,
    Flexible,
    NotSure
}

public enum FireQuizTimeline
{
    WithinFiveYears,
    FiveToTenYears,
    TenToTwentyYears,
    TwentyPlusYears,
    NotSure
}

public enum FireQuizPrimaryGoal
{
    RetireEarly,
    FinancialSecurity,
    MaintainLifestyle,
    Flexibility,
    NotSure
}

public enum FireQuizConfidence
{
    Low,
    Medium,
    High
}

public sealed record FireQuizAnswers(
    FireQuizLifestyle Lifestyle,
    FireQuizWorkPreference WorkPreference,
    FireQuizTimeline Timeline,
    FireQuizPrimaryGoal PrimaryGoal,
    int? CurrentAge = null,
    int? RetirementAge = null,
    double? CurrentSavings = null,
    double? AnnualIncome = null,
    double? AnnualExpenses = null);

public sealed record FireQuizMatch(
    string CalculatorId,
    string Title,
    string Reason,
    string Description,
    IReadOnlyList<string> Benefits,
    IReadOnlyList<string> ReasonIds,
    int Score);

public sealed record FireQuizRecommendation(
    FireQuizMatch Primary,
    IReadOnlyList<FireQuizMatch> Alternatives,
    FireQuizConfidence Confidence);

public static class FireQuizRecommender
{
    private static readonly string[] TieBreakOrder =
    [
        "standard-fire",
        "coast-fire",
        "barista-fire",
        "reverse-fire",
        "lean-fire",
        "fat-fire"
    ];

    private static readonly IReadOnlyDictionary<string, PathDefinition> Definitions =
        new Dictionary<string, PathDefinition>
        {
            ["standard-fire"] = new(
                "Standard FIRE",
                "Build a complete retirement portfolio using a balanced spending and withdrawal plan.",
                ["Plan for full financial independence", "Balance lifestyle and timing", "Adjust assumptions as life changes"]),
            ["lean-fire"] = new(
                "Lean FIRE",
                "Reach financial independence with intentional spending and a smaller portfolio target.",
                ["Lower the portfolio target", "Prioritize an earlier timeline", "Keep spending intentional"]),
            ["fat-fire"] = new(
                "Fat FIRE",
                "Build a larger portfolio target to support more spending and additional financial margin.",
                ["Maintain a higher-spending lifestyle", "Create a larger market buffer", "Preserve room for travel and family goals"]),
            ["barista-fire"] = new(
                "Barista FIRE",
                "Blend portfolio income with part-time work to leave full-time work sooner.",
                ["Leave full-time work earlier", "Cover some expenses with earned income", "Keep flexibility and social connection"]),
            ["coast-fire"] = new(
                "Coast FIRE",
                "Let invested savings grow toward retirement while current work covers today's expenses.",
                ["Front-load retirement savings", "Give compound growth more time", "Create room for lower-stress work"]),
            ["reverse-fire"] = new(
                "Reverse FIRE",
                "Work backward from a target timeline to find the savings required to reach it.",
                ["Set a clear savings target", "Compare timeline and contribution tradeoffs", "Plan around a firm deadline"])
        };

    private static readonly IReadOnlyDictionary<string, string> ReasonText =
        new Dictionary<string, string>
        {
            ["balanced-lifestyle"] = "your balanced lifestyle goal",
            ["comfortable-lifestyle"] = "your preference for comfort without the highest spending target",
            ["full-retirement"] = "your goal of fully leaving paid work",
            ["security-first"] = "your focus on long-term financial security",
            ["balanced-timeline"] = "your flexible mid-to-long-term timeline",
            ["minimal-lifestyle"] = "your lower-spending lifestyle",
            ["early-priority"] = "your priority to reach financial independence sooner",
            ["lower-expenses"] = "the lower annual expenses you shared",
            ["luxury-lifestyle"] = "your higher-spending lifestyle goal",
            ["maintain-lifestyle"] = "your priority to preserve your lifestyle",
            ["higher-expenses"] = "the higher annual expenses you shared",
            ["part-time-work"] = "your interest in part-time work",
            ["work-flexibility"] = "your desire to keep work options open",
            ["near-term-transition"] = "your near-term transition goal",
            ["coast-work"] = "your preference to let investments grow while work covers current expenses",
            ["long-horizon"] = "your longer time horizon",
            ["deadline-focus"] = "your firm, near-term timeline",
            ["retire-early"] = "your goal to retire as early as practical",
            ["balanced-start"] = "a balanced starting point while you refine your preferences"
        };

    public static FireQuizRecommendation Recommend(FireQuizAnswers answers)
    {
        var candidates = Definitions.ToDictionary(
            pair => pair.Key,
            pair => new Candidate(pair.Key, pair.Value));

        Add(candidates, "standard-fire", 1, "balanced-start");

        switch (answers.Lifestyle)
        {
            case FireQuizLifestyle.Minimal:
                Add(candidates, "lean-fire", 6, "minimal-lifestyle");
                break;
            case FireQuizLifestyle.Moderate:
                Add(candidates, "standard-fire", 4, "balanced-lifestyle");
                break;
            case FireQuizLifestyle.Comfortable:
                Add(candidates, "standard-fire", 3, "comfortable-lifestyle");
                Add(candidates, "fat-fire", 1, "comfortable-lifestyle");
                break;
            case FireQuizLifestyle.Luxury:
                Add(candidates, "fat-fire", 6, "luxury-lifestyle");
                break;
        }

        switch (answers.WorkPreference)
        {
            case FireQuizWorkPreference.QuitCompletely:
                Add(candidates, "standard-fire", 2, "full-retirement");
                Add(candidates, "reverse-fire", 1, "full-retirement");
                break;
            case FireQuizWorkPreference.PartTime:
                Add(candidates, "barista-fire", 7, "part-time-work");
                break;
            case FireQuizWorkPreference.Coast:
                Add(candidates, "coast-fire", 7, "coast-work");
                break;
            case FireQuizWorkPreference.Flexible:
                Add(candidates, "barista-fire", 2, "work-flexibility");
                Add(candidates, "coast-fire", 2, "work-flexibility");
                Add(candidates, "standard-fire", 1, "work-flexibility");
                break;
        }

        switch (answers.Timeline)
        {
            case FireQuizTimeline.WithinFiveYears:
                Add(candidates, "reverse-fire", 5, "deadline-focus");
                Add(candidates, "barista-fire", 2, "near-term-transition");
                Add(candidates, "lean-fire", 1, "near-term-transition");
                break;
            case FireQuizTimeline.FiveToTenYears:
                Add(candidates, "reverse-fire", 3, "deadline-focus");
                Add(candidates, "barista-fire", 1, "near-term-transition");
                Add(candidates, "lean-fire", 1, "near-term-transition");
                break;
            case FireQuizTimeline.TenToTwentyYears:
                Add(candidates, "standard-fire", 2, "balanced-timeline");
                Add(candidates, "coast-fire", 1, "long-horizon");
                break;
            case FireQuizTimeline.TwentyPlusYears:
                Add(candidates, "coast-fire", 3, "long-horizon");
                Add(candidates, "standard-fire", 1, "balanced-timeline");
                break;
        }

        switch (answers.PrimaryGoal)
        {
            case FireQuizPrimaryGoal.RetireEarly:
                Add(candidates, "reverse-fire", 4, "retire-early");
                Add(candidates, "lean-fire", 2, "early-priority");
                break;
            case FireQuizPrimaryGoal.FinancialSecurity:
                Add(candidates, "standard-fire", 4, "security-first");
                break;
            case FireQuizPrimaryGoal.MaintainLifestyle:
                Add(candidates, "fat-fire", 2, "maintain-lifestyle");
                Add(candidates, "standard-fire", 2, "maintain-lifestyle");
                break;
            case FireQuizPrimaryGoal.Flexibility:
                Add(candidates, "coast-fire", 2, "work-flexibility");
                Add(candidates, "barista-fire", 2, "work-flexibility");
                break;
        }

        if (answers.AnnualExpenses is < 40_000)
        {
            Add(candidates, "lean-fire", 2, "lower-expenses");
        }
        else if (answers.AnnualExpenses is >= 100_000)
        {
            Add(candidates, "fat-fire", 3, "higher-expenses");
        }

        var ranked = candidates.Values
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => Array.IndexOf(TieBreakOrder, candidate.CalculatorId))
            .Select(ToMatch)
            .ToArray();

        var knownCoreAnswers = new[]
        {
            answers.Lifestyle != FireQuizLifestyle.NotSure,
            answers.WorkPreference != FireQuizWorkPreference.NotSure,
            answers.Timeline != FireQuizTimeline.NotSure,
            answers.PrimaryGoal != FireQuizPrimaryGoal.NotSure
        }.Count(value => value);
        var margin = ranked[0].Score - ranked[1].Score;
        var confidence = knownCoreAnswers >= 4 && margin >= 3
            ? FireQuizConfidence.High
            : knownCoreAnswers >= 2 && margin >= 2
                ? FireQuizConfidence.Medium
                : FireQuizConfidence.Low;

        return new FireQuizRecommendation(ranked[0], ranked.Skip(1).Take(2).ToArray(), confidence);
    }

    private static void Add(
        IDictionary<string, Candidate> candidates,
        string calculatorId,
        int score,
        string reasonId)
    {
        var candidate = candidates[calculatorId];
        candidate.Score += score;
        if (!candidate.ReasonIds.Contains(reasonId))
        {
            candidate.ReasonIds.Add(reasonId);
        }
    }

    private static FireQuizMatch ToMatch(Candidate candidate)
    {
        var meaningfulReasons = candidate.ReasonIds
            .Where(reasonId => reasonId != "balanced-start" || candidate.ReasonIds.Count == 1)
            .Take(2)
            .Select(reasonId => ReasonText[reasonId])
            .ToArray();
        var reason = meaningfulReasons.Length switch
        {
            0 => $"This is a useful path to compare with your leading match.",
            1 => $"This path aligns with {meaningfulReasons[0]}.",
            _ => $"This path aligns with {meaningfulReasons[0]} and {meaningfulReasons[1]}."
        };

        return new FireQuizMatch(
            candidate.CalculatorId,
            candidate.Definition.Title,
            reason,
            candidate.Definition.Description,
            candidate.Definition.Benefits,
            candidate.ReasonIds.ToArray(),
            candidate.Score);
    }

    private sealed record PathDefinition(
        string Title,
        string Description,
        IReadOnlyList<string> Benefits);

    private sealed class Candidate(string calculatorId, PathDefinition definition)
    {
        public string CalculatorId { get; } = calculatorId;
        public PathDefinition Definition { get; } = definition;
        public int Score { get; set; }
        public List<string> ReasonIds { get; } = [];
    }
}
