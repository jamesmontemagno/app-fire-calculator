using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using MyFireNumber.Core.Calculations;
using MyFireNumber.Services;
using SkiaSharp;

namespace MyFireNumber.ViewModels;

public sealed record SeppMethodOption(SeppMethod Method, string Name);

public sealed partial class SeppViewModel : CalculatorViewModelBase<SeppDraft>
{
    private readonly ISeppExportService exportService;

    public SeppViewModel(CalculatorViewModelServices services, ISeppExportService exportService)
        : base(services)
    {
        this.exportService = exportService;
        MethodOptions =
        [
            new(SeppMethod.RequiredMinimumDistribution, "Required minimum distribution (changes yearly)"),
            new(SeppMethod.FixedAmortization, "Fixed amortization"),
            new(SeppMethod.FixedAnnuitization, "Fixed annuitization")
        ];
    }

    public ObservableCollection<SeppAccount> Accounts { get; } = [];
    public IReadOnlyList<SeppMethodOption> MethodOptions { get; }

    [ObservableProperty] private SeppAccount? selectedAccount;
    [ObservableProperty] private SeppMethodOption? selectedMethod;
    [ObservableProperty] private string accountNameText = string.Empty;
    [ObservableProperty] private string accountBalanceText = string.Empty;
    [ObservableProperty] private string expectedReturnText = string.Empty;
    [ObservableProperty] private DateTime birthDate = DateTime.Today.AddYears(-50);
    [ObservableProperty] private DateTime firstPaymentDate = DateTime.Today;
    [ObservableProperty] private string interestRateText = string.Empty;
    [ObservableProperty] private string maximumInterestRateText = string.Empty;
    [ObservableProperty] private string annuityFactorText = string.Empty;

    [ObservableProperty] private string startingAgeText = string.Empty;
    [ObservableProperty] private string lifeExpectancyFactorText = string.Empty;
    [ObservableProperty] private string requiredEndDateText = string.Empty;
    [ObservableProperty] private string requiredTermText = string.Empty;
    [ObservableProperty] private string selectedAnnualPaymentText = string.Empty;
    [ObservableProperty] private string selectedMonthlyPaymentText = string.Empty;
    [ObservableProperty] private string rmdPaymentText = string.Empty;
    [ObservableProperty] private string amortizationPaymentText = string.Empty;
    [ObservableProperty] private string annuitizationPaymentText = string.Empty;
    [ObservableProperty] private string projectionSummary = string.Empty;

    protected override string CalculatorId => "sepp-72t";
    protected override int DraftPayloadVersion => SeppDraft.PayloadVersion;
    protected override SeppDraft DefaultDraft => CalculatorDefaults.Sepp;
    protected override string DefaultPlanName => "My 72(t) SEPP Plan";
    protected override string ExportSuccessMessage => "Your 72(t) / SEPP workbook is ready to share.";
    protected override string ExportFailureMessage => "The 72(t) / SEPP workbook could not be created locally.";

    partial void OnSelectedAccountChanged(SeppAccount? value)
    {
        if (value is not null)
        {
            AccountNameText = value.Name;
            AccountBalanceText = FormatNumber(value.Balance);
            ExpectedReturnText = FormatNumber(value.ExpectedReturn * 100);
        }
        OnDraftInputChanged();
    }

    partial void OnSelectedMethodChanged(SeppMethodOption? value) => OnDraftInputChanged();
    partial void OnAccountNameTextChanged(string value) => OnDraftInputChanged();
    partial void OnAccountBalanceTextChanged(string value) => OnDraftInputChanged();
    partial void OnExpectedReturnTextChanged(string value) => OnDraftInputChanged();
    partial void OnBirthDateChanged(DateTime value) => OnDraftInputChanged();
    partial void OnFirstPaymentDateChanged(DateTime value) => OnDraftInputChanged();
    partial void OnInterestRateTextChanged(string value) => OnDraftInputChanged();
    partial void OnMaximumInterestRateTextChanged(string value) => OnDraftInputChanged();
    partial void OnAnnuityFactorTextChanged(string value) => OnDraftInputChanged();

    protected override void ApplyDraft(SeppDraft draft)
    {
        Accounts.Clear();
        foreach (var account in draft.Accounts)
        {
            Accounts.Add(account);
        }

        SelectedAccount = Accounts.FirstOrDefault(account => account.Id == draft.SelectedAccountId)
            ?? Accounts.FirstOrDefault();
        BirthDate = draft.BirthDate.ToDateTime(TimeOnly.MinValue);
        FirstPaymentDate = draft.FirstPaymentDate.ToDateTime(TimeOnly.MinValue);
        InterestRateText = FormatNumber(draft.InterestRate * 100);
        MaximumInterestRateText = FormatNumber(draft.MaximumInterestRate * 100);
        AnnuityFactorText = draft.AnnuityFactor?.ToString("0.###", CultureInfo.CurrentCulture) ?? string.Empty;
        SelectedMethod = MethodOptions.First(option => option.Method == draft.Method);
    }

