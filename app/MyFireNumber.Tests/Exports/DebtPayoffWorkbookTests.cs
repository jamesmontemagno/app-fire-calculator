using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Exports;

namespace MyFireNumber.Tests.Exports;

public sealed class DebtPayoffWorkbookTests : IDisposable
{
    private readonly string workbookPath = Path.Combine(Path.GetTempPath(), $"my-fire-number-debt-{Guid.NewGuid():N}.xlsx");

    [Fact]
    public void Create_WritesDebtListAndPayoffProjection()
    {
        var draft = DebtPayoffDraft.Default with { Debts = [new DebtItem("card", "Credit card", 1_000, 0, 100, 50)], MonthlyBudget = 500 };
        var result = FinancialCalculator.CalculateSnowballPayoff(draft.Debts, draft.MonthlyBudget);

        DebtPayoffWorkbook.Create(workbookPath, draft, result, new DateTimeOffset(2026, 8, 9, 12, 0, 0, TimeSpan.Zero));

        using var document = SpreadsheetDocument.Open(workbookPath, false);
        var workbookPart = document.WorkbookPart ?? throw new InvalidOperationException("Workbook part was not created.");
        var sheets = (workbookPart.Workbook?.Sheets ?? throw new InvalidOperationException("Workbook sheets were not created.")).Elements<Sheet>().ToArray();

        Assert.Equal(["Inputs", "Results", "Debt List", "Payoff Projection"], sheets.Select(sheet => sheet.Name!.Value));
        Assert.Equal("Credit card", GetCell(workbookPart, sheets[2], "A2").InlineString!.Text!.Text);
        Assert.Equal("Usual extra payment", GetCell(workbookPart, sheets[2], "E1").InlineString!.Text!.Text);
        Assert.Equal("50", GetCell(workbookPart, sheets[2], "E2").CellValue!.Text);
        Assert.Equal("2", GetCell(workbookPart, sheets[1], "B5").CellValue!.Text);
        Assert.Equal("1", GetCell(workbookPart, sheets[3], "A2").CellValue!.Text);
    }

    public void Dispose()
    {
        if (File.Exists(workbookPath)) File.Delete(workbookPath);
    }

    private static Cell GetCell(WorkbookPart workbookPart, Sheet sheet, string cellReference)
    {
        var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id?.Value ?? throw new InvalidOperationException("Worksheet relationship ID was not created."));
        return Assert.Single((worksheetPart.Worksheet ?? throw new InvalidOperationException("Worksheet was not created.")).Descendants<Cell>(), cell => cell.CellReference?.Value == cellReference);
    }
}