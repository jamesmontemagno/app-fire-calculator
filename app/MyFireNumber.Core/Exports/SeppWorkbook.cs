using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MyFireNumber.Core.Calculations;

namespace MyFireNumber.Core.Exports;

public static class SeppWorkbook
{
    public static void Create(string filePath, SeppDraft draft, SeppResult result, DateTimeOffset generatedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(result);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");
        if (File.Exists(filePath)) File.Delete(filePath);

        using var document = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook);
        var workbookPart = document.AddWorkbookPart();
        workbookPart.Workbook = new Workbook();
        WorkbookStyles.Apply(workbookPart);
        var sheets = workbookPart.Workbook.AppendChild(new Sheets());
        AddInputs(workbookPart, sheets, draft, generatedAt);
        AddResults(workbookPart, sheets, draft, result);
        AddProjection(workbookPart, sheets, result.For(draft.Method).Projections);
        workbookPart.Workbook.Save();
    }

    private static void AddInputs(WorkbookPart workbookPart, Sheets sheets, SeppDraft draft, DateTimeOffset generatedAt)
    {
        var account = draft.SelectedAccount;
        var rows = new List<Row>
        {
            RowOf(Text("A1", "72(t) / SEPP Inputs")),
            RowOf(Text("A2", "Generated UTC"), Text("B2", generatedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture))),
            RowOf(Text("A4", "Input"), Text("B4", "Value")),
            RowOf(Text("A5", "Account"), Text("B5", account.Name)),
            RowOf(Text("A6", "Account type"), Text("B6", account.Type.ToString())),
            RowOf(Text("A7", "Balance"), Number("B7", account.Balance, WorkbookStyles.CurrencyStyleIndex)),
            RowOf(Text("A8", "Expected return"), Number("B8", account.ExpectedReturn, WorkbookStyles.PercentageStyleIndex)),
            RowOf(Text("A9", "Birth date"), Text("B9", draft.BirthDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))),
            RowOf(Text("A10", "First payment date"), Text("B10", draft.FirstPaymentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))),
            RowOf(Text("A11", "Chosen interest rate"), Number("B11", draft.InterestRate, WorkbookStyles.PercentageStyleIndex)),
            RowOf(Text("A12", "Entered maximum rate"), Number("B12", draft.MaximumInterestRate, WorkbookStyles.PercentageStyleIndex)),
            RowOf(Text("A13", "Actuarial annuity factor"), draft.AnnuityFactor is double factor ? Number("B13", factor, WorkbookStyles.DecimalStyleIndex) : Text("B13", "Not entered")),
            RowOf(Text("A14", "Selected method"), Text("B14", MethodName(draft.Method)))
        };
        AddSheet(workbookPart, sheets, "Inputs", 1, rows, 30, 24);
    }

    private static void AddResults(WorkbookPart workbookPart, Sheets sheets, SeppDraft draft, SeppResult result)
    {
        var rows = new List<Row>
        {
            RowOf(Text("A1", "72(t) / SEPP Results")),
            RowOf(Text("A3", "Result"), Text("B3", "Value")),
            RowOf(Text("A4", "Starting age"), Number("B4", result.StartingAge, IntegerFormat.Plain)),
            RowOf(Text("A5", "Single Life factor"), Number("B5", result.LifeExpectancyFactor, WorkbookStyles.DecimalStyleIndex)),
            RowOf(Text("A6", "Required end date"), Text("B6", result.RequiredEndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))),
            RowOf(Text("A7", "Required annual payment years"), Number("B7", result.RequiredYears, IntegerFormat.Plain)),
            RowOf(Text("A8", "RMD first-year payment"), OptionalCurrency("B8", result.Rmd.AnnualPayment)),
            RowOf(Text("A9", "Fixed amortization payment"), OptionalCurrency("B9", result.Amortization.AnnualPayment)),
            RowOf(Text("A10", "Fixed annuitization payment"), OptionalCurrency("B10", result.Annuitization.AnnualPayment)),
            RowOf(Text("A12", "Important"), Text("B12", "Educational estimate only. Verify account eligibility, IRS rates, tables, timing, and payment amount with a qualified tax professional before beginning a SEPP series."))
        };
        AddSheet(workbookPart, sheets, "Results", 2, rows, 34, 90);
    }

    private static void AddProjection(WorkbookPart workbookPart, Sheets sheets, IReadOnlyList<SeppProjectionPoint> points)
    {
        var rows = new List<Row>
        {
            RowOf(Text("A1", "Payment year"), Text("B1", "Calendar year"), Text("C1", "Age"), Text("D1", "Starting balance"), Text("E1", "Payment"), Text("F1", "Ending balance"))
        };
        foreach (var point in points)
        {
            var row = point.YearNumber + 1;
            rows.Add(RowOf(
                Number($"A{row}", point.YearNumber, IntegerFormat.Plain),
                Number($"B{row}", point.CalendarYear, IntegerFormat.Plain),
                Number($"C{row}", point.Age, IntegerFormat.Plain),
                Number($"D{row}", point.StartingBalance, WorkbookStyles.CurrencyStyleIndex),
                Number($"E{row}", point.AnnualPayment, WorkbookStyles.CurrencyStyleIndex),
                Number($"F{row}", point.EndingBalance, WorkbookStyles.CurrencyStyleIndex)));
        }
        AddSheet(workbookPart, sheets, "Projection", 3, rows, 15, 16, 10, 20, 18, 20);
    }

    private static string MethodName(SeppMethod method) => method switch
    {
        SeppMethod.RequiredMinimumDistribution => "Required minimum distribution",
        SeppMethod.FixedAmortization => "Fixed amortization",
        SeppMethod.FixedAnnuitization => "Fixed annuitization",
        _ => method.ToString()
    };

    private static void AddSheet(WorkbookPart workbookPart, Sheets sheets, string name, uint id, IEnumerable<Row> rows, params double[] widths)
    {
        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        var columns = new Columns(widths.Select((width, index) => new Column
        {
            Min = (uint)index + 1,
            Max = (uint)index + 1,
            Width = width,
            CustomWidth = true
        }));
        worksheetPart.Worksheet = new Worksheet(columns, new SheetData(rows));
        sheets.Append(new Sheet { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = id, Name = name });
    }

    private static Row RowOf(params Cell[] cells) => new(cells);
    private static Cell Text(string reference, string value) => new()
    {
        CellReference = reference,
        DataType = CellValues.InlineString,
        InlineString = new InlineString(new Text(value))
    };
    private static Cell Number(string reference, double value, uint style) => new()
    {
        CellReference = reference,
        StyleIndex = style,
        CellValue = new CellValue(value.ToString(CultureInfo.InvariantCulture))
    };
    [Obsolete("An int cell must use IntegerFormat, never a raw style index.", error: true)]
    private static Cell Number(string reference, int value, uint style) =>
        throw new InvalidOperationException();
    private static Cell Number(string reference, int value, IntegerFormat format) =>
        Number(reference, (double)value, WorkbookStyles.StyleIndexFor(format));
    private static Cell OptionalCurrency(string reference, double? value) =>
        value is double amount ? Number(reference, amount, WorkbookStyles.CurrencyStyleIndex) : Text(reference, "Factor required");
}
