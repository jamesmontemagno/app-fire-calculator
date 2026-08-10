using MyFireNumber.ViewModels;

namespace MyFireNumber.Views;

public partial class ReverseFirePage : CalculatorPageBase
{
    public ReverseFirePage(ReverseFireViewModel viewModel)
    {
        InitializeComponent();
        InitializeCalculator(viewModel);
    }
}