    protected override bool TryBuildDraft(out SeppDraft draft)
    {
        draft = DefaultDraft;
        if (SelectedAccount is null)
        {
            ValidationMessage = "Choose an eligible retirement account.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(AccountNameText))
        {
            ValidationMessage = "Enter an account name.";
            return false;
        }

        if (!double.TryParse(AccountBalanceText, NumberStyles.Number, CultureInfo.CurrentCulture, out var balance)
            || balance <= 0)
        {
            ValidationMessage = "Enter an account balance greater than zero.";
            return false;
        }

        if (!TryParsePercentage(ExpectedReturnText, -100, 100, out var expectedReturn))
        {
            ValidationMessage = "Enter an expected account return from -100% to 100%.";
            return false;
        }

        if (!TryParsePercentage(InterestRateText, 0, 20, out var interestRate)
            || !TryParsePercentage(MaximumInterestRateText, 5, 20, out var maximumInterestRate))
        {
            ValidationMessage = "Enter a chosen rate from 0% to 20% and an IRS maximum from 5% to 20%.";
            return false;
        }

        if (interestRate > maximumInterestRate)
        {
            ValidationMessage = "The chosen interest rate cannot exceed the IRS limit you entered.";
            return false;
        }

        double? annuityFactor = null;
        if (!string.IsNullOrWhiteSpace(AnnuityFactorText))
        {
            if (!double.TryParse(
                    AnnuityFactorText,
                    NumberStyles.Number,
                    CultureInfo.CurrentCulture,
                    out var parsedFactor)
                || parsedFactor <= 0)
            {
                ValidationMessage = "The actuarial annuity factor must be greater than zero.";
                return false;
            }
            annuityFactor = parsedFactor;
        }

        var method = SelectedMethod?.Method ?? SeppMethod.FixedAmortization;
        if (method == SeppMethod.FixedAnnuitization && annuityFactor is null)
        {
            ValidationMessage = "Enter an actuarial annuity factor supplied by a qualified professional for fixed annuitization.";
            return false;
        }

        var birth = DateOnly.FromDateTime(BirthDate);
        var firstPayment = DateOnly.FromDateTime(FirstPaymentDate);
        if (firstPayment >= birth.AddYears(59).AddMonths(6))
        {
            ValidationMessage = "The first payment must occur before age 59½.";
            return false;
        }
        var age = AgeOn(birth, firstPayment);
        if (age is < 18 or > 59)
        {
            ValidationMessage = "The age on the first payment date must be from 18 through 59.";
            return false;
        }

        var accounts = Accounts.ToArray();
        var selected = SelectedAccount with
        {
            Name = AccountNameText.Trim(),
            Balance = balance,
            ExpectedReturn = expectedReturn
        };
        accounts = accounts.Select(account => account.Id == selected.Id ? selected : account).ToArray();
        draft = new(
            accounts,
            selected.Id,
            birth,
            firstPayment,
            interestRate,
            maximumInterestRate,
            annuityFactor,
            method);
        return true;
    }

    protected override void Recalculate(SeppDraft draft)
    {
        var result = SeppCalculator.Calculate(draft.ToInputs());
        var selected = result.For(draft.Method);
        StartingAgeText = result.StartingAge.ToString(CultureInfo.CurrentCulture);
        LifeExpectancyFactorText = result.LifeExpectancyFactor.ToString("0.0", CultureInfo.CurrentCulture);
        RequiredEndDateText = result.RequiredEndDate.ToString("MMM d, yyyy", CultureInfo.CurrentCulture);
        RequiredTermText = $"{result.RequiredYears} annual payment years";
        SelectedAnnualPaymentText = FormatPayment(selected.AnnualPayment);
        SelectedMonthlyPaymentText = selected.MonthlyPayment is double monthly
            ? $"{FormatCurrency(monthly)} / month"
            : "Factor required";
        RmdPaymentText = FormatPayment(result.Rmd.AnnualPayment);
        AmortizationPaymentText = FormatPayment(result.Amortization.AnnualPayment);
        AnnuitizationPaymentText = FormatPayment(result.Annuitization.AnnualPayment);
        ProjectionSummary = $"Illustrative balance path for {SelectedMethod?.Name ?? "the selected method"} through the required commitment period, assuming {draft.SelectedAccount.ExpectedReturn:P1} annual growth.";

        ProjectionSeries =
        [
            CreateProjectionSeries("Starting balance", selected.Projections.Select(point => point.StartingBalance), new SKColor(72, 93, 165)),
            CreateProjectionSeries("Ending balance", selected.Projections.Select(point => point.EndingBalance), new SKColor(43, 111, 83))
        ];
        ProjectionXAxes =
        [
            CreateLabelledAxis("Year", selected.Projections.Select(point => point.CalendarYear.ToString(CultureInfo.CurrentCulture)))
        ];
        ProjectionChartDescription = $"{SelectedMethod?.Name} account balance projection from {FirstPaymentDate:yyyy} through {result.RequiredEndDate:yyyy}.";
    }

    protected override Task ShareAsync(SeppDraft draft) =>
        exportService.ShareAsync(draft, SeppCalculator.Calculate(draft.ToInputs()));

    private string FormatPayment(double? payment) =>
        payment is double value ? $"{FormatCurrency(value)} / year" : "Enter actuarial factor";

    private static int AgeOn(DateOnly birthDate, DateOnly date)
    {
        if (date < birthDate)
        {
            return -1;
        }
        var birthday = birthDate.Month == 2 && birthDate.Day == 29 && !DateTime.IsLeapYear(date.Year)
            ? new DateOnly(date.Year, 2, 28)
            : new DateOnly(date.Year, birthDate.Month, birthDate.Day);
        return date.Year - birthDate.Year - (date < birthday ? 1 : 0);
    }
}
