namespace MyFireNumber.Views.Controls;

public partial class FieldHelpView : ContentView
{
    public static readonly BindableProperty HeaderProperty = BindableProperty.Create(
        nameof(Header),
        typeof(string),
        typeof(FieldHelpView),
        string.Empty);

    public static readonly BindableProperty HelpTextProperty = BindableProperty.Create(
        nameof(HelpText),
        typeof(string),
        typeof(FieldHelpView),
        string.Empty);

    public FieldHelpView()
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

    private async void OnInfoClicked(object? sender, EventArgs e)
    {
        await Shell.Current.DisplayAlertAsync(Header, HelpText, "Close");
    }
}
