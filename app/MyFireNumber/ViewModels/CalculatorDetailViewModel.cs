using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using MyFireNumber.Core.Calculators;
using MyFireNumber.Core.Calculations;
using MyFireNumber.Storage;
using MyFireNumber.Services;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Text.Json;

namespace MyFireNumber.ViewModels;

public partial class CalculatorDetailViewModel : ObservableObject
{
    private readonly IBaristaFireExportService baristaExportService;
    private readonly IAppBehaviorPreferencesService behaviorPreferencesService;
    private readonly ICalculatorCatalog catalog;
    private readonly ICalculatorDefaultsService calculatorDefaultsService;
    private readonly ICurrencyPreferencesService currencyPreferencesService;
    private readonly ICoastFireExportService coastExportService;
    private readonly ICorruptPayloadRepository corruptPayloadRepository;
    private readonly IDeferredCompensationExportService deferredCompensationExportService;
    private readonly IDebtPayoffExportService debtPayoffExportService;
    private readonly IDraftRepository draftRepository;
    private readonly IFatFireExportService fatExportService;
    private readonly IHealthcareGapExportService healthcareGapExportService;
    private readonly ILeanFireExportService leanExportService;
    private readonly INavigationService navigationService;
    private readonly IReverseFireExportService reverseExportService;
    private readonly ISavingsInvestmentExportService savingsInvestmentExportService;
    private readonly IStandardFireExportService exportService;
    private readonly IWithdrawalRateExportService withdrawalRateExportService;
    private readonly IPlanRepository planRepository;
    private readonly SemaphoreSlim draftSaveLock = new(1, 1);
    private readonly object pendingDraftLock = new();
    private CancellationTokenSource? saveCancellationTokenSource;
    private DraftRecord? pendingDraft;
    private bool isApplyingDraft;
    private DateTime? loadedPlanCreatedAtUtc;
    private string? loadedPlanId;

    public ObservableCollection<DebtEditorItem> DebtItems { get; } = [];
    public ObservableCollection<RetirementAccountEditorItem> RetirementAccounts { get; } = [];
    public ObservableCollection<RetirementIncomeEditorItem> RetirementIncomeSources { get; } = [];
    public ObservableCollection<RetirementExpenseEditorItem> RetirementAdditionalExpenses { get; } = [];

    public TimeSpan ChartAnimationsSpeed => behaviorPreferencesService.Current.ReduceMotion
        ? TimeSpan.Zero
        : TimeSpan.FromMilliseconds(800);

