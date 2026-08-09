namespace MyFireNumber.Services;

public interface IOnboardingService
{
    bool IsComplete { get; }

    void Complete();

    void Reset();
}

public sealed class OnboardingService : IOnboardingService
{
    private const string CompletionKey = "onboarding-completed";

    public bool IsComplete => Preferences.Default.Get(CompletionKey, false);

    public void Complete()
    {
        Preferences.Default.Set(CompletionKey, true);
    }

    public void Reset()
    {
        Preferences.Default.Remove(CompletionKey);
    }
}