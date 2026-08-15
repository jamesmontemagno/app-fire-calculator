using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Exports;
using MyFireNumber.Core.Presentation;

namespace MyFireNumber.Tests.Exports;

/// <summary>
/// A 0% expected return against 3% inflation makes the real return negative, so the FIRE target is
/// genuinely unreachable and the calculators correctly return <see cref="double.PositiveInfinity"/>.
/// The exporters used to write that straight into a numeric cell, where it serialized as "Infinity"
/// and produced a workbook cell Excel cannot read as a number. These lock in the text substitution.
/// </summary>
public sealed class UnreachableResultExportTests : IDisposable
{
    private readonly string workbookPath = Path.Combine(Path.GetTempPath(), $"my-fire-number-unreachable-{Guid.NewGuid():N}.xlsx");

    [Fact]
    public void StandardFire_UnreachableYears_WritesTextInsteadOfInfinity()
    {
        var draft = StandardFireDraft.Default with { ExpectedReturn = 0 };
        var result = FinancialCalculator.CalculateStandardFire(draft.ToFireInputs(2026));

        Assert.True(double.IsPositiveInfinity(result.YearsToFire), "Expected the target to be unreachable.");

        StandardFireWorkbook.Create(workbookPath, draft, result, CurrencyPeriod.Annual, new DateTimeOffset(2026, 8, 9, 12, 0, 0, TimeSpan.Zero));

        var cell = GetCell(1, "B6");
        Assert.Equal(WorkbookValues.Unreachable, cell.InlineString?.Text?.Text);
        Assert.Null(cell.CellValue);
    }

    [Fact]
    public void CoastFire_UnreachableYears_WritesTextInsteadOfInfinity()
    {
        var draft = CoastFireDraft.Default with { ExpectedReturn = 0, AnnualContribution = 0 };
        var result = FinancialCalculator.CalculateCoastFire(draft.ToFireInputs(2026));

        Assert.True(double.IsPositiveInfinity(result.YearsToCoast), "Expected the coast target to be unreachable.");

        CoastFireWorkbook.Create(workbookPath, draft, result, CurrencyPeriod.Annual, new DateTimeOffset(2026, 8, 9, 12, 0, 0, TimeSpan.Zero));

        Assert.Equal(WorkbookValues.Unreachable, GetCell(1, "B7").InlineString?.Text?.Text);
    }

    [Fact]
    public void BaristaFire_UnreachableYears_WritesTextInsteadOfInfinity()
    {
        var draft = BaristaFireDraft.Default with { ExpectedReturn = 0, AnnualContribution = 12_000 };
        var result = FinancialCalculator.CalculateBaristaFire(draft.ToFireInputs(2026), draft.PartTimeAnnualIncome);

        Assert.True(double.IsPositiveInfinity(result.YearsToBaristaFire), "Expected the barista target to be unreachable.");

        BaristaFireWorkbook.Create(workbookPath, draft, result, CurrencyPeriod.Annual, new DateTimeOffset(2026, 8, 9, 12, 0, 0, TimeSpan.Zero));

        Assert.Equal(WorkbookValues.Unreachable, GetCell(1, "B7").InlineString?.Text?.Text);
    }

    [Fact]
    public void ReachableResult_StillWritesANumber()
    {
        var draft = StandardFireDraft.Default;
        var result = FinancialCalculator.CalculateStandardFire(draft.ToFireInputs(2026));

        StandardFireWorkbook.Create(workbookPath, draft, result, CurrencyPeriod.Annual, new DateTimeOffset(2026, 8, 9, 12, 0, 0, TimeSpan.Zero));

        var cell = GetCell(1, "B6");
        Assert.Null(cell.InlineString);
        Assert.Equal(result.YearsToFire, double.Parse(cell.CellValue!.Text, System.Globalization.CultureInfo.InvariantCulture), 3);
    }

    public void Dispose()
    {
        if (File.Exists(workbookPath))
        {
            File.Delete(workbookPath);
        }
    }

    private Cell GetCell(int sheetIndex, string cellReference)
    {
        using var document = SpreadsheetDocument.Open(workbookPath, false);
        var workbookPart = document.WorkbookPart ?? throw new InvalidOperationException("Workbook part was not created.");
        var workbook = workbookPart.Workbook ?? throw new InvalidOperationException("Workbook was not created.");
        var sheet = (workbook.Sheets ?? throw new InvalidOperationException("Workbook sheets were not created."))
            .Elements<Sheet>()
            .ElementAt(sheetIndex);
        var relationshipId = sheet.Id?.Value ?? throw new InvalidOperationException("Worksheet relationship ID was not created.");
        var worksheetPart = (WorksheetPart)workbookPart.GetPartById(relationshipId);
        var worksheet = worksheetPart.Worksheet ?? throw new InvalidOperationException("Worksheet was not created.");
        return (Cell)Assert.Single(worksheet.Descendants<Cell>(), cell => cell.CellReference?.Value == cellReference).CloneNode(true);
    }
}
