namespace MyFireNumber.Services;

public interface IErrorPresentationService
{
    Task ShowAsync(string title, string message);
}

public sealed class ErrorPresentationService : IErrorPresentationService
{
    public Task ShowAsync(string title, string message)
    {
        return MainThread.InvokeOnMainThreadAsync(
            () => Shell.Current.DisplayAlertAsync(title, message, "OK"));
    }
}
