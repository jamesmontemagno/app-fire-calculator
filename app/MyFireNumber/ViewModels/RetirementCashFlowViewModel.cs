using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Presentation;
using MyFireNumber.Services;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;

namespace MyFireNumber.ViewModels;

public sealed partial class RetirementCashFlowViewModel : CalculatorViewModelBase<DeferredCompensationDraft>
{
    private static readonly SKColor[] BucketColors =
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

    private readonly IDeferredCompensationExportService exportService;
    private readonly IRetirementCashFlowPromptService promptService;

    // Recurring amount. Delegates to a Core periodic field instead of holding its own text, so the
    // canonical amount is the single source of truth and the monthly/annual toggle only changes how
    // it is rendered.
    public string RetirementExpensesText
    {
        get => PeriodicText(PeriodicFieldCatalog.AnnualExpenses);
        set => SetPeriodicText(PeriodicFieldCatalog.AnnualExpenses, value);
    }

    public RetirementCashFlowViewModel(
        CalculatorViewModelServices services,
        IDeferredCompensationExportService exportService,
        IRetirementCashFlowPromptService promptService)
        : base(services)
    {
        this.exportService = exportService;
        this.promptService = promptService;
        RetirementAccounts.CollectionChanged += OnRetirementAccountsChanged;
        RetirementIncomeSources.CollectionChanged += OnRetirementIncomeSourcesChanged;
        RetirementAdditionalExpenses.CollectionChanged += OnRetirementExpensesChanged;
    }

    public ObservableCollection<RetirementAccountEditorItem> RetirementAccounts { get; } = [];

    public ObservableCollection<RetirementIncomeEditorItem> RetirementIncomeSources { get; } = [];

    public ObservableCollection<RetirementExpenseEditorItem> RetirementAdditionalExpenses { get; } = [];

    [ObservableProperty] private string retirementCurrentAgeText = "45";
    [ObservableProperty] private string retirementSemiAgeText = "55";
    [ObservableProperty] private string retirementPlanThroughAgeText = "90";
    [ObservableProperty] private string retirementInflationText = "3";
    [ObservableProperty] private string retirementCurrentBalanceText = string.Empty;
    [ObservableProperty] private string retirementBalanceAtSemiText = string.Empty;
    [ObservableProperty] private string retirementEndingBalanceText = string.Empty;
    [ObservableProperty] private string retirementFundedYearsText = string.Empty;
    [ObservableProperty] private string retirementFirstShortfallText = string.Empty;
    [ObservableProperty] private string retirementBalanceBasisText = string.Empty;
    [ObservableProperty] private string retirementEndingBasisText = string.Empty;
    [ObservableProperty] private string retirementPolicyExcessText = string.Empty;
    [ObservableProperty] private bool hasRetirementPolicyExcess;

    [ObservableProperty]
    private bool withdrawOnlyAfterRetirement = true;

    [ObservableProperty]
    private bool reinvestRetirementSurplus;

    [ObservableProperty]
    private IReadOnlyList<ISeries> retirementBucketSeries = [];

    [ObservableProperty]
    private string retirementBucketDescription = string.Empty;

    [ObservableProperty]
    private string retirementBucketSummary = string.Empty;

    protected override string CalculatorId => "retirement-cash-flow";

    protected override int DraftPayloadVersion => DeferredCompensationDraft.PayloadVersion;

    protected override DeferredCompensationDraft DefaultDraft => CalculatorDefaults.RetirementCashFlow;

    protected override string DefaultPlanName => "My Retirement Cash Flow Plan";

    protected override string ExportSuccessMessage => "Your Retirement Cash Flow workbook is ready to share.";

    protected override string ExportFailureMessage => "The Retirement Cash Flow workbook could not be created locally.";

    partial void OnRetirementCurrentAgeTextChanged(string value) => OnDraftInputChanged();
    public double RetirementPlanThroughMinimum =>
        int.TryParse(RetirementSemiAgeText, NumberStyles.Integer, CultureInfo.CurrentCulture, out var semiAge)
            ? Math.Clamp(semiAge, 18, 100)
            : 18;

