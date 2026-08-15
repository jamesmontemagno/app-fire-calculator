namespace MyFireNumber.Views.Controls;

public partial class NumericInputView : ContentView
{
    public static readonly BindableProperty HeaderProperty = BindableProperty.Create(
        nameof(Header), typeof(string), typeof(NumericInputView), string.Empty, propertyChanged: OnHeaderPartChanged);

    public static readonly BindableProperty HelpTextProperty = BindableProperty.Create(nameof(HelpText), typeof(string), typeof(NumericInputView), string.Empty);

    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(NumericInputView), string.Empty);

    /// <summary>
    /// Appended to the label when this field holds a recurring amount, e.g. <c>per month</c>. Bound to
    /// the view model's display period so the label always says which period the number is in.
    /// </summary>
    public static readonly BindableProperty PeriodQualifierProperty = BindableProperty.Create(
        nameof(PeriodQualifier), typeof(string), typeof(NumericInputView), string.Empty, propertyChanged: OnHeaderPartChanged);

    /// <summary>Short marker shown beside the entry, e.g. <c>/mo</c>.</summary>
    public static readonly BindableProperty PeriodSuffixProperty = BindableProperty.Create(
        nameof(PeriodSuffix), typeof(string), typeof(NumericInputView), string.Empty, propertyChanged: OnPeriodSuffixChanged);

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

    public string PeriodQualifier
    {
        get => (string)GetValue(PeriodQualifierProperty);
        set => SetValue(PeriodQualifierProperty, value);
    }

    public string PeriodSuffix
    {
        get => (string)GetValue(PeriodSuffixProperty);
        set => SetValue(PeriodSuffixProperty, value);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>The label actually rendered, e.g. <c>Contribution (per month)</c>.</summary>
    public string DisplayHeader => string.IsNullOrWhiteSpace(PeriodQualifier)
        ? Header
        : $"{Header} ({PeriodQualifier})";

    public bool HasPeriodSuffix => !string.IsNullOrWhiteSpace(PeriodSuffix);

    /// <summary>Screen readers should hear the period, not the "/mo" punctuation.</summary>
    public string PeriodSuffixDescription => string.IsNullOrWhiteSpace(PeriodQualifier)
        ? string.Empty
        : $"Amount is {PeriodQualifier}";

    private static void OnHeaderPartChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (NumericInputView)bindable;
        view.OnPropertyChanged(nameof(DisplayHeader));
        view.OnPropertyChanged(nameof(PeriodSuffixDescription));
    }

    private static void OnPeriodSuffixChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((NumericInputView)bindable).OnPropertyChanged(nameof(HasPeriodSuffix));
    }
}
