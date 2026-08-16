namespace MyFireNumber.Views.Controls;

public partial class CalculationExplanationView : ContentView
{
    public static readonly BindableProperty ExplanationProperty = BindableProperty.Create(
        nameof(Explanation),
        typeof(string),
        typeof(CalculationExplanationView),
        string.Empty,
        propertyChanged: OnExplanationChanged);

    private bool isExpanded;

    public CalculationExplanationView()
    {
        InitializeComponent();
        ToggleButton.Clicked += OnToggleClicked;
    }

    public string Explanation
    {
        get => (string)GetValue(ExplanationProperty);
        set => SetValue(ExplanationProperty, value);
    }

    private static void OnExplanationChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is CalculationExplanationView view && newValue is string explanation)
        {
            view.ExplanationLabel.Text = explanation;
        }
    }

    private void OnToggleClicked(object? sender, EventArgs e)
    {
        isExpanded = !isExpanded;
        ExplanationLabel.IsVisible = isExpanded;
        ToggleButton.Text = isExpanded ? "Hide how it is calculated" : "How it is calculated";
        SemanticProperties.SetDescription(
            ToggleButton,
            isExpanded ? "Hide how this calculator works" : "Show how this calculator works");
    }
}
