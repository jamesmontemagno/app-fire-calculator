using MyFireNumber.Core.Calculations;
using MyFireNumber.Services;

namespace MyFireNumber.ViewModels;

public sealed class FatFireViewModel : FireNumberViewModelBase<FatFireDraft>
{
    private readonly IFatFireExportService exportService;

    public FatFireViewModel(
        CalculatorViewModelServices services,
        IFatFireExportService exportService)
        : base(services)
    {
        this.exportService = exportService;
    }

    public override bool IsFatFire => true;

    public override string FireNumberLabel => "Fat FIRE Number";

    public override string YearsToFireLabel => "Years to Fat FIRE";

    public override string OutlookTitle => "Your Fat FIRE outlook";

    public override string ProjectionTitle => "Fat FIRE projection";

    public override string PlanNameDescription => "Name for this saved Fat FIRE plan";

    public override string ExportDescription => "Create a Fat FIRE workbook on this device and open the share sheet.";

    protected override string CalculatorId => "fat-fire";

    protected override int DraftPayloadVersion => FatFireDraft.PayloadVersion;

    protected override FatFireDraft DefaultDraft => CalculatorDefaults.FatFire;

    protected override string DefaultPlanName => "My Fat FIRE Plan";

    protected override string ExportSuccessMessage => "Your Fat FIRE workbook is ready to share.";

    protected override string ExportFailureMessage => "The Fat FIRE workbook could not be created locally.";

    protected override FatFireDraft FromStandard(StandardFireDraft draft)
    {
        return new FatFireDraft(
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

    protected override StandardFireDraft ToStandard(FatFireDraft draft)
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

    protected override StandardFireResult CalculateResult(FatFireDraft draft)
    {
        FatStatusText = draft.IsWithinFatThreshold ? "You're in Fat FIRE territory!" : "Below the Fat FIRE threshold";
        FatGuidanceText = draft.IsWithinFatThreshold
            ? $"Your annual expenses meet the {FormatCurrency(FinancialCalculator.FatFireThreshold)} Fat FIRE guideline."
            : $"Fat FIRE typically starts at {FormatCurrency(FinancialCalculator.FatFireThreshold)} in annual expenses; your current plan uses {FormatCurrency(draft.AnnualExpenses)}.";
        return FinancialCalculator.CalculateFatFire(draft.ToFireInputs()).Standard;
    }

    protected override async Task ShareAsync(FatFireDraft draft)
    {
        await exportService.ShareAsync(draft, FinancialCalculator.CalculateFatFire(draft.ToFireInputs()).Standard);
    }
}
