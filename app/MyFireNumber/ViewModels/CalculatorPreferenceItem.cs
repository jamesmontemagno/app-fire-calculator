using CommunityToolkit.Mvvm.ComponentModel;

namespace MyFireNumber.ViewModels;

public partial class CalculatorPreferenceItem(string calculatorId, string title, bool isVisible, int sortOrder) : ObservableObject
{
    public string CalculatorId { get; } = calculatorId;

    public string Title { get; } = title;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VisibilityAction))]
    private bool isVisible = isVisible;

    [ObservableProperty]
    private int sortOrder = sortOrder;

    public string VisibilityAction => IsVisible ? "Hide" : "Show";
}
