using Microsoft.Extensions.Logging;
using LiveChartsCore.SkiaSharpView.Maui;
using MyFireNumber.Core.Calculators;
using MyFireNumber.Services;
using MyFireNumber.Storage;
using MyFireNumber.ViewModels;
using MyFireNumber.Views;
using SkiaSharp.Views.Maui.Controls.Hosting;
#if IOS
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
#endif
#if MAUI_DEVFLOW && !IOS
using Microsoft.Maui.DevFlow.Agent;
#endif

namespace MyFireNumber;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
#if IOS
		SearchBarHandler.Mapper.AppendToMapping(
			nameof(SearchBar.Background),
			static (handler, _) =>
			{
				if (handler.VirtualView is SearchBar searchBar &&
					searchBar.Background is SolidColorBrush brush)
				{
					handler.PlatformView.SearchTextField.BackgroundColor = brush.Color.ToPlatform();
				}
			});
#endif

		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseSkiaSharp()
			.UseLiveCharts()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				fonts.AddFont("fa-solid-900.ttf", "FontAwesomeSolid");
			});

		builder.Services.AddSingleton<ICalculatorCatalog, CalculatorCatalog>();
		builder.Services.AddSingleton<IConfirmationService, ConfirmationService>();
		builder.Services.AddSingleton<IAppResetService, AppResetService>();
		builder.Services.AddSingleton<IAppDataTransferService, AppDataTransferService>();
		builder.Services.AddSingleton<ICalculatorDefaultsService, CalculatorDefaultsService>();
		builder.Services.AddSingleton<IAppBehaviorPreferencesService, AppBehaviorPreferencesService>();
		builder.Services.AddSingleton<ICurrencyPreferencesService, CurrencyPreferencesService>();
		builder.Services.AddSingleton<ITemporaryExportCleanupService, TemporaryExportCleanupService>();
		builder.Services.AddSingleton<IErrorPresentationService, ErrorPresentationService>();
		builder.Services.AddSingleton<IExternalLinkService, ExternalLinkService>();
		builder.Services.AddSingleton<IPlanNamePromptService, PlanNamePromptService>();
		builder.Services.AddSingleton<IOnboardingService, OnboardingService>();
		builder.Services.AddSingleton<IThemeService, ThemeService>();
		builder.Services.AddSingleton<INavigationService, ShellNavigationService>();
		builder.Services.AddSingleton<IBaristaFireExportService, BaristaFireExportService>();
		builder.Services.AddSingleton<ICoastFireExportService, CoastFireExportService>();
		builder.Services.AddSingleton<IDebtPayoffExportService, DebtPayoffExportService>();
		builder.Services.AddSingleton<IDeferredCompensationExportService, DeferredCompensationExportService>();
		builder.Services.AddSingleton<IFatFireExportService, FatFireExportService>();
		builder.Services.AddSingleton<IHealthcareGapExportService, HealthcareGapExportService>();
		builder.Services.AddSingleton<ILeanFireExportService, LeanFireExportService>();
		builder.Services.AddSingleton<IReverseFireExportService, ReverseFireExportService>();
		builder.Services.AddSingleton<ISavingsInvestmentExportService, SavingsInvestmentExportService>();
		builder.Services.AddSingleton<IStandardFireExportService, StandardFireExportService>();
		builder.Services.AddSingleton<IWithdrawalRateExportService, WithdrawalRateExportService>();
		builder.Services.AddSingleton(_ => new LocalDatabase(Path.Combine(FileSystem.AppDataDirectory, "my-fire-number.db3")));
		builder.Services.AddSingleton<IDraftRepository, SqliteDraftRepository>();
		builder.Services.AddSingleton<IPlanRepository, SqlitePlanRepository>();
		builder.Services.AddSingleton<ICalculatorPreferencesRepository, SqliteCalculatorPreferencesRepository>();
		builder.Services.AddSingleton<IRecentActivityRepository, SqliteRecentActivityRepository>();
		builder.Services.AddSingleton<ICorruptPayloadRepository, SqliteCorruptPayloadRepository>();
		builder.Services.AddSingleton<ILocalDataArchiveRepository, SqliteLocalDataArchiveRepository>();
		builder.Services.AddSingleton<AppShell>();
		builder.Services.AddSingleton<App>();
		builder.Services.AddSingleton<HomePage>();
		builder.Services.AddSingleton<CalculatorsPage>();
		builder.Services.AddSingleton<PlansPage>();
		builder.Services.AddSingleton<SettingsPage>();
		builder.Services.AddTransient<CalculatorDetailPage>();
		builder.Services.AddTransient<QuizPage>();
		builder.Services.AddTransient<WelcomePage>();
		builder.Services.AddTransient<DefaultsOnboardingPage>();
		builder.Services.AddTransient<OnboardingChoicePage>();
		builder.Services.AddTransient<RetirementAnnualDetailsPage>();
		builder.Services.AddSingleton<HomeViewModel>();
		builder.Services.AddSingleton<CalculatorCatalogViewModel>();
		builder.Services.AddSingleton<PlansViewModel>();
		builder.Services.AddSingleton<SettingsViewModel>();
		builder.Services.AddTransient<CalculatorDetailViewModel>();
		builder.Services.AddTransient<QuizViewModel>();
		builder.Services.AddTransient<WelcomeViewModel>();
		builder.Services.AddTransient<DefaultsOnboardingViewModel>();
		builder.Services.AddTransient<OnboardingChoiceViewModel>();
		builder.Services.AddTransient<RetirementAnnualDetailsViewModel>();

#if MAUI_DEVFLOW && !IOS
		// The current DevFlow preview fails MAUI native class registration on iOS at startup.
		builder.AddMauiDevFlowAgent();
#endif

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
