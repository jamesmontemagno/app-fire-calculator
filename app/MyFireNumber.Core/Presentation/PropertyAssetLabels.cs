using MyFireNumber.Core.Calculations;

namespace MyFireNumber.Core.Presentation;

/// <summary>Plain-language names for <see cref="PropertyAssetType"/>, shared by every asset UI.</summary>
public static class PropertyAssetLabels
{
    public static string Format(PropertyAssetType type) => type switch
    {
        PropertyAssetType.Home => "Primary home",
        PropertyAssetType.RealEstate => "Other real estate",
        PropertyAssetType.Land => "Land",
        PropertyAssetType.Vehicle => "Vehicle",
        PropertyAssetType.Collectible => "Collectible or valuables",
        PropertyAssetType.Other => "Other asset",
        _ => type.ToString()
    };

    /// <summary>A short hint describing what someone would typically enter for this asset type.</summary>
    public static string PlaceholderName(PropertyAssetType type) => type switch
    {
        PropertyAssetType.Home => "Our house",
        PropertyAssetType.RealEstate => "Rental condo",
        PropertyAssetType.Land => "Lake lot",
        PropertyAssetType.Vehicle => "Car",
        PropertyAssetType.Collectible => "Collection",
        _ => "Asset"
    };
}
