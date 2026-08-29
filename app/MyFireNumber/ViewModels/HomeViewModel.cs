using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyFireNumber.Core.Books;
using MyFireNumber.Core.Calculators;
using MyFireNumber.Core.Profile;
using MyFireNumber.Services;
using MyFireNumber.Storage;
using System.Collections.ObjectModel;

namespace MyFireNumber.ViewModels;

public sealed record RecentPlanItem(string Id, string CalculatorId, string Name, string CalculatorTitle);

public partial class HomeViewModel : ObservableObject
{
    private static readonly string[] SuggestedCalculatorIds =
    [
        "standard-fire",
        "coast-fire",
        "savings-rate"
    ];

    private readonly ICalculatorCatalog catalog;
    private readonly IRecommendedBookCatalog recommendedBookCatalog;
    private readonly INavigationService navigationService;
    private readonly IOnboardingService onboardingService;
    private readonly ICalculatorPreferencesRepository preferencesRepository;
    private readonly IPlanRepository planRepository;
    private readonly IRecentActivityRepository recentActivityRepository;
    private readonly IAppBehaviorPreferencesService behaviorPreferencesService;
    private readonly IExternalLinkService externalLinkService;
    private readonly IErrorPresentationService errorPresentationService;
    private readonly IProfileService profileService;
    private readonly IProfileScenarioResolver profileScenarioResolver;
    private readonly IScenarioModePromptService scenarioModePromptService;
    private readonly IProfileAccountRepository profileAccountRepository;
    private readonly IProfileDebtRepository profileDebtRepository;
    private readonly IFinancialCheckInRepository checkInRepository;
    private readonly ICurrencyPreferencesService currencyPreferencesService;
    private readonly IPrivacyModePreferencesService privacyModePreferencesService;

    private const string PrivacyMask = "••••••";
    private double rawTotalAssets;
    private double rawTotalDebts;
    private string rawNetWorthChangeText = string.Empty;

    public HomeViewModel(
        ICalculatorCatalog catalog,
        IRecommendedBookCatalog recommendedBookCatalog,
        INavigationService navigationService,
        IOnboardingService onboardingService,
        ICalculatorPreferencesRepository preferencesRepository,
        IPlanRepository planRepository,
        IRecentActivityRepository recentActivityRepository,
        IAppBehaviorPreferencesService behaviorPreferencesService,
        IExternalLinkService externalLinkService,
        IErrorPresentationService errorPresentationService,
        IProfileService profileService,
        IProfileScenarioResolver profileScenarioResolver,
        IScenarioModePromptService scenarioModePromptService,
        IProfileAccountRepository profileAccountRepository,
        IProfileDebtRepository profileDebtRepository,
        IFinancialCheckInRepository checkInRepository,
        ICurrencyPreferencesService currencyPreferencesService,
        IPrivacyModePreferencesService privacyModePreferencesService)
    {
        this.catalog = catalog;
        this.recommendedBookCatalog = recommendedBookCatalog;
        this.navigationService = navigationService;
        this.onboardingService = onboardingService;
        this.preferencesRepository = preferencesRepository;
        this.planRepository = planRepository;
        this.recentActivityRepository = recentActivityRepository;
        this.behaviorPreferencesService = behaviorPreferencesService;
        this.externalLinkService = externalLinkService;
        this.errorPresentationService = errorPresentationService;
        this.profileService = profileService;
        this.profileScenarioResolver = profileScenarioResolver;
        this.scenarioModePromptService = scenarioModePromptService;
        this.profileAccountRepository = profileAccountRepository;
        this.profileDebtRepository = profileDebtRepository;
        this.checkInRepository = checkInRepository;
        this.currencyPreferencesService = currencyPreferencesService;
        this.privacyModePreferencesService = privacyModePreferencesService;
        isPrivacyModeEnabled = privacyModePreferencesService.HomePrivacyEnabled;
    }

    public ObservableCollection<CalculatorDefinition> FeaturedCalculators { get; } = [];
    public ObservableCollection<CalculatorDefinition> RecentCalculators { get; } = [];
    public ObservableCollection<RecentPlanItem> RecentPlans { get; } = [];
    public IReadOnlyList<RecommendedBook> RecommendedBooks => recommendedBookCatalog.All;

    public bool HasRecentCalculators => RecentCalculators.Count > 0;
    public bool HasRecentPlans => RecentPlans.Count > 0;

    /// <summary>
    /// The profile display name, shown only when onboarding or Profile captured one. The header
    /// falls back to the product name so an anonymous profile still reads correctly.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasGreeting))]
    [NotifyPropertyChangedFor(nameof(HasNoGreeting))]
    private string greeting = string.Empty;

    public bool HasGreeting => !string.IsNullOrWhiteSpace(Greeting);
    public bool HasNoGreeting => !HasGreeting;
    public bool ShowGettingStarted { get; private set; }
    public bool HasQuizRecommendation => ShowGettingStarted && RecommendedCalculator is not null;
    public bool ShowFeaturedCalculators => ShowGettingStarted && RecommendedCalculator is null;

