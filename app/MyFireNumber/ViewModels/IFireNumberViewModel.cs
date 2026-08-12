using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using MyFireNumber.Core.Calculations;

namespace MyFireNumber.ViewModels;

/// <summary>
/// Non-generic surface shared by the Standard, Lean, and Fat FIRE view models so a
/// single page can bind against any of the three variants with compiled bindings.
/// </summary>
public interface IFireNumberViewModel : ICalculatorViewModel
{
    string Title { get; }

    bool IsLoading { get; }

    bool IsStandardFire { get; }

    bool IsLeanFire { get; }

    bool IsFatFire { get; }

    string ValidationMessage { get; }

    bool HasValidationMessage { get; }

    string CurrentAgeText { get; set; }

    string RetirementAgeText { get; set; }

    string CurrentSavingsText { get; set; }

    string AnnualContributionText { get; set; }

    string AnnualIncomeText { get; set; }

    string AnnualExpensesText { get; set; }

    string ExpectedReturnText { get; set; }

    string InflationRateText { get; set; }

    string WithdrawalRateText { get; set; }

    string FireNumberText { get; }

    string YearsToFireText { get; }

    string FireAgeText { get; }

    string RetirementGoalText { get; }

    string SavingsRateText { get; }

    string MonthlyContributionText { get; }

    string ProgressDescription { get; }

    double ProgressToFire { get; }

    string ProjectionSummary { get; }

    string LeanStatusText { get; }

    string LeanGuidanceText { get; }

    string FatStatusText { get; }

    string FatGuidanceText { get; }

    string FireNumberLabel { get; }

    string YearsToFireLabel { get; }

    string OutlookTitle { get; }

    string ProjectionTitle { get; }

    string PlanNamePlaceholder { get; }

    string PlanNameDescription { get; }

    string ExportDescription { get; }

    string PlanNameText { get; set; }

    string PlanStatusMessage { get; }

    bool HasPlanStatusMessage { get; }

    string ExportStatusMessage { get; }

    bool HasExportStatusMessage { get; }

    string SavePlanActionText { get; }

    string SavePlanActionDescription { get; }

    IReadOnlyList<StandardFirePreset> StandardFirePresets { get; }

    StandardFirePreset? SelectedPreset { get; set; }

    IReadOnlyList<ISeries> ProjectionSeries { get; }

    Axis[] ProjectionXAxes { get; }

    string ProjectionChartDescription { get; }

    TimeSpan ChartAnimationsSpeed { get; }

    IRelayCommand ResetDefaultsCommand { get; }

    IAsyncRelayCommand SavePlanCommand { get; }

    IAsyncRelayCommand SavePlanAndCloseCommand { get; }

    IAsyncRelayCommand ExportCommand { get; }
}
