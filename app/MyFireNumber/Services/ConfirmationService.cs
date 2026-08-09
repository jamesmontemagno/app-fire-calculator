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