    [ObservableProperty]
    private CalculatorDefinition? recommendedCalculator;

    [ObservableProperty]
    private bool showRecommendedBooks;

    // Accounts dashboard summary. Always computed from live account/debt data, never from check-in
    // history, so it matches whatever Accounts shows right now even if a check-in was never done.
    [ObservableProperty] private bool hasAccountsData;
    public bool HasNoAccountsData => !HasAccountsData;
    partial void OnHasAccountsDataChanged(bool value) => OnPropertyChanged(nameof(HasNoAccountsData));
    [ObservableProperty] private string netWorthText = "$0";
    [ObservableProperty] private string totalAssetsText = "$0";
    [ObservableProperty] private string totalDebtsText = "$0";
    [ObservableProperty] private bool hasCompletedCheckIn;
    [ObservableProperty] private string checkInFreshnessText = string.Empty;
    [ObservableProperty] private string nextCheckInText = string.Empty;
    [ObservableProperty] private bool isCheckInOverdue;
    [ObservableProperty] private bool hasNetWorthChange;
    [ObservableProperty] private string netWorthChangeText = string.Empty;
    [ObservableProperty] private bool isLoading = true;

    /// <summary>
    /// Masks net worth, assets, and debts on the Home dashboard when on. Off by default; persisted
    /// per-page in <see cref="IPrivacyModePreferencesService"/> and only forced on at app launch when
    /// the Settings "privacy mode on startup" override is enabled.
    /// </summary>
    [ObservableProperty]
    private bool isPrivacyModeEnabled;

    partial void OnIsPrivacyModeEnabledChanged(bool value)
    {
        privacyModePreferencesService.HomePrivacyEnabled = value;
        ApplyPrivacyMasking();
    }

