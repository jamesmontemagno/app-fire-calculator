using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MyFireNumber.Core.Calculations;
using System.Globalization;

namespace MyFireNumber.Core.Exports;

public static class HealthcareGapWorkbook
{
    private const uint CurrencyStyleIndex = 1;
    private const uint PercentageStyleIndex = 2;
    private const uint DecimalStyleIndex = 3;

    public static void Create(string filePath, HealthcareGapDraft draft, HealthcareGapResult result, DateTimeOffset generatedAt)
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
        AddProjectionSheet(workbookPart, sheets, result.YearlyBreakdown);
        workbookPart.Workbook.Save();
    }

    private static void AddInputsSheet(WorkbookPart workbookPart, Sheets sheets, HealthcareGapDraft draft, DateTimeOffset generatedAt)
    {
        var rows = new List<Row>
        {
            new(CreateTextCell("A1", "Healthcare Gap Inputs")),
            new(CreateTextCell("A2", "Generated UTC"), CreateTextCell("B2", generatedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture))),
            new(CreateTextCell("A4", "Input"), CreateTextCell("B4", "Value")),
            new(CreateTextCell("A5", "Current age"), CreateNumberCell("B5", draft.CurrentAge, DecimalStyleIndex)),
            new(CreateTextCell("A6", "Early retirement age"), CreateNumberCell("B6", draft.EarlyRetirementAge, DecimalStyleIndex)),
            new(CreateTextCell("A7", "Medicare age"), CreateNumberCell("B7", draft.MedicareAge, DecimalStyleIndex)),
            new(CreateTextCell("A8", "Monthly premium"), CreateNumberCell("B8", draft.MonthlyPremium, CurrencyStyleIndex)),
            new(CreateTextCell("A9", "Annual deductible"), CreateNumberCell("B9", draft.AnnualDeductible, CurrencyStyleIndex)),
            new(CreateTextCell("A10", "Annual out-of-pocket"), CreateNumberCell("B10", draft.AnnualOutOfPocket, CurrencyStyleIndex)),
            new(CreateTextCell("A11", "Inflation rate"), CreateNumberCell("B11", draft.InflationRate, PercentageStyleIndex))
        };
        AddWorksheet(workbookPart, sheets, "Inputs", 1, rows, 30, 20);
    }

    private static void AddResultsSheet(WorkbookPart workbookPart, Sheets sheets, HealthcareGapResult result, DateTimeOffset generatedAt)
    {
        var rows = new List<Row>
        {
            new(CreateTextCell("A1", "Healthcare Gap Results")),
            new(CreateTextCell("A2", "Generated UTC"), CreateTextCell("B2", generatedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture))),
            new(CreateTextCell("A4", "Result"), CreateTextCell("B4", "Value")),
            new(CreateTextCell("A5", "Coverage gap years"), CreateFormulaCell("B5", "MAX(0,Inputs!B7-Inputs!B6)", DecimalStyleIndex)),
            new(CreateTextCell("A6", "First-year annual cost"), CreateFormulaCell("B6", "Inputs!B8*12+Inputs!B9+Inputs!B10", CurrencyStyleIndex)),
            new(CreateTextCell("A7", "Total projected cost"), CreateNumberCell("B7", result.TotalCost, CurrencyStyleIndex)),
            new(CreateTextCell("A8", "Average annual cost"), CreateNumberCell("B8", result.AverageAnnualCost, CurrencyStyleIndex))
        };
        AddWorksheet(workbookPart, sheets, "Results", 2, rows, 34, 22);
    }

    private static void AddProjectionSheet(WorkbookPart workbookPart, Sheets sheets, IReadOnlyList<HealthcareYear> years)
    {
        var rows = new List<Row>
        {
            new(CreateTextCell("A1", "Age"), CreateTextCell("B1", "Year"), CreateTextCell("C1", "Total cost"), CreateTextCell("D1", "Premium"), CreateTextCell("E1", "Deductible"), CreateTextCell("F1", "Out-of-pocket"))
        };
        for (var index = 0; index < years.Count; index++)
        {
            var rowNumber = index + 2;
            var year = years[index];
            rows.Add(new Row(
                CreateNumberCell($"A{rowNumber}", year.Age, DecimalStyleIndex),
                CreateNumberCell($"B{rowNumber}", year.Year, DecimalStyleIndex),
                CreateFormulaCell($"C{rowNumber}", $"D{rowNumber}+E{rowNumber}+F{rowNumber}", CurrencyStyleIndex),
                CreateFormulaCell($"D{rowNumber}", $"Inputs!$B$8*12*((1+Inputs!$B$11)^(A{rowNumber}-Inputs!$B$6))", CurrencyStyleIndex),
                CreateFormulaCell($"E{rowNumber}", $"Inputs!$B$9*((1+Inputs!$B$11)^(A{rowNumber}-Inputs!$B$6))", CurrencyStyleIndex),
                CreateFormulaCell($"F{rowNumber}", $"Inputs!$B$10*((1+Inputs!$B$11)^(A{rowNumber}-Inputs!$B$6))", CurrencyStyleIndex)));
        }
        AddWorksheet(workbookPart, sheets, "Annual Cost", 3, rows, 12, 14, 20, 20, 20, 20);
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

    private static void AddStyles(WorkbookPart workbookPart)
    {
        var stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
        stylesPart.Stylesheet = new Stylesheet(
            new NumberingFormats(new NumberingFormat { NumberFormatId = 164U, FormatCode = "$#,##0" }, new NumberingFormat { NumberFormatId = 165U, FormatCode = "0.0%" }, new NumberingFormat { NumberFormatId = 166U, FormatCode = "0.0" }),
            new Fonts(new Font()),
            new Fills(new Fill(new PatternFill { PatternType = PatternValues.None }), new Fill(new PatternFill { PatternType = PatternValues.Gray125 })),
            new Borders(new Border()),
            new CellStyleFormats(new CellFormat()),
            new CellFormats(new CellFormat(), new CellFormat { NumberFormatId = 164U, ApplyNumberFormat = true }, new CellFormat { NumberFormatId = 165U, ApplyNumberFormat = true }, new CellFormat { NumberFormatId = 166U, ApplyNumberFormat = true }));
    }
}