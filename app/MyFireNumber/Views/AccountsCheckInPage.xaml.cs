using MyFireNumber.ViewModels;

namespace MyFireNumber.Views;

public partial class AccountsCheckInPage : ContentPage
{
    private readonly AccountsCheckInViewModel viewModel;

    public AccountsCheckInPage(AccountsCheckInViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        this.viewModel = viewModel;
        Appearing += OnAppearing;
    }

    private async void OnAppearing(object? sender, EventArgs eventArgs) => await viewModel.LoadAsync();
}
