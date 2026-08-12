namespace MyFireNumber.Views.Controls;

public sealed class AdvancedAssumptionsExpander : VerticalStackLayout
{
    private readonly Button toggleButton;
    private bool isExpanded;

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
        Loaded += (_, _) => SetExpanded(false);
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
