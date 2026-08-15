using MyFireNumber.Core.Presentation;
using MyFireNumber.ViewModels;

namespace MyFireNumber.Views;

public partial class BaristaFirePage : CalculatorPageBase
{
    public BaristaFirePage(BaristaFireViewModel viewModel, IAdvancedAssumptionsSessionState advancedAssumptionsState)
        : base(advancedAssumptionsState)
    {
        InitializeComponent();
        InitializeCalculator(viewModel);
    }
}
