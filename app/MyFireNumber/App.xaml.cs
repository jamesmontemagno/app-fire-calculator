using Microsoft.Extensions.DependencyInjection;
using MyFireNumber.Services;

namespace MyFireNumber;

public partial class App : Application
{
	private readonly IServiceProvider services;

	public App(
		IServiceProvider services,
		IThemeService themeService,
		IAppBehaviorPreferencesService behaviorPreferencesService,
		IPrivacyModePreferencesService privacyModePreferencesService,
		ITemporaryExportCleanupService temporaryExportCleanupService,
		IProfileService profileService)
	{
		InitializeComponent();
		this.services = services;
		temporaryExportCleanupService.RemoveStaleFiles();
		themeService.Apply(behaviorPreferencesService.Current.HighContrast
			? ThemePreference.Dark
			: themeService.Preference);
		privacyModePreferencesService.ApplyStartupOverride();
		_ = profileService.LoadAsync();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		// Resolve AppShell here rather than injecting it into the constructor. Constructor
		// injection forced DI to build AppShell -- and with it the four tab pages -- before
		// InitializeComponent() merged Colors.xaml and Styles.xaml into Application.Resources,
		// so any {StaticResource} in those pages threw "StaticResource not found" at startup.
		return new Window(services.GetRequiredService<AppShell>());
	}
}