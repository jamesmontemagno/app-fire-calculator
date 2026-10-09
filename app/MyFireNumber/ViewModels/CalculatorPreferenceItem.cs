using CommunityToolkit.Mvvm.ComponentModel;

namespace MyFireNumber.ViewModels;

public partial class CalculatorPreferenceItem(string calculatorId, string title, bool isVisible, int sortOrder) : ObservableObject
{
    public string CalculatorId { get; } = calculatorId;

    public string Title { get; } = title;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VisibilityIcon), nameof(VisibilityDescription))]
    public partial bool IsVisible { get; set; } = isVisible;

    [ObservableProperty]
    public partial int SortOrder { get; set; } = sortOrder;

    public string VisibilityIcon => IsVisible ? "\uf070" : "\uf06e";

    public string VisibilityDescription => IsVisible
        ? $"Hide {Title} from Home."
        : $"Show {Title} on Home.";
}
