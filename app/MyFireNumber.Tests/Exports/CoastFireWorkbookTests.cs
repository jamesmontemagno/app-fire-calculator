using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Exports;
using MyFireNumber.Core.Presentation;

namespace MyFireNumber.Tests.Exports;

public sealed class CoastFireWorkbookTests : IDisposable
{
    private readonly string workbookPath = Path.Combine(Path.GetTempPath(), $"my-fire-number-coast-{Guid.NewGuid():N}.xlsx");

    [Fact]
    public void Create_WritesInputsResultsAndBothProjectionPaths()
    {
        var draft = CoastFireDraft.Default;
        var result = FinancialCalculator.CalculateCoastFire(draft.ToFireInputs(2026));

        CoastFireWorkbook.Create(workbookPath, draft, result, CurrencyPeriod.Annual, new DateTimeOffset(2026, 8, 9, 12, 0, 0, TimeSpan.Zero));

        using var document = SpreadsheetDocument.Open(workbookPath, false);
        var workbookPart = document.WorkbookPart ?? throw new InvalidOperationException("Workbook part was not created.");
        var workbook = workbookPart.Workbook ?? throw new InvalidOperationException("Workbook was not created.");
        var sheets = (workbook.Sheets ?? throw new InvalidOperationException("Workbook sheets were not created.")).Elements<Sheet>().ToArray();

        Assert.Equal(["Inputs", "Results", "Coast Projection", "Contributions Projection"], sheets.Select(sheet => sheet.Name!.Value));
        Assert.Equal("Expenses (per year)", GetCellText(workbookPart, sheets[0], "A9"));
        Assert.Equal("48000", GetCell(workbookPart, sheets[0], "B9").CellValue!.Text);
        Assert.Equal("Inputs!B9/Inputs!B12", GetCell(workbookPart, sheets[1], "B5").CellFormula!.Text);
        Assert.Equal("C2*(1+Inputs!$B$10)+D3", GetCell(workbookPart, sheets[2], "C3").CellFormula!.Text);
        Assert.Equal("C2*(1+Inputs!$B$10)+D3", GetCell(workbookPart, sheets[3], "C3").CellFormula!.Text);
        Assert.Equal("24000", GetCell(workbookPart, sheets[3], "D3").CellValue!.Text);
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