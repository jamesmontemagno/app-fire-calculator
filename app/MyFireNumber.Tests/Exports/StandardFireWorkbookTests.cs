using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Exports;
using MyFireNumber.Core.Presentation;

namespace MyFireNumber.Tests.Exports;

public sealed class StandardFireWorkbookTests : IDisposable
{
    private readonly string workbookPath = Path.Combine(Path.GetTempPath(), $"my-fire-number-{Guid.NewGuid():N}.xlsx");

    [Fact]
    public void Create_WritesInputsResultsAndFormulaBackedProjections()
    {
        var draft = StandardFireDraft.Default;
        var result = FinancialCalculator.CalculateStandardFire(draft.ToFireInputs(2026));

        StandardFireWorkbook.Create(workbookPath, draft, result, CurrencyPeriod.Annual, new DateTimeOffset(2026, 8, 8, 12, 0, 0, TimeSpan.Zero));

        using var document = SpreadsheetDocument.Open(workbookPath, false);
        var workbookPart = document.WorkbookPart ?? throw new InvalidOperationException("Workbook part was not created.");
        var workbook = workbookPart.Workbook ?? throw new InvalidOperationException("Workbook was not created.");
        var sheets = (workbook.Sheets ?? throw new InvalidOperationException("Workbook sheets were not created.")).Elements<Sheet>().ToArray();

        Assert.Equal(["Inputs", "Results", "Projections"], sheets.Select(sheet => sheet.Name!.Value));
        Assert.Equal("Annual income (after tax)", GetCellText(workbookPart, sheets[0], "A9"));
        Assert.Equal("Expenses (per year)", GetCellText(workbookPart, sheets[0], "A10"));
        Assert.Equal("100000", GetCell(workbookPart, sheets[0], "B7").CellValue!.Text);
        Assert.Equal("Inputs!B10/Inputs!B13", GetCell(workbookPart, sheets[1], "B5").CellFormula!.Text);
        Assert.Equal("Target retirement age", GetCellText(workbookPart, sheets[1], "A11"));
        Assert.Equal("55", GetCell(workbookPart, sheets[1], "B11").CellValue!.Text);
        Assert.Equal("Target-age goal", GetCellText(workbookPart, sheets[1], "A12"));
        Assert.Equal(result.RetirementGoal.Message, GetCellText(workbookPart, sheets[1], "B12"));
        Assert.Equal("C2*(1+Inputs!$B$11)+D3", GetCell(workbookPart, sheets[2], "C3").CellFormula!.Text);
        Assert.Equal("C3/((1+Inputs!$B$12)^(A3-$A$2))", GetCell(workbookPart, sheets[2], "F3").CellFormula!.Text);
    }

    [Fact]
    public void CreateLean_WritesLeanTitleAndCappedExpenseInput()
    {
        var draft = LeanFireDraft.Default;
        var result = FinancialCalculator.CalculateLeanFire(draft.ToFireInputs(2026)).Standard;

        StandardFireWorkbook.CreateLean(workbookPath, draft, result, CurrencyPeriod.Annual, new DateTimeOffset(2026, 8, 9, 12, 0, 0, TimeSpan.Zero));

        using var document = SpreadsheetDocument.Open(workbookPath, false);
        var workbookPart = document.WorkbookPart ?? throw new InvalidOperationException("Workbook part was not created.");
        var workbook = workbookPart.Workbook ?? throw new InvalidOperationException("Workbook was not created.");
        var sheets = (workbook.Sheets ?? throw new InvalidOperationException("Workbook sheets were not created.")).Elements<Sheet>().ToArray();

        Assert.Equal("Lean FIRE Inputs", GetCell(workbookPart, sheets[0], "A1").InlineString!.Text!.Text);
        Assert.Equal("40000", GetCell(workbookPart, sheets[0], "B10").CellValue!.Text);
        Assert.Equal("Lean FIRE Results", GetCell(workbookPart, sheets[1], "A1").InlineString!.Text!.Text);
    }

    [Fact]
    public void CreateFat_WritesFatTitleAndEnteredExpenseInput()
    {
        var draft = FatFireDraft.Default with { AnnualExpenses = 125_000 };
        var result = FinancialCalculator.CalculateFatFire(draft.ToFireInputs(2026)).Standard;

        StandardFireWorkbook.CreateFat(workbookPath, draft, result, CurrencyPeriod.Annual, new DateTimeOffset(2026, 8, 9, 12, 0, 0, TimeSpan.Zero));

        using var document = SpreadsheetDocument.Open(workbookPath, false);
        var workbookPart = document.WorkbookPart ?? throw new InvalidOperationException("Workbook part was not created.");
        var workbook = workbookPart.Workbook ?? throw new InvalidOperationException("Workbook was not created.");
        var sheets = (workbook.Sheets ?? throw new InvalidOperationException("Workbook sheets were not created.")).Elements<Sheet>().ToArray();

        Assert.Equal("Fat FIRE Inputs", GetCell(workbookPart, sheets[0], "A1").InlineString!.Text!.Text);
        Assert.Equal("125000", GetCell(workbookPart, sheets[0], "B10").CellValue!.Text);
        Assert.Equal("Fat FIRE Results", GetCell(workbookPart, sheets[1], "A1").InlineString!.Text!.Text);
    }

    [Fact]
    public void Create_UsesTheActiveMonthlyDisplayPeriodForRetirementSpending()
    {
        var draft = StandardFireDraft.Default with { AnnualExpenses = 48_000 };
        var result = FinancialCalculator.CalculateStandardFire(draft.ToFireInputs(2026));

        StandardFireWorkbook.Create(workbookPath, draft, result, CurrencyPeriod.Monthly, new DateTimeOffset(2026, 8, 9, 12, 0, 0, TimeSpan.Zero));

        using var document = SpreadsheetDocument.Open(workbookPath, false);
        var workbookPart = document.WorkbookPart ?? throw new InvalidOperationException("Workbook part was not created.");
        var sheets = (workbookPart.Workbook?.Sheets ?? throw new InvalidOperationException("Workbook sheets were not created.")).Elements<Sheet>().ToArray();

        Assert.Equal("Expenses (per month)", GetCellText(workbookPart, sheets[0], "A10"));
        Assert.Equal("4000", GetCell(workbookPart, sheets[0], "B10").CellValue!.Text);
    }

    public void Dispose()
    {
        if (File.Exists(workbookPath))
        {
            File.Delete(workbookPath);
        }
    }

    private static Cell GetCell(WorkbookPart workbookPart, Sheet sheet, string cellReference)
    {
        var relationshipId = sheet.Id?.Value ?? throw new InvalidOperationException("Worksheet relationship ID was not created.");
        var worksheetPart = (WorksheetPart)workbookPart.GetPartById(relationshipId);
        var worksheet = worksheetPart.Worksheet ?? throw new InvalidOperationException("Worksheet was not created.");
        return Assert.Single(worksheet.Descendants<Cell>(), cell => cell.CellReference?.Value == cellReference);
    }

    private static string GetCellText(WorkbookPart workbookPart, Sheet sheet, string cellReference)
    {
        return GetCell(workbookPart, sheet, cellReference).InlineString?.Text?.Text ?? string.Empty;
    }
}