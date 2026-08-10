using MyFireNumber.ViewModels;

namespace MyFireNumber.Views;

public partial class BaristaFirePage : CalculatorPageBase
{
    public BaristaFirePage(BaristaFireViewModel viewModel)
    {
        InitializeComponent();
        InitializeCalculator(viewModel);
    }
}
