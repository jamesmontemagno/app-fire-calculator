using MyFireNumber.Core.Calculations;
using MyFireNumber.Services;

namespace MyFireNumber.ViewModels;

public sealed class StandardFireViewModel : FireNumberViewModelBase<StandardFireDraft>
{
    private readonly IStandardFireExportService exportService;

    public StandardFireViewModel(
        CalculatorViewModelServices services,
        IStandardFireExportService exportService)
        : base(services)
    {
        this.exportService = exportService;
    }

    public override bool IsStandardFire => true;

    public override string FireNumberLabel => "FIRE Number";

    public override string YearsToFireLabel => "Years to FIRE";

    public override string OutlookTitle => "Your FIRE outlook";

    public override string ProjectionTitle => "Portfolio projection";

    public override string PlanNameDescription => "Name for this saved Standard FIRE plan";

    public override string ExportDescription => "Create a Standard FIRE workbook on this device and open the share sheet.";

    protected override string CalculatorId => "standard-fire";

    protected override int DraftPayloadVersion => StandardFireDraft.PayloadVersion;

    protected override StandardFireDraft DefaultDraft => CalculatorDefaults.StandardFire;

    protected override string DefaultPlanName => "My Standard FIRE Plan";

    protected override string ExportSuccessMessage => "Your Standard FIRE workbook is ready to share.";

    protected override string ExportFailureMessage => "The Standard FIRE workbook could not be created locally.";

    protected override StandardFireDraft FromStandard(StandardFireDraft draft) => draft;

    protected override StandardFireDraft ToStandard(StandardFireDraft draft) => draft;

    protected override StandardFireResult CalculateResult(StandardFireDraft draft)
    {
        return FinancialCalculator.CalculateStandardFire(draft.ToFireInputs());
    }

    protected override async Task ShareAsync(StandardFireDraft draft)
    {
        await exportService.ShareAsync(draft, FinancialCalculator.CalculateStandardFire(draft.ToFireInputs()));
    }
}
