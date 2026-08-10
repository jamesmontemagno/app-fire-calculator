using MyFireNumber.ViewModels;

namespace MyFireNumber.Views;

public partial class OnboardingChoicePage : ContentPage
{
    public OnboardingChoicePage(OnboardingChoiceViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
