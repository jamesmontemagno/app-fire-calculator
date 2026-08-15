using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace MyFireNumber.Core.Exports;

/// <summary>
/// The single source of truth for the number formats and style indexes every workbook exporter uses.
/// Keeping the stylesheet here (rather than duplicated per exporter) is deliberate: the duplicated
/// stylesheet is exactly what let the exporters drift and ship without an integer format, so a whole
/// number such as a calendar year rendered as "2026.0". See issue #69.
/// </summary>
public static class WorkbookStyles
{
    /// <summary><c>$#,##0</c> — currency, no cents.</summary>
    public const uint CurrencyStyleIndex = 1;

    /// <summary><c>0.0%</c> — percentage with one decimal.</summary>
    public const uint PercentageStyleIndex = 2;

    /// <summary><c>0.0</c> — a genuinely fractional value (a <c>double</c> such as years-to-FIRE).</summary>
    public const uint DecimalStyleIndex = 3;

    /// <summary>
    /// <c>#,##0</c> — a whole-number <em>magnitude</em> where a thousands separator can help a reader
    /// (a month or year count). Sourced from an <c>int</c>. Using <see cref="DecimalStyleIndex"/> here
    /// is the #69 defect.
    /// </summary>
    public const uint IntegerStyleIndex = 4;

    /// <summary>
    /// <c>0</c> — a whole-number <em>identifier or small count</em> where grouping is meaningless: a
    /// calendar year or an age. A separator would turn the year 2026 into "2,026", so these must not
    /// use <see cref="IntegerStyleIndex"/>. Also sourced from an <c>int</c>.
    /// </summary>
    public const uint PlainIntegerStyleIndex = 5;

    /// <summary>
    /// Builds and attaches the shared stylesheet to the workbook. The <see cref="CellFormats"/> order
    /// must match the style-index constants above.
    /// </summary>
    public static void Apply(WorkbookPart workbookPart)
    {
        var stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
        stylesPart.Stylesheet = new Stylesheet(
            new NumberingFormats(
                new NumberingFormat { NumberFormatId = 164U, FormatCode = "$#,##0" },
                new NumberingFormat { NumberFormatId = 165U, FormatCode = "0.0%" },
                new NumberingFormat { NumberFormatId = 166U, FormatCode = "0.0" },
                new NumberingFormat { NumberFormatId = 167U, FormatCode = "#,##0" },
                new NumberingFormat { NumberFormatId = 168U, FormatCode = "0" }),
            new Fonts(new Font()),
            new Fills(
                new Fill(new PatternFill { PatternType = PatternValues.None }),
                new Fill(new PatternFill { PatternType = PatternValues.Gray125 })),
            new Borders(new Border()),
            new CellStyleFormats(new CellFormat()),
            new CellFormats(
                new CellFormat(),
                new CellFormat { NumberFormatId = 164U, ApplyNumberFormat = true },
                new CellFormat { NumberFormatId = 165U, ApplyNumberFormat = true },
                new CellFormat { NumberFormatId = 166U, ApplyNumberFormat = true },
                new CellFormat { NumberFormatId = 167U, ApplyNumberFormat = true },
                new CellFormat { NumberFormatId = 168U, ApplyNumberFormat = true }));
    }

    /// <summary>
    /// Resolves the style index for an integer cell. The only inputs are the two integer formats, so an
    /// <c>int</c> routed through this helper can never land on <see cref="DecimalStyleIndex"/>.
    ///
    /// On its own the enum makes the correct call the easy one but not the only one: because <c>int</c>
    /// widens implicitly to <c>double</c>, a raw <c>CreateNumberCell(reference, someInt, DecimalStyleIndex)</c>
    /// would otherwise bind to the <c>(double, uint)</c> overload and silently reintroduce #69. Each
    /// exporter therefore also declares an <c>[Obsolete(error: true)]</c> <c>(int, uint)</c> overload whose
    /// sole purpose is to be un-callable, turning that shape into a compile error. Enum plus poison
    /// overload together make the defect genuinely unrepresentable at compile time, not merely unlikely.
    /// </summary>
    public static uint StyleIndexFor(IntegerFormat format) => format switch
    {
        IntegerFormat.Plain => PlainIntegerStyleIndex,
        IntegerFormat.Grouped => IntegerStyleIndex,
        _ => throw new ArgumentOutOfRangeException(nameof(format))
    };
}

/// <summary>
/// How a whole-number (<c>int</c>) cell should render. There is deliberately no "decimal" member: an
/// <c>int</c> must never carry a fractional format (issue #69), and forcing every integer cell through
/// this enum makes that mistake impossible to express at a call site.
/// </summary>
public enum IntegerFormat
{
    /// <summary><c>0</c> — an identifier or small count (a calendar year or an age); no separator.</summary>
    Plain,

    /// <summary><c>#,##0</c> — a magnitude where a thousands separator can help (a month or year count).</summary>
    Grouped
}
