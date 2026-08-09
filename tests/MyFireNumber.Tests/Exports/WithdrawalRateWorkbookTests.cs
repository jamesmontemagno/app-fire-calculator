using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Exports;

namespace MyFireNumber.Tests.Exports;

public sealed class WithdrawalRateWorkbookTests : IDisposable
{
    private readonly string workbookPath = Path.Combine(Path.GetTempPath(), $"my-fire-number-withdrawal-{Guid.NewGuid():N}.xlsx");

    [Fact]
    public void Create_WritesWithdrawalProjectionAndRateAnalysis()
    {
        var draft = WithdrawalRateDraft.Default;
        var result = FinancialCalculator.CalculateWithdrawal(draft.PortfolioValue, draft.WithdrawalRate, draft.ExpectedReturn, draft.InflationRate, draft.RetirementYears);

        WithdrawalRateWorkbook.Create(workbookPath, draft, result, new DateTimeOffset(2026, 8, 9, 12, 0, 0, TimeSpan.Zero));

        using var document = SpreadsheetDocument.Open(workbookPath, false);
        var workbookPart = document.WorkbookPart ?? throw new InvalidOperationException("Workbook part was not created.");
        var sheets = (workbookPart.Workbook?.Sheets ?? throw new InvalidOperationException("Workbook sheets were not created.")).Elements<Sheet>().ToArray();

        Assert.Equal(["Inputs", "Results", "Withdrawal Projection", "Rate Analysis"], sheets.Select(sheet => sheet.Name!.Value));
        Assert.Equal("Inputs!B5*Inputs!B6", GetCell(workbookPart, sheets[1], "B5").CellFormula!.Text);
        Assert.Equal("B5/12", GetCell(workbookPart, sheets[1], "B6").CellFormula!.Text);
        Assert.Equal("B2*(1+Inputs!$B$7)-C2", GetCell(workbookPart, sheets[2], "B3").CellFormula!.Text);
        Assert.Equal("C2*(1+Inputs!$B$8)", GetCell(workbookPart, sheets[2], "C3").CellFormula!.Text);
        Assert.Equal("Inputs!B5*A4", GetCell(workbookPart, sheets[3], "B4").CellFormula!.Text);
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
}