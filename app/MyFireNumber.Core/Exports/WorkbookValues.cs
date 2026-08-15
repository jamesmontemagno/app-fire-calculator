namespace MyFireNumber.Core.Exports;

/// <summary>
/// Shared text the workbook exporters write when a modelled value has no finite answer.
/// </summary>
public static class WorkbookValues
{
    /// <summary>
    /// Substituted for any non-finite number. Writing <see cref="double.PositiveInfinity"/> straight
    /// into a numeric cell serializes it as "Infinity", which Excel cannot read back as a number, so
    /// the value is emitted as text instead. This mirrors the on-screen wording in the apps.
    /// </summary>
    public const string Unreachable = "Not reachable";
}
