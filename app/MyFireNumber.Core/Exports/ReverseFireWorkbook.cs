using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MyFireNumber.Core.Calculations;
using System.Globalization;

namespace MyFireNumber.Core.Exports;

public static class ReverseFireWorkbook
{
    private const uint CurrencyStyleIndex = WorkbookStyles.CurrencyStyleIndex;
    private const uint PercentageStyleIndex = WorkbookStyles.PercentageStyleIndex;
    private const uint DecimalStyleIndex = WorkbookStyles.DecimalStyleIndex;
    private const uint IntegerStyleIndex = WorkbookStyles.IntegerStyleIndex;
    private const uint PlainIntegerStyleIndex = WorkbookStyles.PlainIntegerStyleIndex;

    public static void Create(string filePath, ReverseFireDraft draft, ReverseFireResult result, DateTimeOffset generatedAt)
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
        AddProjectionSheet(workbookPart, sheets, result.Projections);
        workbookPart.Workbook.Save();
    }

    private static void AddInputsSheet(WorkbookPart workbookPart, Sheets sheets, ReverseFireDraft draft, DateTimeOffset generatedAt)
    {
        var rows = new List<Row>
        {
            new(CreateTextCell("A1", "Reverse FIRE Inputs")),
            new(CreateTextCell("A2", "Generated UTC"), CreateTextCell("B2", generatedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture))),
            new(CreateTextCell("A4", "Input"), CreateTextCell("B4", "Value"))
        };
        var inputs = new (string Label, double Value, uint Style)[]
        {
            ("Current age", draft.CurrentAge, PlainIntegerStyleIndex),
            ("Target FIRE age", draft.TargetRetirementAge, PlainIntegerStyleIndex),
            ("Current savings", draft.CurrentSavings, CurrencyStyleIndex),
            ("Annual retirement spending (today's dollars)", draft.AnnualExpenses, CurrencyStyleIndex),
            ("Expected return", draft.ExpectedReturn, PercentageStyleIndex),
            ("Inflation rate", draft.InflationRate, PercentageStyleIndex),
            ("Safe withdrawal rate", draft.WithdrawalRate, PercentageStyleIndex)
        };
        for (var index = 0; index < inputs.Length; index++)
        {
            var rowNumber = index + 5;
            var input = inputs[index];
            rows.Add(new Row(CreateTextCell($"A{rowNumber}", input.Label), CreateNumberCell($"B{rowNumber}", input.Value, input.Style)));
        }

        AddWorksheet(workbookPart, sheets, "Inputs", 1, rows, 32, 20);
    }

    private static void AddResultsSheet(WorkbookPart workbookPart, Sheets sheets, ReverseFireResult result, DateTimeOffset generatedAt)
    {
        var rows = new List<Row>
        {
            new(CreateTextCell("A1", "Reverse FIRE Results")),
            new(CreateTextCell("A2", "Generated UTC"), CreateTextCell("B2", generatedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture))),
            new(CreateTextCell("A4", "Result"), CreateTextCell("B4", "Value")),
            new(CreateTextCell("A5", "FIRE Number"), CreateFormulaCell("B5", "Inputs!B8/Inputs!B11", CurrencyStyleIndex)),
            new(CreateTextCell("A6", "Years to FIRE"), CreateFormulaCell("B6", "Inputs!B6-Inputs!B5", IntegerStyleIndex)),
            new(CreateTextCell("A7", "Required annual savings"), CreateNumberCell("B7", result.RequiredAnnualSavings, CurrencyStyleIndex)),
            new(CreateTextCell("A8", "Required monthly savings"), CreateNumberCell("B8", result.RequiredMonthlySavings, CurrencyStyleIndex)),
            new(CreateTextCell("A9", "Current savings will grow to"), CreateNumberCell("B9", result.CurrentWillGrowTo, CurrencyStyleIndex))
        };

        AddWorksheet(workbookPart, sheets, "Results", 2, rows, 34, 20);
    }

    private static void AddProjectionSheet(WorkbookPart workbookPart, Sheets sheets, IReadOnlyList<ProjectionPoint> projections)
    {
        var rows = new List<Row>
        {
            new(CreateTextCell("A1", "Age"), CreateTextCell("B1", "Year"), CreateTextCell("C1", "Portfolio"), CreateTextCell("D1", "Annual Savings"), CreateTextCell("E1", "Inflation-Adjusted Portfolio"))
        };
        for (var index = 0; index < projections.Count; index++)
        {
            var rowNumber = index + 2;
            var point = projections[index];
            if (index == 0)
            {
                rows.Add(new Row(
                    CreateNumberCell($"A{rowNumber}", point.Age, DecimalStyleIndex),
                    CreateNumberCell($"B{rowNumber}", point.Year, PlainIntegerStyleIndex),
                    CreateNumberCell($"C{rowNumber}", point.Portfolio, CurrencyStyleIndex),
                    CreateNumberCell($"D{rowNumber}", 0, CurrencyStyleIndex),
                    CreateNumberCell($"E{rowNumber}", point.InflationAdjusted, CurrencyStyleIndex)));
                continue;
            }

            var previousRowNumber = rowNumber - 1;
            rows.Add(new Row(
                CreateNumberCell($"A{rowNumber}", point.Age, DecimalStyleIndex),
                CreateNumberCell($"B{rowNumber}", point.Year, PlainIntegerStyleIndex),
                CreateFormulaCell($"C{rowNumber}", $"C{previousRowNumber}*(1+Inputs!$B$9)+D{rowNumber}", CurrencyStyleIndex),
                CreateNumberCell($"D{rowNumber}", point.Contributions, CurrencyStyleIndex),
                CreateFormulaCell($"E{rowNumber}", $"C{rowNumber}/((1+Inputs!$B$10)^(A{rowNumber}-$A$2))", CurrencyStyleIndex)));
        }

        AddWorksheet(workbookPart, sheets, "Projection", 3, rows, 14, 18, 20, 22, 30);
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
    private static Cell CreateNumberCell(string reference, double value, uint styleIndex) => double.IsFinite(value)
        ? new() { CellReference = reference, StyleIndex = styleIndex, CellValue = new CellValue(value.ToString(CultureInfo.InvariantCulture)) }
        : CreateTextCell(reference, WorkbookValues.Unreachable);

    private static Cell CreateFormulaCell(string reference, string formula, uint styleIndex) => new() { CellReference = reference, StyleIndex = styleIndex, CellFormula = new CellFormula(formula) };

    private static void AddStyles(WorkbookPart workbookPart) => WorkbookStyles.Apply(workbookPart);
}