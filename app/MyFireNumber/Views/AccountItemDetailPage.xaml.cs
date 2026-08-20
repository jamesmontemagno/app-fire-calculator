using MyFireNumber.ViewModels;

namespace MyFireNumber.Views;

public partial class AccountItemDetailPage : ContentPage
{
    public AccountItemDetailPage(AccountItemDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
