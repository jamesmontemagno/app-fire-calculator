using MyFireNumber.ViewModels;

namespace MyFireNumber.Views;

public partial class AccountsPage : ContentPage
{
    private readonly AccountsViewModel viewModel;

    public AccountsPage(AccountsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        this.viewModel = viewModel;
        Appearing += OnAppearing;
    }

    private async void OnAppearing(object? sender, EventArgs eventArgs) => await viewModel.LoadAsync();
}
