using MyFireNumber.ViewModels;

namespace MyFireNumber.Views;

public partial class SavingsInvestmentPage : CalculatorPageBase
{
    public SavingsInvestmentPage(SavingsInvestmentViewModel viewModel)
    {
        InitializeComponent();
        InitializeCalculator(viewModel);
    }
}
