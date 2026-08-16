using MyFireNumber.ViewModels;

namespace MyFireNumber.Views;

public partial class TimelineOnboardingPage : ContentPage
{
    public TimelineOnboardingPage(TimelineOnboardingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
