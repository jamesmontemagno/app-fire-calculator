using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MyFireNumber.Core.Calculations;

namespace MyFireNumber.Core.Exports;

public static class InterestCalculatorWorkbook
{
    public static void Create(string filePath, InterestCalculatorDraft draft, InterestCalculatorResult result, DateTimeOffset generatedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(result);

        Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        using var document = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook);
        var workbookPart = document.AddWorkbookPart();
        workbookPart.Workbook = new Workbook();
        WorkbookStyles.Apply(workbookPart);
        var sheets = workbookPart.Workbook.AppendChild(new Sheets());

        AddWorksheet(workbookPart, sheets, "Inputs", 1,
        [
            new Row(Text("A1", "Interest Calculator Inputs")),
            new Row(Text("A2", "Generated UTC"), Text("B2", generatedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture))),
            new Row(Text("A4", "Input"), Text("B4", "Value")),
            new Row(Text("A5", "Starting balance"), Number("B5", draft.StartingBalance, WorkbookStyles.CurrencyStyleIndex)),
            new Row(Text("A6", "Monthly contribution"), Number("B6", draft.MonthlyContribution, WorkbookStyles.CurrencyStyleIndex)),
            new Row(Text("A7", "Annual interest rate"), Number("B7", draft.AnnualInterestRate, WorkbookStyles.PercentageStyleIndex)),
            new Row(Text("A8", "Years"), Number("B8", draft.Years, IntegerFormat.Grouped))
        ], 30, 20);

        AddWorksheet(workbookPart, sheets, "Results", 2,
        [
            new Row(Text("A1", "Interest Calculator Results")),
            new Row(Text("A4", "Result"), Text("B4", "Value")),
            new Row(Text("A5", "Ending balance"), Number("B5", result.EndingBalance, WorkbookStyles.CurrencyStyleIndex)),
            new Row(Text("A6", "Total contributions"), Number("B6", result.TotalContributions, WorkbookStyles.CurrencyStyleIndex)),
            new Row(Text("A7", "Interest earned"), Number("B7", result.InterestEarned, WorkbookStyles.CurrencyStyleIndex)),
            new Row(Text("A8", "Effective annual yield"), Number("B8", result.EffectiveAnnualYield, WorkbookStyles.PercentageStyleIndex))
        ], 28, 20);

        var projectionRows = new List<Row>
        {
            new(Text("A1", "Year"), Text("B1", "Balance"), Text("C1", "Total Contributions"), Text("D1", "Interest Earned"))
        };
        projectionRows.AddRange(result.Projections.Select((point, index) => new Row(
            Number($"A{index + 2}", point.Year, IntegerFormat.Grouped),
            Number($"B{index + 2}", point.Balance, WorkbookStyles.CurrencyStyleIndex),
            Number($"C{index + 2}", point.TotalContributions, WorkbookStyles.CurrencyStyleIndex),
            Number($"D{index + 2}", point.InterestEarned, WorkbookStyles.CurrencyStyleIndex))));
        AddWorksheet(workbookPart, sheets, "Projection", 3, projectionRows, 14, 20, 22, 20);
        workbookPart.Workbook.Save();
    }

    private static void AddWorksheet(WorkbookPart workbookPart, Sheets sheets, string name, uint sheetId, IEnumerable<Row> rows, params double[] widths)
    {
        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        worksheetPart.Worksheet = new Worksheet(
            new Columns(widths.Select((width, index) => new Column { Min = (uint)index + 1, Max = (uint)index + 1, Width = width, CustomWidth = true })),
            new SheetData(rows));
        sheets.Append(new Sheet { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = sheetId, Name = name });
    }

    private static Cell Text(string reference, string value) =>
        new() { CellReference = reference, DataType = CellValues.InlineString, InlineString = new InlineString(new Text(value)) };

    private static Cell Number(string reference, double value, uint styleIndex) =>
        new() { CellReference = reference, StyleIndex = styleIndex, CellValue = new CellValue(value.ToString(CultureInfo.InvariantCulture)) };

    [Obsolete("An int cell must use IntegerFormat, never a raw style index.", error: true)]
    private static Cell Number(string reference, int value, uint styleIndex) =>
        throw new InvalidOperationException();

    private static Cell Number(string reference, int value, IntegerFormat format) =>
        Number(reference, (double)value, WorkbookStyles.StyleIndexFor(format));
}
