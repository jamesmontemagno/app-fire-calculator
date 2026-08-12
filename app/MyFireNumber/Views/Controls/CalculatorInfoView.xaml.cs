namespace MyFireNumber.Views.Controls;

public partial class CalculatorInfoView : ContentView
{
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title),
        typeof(string),
        typeof(CalculatorInfoView),
        string.Empty);

    public static readonly BindableProperty SummaryProperty = BindableProperty.Create(
        nameof(Summary),
        typeof(string),
        typeof(CalculatorInfoView),
        string.Empty);

    public static readonly BindableProperty DetailsProperty = BindableProperty.Create(
        nameof(Details),
        typeof(string),
        typeof(CalculatorInfoView),
        string.Empty);

    public static readonly BindableProperty IconGlyphProperty = BindableProperty.Create(
        nameof(IconGlyph),
        typeof(string),
        typeof(CalculatorInfoView),
        "\uf05a");

    public CalculatorInfoView()
    {
        InitializeComponent();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Summary
    {
        get => (string)GetValue(SummaryProperty);
        set => SetValue(SummaryProperty, value);
    }

    public string Details
    {
        get => (string)GetValue(DetailsProperty);
        set => SetValue(DetailsProperty, value);
    }

    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    private void OnDetailsClicked(object? sender, EventArgs e)
    {
        DetailsLabel.IsVisible = !DetailsLabel.IsVisible;
        DetailsButton.Text = DetailsLabel.IsVisible ? "Show less" : "Learn more";
        SemanticProperties.SetDescription(
            DetailsButton,
            DetailsLabel.IsVisible
                ? "Hide additional calculator information"
                : "Show more information about this calculator");
    }
}
