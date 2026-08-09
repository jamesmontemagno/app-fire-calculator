using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyFireNumber.Core.Calculators;
using MyFireNumber.Core.Calculations;
using MyFireNumber.Storage;
using System.Globalization;
using System.Text.Json;

namespace MyFireNumber.ViewModels;

public partial class CalculatorDetailViewModel : ObservableObject
{
    private readonly ICalculatorCatalog catalog;
    private readonly IDraftRepository draftRepository;
    private CancellationTokenSource? saveCancellationTokenSource;
    private bool isApplyingDraft;

    public CalculatorDetailViewModel(ICalculatorCatalog catalog, IDraftRepository draftRepository)
    {
        this.catalog = catalog;
        this.draftRepository = draftRepository;
        ApplyDraft(StandardFireDraft.Default);
    }

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string summary = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotStandardFire))]
    private bool isStandardFire;

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
    private double progressToFire;

    public bool IsNotStandardFire => !IsStandardFire;

    public bool HasValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);

    public async Task LoadAsync(string calculatorId)
    {
        var definition = catalog.GetRequired(calculatorId);
        Title = definition.Title;
        Summary = definition.Summary;
        IsStandardFire = calculatorId == "standard-fire";

        if (!IsStandardFire)
        {
            return;
        }

        IsLoading = true;
        ValidationMessage = string.Empty;
        try
        {
            var savedDraft = await draftRepository.GetAsync(calculatorId);
            if (savedDraft is null)
            {
                ApplyDraft(StandardFireDraft.Default);
            }
            else if (savedDraft.PayloadVersion == StandardFireDraft.PayloadVersion)
            {
                var draft = JsonSerializer.Deserialize<StandardFireDraft>(savedDraft.PayloadJson);
                ApplyDraft(draft ?? StandardFireDraft.Default);
            }
            else
            {
                ValidationMessage = "This saved draft uses an unsupported format. Default values are shown.";
                ApplyDraft(StandardFireDraft.Default);
            }
        }
        catch (JsonException)
        {
            ValidationMessage = "This saved draft could not be read. Default values are shown.";
            ApplyDraft(StandardFireDraft.Default);
        }
        catch (Exception)
        {
            ValidationMessage = "Your saved draft could not be restored. You can continue with the values shown.";
            ApplyDraft(StandardFireDraft.Default);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ResetDefaults()
    {
        ValidationMessage = string.Empty;
        ApplyDraft(StandardFireDraft.Default);
        RecalculateAndSave();
    }

    partial void OnCurrentAgeTextChanged(string value) => RecalculateAndSave();
    partial void OnRetirementAgeTextChanged(string value) => RecalculateAndSave();
    partial void OnCurrentSavingsTextChanged(string value) => RecalculateAndSave();
    partial void OnAnnualContributionTextChanged(string value) => RecalculateAndSave();
    partial void OnAnnualIncomeTextChanged(string value) => RecalculateAndSave();
    partial void OnAnnualExpensesTextChanged(string value) => RecalculateAndSave();
    partial void OnExpectedReturnTextChanged(string value) => RecalculateAndSave();
    partial void OnInflationRateTextChanged(string value) => RecalculateAndSave();
    partial void OnWithdrawalRateTextChanged(string value) => RecalculateAndSave();

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

    private void RecalculateAndSave()
    {
        if (isApplyingDraft || !IsStandardFire || !TryCreateDraft(out var draft))
        {
            return;
        }

        var result = FinancialCalculator.CalculateStandardFire(draft.ToFireInputs());
        ValidationMessage = string.Empty;
        FireNumberText = FormatCurrency(result.FireNumber);
        YearsToFireText = double.IsPositiveInfinity(result.YearsToFire)
            ? "Not reachable with these inputs"
            : $"{result.YearsToFire:N1} years";
        FireAgeText = double.IsPositiveInfinity(result.FireAge) ? "--" : $"Age {result.FireAge:N1}";
        SavingsRateText = $"{result.SavingsRate:P1}";
        MonthlyContributionText = FormatCurrency(result.MonthlyContribution);
        ProgressToFire = result.FireNumber <= 0 ? 0 : Math.Clamp(draft.CurrentSavings / result.FireNumber, 0, 1);
        ProgressDescription = $"{ProgressToFire:P0} of your FIRE Number is currently funded.";
        ProjectionSummary = double.IsPositiveInfinity(result.YearsToFire)
            ? "The current contribution and return assumptions do not reach the FIRE Number."
            : $"At the current assumptions, your portfolio is projected to reach {FormatCurrency(result.FireNumber)} in approximately {result.YearsToFire:N1} years.";

        ScheduleSave(draft);
    }

    private bool TryCreateDraft(out StandardFireDraft draft)
    {
        draft = StandardFireDraft.Default;
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
        saveCancellationTokenSource?.Cancel();
        saveCancellationTokenSource?.Dispose();
        saveCancellationTokenSource = new CancellationTokenSource();
        _ = SaveDraftAsync(draft, saveCancellationTokenSource.Token);
    }

    private async Task SaveDraftAsync(StandardFireDraft draft, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(400), cancellationToken);
            var payloadJson = JsonSerializer.Serialize(draft);
            await draftRepository.SaveAsync(
                new DraftRecord("standard-fire", StandardFireDraft.PayloadVersion, payloadJson, DateTime.UtcNow),
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
            ValidationMessage = "Your changes are shown here, but could not be saved locally yet.";
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

    private static string FormatCurrency(double value)
    {
        return value.ToString("C0", CultureInfo.CurrentCulture);
    }
}