using MyFireNumber.ViewModels;

namespace MyFireNumber.Views;

public partial class CoastFirePage : CalculatorPageBase
{
    public CoastFirePage(CoastFireViewModel viewModel)
    {
        InitializeComponent();
        InitializeCalculator(viewModel);
    }
}