    public CalculatorDetailViewModel(
        IBaristaFireExportService baristaExportService,
        IAppBehaviorPreferencesService behaviorPreferencesService,
        ICalculatorCatalog catalog,
        ICalculatorDefaultsService calculatorDefaultsService,
        ICurrencyPreferencesService currencyPreferencesService,
        ICoastFireExportService coastExportService,
        ICorruptPayloadRepository corruptPayloadRepository,
        IDeferredCompensationExportService deferredCompensationExportService,
        IDebtPayoffExportService debtPayoffExportService,
        IDraftRepository draftRepository,
        IFatFireExportService fatExportService,
        IHealthcareGapExportService healthcareGapExportService,
        ILeanFireExportService leanExportService,
        INavigationService navigationService,
        IPlanRepository planRepository,
        IReverseFireExportService reverseExportService,
        ISavingsInvestmentExportService savingsInvestmentExportService,
        IStandardFireExportService exportService,
        IWithdrawalRateExportService withdrawalRateExportService)
    {
        this.baristaExportService = baristaExportService;
        this.behaviorPreferencesService = behaviorPreferencesService;
        this.catalog = catalog;
        this.calculatorDefaultsService = calculatorDefaultsService;
        this.currencyPreferencesService = currencyPreferencesService;
        this.coastExportService = coastExportService;
        this.corruptPayloadRepository = corruptPayloadRepository;
        this.deferredCompensationExportService = deferredCompensationExportService;
        this.debtPayoffExportService = debtPayoffExportService;
        this.draftRepository = draftRepository;
        this.fatExportService = fatExportService;
        this.healthcareGapExportService = healthcareGapExportService;
        this.leanExportService = leanExportService;
        this.navigationService = navigationService;
        this.planRepository = planRepository;
        this.reverseExportService = reverseExportService;
        this.savingsInvestmentExportService = savingsInvestmentExportService;
        this.exportService = exportService;
        this.withdrawalRateExportService = withdrawalRateExportService;
        DebtItems.CollectionChanged += OnDebtItemsChanged;
        RetirementAccounts.CollectionChanged += OnRetirementAccountsChanged;
        RetirementIncomeSources.CollectionChanged += OnRetirementIncomeSourcesChanged;
        RetirementAdditionalExpenses.CollectionChanged += OnRetirementExpensesChanged;
        ApplyDraft(calculatorDefaultsService.StandardFire);
    }

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string summary = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SavePlanActionText), nameof(SavePlanActionDescription))]
    private bool isLoadedPlan;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStandardFireOrLeanFire), nameof(IsUnsupportedCalculator))]
    private bool isStandardFire;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUnsupportedCalculator))]
    private bool isCoastFire;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStandardFireOrLeanFire), nameof(IsUnsupportedCalculator), nameof(FireNumberLabel), nameof(YearsToFireLabel), nameof(OutlookTitle), nameof(ProjectionTitle), nameof(PlanNamePlaceholder), nameof(PlanNameDescription), nameof(ExportDescription))]
    private bool isLeanFire;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStandardFireOrLeanFire), nameof(IsUnsupportedCalculator), nameof(FireNumberLabel), nameof(YearsToFireLabel), nameof(OutlookTitle), nameof(ProjectionTitle), nameof(PlanNamePlaceholder), nameof(PlanNameDescription), nameof(ExportDescription))]
    private bool isFatFire;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUnsupportedCalculator))]
    private bool isBaristaFire;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUnsupportedCalculator))]
    private bool isReverseFire;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUnsupportedCalculator))]
    private bool isWithdrawalRate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUnsupportedCalculator))]
    private bool isSavingsInvestmentRate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUnsupportedCalculator))]
    private bool isHealthcareGap;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUnsupportedCalculator))]
    private bool isDebtPayoff;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUnsupportedCalculator))]
    private bool isRetirementCashFlow;

    [ObservableProperty]
    private bool withdrawOnlyAfterRetirement = true;

    [ObservableProperty]
    private bool reinvestRetirementSurplus = true;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValidationMessage))]
    private string validationMessage = string.Empty;

    [ObservableProperty]
    private string currentAgeText = string.Empty;

    [ObservableProperty]
    private string retirementAgeText = string.Empty;

    [ObservableProperty]
    private string currentSavingsText = string.Empty;

    [ObservableProperty]
    private string annualContributionText = string.Empty;

    [ObservableProperty]
    private string annualIncomeText = string.Empty;

    [ObservableProperty]
    private string annualExpensesText = string.Empty;

    [ObservableProperty]
    private string expectedReturnText = string.Empty;

    [ObservableProperty]
    private string inflationRateText = string.Empty;

    [ObservableProperty]
    private string withdrawalRateText = string.Empty;

    [ObservableProperty]
    private string fireNumberText = string.Empty;

    [ObservableProperty]
    private string yearsToFireText = string.Empty;

    [ObservableProperty]
    private string fireAgeText = string.Empty;

    [ObservableProperty]
    private string savingsRateText = string.Empty;

    [ObservableProperty]
    private string monthlyContributionText = string.Empty;

    [ObservableProperty]
    private string progressDescription = string.Empty;

    [ObservableProperty]
    private string projectionSummary = string.Empty;

    [ObservableProperty]
    private string coastNumberText = string.Empty;

    [ObservableProperty]
    private string fullFireNumberText = string.Empty;

    [ObservableProperty]
    private string yearsToCoastText = string.Empty;

    [ObservableProperty]
    private string coastStatusText = string.Empty;

    [ObservableProperty]
    private string coastProgressDescription = string.Empty;

    [ObservableProperty]
    private string coastProjectionSummary = string.Empty;

    [ObservableProperty]
    private string leanStatusText = string.Empty;

    [ObservableProperty]
    private string leanGuidanceText = string.Empty;

    [ObservableProperty]
    private string fatStatusText = string.Empty;

    [ObservableProperty]
    private string fatGuidanceText = string.Empty;

    [ObservableProperty]
    private string partTimeAnnualIncomeText = string.Empty;

    [ObservableProperty]
    private string baristaNumberText = string.Empty;

    [ObservableProperty]
    private string baristaYearsText = string.Empty;

    [ObservableProperty]
    private string baristaReductionText = string.Empty;

    [ObservableProperty]
    private string baristaProgressDescription = string.Empty;

    [ObservableProperty]
    private string baristaProjectionSummary = string.Empty;

    [ObservableProperty]
    private string reverseRequiredAnnualSavingsText = string.Empty;

    [ObservableProperty]
    private string reverseRequiredMonthlySavingsText = string.Empty;

    [ObservableProperty]
    private string reverseYearsText = string.Empty;

    [ObservableProperty]
    private string reverseCurrentGrowthText = string.Empty;

    [ObservableProperty]
    private string reverseStatusText = string.Empty;

    [ObservableProperty]
    private string reverseProjectionSummary = string.Empty;

    [ObservableProperty]
    private string portfolioValueText = string.Empty;

    [ObservableProperty]
    private string retirementYearsText = string.Empty;

    [ObservableProperty]
    private string withdrawalAnnualText = string.Empty;

    [ObservableProperty]
    private string withdrawalMonthlyText = string.Empty;

    [ObservableProperty]
    private string withdrawalLongevityText = string.Empty;

    [ObservableProperty]
    private string withdrawalSuccessText = string.Empty;

    [ObservableProperty]
    private string withdrawalStatusText = string.Empty;

    [ObservableProperty]
    private string withdrawalRateAnalysisText = string.Empty;

    [ObservableProperty]
    private string startingAmountText = string.Empty;

    [ObservableProperty]
    private string investmentContributionText = string.Empty;

    [ObservableProperty]
    private string yearsInvestingText = string.Empty;

    [ObservableProperty]
    private string investmentAnnualIncomeText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMonthlyContribution), nameof(IsYearlyContribution))]
    private ContributionFrequency contributionFrequency = ContributionFrequency.Monthly;

    [ObservableProperty]
    private string investmentSavingsRateText = string.Empty;

    [ObservableProperty]
    private string investmentAnnualContributionText = string.Empty;

    [ObservableProperty]
    private string investmentFinalBalanceText = string.Empty;

    [ObservableProperty]
    private string investmentInflationAdjustedText = string.Empty;

    [ObservableProperty]
    private string investmentGrowthText = string.Empty;

    [ObservableProperty]
    private string investmentCategoryText = string.Empty;

    [ObservableProperty]
    private string investmentProjectionSummary = string.Empty;

    [ObservableProperty]
    private string healthcareCurrentAgeText = string.Empty;

    [ObservableProperty]
    private string earlyRetirementAgeText = string.Empty;

    [ObservableProperty]
    private string medicareAgeText = string.Empty;

    [ObservableProperty]
    private string monthlyPremiumText = string.Empty;

    [ObservableProperty]
    private string annualDeductibleText = string.Empty;

    [ObservableProperty]
    private string annualOutOfPocketText = string.Empty;

    [ObservableProperty]
    private string healthcareGapYearsText = string.Empty;

    [ObservableProperty]
    private string healthcareAnnualCostText = string.Empty;

    [ObservableProperty]
    private string healthcareTotalCostText = string.Empty;

    [ObservableProperty]
    private string healthcareAverageAnnualCostText = string.Empty;

    [ObservableProperty]
    private string healthcareSubsidyEstimateText = string.Empty;

    [ObservableProperty]
    private string healthcareProjectionSummary = string.Empty;

    [ObservableProperty]
    private string debtMonthlyBudgetText = string.Empty;

    [ObservableProperty]
    private string debtExtraPaymentText = string.Empty;

    [ObservableProperty]
    private string debtTargetMonthsText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFixedDebtPayoff), nameof(IsTargetDebtPayoff))]
    private DebtPayoffMode debtPayoffMode = DebtPayoffMode.FixedBudget;

    [ObservableProperty]
    private DebtPayoffStrategy debtPayoffStrategy = DebtPayoffStrategy.Snowball;

    [ObservableProperty]
    private string debtTotalText = string.Empty;

    [ObservableProperty]
    private string debtMinimumPaymentsText = string.Empty;

    [ObservableProperty]
    private string debtPayoffTimeText = string.Empty;

    [ObservableProperty]
    private string debtInterestText = string.Empty;

    [ObservableProperty]
    private string debtPaymentText = string.Empty;

    [ObservableProperty]
    private string debtStrategySummary = string.Empty;

    [ObservableProperty]
    private string debtSnowballComparisonText = string.Empty;

    [ObservableProperty]
    private string debtAvalancheComparisonText = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<ISeries> debtBreakdownSeries = [];

    [ObservableProperty]
    private string debtBreakdownDescription = string.Empty;

    [ObservableProperty]
    private string debtBreakdownSummary = string.Empty;

    [ObservableProperty] private string retirementCurrentAgeText = "45";
    [ObservableProperty] private string retirementSemiAgeText = "55";
    [ObservableProperty] private string retirementPlanThroughAgeText = "90";
    [ObservableProperty] private string retirementExpensesText = "80000";
    [ObservableProperty] private string retirementInflationText = "3";
    [ObservableProperty] private string retirementCurrentBalanceText = string.Empty;
    [ObservableProperty] private string retirementBalanceAtSemiText = string.Empty;
    [ObservableProperty] private string retirementEndingBalanceText = string.Empty;
    [ObservableProperty] private string retirementFundedYearsText = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<ISeries> retirementBucketSeries = [];

    [ObservableProperty]
    private string retirementBucketDescription = string.Empty;

    [ObservableProperty]
    private string retirementBucketSummary = string.Empty;

    [ObservableProperty]
    private string planNameText = "My Standard FIRE Plan";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPlanStatusMessage))]
    private string planStatusMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasExportStatusMessage))]
    private string exportStatusMessage = string.Empty;

    [ObservableProperty]
    private StandardFirePreset? selectedPreset;

    [ObservableProperty]
    private IReadOnlyList<ISeries> projectionSeries = [];

    [ObservableProperty]
    private Axis[] projectionXAxes = [];

    [ObservableProperty]
    private string projectionChartDescription = string.Empty;

    [ObservableProperty]
    private double progressToFire;

    public IReadOnlyList<StandardFirePreset> StandardFirePresets => StandardFirePreset.All;

    public bool IsStandardFireOrLeanFire => IsStandardFire || IsLeanFire || IsFatFire;

    public bool IsUnsupportedCalculator => !IsStandardFire && !IsCoastFire && !IsLeanFire && !IsFatFire && !IsBaristaFire && !IsReverseFire && !IsWithdrawalRate && !IsSavingsInvestmentRate && !IsHealthcareGap && !IsDebtPayoff && !IsRetirementCashFlow;

    public bool IsMonthlyContribution => ContributionFrequency == ContributionFrequency.Monthly;

    public bool IsYearlyContribution => ContributionFrequency == ContributionFrequency.Yearly;

    public bool IsFixedDebtPayoff => DebtPayoffMode == DebtPayoffMode.FixedBudget;

    public bool IsTargetDebtPayoff => DebtPayoffMode == DebtPayoffMode.TargetTimeline;

    public string FireNumberLabel => IsLeanFire ? "Lean FIRE Number" : IsFatFire ? "Fat FIRE Number" : "FIRE Number";

    public string YearsToFireLabel => IsLeanFire ? "Years to Lean FIRE" : IsFatFire ? "Years to Fat FIRE" : "Years to FIRE";

    public string OutlookTitle => IsLeanFire ? "Your Lean FIRE outlook" : IsFatFire ? "Your Fat FIRE outlook" : "Your FIRE outlook";

    public string ProjectionTitle => IsLeanFire ? "Lean FIRE projection" : IsFatFire ? "Fat FIRE projection" : "Portfolio projection";

    public string PlanNamePlaceholder => IsLeanFire ? "My Lean FIRE Plan" : IsFatFire ? "My Fat FIRE Plan" : "My Standard FIRE Plan";

    public string PlanNameDescription => IsLeanFire ? "Name for this saved Lean FIRE plan" : IsFatFire ? "Name for this saved Fat FIRE plan" : "Name for this saved Standard FIRE plan";

    public string ExportDescription => IsLeanFire
        ? "Create a Lean FIRE workbook on this device and open the native share sheet."
        : IsFatFire
            ? "Create a Fat FIRE workbook on this device and open the native share sheet."
        : "Create a Standard FIRE workbook on this device and open the native share sheet.";

    public bool HasValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);

    public bool HasPlanStatusMessage => !string.IsNullOrWhiteSpace(PlanStatusMessage);

    public bool HasExportStatusMessage => !string.IsNullOrWhiteSpace(ExportStatusMessage);

    public string SavePlanActionText => IsLoadedPlan ? "Update Plan" : "Save to Plans";

    public string SavePlanActionDescription => IsLoadedPlan
        ? "Update this saved plan with the current values."
        : "Save the current values as a named plan.";

    public async Task LoadAsync(string calculatorId, string? planId = null)
    {
        loadedPlanId = null;
        loadedPlanCreatedAtUtc = null;
        IsLoadedPlan = false;
        var definition = catalog.GetRequired(calculatorId);
        Title = definition.Title;
        Summary = definition.Summary;
        IsStandardFire = calculatorId == "standard-fire";
        IsCoastFire = calculatorId == "coast-fire";
        IsLeanFire = calculatorId == "lean-fire";
        IsFatFire = calculatorId == "fat-fire";
        IsBaristaFire = calculatorId == "barista-fire";
        IsReverseFire = calculatorId == "reverse-fire";
        IsWithdrawalRate = calculatorId == "withdrawal-rate";
        IsSavingsInvestmentRate = calculatorId == "savings-rate";
        IsHealthcareGap = calculatorId == "healthcare-gap";
        IsDebtPayoff = calculatorId == "debt-payoff";
        IsRetirementCashFlow = calculatorId == "retirement-cash-flow";
        PlanNameText = calculatorId switch
        {
            "coast-fire" => "My Coast FIRE Plan",
            "lean-fire" => "My Lean FIRE Plan",
            "fat-fire" => "My Fat FIRE Plan",
            "barista-fire" => "My Barista FIRE Plan",
            "reverse-fire" => "My Reverse FIRE Plan",
            "withdrawal-rate" => "My Withdrawal Plan",
            "savings-rate" => "My Investment Plan",
            "healthcare-gap" => "My Healthcare Gap Plan",
            "debt-payoff" => "My Debt Payoff Plan",
            "retirement-cash-flow" => "My Retirement Cash Flow Plan",
            _ => "My Standard FIRE Plan"
        };

        if (IsUnsupportedCalculator)
        {
            return;
        }

        if (IsRetirementCashFlow)
        {
            ApplyDeferredCompensationDraft(calculatorDefaultsService.RetirementCashFlow);
        }

        IsLoading = true;
        ValidationMessage = string.Empty;
        try
        {
            if (!string.IsNullOrWhiteSpace(planId))
            {
                var savedPlan = await planRepository.GetAsync(planId);
                if (savedPlan is null || savedPlan.CalculatorId != calculatorId)
                {
                    ValidationMessage = "The requested plan could not be found. Default values are shown.";
                    ApplyDefaultDraft();
                }
                else if (IsStandardFire && savedPlan.PayloadVersion == StandardFireDraft.PayloadVersion)
                {
                    var draft = JsonSerializer.Deserialize<StandardFireDraft>(savedPlan.PayloadJson);
                    PlanNameText = savedPlan.Name;
                    ApplyDraft(draft ?? calculatorDefaultsService.StandardFire);
                    TrackLoadedPlan(savedPlan);
                }
                else if (IsCoastFire && savedPlan.PayloadVersion == CoastFireDraft.PayloadVersion)
                {
                    var draft = JsonSerializer.Deserialize<CoastFireDraft>(savedPlan.PayloadJson);
                    PlanNameText = savedPlan.Name;
                    ApplyDraft(draft ?? calculatorDefaultsService.CoastFire);
                    TrackLoadedPlan(savedPlan);
                }
                else if (IsLeanFire && savedPlan.PayloadVersion == LeanFireDraft.PayloadVersion)
                {
                    var draft = JsonSerializer.Deserialize<LeanFireDraft>(savedPlan.PayloadJson);
                    PlanNameText = savedPlan.Name;
                    ApplyDraft(draft ?? calculatorDefaultsService.LeanFire);
                    TrackLoadedPlan(savedPlan);
                }
                else if (IsFatFire && savedPlan.PayloadVersion == FatFireDraft.PayloadVersion)
                {
                    var draft = JsonSerializer.Deserialize<FatFireDraft>(savedPlan.PayloadJson);
                    PlanNameText = savedPlan.Name;
                    ApplyDraft(draft ?? calculatorDefaultsService.FatFire);
                    TrackLoadedPlan(savedPlan);
                }
                else if (IsBaristaFire && savedPlan.PayloadVersion == BaristaFireDraft.PayloadVersion)
                {
                    var draft = JsonSerializer.Deserialize<BaristaFireDraft>(savedPlan.PayloadJson);
                    PlanNameText = savedPlan.Name;
                    ApplyDraft(draft ?? calculatorDefaultsService.BaristaFire);
                    TrackLoadedPlan(savedPlan);
                }
                else if (IsReverseFire && savedPlan.PayloadVersion == ReverseFireDraft.PayloadVersion)
                {
                    var draft = JsonSerializer.Deserialize<ReverseFireDraft>(savedPlan.PayloadJson);
                    PlanNameText = savedPlan.Name;
                    ApplyDraft(draft ?? calculatorDefaultsService.ReverseFire);
                    TrackLoadedPlan(savedPlan);
                }
                else if (IsWithdrawalRate && savedPlan.PayloadVersion == WithdrawalRateDraft.PayloadVersion)
                {
                    var draft = JsonSerializer.Deserialize<WithdrawalRateDraft>(savedPlan.PayloadJson);
                    PlanNameText = savedPlan.Name;
                    ApplyDraft(draft ?? calculatorDefaultsService.WithdrawalRate);
                    TrackLoadedPlan(savedPlan);
                }
                else if (IsSavingsInvestmentRate && savedPlan.PayloadVersion == SavingsInvestmentDraft.PayloadVersion)
                {
                    var draft = JsonSerializer.Deserialize<SavingsInvestmentDraft>(savedPlan.PayloadJson);
                    PlanNameText = savedPlan.Name;
                    ApplyDraft(draft ?? calculatorDefaultsService.SavingsInvestment);
                    TrackLoadedPlan(savedPlan);
                }
                else if (IsHealthcareGap && savedPlan.PayloadVersion == HealthcareGapDraft.PayloadVersion)
                {
                    var draft = JsonSerializer.Deserialize<HealthcareGapDraft>(savedPlan.PayloadJson);
                    PlanNameText = savedPlan.Name;
                    ApplyDraft(draft ?? calculatorDefaultsService.HealthcareGap);
                    TrackLoadedPlan(savedPlan);
                }
                else if (IsDebtPayoff && savedPlan.PayloadVersion == DebtPayoffDraft.PayloadVersion)
                {
                    var draft = JsonSerializer.Deserialize<DebtPayoffDraft>(savedPlan.PayloadJson);
                    PlanNameText = savedPlan.Name;
                    ApplyDraft(draft ?? DebtPayoffDraft.Default);
                    TrackLoadedPlan(savedPlan);
                }
                else if (IsRetirementCashFlow && savedPlan.PayloadVersion == DeferredCompensationDraft.PayloadVersion)
                {
                    var draft = JsonSerializer.Deserialize<DeferredCompensationDraft>(savedPlan.PayloadJson);
                    PlanNameText = savedPlan.Name;
                    ApplyDeferredCompensationDraft(draft ?? calculatorDefaultsService.RetirementCashFlow);
                    TrackLoadedPlan(savedPlan);
                }
                else
                {
                    ValidationMessage = "This saved plan uses an unsupported format. Default values are shown.";
                    ApplyDefaultDraft();
                }
            }
            else
            {
                var savedDraft = behaviorPreferencesService.Current.RestoreDrafts
                    ? await draftRepository.GetAsync(calculatorId)
                    : null;
                if (savedDraft is null)
                {
                    ApplyDefaultDraft();
                }
                else if (IsStandardFire && savedDraft.PayloadVersion == StandardFireDraft.PayloadVersion)
                {
                    var draft = JsonSerializer.Deserialize<StandardFireDraft>(savedDraft.PayloadJson);
                    ApplyDraft(draft ?? calculatorDefaultsService.StandardFire);
                }
                else if (IsCoastFire && savedDraft.PayloadVersion == CoastFireDraft.PayloadVersion)
                {
                    var draft = JsonSerializer.Deserialize<CoastFireDraft>(savedDraft.PayloadJson);
                    ApplyDraft(draft ?? calculatorDefaultsService.CoastFire);
                }
                else if (IsLeanFire && savedDraft.PayloadVersion == LeanFireDraft.PayloadVersion)
                {
                    var draft = JsonSerializer.Deserialize<LeanFireDraft>(savedDraft.PayloadJson);
                    ApplyDraft(draft ?? calculatorDefaultsService.LeanFire);
                }
                else if (IsFatFire && savedDraft.PayloadVersion == FatFireDraft.PayloadVersion)
                {
                    var draft = JsonSerializer.Deserialize<FatFireDraft>(savedDraft.PayloadJson);
                    ApplyDraft(draft ?? calculatorDefaultsService.FatFire);
                }
                else if (IsBaristaFire && savedDraft.PayloadVersion == BaristaFireDraft.PayloadVersion)
                {
                    var draft = JsonSerializer.Deserialize<BaristaFireDraft>(savedDraft.PayloadJson);
                    ApplyDraft(draft ?? calculatorDefaultsService.BaristaFire);
                }
                else if (IsReverseFire && savedDraft.PayloadVersion == ReverseFireDraft.PayloadVersion)
                {
                    var draft = JsonSerializer.Deserialize<ReverseFireDraft>(savedDraft.PayloadJson);
                    ApplyDraft(draft ?? calculatorDefaultsService.ReverseFire);
                }
                else if (IsWithdrawalRate && savedDraft.PayloadVersion == WithdrawalRateDraft.PayloadVersion)
                {
                    var draft = JsonSerializer.Deserialize<WithdrawalRateDraft>(savedDraft.PayloadJson);
                    ApplyDraft(draft ?? calculatorDefaultsService.WithdrawalRate);
                }
                else if (IsSavingsInvestmentRate && savedDraft.PayloadVersion == SavingsInvestmentDraft.PayloadVersion)
                {
                    var draft = JsonSerializer.Deserialize<SavingsInvestmentDraft>(savedDraft.PayloadJson);
                    ApplyDraft(draft ?? calculatorDefaultsService.SavingsInvestment);
                }
                else if (IsHealthcareGap && savedDraft.PayloadVersion == HealthcareGapDraft.PayloadVersion)
                {
                    var draft = JsonSerializer.Deserialize<HealthcareGapDraft>(savedDraft.PayloadJson);
                    ApplyDraft(draft ?? calculatorDefaultsService.HealthcareGap);
                }
                else if (IsDebtPayoff && savedDraft.PayloadVersion == DebtPayoffDraft.PayloadVersion)
                {
                    var draft = JsonSerializer.Deserialize<DebtPayoffDraft>(savedDraft.PayloadJson);
                    ApplyDraft(draft ?? DebtPayoffDraft.Default);
                }
                else if (IsRetirementCashFlow && savedDraft.PayloadVersion == DeferredCompensationDraft.PayloadVersion)
                {
                    var draft = JsonSerializer.Deserialize<DeferredCompensationDraft>(savedDraft.PayloadJson);
                    ApplyDeferredCompensationDraft(draft ?? calculatorDefaultsService.RetirementCashFlow);
                }
                else
                {
                    ValidationMessage = "This saved draft uses an unsupported format. Default values are shown.";
                    ApplyDefaultDraft();
                }
            }
        }
        catch (JsonException)
        {
            var wasQuarantined = await QuarantineCorruptPayloadAsync(calculatorId, planId);
            ValidationMessage = wasQuarantined
                ? "Unreadable saved data was moved to local recovery storage. Default values are shown."
                : "Saved data could not be read or moved to recovery storage. Default values are shown.";
            ApplyDefaultDraft();
        }

        catch (Exception)
        {
            ValidationMessage = "Your saved draft could not be restored. You can continue with the values shown.";
            ApplyDefaultDraft();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task<bool> QuarantineCorruptPayloadAsync(string calculatorId, string? planId)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(planId))
            {
                var plan = await planRepository.GetAsync(planId);
                if (plan is null)
                {
                    return false;
                }

                await corruptPayloadRepository.QuarantinePlanAsync(plan);
                loadedPlanId = null;
                loadedPlanCreatedAtUtc = null;
                return true;
            }

            var draft = await draftRepository.GetAsync(calculatorId);
            if (draft is null)
            {
                return false;
            }

            await corruptPayloadRepository.QuarantineDraftAsync(draft);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    [RelayCommand]
    private void ResetDefaults()
    {
        ValidationMessage = string.Empty;
        SelectedPreset = null;
        ApplyDefaultDraft();
    }

    [RelayCommand]
    private async Task SavePlanAsync()
    {
        await SavePlanCoreAsync(PlanNameText);
    }

    private async Task SavePlanCoreAsync(string planName)
    {
        if (string.IsNullOrWhiteSpace(planName))
        {
            ValidationMessage = "Enter a name before saving this plan.";
            return;
        }

        string calculatorId;
        int payloadVersion;
        string payloadJson;
        if (IsStandardFire && TryCreateDraft(out var standardDraft))
        {
            calculatorId = "standard-fire";
            payloadVersion = StandardFireDraft.PayloadVersion;
            payloadJson = JsonSerializer.Serialize(standardDraft);
        }
        else if (IsCoastFire && TryCreateCoastDraft(out var coastDraft))
        {
            calculatorId = "coast-fire";
            payloadVersion = CoastFireDraft.PayloadVersion;
            payloadJson = JsonSerializer.Serialize(coastDraft);
        }
        else if (IsLeanFire && TryCreateLeanDraft(out var leanDraft))
        {
            calculatorId = "lean-fire";
            payloadVersion = LeanFireDraft.PayloadVersion;
            payloadJson = JsonSerializer.Serialize(leanDraft);
        }
        else if (IsFatFire && TryCreateFatDraft(out var fatDraft))
        {
            calculatorId = "fat-fire";
            payloadVersion = FatFireDraft.PayloadVersion;
            payloadJson = JsonSerializer.Serialize(fatDraft);
        }
        else if (IsBaristaFire && TryCreateBaristaDraft(out var baristaDraft))
        {
            calculatorId = "barista-fire";
            payloadVersion = BaristaFireDraft.PayloadVersion;
            payloadJson = JsonSerializer.Serialize(baristaDraft);
        }
        else if (IsReverseFire && TryCreateReverseDraft(out var reverseDraft))
        {
            calculatorId = "reverse-fire";
            payloadVersion = ReverseFireDraft.PayloadVersion;
            payloadJson = JsonSerializer.Serialize(reverseDraft);
        }
        else if (IsWithdrawalRate && TryCreateWithdrawalRateDraft(out var withdrawalDraft))
        {
            calculatorId = "withdrawal-rate";
            payloadVersion = WithdrawalRateDraft.PayloadVersion;
            payloadJson = JsonSerializer.Serialize(withdrawalDraft);
        }
        else if (IsSavingsInvestmentRate && TryCreateSavingsInvestmentDraft(out var investmentDraft))
        {
            calculatorId = "savings-rate";
            payloadVersion = SavingsInvestmentDraft.PayloadVersion;
            payloadJson = JsonSerializer.Serialize(investmentDraft);
        }
        else if (IsHealthcareGap && TryCreateHealthcareGapDraft(out var healthcareDraft))
        {
            calculatorId = "healthcare-gap";
            payloadVersion = HealthcareGapDraft.PayloadVersion;
            payloadJson = JsonSerializer.Serialize(healthcareDraft);
        }
        else if (IsDebtPayoff && TryCreateDebtPayoffDraft(out var debtDraft))
        {
            calculatorId = "debt-payoff";
            payloadVersion = DebtPayoffDraft.PayloadVersion;
            payloadJson = JsonSerializer.Serialize(debtDraft);
        }
        else if (IsRetirementCashFlow && TryCreateDeferredCompensationDraft(out var deferredDraft))
        {
            calculatorId = "retirement-cash-flow";
            payloadVersion = DeferredCompensationDraft.PayloadVersion;
            payloadJson = JsonSerializer.Serialize(deferredDraft);
        }
        else
        {
            return;
        }

        try
        {
            var now = DateTime.UtcNow;
            var isUpdatingLoadedPlan = loadedPlanId is not null;
            var planIdToSave = isUpdatingLoadedPlan ? loadedPlanId! : Guid.NewGuid().ToString("N");
            await planRepository.SaveAsync(new PlanRecord(
                planIdToSave,
                calculatorId,
                planName.Trim(),
                payloadVersion,
                payloadJson,
                isUpdatingLoadedPlan ? loadedPlanCreatedAtUtc ?? now : now,
                now));
            loadedPlanId = planIdToSave;
            loadedPlanCreatedAtUtc = isUpdatingLoadedPlan ? loadedPlanCreatedAtUtc ?? now : now;
            IsLoadedPlan = true;
            PlanNameText = planName.Trim();
            PlanStatusMessage = isUpdatingLoadedPlan
                ? $"Updated \"{PlanNameText.Trim()}\" in Plans."
                : $"Saved \"{PlanNameText.Trim()}\" to Plans.";
            behaviorPreferencesService.PerformHaptic();
        }
        catch (Exception)
        {
            PlanStatusMessage = "This plan could not be saved locally yet.";
        }
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        try
        {
            if (IsStandardFire && TryCreateDraft(out var standardDraft))
            {
                var result = FinancialCalculator.CalculateStandardFire(standardDraft.ToFireInputs());
                await exportService.ShareAsync(standardDraft, result);
                ExportStatusMessage = "Your Standard FIRE workbook is ready to share.";
            }
            else if (IsCoastFire && TryCreateCoastDraft(out var coastDraft))
            {
                var result = FinancialCalculator.CalculateCoastFire(coastDraft.ToFireInputs());
                await coastExportService.ShareAsync(coastDraft, result);
                ExportStatusMessage = "Your Coast FIRE workbook is ready to share.";
            }
            else if (IsLeanFire && TryCreateLeanDraft(out var leanDraft))
            {
                var result = FinancialCalculator.CalculateLeanFire(leanDraft.ToFireInputs()).Standard;
                await leanExportService.ShareAsync(leanDraft, result);
                ExportStatusMessage = "Your Lean FIRE workbook is ready to share.";
            }
            else if (IsFatFire && TryCreateFatDraft(out var fatDraft))
            {
                var result = FinancialCalculator.CalculateFatFire(fatDraft.ToFireInputs()).Standard;
                await fatExportService.ShareAsync(fatDraft, result);
                ExportStatusMessage = "Your Fat FIRE workbook is ready to share.";
            }
            else if (IsBaristaFire && TryCreateBaristaDraft(out var baristaDraft))
            {
                var result = FinancialCalculator.CalculateBaristaFire(baristaDraft.ToFireInputs(), baristaDraft.PartTimeAnnualIncome);
                await baristaExportService.ShareAsync(baristaDraft, result);
                ExportStatusMessage = "Your Barista FIRE workbook is ready to share.";
            }
            else if (IsReverseFire && TryCreateReverseDraft(out var reverseDraft))
            {
                var result = FinancialCalculator.CalculateReverseFire(reverseDraft.ToFireInputs());
                await reverseExportService.ShareAsync(reverseDraft, result);
                ExportStatusMessage = "Your Reverse FIRE workbook is ready to share.";
            }
            else if (IsWithdrawalRate && TryCreateWithdrawalRateDraft(out var withdrawalDraft))
            {
                var result = FinancialCalculator.CalculateWithdrawal(
                    withdrawalDraft.PortfolioValue,
                    withdrawalDraft.WithdrawalRate,
                    withdrawalDraft.ExpectedReturn,
                    withdrawalDraft.InflationRate,
                    withdrawalDraft.RetirementYears);
                await withdrawalRateExportService.ShareAsync(withdrawalDraft, result);
                ExportStatusMessage = "Your Withdrawal Rate workbook is ready to share.";
            }
            else if (IsSavingsInvestmentRate && TryCreateSavingsInvestmentDraft(out var investmentDraft))
            {
                var result = FinancialCalculator.CalculateInvestmentGrowth(investmentDraft.ToInputs());
                await savingsInvestmentExportService.ShareAsync(investmentDraft, result);
                ExportStatusMessage = "Your Savings & Investment Rate workbook is ready to share.";
            }
            else if (IsHealthcareGap && TryCreateHealthcareGapDraft(out var healthcareDraft))
            {
                var result = FinancialCalculator.CalculateHealthcareGap(healthcareDraft.ToInputs());
                await healthcareGapExportService.ShareAsync(healthcareDraft, result);
                ExportStatusMessage = "Your Healthcare Gap workbook is ready to share.";
            }
            else if (IsDebtPayoff && TryCreateDebtPayoffDraft(out var debtDraft))
            {
                var totalMinimumPayments = debtDraft.Debts.Sum(debt => debt.MinimumPayment);
                DebtPayoffResult result;
                if (debtDraft.Mode == DebtPayoffMode.TargetTimeline)
                {
                    result = FinancialCalculator.CalculateDebtPayoffByTimeline(
                        debtDraft.Debts,
                        debtDraft.TargetMonths,
                        debtDraft.Strategy == DebtPayoffStrategy.Snowball,
                        debtDraft.ExtraPayment)?.Result
                        ?? throw new InvalidOperationException("A payoff timeline could not be calculated for these debts.");
                }
                else
                {
                    if (debtDraft.MonthlyBudget < totalMinimumPayments)
                    {
                        throw new InvalidOperationException("Monthly budget must cover minimum payments.");
                    }

                    result = debtDraft.Strategy == DebtPayoffStrategy.Snowball
                        ? FinancialCalculator.CalculateSnowballPayoff(debtDraft.Debts, debtDraft.MonthlyBudget, debtDraft.ExtraPayment)
                        : FinancialCalculator.CalculateAvalanchePayoff(debtDraft.Debts, debtDraft.MonthlyBudget, debtDraft.ExtraPayment);
                }

                await debtPayoffExportService.ShareAsync(debtDraft, result);
                ExportStatusMessage = "Your Debt Payoff workbook is ready to share.";
            }
            else if (IsRetirementCashFlow && TryCreateDeferredCompensationDraft(out var deferredDraft))
            {
                var result = DeferredCompensationCalculator.Calculate(deferredDraft.ToInputs());
                await deferredCompensationExportService.ShareAsync(deferredDraft, result);
                ExportStatusMessage = "Your Retirement Cash Flow workbook is ready to share.";
            }
        }
        catch (Exception)
        {
            ExportStatusMessage = IsCoastFire
                ? "The Coast FIRE workbook could not be created locally."
                : IsLeanFire
                    ? "The Lean FIRE workbook could not be created locally."
                    : IsFatFire
                        ? "The Fat FIRE workbook could not be created locally."
                        : IsBaristaFire
                            ? "The Barista FIRE workbook could not be created locally."
                            : IsReverseFire
                                ? "The Reverse FIRE workbook could not be created locally."
                                : IsWithdrawalRate
                                    ? "The Withdrawal Rate workbook could not be created locally."
                                    : IsSavingsInvestmentRate
                                        ? "The Savings & Investment Rate workbook could not be created locally."
                                        : IsHealthcareGap
                                            ? "The Healthcare Gap workbook could not be created locally."
                                            : IsDebtPayoff
                                                ? "The Debt Payoff workbook could not be created locally."
                                                : IsRetirementCashFlow
                                                    ? "The Retirement Cash Flow workbook could not be created locally."
                                            : "The Standard FIRE workbook could not be created locally.";
        }
    }

    partial void OnCurrentAgeTextChanged(string value) => OnDraftInputChanged();
    partial void OnRetirementAgeTextChanged(string value) => OnDraftInputChanged();
    partial void OnCurrentSavingsTextChanged(string value) => OnDraftInputChanged();
    partial void OnAnnualContributionTextChanged(string value) => OnDraftInputChanged();
    partial void OnAnnualIncomeTextChanged(string value) => OnDraftInputChanged();
    partial void OnAnnualExpensesTextChanged(string value) => OnDraftInputChanged();
    partial void OnPartTimeAnnualIncomeTextChanged(string value) => OnDraftInputChanged();
    partial void OnPortfolioValueTextChanged(string value) => OnDraftInputChanged();
    partial void OnRetirementYearsTextChanged(string value) => OnDraftInputChanged();
    partial void OnStartingAmountTextChanged(string value) => OnDraftInputChanged();
    partial void OnInvestmentContributionTextChanged(string value) => OnDraftInputChanged();
    partial void OnYearsInvestingTextChanged(string value) => OnDraftInputChanged();
    partial void OnInvestmentAnnualIncomeTextChanged(string value) => OnDraftInputChanged();
    partial void OnHealthcareCurrentAgeTextChanged(string value) => OnDraftInputChanged();
    partial void OnRetirementCurrentAgeTextChanged(string value) => OnDraftInputChanged();
    partial void OnRetirementSemiAgeTextChanged(string value) => OnDraftInputChanged();
    partial void OnRetirementPlanThroughAgeTextChanged(string value) => OnDraftInputChanged();
    partial void OnRetirementExpensesTextChanged(string value) => OnDraftInputChanged();
    partial void OnRetirementInflationTextChanged(string value) => OnDraftInputChanged();
    partial void OnEarlyRetirementAgeTextChanged(string value) => OnDraftInputChanged();
    partial void OnMedicareAgeTextChanged(string value) => OnDraftInputChanged();
    partial void OnMonthlyPremiumTextChanged(string value) => OnDraftInputChanged();
    partial void OnAnnualDeductibleTextChanged(string value) => OnDraftInputChanged();
    partial void OnAnnualOutOfPocketTextChanged(string value) => OnDraftInputChanged();
    partial void OnDebtMonthlyBudgetTextChanged(string value) => OnDraftInputChanged();
    partial void OnDebtExtraPaymentTextChanged(string value) => OnDraftInputChanged();
    partial void OnDebtTargetMonthsTextChanged(string value) => OnDraftInputChanged();
    partial void OnDebtPayoffModeChanged(DebtPayoffMode value) => OnDraftInputChanged();
    partial void OnDebtPayoffStrategyChanged(DebtPayoffStrategy value) => OnDraftInputChanged();
    partial void OnWithdrawOnlyAfterRetirementChanged(bool value) => OnDraftInputChanged();
    partial void OnReinvestRetirementSurplusChanged(bool value) => OnDraftInputChanged();
    partial void OnExpectedReturnTextChanged(string value) => OnDraftInputChanged();
    partial void OnInflationRateTextChanged(string value) => OnDraftInputChanged();
    partial void OnWithdrawalRateTextChanged(string value) => OnDraftInputChanged();

    partial void OnSelectedPresetChanged(StandardFirePreset? value)
    {
        if (value is not null && IsStandardFire)
        {
            ValidationMessage = string.Empty;
            ApplyDraft(value.Draft);
        }
    }

    partial void OnContributionFrequencyChanged(ContributionFrequency value) => OnDraftInputChanged();

    [RelayCommand]
    private void SetContributionFrequency(string frequency)
    {
        ContributionFrequency = string.Equals(frequency, "yearly", StringComparison.OrdinalIgnoreCase)
            ? ContributionFrequency.Yearly
            : ContributionFrequency.Monthly;
    }

    [RelayCommand]
    private void AddDebt()
    {
        DebtItems.Add(new DebtEditorItem
        {
            Name = "New debt",
            BalanceText = "1000",
            RateText = "19.99",
            MinimumPaymentText = "50"
        });
    }

    [RelayCommand]
    private void RemoveDebt(DebtEditorItem? debt)
    {
        if (debt is not null)
        {
            DebtItems.Remove(debt);
        }
    }

    [RelayCommand]
    private void AddRetirementAccount()
    {
        RetirementAccounts.Add(new RetirementAccountEditorItem
        {
            Name = "New retirement account",
            AvailableAgeText = RetirementSemiAgeText,
            IsExpanded = true
        });
    }

    [RelayCommand]
    private void RemoveRetirementAccount(RetirementAccountEditorItem? account)
    {
        if (account is not null)
        {
            RetirementAccounts.Remove(account);
        }
    }

    [RelayCommand]
    private void AddRetirementIncome()
    {
        RetirementIncomeSources.Add(new RetirementIncomeEditorItem
        {
            Name = "New retirement income",
            StartAgeText = RetirementSemiAgeText,
            EndAgeText = RetirementPlanThroughAgeText,
            IsExpanded = true
        });
    }

    [RelayCommand]
    private void RemoveRetirementIncome(RetirementIncomeEditorItem? income)
    {
        if (income is not null)
        {
            RetirementIncomeSources.Remove(income);
        }
    }

    [RelayCommand]
    private void AddRetirementExpense()
    {
        RetirementAdditionalExpenses.Add(new RetirementExpenseEditorItem
        {
            Name = "New retirement expense",
            StartAgeText = RetirementSemiAgeText,
            IsExpanded = true
        });
    }

    [RelayCommand]
    private void RemoveRetirementExpense(RetirementExpenseEditorItem? expense)
    {
        if (expense is not null)
        {
            RetirementAdditionalExpenses.Remove(expense);
        }
    }

    [RelayCommand]
    private void SetDebtStrategy(string strategy)
    {
        DebtPayoffStrategy = string.Equals(strategy, "avalanche", StringComparison.OrdinalIgnoreCase)
            ? DebtPayoffStrategy.Avalanche
            : DebtPayoffStrategy.Snowball;
    }

    [RelayCommand]
    private void SetDebtPayoffMode(string mode)
    {
        DebtPayoffMode = string.Equals(mode, "target", StringComparison.OrdinalIgnoreCase)
            ? DebtPayoffMode.TargetTimeline
            : DebtPayoffMode.FixedBudget;
    }

    private void ApplyDraft(StandardFireDraft draft)
    {
        isApplyingDraft = true;
        CurrentAgeText = draft.CurrentAge.ToString(CultureInfo.CurrentCulture);
        RetirementAgeText = draft.RetirementAge.ToString(CultureInfo.CurrentCulture);
        CurrentSavingsText = FormatNumber(draft.CurrentSavings);
        AnnualContributionText = FormatNumber(draft.AnnualContribution);
        AnnualIncomeText = FormatNumber(draft.AnnualIncome);
        AnnualExpensesText = FormatNumber(draft.AnnualExpenses);
        ExpectedReturnText = FormatNumber(draft.ExpectedReturn * 100);
        InflationRateText = FormatNumber(draft.InflationRate * 100);
        WithdrawalRateText = FormatNumber(draft.WithdrawalRate * 100);
        isApplyingDraft = false;
        RecalculateAndSave();
    }

    private void ApplyDraft(CoastFireDraft draft)
    {
        isApplyingDraft = true;
        CurrentAgeText = draft.CurrentAge.ToString(CultureInfo.CurrentCulture);
        RetirementAgeText = draft.RetirementAge.ToString(CultureInfo.CurrentCulture);
        CurrentSavingsText = FormatNumber(draft.CurrentSavings);
        AnnualContributionText = FormatNumber(draft.AnnualContribution);
        AnnualExpensesText = FormatNumber(draft.AnnualExpenses);
        ExpectedReturnText = FormatNumber(draft.ExpectedReturn * 100);
        InflationRateText = FormatNumber(draft.InflationRate * 100);
        WithdrawalRateText = FormatNumber(draft.WithdrawalRate * 100);
        isApplyingDraft = false;
        RecalculateAndSave();
    }

    private void ApplyDraft(LeanFireDraft draft)
    {
        isApplyingDraft = true;
        CurrentAgeText = draft.CurrentAge.ToString(CultureInfo.CurrentCulture);
        RetirementAgeText = draft.RetirementAge.ToString(CultureInfo.CurrentCulture);
        CurrentSavingsText = FormatNumber(draft.CurrentSavings);
        AnnualContributionText = FormatNumber(draft.AnnualContribution);
        AnnualIncomeText = FormatNumber(draft.AnnualIncome);
        AnnualExpensesText = FormatNumber(draft.AnnualExpenses);
        ExpectedReturnText = FormatNumber(draft.ExpectedReturn * 100);
        InflationRateText = FormatNumber(draft.InflationRate * 100);
        WithdrawalRateText = FormatNumber(draft.WithdrawalRate * 100);
        isApplyingDraft = false;
        RecalculateAndSave();
    }

    private void ApplyDraft(FatFireDraft draft)
    {
        isApplyingDraft = true;
        CurrentAgeText = draft.CurrentAge.ToString(CultureInfo.CurrentCulture);
        RetirementAgeText = draft.RetirementAge.ToString(CultureInfo.CurrentCulture);
        CurrentSavingsText = FormatNumber(draft.CurrentSavings);
        AnnualContributionText = FormatNumber(draft.AnnualContribution);
        AnnualIncomeText = FormatNumber(draft.AnnualIncome);
        AnnualExpensesText = FormatNumber(draft.AnnualExpenses);
        ExpectedReturnText = FormatNumber(draft.ExpectedReturn * 100);
        InflationRateText = FormatNumber(draft.InflationRate * 100);
        WithdrawalRateText = FormatNumber(draft.WithdrawalRate * 100);
        isApplyingDraft = false;
        RecalculateAndSave();
    }

    private void ApplyDraft(BaristaFireDraft draft)
    {
        isApplyingDraft = true;
        CurrentAgeText = draft.CurrentAge.ToString(CultureInfo.CurrentCulture);
        CurrentSavingsText = FormatNumber(draft.CurrentSavings);
        AnnualContributionText = FormatNumber(draft.AnnualContribution);
        AnnualExpensesText = FormatNumber(draft.AnnualExpenses);
        PartTimeAnnualIncomeText = FormatNumber(draft.PartTimeAnnualIncome);
        ExpectedReturnText = FormatNumber(draft.ExpectedReturn * 100);
        InflationRateText = FormatNumber(draft.InflationRate * 100);
        WithdrawalRateText = FormatNumber(draft.WithdrawalRate * 100);
        isApplyingDraft = false;
        RecalculateAndSave();
    }

    private void ApplyDraft(ReverseFireDraft draft)
    {
        isApplyingDraft = true;
        CurrentAgeText = draft.CurrentAge.ToString(CultureInfo.CurrentCulture);
        RetirementAgeText = draft.TargetRetirementAge.ToString(CultureInfo.CurrentCulture);
        CurrentSavingsText = FormatNumber(draft.CurrentSavings);
        AnnualExpensesText = FormatNumber(draft.AnnualExpenses);
        ExpectedReturnText = FormatNumber(draft.ExpectedReturn * 100);
        InflationRateText = FormatNumber(draft.InflationRate * 100);
        WithdrawalRateText = FormatNumber(draft.WithdrawalRate * 100);
        isApplyingDraft = false;
        RecalculateAndSave();
    }

    private void ApplyDraft(WithdrawalRateDraft draft)
    {
        isApplyingDraft = true;
        PortfolioValueText = FormatNumber(draft.PortfolioValue);
        WithdrawalRateText = FormatNumber(draft.WithdrawalRate * 100);
        ExpectedReturnText = FormatNumber(draft.ExpectedReturn * 100);
        InflationRateText = FormatNumber(draft.InflationRate * 100);
        RetirementYearsText = draft.RetirementYears.ToString(CultureInfo.CurrentCulture);
        isApplyingDraft = false;
        RecalculateAndSave();
    }

    private void ApplyDraft(SavingsInvestmentDraft draft)
    {
        isApplyingDraft = true;
        StartingAmountText = FormatNumber(draft.StartingAmount);
        InvestmentContributionText = FormatNumber(draft.ContributionAmount);
        ContributionFrequency = draft.ContributionFrequency;
        YearsInvestingText = draft.YearsInvesting.ToString(CultureInfo.CurrentCulture);
        ExpectedReturnText = FormatNumber(draft.ExpectedReturn * 100);
        InflationRateText = FormatNumber(draft.InflationRate * 100);
        InvestmentAnnualIncomeText = FormatNumber(draft.AnnualIncome);
        CurrentAgeText = draft.CurrentAge.ToString(CultureInfo.CurrentCulture);
        isApplyingDraft = false;
        RecalculateAndSave();
    }

    private void ApplyDraft(HealthcareGapDraft draft)
    {
        isApplyingDraft = true;
        HealthcareCurrentAgeText = draft.CurrentAge.ToString(CultureInfo.CurrentCulture);
        EarlyRetirementAgeText = draft.EarlyRetirementAge.ToString(CultureInfo.CurrentCulture);
        MedicareAgeText = draft.MedicareAge.ToString(CultureInfo.CurrentCulture);
        MonthlyPremiumText = FormatNumber(draft.MonthlyPremium);
        AnnualDeductibleText = FormatNumber(draft.AnnualDeductible);
        AnnualOutOfPocketText = FormatNumber(draft.AnnualOutOfPocket);
        InflationRateText = FormatNumber(draft.InflationRate * 100);
        isApplyingDraft = false;
        RecalculateAndSave();
    }

    private void ApplyDraft(DebtPayoffDraft draft)
    {
        isApplyingDraft = true;
        ReplaceDebtItems(draft.Debts);
        DebtMonthlyBudgetText = FormatNumber(draft.MonthlyBudget);
        DebtExtraPaymentText = FormatNumber(draft.ExtraPayment);
        DebtTargetMonthsText = draft.TargetMonths.ToString(CultureInfo.CurrentCulture);
        DebtPayoffMode = draft.Mode;
        DebtPayoffStrategy = draft.Strategy;
        isApplyingDraft = false;
        RecalculateAndSave();
    }

    private void ApplyDefaultDraft()
    {
        if (IsStandardFire)
        {
            ApplyDraft(calculatorDefaultsService.StandardFire);
        }
        else if (IsCoastFire)
        {
            ApplyDraft(calculatorDefaultsService.CoastFire);
        }
        else if (IsLeanFire)
        {
            ApplyDraft(calculatorDefaultsService.LeanFire);
        }
        else if (IsFatFire)
        {
            ApplyDraft(calculatorDefaultsService.FatFire);
        }
        else if (IsBaristaFire)
        {
            ApplyDraft(calculatorDefaultsService.BaristaFire);
        }
        else if (IsReverseFire)
        {
            ApplyDraft(calculatorDefaultsService.ReverseFire);
        }
        else if (IsWithdrawalRate)
        {
            ApplyDraft(calculatorDefaultsService.WithdrawalRate);
        }
        else if (IsSavingsInvestmentRate)
        {
            ApplyDraft(calculatorDefaultsService.SavingsInvestment);
        }
        else if (IsHealthcareGap)
        {
            ApplyDraft(calculatorDefaultsService.HealthcareGap);
        }
        else if (IsDebtPayoff)
        {
            ApplyDraft(DebtPayoffDraft.Default);
        }
        else if (IsRetirementCashFlow)
        {
            ApplyDeferredCompensationDraft(calculatorDefaultsService.RetirementCashFlow);
        }
    }

    private void RecalculateAndSave()
    {
        if (isApplyingDraft)
        {
            return;
        }

        if (IsStandardFire && TryCreateDraft(out var standardDraft))
        {
            var result = FinancialCalculator.CalculateStandardFire(standardDraft.ToFireInputs());
            ValidationMessage = string.Empty;
            FireNumberText = FormatCurrency(result.FireNumber);
            YearsToFireText = double.IsPositiveInfinity(result.YearsToFire)
                ? "Not reachable with these inputs"
                : $"{result.YearsToFire:N1} years";
            FireAgeText = double.IsPositiveInfinity(result.FireAge) ? "--" : $"Age {result.FireAge:N1}";
            SavingsRateText = $"{result.SavingsRate:P1}";
            MonthlyContributionText = FormatCurrency(result.MonthlyContribution);
            ProgressToFire = result.FireNumber <= 0 ? 0 : Math.Clamp(standardDraft.CurrentSavings / result.FireNumber, 0, 1);
            ProgressDescription = $"{ProgressToFire:P0} of your FIRE Number is currently funded.";
            ProjectionSummary = double.IsPositiveInfinity(result.YearsToFire)
                ? "The current contribution and return assumptions do not reach the FIRE Number."
                : $"At the current assumptions, your portfolio is projected to reach {FormatCurrency(result.FireNumber)} in approximately {result.YearsToFire:N1} years.";
            UpdateProjectionChart(result);
            ScheduleSave(standardDraft);
        }
        else if (IsCoastFire && TryCreateCoastDraft(out var coastDraft))
        {
            var result = FinancialCalculator.CalculateCoastFire(coastDraft.ToFireInputs());
            ValidationMessage = string.Empty;
            CoastNumberText = FormatCurrency(result.CoastNumber);
            FullFireNumberText = FormatCurrency(result.FireNumber);
            YearsToCoastText = result.AlreadyCoasting ? "Already coasting" : $"{result.YearsToCoast:N1} years";
            CoastStatusText = result.AlreadyCoasting
                ? "You're already Coast FIRE!"
                : $"{result.YearsToCoast:N1} years to Coast FIRE";
            var progress = result.CoastNumber <= 0 ? 0 : Math.Clamp(coastDraft.CurrentSavings / result.CoastNumber, 0, 1);
            CoastProgressDescription = $"{progress:P0} of your Coast FIRE Number is currently funded.";
            CoastProjectionSummary = $"By age {result.Projections[^1].Age:0}, coasting projects {FormatCurrency(result.Projections[^1].Portfolio)} and continuing contributions projects {FormatCurrency(result.ProjectionsWithContributions[^1].Portfolio)}.";
            UpdateCoastProjectionChart(result);
            ScheduleSave(coastDraft);
        }
        else if (IsLeanFire && TryCreateLeanDraft(out var leanDraft))
        {
            var result = FinancialCalculator.CalculateLeanFire(leanDraft.ToFireInputs()).Standard;
            ValidationMessage = string.Empty;
            FireNumberText = FormatCurrency(result.FireNumber);
            YearsToFireText = double.IsPositiveInfinity(result.YearsToFire)
                ? "Not reachable with these inputs"
                : $"{result.YearsToFire:N1} years";
            FireAgeText = double.IsPositiveInfinity(result.FireAge) ? "--" : $"Age {result.FireAge:N1}";
            SavingsRateText = $"{result.SavingsRate:P1}";
            MonthlyContributionText = FormatCurrency(result.MonthlyContribution);
            ProgressToFire = result.FireNumber <= 0 ? 0 : Math.Clamp(leanDraft.CurrentSavings / result.FireNumber, 0, 1);
            ProgressDescription = $"{ProgressToFire:P0} of your Lean FIRE Number is currently funded.";
            ProjectionSummary = $"At the current assumptions, your portfolio is projected to reach {FormatCurrency(result.FireNumber)} in approximately {result.YearsToFire:N1} years.";
            LeanStatusText = leanDraft.IsWithinLeanThreshold ? "You're in Lean territory!" : "Expenses above the Lean threshold";
            LeanGuidanceText = leanDraft.IsWithinLeanThreshold
                ? $"Your annual expenses are within the {FormatCurrency(FinancialCalculator.LeanFireThreshold)} Lean FIRE guideline."
                : $"Lean FIRE calculations use the {FormatCurrency(FinancialCalculator.LeanFireThreshold)} guideline; your entered expenses are {FormatCurrency(leanDraft.AnnualExpenses)}.";
            UpdateProjectionChart(result);
            ScheduleSave(leanDraft);
        }
        else if (IsFatFire && TryCreateFatDraft(out var fatDraft))
        {
            var result = FinancialCalculator.CalculateFatFire(fatDraft.ToFireInputs()).Standard;
            ValidationMessage = string.Empty;
            FireNumberText = FormatCurrency(result.FireNumber);
            YearsToFireText = double.IsPositiveInfinity(result.YearsToFire)
                ? "Not reachable with these inputs"
                : $"{result.YearsToFire:N1} years";
            FireAgeText = double.IsPositiveInfinity(result.FireAge) ? "--" : $"Age {result.FireAge:N1}";
            SavingsRateText = $"{result.SavingsRate:P1}";
            MonthlyContributionText = FormatCurrency(result.MonthlyContribution);
            ProgressToFire = result.FireNumber <= 0 ? 0 : Math.Clamp(fatDraft.CurrentSavings / result.FireNumber, 0, 1);
            ProgressDescription = $"{ProgressToFire:P0} of your Fat FIRE Number is currently funded.";
            ProjectionSummary = $"At the current assumptions, your portfolio is projected to reach {FormatCurrency(result.FireNumber)} in approximately {result.YearsToFire:N1} years.";
            FatStatusText = fatDraft.IsWithinFatThreshold ? "You're in Fat FIRE territory!" : "Below the Fat FIRE threshold";
            FatGuidanceText = fatDraft.IsWithinFatThreshold
                ? $"Your annual expenses meet the {FormatCurrency(FinancialCalculator.FatFireThreshold)} Fat FIRE guideline."
                : $"Fat FIRE typically starts at {FormatCurrency(FinancialCalculator.FatFireThreshold)} in annual expenses; your current plan uses {FormatCurrency(fatDraft.AnnualExpenses)}.";
            UpdateProjectionChart(result);
            ScheduleSave(fatDraft);
        }
        else if (IsBaristaFire && TryCreateBaristaDraft(out var baristaDraft))
        {
            var result = FinancialCalculator.CalculateBaristaFire(baristaDraft.ToFireInputs(), baristaDraft.PartTimeAnnualIncome);
            ValidationMessage = string.Empty;
            BaristaNumberText = FormatCurrency(result.BaristaNumber);
            FullFireNumberText = FormatCurrency(result.FullFireNumber);
            BaristaYearsText = double.IsPositiveInfinity(result.YearsToBaristaFire)
                ? "Not reachable with these inputs"
                : $"{result.YearsToBaristaFire:N1} years";
            BaristaReductionText = $"{FormatCurrency(result.SavingsFromPartTime)} less needed with part-time income.";
            var progress = result.BaristaNumber <= 0 ? 0 : Math.Clamp(baristaDraft.CurrentSavings / result.BaristaNumber, 0, 1);
            BaristaProgressDescription = $"{progress:P0} of your Barista FIRE Number is currently funded.";
            BaristaProjectionSummary = double.IsPositiveInfinity(result.YearsToBaristaFire)
                ? "The current contribution and return assumptions do not reach the Barista FIRE Number."
                : $"At the current assumptions, your portfolio is projected to reach {FormatCurrency(result.BaristaNumber)} in approximately {result.YearsToBaristaFire:N1} years.";
            UpdateBaristaProjectionChart(result);
            ScheduleSave(baristaDraft);
        }
        else if (IsReverseFire && TryCreateReverseDraft(out var reverseDraft))
        {
            var result = FinancialCalculator.CalculateReverseFire(reverseDraft.ToFireInputs());
            ValidationMessage = string.Empty;
            ReverseRequiredAnnualSavingsText = FormatCurrency(result.RequiredAnnualSavings);
            ReverseRequiredMonthlySavingsText = FormatCurrency(result.RequiredMonthlySavings);
            ReverseYearsText = $"{result.YearsToFire:0} years";
            ReverseCurrentGrowthText = FormatCurrency(result.CurrentWillGrowTo);
            ReverseStatusText = result.AlreadyAchievable
                ? "You're already on track!"
                : $"To FIRE by age {reverseDraft.TargetRetirementAge}, you need to save {FormatCurrency(result.RequiredMonthlySavings)} per month.";
            ReverseProjectionSummary = $"Your current savings are projected to grow to {FormatCurrency(result.CurrentWillGrowTo)} by age {reverseDraft.TargetRetirementAge}; the target portfolio is {FormatCurrency(result.FireNumber)}.";
            UpdateReverseProjectionChart(result);
            ScheduleSave(reverseDraft);
        }
        else if (IsWithdrawalRate && TryCreateWithdrawalRateDraft(out var withdrawalDraft))
        {
            var result = FinancialCalculator.CalculateWithdrawal(
                withdrawalDraft.PortfolioValue,
                withdrawalDraft.WithdrawalRate,
                withdrawalDraft.ExpectedReturn,
                withdrawalDraft.InflationRate,
                withdrawalDraft.RetirementYears);
            ValidationMessage = string.Empty;
            WithdrawalAnnualText = FormatCurrency(result.AnnualWithdrawal);
            WithdrawalMonthlyText = FormatCurrency(result.MonthlyWithdrawal);
            WithdrawalLongevityText = result.PortfolioLongevity >= withdrawalDraft.RetirementYears
                ? $"{withdrawalDraft.RetirementYears}+ years"
                : $"{result.PortfolioLongevity:0} years";
            WithdrawalSuccessText = result.SuccessRate.ToString("P0", CultureInfo.CurrentCulture);
            WithdrawalStatusText = result.PortfolioLongevity >= withdrawalDraft.RetirementYears
                ? "Your withdrawal rate is sustainable for this goal."
                : "Your portfolio may run out before this goal.";
            WithdrawalRateAnalysisText = string.Join("  ", result.RateAnalysis.Select(analysis =>
                $"{analysis.Rate:P1}: {(analysis.Years >= withdrawalDraft.RetirementYears ? "Sustainable" : $"{analysis.Years:0} years")}"));
            UpdateWithdrawalProjectionChart(result);
            ScheduleSave(withdrawalDraft);
        }
        else if (IsSavingsInvestmentRate && TryCreateSavingsInvestmentDraft(out var investmentDraft))
        {
            var result = FinancialCalculator.CalculateInvestmentGrowth(investmentDraft.ToInputs());
            ValidationMessage = string.Empty;
            InvestmentSavingsRateText = result.SavingsRate.ToString("P1", CultureInfo.CurrentCulture);
            InvestmentAnnualContributionText = FormatCurrency(result.AnnualContribution);
            InvestmentFinalBalanceText = FormatCurrency(result.FinalNominalBalance);
            InvestmentInflationAdjustedText = FormatCurrency(result.FinalInflationAdjustedBalance);
            InvestmentGrowthText = FormatCurrency(result.TotalGrowth);
            InvestmentCategoryText = result.SavingsCategory;
            InvestmentProjectionSummary = $"After {investmentDraft.YearsInvesting} years, your portfolio is projected to reach {FormatCurrency(result.FinalNominalBalance)}; that is {FormatCurrency(result.FinalInflationAdjustedBalance)} in today's dollars.";
            UpdateInvestmentProjectionChart(result);
            ScheduleSave(investmentDraft);
        }
        else if (IsHealthcareGap && TryCreateHealthcareGapDraft(out var healthcareDraft))
        {
            var result = FinancialCalculator.CalculateHealthcareGap(healthcareDraft.ToInputs());
            ValidationMessage = string.Empty;
            HealthcareGapYearsText = $"{result.GapYears} years";
            HealthcareAnnualCostText = FormatCurrency(result.AnnualCost);
            HealthcareTotalCostText = FormatCurrency(result.TotalCost);
            HealthcareAverageAnnualCostText = FormatCurrency(result.AverageAnnualCost);
            HealthcareSubsidyEstimateText = $"$30k income: {FormatCurrency(result.EstimatedSubsidy30k)}  |  $50k: {FormatCurrency(result.EstimatedSubsidy50k)}  |  $75k: {FormatCurrency(result.EstimatedSubsidy75k)}";
            HealthcareProjectionSummary = result.GapYears == 0
                ? "Your retirement age is at or beyond Medicare eligibility, so no pre-Medicare coverage gap is projected."
                : $"From age {healthcareDraft.EarlyRetirementAge} to {healthcareDraft.MedicareAge}, estimated healthcare costs total {FormatCurrency(result.TotalCost)} before Medicare eligibility.";
            UpdateHealthcareProjectionChart(result);
            ScheduleSave(healthcareDraft);
        }
        else if (IsRetirementCashFlow && TryCreateDeferredCompensationDraft(out var deferredDraft))
        {
            var result = DeferredCompensationCalculator.Calculate(deferredDraft.ToInputs());
            ValidationMessage = string.Empty;
            RetirementCurrentBalanceText = FormatCurrency(result.CurrentBalance);
            RetirementBalanceAtSemiText = FormatCurrency(result.BalanceAtSemiRetirement);
            RetirementEndingBalanceText = FormatCurrency(result.EndingBalance);
            RetirementFundedYearsText = $"{result.FundedYears} of {result.Projections.Count} years funded";
            ProjectionSeries =
            [
                CreateProjectionSeries("Portfolio balance", result.Projections.Select(point => point.TotalBalance), new SKColor(43, 111, 83)),
                CreateProjectionSeries("Expenses", result.Projections.Select(point => point.Expenses), new SKColor(190, 81, 66))
            ];
            ProjectionXAxes = [new Axis { Name = "Age", Labels = result.Projections.Where((_, index) => index % Math.Max(1, result.Projections.Count / 6) == 0).Select(point => point.Age.ToString(CultureInfo.CurrentCulture)).ToArray(), TextSize = 10 }];
            ProjectionChartDescription = $"Retirement cash-flow projection through age {deferredDraft.PlanThroughAge}. Ending balance is {FormatCurrency(result.EndingBalance)}.";
            UpdateRetirementBucketChart(deferredDraft, result);
            ScheduleSave(deferredDraft);
        }
        else if (IsDebtPayoff && TryCreateDebtPayoffDraft(out var debtDraft))
        {
            var totalMinimumPayments = debtDraft.Debts.Sum(debt => debt.MinimumPayment);
            DebtPayoffResult result;
            if (debtDraft.Mode == DebtPayoffMode.TargetTimeline)
            {
                var timeline = FinancialCalculator.CalculateDebtPayoffByTimeline(
                    debtDraft.Debts,
                    debtDraft.TargetMonths,
                    debtDraft.Strategy == DebtPayoffStrategy.Snowball,
                    debtDraft.ExtraPayment);
                if (timeline is null)
                {
                    ValidationMessage = "A payoff timeline could not be calculated for these debts.";
                    return;
                }

                result = timeline.Result;
            }
            else
            {
                if (debtDraft.MonthlyBudget < totalMinimumPayments)
                {
                    ValidationMessage = $"Monthly budget must be at least {FormatCurrency(totalMinimumPayments)} to cover minimum payments.";
                    return;
                }

                result = debtDraft.Strategy == DebtPayoffStrategy.Snowball
                    ? FinancialCalculator.CalculateSnowballPayoff(debtDraft.Debts, debtDraft.MonthlyBudget, debtDraft.ExtraPayment)
                    : FinancialCalculator.CalculateAvalanchePayoff(debtDraft.Debts, debtDraft.MonthlyBudget, debtDraft.ExtraPayment);
            }
            ValidationMessage = string.Empty;
            DebtTotalText = FormatCurrency(debtDraft.Debts.Sum(debt => debt.Balance));
            DebtMinimumPaymentsText = FormatCurrency(totalMinimumPayments);
            DebtPayoffTimeText = $"{result.TotalMonths} months";
            DebtInterestText = FormatCurrency(result.TotalInterest);
            DebtPaymentText = FormatCurrency(result.MonthlyPayment);
            DebtStrategySummary = debtDraft.Strategy == DebtPayoffStrategy.Snowball
                ? $"Snowball pays the smallest balance first. Your payoff order is {string.Join(", ", result.PayoffOrder)}."
                : $"Avalanche pays the highest interest rate first. Your payoff order is {string.Join(", ", result.PayoffOrder)}.";
            var comparisonPayment = result.MonthlyPayment;
            var snowball = FinancialCalculator.CalculateSnowballPayoff(debtDraft.Debts, comparisonPayment);
            var avalanche = FinancialCalculator.CalculateAvalanchePayoff(debtDraft.Debts, comparisonPayment);
            DebtSnowballComparisonText = $"{snowball.TotalMonths} months, {FormatCurrency(snowball.TotalInterest)} interest";
            DebtAvalancheComparisonText = $"{avalanche.TotalMonths} months, {FormatCurrency(avalanche.TotalInterest)} interest";
            UpdateDebtProjectionChart(result);
            UpdateDebtBreakdownChart(result);
            ScheduleSave(debtDraft);
        }
    }

    private void ApplyDeferredCompensationDraft(DeferredCompensationDraft draft)
    {
        isApplyingDraft = true;
        RetirementCurrentAgeText = draft.CurrentAge.ToString(CultureInfo.CurrentCulture);
        RetirementSemiAgeText = draft.SemiRetirementAge.ToString(CultureInfo.CurrentCulture);
        RetirementPlanThroughAgeText = draft.PlanThroughAge.ToString(CultureInfo.CurrentCulture);
        RetirementExpensesText = draft.AnnualExpenses.ToString("0.##", CultureInfo.CurrentCulture);
        RetirementInflationText = (draft.InflationRate * 100).ToString("0.##", CultureInfo.CurrentCulture);
        WithdrawOnlyAfterRetirement = draft.WithdrawOnlyAfterRetirement;
        ReinvestRetirementSurplus = draft.ReinvestSurplus;
        ReplaceRetirementAccounts(draft.Accounts);
        ReplaceRetirementIncomeSources(draft.IncomeSources);
        ReplaceRetirementExpenses(draft.AdditionalExpenses);
        isApplyingDraft = false;
        RecalculateAndSave();
    }

    private bool TryCreateDeferredCompensationDraft(out DeferredCompensationDraft draft)
    {
        draft = calculatorDefaultsService.RetirementCashFlow;
        if (!TryParseWholeNumber(RetirementCurrentAgeText, out var currentAge) || currentAge is < 18 or > 100
            || !TryParseWholeNumber(RetirementSemiAgeText, out var semiAge) || semiAge < currentAge
            || !TryParseWholeNumber(RetirementPlanThroughAgeText, out var planThroughAge) || planThroughAge < semiAge)
        {
            ValidationMessage = "Enter ages from 18 to 100 in chronological order.";
            return false;
        }
        if (!TryParseNonNegative(RetirementExpensesText, out var annualExpenses) || !TryParsePercentage(RetirementInflationText, 0, 10, out var inflationRate))
        {
            ValidationMessage = "Enter a non-negative annual expense and inflation from 0% to 10%.";
            return false;
        }

        var accounts = new List<RetirementAccount>();
        foreach (var editor in RetirementAccounts)
        {
            if (!editor.TryCreateAccount(out var account))
            {
                ValidationMessage = "Complete every retirement account with valid amounts, percentages, and an available age from 18 to 100.";
                return false;
            }

            accounts.Add(account);
        }

        var incomeSources = new List<RetirementIncomeSource>();
        foreach (var editor in RetirementIncomeSources)
        {
            if (!editor.TryCreateIncome(out var income))
            {
                ValidationMessage = "Complete every income source with valid amounts, percentages, and chronological ages.";
                return false;
            }

            incomeSources.Add(income);
        }

        var additionalExpenses = new List<RetirementExpense>();
        foreach (var editor in RetirementAdditionalExpenses)
        {
            if (!editor.TryCreateExpense(out var expense))
            {
                ValidationMessage = "Complete every additional expense with a valid annual amount and start age.";
                return false;
            }

            additionalExpenses.Add(expense);
        }

        draft = new DeferredCompensationDraft(
            currentAge,
            semiAge,
            planThroughAge,
            annualExpenses,
            inflationRate,
            accounts,
            incomeSources,
            additionalExpenses,
            WithdrawOnlyAfterRetirement,
            ReinvestRetirementSurplus);
        return true;
    }

    private bool TryCreateCoastDraft(out CoastFireDraft draft)
    {
        draft = calculatorDefaultsService.CoastFire;
        if (!TryParseWholeNumber(CurrentAgeText, out var currentAge) || currentAge is < 18 or > 100)
        {
            ValidationMessage = "Enter a current age from 18 to 100.";
            return false;
        }

        if (!TryParseWholeNumber(RetirementAgeText, out var retirementAge) || retirementAge < currentAge || retirementAge > 100)
        {
            ValidationMessage = "Enter a retirement age from your current age through 100.";
            return false;
        }

        if (!TryParseNonNegative(CurrentSavingsText, out var currentSavings)
            || !TryParseNonNegative(AnnualContributionText, out var annualContribution)
            || !TryParseNonNegative(AnnualExpensesText, out var annualExpenses))
        {
            ValidationMessage = "Enter zero or a positive amount for each dollar value.";
            return false;
        }

        if (!TryParsePercentage(ExpectedReturnText, 0, 15, out var expectedReturn)
            || !TryParsePercentage(InflationRateText, 0, 10, out var inflationRate)
            || !TryParsePercentage(WithdrawalRateText, 2, 6, out var withdrawalRate))
        {
            ValidationMessage = "Expected return must be 0% to 15%, inflation 0% to 10%, and withdrawal rate 2% to 6%.";
            return false;
        }

        draft = new CoastFireDraft(
            currentAge,
            retirementAge,
            currentSavings,
            annualContribution,
            expectedReturn,
            inflationRate,
            withdrawalRate,
            annualExpenses);
        return true;
    }

    private bool TryCreateLeanDraft(out LeanFireDraft draft)
    {
        draft = calculatorDefaultsService.LeanFire;
        if (!TryCreateDraft(out var standardDraft))
        {
            return false;
        }

        draft = new LeanFireDraft(
            standardDraft.CurrentAge,
            standardDraft.RetirementAge,
            standardDraft.CurrentSavings,
            standardDraft.AnnualContribution,
            standardDraft.AnnualIncome,
            standardDraft.ExpectedReturn,
            standardDraft.InflationRate,
            standardDraft.WithdrawalRate,
            standardDraft.AnnualExpenses);
        return true;
    }

    private bool TryCreateFatDraft(out FatFireDraft draft)
    {
        draft = calculatorDefaultsService.FatFire;
        if (!TryCreateDraft(out var standardDraft))
        {
            return false;
        }

        draft = new FatFireDraft(
            standardDraft.CurrentAge,
            standardDraft.RetirementAge,
            standardDraft.CurrentSavings,
            standardDraft.AnnualContribution,
            standardDraft.AnnualIncome,
            standardDraft.ExpectedReturn,
            standardDraft.InflationRate,
            standardDraft.WithdrawalRate,
            standardDraft.AnnualExpenses);
        return true;
    }

    private bool TryCreateBaristaDraft(out BaristaFireDraft draft)
    {
        draft = calculatorDefaultsService.BaristaFire;
        if (!TryParseWholeNumber(CurrentAgeText, out var currentAge) || currentAge is < 18 or > 100)
        {
            ValidationMessage = "Enter a current age from 18 to 100.";
            return false;
        }

        if (!TryParseNonNegative(CurrentSavingsText, out var currentSavings)
            || !TryParseNonNegative(AnnualContributionText, out var annualContribution)
            || !TryParseNonNegative(AnnualExpensesText, out var annualExpenses)
            || !TryParseNonNegative(PartTimeAnnualIncomeText, out var partTimeIncome))
        {
            ValidationMessage = "Enter zero or a positive amount for each dollar value.";
            return false;
        }

        if (!TryParsePercentage(ExpectedReturnText, 0, 15, out var expectedReturn)
            || !TryParsePercentage(InflationRateText, 0, 10, out var inflationRate)
            || !TryParsePercentage(WithdrawalRateText, 2, 6, out var withdrawalRate))
        {
            ValidationMessage = "Expected return must be 0% to 15%, inflation 0% to 10%, and withdrawal rate 2% to 6%.";
            return false;
        }

        draft = new BaristaFireDraft(currentAge, currentSavings, annualContribution, expectedReturn, inflationRate, withdrawalRate, annualExpenses, partTimeIncome);
        return true;
    }

    private bool TryCreateReverseDraft(out ReverseFireDraft draft)
    {
        draft = calculatorDefaultsService.ReverseFire;
        if (!TryParseWholeNumber(CurrentAgeText, out var currentAge) || currentAge is < 18 or > 100)
        {
            ValidationMessage = "Enter a current age from 18 to 100.";
            return false;
        }

        if (!TryParseWholeNumber(RetirementAgeText, out var targetRetirementAge)
            || targetRetirementAge <= currentAge
            || targetRetirementAge > 100)
        {
            ValidationMessage = "Enter a target FIRE age after your current age through 100.";
            return false;
        }

        if (!TryParseNonNegative(CurrentSavingsText, out var currentSavings)
            || !TryParseNonNegative(AnnualExpensesText, out var annualExpenses))
        {
            ValidationMessage = "Enter zero or a positive amount for each dollar value.";
            return false;
        }

        if (!TryParsePercentage(ExpectedReturnText, 0, 15, out var expectedReturn)
            || !TryParsePercentage(InflationRateText, 0, 10, out var inflationRate)
            || !TryParsePercentage(WithdrawalRateText, 2, 6, out var withdrawalRate))
        {
            ValidationMessage = "Expected return must be 0% to 15%, inflation 0% to 10%, and withdrawal rate 2% to 6%.";
            return false;
        }

        draft = new ReverseFireDraft(currentAge, targetRetirementAge, currentSavings, expectedReturn, inflationRate, withdrawalRate, annualExpenses);
        return true;
    }

    private bool TryCreateWithdrawalRateDraft(out WithdrawalRateDraft draft)
    {
        draft = calculatorDefaultsService.WithdrawalRate;
        if (!TryParseNonNegative(PortfolioValueText, out var portfolioValue) || portfolioValue <= 0)
        {
            ValidationMessage = "Enter a portfolio value greater than zero.";
            return false;
        }

        if (!TryParseWholeNumber(RetirementYearsText, out var retirementYears) || retirementYears is < 10 or > 60)
        {
            ValidationMessage = "Enter a retirement duration from 10 to 60 years.";
            return false;
        }

        if (!TryParsePercentage(WithdrawalRateText, 2, 8, out var withdrawalRate)
            || !TryParsePercentage(ExpectedReturnText, 0, 15, out var expectedReturn)
            || !TryParsePercentage(InflationRateText, 0, 10, out var inflationRate))
        {
            ValidationMessage = "Withdrawal rate must be 2% to 8%, return 0% to 15%, and inflation 0% to 10%.";
            return false;
        }

        draft = new WithdrawalRateDraft(portfolioValue, withdrawalRate, expectedReturn, inflationRate, retirementYears);
        return true;
    }

    private bool TryCreateSavingsInvestmentDraft(out SavingsInvestmentDraft draft)
    {
        draft = calculatorDefaultsService.SavingsInvestment;
        if (!TryParseWholeNumber(CurrentAgeText, out var currentAge) || currentAge is < 18 or > 100)
        {
            ValidationMessage = "Enter a current age from 18 to 100.";
            return false;
        }

        if (!TryParseWholeNumber(YearsInvestingText, out var yearsInvesting) || yearsInvesting is < 1 or > 60)
        {
            ValidationMessage = "Enter an investment timeline from 1 to 60 years.";
            return false;
        }

        if (!TryParseNonNegative(StartingAmountText, out var startingAmount)
            || !TryParseNonNegative(InvestmentContributionText, out var contributionAmount)
            || !TryParseNonNegative(InvestmentAnnualIncomeText, out var annualIncome))
        {
            ValidationMessage = "Enter zero or a positive amount for each dollar value.";
            return false;
        }

        if (!TryParsePercentage(ExpectedReturnText, 0, 15, out var expectedReturn)
            || !TryParsePercentage(InflationRateText, 0, 10, out var inflationRate))
        {
            ValidationMessage = "Expected return must be 0% to 15% and inflation 0% to 10%.";
            return false;
        }

        draft = new SavingsInvestmentDraft(startingAmount, contributionAmount, ContributionFrequency, yearsInvesting, expectedReturn, inflationRate, annualIncome, currentAge);
        return true;
    }

    private bool TryCreateHealthcareGapDraft(out HealthcareGapDraft draft)
    {
        draft = calculatorDefaultsService.HealthcareGap;
        if (!TryParseWholeNumber(HealthcareCurrentAgeText, out var currentAge) || currentAge is < 18 or > 100)
        {
            ValidationMessage = "Enter a current age from 18 to 100.";
            return false;
        }

        if (!TryParseWholeNumber(EarlyRetirementAgeText, out var earlyRetirementAge)
            || earlyRetirementAge < currentAge
            || earlyRetirementAge > 100)
        {
            ValidationMessage = "Enter a retirement age from your current age through 100.";
            return false;
        }

        if (!TryParseWholeNumber(MedicareAgeText, out var medicareAge)
            || medicareAge < earlyRetirementAge
            || medicareAge > 100)
        {
            ValidationMessage = "Enter a Medicare age from retirement age through 100.";
            return false;
        }

        if (!TryParseNonNegative(MonthlyPremiumText, out var monthlyPremium)
            || !TryParseNonNegative(AnnualDeductibleText, out var annualDeductible)
            || !TryParseNonNegative(AnnualOutOfPocketText, out var annualOutOfPocket))
        {
            ValidationMessage = "Enter zero or a positive amount for each healthcare cost.";
            return false;
        }

        if (!TryParsePercentage(InflationRateText, 0, 10, out var inflationRate))
        {
            ValidationMessage = "Enter an inflation rate from 0% to 10%.";
            return false;
        }

        draft = new HealthcareGapDraft(currentAge, earlyRetirementAge, medicareAge, monthlyPremium, annualDeductible, annualOutOfPocket, inflationRate);
        return true;
    }

    private bool TryCreateDebtPayoffDraft(out DebtPayoffDraft draft)
    {
        draft = DebtPayoffDraft.Default;
        var debts = new List<DebtItem>();
        foreach (var debtItem in DebtItems)
        {
            if (!debtItem.TryCreateDebt(out var debt))
            {
                ValidationMessage = "Every debt needs a name, positive balance, interest rate, and minimum payment.";
                return false;
            }

            debts.Add(debt);
        }

        if (debts.Count == 0)
        {
            ValidationMessage = "Add at least one debt to calculate a payoff strategy.";
            return false;
        }

        if (!TryParseNonNegative(DebtMonthlyBudgetText, out var monthlyBudget)
            || !TryParseNonNegative(DebtExtraPaymentText, out var extraPayment)
            || !TryParseWholeNumber(DebtTargetMonthsText, out var targetMonths)
            || targetMonths is < 1 or > 360)
        {
            ValidationMessage = "Enter positive debt payments and a payoff timeline from 1 to 360 months.";
            return false;
        }

        draft = new DebtPayoffDraft(debts, monthlyBudget, extraPayment, targetMonths, DebtPayoffMode, DebtPayoffStrategy);
        return true;
    }

    private void OnDraftInputChanged()
    {
        if (!isApplyingDraft)
        {
            SelectedPreset = null;
        }

        RecalculateAndSave();
    }

    private void TrackLoadedPlan(PlanRecord plan)
    {
        loadedPlanId = plan.Id;
        loadedPlanCreatedAtUtc = plan.CreatedAtUtc;
        IsLoadedPlan = true;
    }

    private void UpdateProjectionChart(StandardFireResult result)
    {
        ProjectionSeries =
        [
            CreateProjectionSeries("Portfolio", result.Projections.Select(point => point.Portfolio), new SKColor(47, 107, 87)),
            CreateProjectionSeries("Today's dollars", result.Projections.Select(point => point.InflationAdjusted), new SKColor(84, 112, 104)),
            CreateProjectionSeries("FIRE target", Enumerable.Repeat(result.FireNumber, result.Projections.Count), new SKColor(201, 119, 39))
        ];
        ProjectionXAxes =
        [
            new Axis
            {
                Name = "Age",
                Labels = result.Projections.Select(point => point.Age.ToString("0", CultureInfo.CurrentCulture)).ToArray(),
                LabelsRotation = 0,
                TextSize = 10
            }
        ];
        ProjectionChartDescription = $"Portfolio projection from age {result.Projections[0].Age:0} through age {result.Projections[^1].Age:0}. "
            + $"The portfolio starts at {FormatCurrency(result.Projections[0].Portfolio)} and the FIRE target is {FormatCurrency(result.FireNumber)}.";
    }

    private void UpdateCoastProjectionChart(CoastFireResult result)
    {
        ProjectionSeries =
        [
            CreateProjectionSeries("Coast", result.Projections.Select(point => point.Portfolio), new SKColor(51, 117, 171)),
            CreateProjectionSeries("Continue contributing", result.ProjectionsWithContributions.Select(point => point.Portfolio), new SKColor(95, 86, 189)),
            CreateProjectionSeries("Full FIRE target", Enumerable.Repeat(result.FireNumber, result.Projections.Count), new SKColor(201, 119, 39))
        ];
        ProjectionXAxes =
        [
            new Axis
            {
                Name = "Age",
                Labels = result.Projections.Select(point => point.Age.ToString("0", CultureInfo.CurrentCulture)).ToArray(),
                LabelsRotation = 0,
                TextSize = 10
            }
        ];
        ProjectionChartDescription = $"Coast FIRE comparison from age {result.Projections[0].Age:0} through age {result.Projections[^1].Age:0}. "
            + $"The Coast path has no further contributions, while the comparison path continues contributions. The full FIRE target is {FormatCurrency(result.FireNumber)}.";
    }

    private void UpdateBaristaProjectionChart(BaristaFireResult result)
    {
        ProjectionSeries =
        [
            CreateProjectionSeries("Portfolio", result.Projections.Select(point => point.Portfolio), new SKColor(154, 92, 23)),
            CreateProjectionSeries("Today's dollars", result.Projections.Select(point => point.InflationAdjusted), new SKColor(84, 112, 104)),
            CreateProjectionSeries("Barista FIRE target", Enumerable.Repeat(result.BaristaNumber, result.Projections.Count), new SKColor(201, 119, 39)),
            CreateProjectionSeries("Full FIRE target", Enumerable.Repeat(result.FullFireNumber, result.Projections.Count), new SKColor(75, 90, 84))
        ];
        ProjectionXAxes =
        [
            new Axis
            {
                Name = "Age",
                Labels = result.Projections.Select(point => point.Age.ToString("0", CultureInfo.CurrentCulture)).ToArray(),
                LabelsRotation = 0,
                TextSize = 10
            }
        ];
        ProjectionChartDescription = $"Barista FIRE portfolio projection from age {result.Projections[0].Age:0} through age {result.Projections[^1].Age:0}. "
            + $"The Barista FIRE target is {FormatCurrency(result.BaristaNumber)} and the full FIRE target is {FormatCurrency(result.FullFireNumber)}.";
    }

    private void UpdateReverseProjectionChart(ReverseFireResult result)
    {
        ProjectionSeries =
        [
            CreateProjectionSeries("Portfolio", result.Projections.Select(point => point.Portfolio), new SKColor(29, 119, 130)),
            CreateProjectionSeries("Today's dollars", result.Projections.Select(point => point.InflationAdjusted), new SKColor(84, 112, 104)),
            CreateProjectionSeries("FIRE target", Enumerable.Repeat(result.FireNumber, result.Projections.Count), new SKColor(201, 119, 39))
        ];
        ProjectionXAxes =
        [
            new Axis
            {
                Name = "Age",
                Labels = result.Projections.Select(point => point.Age.ToString("0", CultureInfo.CurrentCulture)).ToArray(),
                LabelsRotation = 0,
                TextSize = 10
            }
        ];
        ProjectionChartDescription = $"Reverse FIRE projection from age {result.Projections[0].Age:0} through age {result.Projections[^1].Age:0}. "
            + $"The required-saving path targets {FormatCurrency(result.FireNumber)}.";
    }

    private void UpdateWithdrawalProjectionChart(WithdrawalResult result)
    {
        ProjectionSeries =
        [
            CreateProjectionSeries("Portfolio balance", result.WithdrawalProjections.Select(point => point.Balance), new SKColor(42, 121, 160)),
            CreateProjectionSeries("Annual withdrawal", result.WithdrawalProjections.Select(point => point.Withdrawal), new SKColor(201, 119, 39))
        ];
        ProjectionXAxes =
        [
            new Axis
            {
                Name = "Retirement year",
                Labels = result.WithdrawalProjections.Select(point => point.Year.ToString("0", CultureInfo.CurrentCulture)).ToArray(),
                LabelsRotation = 0,
                TextSize = 10
            }
        ];
        ProjectionChartDescription = $"Withdrawal projection through retirement year {result.WithdrawalProjections[^1].Year:0}. "
            + $"The ending portfolio balance is {FormatCurrency(result.EndingBalance)}.";
    }

    private void UpdateInvestmentProjectionChart(InvestmentGrowthResult result)
    {
        ProjectionSeries =
        [
            CreateProjectionSeries("Portfolio", result.Projections.Select(point => point.Portfolio), new SKColor(72, 93, 165)),
            CreateProjectionSeries("Today's dollars", result.Projections.Select(point => point.InflationAdjusted), new SKColor(84, 112, 104)),
            CreateProjectionSeries("Total invested", result.Projections.Select(point => point.TotalContributions), new SKColor(201, 119, 39))
        ];
        ProjectionXAxes =
        [
            new Axis
            {
                Name = "Age",
                Labels = result.Projections.Select(point => point.Age.ToString("0", CultureInfo.CurrentCulture)).ToArray(),
                LabelsRotation = 0,
                TextSize = 10
            }
        ];
        ProjectionChartDescription = $"Investment growth projection from age {result.Projections[0].Age:0} through age {result.Projections[^1].Age:0}. "
            + $"The projected final portfolio is {FormatCurrency(result.FinalNominalBalance)}.";
    }

    private void UpdateHealthcareProjectionChart(HealthcareGapResult result)
    {
        ProjectionSeries =
        [
            CreateProjectionSeries("Total cost", result.YearlyBreakdown.Select(year => year.Cost), new SKColor(190, 81, 66)),
            CreateProjectionSeries("Premium", result.YearlyBreakdown.Select(year => year.Premium), new SKColor(72, 93, 165)),
            CreateProjectionSeries("Deductible and out-of-pocket", result.YearlyBreakdown.Select(year => year.Deductible + year.OutOfPocket), new SKColor(201, 119, 39))
        ];
        ProjectionXAxes =
        [
            new Axis
            {
                Name = "Age",
                Labels = result.YearlyBreakdown.Select(year => year.Age.ToString("0", CultureInfo.CurrentCulture)).ToArray(),
                LabelsRotation = 0,
                TextSize = 10
            }
        ];
        ProjectionChartDescription = result.GapYears == 0
            ? "No pre-Medicare healthcare coverage gap is projected."
            : $"Pre-Medicare healthcare costs from age {result.YearlyBreakdown[0].Age:0} through age {result.YearlyBreakdown[^1].Age:0}. Total estimated cost is {FormatCurrency(result.TotalCost)}.";
    }

    private void UpdateDebtProjectionChart(DebtPayoffResult result)
    {
        ProjectionSeries =
        [
            CreateProjectionSeries("Debt balance", result.Projections.Select(month => month.TotalBalance), new SKColor(190, 81, 66))
        ];
        ProjectionXAxes =
        [
            new Axis
            {
                Name = "Month",
                Labels = result.Projections.Where((_, index) => index % Math.Max(1, result.Projections.Count / 6) == 0).Select(month => month.Month.ToString(CultureInfo.CurrentCulture)).ToArray(),
                LabelsRotation = 0,
                TextSize = 10
            }
        ];
        ProjectionChartDescription = $"Debt balance projection over {result.TotalMonths} months. Total interest is {FormatCurrency(result.TotalInterest)}.";
    }

    private void UpdateDebtBreakdownChart(DebtPayoffResult result)
    {
        DebtBreakdownSeries =
        [
            CreateStackedAreaSeries("Principal paid", result.Projections.Select(month => month.CumulativePrincipal), new SKColor(16, 185, 129)),
            CreateStackedAreaSeries("Interest paid", result.Projections.Select(month => month.CumulativeInterest), new SKColor(239, 68, 68))
        ];
        DebtBreakdownDescription = $"Cumulative principal and interest paid over {result.TotalMonths} months.";
        DebtBreakdownSummary = $"Across the payoff plan, {FormatCurrency(result.TotalPrincipal)} goes to principal and {FormatCurrency(result.TotalInterest)} goes to interest.";
    }

    private void UpdateRetirementBucketChart(
        DeferredCompensationDraft draft,
        DeferredCompensationResult result)
    {
        SKColor[] colors =
        [
            new(139, 92, 246),
            new(14, 165, 233),
            new(20, 184, 166),
            new(245, 158, 11),
            new(236, 72, 153),
            new(132, 204, 22),
            new(249, 115, 22),
            new(99, 102, 241)
        ];
        RetirementBucketSeries = draft.Accounts
            .Select((account, index) => (ISeries)CreateProjectionSeries(
                string.IsNullOrWhiteSpace(account.Name) ? $"Account {index + 1}" : account.Name,
                result.Projections.Select(point => point.Balances.GetValueOrDefault(account.Id)),
                colors[index % colors.Length]))
            .ToArray();

        var endingPoint = result.Projections[^1];
        var endingBalances = draft.Accounts.Select((account, index) =>
            $"{(string.IsNullOrWhiteSpace(account.Name) ? $"Account {index + 1}" : account.Name)} {FormatCurrency(endingPoint.Balances.GetValueOrDefault(account.Id))}");
        RetirementBucketDescription = $"Account balances from age {result.Projections[0].Age} through age {endingPoint.Age}.";
        RetirementBucketSummary = $"At age {endingPoint.Age}, projected account balances are {string.Join(", ", endingBalances)}.";
    }

    [RelayCommand]
    private async Task ViewRetirementAnnualDetailsAsync()
    {
        if (!TryCreateDeferredCompensationDraft(out var draft))
        {
            return;
        }

        var result = DeferredCompensationCalculator.Calculate(draft.ToInputs());
        var details = result.Projections
            .Select(point => CreateRetirementAnnualDetail(draft, point))
            .ToArray();
        await navigationService.GoToAsync(
            "retirement-annual-details",
            new Dictionary<string, object> { ["details"] = details });
    }

    private RetirementAnnualDetailItem CreateRetirementAnnualDetail(
        DeferredCompensationDraft draft,
        RetirementCashFlowPoint point)
    {
        var incomeParts = draft.IncomeSources
            .Select(source => (source.Name, Amount: point.IncomeBySource.GetValueOrDefault(source.Id)))
            .Where(item => item.Amount > 0)
            .Select(item => $"{item.Name}: {FormatCurrency(item.Amount)}")
            .Concat(draft.Accounts
                .Select(account => (account.Name, Amount: point.Withdrawals.GetValueOrDefault(account.Id)))
                .Where(item => item.Amount > 0)
                .Select(item => $"{item.Name} withdrawal: {FormatCurrency(item.Amount)}"));
        var expenseParts = new[] { $"Core expenses: {FormatCurrency(point.CoreExpenses)}" }
            .Concat(draft.AdditionalExpenses
                .Select(expense => (expense.Name, Amount: point.ExpensesByItem.GetValueOrDefault(expense.Id)))
                .Where(item => item.Amount > 0)
                .Select(item => $"{item.Name}: {FormatCurrency(item.Amount)}"));
        var balanceParts = draft.Accounts.Select((account, index) =>
            $"{(string.IsNullOrWhiteSpace(account.Name) ? $"Account {index + 1}" : account.Name)}: {FormatCurrency(point.Balances.GetValueOrDefault(account.Id))}");

        return new RetirementAnnualDetailItem(
            $"Age {point.Age} - {point.Year}",
            FormatCurrency(point.TotalBalance),
            FormatCurrency(point.TotalIncome),
            FormatCurrency(point.Expenses),
            FormatCurrency(point.Surplus),
            incomeParts.Any() ? string.Join(Environment.NewLine, incomeParts) : "No income or account withdrawals this year.",
            string.Join(Environment.NewLine, expenseParts),
            string.Join(Environment.NewLine, balanceParts));
    }

    private static LineSeries<double> CreateProjectionSeries(string name, IEnumerable<double> values, SKColor color)
    {
        return new LineSeries<double>
        {
            Name = name,
            Values = values.ToArray(),
            GeometrySize = 0,
            Fill = null,
            Stroke = new SolidColorPaint(color) { StrokeThickness = 3 }
        };
    }

    private static StackedAreaSeries<double> CreateStackedAreaSeries(string name, IEnumerable<double> values, SKColor color)
    {
        return new StackedAreaSeries<double>
        {
            Name = name,
            Values = values.ToArray(),
            GeometrySize = 0,
            LineSmoothness = 0,
            Fill = new SolidColorPaint(color.WithAlpha(110)),
            Stroke = new SolidColorPaint(color) { StrokeThickness = 2 }
        };
    }

    private bool TryCreateDraft(out StandardFireDraft draft)
    {
        draft = calculatorDefaultsService.StandardFire;
        if (!TryParseWholeNumber(CurrentAgeText, out var currentAge) || currentAge is < 18 or > 100)
        {
            ValidationMessage = "Enter a current age from 18 to 100.";
            return false;
        }

        if (!TryParseWholeNumber(RetirementAgeText, out var retirementAge) || retirementAge < currentAge || retirementAge > 100)
        {
            ValidationMessage = "Enter a retirement age from your current age through 100.";
            return false;
        }

        if (!TryParseNonNegative(CurrentSavingsText, out var currentSavings)
            || !TryParseNonNegative(AnnualContributionText, out var annualContribution)
            || !TryParseNonNegative(AnnualIncomeText, out var annualIncome)
            || !TryParseNonNegative(AnnualExpensesText, out var annualExpenses))
        {
            ValidationMessage = "Enter zero or a positive amount for each dollar value.";
            return false;
        }

        if (!TryParsePercentage(ExpectedReturnText, 0, 20, out var expectedReturn)
            || !TryParsePercentage(InflationRateText, 0, 10, out var inflationRate)
            || !TryParsePercentage(WithdrawalRateText, 2, 6, out var withdrawalRate))
        {
            ValidationMessage = "Expected return must be 0% to 20%, inflation 0% to 10%, and withdrawal rate 2% to 6%.";
            return false;
        }

        draft = new StandardFireDraft(
            currentAge,
            retirementAge,
            currentSavings,
            annualContribution,
            annualIncome,
            expectedReturn,
            inflationRate,
            withdrawalRate,
            annualExpenses);
        return true;
    }

    private void ScheduleSave(StandardFireDraft draft)
    {
        ScheduleSave("standard-fire", StandardFireDraft.PayloadVersion, JsonSerializer.Serialize(draft));
    }

    private void ScheduleSave(CoastFireDraft draft)
    {
        ScheduleSave("coast-fire", CoastFireDraft.PayloadVersion, JsonSerializer.Serialize(draft));
    }

    private void ScheduleSave(LeanFireDraft draft)
    {
        ScheduleSave("lean-fire", LeanFireDraft.PayloadVersion, JsonSerializer.Serialize(draft));
    }

    private void ScheduleSave(FatFireDraft draft)
    {
        ScheduleSave("fat-fire", FatFireDraft.PayloadVersion, JsonSerializer.Serialize(draft));
    }

    private void ScheduleSave(BaristaFireDraft draft)
    {
        ScheduleSave("barista-fire", BaristaFireDraft.PayloadVersion, JsonSerializer.Serialize(draft));
    }

    private void ScheduleSave(ReverseFireDraft draft)
    {
        ScheduleSave("reverse-fire", ReverseFireDraft.PayloadVersion, JsonSerializer.Serialize(draft));
    }

    private void ScheduleSave(WithdrawalRateDraft draft)
    {
        ScheduleSave("withdrawal-rate", WithdrawalRateDraft.PayloadVersion, JsonSerializer.Serialize(draft));
    }

    private void ScheduleSave(SavingsInvestmentDraft draft)
    {
        ScheduleSave("savings-rate", SavingsInvestmentDraft.PayloadVersion, JsonSerializer.Serialize(draft));
    }

    private void ScheduleSave(HealthcareGapDraft draft)
    {
        ScheduleSave("healthcare-gap", HealthcareGapDraft.PayloadVersion, JsonSerializer.Serialize(draft));
    }

    private void ScheduleSave(DeferredCompensationDraft draft)
    {
        ScheduleSave("retirement-cash-flow", DeferredCompensationDraft.PayloadVersion, JsonSerializer.Serialize(draft));
    }

    private void ScheduleSave(DebtPayoffDraft draft)
    {
        ScheduleSave("debt-payoff", DebtPayoffDraft.PayloadVersion, JsonSerializer.Serialize(draft));
    }

    private void OnDebtItemsChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        if (eventArgs.OldItems is not null)
        {
            foreach (DebtEditorItem debt in eventArgs.OldItems)
            {
                debt.Changed -= OnDebtItemChanged;
            }
        }

        if (eventArgs.NewItems is not null)
        {
            foreach (DebtEditorItem debt in eventArgs.NewItems)
            {
                debt.Changed += OnDebtItemChanged;
            }
        }

        OnDraftInputChanged();
    }

    private void OnDebtItemChanged(object? sender, EventArgs eventArgs) => OnDraftInputChanged();

    private void ReplaceDebtItems(IReadOnlyList<DebtItem> debts)
    {
        DebtItems.Clear();
        foreach (var debt in debts)
        {
            DebtItems.Add(DebtEditorItem.FromDebt(debt));
        }
    }

    private void OnRetirementAccountsChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        if (eventArgs.OldItems is not null)
        {
            foreach (RetirementAccountEditorItem account in eventArgs.OldItems)
            {
                account.Changed -= OnRetirementEditorChanged;
            }
        }

        if (eventArgs.NewItems is not null)
        {
            foreach (RetirementAccountEditorItem account in eventArgs.NewItems)
            {
                account.Changed += OnRetirementEditorChanged;
            }
        }

        OnDraftInputChanged();
    }

    private void OnRetirementIncomeSourcesChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        if (eventArgs.OldItems is not null)
        {
            foreach (RetirementIncomeEditorItem income in eventArgs.OldItems)
            {
                income.Changed -= OnRetirementEditorChanged;
            }
        }

        if (eventArgs.NewItems is not null)
        {
            foreach (RetirementIncomeEditorItem income in eventArgs.NewItems)
            {
                income.Changed += OnRetirementEditorChanged;
            }
        }

        OnDraftInputChanged();
    }

    private void OnRetirementExpensesChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        if (eventArgs.OldItems is not null)
        {
            foreach (RetirementExpenseEditorItem expense in eventArgs.OldItems)
            {
                expense.Changed -= OnRetirementEditorChanged;
            }
        }

        if (eventArgs.NewItems is not null)
        {
            foreach (RetirementExpenseEditorItem expense in eventArgs.NewItems)
            {
                expense.Changed += OnRetirementEditorChanged;
            }
        }

        OnDraftInputChanged();
    }

    private void OnRetirementEditorChanged(object? sender, EventArgs eventArgs) => OnDraftInputChanged();

    private void ReplaceRetirementAccounts(IReadOnlyList<RetirementAccount> accounts)
    {
        RetirementAccounts.Clear();
        foreach (var account in accounts)
        {
            RetirementAccounts.Add(RetirementAccountEditorItem.FromAccount(account));
        }
    }

    private void ReplaceRetirementIncomeSources(IReadOnlyList<RetirementIncomeSource> incomeSources)
    {
        RetirementIncomeSources.Clear();
        foreach (var income in incomeSources)
        {
            RetirementIncomeSources.Add(RetirementIncomeEditorItem.FromIncome(income));
        }
    }

    private void ReplaceRetirementExpenses(IReadOnlyList<RetirementExpense> expenses)
    {
        RetirementAdditionalExpenses.Clear();
        foreach (var expense in expenses)
        {
            RetirementAdditionalExpenses.Add(RetirementExpenseEditorItem.FromExpense(expense));
        }
    }

    private void ScheduleSave(string calculatorId, int payloadVersion, string payloadJson)
    {
        lock (pendingDraftLock)
        {
            pendingDraft = new DraftRecord(calculatorId, payloadVersion, payloadJson, DateTime.UtcNow);
            saveCancellationTokenSource?.Cancel();
            saveCancellationTokenSource?.Dispose();
            saveCancellationTokenSource = new CancellationTokenSource();
            _ = SaveDraftAsync(pendingDraft, saveCancellationTokenSource.Token);
        }
    }

    public async Task FlushPendingDraftAsync()
    {
        DraftRecord? draft;
        lock (pendingDraftLock)
        {
            draft = pendingDraft;
            pendingDraft = null;
            saveCancellationTokenSource?.Cancel();
            saveCancellationTokenSource?.Dispose();
            saveCancellationTokenSource = null;
        }

        if (draft is not null)
        {
            await SaveDraftRecordAsync(draft, CancellationToken.None);
        }
    }

    private async Task SaveDraftAsync(DraftRecord draft, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(400), cancellationToken);
            await SaveDraftRecordAsync(draft, cancellationToken);
            lock (pendingDraftLock)
            {
                if (pendingDraft == draft)
                {
                    pendingDraft = null;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
            ValidationMessage = "Your changes are shown here, but could not be saved locally yet.";
        }
    }

    private async Task SaveDraftRecordAsync(DraftRecord draft, CancellationToken cancellationToken)
    {
        await draftSaveLock.WaitAsync(cancellationToken);
        try
        {
            await draftRepository.SaveAsync(draft with { UpdatedAtUtc = DateTime.UtcNow }, cancellationToken);
        }
        finally
        {
            draftSaveLock.Release();
        }
    }

    private static bool TryParseWholeNumber(string text, out int value)
    {
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out value);
    }

    private static bool TryParseNonNegative(string text, out double value)
    {
        return double.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value) && value >= 0;
    }

    private static bool TryParsePercentage(string text, double minimumPercent, double maximumPercent, out double value)
    {
        value = 0;
        if (!double.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var percent)
            || percent < minimumPercent
            || percent > maximumPercent)
        {
            return false;
        }

        value = percent / 100;
        return true;
    }

    private static string FormatNumber(double value)
    {
        return value.ToString("0.##", CultureInfo.CurrentCulture);
    }

    private string FormatCurrency(double value)
    {
        return currencyPreferencesService.Format(value);
    }
}