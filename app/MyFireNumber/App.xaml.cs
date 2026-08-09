using MyFireNumber.Services;

namespace MyFireNumber;

public partial class App : Application
{
	private readonly AppShell appShell;

	public App(
		AppShell appShell,
		IThemeService themeService,
		IAppBehaviorPreferencesService behaviorPreferencesService)
	{
		InitializeComponent();
		this.appShell = appShell;
		themeService.Apply(behaviorPreferencesService.Current.HighContrast
			? ThemePreference.Dark
			: themeService.Preference);
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(appShell);
	}
}