using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Presentation;

namespace MyFireNumber.Services;

public interface IRetirementCashFlowPromptService
{
    Task<RetirementAccountType?> ChooseAccountTypeAsync();

    Task<PropertyAssetType?> ChooseAssetTypeAsync();

    Task<bool?> ChooseIncomeTaxTreatmentAsync();
}

public sealed class RetirementCashFlowPromptService : IRetirementCashFlowPromptService
{
    public async Task<RetirementAccountType?> ChooseAccountTypeAsync()
    {
        var choices = Enum.GetValues<RetirementAccountType>()
            .ToDictionary(FormatAccountType, type => type, StringComparer.Ordinal);
        var choice = await Shell.Current.DisplayActionSheetAsync(
            "New account",
            "Cancel",
            null,
            [.. choices.Keys]);
        return choice is not null && choices.TryGetValue(choice, out var type) ? type : null;
    }

    public async Task<PropertyAssetType?> ChooseAssetTypeAsync()
    {
        var choices = Enum.GetValues<PropertyAssetType>()
            .ToDictionary(PropertyAssetLabels.Format, type => type, StringComparer.Ordinal);
        var choice = await Shell.Current.DisplayActionSheetAsync(
            "New asset",
            "Cancel",
            null,
            [.. choices.Keys]);
        return choice is not null && choices.TryGetValue(choice, out var type) ? type : null;
    }

    public async Task<bool?> ChooseIncomeTaxTreatmentAsync()
    {
        var choice = await Shell.Current.DisplayActionSheetAsync(
            "New income source",
            "Cancel",
            null,
            "After-tax income",
            "Pre-tax income");
        return choice switch
        {
            "After-tax income" => true,
            "Pre-tax income" => false,
            _ => null
        };
    }

    private static string FormatAccountType(RetirementAccountType type) => type switch
    {
        RetirementAccountType.Deferred => "Deferred compensation",
        RetirementAccountType.Traditional => "Traditional 401(k) or IRA",
        RetirementAccountType.Roth => "Roth IRA",
        RetirementAccountType.Taxable => "Taxable brokerage",
        RetirementAccountType.Savings => "Savings",
        RetirementAccountType.Hsa => "HSA",
        RetirementAccountType.Other => "Other",
        _ => type.ToString()
    };
}
