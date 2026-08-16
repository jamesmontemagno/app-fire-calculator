using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Exports;

namespace MyFireNumber.Tests.Exports;

public sealed class SavingsInvestmentWorkbookTests : IDisposable
{
    private readonly string workbookPath = Path.Combine(Path.GetTempPath(), $"my-fire-number-investment-{Guid.NewGuid():N}.xlsx");

    [Fact]
    public void Create_WritesFrequencyAwareResultsAndProjection()
    {
        var draft = SavingsInvestmentDraft.Default;
        var result = FinancialCalculator.CalculateInvestmentGrowth(draft.ToInputs(2026));

        SavingsInvestmentWorkbook.Create(workbookPath, draft, result, new DateTimeOffset(2026, 8, 9, 12, 0, 0, TimeSpan.Zero));

        using var document = SpreadsheetDocument.Open(workbookPath, false);
        var workbookPart = document.WorkbookPart ?? throw new InvalidOperationException("Workbook part was not created.");
        var sheets = (workbookPart.Workbook?.Sheets ?? throw new InvalidOperationException("Workbook sheets were not created.")).Elements<Sheet>().ToArray();

        Assert.Equal(["Inputs", "Results", "Projection"], sheets.Select(sheet => sheet.Name!.Value));
        Assert.Equal("Monthly", GetCellText(workbookPart, sheets[0], "B7"));
        Assert.Equal("Annual income (after tax)", GetCellText(workbookPart, sheets[0], "A11"));
        Assert.Equal("IF(Inputs!B7=\"Monthly\",Inputs!B6*12,Inputs!B6)", GetCell(workbookPart, sheets[1], "B5").CellFormula!.Text);
        Assert.Equal("IF(Inputs!B11=0,0,B5/Inputs!B11)", GetCell(workbookPart, sheets[1], "B6").CellFormula!.Text);
        Assert.Equal("C2*(1+Inputs!$B$9)+D3", GetCell(workbookPart, sheets[2], "C3").CellFormula!.Text);
        Assert.Equal("Results!B5", GetCell(workbookPart, sheets[2], "D3").CellFormula!.Text);
    }

    public void Dispose()
    {
        if (File.Exists(workbookPath))
        {
            File.Delete(workbookPath);
        }
    }

    private static string GetCellText(WorkbookPart workbookPart, Sheet sheet, string cellReference)
    {
        return GetCell(workbookPart, sheet, cellReference).InlineString?.Text?.Text ?? string.Empty;
    }

    private static Cell GetCell(WorkbookPart workbookPart, Sheet sheet, string cellReference)
    {
        var relationshipId = sheet.Id?.Value ?? throw new InvalidOperationException("Worksheet relationship ID was not created.");
        var worksheetPart = (WorksheetPart)workbookPart.GetPartById(relationshipId);
        var worksheet = worksheetPart.Worksheet ?? throw new InvalidOperationException("Worksheet was not created.");
        return Assert.Single(worksheet.Descendants<Cell>(), cell => cell.CellReference?.Value == cellReference);
    }
}