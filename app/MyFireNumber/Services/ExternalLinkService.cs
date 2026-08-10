namespace MyFireNumber.Services;

public interface IExternalLinkService
{
    Task OpenTermsAsync();
    Task OpenPrivacyAsync();
    Task<bool> OpenBookAsync(Uri bookUri);
}

public sealed class ExternalLinkService : IExternalLinkService
{
    private static readonly Uri TermsUri = new("https://myfirenumber.com/legal#terms");
    private static readonly Uri PrivacyUri = new("https://myfirenumber.com/legal#privacy");

    public Task OpenTermsAsync() => Launcher.Default.OpenAsync(TermsUri);

    public Task OpenPrivacyAsync() => Launcher.Default.OpenAsync(PrivacyUri);

    public Task<bool> OpenBookAsync(Uri bookUri)
    {
        if (!bookUri.IsAbsoluteUri || bookUri.Scheme != Uri.UriSchemeHttps)
        {
            return Task.FromResult(false);
        }

        return Launcher.Default.OpenAsync(bookUri);
    }
}
