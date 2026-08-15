using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MyFireNumber.Core.Calculations;
using System.Globalization;

namespace MyFireNumber.Core.Exports;

public static class StandardFireWorkbook
{
    private const uint CurrencyStyleIndex = WorkbookStyles.CurrencyStyleIndex;
    private const uint PercentageStyleIndex = WorkbookStyles.PercentageStyleIndex;
    private const uint DecimalStyleIndex = WorkbookStyles.DecimalStyleIndex;
    private const uint IntegerStyleIndex = WorkbookStyles.IntegerStyleIndex;
    private const uint PlainIntegerStyleIndex = WorkbookStyles.PlainIntegerStyleIndex;

    public static void Create(string filePath, StandardFireDraft draft, StandardFireResult result, DateTimeOffset generatedAt)
    {
        CreateWorkbook(filePath, "Standard FIRE", draft, result, generatedAt);
    }

    public static void CreateLean(string filePath, LeanFireDraft draft, StandardFireResult result, DateTimeOffset generatedAt)
    {
        ArgumentNullException.ThrowIfNull(draft);
        var calculationDraft = new StandardFireDraft(
            draft.CurrentAge,
            draft.RetirementAge,
            draft.CurrentSavings,
            draft.AnnualContribution,
            draft.AnnualIncome,
            draft.ExpectedReturn,
            draft.InflationRate,
            draft.WithdrawalRate,
            Math.Min(draft.AnnualExpenses, FinancialCalculator.LeanFireThreshold));
        CreateWorkbook(filePath, "Lean FIRE", calculationDraft, result, generatedAt);
    }

    public static void CreateFat(string filePath, FatFireDraft draft, StandardFireResult result, DateTimeOffset generatedAt)
    {
        ArgumentNullException.ThrowIfNull(draft);
        var calculationDraft = new StandardFireDraft(
            draft.CurrentAge,
            draft.RetirementAge,
            draft.CurrentSavings,
            draft.AnnualContribution,
            draft.AnnualIncome,
            draft.ExpectedReturn,
            draft.InflationRate,
            draft.WithdrawalRate,
            draft.AnnualExpenses);
        CreateWorkbook(filePath, "Fat FIRE", calculationDraft, result, generatedAt);
    }

