using MyFireNumber.ViewModels;

namespace MyFireNumber.Views;

public partial class CalculatorDetailPage : ContentPage, IQueryAttributable
{
    private readonly CalculatorDetailViewModel viewModel;

    public CalculatorDetailPage(CalculatorDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        this.viewModel = viewModel;
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("calculatorId", out var calculatorId) && calculatorId is string value)
        {
            await viewModel.LoadAsync(Uri.UnescapeDataString(value));
        }
    }
}