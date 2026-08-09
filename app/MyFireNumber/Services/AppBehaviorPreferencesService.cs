namespace MyFireNumber.Services;

public enum LaunchDestination
{
    Home,
    Calculators,
    Plans
}

public sealed record AppBehaviorPreferences(
    LaunchDestination LaunchDestination,
    bool RestoreDrafts,
    bool ConfirmPlanDeletion,
    bool Haptics,
    bool ReduceMotion,
    bool HighContrast);

public interface IAppBehaviorPreferencesService
{
    AppBehaviorPreferences Current { get; }

    void Save(AppBehaviorPreferences preferences);

    void PerformHaptic();
}

public sealed class AppBehaviorPreferencesService : IAppBehaviorPreferencesService
{
    private const string LaunchDestinationKey = "behavior-launch-destination";
    private const string RestoreDraftsKey = "behavior-restore-drafts";
    private const string ConfirmPlanDeletionKey = "behavior-confirm-plan-deletion";
    private const string HapticsKey = "behavior-haptics";
    private const string ReduceMotionKey = "accessibility-reduce-motion";
    private const string HighContrastKey = "accessibility-high-contrast";

    public AppBehaviorPreferences Current
    {
        get
        {
            var storedDestination = Preferences.Default.Get(LaunchDestinationKey, LaunchDestination.Home.ToString());
            var destination = Enum.TryParse<LaunchDestination>(storedDestination, ignoreCase: true, out var parsed)
                ? parsed
                : LaunchDestination.Home;
            return new(
                destination,
                Preferences.Default.Get(RestoreDraftsKey, true),
                Preferences.Default.Get(ConfirmPlanDeletionKey, true),
                Preferences.Default.Get(HapticsKey, true),
                Preferences.Default.Get(ReduceMotionKey, false),
                Preferences.Default.Get(HighContrastKey, false));
        }
    }

    public void Save(AppBehaviorPreferences preferences)
    {
        Preferences.Default.Set(LaunchDestinationKey, preferences.LaunchDestination.ToString());
        Preferences.Default.Set(RestoreDraftsKey, preferences.RestoreDrafts);
        Preferences.Default.Set(ConfirmPlanDeletionKey, preferences.ConfirmPlanDeletion);
        Preferences.Default.Set(HapticsKey, preferences.Haptics);
        Preferences.Default.Set(ReduceMotionKey, preferences.ReduceMotion);
        Preferences.Default.Set(HighContrastKey, preferences.HighContrast);
    }

    public void PerformHaptic()
    {
        if (Current.Haptics && HapticFeedback.Default.IsSupported)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
    }
}