    private void ApplyPrivacyMasking()
    {
        TotalAssetsText = IsPrivacyModeEnabled ? PrivacyMask : currencyPreferencesService.Format(rawTotalAssets);
        TotalDebtsText = IsPrivacyModeEnabled ? PrivacyMask : currencyPreferencesService.Format(rawTotalDebts);
        NetWorthText = IsPrivacyModeEnabled ? PrivacyMask : currencyPreferencesService.Format(rawTotalAssets - rawTotalDebts);
        NetWorthChangeText = IsPrivacyModeEnabled ? PrivacyMask : rawNetWorthChangeText;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            await LoadCoreAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadCoreAsync()
    {
        await profileService.LoadAsync();
        var displayName = profileService.Current.DisplayName;
        Greeting = string.IsNullOrWhiteSpace(displayName) ? string.Empty : $"Welcome back, {displayName.Trim()}";

        var preferencesTask = preferencesRepository.ListAsync();
        var recentCalculatorsTask = recentActivityRepository.ListAsync(RecentActivityKind.Calculator, 3);
        var recentPlansTask = recentActivityRepository.ListAsync(RecentActivityKind.Plan, 3);
        var plansTask = planRepository.ListAsync();
        await Task.WhenAll(preferencesTask, recentCalculatorsTask, recentPlansTask, plansTask);

        var preferences = await preferencesTask;
        var preferencesByCalculator = preferences.ToDictionary(preference => preference.CalculatorId);
        var visibleCalculators = catalog.All
            .Select((definition, defaultOrder) => new
            {
                Definition = definition,
                Preference = preferencesByCalculator.GetValueOrDefault(
                    definition.Id,
                    new CalculatorPreferenceRecord(definition.Id, true, defaultOrder))
            })
            .Where(item => item.Preference.IsVisible)
            .OrderBy(item => item.Preference.SortOrder)
            .Select(item => item.Definition)
            .ToArray();

        FeaturedCalculators.Clear();
        foreach (var calculator in SuggestedCalculatorIds
                     .Select(id => visibleCalculators.FirstOrDefault(definition => definition.Id == id))
                     .OfType<CalculatorDefinition>()
                     .Concat(visibleCalculators)
                     .DistinctBy(definition => definition.Id)
                     .Take(3))
        {
            FeaturedCalculators.Add(calculator);
        }

        RecentCalculators.Clear();
        foreach (var activity in await recentCalculatorsTask)
        {
            var calculator = catalog.All.FirstOrDefault(definition => definition.Id == activity.ItemId);
            if (calculator is not null)
            {
                RecentCalculators.Add(calculator);
            }
        }

        var plansById = (await plansTask).ToDictionary(plan => plan.Id);
        RecentPlans.Clear();
        foreach (var activity in await recentPlansTask)
        {
            if (plansById.TryGetValue(activity.ItemId, out var plan))
            {
                RecentPlans.Add(new RecentPlanItem(
                    plan.Id,
                    plan.CalculatorId,
                    plan.Name,
                    catalog.GetRequired(plan.CalculatorId).Title));
            }
        }

        ShowGettingStarted = RecentCalculators.Count == 0
            && RecentPlans.Count == 0
            && plansById.Count == 0;
        RecommendedCalculator = onboardingService.RecommendationCalculatorId is { } recommendationId
            ? catalog.All.FirstOrDefault(definition => definition.Id == recommendationId)
            : null;
        ShowRecommendedBooks = behaviorPreferencesService.Current.ShowRecommendedBooks;
        OnPropertyChanged(nameof(HasRecentCalculators));
        OnPropertyChanged(nameof(HasRecentPlans));
        OnPropertyChanged(nameof(ShowGettingStarted));
        OnPropertyChanged(nameof(HasQuizRecommendation));
        OnPropertyChanged(nameof(ShowFeaturedCalculators));

        await LoadAccountsSummaryAsync();
    }

    /// <summary>
    /// Current net worth/assets/debts (live, not from history), plus freshness and the change in net
    /// worth since the last completed monthly check-in. Trend-related UI (the change line) only shows
    /// once at least one check-in has ever completed, since "change since last update" is meaningless
    /// before that.
    /// </summary>
    private async Task LoadAccountsSummaryAsync()
    {
        var accounts = await profileAccountRepository.ListAsync();
        var debts = await profileDebtRepository.ListAsync();
        HasAccountsData = accounts.Count > 0 || debts.Count > 0;

        var totalAssets = accounts.Sum(account => account.Balance);
        var totalDebts = debts.Sum(debt => debt.Balance);
        rawTotalAssets = totalAssets;
        rawTotalDebts = totalDebts;

        var latest = await checkInRepository.GetLatestAsync();
        HasCompletedCheckIn = latest is not null;
        if (latest is null)
        {
            CheckInFreshnessText = "You haven't completed a monthly update yet.";
            NextCheckInText = string.Empty;
            IsCheckInOverdue = false;
            HasNetWorthChange = false;
            rawNetWorthChangeText = string.Empty;
            ApplyPrivacyMasking();
            return;
        }

        var now = DateTime.UtcNow;
        var days = CheckInSchedule.DaysSince(latest.CompletedAtUtc, now);
        CheckInFreshnessText = days == 0 ? "Last updated today." : $"Last updated {days} day{(days == 1 ? "" : "s")} ago.";
        var status = CheckInSchedule.Classify(latest.CompletedAtUtc, now);
        IsCheckInOverdue = status == FreshnessStatus.Overdue;
        var dueDate = CheckInSchedule.NextDueUtc(latest.CompletedAtUtc);
        NextCheckInText = status == FreshnessStatus.Overdue
            ? $"Overdue since {dueDate:MMM d, yyyy}."
            : $"Next update due {dueDate:MMM d, yyyy}.";

        var change = (totalAssets - totalDebts) - latest.NetWorth;
        HasNetWorthChange = true;
        rawNetWorthChangeText = change switch
        {
            > 0 => $"Up {currencyPreferencesService.Format(change)} since your last update.",
            < 0 => $"Down {currencyPreferencesService.Format(Math.Abs(change))} since your last update.",
            _ => "No change since your last update."
        };
        ApplyPrivacyMasking();
    }

    [RelayCommand]
    private async Task OpenCalculatorAsync(CalculatorDefinition definition)
    {
        var canChooseProfileLink = definition.Id == "retirement-cash-flow"
            || await profileScenarioResolver.HasCompatibleDataAsync(definition.Id);
        var dataMode = canChooseProfileLink
            ? await scenarioModePromptService.ChooseAsync(definition.Title)
            : ScenarioDataMode.Standalone;
        if (dataMode is null)
        {
            return;
        }

        await recentActivityRepository.TrackAsync(new RecentActivityRecord(
            RecentActivityKind.Calculator,
            definition.Id,
            DateTime.UtcNow));
        await navigationService.GoToAsync(CalculatorRoutes.Build(definition.Id, dataMode: dataMode));
    }

    [RelayCommand]
    private async Task OpenPlanAsync(RecentPlanItem plan)
    {
        var now = DateTime.UtcNow;
        await recentActivityRepository.TrackAsync(new RecentActivityRecord(
            RecentActivityKind.Calculator,
            plan.CalculatorId,
            now));
        await recentActivityRepository.TrackAsync(new RecentActivityRecord(
            RecentActivityKind.Plan,
            plan.Id,
            now));
        await navigationService.GoToAsync(CalculatorRoutes.Build(plan.CalculatorId, plan.Id));
    }

    [RelayCommand]
    private async Task OpenBookAsync(RecommendedBook book)
    {
        if (!await externalLinkService.OpenBookAsync(book.AmazonUri))
        {
            await errorPresentationService.ShowAsync(
                "Couldn’t open book",
                "Amazon could not be opened. Please try again.");
        }
    }

    [RelayCommand]
    private Task BrowseCalculatorsAsync()
    {
        return navigationService.GoToAsync("//calculators");
    }

    [RelayCommand]
    private Task RetakeQuizAsync()
    {
        return navigationService.GoToAsync("quiz");
    }

    [RelayCommand]
    private Task GoToAccountsAsync() => navigationService.GoToAsync("//accounts");

    [RelayCommand]
    private Task StartCheckInAsync() => navigationService.GoToAsync("accounts-check-in");

    [RelayCommand]
    private Task ViewHistoryAsync() => navigationService.GoToAsync("accounts-history");
}