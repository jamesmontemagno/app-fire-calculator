using MyFireNumber.ViewModels;

namespace MyFireNumber.Views;

public partial class HomePage : ContentPage
{
    private readonly HomeViewModel viewModel;

    public HomePage(HomeViewModel viewModel)
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