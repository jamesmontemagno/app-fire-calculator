using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MyFireNumber.Core.Calculations;
using System.Globalization;

namespace MyFireNumber.Core.Exports;

public static class WithdrawalRateWorkbook
{
    private const uint CurrencyStyleIndex = 1;
    private const uint PercentageStyleIndex = 2;
    private const uint DecimalStyleIndex = 3;

    public static void Create(string filePath, WithdrawalRateDraft draft, WithdrawalResult result, DateTimeOffset generatedAt)
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
        AddProjectionSheet(workbookPart, sheets, result.WithdrawalProjections);
        AddRateAnalysisSheet(workbookPart, sheets, draft, result.RateAnalysis);
        workbookPart.Workbook.Save();
    }

    private static void AddInputsSheet(WorkbookPart workbookPart, Sheets sheets, WithdrawalRateDraft draft, DateTimeOffset generatedAt)
    {
        var rows = new List<Row>
        {
            new(CreateTextCell("A1", "Withdrawal Rate Inputs")),
            new(CreateTextCell("A2", "Generated UTC"), CreateTextCell("B2", generatedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture))),
            new(CreateTextCell("A4", "Input"), CreateTextCell("B4", "Value")),
            new(CreateTextCell("A5", "Portfolio value"), CreateNumberCell("B5", draft.PortfolioValue, CurrencyStyleIndex)),
            new(CreateTextCell("A6", "Withdrawal rate"), CreateNumberCell("B6", draft.WithdrawalRate, PercentageStyleIndex)),
            new(CreateTextCell("A7", "Expected return"), CreateNumberCell("B7", draft.ExpectedReturn, PercentageStyleIndex)),
            new(CreateTextCell("A8", "Inflation rate"), CreateNumberCell("B8", draft.InflationRate, PercentageStyleIndex)),
            new(CreateTextCell("A9", "Retirement duration"), CreateNumberCell("B9", draft.RetirementYears, DecimalStyleIndex))
        };

        AddWorksheet(workbookPart, sheets, "Inputs", 1, rows, 32, 20);
    }

    private static void AddResultsSheet(WorkbookPart workbookPart, Sheets sheets, WithdrawalResult result, DateTimeOffset generatedAt)
    {
        var rows = new List<Row>
        {
            new(CreateTextCell("A1", "Withdrawal Rate Results")),
            new(CreateTextCell("A2", "Generated UTC"), CreateTextCell("B2", generatedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture))),
            new(CreateTextCell("A4", "Result"), CreateTextCell("B4", "Value")),
            new(CreateTextCell("A5", "Annual withdrawal"), CreateFormulaCell("B5", "Inputs!B5*Inputs!B6", CurrencyStyleIndex)),
            new(CreateTextCell("A6", "Monthly withdrawal"), CreateFormulaCell("B6", "B5/12", CurrencyStyleIndex)),
            new(CreateTextCell("A7", "Portfolio longevity"), CreateNumberCell("B7", result.PortfolioLongevity, DecimalStyleIndex)),
            new(CreateTextCell("A8", "Success rate"), CreateNumberCell("B8", result.SuccessRate, PercentageStyleIndex)),
            new(CreateTextCell("A9", "Ending balance"), CreateNumberCell("B9", result.EndingBalance, CurrencyStyleIndex))
        };

        AddWorksheet(workbookPart, sheets, "Results", 2, rows, 30, 20);
    }

    private static void AddProjectionSheet(WorkbookPart workbookPart, Sheets sheets, IReadOnlyList<WithdrawalProjection> projections)
    {
        var rows = new List<Row>
        {
            new(CreateTextCell("A1", "Retirement Year"), CreateTextCell("B1", "Portfolio Balance"), CreateTextCell("C1", "Annual Withdrawal"))
        };

        for (var index = 0; index < projections.Count; index++)
        {
            var rowNumber = index + 2;
            var projection = projections[index];
            if (index == 0)
            {
                rows.Add(new Row(
                    CreateNumberCell($"A{rowNumber}", projection.Year, DecimalStyleIndex),
                    CreateNumberCell($"B{rowNumber}", projection.Balance, CurrencyStyleIndex),
                    CreateFormulaCell($"C{rowNumber}", "Results!B5", CurrencyStyleIndex)));
                continue;
            }

            var previousRowNumber = rowNumber - 1;
            rows.Add(new Row(
                CreateNumberCell($"A{rowNumber}", projection.Year, DecimalStyleIndex),
                CreateFormulaCell($"B{rowNumber}", $"B{previousRowNumber}*(1+Inputs!$B$7)-C{previousRowNumber}", CurrencyStyleIndex),
                CreateFormulaCell($"C{rowNumber}", $"C{previousRowNumber}*(1+Inputs!$B$8)", CurrencyStyleIndex)));
        }

        AddWorksheet(workbookPart, sheets, "Withdrawal Projection", 3, rows, 18, 22, 22);
    }

    private static void AddRateAnalysisSheet(WorkbookPart workbookPart, Sheets sheets, WithdrawalRateDraft draft, IReadOnlyList<WithdrawalRateAnalysis> analysis)
    {
        var rows = new List<Row>
        {
            new(CreateTextCell("A1", "Withdrawal Rate Analysis")),
            new(CreateTextCell("A3", "Rate"), CreateTextCell("B3", "Annual Withdrawal"), CreateTextCell("C3", "Portfolio Lasts"), CreateTextCell("D3", "Ending Balance"))
        };
        for (var index = 0; index < analysis.Count; index++)
        {
            var rowNumber = index + 4;
            var rate = analysis[index];
            rows.Add(new Row(
                CreateNumberCell($"A{rowNumber}", rate.Rate, PercentageStyleIndex),
                CreateFormulaCell($"B{rowNumber}", $"Inputs!B5*A{rowNumber}", CurrencyStyleIndex),
                CreateNumberCell($"C{rowNumber}", rate.Years, DecimalStyleIndex),
                CreateNumberCell($"D{rowNumber}", rate.EndBalance, CurrencyStyleIndex)));
        }

        AddWorksheet(workbookPart, sheets, "Rate Analysis", 4, rows, 18, 22, 20, 22);
    }

    private static void AddWorksheet(WorkbookPart workbookPart, Sheets sheets, string name, uint sheetId, IEnumerable<Row> rows, params double[] widths)
    {
        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        var columns = new Columns(widths.Select((width, index) => new Column { Min = (uint)index + 1, Max = (uint)index + 1, Width = width, CustomWidth = true }));
        worksheetPart.Worksheet = new Worksheet(columns, new SheetData(rows));
        sheets.Append(new Sheet { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = sheetId, Name = name });
    }

    private static Cell CreateTextCell(string reference, string value) => new() { CellReference = reference, DataType = CellValues.InlineString, InlineString = new InlineString(new Text(value)) };

    private static Cell CreateNumberCell(string reference, double value, uint styleIndex) => new() { CellReference = reference, StyleIndex = styleIndex, CellValue = new CellValue(value.ToString(CultureInfo.InvariantCulture)) };

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