using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Exports;

namespace MyFireNumber.Tests.Exports;

public sealed class SeppWorkbookTests : IDisposable
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"sepp-{Guid.NewGuid():N}.xlsx");

    [Fact]
    public void Create_WritesInputsResultsAndSelectedProjection()
    {
        var draft = SeppDraft.Default with
        {
            BirthDate = new DateOnly(1976, 8, 22),
            FirstPaymentDate = new DateOnly(2026, 8, 22),
            AnnuityFactor = 16.2
        };
        var result = SeppCalculator.Calculate(draft.ToInputs());

        SeppWorkbook.Create(path, draft, result, DateTimeOffset.UtcNow);

        using var document = SpreadsheetDocument.Open(path, false);
        var workbook = document.WorkbookPart ?? throw new InvalidOperationException();
        var workbookElement = workbook.Workbook ?? throw new InvalidOperationException();
        var sheets = (workbookElement.Sheets ?? throw new InvalidOperationException()).Elements<Sheet>().ToArray();
        Assert.Equal(["Inputs", "Results", "Projection"], sheets.Select(sheet => sheet.Name!.Value));
        Assert.Equal("500000", Cell(workbook, sheets[0], "B7").CellValue!.Text);
        Assert.Equal("10", Cell(workbook, sheets[1], "B7").CellValue!.Text);
        Assert.Equal("1", Cell(workbook, sheets[2], "A2").CellValue!.Text);
    }

    public void Dispose()
    {
        if (File.Exists(path)) File.Delete(path);
    }

    private static Cell Cell(WorkbookPart workbook, Sheet sheet, string reference)
    {
        var relationshipId = sheet.Id?.Value ?? throw new InvalidOperationException();
        var part = (WorksheetPart)workbook.GetPartById(relationshipId);
        var worksheet = part.Worksheet ?? throw new InvalidOperationException();
        return Assert.Single(worksheet.Descendants<Cell>(), cell => cell.CellReference?.Value == reference);
    }
}
