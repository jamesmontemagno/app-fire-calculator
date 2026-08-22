using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MyFireNumber.Core.Calculations;

namespace MyFireNumber.Core.Exports;

public static class RothConversionWorkbook
{
    public static void Create(
        string filePath,
        RothConversionDraft draft,
        RothConversionResult result,
        DateTimeOffset generatedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(result);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");
        if (File.Exists(filePath)) File.Delete(filePath);

        using var document = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook);
        var workbookPart = document.AddWorkbookPart();
        workbookPart.Workbook = new Workbook();
        WorkbookStyles.Apply(workbookPart);
        var sheets = workbookPart.Workbook.AppendChild(new Sheets());
        AddInputs(workbookPart, sheets, draft, generatedAt);
        AddResults(workbookPart, sheets, result);
        AddProjection(workbookPart, sheets, result.Projections);
        workbookPart.Workbook.Save();
    }

    private static void AddInputs(
        WorkbookPart workbookPart,
        Sheets sheets,
        RothConversionDraft draft,
        DateTimeOffset generatedAt)
    {
        var rows = new List<Row>
        {
            RowOf(Text("A1", "Roth Conversion Strategy Inputs")),
            RowOf(Text("A2", "Generated UTC"), Text("B2", generatedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture))),
            RowOf(Text("A4", "Input"), Text("B4", "Value")),
            RowOf(Text("A5", "Current age"), Number("B5", draft.CurrentAge, IntegerFormat.Plain)),
            RowOf(Text("A6", "First conversion year"), Number("B6", draft.StartYear, IntegerFormat.Plain)),
            RowOf(Text("A7", "Traditional balance"), Number("B7", draft.TraditionalBalance, WorkbookStyles.CurrencyStyleIndex)),
            RowOf(Text("A8", "Existing Roth balance"), Number("B8", draft.RothBalance, WorkbookStyles.CurrencyStyleIndex)),
            RowOf(Text("A9", "Planned annual conversion"), Number("B9", draft.AnnualConversion, WorkbookStyles.CurrencyStyleIndex)),
            RowOf(Text("A10", "Conversion years"), Number("B10", draft.ConversionYears, IntegerFormat.Plain)),
            RowOf(Text("A11", "Expected return"), Number("B11", draft.ExpectedReturn, WorkbookStyles.PercentageStyleIndex)),
            RowOf(Text("A12", "Estimated conversion tax rate"), Number("B12", draft.EstimatedTaxRate, WorkbookStyles.PercentageStyleIndex))
        };
        AddSheet(workbookPart, sheets, "Inputs", 1, rows, 34, 24);
    }

    private static void AddResults(WorkbookPart workbookPart, Sheets sheets, RothConversionResult result)
    {
        var rows = new List<Row>
        {
            RowOf(Text("A1", "Roth Conversion Strategy Results")),
            RowOf(Text("A3", "Result"), Text("B3", "Value")),
            RowOf(Text("A4", "Total planned conversions"), Number("B4", result.TotalConverted, WorkbookStyles.CurrencyStyleIndex)),
            RowOf(Text("A5", "Total estimated conversion taxes"), Number("B5", result.TotalEstimatedTaxes, WorkbookStyles.CurrencyStyleIndex)),
            RowOf(Text("A6", "First converted principal accessible"), result.FirstAccessibleYear is int year
                ? Number("B6", year, IntegerFormat.Plain)
                : Text("B6", "No conversion available")),
            RowOf(Text("A7", "Ending traditional balance"), Number("B7", result.EndingTraditionalBalance, WorkbookStyles.CurrencyStyleIndex)),
            RowOf(Text("A8", "Ending Roth balance"), Number("B8", result.EndingRothBalance, WorkbookStyles.CurrencyStyleIndex)),
            RowOf(
                Text("A10", "Important"),
                Text("B10", "Educational estimate only. Taxable income, conversion taxes, Roth ordering rules, and the five-tax-year rules depend on individual circumstances. Confirm a conversion plan with a qualified tax professional."))
        };
        AddSheet(workbookPart, sheets, "Results", 2, rows, 40, 100);
    }

    private static void AddProjection(
        WorkbookPart workbookPart,
        Sheets sheets,
        IReadOnlyList<RothConversionProjectionPoint> points)
    {
        var rows = new List<Row>
        {
            RowOf(
                Text("A1", "Plan year"),
                Text("B1", "Calendar year"),
                Text("C1", "Age"),
                Text("D1", "Starting traditional"),
                Text("E1", "Conversion"),
                Text("F1", "Estimated taxes"),
                Text("G1", "Ending traditional"),
                Text("H1", "Ending Roth"),
                Text("I1", "Newly accessible principal"),
                Text("J1", "Cumulative accessible principal"))
        };
        foreach (var point in points)
        {
            var row = point.YearNumber + 1;
            rows.Add(RowOf(
                Number($"A{row}", point.YearNumber, IntegerFormat.Plain),
                Number($"B{row}", point.CalendarYear, IntegerFormat.Plain),
                Number($"C{row}", point.Age, IntegerFormat.Plain),
                Number($"D{row}", point.StartingTraditionalBalance, WorkbookStyles.CurrencyStyleIndex),
                Number($"E{row}", point.Conversion, WorkbookStyles.CurrencyStyleIndex),
                Number($"F{row}", point.EstimatedTaxes, WorkbookStyles.CurrencyStyleIndex),
                Number($"G{row}", point.EndingTraditionalBalance, WorkbookStyles.CurrencyStyleIndex),
                Number($"H{row}", point.EndingRothBalance, WorkbookStyles.CurrencyStyleIndex),
                Number($"I{row}", point.NewlyAccessiblePrincipal, WorkbookStyles.CurrencyStyleIndex),
                Number($"J{row}", point.CumulativeAccessiblePrincipal, WorkbookStyles.CurrencyStyleIndex)));
        }
        AddSheet(workbookPart, sheets, "Projection", 3, rows, 12, 15, 9, 22, 18, 18, 20, 18, 24, 28);
    }

    private static void AddSheet(
        WorkbookPart workbookPart,
        Sheets sheets,
        string name,
        uint id,
        IEnumerable<Row> rows,
        params double[] widths)
    {
        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        var columns = new Columns(widths.Select((width, index) => new Column
        {
            Min = (uint)index + 1,
            Max = (uint)index + 1,
            Width = width,
            CustomWidth = true
        }));
        worksheetPart.Worksheet = new Worksheet(columns, new SheetData(rows));
        sheets.Append(new Sheet { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = id, Name = name });
    }

    private static Row RowOf(params Cell[] cells) => new(cells);

    private static Cell Text(string reference, string value) => new()
    {
        CellReference = reference,
        DataType = CellValues.InlineString,
        InlineString = new InlineString(new Text(value))
    };

    private static Cell Number(string reference, double value, uint style) => new()
    {
        CellReference = reference,
        StyleIndex = style,
        CellValue = new CellValue(value.ToString(CultureInfo.InvariantCulture))
    };

    [Obsolete("An int cell must use IntegerFormat, never a raw style index.", error: true)]
    private static Cell Number(string reference, int value, uint style) =>
        throw new InvalidOperationException();

    private static Cell Number(string reference, int value, IntegerFormat format) =>
        Number(reference, (double)value, WorkbookStyles.StyleIndexFor(format));
}
