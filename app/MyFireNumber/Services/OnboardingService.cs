namespace MyFireNumber.Services;

public interface IOnboardingService
{
    bool HasSeenWelcome { get; }
    bool IsComplete { get; }
    string? RecommendationCalculatorId { get; }

    void MarkWelcomeSeen();
    void Complete();
    void SetRecommendation(string calculatorId);

    void Reset();
}

public sealed class OnboardingService : IOnboardingService
{
    private const string WelcomeSeenKey = "onboarding-v2-welcome-seen";
    private const string CompletionKey = "onboarding-v2-completed";
    private const string RecommendationKey = "onboarding-v2-recommendation-calculator";

    public bool HasSeenWelcome => Preferences.Default.Get(WelcomeSeenKey, false);
    public bool IsComplete => Preferences.Default.Get(CompletionKey, false);
    public string? RecommendationCalculatorId => Preferences.Default.Get<string?>(RecommendationKey, null);

    public void MarkWelcomeSeen()
    {
        Preferences.Default.Set(WelcomeSeenKey, true);
    }

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
        Preferences.Default.Remove(WelcomeSeenKey);
        Preferences.Default.Remove(CompletionKey);
        Preferences.Default.Remove(RecommendationKey);
    }
}