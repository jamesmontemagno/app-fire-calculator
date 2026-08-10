using MyFireNumber.ViewModels;

namespace MyFireNumber.Views;

/// <summary>
/// Shared lifecycle for calculator pages: applies navigation query attributes and
/// flushes the pending local draft when the page or window goes away.
/// </summary>
public abstract class CalculatorPageBase : ContentPage, IQueryAttributable
{
    private ICalculatorViewModel? calculatorViewModel;
    private Window? subscribedWindow;

    protected void InitializeCalculator(ICalculatorViewModel viewModel)
    {
        calculatorViewModel = viewModel;
        BindingContext = viewModel;
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (calculatorViewModel is null)
        {
            return;
        }

        var planId = query.TryGetValue("planId", out var savedPlanId) && savedPlanId is string id
            ? Uri.UnescapeDataString(id)
            : null;
        var returnHomeAfterSave = query.TryGetValue("returnHomeAfterSave", out var returnHomeValue)
            && bool.TryParse(returnHomeValue?.ToString(), out var parsedReturnHome)
            && parsedReturnHome;

        await calculatorViewModel.LoadAsync(planId, returnHomeAfterSave);
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
        if (calculatorViewModel is not null)
        {
            await calculatorViewModel.FlushPendingDraftAsync();
        }

        base.OnDisappearing();
    }

    private async void OnWindowSuspending(object? sender, EventArgs eventArgs)
    {
        if (calculatorViewModel is not null)
        {
            await calculatorViewModel.FlushPendingDraftAsync();
        }
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
