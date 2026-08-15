namespace MyFireNumber.ViewModels;

/// <summary>
/// Non-generic surface used by calculator pages so a single page base class can
/// drive load and draft-flush lifecycle for any calculator view model.
/// </summary>
public interface ICalculatorViewModel
{
    /// <summary>
    /// Catalog identifier, e.g. <c>withdrawal-rate</c>. Surfaced here because Standard, Lean, and
    /// Fat FIRE share one page type, so the page cannot infer which calculator it is showing from
    /// its own type — only the selected view model knows.
    /// </summary>
    string CalculatorId { get; }

    Task LoadAsync(string? planId = null, bool returnHomeAfterSave = false);

    Task FlushPendingDraftAsync();
}
