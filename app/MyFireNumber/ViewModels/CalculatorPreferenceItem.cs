using CommunityToolkit.Mvvm.ComponentModel;

namespace MyFireNumber.ViewModels;

public partial class CalculatorPreferenceItem(string calculatorId, string title, bool isVisible, int sortOrder) : ObservableObject
{
    public string CalculatorId { get; } = calculatorId;

    public string Title { get; } = title;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VisibilityIcon), nameof(VisibilityDescription))]
    private bool isVisible = isVisible;

    [ObservableProperty]
    private int sortOrder = sortOrder;

    public string VisibilityIcon => IsVisible ? "\uf070" : "\uf06e";

    public string VisibilityDescription => IsVisible
        ? $"Hide {Title} from Home."
        : $"Show {Title} on Home.";
}