    partial void OnRetirementSemiAgeTextChanged(string value)
    {
        OnPropertyChanged(nameof(RetirementPlanThroughMinimum));
        OnDraftInputChanged();
    }
    partial void OnRetirementPlanThroughAgeTextChanged(string value) => OnDraftInputChanged();
    partial void OnRetirementInflationTextChanged(string value) => OnDraftInputChanged();
    partial void OnWithdrawOnlyAfterRetirementChanged(bool value) => OnDraftInputChanged();
    partial void OnReinvestRetirementSurplusChanged(bool value) => OnDraftInputChanged();

    [RelayCommand]
    private async Task AddRetirementAccountAsync()
    {
        if (IsLinkedProfile)
        {
            return;
        }

        var type = await promptService.ChooseAccountTypeAsync();
        if (type is null)
        {
            return;
        }

        RetirementAccounts.Add(new RetirementAccountEditorItem
        {
            Name = "New retirement account",
            AvailableAgeText = RetirementSemiAgeText,
            Type = type.Value,
            IsExpanded = true
        });
    }

    [RelayCommand]
    private void RemoveRetirementAccount(RetirementAccountEditorItem? account)
    {
        if (!IsLinkedProfile && account is not null)
        {
            RetirementAccounts.Remove(account);
        }
    }

    [RelayCommand]
    private async Task AddRetirementIncomeAsync()
    {
        if (IsLinkedProfile)
        {
            return;
        }

        var isAfterTax = await promptService.ChooseIncomeTaxTreatmentAsync();
        if (isAfterTax is null)
        {
            return;
        }

        RetirementIncomeSources.Add(new RetirementIncomeEditorItem
        {
            Name = "New retirement income",
            StartAgeText = RetirementSemiAgeText,
            EndAgeText = RetirementPlanThroughAgeText,
            IsAfterTax = isAfterTax.Value,
            IsExpanded = true
        });
    }

    [RelayCommand]
    private void RemoveRetirementIncome(RetirementIncomeEditorItem? income)
    {
        if (!IsLinkedProfile && income is not null)
        {
            RetirementIncomeSources.Remove(income);
        }
    }

    [RelayCommand]
    private void AddRetirementExpense()
    {
        if (IsLinkedProfile)
        {
            return;
        }

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
        if (!IsLinkedProfile && expense is not null)
        {
            RetirementAdditionalExpenses.Remove(expense);
        }
    }

    [RelayCommand]
    private async Task ViewRetirementAnnualDetailsAsync()
    {
        if (!TryBuildDraft(out var draft))
        {
            return;
        }

        var result = DeferredCompensationCalculator.Calculate(draft.ToInputs());
        var details = result.Projections
            .Select(point => CreateRetirementAnnualDetail(draft, point))
            .ToArray();
        await Navigation.GoToAsync(
            "retirement-annual-details",
            new Dictionary<string, object> { ["details"] = details });
    }

    protected override void ApplyDraft(DeferredCompensationDraft draft)
    {
        RetirementCurrentAgeText = draft.CurrentAge.ToString(CultureInfo.CurrentCulture);
        RetirementSemiAgeText = draft.SemiRetirementAge.ToString(CultureInfo.CurrentCulture);
        RetirementPlanThroughAgeText = draft.PlanThroughAge.ToString(CultureInfo.CurrentCulture);
        LoadPeriodicValue(PeriodicFieldCatalog.AnnualExpenses, draft.AnnualExpenses, nameof(RetirementExpensesText));
        RetirementInflationText = (draft.InflationRate * 100).ToString("0.##", CultureInfo.CurrentCulture);
        WithdrawOnlyAfterRetirement = draft.WithdrawOnlyAfterRetirement;
        ReinvestRetirementSurplus = draft.ReinvestSurplus;
        ReplaceRetirementAccounts(draft.Accounts);
        ReplaceRetirementIncomeSources(draft.IncomeSources);
        ReplaceRetirementExpenses(draft.AdditionalExpenses);
    }

