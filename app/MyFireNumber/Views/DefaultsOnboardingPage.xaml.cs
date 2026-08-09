using MyFireNumber.ViewModels;

namespace MyFireNumber.Views;

public partial class DefaultsOnboardingPage : ContentPage
{
    public DefaultsOnboardingPage(DefaultsOnboardingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
