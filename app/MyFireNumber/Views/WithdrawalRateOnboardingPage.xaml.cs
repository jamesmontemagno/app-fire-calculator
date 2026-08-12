using MyFireNumber.ViewModels;

namespace MyFireNumber.Views;

public partial class WithdrawalRateOnboardingPage : ContentPage
{
    public WithdrawalRateOnboardingPage(WithdrawalRateOnboardingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
