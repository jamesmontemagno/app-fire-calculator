using MyFireNumber.Core.Presentation;
using MyFireNumber.ViewModels;
using MyFireNumber.Core.Profile;
using MyFireNumber.Views.Controls;

namespace MyFireNumber.Views;

/// <summary>
/// Shared lifecycle for calculator pages: applies navigation query attributes, restores the
/// advanced assumptions disclosure state, and flushes the pending local draft when the page or
/// window goes away.
/// </summary>
public abstract class CalculatorPageBase : ContentPage, IQueryAttributable
{
    private readonly IAdvancedAssumptionsSessionState advancedAssumptionsState;
    private ICalculatorViewModel? calculatorViewModel;
    private Window? subscribedWindow;

    protected CalculatorPageBase(IAdvancedAssumptionsSessionState advancedAssumptionsState)
    {
        this.advancedAssumptionsState = advancedAssumptionsState;
    }

    protected void InitializeCalculator(ICalculatorViewModel viewModel)
    {
        calculatorViewModel = viewModel;
        BindingContext = viewModel;
        AddScenarioModeBanner();
    }

    private void AddScenarioModeBanner()
    {
        if (Content is null || Content is Grid { StyleId: "ScenarioModeRoot" })
        {
            return;
        }

        var originalContent = Content;
        var status = new Label
        {
            FontSize = 12,
            VerticalTextAlignment = TextAlignment.Center
        };
        status.SetBinding(Label.TextProperty, "ScenarioDataModeText");

        var unlink = new Button
        {
            Text = "Unlink",
            FontSize = 12,
            Padding = new Thickness(12, 6)
        };
        SemanticProperties.SetDescription(unlink, "Make this linked scenario an independent standalone snapshot.");
        unlink.SetBinding(Button.CommandProperty, "UnlinkFromProfileCommand");

        var bannerContent = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 12
        };
        bannerContent.Add(status);
        bannerContent.Add(unlink, 1);

        var banner = new Border
        {
            Padding = new Thickness(16, 8),
            Background = new SolidColorBrush(Color.FromArgb("#E8F3EF")),
            Stroke = new SolidColorBrush(Color.FromArgb("#2B6F57")),
            StrokeThickness = 1,
            Content = bannerContent
        };
        banner.SetBinding(IsVisibleProperty, "IsLinkedProfile");

        var root = new Grid
        {
            StyleId = "ScenarioModeRoot",
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star)
            }
        };
        root.Add(banner);
        root.Add(originalContent, 0, 1);
        Content = root;
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
        var requestedMode = query.TryGetValue("dataMode", out var dataModeValue) &&
            Enum.TryParse<ScenarioDataMode>(dataModeValue?.ToString(), out var parsedMode)
                ? parsedMode
                : (ScenarioDataMode?)null;

        RestoreAdvancedAssumptions(viewModel.CalculatorId);

        await viewModel.LoadAsync(planId, returnHomeAfterSave, requestedMode);
    }

    /// <summary>
    /// Reapplies the disclosure state the user last chose for this calculator in this app run.
    /// </summary>
    /// <remarks>
    /// <para>Runs synchronously, before the first <c>await</c> above. Shell applies query attributes
    /// after the page's XAML is inflated but before the page is given a handler and drawn, so a
    /// restored expansion lands ahead of the first paint and never flashes.</para>
    /// <para>Keyed on the view model's calculator id rather than the page type on purpose: Standard,
    /// Lean, and Fat FIRE all resolve to <see cref="FireNumberPage"/>, so a page-typed key would let
    /// one variant open the other two.</para>
    /// </remarks>
    private void RestoreAdvancedAssumptions(string calculatorId)
    {
        foreach (var expander in FindExpanders(this))
        {
            expander.BindSessionState(advancedAssumptionsState, calculatorId);
        }
    }

    /// <summary>
    /// Walks the page's own logical tree for expanders, so the state survives without every
    /// calculator's XAML having to name itself.
    /// </summary>
    private static IEnumerable<AdvancedAssumptionsExpander> FindExpanders(Element root)
    {
        foreach (var child in ((IVisualTreeElement)root).GetVisualChildren())
        {
            if (child is AdvancedAssumptionsExpander expander)
            {
                // Expanders are never nested inside one another, so stop descending here.
                yield return expander;
            }
            else if (child is Element element)
            {
                foreach (var nested in FindExpanders(element))
                {
                    yield return nested;
                }
            }
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (calculatorViewModel is not null)
        {
            _ = calculatorViewModel.RefreshLinkedProfileAsync();
        }
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
