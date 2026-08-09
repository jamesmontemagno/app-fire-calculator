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
            var planId = query.TryGetValue("planId", out var savedPlanId) && savedPlanId is string id
                ? Uri.UnescapeDataString(id)
                : null;
            await viewModel.LoadAsync(Uri.UnescapeDataString(value), planId);
        }
    }
}