using MyFireNumber.Core.Calculators;
using MyFireNumber.Services;
using MyFireNumber.Storage;

namespace MyFireNumber.ViewModels;

/// <summary>
/// Bundles the services every calculator view model needs so derived view models
/// only declare the dependencies unique to their own calculator.
/// </summary>
public sealed class CalculatorViewModelServices(
    IAppBehaviorPreferencesService behaviorPreferences,
    ICalculatorCatalog catalog,
    ICalculatorDefaultsService calculatorDefaults,
    ICorruptPayloadRepository corruptPayloadRepository,
    ICurrencyPreferencesService currencyPreferences,
    IDraftRepository draftRepository,
    INavigationService navigation,
    IPlanRepository planRepository)
{
    public IAppBehaviorPreferencesService BehaviorPreferences { get; } = behaviorPreferences;

    public ICalculatorCatalog Catalog { get; } = catalog;

    public ICalculatorDefaultsService CalculatorDefaults { get; } = calculatorDefaults;

    public ICorruptPayloadRepository CorruptPayloadRepository { get; } = corruptPayloadRepository;

    public ICurrencyPreferencesService CurrencyPreferences { get; } = currencyPreferences;

    public IDraftRepository DraftRepository { get; } = draftRepository;

    public INavigationService Navigation { get; } = navigation;

    public IPlanRepository PlanRepository { get; } = planRepository;
}
