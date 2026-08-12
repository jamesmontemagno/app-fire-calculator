namespace MyFireNumber.Views.Controls;

public partial class NumericInputView : ContentView
{
    public static readonly BindableProperty HeaderProperty = BindableProperty.Create(nameof(Header), typeof(string), typeof(NumericInputView), string.Empty);

    public static readonly BindableProperty HelpTextProperty = BindableProperty.Create(nameof(HelpText), typeof(string), typeof(NumericInputView), string.Empty);

    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(NumericInputView), string.Empty);

    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text),
        typeof(string),
        typeof(NumericInputView),
        string.Empty,
        defaultBindingMode: BindingMode.TwoWay);

    public NumericInputView()
    {
        InitializeComponent();
    }

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public string HelpText
    {
        get => (string)GetValue(HelpTextProperty);
        set => SetValue(HelpTextProperty, value);
    }

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
}
