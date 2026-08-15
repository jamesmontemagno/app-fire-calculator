namespace MyFireNumber.Views.Controls;

public sealed class AdvancedAssumptionsExpander : VerticalStackLayout
{
    private readonly Button toggleButton;
    private bool isExpanded;
    private bool hasAppliedInitialState;

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
    /// Safety net in case a child is ever added without going through <see cref="OnChildAdded"/>.
    /// Runs once: Shell reuses cached pages, so collapsing on every <c>Loaded</c> would throw away
    /// the user's expanded section each time they returned to a calculator.
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

    private void OnToggleClicked(object? sender, EventArgs e) => SetExpanded(!isExpanded);

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
