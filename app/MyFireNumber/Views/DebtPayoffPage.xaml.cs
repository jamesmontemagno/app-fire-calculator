using MyFireNumber.ViewModels;

namespace MyFireNumber.Views;

public partial class DebtPayoffPage : CalculatorPageBase
{
    public DebtPayoffPage(DebtPayoffViewModel viewModel)
    {
        InitializeComponent();
        InitializeCalculator(viewModel);
    }
}
