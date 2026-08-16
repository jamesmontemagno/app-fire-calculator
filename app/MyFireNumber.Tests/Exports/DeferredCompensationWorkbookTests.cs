using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Exports;
using MyFireNumber.Core.Presentation;

namespace MyFireNumber.Tests.Exports;

public sealed class DeferredCompensationWorkbookTests : IDisposable
{
    private readonly string workbookPath = Path.Combine(Path.GetTempPath(), $"my-fire-number-retirement-{Guid.NewGuid():N}.xlsx");

    [Fact]
    public void Create_WritesEveryCustomCollection()
    {
        var draft = DeferredCompensationDraft.Default with
        {
            Accounts =
            [
                new RetirementAccount("deferred", "Custom Deferred", RetirementAccountType.Deferred, 300_000, 0, 0.05, 55, 0, 5),
                new RetirementAccount("roth", "Custom Roth", RetirementAccountType.Roth, 125_000, 7_000, 0.06, 59, 0.04, 1)
            ],
            IncomeSources =
            [
                new RetirementIncomeSource("pension", "Custom Pension", 30_000, 62, 90, 0.02, false, 0.2)
            ],
            AdditionalExpenses =
            [
                new RetirementExpense("travel", "Custom Travel", 12_000, 60)
            ]
        };
        var result = DeferredCompensationCalculator.Calculate(draft.ToInputs(2026));

        DeferredCompensationWorkbook.Create(
            workbookPath,
            draft,
            result,
            CurrencyPeriod.Annual,
            new DateTimeOffset(2026, 8, 9, 12, 0, 0, TimeSpan.Zero));

        using var document = SpreadsheetDocument.Open(workbookPath, false);
        var workbookPart = document.WorkbookPart ?? throw new InvalidOperationException("Workbook part was not created.");
        var sheets = (workbookPart.Workbook?.Sheets ?? throw new InvalidOperationException("Workbook sheets were not created."))
            .Elements<Sheet>()
            .ToArray();

        Assert.Equal(
            ["Inputs", "Results", "Annual Cash Flow", "Accounts", "Income Sources", "Additional Expenses"],
            sheets.Select(sheet => sheet.Name!.Value));
        Assert.Equal("Expenses (per year)", GetCellText(workbookPart, sheets[0], "A8"));
        Assert.Equal("Custom Deferred", GetCell(workbookPart, sheets[3], "A2").InlineString!.Text!.Text);
        Assert.Equal("Equal annual payouts", GetCell(workbookPart, sheets[3], "G2").InlineString!.Text!.Text);
        Assert.Equal("5", GetCell(workbookPart, sheets[3], "H2").CellValue!.Text);
        Assert.Equal("Custom Roth", GetCell(workbookPart, sheets[3], "A3").InlineString!.Text!.Text);
        Assert.Equal("Withdrawal rate", GetCell(workbookPart, sheets[3], "G3").InlineString!.Text!.Text);
        Assert.Equal("0.04", GetCell(workbookPart, sheets[3], "H3").CellValue!.Text);
        Assert.Equal("Custom Pension", GetCell(workbookPart, sheets[4], "A2").InlineString!.Text!.Text);
        Assert.Equal("Annual amount", GetCellText(workbookPart, sheets[5], "B1"));
        Assert.Equal("Custom Travel", GetCell(workbookPart, sheets[5], "A2").InlineString!.Text!.Text);
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
        var worksheetPart = (WorksheetPart)workbookPart.GetPartById(
            sheet.Id?.Value ?? throw new InvalidOperationException("Worksheet relationship ID was not created."));
        return Assert.Single(
            (worksheetPart.Worksheet ?? throw new InvalidOperationException("Worksheet was not created.")).Descendants<Cell>(),
            cell => cell.CellReference?.Value == cellReference);
    }

    private static string GetCellText(WorkbookPart workbookPart, Sheet sheet, string cellReference)
    {
        return GetCell(workbookPart, sheet, cellReference).InlineString?.Text?.Text ?? string.Empty;
    }
}
