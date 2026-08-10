using MyFireNumber.ViewModels;

namespace MyFireNumber.Views;

public partial class HealthcareGapPage : CalculatorPageBase
{
    public HealthcareGapPage(HealthcareGapViewModel viewModel)
    {
        InitializeComponent();
        InitializeCalculator(viewModel);
    }
}
