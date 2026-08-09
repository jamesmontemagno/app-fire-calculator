namespace MyFireNumber.Services;

public interface INavigationService
{
    Task GoToAsync(string route);
    Task GoToAsync(string route, IReadOnlyDictionary<string, object> parameters);
}

public sealed class ShellNavigationService : INavigationService
{
    public Task GoToAsync(string route)
    {
        return Shell.Current.GoToAsync(route);
    }

    public Task GoToAsync(string route, IReadOnlyDictionary<string, object> parameters)
    {
        var queryParameters = new ShellNavigationQueryParameters();
        foreach (var parameter in parameters)
        {
            queryParameters.Add(parameter.Key, parameter.Value);
        }

        return Shell.Current.GoToAsync(route, queryParameters);
    }
}