using MyFireNumber.Core.Calculations;
using MyFireNumber.Services;

namespace MyFireNumber.ViewModels;

public sealed class LeanFireViewModel : FireNumberViewModelBase<LeanFireDraft>
{
    private readonly ILeanFireExportService exportService;

    public LeanFireViewModel(
        CalculatorViewModelServices services,
        ILeanFireExportService exportService)
        : base(services)
    {
        this.exportService = exportService;
    }

    public override bool IsLeanFire => true;

    public override string FireNumberLabel => "Lean FIRE Number";

    public override string YearsToFireLabel => "Years to Lean FIRE";

    public override string OutlookTitle => "Your Lean FIRE outlook";

    public override string ProjectionTitle => "Lean FIRE projection";

    public override string PlanNameDescription => "Name for this saved Lean FIRE plan";

    public override string ExportDescription => "Create a Lean FIRE workbook on this device and open the share sheet.";

    protected override string CalculatorId => "lean-fire";

    protected override int DraftPayloadVersion => LeanFireDraft.PayloadVersion;

    protected override LeanFireDraft DefaultDraft => CalculatorDefaults.LeanFire;

    protected override string DefaultPlanName => "My Lean FIRE Plan";

    protected override string ExportSuccessMessage => "Your Lean FIRE workbook is ready to share.";

    protected override string ExportFailureMessage => "The Lean FIRE workbook could not be created locally.";

    protected override LeanFireDraft FromStandard(StandardFireDraft draft)
    {
        return new LeanFireDraft(
            draft.CurrentAge,
            draft.RetirementAge,
            draft.CurrentSavings,
            draft.AnnualContribution,
            draft.AnnualIncome,
            draft.ExpectedReturn,
            draft.InflationRate,
            draft.WithdrawalRate,
            draft.AnnualExpenses);
    }

    protected override StandardFireDraft ToStandard(LeanFireDraft draft)
    {
        return new StandardFireDraft(
            draft.CurrentAge,
            draft.RetirementAge,
            draft.CurrentSavings,
            draft.AnnualContribution,
            draft.AnnualIncome,
            draft.ExpectedReturn,
            draft.InflationRate,
            draft.WithdrawalRate,
            draft.AnnualExpenses);
    }

    protected override StandardFireResult CalculateResult(LeanFireDraft draft)
    {
        LeanStatusText = draft.IsWithinLeanThreshold ? "You're in Lean territory!" : "Expenses above the Lean threshold";
        LeanGuidanceText = draft.IsWithinLeanThreshold
            ? $"Your annual expenses are within the {FormatCurrency(FinancialCalculator.LeanFireThreshold)} Lean FIRE guideline."
            : $"Lean FIRE calculations use the {FormatCurrency(FinancialCalculator.LeanFireThreshold)} guideline; your entered expenses are {FormatCurrency(draft.AnnualExpenses)}.";
        return FinancialCalculator.CalculateLeanFire(draft.ToFireInputs()).Standard;
    }

    protected override async Task ShareAsync(LeanFireDraft draft)
    {
        await exportService.ShareAsync(draft, FinancialCalculator.CalculateLeanFire(draft.ToFireInputs()).Standard);
    }
}
