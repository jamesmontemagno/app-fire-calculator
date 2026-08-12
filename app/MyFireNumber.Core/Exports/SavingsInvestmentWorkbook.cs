using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MyFireNumber.Core.Calculations;
using System.Globalization;

namespace MyFireNumber.Core.Exports;

public static class SavingsInvestmentWorkbook
{
    private const uint CurrencyStyleIndex = 1;
    private const uint PercentageStyleIndex = 2;
    private const uint DecimalStyleIndex = 3;

    public static void Create(string filePath, SavingsInvestmentDraft draft, InvestmentGrowthResult result, DateTimeOffset generatedAt)
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

    private static void AddInputsSheet(WorkbookPart workbookPart, Sheets sheets, SavingsInvestmentDraft draft, DateTimeOffset generatedAt)
    {
        var rows = new List<Row>
        {
            new(CreateTextCell("A1", "Savings & Investment Rate Inputs")),
            new(CreateTextCell("A2", "Generated UTC"), CreateTextCell("B2", generatedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture))),
            new(CreateTextCell("A4", "Input"), CreateTextCell("B4", "Value")),
            new(CreateTextCell("A5", "Starting amount"), CreateNumberCell("B5", draft.StartingAmount, CurrencyStyleIndex)),
            new(CreateTextCell("A6", "Contribution amount"), CreateNumberCell("B6", draft.ContributionAmount, CurrencyStyleIndex)),
            new(CreateTextCell("A7", "Contribution frequency"), CreateTextCell("B7", draft.ContributionFrequency.ToString())),
            new(CreateTextCell("A8", "Years investing"), CreateNumberCell("B8", draft.YearsInvesting, DecimalStyleIndex)),
            new(CreateTextCell("A9", "Expected return"), CreateNumberCell("B9", draft.ExpectedReturn, PercentageStyleIndex)),
            new(CreateTextCell("A10", "Inflation rate"), CreateNumberCell("B10", draft.InflationRate, PercentageStyleIndex)),
            new(CreateTextCell("A11", "Annual take-home income (after tax)"), CreateNumberCell("B11", draft.AnnualIncome, CurrencyStyleIndex)),
            new(CreateTextCell("A12", "Current age"), CreateNumberCell("B12", draft.CurrentAge, DecimalStyleIndex))
        };
        AddWorksheet(workbookPart, sheets, "Inputs", 1, rows, 32, 20);
    }

    private static void AddResultsSheet(WorkbookPart workbookPart, Sheets sheets, InvestmentGrowthResult result, DateTimeOffset generatedAt)
    {
        var rows = new List<Row>
        {
            new(CreateTextCell("A1", "Savings & Investment Rate Results")),
            new(CreateTextCell("A2", "Generated UTC"), CreateTextCell("B2", generatedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture))),
            new(CreateTextCell("A4", "Result"), CreateTextCell("B4", "Value")),
            new(CreateTextCell("A5", "Annual contribution"), CreateFormulaCell("B5", "IF(Inputs!B7=\"Monthly\",Inputs!B6*12,Inputs!B6)", CurrencyStyleIndex)),
            new(CreateTextCell("A6", "Savings rate"), CreateFormulaCell("B6", "IF(Inputs!B11=0,0,B5/Inputs!B11)", PercentageStyleIndex)),
            new(CreateTextCell("A7", "Projected portfolio"), CreateNumberCell("B7", result.FinalNominalBalance, CurrencyStyleIndex)),
            new(CreateTextCell("A8", "Portfolio in today's dollars"), CreateNumberCell("B8", result.FinalInflationAdjustedBalance, CurrencyStyleIndex)),
            new(CreateTextCell("A9", "Total invested"), CreateNumberCell("B9", result.TotalInvested, CurrencyStyleIndex)),
            new(CreateTextCell("A10", "Total growth"), CreateNumberCell("B10", result.TotalGrowth, CurrencyStyleIndex)),
            new(CreateTextCell("A11", "Savings category"), CreateTextCell("B11", result.SavingsCategory))
        };
        AddWorksheet(workbookPart, sheets, "Results", 2, rows, 34, 20);
    }

    private static void AddProjectionSheet(WorkbookPart workbookPart, Sheets sheets, IReadOnlyList<InvestmentProjectionPoint> projections)
    {
        var rows = new List<Row>
        {
            new(CreateTextCell("A1", "Age"), CreateTextCell("B1", "Year"), CreateTextCell("C1", "Portfolio"), CreateTextCell("D1", "Annual Contribution"), CreateTextCell("E1", "Total Invested"), CreateTextCell("F1", "Today's Dollars"))
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
                CreateFormulaCell($"C{rowNumber}", $"C{previousRowNumber}*(1+Inputs!$B$9)+D{rowNumber}", CurrencyStyleIndex),
                CreateFormulaCell($"D{rowNumber}", "Results!B5", CurrencyStyleIndex),
                CreateFormulaCell($"E{rowNumber}", $"E{previousRowNumber}+D{rowNumber}", CurrencyStyleIndex),
                CreateFormulaCell($"F{rowNumber}", $"C{rowNumber}/((1+Inputs!$B$10)^(A{rowNumber}-$A$2))", CurrencyStyleIndex)));
        }
        AddWorksheet(workbookPart, sheets, "Projection", 3, rows, 14, 18, 20, 22, 20, 22);
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