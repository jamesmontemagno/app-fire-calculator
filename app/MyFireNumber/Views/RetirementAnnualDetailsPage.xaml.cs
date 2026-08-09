using MyFireNumber.ViewModels;

namespace MyFireNumber.Views;

public partial class RetirementAnnualDetailsPage : ContentPage
{
    public RetirementAnnualDetailsPage(RetirementAnnualDetailsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
