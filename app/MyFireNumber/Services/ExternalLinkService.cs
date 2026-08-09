namespace MyFireNumber.Services;

public interface IExternalLinkService
{
    Task OpenTermsAsync();
    Task OpenPrivacyAsync();
}

public sealed class ExternalLinkService : IExternalLinkService
{
    private static readonly Uri TermsUri = new("https://jamesmontemagno.github.io/app-fire-calculator/legal#terms");
    private static readonly Uri PrivacyUri = new("https://jamesmontemagno.github.io/app-fire-calculator/legal#privacy");

    public Task OpenTermsAsync() => Launcher.Default.OpenAsync(TermsUri);

    public Task OpenPrivacyAsync() => Launcher.Default.OpenAsync(PrivacyUri);
}
