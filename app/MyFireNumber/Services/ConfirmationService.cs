using MyFireNumber.Core.Calculations;

namespace MyFireNumber.Services;

public interface IConfirmationService
{
    Task<bool> ConfirmAsync(string title, string message, string accept, string cancel);
}

public sealed class ConfirmationService : IConfirmationService
{
    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
    {
        return Shell.Current.DisplayAlertAsync(title, message, accept, cancel);
    }
}

public interface IPlanNamePromptService
{
    Task<string?> PromptAsync(string title, string message, string initialValue);
}

public sealed class PlanNamePromptService : IPlanNamePromptService
{
    public Task<string?> PromptAsync(string title, string message, string initialValue)
    {
        return Shell.Current.DisplayPromptAsync(
            title,
            message,
            "Save",
            "Cancel",
            initialValue: initialValue);
    }
}

public interface IRetirementCashFlowPromptService
{
    Task<RetirementAccountType?> ChooseAccountTypeAsync();

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
        RetirementAccountType.Hsa => "HSA",
        _ => type.ToString()
    };
}