    protected override bool TryBuildDraft(out DeferredCompensationDraft draft)
    {
        draft = DefaultDraft;
        if (!TryParseWholeNumber(RetirementCurrentAgeText, out var currentAge) || currentAge is < 18 or > 100)
        {
            ValidationMessage = "Scenario: Current age must be a whole number from 18 to 100.";
            return false;
        }

        if (!TryParseWholeNumber(RetirementSemiAgeText, out var semiAge) || semiAge < currentAge || semiAge > 100)
        {
            ValidationMessage = $"Scenario: Semi-retirement age must be a whole number from {currentAge} to 100.";
            return false;
        }

        if (!TryParseWholeNumber(RetirementPlanThroughAgeText, out var planThroughAge) || planThroughAge < semiAge || planThroughAge > 100)
        {
            ValidationMessage = $"Scenario: Plan-through age must be a whole number from {semiAge} to 100.";
            return false;
        }

        if (!TryGetPeriodicValue(PeriodicFieldCatalog.AnnualExpenses, out var annualExpenses))
        {
            ValidationMessage = "Scenario: Annual retirement spending in today's dollars must be a number of zero or more.";
            return false;
        }

        if (!TryParsePercentage(RetirementInflationText, 0, 10, out var inflationRate))
        {
            ValidationMessage = "Scenario: Inflation rate must be between 0% and 10%.";
            return false;
        }

        var accounts = new List<RetirementAccount>();
        for (var index = 0; index < RetirementAccounts.Count; index++)
        {
            var editor = RetirementAccounts[index];
            if (!editor.TryCreateAccount(out var account, out var validationError))
            {
                editor.IsExpanded = true;
                ValidationMessage = $"{DescribeItem("Account", index, editor.Name)}: {validationError}";
                return false;
            }

            accounts.Add(account);
        }

        var incomeSources = new List<RetirementIncomeSource>();
        for (var index = 0; index < RetirementIncomeSources.Count; index++)
        {
            var editor = RetirementIncomeSources[index];
            if (!editor.TryCreateIncome(out var income, out var validationError))
            {
                editor.IsExpanded = true;
                ValidationMessage = $"{DescribeItem("Income source", index, editor.Name)}: {validationError}";
                return false;
            }

            incomeSources.Add(income);
        }

        var additionalExpenses = new List<RetirementExpense>();
        for (var index = 0; index < RetirementAdditionalExpenses.Count; index++)
        {
            var editor = RetirementAdditionalExpenses[index];
            if (!editor.TryCreateExpense(out var expense, out var validationError))
            {
                editor.IsExpanded = true;
                ValidationMessage = $"{DescribeItem("Additional expense", index, editor.Name)}: {validationError}";
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

    private static string DescribeItem(string itemType, int index, string name)
    {
        var trimmedName = name.Trim();
        return string.IsNullOrWhiteSpace(trimmedName)
            ? $"{itemType} {index + 1}"
            : $"{itemType} \"{trimmedName}\"";
    }

    protected override void Recalculate(DeferredCompensationDraft draft)
    {
        var result = DeferredCompensationCalculator.Calculate(draft.ToInputs());
        ValidationMessage = string.Empty;
        RetirementCurrentBalanceText = FormatCurrency(result.CurrentBalance);
        RetirementBalanceAtSemiText = FormatCurrency(result.BalanceAtSemiRetirement);
        RetirementEndingBalanceText = FormatCurrency(result.EndingBalance);
        RetirementFundedYearsText = result.FirstShortfallAge is null
            ? $"All {result.RetirementYears} retirement years funded"
            : $"{result.FundedYears} consecutive years from age {draft.SemiRetirementAge} ({result.YearsFullyCovered} of {result.RetirementYears} covered in total)";
        RetirementFirstShortfallText = result.FirstShortfallAge is int shortfallAge
            ? $"Age {shortfallAge}"
            : "None projected";
        RetirementBalanceBasisText = $"Future dollars at age {draft.SemiRetirementAge}";
        RetirementEndingBasisText = $"Future dollars at age {draft.PlanThroughAge}";

        // Reports the years the plan had to spend past the stated withdrawal policy to stay funded,
        // which is the opposite of what this disclosure said before issue #56: the rate is no longer
        // a hard limit, so it can never be "what's binding" on a shortfall.
        //
        // No longer branches on the rounded Surplus. Policy excess is decided in the engine from the
        // unrounded gap, and a policy-exceeding year is usually a *funded* year — that is the whole
        // point of the fix — so pairing it with a shortfall test would have hidden every case it
        // exists to report. That also removes the last rounded-field branch outside the engine, which
        // is the rule issue #63 established.
        var policyExcessPoints = result.Projections
            .Where(point => point.Age >= draft.SemiRetirementAge && point.PolicyExcessWithdrawals > 0)
            .ToArray();
        var firstPolicyExcessPoint = policyExcessPoints.FirstOrDefault();
        HasRetirementPolicyExcess = firstPolicyExcessPoint is not null;
        RetirementPolicyExcessText = firstPolicyExcessPoint is null
            ? string.Empty
            : $"To stay funded, this plan withdraws more than your withdrawal-rate limits allow in {policyExcessPoints.Length} of {result.RetirementYears} retirement years, starting at age {firstPolicyExcessPoint.Age} — {FormatCurrency(firstPolicyExcessPoint.PolicyExcessWithdrawals)} above your limits that year. The withdrawal rate is a spending policy you set, not the amount of money available, so the plan spends past it rather than reporting a shortfall next to an untouched balance. See issue #56.";
        ProjectionSeries =
        [
            CreateProjectionSeries("Portfolio balance", result.Projections.Select(point => point.TotalBalance), new SKColor(43, 111, 83)),
            CreateProjectionSeries("Expenses", result.Projections.Select(point => point.Expenses), new SKColor(190, 81, 66))
        ];
        ProjectionXAxes =
        [
            CreateLabelledAxis(
                "Age",
                result.Projections
                    .Select(point => point.Age.ToString(CultureInfo.CurrentCulture)))
        ];
        ProjectionChartDescription = $"Retirement cash-flow projection through age {draft.PlanThroughAge}. Ending balance is {FormatCurrency(result.EndingBalance)}.";
        UpdateBucketChart(draft, result);
    }

    protected override async Task ShareAsync(DeferredCompensationDraft draft)
    {
        await exportService.ShareAsync(draft, DeferredCompensationCalculator.Calculate(draft.ToInputs()));
    }

    private void UpdateBucketChart(DeferredCompensationDraft draft, DeferredCompensationResult result)
    {
        RetirementBucketSeries = draft.Accounts
            .Select((account, index) => (ISeries)CreateProjectionSeries(
                string.IsNullOrWhiteSpace(account.Name) ? $"Account {index + 1}" : account.Name,
                result.Projections.Select(point => point.Balances.GetValueOrDefault(account.Id)),
                BucketColors[index % BucketColors.Length]))
            .ToArray();

        var endingPoint = result.Projections[^1];
        var endingBalances = draft.Accounts.Select((account, index) =>
            $"{(string.IsNullOrWhiteSpace(account.Name) ? $"Account {index + 1}" : account.Name)} {FormatCurrency(endingPoint.Balances.GetValueOrDefault(account.Id))}");
        RetirementBucketDescription = $"Account balances from age {result.Projections[0].Age} through age {endingPoint.Age}.";
        RetirementBucketSummary = $"At age {endingPoint.Age}, projected account balances are {string.Join(", ", endingBalances)}.";
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
                .Select(item => $"{item.Name} withdrawal (gross): {FormatCurrency(item.Amount)}"))
            .Concat(point.WithdrawalTaxes > 0
                ? [$"Estimated withdrawal tax: -{FormatCurrency(point.WithdrawalTaxes)}"]
                : Array.Empty<string>());
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
            var editor = RetirementAccountEditorItem.FromAccount(account);
            editor.IsReadOnly = IsLinkedProfile;
            RetirementAccounts.Add(editor);
        }
    }

    private void ReplaceRetirementIncomeSources(IReadOnlyList<RetirementIncomeSource> incomeSources)
    {
        RetirementIncomeSources.Clear();
        foreach (var income in incomeSources)
        {
            var editor = RetirementIncomeEditorItem.FromIncome(income);
            editor.IsReadOnly = IsLinkedProfile;
            RetirementIncomeSources.Add(editor);
        }
    }

    private void ReplaceRetirementExpenses(IReadOnlyList<RetirementExpense> expenses)
    {
        RetirementAdditionalExpenses.Clear();
        foreach (var expense in expenses)
        {
            var editor = RetirementExpenseEditorItem.FromExpense(expense);
            editor.IsReadOnly = IsLinkedProfile;
            RetirementAdditionalExpenses.Add(editor);
        }
    }
}
