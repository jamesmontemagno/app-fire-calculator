namespace MyFireNumber.Services;

public interface IOnboardingService
{
    bool IsComplete { get; }
    string? RecommendationCalculatorId { get; }

    void Complete();
    void SetRecommendation(string calculatorId);

    void Reset();
}

public sealed class OnboardingService : IOnboardingService
{
    private const string CompletionKey = "onboarding-completed";
    private const string RecommendationKey = "onboarding-recommendation-calculator";

    public bool IsComplete => Preferences.Default.Get(CompletionKey, false);
    public string? RecommendationCalculatorId => Preferences.Default.Get<string?>(RecommendationKey, null);

    public void Complete()
    {
        Preferences.Default.Set(CompletionKey, true);
    }

    public void SetRecommendation(string calculatorId)
    {
        Preferences.Default.Set(RecommendationKey, calculatorId);
    }

    public void Reset()
    {
        Preferences.Default.Remove(CompletionKey);
        Preferences.Default.Remove(RecommendationKey);
    }
}