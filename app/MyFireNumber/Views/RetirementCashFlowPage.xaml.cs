using MyFireNumber.ViewModels;

namespace MyFireNumber.Views;

public partial class RetirementCashFlowPage : CalculatorPageBase
{
    public RetirementCashFlowPage(RetirementCashFlowViewModel viewModel)
    {
        InitializeComponent();
        InitializeCalculator(viewModel);
    }
}
