using System;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MyFireNumber.Core.Calculations;
using System.Globalization;

namespace MyFireNumber.Core.Exports;

public static class DeferredCompensationWorkbook
{
    private const uint CurrencyStyleIndex = WorkbookStyles.CurrencyStyleIndex;
    private const uint PercentageStyleIndex = WorkbookStyles.PercentageStyleIndex;
    private const uint DecimalStyleIndex = WorkbookStyles.DecimalStyleIndex;
    private const uint IntegerStyleIndex = WorkbookStyles.IntegerStyleIndex;
    private const uint PlainIntegerStyleIndex = WorkbookStyles.PlainIntegerStyleIndex;

    public static void Create(string filePath, DeferredCompensationDraft draft, DeferredCompensationResult result, DateTimeOffset generatedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");
        if (File.Exists(filePath)) File.Delete(filePath);

        using var document = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook);
        var workbookPart = document.AddWorkbookPart();
        workbookPart.Workbook = new Workbook();
        AddStyles(workbookPart);
        var sheets = workbookPart.Workbook.AppendChild(new Sheets());
        AddWorksheet(workbookPart, sheets, "Inputs", 1,
        [new Row(Text("A1", "Retirement Cash Flow Inputs")), new Row(Text("A2", "Generated UTC"), Text("B2", generatedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture))), new Row(Text("A4", "Input"), Text("B4", "Value")), new Row(Text("A5", "Current age"), Number("B5", draft.CurrentAge, IntegerFormat.Plain)), new Row(Text("A6", "Semi-retirement age"), Number("B6", draft.SemiRetirementAge, IntegerFormat.Plain)), new Row(Text("A7", "Plan through age"), Number("B7", draft.PlanThroughAge, IntegerFormat.Plain)), new Row(Text("A8", "Annual retirement spending (today's dollars)"), Number("B8", draft.AnnualExpenses, CurrencyStyleIndex)), new Row(Text("A9", "Inflation rate"), Number("B9", draft.InflationRate, PercentageStyleIndex))], 42, 22);
        AddWorksheet(workbookPart, sheets, "Results", 2,
        [new Row(Text("A1", "Retirement Cash Flow Results")), new Row(Text("A4", "Result"), Text("B4", "Value")), new Row(Text("A5", "Current balance"), Number("B5", result.CurrentBalance, CurrencyStyleIndex)), new Row(Text("A6", "Balance at semi-retirement"), Number("B6", result.BalanceAtSemiRetirement, CurrencyStyleIndex)), new Row(Text("A7", "First-year income (after tax)"), Number("B7", result.FirstYearIncome, CurrencyStyleIndex)), new Row(Text("A8", "First-year surplus"), Number("B8", result.FirstYearSurplus, CurrencyStyleIndex)), new Row(Text("A9", "Ending balance"), Number("B9", result.EndingBalance, CurrencyStyleIndex)), new Row(Text("A10", "Consecutive funded years from retirement"), Number("B10", result.FundedYears, IntegerFormat.Grouped)), new Row(Text("A11", "Retirement years projected"), Number("B11", result.RetirementYears, IntegerFormat.Grouped)), new Row(Text("A12", "Years fully covered (any year)"), Number("B12", result.YearsFullyCovered, IntegerFormat.Grouped)), new Row(Text("A13", "First shortfall age"), result.FirstShortfallAge is int shortfallAge ? Number("B13", shortfallAge, IntegerFormat.Plain) : Text("B13", "None projected"))], 40, 24);
        AddWorksheet(workbookPart, sheets, "Annual Cash Flow", 3, result.Projections.Select((point, index) => new Row(Number($"A{index + 2}", point.Age, IntegerFormat.Plain), Number($"B{index + 2}", point.Year, IntegerFormat.Plain), Number($"C{index + 2}", point.TotalBalance, CurrencyStyleIndex), Number($"D{index + 2}", point.TotalIncome, CurrencyStyleIndex), Number($"E{index + 2}", point.Expenses, CurrencyStyleIndex), Number($"F{index + 2}", point.Surplus, CurrencyStyleIndex), Number($"G{index + 2}", point.WithdrawalTaxes, CurrencyStyleIndex))).Prepend(new Row(Text("A1", "Age"), Text("B1", "Year"), Text("C1", "Total balance"), Text("D1", "Income (after tax)"), Text("E1", "Expenses"), Text("F1", "Surplus"), Text("G1", "Estimated withdrawal tax"))), 12, 14, 20, 20, 20, 20, 24);
        AddWorksheet(workbookPart, sheets, "Accounts", 4, draft.Accounts.Select((account, index) => new Row(
            Text($"A{index + 2}", account.Name),
            Text($"B{index + 2}", account.Type.ToString()),
            Number($"C{index + 2}", account.Balance, CurrencyStyleIndex),
            Number($"D{index + 2}", account.AnnualContribution, CurrencyStyleIndex),
            Number($"E{index + 2}", account.AnnualReturn, PercentageStyleIndex),
            Number($"F{index + 2}", account.AvailableAge, IntegerFormat.Plain),
            Text($"G{index + 2}", account.Type == RetirementAccountType.Deferred ? "Equal annual payouts" : "Withdrawal rate"),
            account.Type == RetirementAccountType.Deferred
                ? Number($"H{index + 2}", account.PayoutYears, IntegerFormat.Grouped)
                : Number($"H{index + 2}", account.WithdrawalRate, PercentageStyleIndex),
            Number($"I{index + 2}", account.EffectiveWithdrawalTaxRate, PercentageStyleIndex)))
            .Prepend(new Row(Text("A1", "Name"), Text("B1", "Type"), Text("C1", "Balance"), Text("D1", "Annual contribution (today's dollars)"), Text("E1", "Annual return"), Text("F1", "Available age"), Text("G1", "Distribution method"), Text("H1", "Value"), Text("I1", "Withdrawal tax rate"))), 28, 18, 18, 30, 18, 16, 22, 16, 20);
        AddWorksheet(workbookPart, sheets, "Income Sources", 5, draft.IncomeSources.Select((income, index) => new Row(
            Text($"A{index + 2}", income.Name),
            Number($"B{index + 2}", income.AnnualAmount, CurrencyStyleIndex),
            Number($"C{index + 2}", income.StartAge, IntegerFormat.Plain),
            Number($"D{index + 2}", income.EndAge, IntegerFormat.Plain),
            Number($"E{index + 2}", income.AnnualGrowth, PercentageStyleIndex),
            Text($"F{index + 2}", income.IsAfterTax ? "Yes" : "No"),
            Number($"G{index + 2}", income.TaxRate, PercentageStyleIndex)))
            .Prepend(new Row(Text("A1", "Name"), Text("B1", "Annual amount"), Text("C1", "Start age"), Text("D1", "End age"), Text("E1", "Annual growth"), Text("F1", "After tax"), Text("G1", "Tax rate"))), 28, 20, 14, 14, 18, 14, 16);
        AddWorksheet(workbookPart, sheets, "Additional Expenses", 6, draft.AdditionalExpenses.Select((expense, index) => new Row(
            Text($"A{index + 2}", expense.Name),
            Number($"B{index + 2}", expense.AnnualAmount, CurrencyStyleIndex),
            Number($"C{index + 2}", expense.StartAge, IntegerFormat.Plain)))
            .Prepend(new Row(Text("A1", "Name"), Text("B1", "Annual amount (today's dollars)"), Text("C1", "Start age"))), 30, 30, 14);
        workbookPart.Workbook.Save();
    }

    private static void AddWorksheet(WorkbookPart workbookPart, Sheets sheets, string name, uint id, IEnumerable<Row> rows, params double[] widths)
    {
        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        worksheetPart.Worksheet = new Worksheet(new Columns(widths.Select((width, index) => new Column { Min = (uint)index + 1, Max = (uint)index + 1, Width = width, CustomWidth = true })), new SheetData(rows));
        sheets.Append(new Sheet { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = id, Name = name });
    }

    private static Cell Text(string reference, string value) => new() { CellReference = reference, DataType = CellValues.InlineString, InlineString = new InlineString(new Text(value)) };
    // This overload exists solely to be un-callable. Without it, Number("B5", someInt, DecimalStyleIndex)
    // would bind to the (double, uint) overload via int->double widening and silently reintroduce #69.
    // Making the (int, uint) shape a compile error (error: true) forces every integer cell through the
    // IntegerFormat overload, so the guarantee holds at compile time rather than merely by convention.
    [Obsolete("An int cell must use IntegerFormat, never a raw style index (issue #69).", error: true)]
    private static Cell Number(string reference, int value, uint style) =>
        throw new InvalidOperationException();

    private static Cell Number(string reference, int value, IntegerFormat format) =>
        Number(reference, (double)value, WorkbookStyles.StyleIndexFor(format));

    // A non-finite result is a legitimate outcome (an unreachable target), but "Infinity" inside a
    // numeric cell is not a number Excel can read. Emit the same wording the apps show on screen.
    private static Cell Number(string reference, double value, uint style) => double.IsFinite(value)
        ? new() { CellReference = reference, StyleIndex = style, CellValue = new CellValue(value.ToString(CultureInfo.InvariantCulture)) }
        : Text(reference, WorkbookValues.Unreachable);
    private static void AddStyles(WorkbookPart workbookPart) => WorkbookStyles.Apply(workbookPart);
}