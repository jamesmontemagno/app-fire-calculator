using MyFireNumber.ViewModels;

namespace MyFireNumber.Views;

public partial class AccountsHistoryPage : ContentPage
{
    private readonly AccountsHistoryViewModel viewModel;

    public AccountsHistoryPage(AccountsHistoryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        this.viewModel = viewModel;
        Appearing += OnAppearing;
    }

    private async void OnAppearing(object? sender, EventArgs eventArgs) => await viewModel.LoadAsync();
}
