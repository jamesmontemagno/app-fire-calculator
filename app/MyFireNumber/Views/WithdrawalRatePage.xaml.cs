using MyFireNumber.Core.Presentation;
using MyFireNumber.ViewModels;

namespace MyFireNumber.Views;

public partial class WithdrawalRatePage : CalculatorPageBase
{
    public WithdrawalRatePage(WithdrawalRateViewModel viewModel, IAdvancedAssumptionsSessionState advancedAssumptionsState)
        : base(advancedAssumptionsState)
    {
        InitializeComponent();
        InitializeCalculator(viewModel);
    }
}
