using MyFireNumber.Core.Presentation;
using MyFireNumber.ViewModels;

namespace MyFireNumber.Views;

public partial class SavingsInvestmentPage : CalculatorPageBase
{
    public SavingsInvestmentPage(SavingsInvestmentViewModel viewModel, IAdvancedAssumptionsSessionState advancedAssumptionsState)
        : base(advancedAssumptionsState)
    {
        InitializeComponent();
        InitializeCalculator(viewModel);
    }
}
