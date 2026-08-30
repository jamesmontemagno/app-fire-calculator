using MyFireNumber.Core.Presentation;
using MyFireNumber.ViewModels;

namespace MyFireNumber.Views;

public partial class InterestCalculatorPage : CalculatorPageBase
{
    public InterestCalculatorPage(InterestCalculatorViewModel viewModel, IAdvancedAssumptionsSessionState advancedAssumptionsState)
        : base(advancedAssumptionsState)
    {
        InitializeComponent();
        InitializeCalculator(viewModel);
    }
}
