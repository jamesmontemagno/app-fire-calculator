using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Exports;

namespace MyFireNumber.Tests.Exports;

public sealed class HealthcareGapWorkbookTests : IDisposable
{
    private readonly string workbookPath = Path.Combine(Path.GetTempPath(), $"my-fire-number-healthcare-{Guid.NewGuid():N}.xlsx");

    [Fact]
    public void Create_WritesGapResultsAndAnnualCostProjection()
    {
        var draft = HealthcareGapDraft.Default;
        var result = FinancialCalculator.CalculateHealthcareGap(draft.ToInputs(2026));

        HealthcareGapWorkbook.Create(workbookPath, draft, result, new DateTimeOffset(2026, 8, 9, 12, 0, 0, TimeSpan.Zero));

        using var document = SpreadsheetDocument.Open(workbookPath, false);
        var workbookPart = document.WorkbookPart ?? throw new InvalidOperationException("Workbook part was not created.");
        var sheets = (workbookPart.Workbook?.Sheets ?? throw new InvalidOperationException("Workbook sheets were not created.")).Elements<Sheet>().ToArray();

        Assert.Equal(["Inputs", "Results", "Annual Cost"], sheets.Select(sheet => sheet.Name!.Value));
        Assert.Equal("MAX(0,Inputs!B7-Inputs!B6)", GetCell(workbookPart, sheets[1], "B5").CellFormula!.Text);
        Assert.Equal("Inputs!B8*12+Inputs!B9+Inputs!B10", GetCell(workbookPart, sheets[1], "B6").CellFormula!.Text);
        Assert.Equal("D2+E2+F2", GetCell(workbookPart, sheets[2], "C2").CellFormula!.Text);
        Assert.Equal("Inputs!$B$8*12*((1+Inputs!$B$11)^(A2-Inputs!$B$6))", GetCell(workbookPart, sheets[2], "D2").CellFormula!.Text);
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