namespace MyFireNumber.ViewModels;

/// <summary>
/// Non-generic surface used by calculator pages so a single page base class can
/// drive load and draft-flush lifecycle for any calculator view model.
/// </summary>
public interface ICalculatorViewModel
{
    Task LoadAsync(string? planId = null, bool returnHomeAfterSave = false);

    Task FlushPendingDraftAsync();
}
