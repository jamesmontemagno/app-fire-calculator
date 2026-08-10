using MyFireNumber.ViewModels;

namespace MyFireNumber.Views;

public partial class WithdrawalRatePage : CalculatorPageBase
{
    public WithdrawalRatePage(WithdrawalRateViewModel viewModel)
    {
        InitializeComponent();
        InitializeCalculator(viewModel);
    }
}
