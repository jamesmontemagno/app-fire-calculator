namespace MyFireNumber.Core.Calculations;

public sealed record StandardFirePreset(string Name, string Description, StandardFireDraft Draft)
{
    public static IReadOnlyList<StandardFirePreset> All { get; } =
    [
        new(
            "Conservative",
            "15% savings rate, 6% return",
            new StandardFireDraft(30, 65, 50_000, 12_000, 80_000, 0.06, 0.03, 0.04, 60_000)),
        new(
            "Moderate",
            "25% savings rate, 7% return",
            new StandardFireDraft(30, 55, 100_000, 24_000, 96_000, 0.07, 0.03, 0.04, 48_000)),
        new(
            "Aggressive",
            "50% savings rate, 7% return",
            new StandardFireDraft(30, 45, 150_000, 48_000, 96_000, 0.07, 0.03, 0.04, 40_000)),
        new(
            "Fat FIRE",
            "High income, high expenses",
            new StandardFireDraft(35, 50, 500_000, 100_000, 250_000, 0.07, 0.03, 0.035, 120_000))
    ];
}