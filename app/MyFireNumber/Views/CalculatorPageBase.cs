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

    /// <summary>
    /// Lets a page that serves several calculator variants pick its view model from
    /// the navigation query before the draft is restored.
    /// </summary>
    protected virtual ICalculatorViewModel? SelectViewModel(IDictionary<string, object> query) => calculatorViewModel;

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var viewModel = SelectViewModel(query);
        if (viewModel is null)
        {
            return;
        }

        if (!ReferenceEquals(viewModel, calculatorViewModel))
        {
            InitializeCalculator(viewModel);
        }

        var planId = query.TryGetValue("planId", out var savedPlanId) && savedPlanId is string id
            ? Uri.UnescapeDataString(id)
            : null;
        var returnHomeAfterSave = query.TryGetValue("returnHomeAfterSave", out var returnHomeValue)
            && bool.TryParse(returnHomeValue?.ToString(), out var parsedReturnHome)
            && parsedReturnHome;

        await viewModel.LoadAsync(planId, returnHomeAfterSave);
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
