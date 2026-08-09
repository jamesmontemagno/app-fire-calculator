namespace MyFireNumber.Services;

public interface INavigationService
{
    Task GoToAsync(string route);
}

public sealed class ShellNavigationService : INavigationService
{
    public Task GoToAsync(string route)
    {
        return Shell.Current.GoToAsync(route);
    }
}