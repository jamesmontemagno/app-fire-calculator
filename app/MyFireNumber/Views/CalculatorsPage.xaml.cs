using MyFireNumber.ViewModels;

namespace MyFireNumber.Views;

public partial class CalculatorsPage : ContentPage
{
    public CalculatorsPage(CalculatorCatalogViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}