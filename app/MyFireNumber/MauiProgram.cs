using Microsoft.Extensions.Logging;
using LiveChartsCore.SkiaSharpView.Maui;
using MyFireNumber.Core.Calculators;
using MyFireNumber.Services;
using MyFireNumber.Storage;
using MyFireNumber.ViewModels;
using MyFireNumber.Views;
using SkiaSharp.Views.Maui.Controls.Hosting;
#if MAUI_DEVFLOW
using Microsoft.Maui.DevFlow.Agent;
#endif

namespace MyFireNumber;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseSkiaSharp()
			.UseLiveCharts()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddSingleton<ICalculatorCatalog, CalculatorCatalog>();
		builder.Services.AddSingleton<IOnboardingService, OnboardingService>();
		builder.Services.AddSingleton<INavigationService, ShellNavigationService>();
		builder.Services.AddSingleton(_ => new LocalDatabase(Path.Combine(FileSystem.AppDataDirectory, "my-fire-number.db3")));
		builder.Services.AddSingleton<IDraftRepository, SqliteDraftRepository>();
		builder.Services.AddSingleton<IPlanRepository, SqlitePlanRepository>();
		builder.Services.AddSingleton<ICalculatorPreferencesRepository, SqliteCalculatorPreferencesRepository>();
		builder.Services.AddSingleton<AppShell>();
		builder.Services.AddSingleton<App>();
		builder.Services.AddSingleton<HomePage>();
		builder.Services.AddSingleton<CalculatorsPage>();
		builder.Services.AddSingleton<PlansPage>();
		builder.Services.AddSingleton<SettingsPage>();
		builder.Services.AddTransient<CalculatorDetailPage>();
		builder.Services.AddTransient<QuizPage>();
		builder.Services.AddSingleton<HomeViewModel>();
		builder.Services.AddSingleton<CalculatorCatalogViewModel>();
		builder.Services.AddSingleton<SettingsViewModel>();
		builder.Services.AddTransient<CalculatorDetailViewModel>();
		builder.Services.AddTransient<QuizViewModel>();

#if MAUI_DEVFLOW
		builder.AddMauiDevFlowAgent();
#endif

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
