using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using MyFireNumber.Core.Calculators;
using MyFireNumber.Services;
using MyFireNumber.Storage;
using SkiaSharp;
using System.Globalization;
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
    private readonly IDraftRepository draftRepository;
    private readonly INavigationService navigation;
    private readonly IPlanRepository planRepository;
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
        draftRepository = services.DraftRepository;
        navigation = services.Navigation;
        planRepository = services.PlanRepository;
    }

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
    private IReadOnlyList<ISeries> projectionSeries = [];

    [ObservableProperty]
    private Axis[] projectionXAxes = [];

    [ObservableProperty]
    private string projectionChartDescription = string.Empty;

    /// <summary>Suppresses recalculation while inputs are being populated from a draft.</summary>
    protected bool IsApplyingDraft { get; private set; }

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

    /// <summary>Serialization version guarding stored draft and plan payloads.</summary>
    protected abstract int DraftPayloadVersion { get; }

    /// <summary>Draft used on first run and when the user resets.</summary>
    protected abstract TDraft DefaultDraft { get; }

    /// <summary>Placeholder name applied to a new, unsaved plan.</summary>
    protected abstract string DefaultPlanName { get; }

    protected abstract string ExportSuccessMessage { get; }

    protected abstract string ExportFailureMessage { get; }

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

    public async Task LoadAsync(string? planId = null, bool returnHomeAfterSave = false)
    {
        this.returnHomeAfterSave = returnHomeAfterSave;
        loadedPlanId = null;
        loadedPlanCreatedAtUtc = null;
        IsLoadedPlan = false;

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
                await LoadDraftAsync();
            }
        }
        catch (JsonException)
        {
            var wasQuarantined = await QuarantineCorruptPayloadAsync(planId);
            ValidationMessage = wasQuarantined
                ? "Unreadable saved data was moved to local recovery storage. Default values are shown."
                : "Saved data could not be read or moved to recovery storage. Default values are shown.";
            LoadInputs(DefaultDraft);
        }
        catch (Exception)
        {
            ValidationMessage = "Your saved draft could not be restored. You can continue with the values shown.";
            LoadInputs(DefaultDraft);
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
            LoadInputs(DefaultDraft);
            return;
        }

        if (savedPlan.PayloadVersion != DraftPayloadVersion)
        {
            ValidationMessage = "This saved plan uses an unsupported format. Default values are shown.";
            LoadInputs(DefaultDraft);
            return;
        }

        PlanNameText = savedPlan.Name;
        LoadInputs(JsonSerializer.Deserialize<TDraft>(savedPlan.PayloadJson) ?? DefaultDraft);
        loadedPlanId = savedPlan.Id;
        loadedPlanCreatedAtUtc = savedPlan.CreatedAtUtc;
        IsLoadedPlan = true;
    }

    private async Task LoadDraftAsync()
    {
        var savedDraft = behaviorPreferences.Current.RestoreDrafts
            ? await draftRepository.GetAsync(CalculatorId)
            : null;

        if (savedDraft is null)
        {
            LoadInputs(DefaultDraft);
            return;
        }

        if (savedDraft.PayloadVersion != DraftPayloadVersion)
        {
            ValidationMessage = "This saved draft uses an unsupported format. Default values are shown.";
            LoadInputs(DefaultDraft);
            return;
        }

        LoadInputs(JsonSerializer.Deserialize<TDraft>(savedDraft.PayloadJson) ?? DefaultDraft);
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
        RecalculateAndSave();
    }

    private void RecalculateAndSave()
    {
        if (IsApplyingDraft)
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
        LoadInputs(DefaultDraft);
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
                now));
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
                DateTime.UtcNow);
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
