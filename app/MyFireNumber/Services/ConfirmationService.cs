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