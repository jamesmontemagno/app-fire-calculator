using MyFireNumber.Services;

namespace MyFireNumber;

public partial class App : Application
{
	private readonly AppShell appShell;

	public App(AppShell appShell, IThemeService themeService)
	{
		InitializeComponent();
		this.appShell = appShell;
		themeService.Apply(themeService.Preference);
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(appShell);
	}
}