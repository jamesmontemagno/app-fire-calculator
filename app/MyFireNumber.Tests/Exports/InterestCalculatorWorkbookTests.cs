using DocumentFormat.OpenXml.Packaging;
using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Exports;

namespace MyFireNumber.Tests.Exports;

public sealed class InterestCalculatorWorkbookTests : IDisposable
{
    private readonly string workbookPath = Path.Combine(Path.GetTempPath(), $"my-fire-number-interest-{Guid.NewGuid():N}.xlsx");

    [Fact]
    public void Create_WritesInputsResultsAndProjection()
    {
        var draft = InterestCalculatorDraft.Default;
        var result = FinancialCalculator.CalculateInterest(draft.ToInputs());

        InterestCalculatorWorkbook.Create(workbookPath, draft, result, DateTimeOffset.UtcNow);

        using var document = SpreadsheetDocument.Open(workbookPath, false);
        var sheets = document.WorkbookPart?.Workbook?.Sheets
            ?? throw new InvalidOperationException("Workbook sheets were not created.");
        var sheetNames = sheets.Elements<DocumentFormat.OpenXml.Spreadsheet.Sheet>()
            .Select(sheet => sheet.Name?.Value ?? string.Empty)
            .ToArray();
        Assert.Equal(["Inputs", "Results", "Projection"], sheetNames);
    }

    public void Dispose()
    {
        if (File.Exists(workbookPath))
        {
            File.Delete(workbookPath);
        }
    }
}
