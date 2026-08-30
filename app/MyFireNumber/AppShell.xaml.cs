using MyFireNumber.Services;
using MyFireNumber.Views;

namespace MyFireNumber;

public partial class AppShell : Shell
{
	private readonly IOnboardingService onboardingService;
	private readonly IAppBehaviorPreferencesService behaviorPreferencesService;
	private readonly IProfileService profileService;
	private bool hasPresentedOnboarding;

	public AppShell(
		HomePage homePage,
		AccountsPage accountsPage,
		CalculatorsPage calculatorsPage,
		PlansPage plansPage,
		ProfilePage profilePage,
		IOnboardingService onboardingService,
		IAppBehaviorPreferencesService behaviorPreferencesService,
		IProfileService profileService)
	{
		InitializeComponent();
		this.onboardingService = onboardingService;
		this.behaviorPreferencesService = behaviorPreferencesService;
		this.profileService = profileService;

		Items.Add(CreateTab("Home", "home", "tab_home.png", homePage));
		Items.Add(CreateTab("Accounts", "accounts", "tab_accounts.png", accountsPage));
		Items.Add(CreateTab("Calculators", "calculators", "tab_calculators.png", calculatorsPage));
		Items.Add(CreateTab("Plans", "plans", "tab_plans.png", plansPage));

		// Profile keeps the platform nav bar so its Settings action sits in the standard toolbar
		// position rather than being a button drawn inside the scrolling content.
		Items.Add(CreateTab("Profile", "profile", "tab_profile.png", profilePage, showNavBar: true));
		Routing.RegisterRoute("settings", typeof(SettingsPage));
		Routing.RegisterRoute("accounts-check-in", typeof(AccountsCheckInPage));
		Routing.RegisterRoute("accounts-history", typeof(AccountsHistoryPage));
		Routing.RegisterRoute("account-item-detail", typeof(AccountItemDetailPage));

		Routing.RegisterRoute("quiz", typeof(QuizPage));
		Routing.RegisterRoute("welcome", typeof(WelcomePage));
		Routing.RegisterRoute("onboarding-defaults", typeof(DefaultsOnboardingPage));
		Routing.RegisterRoute("onboarding-timeline", typeof(TimelineOnboardingPage));
		Routing.RegisterRoute("onboarding-withdrawal-rate", typeof(WithdrawalRateOnboardingPage));
		Routing.RegisterRoute("onboarding-choice", typeof(OnboardingChoicePage));
		Routing.RegisterRoute("standard-fire", typeof(FireNumberPage));
		Routing.RegisterRoute("lean-fire", typeof(FireNumberPage));
		Routing.RegisterRoute("fat-fire", typeof(FireNumberPage));
		Routing.RegisterRoute("withdrawal-rate", typeof(WithdrawalRatePage));
		Routing.RegisterRoute("savings-rate", typeof(SavingsInvestmentPage));
		Routing.RegisterRoute("healthcare-gap", typeof(HealthcareGapPage));
		Routing.RegisterRoute("sepp-72t", typeof(SeppPage));
		Routing.RegisterRoute("roth-conversion", typeof(RothConversionPage));
		Routing.RegisterRoute("coast-fire", typeof(CoastFirePage));
		Routing.RegisterRoute("barista-fire", typeof(BaristaFirePage));
		Routing.RegisterRoute("reverse-fire", typeof(ReverseFirePage));
		Routing.RegisterRoute("debt-payoff", typeof(DebtPayoffPage));
		Routing.RegisterRoute("interest-calculator", typeof(InterestCalculatorPage));
		Routing.RegisterRoute("retirement-cash-flow", typeof(RetirementCashFlowPage));
		Routing.RegisterRoute("retirement-annual-details", typeof(RetirementAnnualDetailsPage));
		Loaded += OnLoaded;
	}

	private static Tab CreateTab(string title, string route, string icon, Page page, bool showNavBar = false)
	{
		Shell.SetNavBarIsVisible(page, showNavBar);

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
		await profileService.LoadAsync();
		if (!onboardingService.IsComplete)
		{
			await GoToAsync(onboardingService.HasSeenWelcome
				? "onboarding-defaults"
				: "welcome");
			return;
		}

		var route = behaviorPreferencesService.Current.LaunchDestination switch
		{
			LaunchDestination.Accounts => "//accounts",
			LaunchDestination.Calculators => "//calculators",
			LaunchDestination.Plans => "//plans",
			_ => "//home"
		};
		await GoToAsync(route);
	}

	protected override void OnNavigating(ShellNavigatingEventArgs args)
	{
		base.OnNavigating(args);

		if (args.Source != ShellNavigationSource.Pop
			|| onboardingService.IsComplete
			|| !IsOnboardingPage(CurrentPage))
		{
			return;
		}

		var navigationStack = Navigation.NavigationStack;
		var hasEarlierOnboardingPage = navigationStack
			.Take(Math.Max(0, navigationStack.Count - 1))
			.Any(IsOnboardingPage);

		if (!hasEarlierOnboardingPage)
		{
			args.Cancel();
		}
	}

	private static bool IsOnboardingPage(Page page) =>
		page is WelcomePage
			or DefaultsOnboardingPage
			or TimelineOnboardingPage
			or WithdrawalRateOnboardingPage
			or OnboardingChoicePage
			or QuizPage;
}
