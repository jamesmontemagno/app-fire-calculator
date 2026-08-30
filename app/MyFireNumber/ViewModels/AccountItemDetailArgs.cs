namespace MyFireNumber.ViewModels;

/// <summary>One balance recorded for a single account or debt by a completed monthly check-in.</summary>
public sealed record AccountItemHistoryPoint(DateTime CompletedAtUtc, double Balance);

/// <summary>
/// Navigation payload for <see cref="AccountItemDetailViewModel"/>. <c>BalanceLabel</c> is the heading
/// shown above the current figure, so an asset can read "Current value" instead of a balance. Built by
/// <see cref="AccountsViewModel"/> from the live editor item plus its check-in history so the detail
/// page never re-reads storage itself.
/// </summary>
public sealed record AccountItemDetailArgs(
    string ItemId,
    string ItemName,
    string ItemTypeLabel,
    bool IsDebt,
    double CurrentBalance,
    string FreshnessText,
    bool IsOverdue,
    IReadOnlyList<AccountItemHistoryPoint> History,
    string BalanceLabel = "Current balance");
