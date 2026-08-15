using System;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MyFireNumber.Core.Calculations;
using System.Globalization;

namespace MyFireNumber.Core.Exports;

public static class DebtPayoffWorkbook
{
    private const uint CurrencyStyleIndex = WorkbookStyles.CurrencyStyleIndex;
    private const uint PercentageStyleIndex = WorkbookStyles.PercentageStyleIndex;
    private const uint DecimalStyleIndex = WorkbookStyles.DecimalStyleIndex;
    private const uint IntegerStyleIndex = WorkbookStyles.IntegerStyleIndex;
    private const uint PlainIntegerStyleIndex = WorkbookStyles.PlainIntegerStyleIndex;

    public static void Create(string filePath, DebtPayoffDraft draft, DebtPayoffResult result, DateTimeOffset generatedAt)
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
        AddStyles(workbookPart);
        var sheets = workbookPart.Workbook.AppendChild(new Sheets());
        AddInputsSheet(workbookPart, sheets, draft, generatedAt);
        AddResultsSheet(workbookPart, sheets, result, generatedAt);
        AddDebtSheet(workbookPart, sheets, draft.Debts);
        AddProjectionSheet(workbookPart, sheets, result.Projections);
        workbookPart.Workbook.Save();
    }

    private static void AddInputsSheet(WorkbookPart workbookPart, Sheets sheets, DebtPayoffDraft draft, DateTimeOffset generatedAt)
    {
        AddWorksheet(workbookPart, sheets, "Inputs", 1,
        [
            new Row(CreateTextCell("A1", "Debt Payoff Inputs")),
            new Row(CreateTextCell("A2", "Generated UTC"), CreateTextCell("B2", generatedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture))),
            new Row(CreateTextCell("A4", "Input"), CreateTextCell("B4", "Value")),
            new Row(CreateTextCell("A5", "Mode"), CreateTextCell("B5", draft.Mode.ToString())),
            new Row(CreateTextCell("A6", "Strategy"), CreateTextCell("B6", draft.Strategy.ToString())),
            new Row(CreateTextCell("A7", "Monthly budget"), CreateNumberCell("B7", draft.MonthlyBudget, CurrencyStyleIndex)),
            new Row(CreateTextCell("A8", "Extra payment"), CreateNumberCell("B8", draft.ExtraPayment, CurrencyStyleIndex)),
            new Row(CreateTextCell("A9", "Target months"), CreateNumberCell("B9", draft.TargetMonths, IntegerFormat.Grouped))
        ], 28, 20);
    }

    private static void AddResultsSheet(WorkbookPart workbookPart, Sheets sheets, DebtPayoffResult result, DateTimeOffset generatedAt)
    {
        AddWorksheet(workbookPart, sheets, "Results", 2,
        [
            new Row(CreateTextCell("A1", "Debt Payoff Results")),
            new Row(CreateTextCell("A2", "Generated UTC"), CreateTextCell("B2", generatedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture))),
            new Row(CreateTextCell("A4", "Result"), CreateTextCell("B4", "Value")),
            new Row(CreateTextCell("A5", "Total months"), CreateNumberCell("B5", result.TotalMonths, IntegerFormat.Grouped)),
            new Row(CreateTextCell("A6", "Total interest"), CreateNumberCell("B6", result.TotalInterest, CurrencyStyleIndex)),
            new Row(CreateTextCell("A7", "Total principal"), CreateNumberCell("B7", result.TotalPrincipal, CurrencyStyleIndex)),
            new Row(CreateTextCell("A8", "Monthly payment"), CreateNumberCell("B8", result.MonthlyPayment, CurrencyStyleIndex)),
            new Row(CreateTextCell("A9", "Payoff order"), CreateTextCell("B9", string.Join(", ", result.PayoffOrder)))
        ], 24, 32);
    }

    private static void AddDebtSheet(WorkbookPart workbookPart, Sheets sheets, IReadOnlyList<DebtItem> debts)
    {
        var rows = new List<Row>
        {
            new(CreateTextCell("A1", "Debt"), CreateTextCell("B1", "Balance"), CreateTextCell("C1", "Rate"), CreateTextCell("D1", "Minimum payment"))
        };
        for (var index = 0; index < debts.Count; index++)
        {
            var row = index + 2;
            var debt = debts[index];
            rows.Add(new Row(CreateTextCell($"A{row}", debt.Name), CreateNumberCell($"B{row}", debt.Balance, CurrencyStyleIndex), CreateNumberCell($"C{row}", debt.Rate, PercentageStyleIndex), CreateNumberCell($"D{row}", debt.MinimumPayment, CurrencyStyleIndex)));
        }
        AddWorksheet(workbookPart, sheets, "Debt List", 3, rows, 28, 18, 14, 20);
    }

    private static void AddProjectionSheet(WorkbookPart workbookPart, Sheets sheets, IReadOnlyList<DebtPayoffMonth> projections)
    {
        var rows = new List<Row>
        {
            new(CreateTextCell("A1", "Month"), CreateTextCell("B1", "Total balance"), CreateTextCell("C1", "Principal paid"), CreateTextCell("D1", "Interest paid"), CreateTextCell("E1", "Cumulative interest"))
        };
        foreach (var point in projections)
        {
            var row = point.Month + 1;
            rows.Add(new Row(CreateNumberCell($"A{row}", point.Month, IntegerFormat.Grouped), CreateNumberCell($"B{row}", point.TotalBalance, CurrencyStyleIndex), CreateNumberCell($"C{row}", point.PrincipalPaid, CurrencyStyleIndex), CreateNumberCell($"D{row}", point.InterestPaid, CurrencyStyleIndex), CreateNumberCell($"E{row}", point.CumulativeInterest, CurrencyStyleIndex)));
        }
        AddWorksheet(workbookPart, sheets, "Payoff Projection", 4, rows, 12, 20, 20, 20, 22);
    }

    private static void AddWorksheet(WorkbookPart workbookPart, Sheets sheets, string name, uint sheetId, IEnumerable<Row> rows, params double[] widths)
    {
        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        var columns = new Columns(widths.Select((width, index) => new Column { Min = (uint)index + 1, Max = (uint)index + 1, Width = width, CustomWidth = true }));
        worksheetPart.Worksheet = new Worksheet(columns, new SheetData(rows));
        sheets.Append(new Sheet { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = sheetId, Name = name });
    }

    private static Cell CreateTextCell(string reference, string value) => new() { CellReference = reference, DataType = CellValues.InlineString, InlineString = new InlineString(new Text(value)) };
    // A non-finite result is a legitimate outcome (an unreachable target), but "Infinity" inside a
    // numeric cell is not a number Excel can read. Emit the same wording the apps show on screen.
    // This overload exists solely to be un-callable. Without it, CreateNumberCell("B5", someInt,
    // DecimalStyleIndex) would bind to the (double, uint) overload via int->double widening and
    // silently reintroduce #69. Making the (int, uint) shape a compile error (error: true) forces
    // every integer cell through the IntegerFormat overload, so an int can never carry a fractional
    // format. This is what makes the guarantee hold at compile time rather than merely by convention.
    [Obsolete("An int cell must use IntegerFormat, never a raw style index (issue #69).", error: true)]
    private static Cell CreateNumberCell(string reference, int value, uint styleIndex) =>
        throw new InvalidOperationException();

    // Integer cells route through IntegerFormat, never a raw style index, so an int can never be
    // written with the fractional DecimalStyleIndex (issue #69). Overload resolution prefers this
    // exact-type method over widening the int to the double overload.
    private static Cell CreateNumberCell(string reference, int value, IntegerFormat format) =>
        CreateNumberCell(reference, (double)value, WorkbookStyles.StyleIndexFor(format));

    private static Cell CreateNumberCell(string reference, double value, uint styleIndex) => double.IsFinite(value)
        ? new() { CellReference = reference, StyleIndex = styleIndex, CellValue = new CellValue(value.ToString(CultureInfo.InvariantCulture)) }
        : CreateTextCell(reference, WorkbookValues.Unreachable);

    private static void AddStyles(WorkbookPart workbookPart) => WorkbookStyles.Apply(workbookPart);
}