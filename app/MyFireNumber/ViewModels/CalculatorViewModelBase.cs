using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using MyFireNumber.Core.Calculators;
using MyFireNumber.Core.Presentation;
using MyFireNumber.Services;
using MyFireNumber.Storage;
using MyFireNumber.Core.Profile;
using SkiaSharp;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace MyFireNumber.ViewModels;

/// <summary>
/// Shared plumbing for a single calculator: draft restore, debounced local draft
/// persistence, named plan save/update, workbook export, and projection charting.
/// Derived view models supply only the inputs, validation, and results for their
/// own calculator.
/// </summary>
public abstract partial class CalculatorViewModelBase<TDraft> : ObservableObject, ICalculatorViewModel
    where TDraft : class
{
    private readonly IAppBehaviorPreferencesService behaviorPreferences;
    private readonly ICalculatorCatalog catalog;
    private readonly ICorruptPayloadRepository corruptPayloadRepository;
    private readonly ICurrencyPreferencesService currencyPreferences;
    private readonly IDisplayPeriodPreferencesService displayPeriodPreferences;
    private readonly Dictionary<string, PeriodicAmountField> periodicFields = new(StringComparer.Ordinal);
    private readonly IDraftRepository draftRepository;
    private readonly INavigationService navigation;
    private readonly IPlanRepository planRepository;
    private readonly IProfileScenarioResolver profileScenarioResolver;
    private readonly IConfirmationService confirmationService;
    private readonly SemaphoreSlim draftSaveLock = new(1, 1);
    private readonly object pendingDraftLock = new();
    private CancellationTokenSource? saveCancellationTokenSource;
    private DraftRecord? pendingDraft;
    private DateTime? loadedPlanCreatedAtUtc;
    private string? loadedPlanId;
    private bool returnHomeAfterSave;

    protected CalculatorViewModelBase(CalculatorViewModelServices services)
    {
        behaviorPreferences = services.BehaviorPreferences;
        catalog = services.Catalog;
        CalculatorDefaults = services.CalculatorDefaults;
        corruptPayloadRepository = services.CorruptPayloadRepository;
        currencyPreferences = services.CurrencyPreferences;
        displayPeriodPreferences = services.DisplayPeriodPreferences;
        draftRepository = services.DraftRepository;
        navigation = services.Navigation;
        planRepository = services.PlanRepository;
        profileScenarioResolver = services.ProfileScenarioResolver;
        confirmationService = services.ConfirmationService;
    }

    protected IConfirmationService ConfirmationService => confirmationService;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string summary = string.Empty;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SavePlanActionText))]
    [NotifyPropertyChangedFor(nameof(SavePlanActionDescription))]
    private bool isLoadedPlan;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValidationMessage))]
    private string validationMessage = string.Empty;

    [ObservableProperty]
    private string planNameText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPlanStatusMessage))]
    private string planStatusMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasExportStatusMessage))]
    private string exportStatusMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLinkedProfile))]
    [NotifyPropertyChangedFor(nameof(CanEditProfileOwnedFields))]
    [NotifyPropertyChangedFor(nameof(ScenarioDataModeText))]
    private ScenarioDataMode scenarioDataMode;

    public bool IsLinkedProfile => ScenarioDataMode == ScenarioDataMode.LinkedProfile;
    public bool CanEditProfileOwnedFields => !IsLinkedProfile;

    public string ScenarioDataModeText => IsLinkedProfile
        ? "Linked to Profile. Profile-owned fields are read-only here and update when Profile changes; calculator-specific assumptions remain editable."
        : "Standalone snapshot — values are independent from Profile.";

    private long? resolvedProfileRevision;
    private bool linkedResolutionValid = true;

    [ObservableProperty]
    private IReadOnlyList<ISeries> projectionSeries = [];

    [ObservableProperty]
    private Axis[] projectionXAxes = [];

    [ObservableProperty]
    private string projectionChartDescription = string.Empty;

    /// <summary>Suppresses recalculation while inputs are being populated from a draft.</summary>
    protected bool IsApplyingDraft { get; private set; }

    #region Display period

    // Presentation only. Nothing below reaches a draft, a saved plan, an exported workbook, or a
    // FinancialCalculator call — recurring amounts stay canonical and every calculation keeps running
    // on the canonical value. This is deliberately not ContributionFrequency, which is an input
    // frequency that does change the math and does belong in a draft.

    private CurrencyPeriod? displayPeriod;

    /// <summary>The period recurring amounts are currently shown in.</summary>
    public CurrencyPeriod DisplayPeriod
    {
        get
        {
            EnsurePeriodicFields();
            return displayPeriod!.Value;
        }
    }

    public bool IsMonthlyDisplay => DisplayPeriod == CurrencyPeriod.Monthly;

    public bool IsAnnualDisplay => DisplayPeriod == CurrencyPeriod.Annual;

    /// <summary>Appended to a periodic field's label, e.g. <c>per month</c>.</summary>
    public string DisplayPeriodQualifier => CurrencyPeriodMath.Qualifier(DisplayPeriod);

    /// <summary>Shown inside a periodic field's entry, e.g. <c>/mo</c>.</summary>
    public string DisplayPeriodSuffix => CurrencyPeriodMath.Suffix(DisplayPeriod);

    /// <summary>Whether this calculator has any recurring amounts to toggle.</summary>
    public bool HasPeriodicFields
    {
        get
        {
            EnsurePeriodicFields();
            return periodicFields.Count > 0;
        }
    }

    [RelayCommand]
    private void SetDisplayPeriod(string period)
    {
        if (!Enum.TryParse(period, out CurrencyPeriod requested) || !Enum.IsDefined(requested))
        {
            return;
        }

        EnsurePeriodicFields();
        if (requested == displayPeriod)
        {
            return;
        }

        displayPeriod = requested;
        foreach (var field in periodicFields.Values)
        {
            field.SetDisplayPeriod(requested);
        }

        displayPeriodPreferences.Save(CalculatorId, requested);

        // Raising "everything changed" rather than naming each bound text property. A hand-kept list
        // would silently stop refreshing a field the day one was added, which is precisely the class
        // of miss this feature is meant to avoid; the derived view models declare their periodic
        // fields in one place already (PeriodicFieldCatalog) and nowhere else needs to repeat it.
        OnPropertyChanged(string.Empty);
    }

    private void EnsurePeriodicFields()
    {
        if (displayPeriod.HasValue)
        {
            return;
        }

        var period = displayPeriodPreferences.Get(CalculatorId);
        displayPeriod = period;
        foreach (var definition in PeriodicFieldCatalog.For(CalculatorId))
        {
            periodicFields[definition.Key] = new PeriodicAmountField(definition.StoredPeriod, period);
        }
    }

    /// <summary>
    /// The field behind a periodic input. Throws for a key this calculator did not declare, so a typo
    /// fails at first use instead of quietly creating a field nothing toggles.
    /// </summary>
    private PeriodicAmountField PeriodicField(string key)
    {
        EnsurePeriodicFields();
        return periodicFields.TryGetValue(key, out var field)
            ? field
            : throw new KeyNotFoundException(
                $"'{key}' is not a periodic field of '{CalculatorId}'. Declare it in {nameof(PeriodicFieldCatalog)}.");
    }

    /// <summary>What a periodic entry should display, in the current display period.</summary>
    protected string PeriodicText(string key) => PeriodicField(key).Text;

    /// <summary>Accept text from a periodic entry, typed by the user or echoed by the two-way binding.</summary>
    protected void SetPeriodicText(string key, string? value, [CallerMemberName] string? propertyName = null)
    {
        var field = PeriodicField(key);
        if (string.Equals(field.Text, value, StringComparison.Ordinal))
        {
            return;
        }

        field.TrySetText(value);
        OnPropertyChanged(propertyName);
        OnDraftInputChanged();
    }

    /// <summary>Load a canonical amount into a periodic field when a draft or plan is applied.</summary>
    protected void LoadPeriodicValue(string key, double value, [CallerMemberName] string? propertyName = null)
    {
        PeriodicField(key).SetStoredValue(value);
        OnPropertyChanged(propertyName);
    }

    /// <summary>
    /// Read a periodic field's canonical amount for a draft. Always the stored period, never what is
    /// on screen, so switching the display cannot change a result.
    /// </summary>
    protected bool TryGetPeriodicValue(string key, out double value)
    {
        var field = PeriodicField(key);
        value = field.StoredValue;
        return field.HasValidText;
    }

    #endregion

    protected ICalculatorDefaultsService CalculatorDefaults { get; }

    protected INavigationService Navigation => navigation;

    public TimeSpan ChartAnimationsSpeed => behaviorPreferences.Current.ReduceMotion
        ? TimeSpan.Zero
        : TimeSpan.FromMilliseconds(800);

    public bool HasValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);

    public bool HasPlanStatusMessage => !string.IsNullOrWhiteSpace(PlanStatusMessage);

    public bool HasExportStatusMessage => !string.IsNullOrWhiteSpace(ExportStatusMessage);

    public string SavePlanActionText => IsLoadedPlan ? "Update Plan" : "Save to Plans";

    public string SavePlanActionDescription => IsLoadedPlan
        ? "Update this saved plan with the current values."
        : "Save the current values as a named plan.";

    /// <summary>Catalog identifier, e.g. <c>withdrawal-rate</c>.</summary>
    protected abstract string CalculatorId { get; }

    /// <summary>
    /// Explicit implementation so <see cref="CalculatorId"/> stays <c>protected</c> for derived view
    /// models while the page base can still read it.
    /// </summary>
    string ICalculatorViewModel.CalculatorId => CalculatorId;

    /// <summary>Serialization version guarding stored draft and plan payloads.</summary>
    protected abstract int DraftPayloadVersion { get; }

    /// <summary>Draft used on first run and when the user resets.</summary>
    protected abstract TDraft DefaultDraft { get; }

    /// <summary>Placeholder name applied to a new, unsaved plan.</summary>
    protected abstract string DefaultPlanName { get; }

    protected abstract string ExportSuccessMessage { get; }

    protected abstract string ExportFailureMessage { get; }

    protected virtual bool SupportsLinkedProfile => true;

    /// <summary>Populates the input properties from <paramref name="draft"/>.</summary>
    protected abstract void ApplyDraft(TDraft draft);

    /// <summary>
    /// Validates the current inputs. Returns <c>false</c> and sets
    /// <see cref="ValidationMessage"/> when the inputs cannot form a draft.
    /// </summary>
    protected abstract bool TryBuildDraft(out TDraft draft);

    /// <summary>Runs the calculation and publishes results and charts.</summary>
    protected abstract void Recalculate(TDraft draft);

    /// <summary>Builds the workbook and hands it to the platform share sheet.</summary>
    protected abstract Task ShareAsync(TDraft draft);

    public async Task LoadAsync(
        string? planId = null,
        bool returnHomeAfterSave = false,
        ScenarioDataMode? requestedMode = null)
    {
        this.returnHomeAfterSave = returnHomeAfterSave;
        loadedPlanId = null;
        loadedPlanCreatedAtUtc = null;
        IsLoadedPlan = false;
        ScenarioDataMode = NormalizeDataMode(requestedMode ?? ScenarioDataMode.Standalone);

        var definition = catalog.GetRequired(CalculatorId);
        Title = definition.Title;
        Summary = definition.Summary;
        PlanNameText = DefaultPlanName;

        IsLoading = true;
        ValidationMessage = string.Empty;
        try
        {
            if (!string.IsNullOrWhiteSpace(planId))
            {
                await LoadPlanAsync(planId);
            }
            else
            {
                await LoadDraftAsync(requestedMode);
            }
        }
        catch (JsonException)
        {
            var wasQuarantined = await QuarantineCorruptPayloadAsync(planId);
            ValidationMessage = wasQuarantined
                ? "Unreadable saved data was moved to local recovery storage. Default values are shown."
                : "Saved data could not be read or moved to recovery storage. Default values are shown.";
            await LoadResolvedInputsAsync(DefaultDraft);
        }
        catch (Exception)
        {
            ValidationMessage = "Your saved draft could not be restored. You can continue with the values shown.";
            await LoadResolvedInputsAsync(DefaultDraft);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadPlanAsync(string planId)
    {
        var savedPlan = await planRepository.GetAsync(planId);
        if (savedPlan is null || savedPlan.CalculatorId != CalculatorId)
        {
            ValidationMessage = "The requested plan could not be found. Default values are shown.";
            await LoadResolvedInputsAsync(DefaultDraft);
            return;
        }

        if (savedPlan.PayloadVersion != DraftPayloadVersion)
        {
            ValidationMessage = "This saved plan uses an unsupported format. Default values are shown.";
            await LoadResolvedInputsAsync(DefaultDraft);
            return;
        }

        PlanNameText = savedPlan.Name;
        ScenarioDataMode = NormalizeDataMode(savedPlan.DataMode);
        await LoadResolvedInputsAsync(JsonSerializer.Deserialize<TDraft>(savedPlan.PayloadJson) ?? DefaultDraft);
        loadedPlanId = savedPlan.Id;
        loadedPlanCreatedAtUtc = savedPlan.CreatedAtUtc;
        IsLoadedPlan = true;
    }

    private async Task LoadDraftAsync(ScenarioDataMode? requestedMode)
    {
        var savedDraft = behaviorPreferences.Current.RestoreDrafts
            ? await draftRepository.GetAsync(CalculatorId)
            : null;

        if (savedDraft is null)
        {
            await LoadResolvedInputsAsync(DefaultDraft);
            return;
        }

        if (savedDraft.PayloadVersion != DraftPayloadVersion)
        {
            ValidationMessage = "This saved draft uses an unsupported format. Default values are shown.";
            await LoadResolvedInputsAsync(DefaultDraft);
            return;
        }

        ScenarioDataMode = NormalizeDataMode(requestedMode ?? savedDraft.DataMode);
        await LoadResolvedInputsAsync(JsonSerializer.Deserialize<TDraft>(savedDraft.PayloadJson) ?? DefaultDraft);
    }

    private ScenarioDataMode NormalizeDataMode(ScenarioDataMode dataMode) =>
        SupportsLinkedProfile ? dataMode : ScenarioDataMode.Standalone;

    private async Task LoadResolvedInputsAsync(TDraft draft)
    {
        if (!IsLinkedProfile)
        {
            resolvedProfileRevision = null;
            linkedResolutionValid = true;
            LoadInputs(draft);
            return;
        }

        var resolution = await profileScenarioResolver.ResolveAsync(draft);
        if (!resolution.IsValid)
        {
            linkedResolutionValid = false;
            resolvedProfileRevision = resolution.ProfileRevision;
            LoadInputs(resolution.Draft);
            ProjectionSeries = [];
            ValidationMessage = string.Join(Environment.NewLine, resolution.Errors);
            return;
        }

        linkedResolutionValid = true;
        resolvedProfileRevision = resolution.ProfileRevision;
        LoadInputs(resolution.Draft);
    }

    public async Task RefreshLinkedProfileAsync()
    {
        if (!IsLinkedProfile || !TryBuildDraft(out var currentDraft))
        {
            return;
        }

        // Callers include fire-and-forget lifecycle and input-change paths, so a storage or
        // serialization failure here would otherwise surface as an unobserved task exception.
        try
        {
            await LoadResolvedInputsAsync(currentDraft);
        }
        catch (Exception)
        {
            linkedResolutionValid = false;
            ValidationMessage = "Your Profile data could not be read. Try again, or unlink this scenario to keep editing it directly.";
        }
    }

    /// <summary>
    /// Applies a draft to the inputs without triggering a save per property change,
    /// then recalculates once.
    /// </summary>
    protected void LoadInputs(TDraft draft)
    {
        IsApplyingDraft = true;
        try
        {
            ApplyDraft(draft);
        }
        finally
        {
            IsApplyingDraft = false;
        }

        RecalculateAndSave();
    }

    /// <summary>Called by derived view models whenever a bound input changes.</summary>
    protected virtual void OnDraftInputChanged()
    {
        if (IsLinkedProfile && !IsApplyingDraft)
        {
            PlanStatusMessage = "Linked values come from Profile. Scenario-only values remain editable.";
            _ = RefreshLinkedProfileAsync();
            return;
        }

        RecalculateAndSave();
    }

    private void RecalculateAndSave()
    {
        if (IsApplyingDraft || IsLinkedProfile && !linkedResolutionValid)
        {
            return;
        }

        if (!TryBuildDraft(out var draft))
        {
            return;
        }

        ValidationMessage = string.Empty;
        Recalculate(draft);
        ScheduleSave(draft);
    }

    [RelayCommand]
    private void ResetDefaults()
    {
        ValidationMessage = string.Empty;
        _ = LoadResolvedInputsAsync(DefaultDraft);
    }

    [RelayCommand]
    private async Task UnlinkFromProfileAsync()
    {
        if (!IsLinkedProfile || !TryBuildDraft(out var draft))
        {
            return;
        }

        if (!await confirmationService.ConfirmAsync(
                "Unlink from Profile?",
                "This keeps the currently resolved values as an editable standalone snapshot. Future Profile changes will no longer update it.",
                "Unlink",
                "Cancel"))
        {
            return;
        }

        ScenarioDataMode = ScenarioDataMode.Standalone;
        resolvedProfileRevision = null;
        LoadInputs(draft);
        PlanStatusMessage = "This scenario is now an independent standalone snapshot.";
        await FlushPendingDraftAsync();
    }

    [RelayCommand]
    private async Task SavePlanAsync()
    {
        await SavePlanCoreAsync(PlanNameText);
    }

    [RelayCommand]
    private async Task SavePlanAndCloseAsync()
    {
        if (await SavePlanCoreAsync(PlanNameText))
        {
            await navigation.GoToAsync(returnHomeAfterSave ? "//home" : "..");
        }
    }

    private async Task<bool> SavePlanCoreAsync(string planName)
    {
        await RefreshLinkedProfileAsync();
        if (IsLinkedProfile && !linkedResolutionValid)
        {
            return false;
        }
        if (string.IsNullOrWhiteSpace(planName))
        {
            ValidationMessage = "Enter a name before saving this plan.";
            return false;
        }

        if (!TryBuildDraft(out var draft))
        {
            return false;
        }

        try
        {
            var now = DateTime.UtcNow;
            var isUpdatingLoadedPlan = loadedPlanId is not null;
            var planIdToSave = isUpdatingLoadedPlan ? loadedPlanId! : Guid.NewGuid().ToString("N");
            await planRepository.SaveAsync(new PlanRecord(
                planIdToSave,
                CalculatorId,
                planName.Trim(),
                DraftPayloadVersion,
                JsonSerializer.Serialize(draft),
                isUpdatingLoadedPlan ? loadedPlanCreatedAtUtc ?? now : now,
                now,
                ScenarioDataMode,
                resolvedProfileRevision));
            loadedPlanId = planIdToSave;
            loadedPlanCreatedAtUtc = isUpdatingLoadedPlan ? loadedPlanCreatedAtUtc ?? now : now;
            IsLoadedPlan = true;
            PlanNameText = planName.Trim();
            PlanStatusMessage = isUpdatingLoadedPlan
                ? $"Updated \"{PlanNameText}\" in Plans."
                : $"Saved \"{PlanNameText}\" to Plans.";
            behaviorPreferences.PerformHaptic();
            return true;
        }
        catch (Exception)
        {
            PlanStatusMessage = "This plan could not be saved locally yet.";
            return false;
        }
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        await RefreshLinkedProfileAsync();
        if (IsLinkedProfile && !linkedResolutionValid)
        {
            ExportStatusMessage = "Fix the linked Profile data or unlink this scenario before exporting.";
            return;
        }
        if (!TryBuildDraft(out var draft))
        {
            return;
        }

        try
        {
            await ShareAsync(draft);
            ExportStatusMessage = ExportSuccessMessage;
        }
        catch (Exception)
        {
            ExportStatusMessage = ExportFailureMessage;
        }
    }

    private async Task<bool> QuarantineCorruptPayloadAsync(string? planId)
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

            var draft = await draftRepository.GetAsync(CalculatorId);
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

    private void ScheduleSave(TDraft draft)
    {
        lock (pendingDraftLock)
        {
            pendingDraft = new DraftRecord(
                CalculatorId,
                DraftPayloadVersion,
                JsonSerializer.Serialize(draft),
                DateTime.UtcNow,
                ScenarioDataMode,
                resolvedProfileRevision);
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
            try
            {
                await SaveDraftRecordAsync(draft, CancellationToken.None);
            }
            catch (Exception)
            {
                ValidationMessage = "Your changes are shown here, but could not be saved locally yet.";
            }
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

    protected static bool TryParseWholeNumber(string text, out int value)
    {
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out value);
    }

    protected static bool TryParseNonNegative(string text, out double value)
    {
        return double.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value) && value >= 0;
    }

    protected static bool TryParsePercentage(string text, double minimumPercent, double maximumPercent, out double value)
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

    protected static string FormatNumber(double value)
    {
        return value.ToString("0.##", CultureInfo.CurrentCulture);
    }

    protected string FormatCurrency(double value)
    {
        return currencyPreferences.Format(value);
    }

    protected LineSeries<double> CreateProjectionSeries(string name, IEnumerable<double> values, SKColor color)
    {
        return new LineSeries<double>
        {
            Name = name,
            Values = values.ToArray(),
            GeometrySize = 0,
            Fill = null,
            Stroke = new SolidColorPaint(color) { StrokeThickness = 3 },
            YToolTipLabelFormatter = point => FormatCurrency(point.Coordinate.PrimaryValue)
        };
    }

    protected StackedAreaSeries<double> CreateStackedAreaSeries(string name, IEnumerable<double> values, SKColor color)
    {
        return new StackedAreaSeries<double>
        {
            Name = name,
            Values = values.ToArray(),
            GeometrySize = 0,
            LineSmoothness = 0,
            Fill = new SolidColorPaint(color.WithAlpha(110)),
            Stroke = new SolidColorPaint(color) { StrokeThickness = 2 },
            YToolTipLabelFormatter = point => FormatCurrency(point.Coordinate.PrimaryValue)
        };
    }

    protected static Axis CreateLabelledAxis(string name, IEnumerable<string> labels)
    {
        return new Axis
        {
            Name = name,
            Labels = labels.ToArray(),
            LabelsRotation = 0,
            TextSize = 10
        };
    }
}
