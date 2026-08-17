using MyFireNumber.Storage;
using System.Text.Json;

namespace MyFireNumber.Services;

public interface IAppDataTransferService
{
    Task ExportAsync();

    Task<bool> PickAndImportAsync();
}

public sealed class AppDataTransferService(
    ILocalDataArchiveRepository archiveRepository,
    IOnboardingService onboardingService,
    IThemeService themeService,
    ICalculatorDefaultsService calculatorDefaultsService,
    IAppBehaviorPreferencesService behaviorPreferencesService,
    ICurrencyPreferencesService currencyPreferencesService,
    IProfileService profileService) : IAppDataTransferService
{
    private const int ArchiveVersion = 1;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task ExportAsync()
    {
        var envelope = new AppDataEnvelope(
            ArchiveVersion,
            DateTime.UtcNow,
            await archiveRepository.ExportAsync(),
            themeService.Preference,
            onboardingService.IsComplete,
            onboardingService.RecommendationCalculatorId,
            calculatorDefaultsService.Current,
            behaviorPreferencesService.Current,
            currencyPreferencesService.SelectedOption);
        var filePath = Path.Combine(
            FileSystem.CacheDirectory,
            $"my-fire-number-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
        await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(envelope, SerializerOptions));

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Export My Fire # data",
            File = new ShareFile(filePath, "application/json")
        });
    }

    public async Task<bool> PickAndImportAsync()
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Choose a My Fire # backup"
        });
        if (result is null)
        {
            return false;
        }

        await using var stream = await result.OpenReadAsync();
        var envelope = await JsonSerializer.DeserializeAsync<AppDataEnvelope>(stream, SerializerOptions)
            ?? throw new InvalidDataException("The selected backup is empty.");
        if (envelope.Version != ArchiveVersion || envelope.LocalData is null)
        {
            throw new InvalidDataException("The selected backup is not supported.");
        }

        await archiveRepository.ImportAsync(envelope.LocalData);
        Preferences.Default.Clear();
        if (envelope.CalculatorDefaults is not null)
        {
            calculatorDefaultsService.Save(envelope.CalculatorDefaults);
        }

        if (envelope.BehaviorPreferences is not null)
        {
            behaviorPreferencesService.Save(envelope.BehaviorPreferences);
        }

        currencyPreferencesService.Save(envelope.CurrencyOption ?? CurrencyPreferencesService.DeviceRegion);
        themeService.Apply(envelope.BehaviorPreferences?.HighContrast == true
            ? ThemePreference.Dark
            : envelope.Theme);
        if (envelope.OnboardingComplete)
        {
            onboardingService.Complete();
        }

        if (!string.IsNullOrWhiteSpace(envelope.RecommendationCalculatorId))
        {
            onboardingService.SetRecommendation(envelope.RecommendationCalculatorId);
        }

        await profileService.NotifyExternalChangeAsync();
        return true;
    }

    private sealed record AppDataEnvelope(
        int Version,
        DateTime ExportedAtUtc,
        LocalDataArchive LocalData,
        ThemePreference Theme,
        bool OnboardingComplete,
        string? RecommendationCalculatorId,
        CalculatorDefaults? CalculatorDefaults,
        AppBehaviorPreferences? BehaviorPreferences,
        string? CurrencyOption);
}
