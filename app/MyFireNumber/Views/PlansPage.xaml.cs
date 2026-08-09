using MyFireNumber.ViewModels;

namespace MyFireNumber.Views;

public partial class PlansPage : ContentPage
{
    private readonly PlansViewModel viewModel;

    public PlansPage(PlansViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        this.viewModel = viewModel;
        Appearing += OnAppearing;
    }

    private async void OnAppearing(object? sender, EventArgs eventArgs)
    {
        await viewModel.LoadAsync();
    }
}