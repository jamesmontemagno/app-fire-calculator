using MyFireNumber.ViewModels;

namespace MyFireNumber.Views;

public partial class CalculatorDetailPage : ContentPage, IQueryAttributable
{
    private readonly CalculatorDetailViewModel viewModel;
    private Window? subscribedWindow;

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

    protected override void OnAppearing()
    {
        base.OnAppearing();
        subscribedWindow = Window;
        if (subscribedWindow is not null)
        {
            subscribedWindow.Deactivated += OnWindowSuspending;
            subscribedWindow.Stopped += OnWindowSuspending;
        }
    }

    protected override async void OnDisappearing()
    {
        UnsubscribeWindowEvents();
        await viewModel.FlushPendingDraftAsync();
        base.OnDisappearing();
    }

    private async void OnWindowSuspending(object? sender, EventArgs eventArgs)
    {
        await viewModel.FlushPendingDraftAsync();
    }

    private void UnsubscribeWindowEvents()
    {
        if (subscribedWindow is null)
        {
            return;
        }

        subscribedWindow.Deactivated -= OnWindowSuspending;
        subscribedWindow.Stopped -= OnWindowSuspending;
        subscribedWindow = null;
    }
}