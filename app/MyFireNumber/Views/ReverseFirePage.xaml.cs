using MyFireNumber.Core.Presentation;
using MyFireNumber.ViewModels;

namespace MyFireNumber.Views;

public partial class ReverseFirePage : CalculatorPageBase
{
    public ReverseFirePage(ReverseFireViewModel viewModel, IAdvancedAssumptionsSessionState advancedAssumptionsState)
        : base(advancedAssumptionsState)
    {
        InitializeComponent();
        InitializeCalculator(viewModel);
    }
}
