using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Exports;

namespace MyFireNumber.Tests.Exports;

public sealed class RothConversionWorkbookTests
{
    [Fact]
    public void Create_WritesInputsResultsAndProjection()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xlsx");
        try
        {
            var draft = RothConversionDraft.Default with
            {
                CurrentAge = 45,
                StartYear = 2026,
                ExpectedReturn = 0
            };
            var result = RothConversionCalculator.Calculate(draft.ToInputs());

            RothConversionWorkbook.Create(path, draft, result, DateTimeOffset.UnixEpoch);

            using var document = SpreadsheetDocument.Open(path, false);
            var sheets = document.WorkbookPart!.Workbook.Sheets!.Elements<Sheet>().ToArray();
            Assert.Equal(["Inputs", "Results", "Projection"], sheets.Select(sheet => sheet.Name!.Value));
            Assert.Equal(result.Projections.Count + 1, RowCount(document, sheets[2]));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private static int RowCount(SpreadsheetDocument document, Sheet sheet)
    {
        var worksheetPart = (WorksheetPart)document.WorkbookPart!.GetPartById(sheet.Id!);
        return worksheetPart.Worksheet.GetFirstChild<SheetData>()!.Elements<Row>().Count();
    }
}
