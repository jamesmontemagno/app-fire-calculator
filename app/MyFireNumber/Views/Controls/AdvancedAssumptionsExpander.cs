using MyFireNumber.Core.Presentation;

namespace MyFireNumber.Views.Controls;

public sealed class AdvancedAssumptionsExpander : VerticalStackLayout
{
    private readonly Button toggleButton;
    private bool isExpanded;
    private bool hasAppliedInitialState;
    private IAdvancedAssumptionsSessionState? sessionState;
    private string? calculatorId;

    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title),
        typeof(string),
        typeof(AdvancedAssumptionsExpander),
        "Advanced assumptions",
        propertyChanged: OnTitleChanged);

    public AdvancedAssumptionsExpander()
    {
        Spacing = 12;
        toggleButton = new Button
        {
            Text = Title,
            HorizontalOptions = LayoutOptions.Start
        };
        SemanticProperties.SetDescription(toggleButton, "Show advanced assumptions");
        toggleButton.Clicked += OnToggleClicked;
        Children.Add(toggleButton);
    }

    /// <summary>
    /// Collapse as children are added rather than waiting for <c>Loaded</c>. Collapsing in
    /// <c>Loaded</c> meant the advanced fields were measured and drawn expanded first, so the
    /// input card visibly jumped shorter on every page open.
    /// </summary>
    protected override void OnChildAdded(Element child)
    {
        base.OnChildAdded(child);

        if (!ReferenceEquals(child, toggleButton) && child is VisualElement visualElement)
        {
            visualElement.IsVisible = isExpanded;
        }
    }

    /// <summary>
    /// Restores what the user last chose for this calculator in this app run, and starts recording
    /// their choices against it.
    /// </summary>
    /// <remarks>
    /// Called from <see cref="CalculatorPageBase.ApplyQueryAttributes"/>, which Shell runs after the
    /// page's XAML is inflated but before the page is given a handler and drawn. That ordering is
    /// what keeps this flash-free in both directions: children were already hidden by
    /// <see cref="OnChildAdded"/>, and a restored expansion is applied before the first paint rather
    /// than after it.
    /// </remarks>
    /// <param name="state">Session-scoped store shared by every calculator.</param>
    /// <param name="calculatorId">
    /// Catalog id, not the page type. Standard, Lean, and Fat FIRE share one page type, so keying on
    /// the page would make all three disclose together.
    /// </param>
    public void BindSessionState(IAdvancedAssumptionsSessionState state, string calculatorId)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(calculatorId);

        sessionState = state;
        this.calculatorId = calculatorId;
        SetExpanded(state.IsExpanded(calculatorId));
    }

    /// <summary>
    /// Safety net in case a child is ever added without going through <see cref="OnChildAdded"/>.
    /// Runs once, and re-applies whatever state is current rather than forcing a collapse, so it
    /// cannot undo a restore from <see cref="BindSessionState"/>.
    /// </summary>
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        if (Handler is null || hasAppliedInitialState)
        {
            return;
        }

        hasAppliedInitialState = true;
        SetExpanded(isExpanded);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    private static void OnTitleChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is AdvancedAssumptionsExpander expander && newValue is string title)
        {
            expander.toggleButton.Text = title;
        }
    }

    private void OnToggleClicked(object? sender, EventArgs e)
    {
        SetExpanded(!isExpanded);

        if (sessionState is not null && calculatorId is not null)
        {
            sessionState.SetExpanded(calculatorId, isExpanded);
        }
    }

    private void SetExpanded(bool value)
    {
        isExpanded = value;
        foreach (var child in Children.Skip(1))
        {
            if (child is VisualElement visualElement)
            {
                visualElement.IsVisible = value;
            }
        }

        toggleButton.Text = value ? $"Hide {Title.ToLowerInvariant()}" : Title;
        SemanticProperties.SetDescription(
            toggleButton,
            value ? "Hide advanced assumptions" : "Show advanced assumptions");
    }
}
