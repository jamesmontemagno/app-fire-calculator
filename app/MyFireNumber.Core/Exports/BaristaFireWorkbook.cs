using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MyFireNumber.Core.Calculations;
using System.Globalization;

namespace MyFireNumber.Core.Exports;

public static class BaristaFireWorkbook
{
    private const uint CurrencyStyleIndex = 1;
    private const uint PercentageStyleIndex = 2;
    private const uint DecimalStyleIndex = 3;

    public static void Create(string filePath, BaristaFireDraft draft, BaristaFireResult result, DateTimeOffset generatedAt)
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

    private static void AddInputsSheet(WorkbookPart workbookPart, Sheets sheets, BaristaFireDraft draft, DateTimeOffset generatedAt)
    {
        var rows = new List<Row>
        {
            new(CreateTextCell("A1", "Barista FIRE Inputs")),
            new(CreateTextCell("A2", "Generated UTC"), CreateTextCell("B2", generatedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture))),
            new(CreateTextCell("A4", "Input"), CreateTextCell("B4", "Value"))
        };
        var inputs = new (string Label, double Value, uint Style)[]
        {
            ("Current age", draft.CurrentAge, DecimalStyleIndex),
            ("Current savings", draft.CurrentSavings, CurrencyStyleIndex),
            ("Annual contribution", draft.AnnualContribution, CurrencyStyleIndex),
            ("Annual retirement spending (today's dollars)", draft.AnnualExpenses, CurrencyStyleIndex),
            ("Part-time take-home income (after tax)", draft.PartTimeAnnualIncome, CurrencyStyleIndex),
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

    private static void AddResultsSheet(WorkbookPart workbookPart, Sheets sheets, BaristaFireResult result, DateTimeOffset generatedAt)
    {
        var rows = new List<Row>
        {
            new(CreateTextCell("A1", "Barista FIRE Results")),
            new(CreateTextCell("A2", "Generated UTC"), CreateTextCell("B2", generatedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture))),
            new(CreateTextCell("A4", "Result"), CreateTextCell("B4", "Value")),
            new(CreateTextCell("A5", "Full FIRE Number"), CreateFormulaCell("B5", "Inputs!B8/Inputs!B12", CurrencyStyleIndex)),
            new(CreateTextCell("A6", "Barista FIRE Number"), CreateFormulaCell("B6", "MAX(0,Inputs!B8-Inputs!B9)/Inputs!B12", CurrencyStyleIndex)),
            new(CreateTextCell("A7", "Years to Barista FIRE"), CreateNumberCell("B7", result.YearsToBaristaFire, DecimalStyleIndex)),
            new(CreateTextCell("A8", "Savings from part-time income"), CreateFormulaCell("B8", "B5-B6", CurrencyStyleIndex))
        };

        AddWorksheet(workbookPart, sheets, "Results", 2, rows, 34, 20);
    }

    private static void AddProjectionSheet(WorkbookPart workbookPart, Sheets sheets, IReadOnlyList<ProjectionPoint> projections)
    {
        var rows = new List<Row>
        {
            new(
                CreateTextCell("A1", "Age"),
                CreateTextCell("B1", "Year"),
                CreateTextCell("C1", "Portfolio"),
                CreateTextCell("D1", "Annual Contribution"),
                CreateTextCell("E1", "Total Contributions"),
                CreateTextCell("F1", "Inflation-Adjusted Portfolio"))
        };

        for (var index = 0; index < projections.Count; index++)
        {
            var rowNumber = index + 2;
            var point = projections[index];
            if (index == 0)
            {
                rows.Add(new Row(
                    CreateNumberCell($"A{rowNumber}", point.Age, DecimalStyleIndex),
                    CreateNumberCell($"B{rowNumber}", point.Year, DecimalStyleIndex),
                    CreateNumberCell($"C{rowNumber}", point.Portfolio, CurrencyStyleIndex),
                    CreateNumberCell($"D{rowNumber}", 0, CurrencyStyleIndex),
                    CreateNumberCell($"E{rowNumber}", point.TotalContributions, CurrencyStyleIndex),
                    CreateNumberCell($"F{rowNumber}", point.InflationAdjusted, CurrencyStyleIndex)));
                continue;
            }

            var previousRowNumber = rowNumber - 1;
            rows.Add(new Row(
                CreateNumberCell($"A{rowNumber}", point.Age, DecimalStyleIndex),
                CreateNumberCell($"B{rowNumber}", point.Year, DecimalStyleIndex),
                CreateFormulaCell($"C{rowNumber}", $"C{previousRowNumber}*(1+Inputs!$B$10)+D{rowNumber}", CurrencyStyleIndex),
                CreateNumberCell($"D{rowNumber}", point.Contributions, CurrencyStyleIndex),
                CreateFormulaCell($"E{rowNumber}", $"E{previousRowNumber}+D{rowNumber}", CurrencyStyleIndex),
                CreateFormulaCell($"F{rowNumber}", $"C{rowNumber}/((1+Inputs!$B$11)^(A{rowNumber}-$A$2))", CurrencyStyleIndex)));
        }

        AddWorksheet(workbookPart, sheets, "Projection", 3, rows, 14, 18, 20, 22, 22, 30);
    }

    private static void AddWorksheet(WorkbookPart workbookPart, Sheets sheets, string name, uint sheetId, IEnumerable<Row> rows, params double[] widths)
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
        sheets.Append(new Sheet
        {
            Id = workbookPart.GetIdOfPart(worksheetPart),
            SheetId = sheetId,
            Name = name
        });
    }

    private static Cell CreateTextCell(string reference, string value) => new()
    {
        CellReference = reference,
        DataType = CellValues.InlineString,
        InlineString = new InlineString(new Text(value))
    };

    private static Cell CreateNumberCell(string reference, double value, uint styleIndex) => new()
    {
        CellReference = reference,
        StyleIndex = styleIndex,
        CellValue = new CellValue(value.ToString(CultureInfo.InvariantCulture))
    };

    private static Cell CreateFormulaCell(string reference, string formula, uint styleIndex) => new()
    {
        CellReference = reference,
        StyleIndex = styleIndex,
        CellFormula = new CellFormula(formula)
    };

    private static void AddStyles(WorkbookPart workbookPart)
    {
        var stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
        stylesPart.Stylesheet = new Stylesheet(
            new NumberingFormats(
                new NumberingFormat { NumberFormatId = 164U, FormatCode = "$#,##0" },
                new NumberingFormat { NumberFormatId = 165U, FormatCode = "0.0%" },
                new NumberingFormat { NumberFormatId = 166U, FormatCode = "0.0" }),
            new Fonts(new Font()),
            new Fills(new Fill(new PatternFill { PatternType = PatternValues.None }), new Fill(new PatternFill { PatternType = PatternValues.Gray125 })),
            new Borders(new Border()),
            new CellStyleFormats(new CellFormat()),
            new CellFormats(
                new CellFormat(),
                new CellFormat { NumberFormatId = 164U, ApplyNumberFormat = true },
                new CellFormat { NumberFormatId = 165U, ApplyNumberFormat = true },
                new CellFormat { NumberFormatId = 166U, ApplyNumberFormat = true }));
    }
}