namespace MyFireNumber.Services;

public interface IExternalLinkService
{
    Task OpenTermsAsync();
    Task OpenPrivacyAsync();
}

public sealed class ExternalLinkService : IExternalLinkService
{
    private static readonly Uri TermsUri = new("https://myfirenumber.com/legal#terms");
    private static readonly Uri PrivacyUri = new("https://myfirenumber.com/legal#privacy");

    public Task OpenTermsAsync() => Launcher.Default.OpenAsync(TermsUri);

    public Task OpenPrivacyAsync() => Launcher.Default.OpenAsync(PrivacyUri);
}