    private static void CreateWorkbook(string filePath, string calculatorTitle, StandardFireDraft draft, StandardFireResult result, DateTimeOffset generatedAt)
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
        AddInputsSheet(workbookPart, sheets, calculatorTitle, draft, generatedAt);
        AddResultsSheet(workbookPart, sheets, calculatorTitle, result, generatedAt);
        AddProjectionsSheet(workbookPart, sheets, result.Projections);
        workbookPart.Workbook.Save();
    }

    private static void AddInputsSheet(WorkbookPart workbookPart, Sheets sheets, string calculatorTitle, StandardFireDraft draft, DateTimeOffset generatedAt)
    {
        var rows = new List<Row>
        {
            new(CreateTextCell("A1", $"{calculatorTitle} Inputs")),
            new(CreateTextCell("A2", "Generated UTC"), CreateTextCell("B2", generatedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture))),
            new(CreateTextCell("A4", "Input"), CreateTextCell("B4", "Value"))
        };

        var inputs = new (string Label, double Value, uint Style)[]
        {
            ("Current age", draft.CurrentAge, DecimalStyleIndex),
            ("Retirement age", draft.RetirementAge, DecimalStyleIndex),
            ("Current savings", draft.CurrentSavings, CurrencyStyleIndex),
            ("Annual contribution", draft.AnnualContribution, CurrencyStyleIndex),
            ("Annual take-home income (after tax)", draft.AnnualIncome, CurrencyStyleIndex),
            ("Annual retirement spending (today's dollars)", draft.AnnualExpenses, CurrencyStyleIndex),
            ("Expected return", draft.ExpectedReturn, PercentageStyleIndex),
            ("Inflation rate", draft.InflationRate, PercentageStyleIndex),
            ("Safe withdrawal rate", draft.WithdrawalRate, PercentageStyleIndex)
        };

        for (var index = 0; index < inputs.Length; index++)
        {
            var rowNumber = index + 5;
            var input = inputs[index];
            rows.Add(new Row(
                CreateTextCell($"A{rowNumber}", input.Label),
                CreateNumberCell($"B{rowNumber}", input.Value, input.Style)));
        }

        AddWorksheet(workbookPart, sheets, "Inputs", 1, rows, 32, 20);
    }

    private static void AddResultsSheet(WorkbookPart workbookPart, Sheets sheets, string calculatorTitle, StandardFireResult result, DateTimeOffset generatedAt)
    {
        var rows = new List<Row>
        {
            new(CreateTextCell("A1", $"{calculatorTitle} Results")),
            new(CreateTextCell("A2", "Generated UTC"), CreateTextCell("B2", generatedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture))),
            new(CreateTextCell("A4", "Result"), CreateTextCell("B4", "Value")),
            new(CreateTextCell("A5", "FIRE Number"), CreateFormulaCell("B5", "Inputs!B10/Inputs!B13", CurrencyStyleIndex)),
            new(CreateTextCell("A6", "Years to FIRE"), CreateNumberCell("B6", result.YearsToFire, DecimalStyleIndex)),
            new(CreateTextCell("A7", "FIRE age"), CreateNumberCell("B7", result.FireAge, DecimalStyleIndex)),
            new(CreateTextCell("A8", "Savings rate"), CreateFormulaCell("B8", "Inputs!B8/Inputs!B9", PercentageStyleIndex)),
            new(CreateTextCell("A9", "Monthly contribution"), CreateFormulaCell("B9", "Inputs!B8/12", CurrencyStyleIndex)),
            new(CreateTextCell("A10", "Coast FIRE Number"), CreateNumberCell("B10", result.CoastFireNumber, CurrencyStyleIndex)),
            new(CreateTextCell("A11", "Target retirement age"), CreateNumberCell("B11", result.RetirementGoal.TargetRetirementAge, DecimalStyleIndex)),
            new(CreateTextCell("A12", "Target-age goal"), CreateTextCell("B12", result.RetirementGoal.Message))
        };

        AddWorksheet(workbookPart, sheets, "Results", 2, rows, 32, 20);
    }

    private static void AddProjectionsSheet(WorkbookPart workbookPart, Sheets sheets, IReadOnlyList<ProjectionPoint> projections)
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
                    CreateNumberCell($"B{rowNumber}", point.Year, PlainIntegerStyleIndex),
                    CreateNumberCell($"C{rowNumber}", point.Portfolio, CurrencyStyleIndex),
                    CreateNumberCell($"D{rowNumber}", point.Contributions, CurrencyStyleIndex),
                    CreateNumberCell($"E{rowNumber}", point.TotalContributions, CurrencyStyleIndex),
                    CreateNumberCell($"F{rowNumber}", point.InflationAdjusted, CurrencyStyleIndex)));
                continue;
            }

            var previousRowNumber = rowNumber - 1;
            rows.Add(new Row(
                CreateNumberCell($"A{rowNumber}", point.Age, DecimalStyleIndex),
                CreateNumberCell($"B{rowNumber}", point.Year, PlainIntegerStyleIndex),
                CreateFormulaCell($"C{rowNumber}", $"C{previousRowNumber}*(1+Inputs!$B$11)+D{rowNumber}", CurrencyStyleIndex),
                CreateNumberCell($"D{rowNumber}", point.Contributions, CurrencyStyleIndex),
                CreateFormulaCell($"E{rowNumber}", $"E{previousRowNumber}+D{rowNumber}", CurrencyStyleIndex),
                CreateFormulaCell($"F{rowNumber}", $"C{rowNumber}/((1+Inputs!$B$12)^(A{rowNumber}-$A$2))", CurrencyStyleIndex)));
        }

        AddWorksheet(workbookPart, sheets, "Projections", 3, rows, 14, 18, 20, 22, 22, 30);
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

    private static Cell CreateTextCell(string reference, string value)
    {
        return new Cell
        {
            CellReference = reference,
            DataType = CellValues.InlineString,
            InlineString = new InlineString(new Text(value))
        };
    }

    private static Cell CreateNumberCell(string reference, double value, uint styleIndex)
    {
        // A non-finite result is a legitimate outcome (an unreachable target), but "Infinity" inside a
        // numeric cell is not a number Excel can read. Emit the same wording the apps show on screen.
        if (!double.IsFinite(value))
        {
            return CreateTextCell(reference, WorkbookValues.Unreachable);
        }

        return new Cell
        {
            CellReference = reference,
            StyleIndex = styleIndex,
            CellValue = new CellValue(value.ToString(CultureInfo.InvariantCulture))
        };
    }

    private static Cell CreateFormulaCell(string reference, string formula, uint styleIndex)
    {
        return new Cell
        {
            CellReference = reference,
            StyleIndex = styleIndex,
            CellFormula = new CellFormula(formula)
        };
    }

    private static void AddStyles(WorkbookPart workbookPart) => WorkbookStyles.Apply(workbookPart);
}