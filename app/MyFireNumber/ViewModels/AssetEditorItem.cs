using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Presentation;
using System.Globalization;

namespace MyFireNumber.ViewModels;

/// <summary>
/// Editable working copy of a <see cref="PropertyAsset"/>. Current value is the figure net worth
/// uses; purchase value is captured only so the app can show how the asset has appreciated or
/// depreciated since it was bought.
/// </summary>
public sealed partial class AssetEditorItem : ObservableObject
{
    [ObservableProperty]
    private string id = Guid.NewGuid().ToString("N");

    [ObservableProperty]
    private string name = "Asset";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TypeLabel))]
    private PropertyAssetType type = PropertyAssetType.Other;

    [ObservableProperty]
    private string currentValueText = string.Empty;

    [ObservableProperty]
    private string purchaseValueText = string.Empty;

    /// <summary>
    /// Off for a personal-use asset someone would rather not count. The value is still stored and
    /// still shown on the asset itself; it simply stops contributing to net worth.
    /// </summary>
    [ObservableProperty]
    private bool includeInNetWorth = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpansionGlyph))]
    private bool isExpanded;

    /// <summary>Set by the Accounts overview after loading check-in history; not persisted.</summary>
    [ObservableProperty]
    private string freshnessText = "Never confirmed";

    [ObservableProperty]
    private bool isOverdue;

    /// <summary>
    /// Appreciation or depreciation since purchase, set by whoever owns this item so the text can be
    /// formatted with the user's currency preference. Empty when no purchase value was entered.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValueChange))]
    private string valueChangeText = string.Empty;

    public bool HasValueChange => !string.IsNullOrWhiteSpace(ValueChangeText);

    public string TypeLabel => PropertyAssetLabels.Format(Type);

    public string ExpansionGlyph => IsExpanded ? "\uf078" : "\uf054";

    public event EventHandler? Changed;

    public static AssetEditorItem FromAsset(PropertyAsset asset) => new()
    {
        Id = asset.Id,
        Name = asset.Name,
        Type = asset.Type,
        CurrentValueText = asset.CurrentValue.ToString("0.##", CultureInfo.CurrentCulture),
        PurchaseValueText = asset.PurchaseValue.ToString("0.##", CultureInfo.CurrentCulture),
        IncludeInNetWorth = asset.IncludeInNetWorth
    };

    public static AssetEditorItem CreateNew(PropertyAssetType type) => new()
    {
        Type = type,
        Name = PropertyAssetLabels.PlaceholderName(type),
        IsExpanded = true
    };

    public bool TryCreateAsset(out PropertyAsset asset, out string error)
    {
        asset = new PropertyAsset(Id, Name.Trim(), Type, 0, 0, IncludeInNetWorth);
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(Name))
        {
            error = "enter a name.";
            return false;
        }

        if (!TryParseNonNegative(CurrentValueText, out var currentValue))
        {
            error = "enter a current value of zero or more.";
            return false;
        }

        // An unentered purchase value means "not tracked", not zero dollars.
        if (!string.IsNullOrWhiteSpace(PurchaseValueText) && !TryParseNonNegative(PurchaseValueText, out _))
        {
            error = "enter a purchase value of zero or more.";
            return false;
        }

        TryParseNonNegative(PurchaseValueText, out var purchaseValue);
        asset = new PropertyAsset(Id, Name.Trim(), Type, currentValue, purchaseValue, IncludeInNetWorth);
        return true;
    }

    partial void OnNameChanged(string value) => Changed?.Invoke(this, EventArgs.Empty);
    partial void OnCurrentValueTextChanged(string value) => Changed?.Invoke(this, EventArgs.Empty);
    partial void OnPurchaseValueTextChanged(string value) => Changed?.Invoke(this, EventArgs.Empty);
    partial void OnIncludeInNetWorthChanged(bool value) => Changed?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void ToggleExpanded() => IsExpanded = !IsExpanded;

    private static bool TryParseNonNegative(string value, out double number) =>
        double.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out number) && number >= 0;
}
