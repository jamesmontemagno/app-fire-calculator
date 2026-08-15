using MyFireNumber.Core.Presentation;

namespace MyFireNumber.Services;

/// <summary>
/// Remembers whether a calculator's recurring amounts are shown monthly or annually.
/// </summary>
/// <remarks>
/// <para>This is a display preference, not an input. It is stored per calculator in
/// <see cref="Preferences"/> rather than in a draft, because it changes nothing about what a
/// calculator computes — routing it through a draft would bump a payload version, alter saved plans
/// and exported workbooks, and blur the line between this and
/// <c>ContributionFrequency</c>, which genuinely is a calculation input.</para>
/// <para>The service holds no logic on purpose: <see cref="Preferences"/> is unavailable to the unit
/// test project, so everything worth testing lives in <see cref="CurrencyPeriodMath"/> and
/// <see cref="PeriodicAmountField"/> instead.</para>
/// </remarks>
public interface IDisplayPeriodPreferencesService
{
    CurrencyPeriod Get(string calculatorId);

    void Save(string calculatorId, CurrencyPeriod period);
}

public sealed class DisplayPeriodPreferencesService : IDisplayPeriodPreferencesService
{
    private const string KeyPrefix = "display-period:";

    public CurrencyPeriod Get(string calculatorId)
    {
        var stored = Preferences.Default.Get(KeyPrefix + calculatorId, string.Empty);
        return Enum.TryParse(stored, out CurrencyPeriod period) && Enum.IsDefined(period)
            ? period
            : CurrencyPeriod.Annual;
    }

    public void Save(string calculatorId, CurrencyPeriod period)
    {
        Preferences.Default.Set(KeyPrefix + calculatorId, period.Validated(nameof(period)).ToString());
    }
}
