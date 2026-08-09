using MyFireNumber.Services;
using MyFireNumber.Views;

namespace MyFireNumber;

public partial class AppShell : Shell
{
	private readonly IOnboardingService onboardingService;
	private readonly IAppBehaviorPreferencesService behaviorPreferencesService;
	private bool hasPresentedOnboarding;

	public AppShell(
		HomePage homePage,
		CalculatorsPage calculatorsPage,
		PlansPage plansPage,
		SettingsPage settingsPage,
		IOnboardingService onboardingService,
		IAppBehaviorPreferencesService behaviorPreferencesService)
	{
		InitializeComponent();
		this.onboardingService = onboardingService;
		this.behaviorPreferencesService = behaviorPreferencesService;

		Items.Add(CreateTab("Home", "home", "tab_home.png", homePage));
		Items.Add(CreateTab("Calculators", "calculators", "tab_calculators.png", calculatorsPage));
		Items.Add(CreateTab("Plans", "plans", "tab_plans.png", plansPage));
		Items.Add(CreateTab("Settings", "settings", "tab_settings.png", settingsPage));

		Routing.RegisterRoute("quiz", typeof(QuizPage));
		Routing.RegisterRoute("calculator", typeof(CalculatorDetailPage));
		Routing.RegisterRoute("retirement-annual-details", typeof(RetirementAnnualDetailsPage));
		Loaded += OnLoaded;
	}

	private static Tab CreateTab(string title, string route, string icon, Page page)
	{
		Shell.SetNavBarIsVisible(page, false);

		var shellContent = new ShellContent
		{
			Title = title,
			Route = route,
			Icon = icon,
			ContentTemplate = new DataTemplate(() => page)
		};

		var tab = new Tab { Title = title, Route = route, Icon = icon };
		tab.Items.Add(shellContent);
		return tab;
	}

	private async void OnLoaded(object? sender, EventArgs eventArgs)
	{
		Loaded -= OnLoaded;
		if (hasPresentedOnboarding)
		{
			return;
		}

		hasPresentedOnboarding = true;
		if (!onboardingService.IsComplete)
		{
			await GoToAsync("quiz");
			return;
		}

		var route = behaviorPreferencesService.Current.LaunchDestination switch
		{
			LaunchDestination.Calculators => "//calculators",
			LaunchDestination.Plans => "//plans",
			_ => "//home"
		};
		await GoToAsync(route);
	}
}
