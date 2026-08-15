using System.Globalization;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Exports;

namespace MyFireNumber.Tests.Exports;

/// <summary>
/// Guards issue #69: the exporters had no integer number format, so every whole-number field landed
/// on the "0.0" decimal style and a calendar year exported as "2026.0". These tests read the number
/// format actually resolved on the generated cell — the string a user opening the file would see —
/// rather than trusting the style-index constant the exporter passed.
///
/// The invariant is mechanical: an <c>int</c>-typed cell must carry an integer format, a <c>double</c>
/// must keep "0.0". Ages and calendar years use "0" (no thousands separator, so 2026 is not "2,026");
/// magnitudes such as month and year counts use "#,##0".
/// </summary>
public sealed class WorkbookNumberFormatTests : IDisposable
{
    private readonly string workbookPath = Path.Combine(Path.GetTempPath(), $"my-fire-number-format-{Guid.NewGuid():N}.xlsx");
    private static readonly DateTimeOffset GeneratedAt = new(2026, 8, 9, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void DebtPayoff_IntegerFieldsUseGroupedInteger_NotDecimal()
    {
        var draft = DebtPayoffDraft.Default with { Debts = [new DebtItem("card", "Credit card", 1_000, 0.1, 100)], MonthlyBudget = 500 };
        var result = FinancialCalculator.CalculateSnowballPayoff(draft.Debts, draft.MonthlyBudget);
        DebtPayoffWorkbook.Create(workbookPath, draft, result, GeneratedAt);

        Assert.Equal("#,##0", ResolveFormat(0, "B9"));  // TargetMonths (int)
        Assert.Equal("#,##0", ResolveFormat(1, "B5"));  // TotalMonths (int)
        Assert.Equal("#,##0", ResolveFormat(3, "A2"));  // DebtPayoffMonth.Month (int)
    }

    [Fact]
    public void StandardFire_YearIsPlainInteger_AgeStaysDecimal_YearsToFireStaysDecimal()
    {
        var draft = StandardFireDraft.Default;
        var result = FinancialCalculator.CalculateStandardFire(draft.ToFireInputs(2026));
        StandardFireWorkbook.Create(workbookPath, draft, result, GeneratedAt);

        // Draft ages on the Inputs sheet are int and were the omission the reviewer caught (#69) — they
        // must be plain integers, not "45.0".
        Assert.Equal("0", ResolveFormat(0, "B5"));      // StandardFireDraft.CurrentAge (int)
        Assert.Equal("0", ResolveFormat(0, "B6"));      // StandardFireDraft.RetirementAge (int)

        // A calendar year must have no thousands separator, or 2026 renders as "2,026".
        Assert.Equal("0", ResolveFormat(2, "B2"));      // ProjectionPoint.Year (int) -> "0"
        Assert.Equal("2026", ReadCellValue(2, "B2"));

        // ProjectionPoint.Age is double and genuinely fractional; it must not be forced to an integer.
        Assert.Equal("0.0", ResolveFormat(2, "A2"));    // ProjectionPoint.Age (double)
        Assert.Equal("0.0", ResolveFormat(1, "B6"));    // YearsToFire (double)
        // RetirementGoalAssessment.TargetRetirementAge is double, not int, so it stays decimal.
        Assert.Equal("0.0", ResolveFormat(1, "B11"));
    }

    [Fact]
    public void BaristaFire_DraftAgeIsPlainInteger()
    {
        var draft = BaristaFireDraft.Default;
        var result = FinancialCalculator.CalculateBaristaFire(draft.ToFireInputs(2026), draft.PartTimeAnnualIncome);
        BaristaFireWorkbook.Create(workbookPath, draft, result, GeneratedAt);

        Assert.Equal("0", ResolveFormat(0, "B5"));      // BaristaFireDraft.CurrentAge (int)
    }

    [Fact]
    public void CoastFire_DraftAgesArePlainIntegers()
    {
        var draft = CoastFireDraft.Default;
        var result = FinancialCalculator.CalculateCoastFire(draft.ToFireInputs(2026));
        CoastFireWorkbook.Create(workbookPath, draft, result, GeneratedAt);

        Assert.Equal("0", ResolveFormat(0, "B5"));      // CoastFireDraft.CurrentAge (int)
        Assert.Equal("0", ResolveFormat(0, "B6"));      // CoastFireDraft.RetirementAge (int)
    }

    [Fact]
    public void HealthcareGap_AgesAndYearsArePlainIntegers()
    {
        var draft = HealthcareGapDraft.Default;
        var result = FinancialCalculator.CalculateHealthcareGap(draft.ToInputs(2026));
        HealthcareGapWorkbook.Create(workbookPath, draft, result, GeneratedAt);

        Assert.Equal("0", ResolveFormat(0, "B5"));      // CurrentAge (int)
        Assert.Equal("0", ResolveFormat(0, "B6"));      // EarlyRetirementAge (int)
        Assert.Equal("0", ResolveFormat(0, "B7"));      // MedicareAge (int)
        Assert.Equal("0", ResolveFormat(2, "A2"));      // HealthcareYear.Age (int)
        Assert.Equal("0", ResolveFormat(2, "B2"));      // HealthcareYear.Year (int)
        // "Coverage gap years" is an integer duration despite the formula source.
        Assert.Equal("#,##0", ResolveFormat(1, "B5"));
    }

    [Fact]
    public void SavingsInvestment_YearsInvestingGrouped_AgeAndProjectionYearPlain()
    {
        var draft = SavingsInvestmentDraft.Default;
        var result = FinancialCalculator.CalculateInvestmentGrowth(draft.ToInputs(2026));
        SavingsInvestmentWorkbook.Create(workbookPath, draft, result, GeneratedAt);

        Assert.Equal("#,##0", ResolveFormat(0, "B8"));  // YearsInvesting (int magnitude)
        Assert.Equal("0", ResolveFormat(0, "B12"));     // CurrentAge (int)
        Assert.Equal("0", ResolveFormat(2, "B2"));      // InvestmentProjectionPoint.Year (int)
        Assert.Equal("0.0", ResolveFormat(2, "A2"));    // InvestmentProjectionPoint.Age (double)
    }

    [Fact]
    public void ReverseFire_AgesPlain_YearsToFireDurationGrouped()
    {
        var draft = ReverseFireDraft.Default;
        var result = FinancialCalculator.CalculateReverseFire(draft.ToFireInputs(2026));
        ReverseFireWorkbook.Create(workbookPath, draft, result, GeneratedAt);

        Assert.Equal("0", ResolveFormat(0, "B5"));      // CurrentAge (int)
        Assert.Equal("0", ResolveFormat(0, "B6"));      // TargetRetirementAge (int)
        Assert.Equal("#,##0", ResolveFormat(1, "B6"));  // "Years to FIRE" duration formula
        Assert.Equal("0", ResolveFormat(2, "B2"));      // ProjectionPoint.Year (int)
        Assert.Equal("0.0", ResolveFormat(2, "A2"));    // ProjectionPoint.Age (double)
    }

    [Fact]
    public void WithdrawalRate_RetirementYearsGrouped_ButProjectionYearStaysDecimal()
    {
        var draft = WithdrawalRateDraft.Default;
        var result = FinancialCalculator.CalculateWithdrawal(draft.PortfolioValue, draft.WithdrawalRate, draft.ExpectedReturn, draft.InflationRate, draft.RetirementYears);
        WithdrawalRateWorkbook.Create(workbookPath, draft, result, GeneratedAt);

        Assert.Equal("#,##0", ResolveFormat(0, "B9"));  // draft.RetirementYears (int magnitude)

        // The single most counterintuitive cell in the exporters: WithdrawalProjection.Year is declared
        // "double", not int (it is a 1-based retirement-year offset, not a calendar year), so it must
        // stay on the decimal format. Do not "fix" this to an integer format.
        Assert.Equal("0.0", ResolveFormat(2, "A2"));
        Assert.Equal("0.0", ResolveFormat(1, "B7"));    // PortfolioLongevity (double)
    }

    [Fact]
    public void DeferredCompensation_AgesPlain_YearCountsGrouped_ProjectionAgeIsInt()
    {
        var draft = DeferredCompensationDraft.Default;
        var result = DeferredCompensationCalculator.Calculate(draft.ToInputs(2026));
        DeferredCompensationWorkbook.Create(workbookPath, draft, result, GeneratedAt);

        Assert.Equal("0", ResolveFormat(0, "B5"));      // draft.CurrentAge (int)
        Assert.Equal("0", ResolveFormat(0, "B6"));      // SemiRetirementAge (int)
        Assert.Equal("0", ResolveFormat(0, "B7"));      // PlanThroughAge (int)
        Assert.Equal("#,##0", ResolveFormat(1, "B11")); // result.RetirementYears (int magnitude)
        // RetirementCashFlowPoint.Age is int (unlike ProjectionPoint.Age which is double), so plain.
        Assert.Equal("0", ResolveFormat(2, "A2"));      // point.Age (int)
        Assert.Equal("0", ResolveFormat(2, "B2"));      // point.Year (int)
    }

    /// <summary>
    /// Resolves the number format code applied to a cell by walking
    /// StyleIndex -> CellFormats -> NumberFormatId -> NumberingFormats. A missing StyleIndex means the
    /// General format, which we surface as an empty string so an unstyled cell fails an integer assertion.
    /// </summary>
    private string ResolveFormat(int sheetIndex, string cellReference)
    {
        using var document = SpreadsheetDocument.Open(workbookPath, false);
        var workbookPart = document.WorkbookPart ?? throw new InvalidOperationException("Workbook part was not created.");
        var cell = GetCell(workbookPart, sheetIndex, cellReference);

        if (cell.StyleIndex?.Value is not uint styleIndex)
        {
            return string.Empty;
        }

        var stylesheet = (workbookPart.WorkbookStylesPart ?? throw new InvalidOperationException("Styles part was not created.")).Stylesheet
            ?? throw new InvalidOperationException("Stylesheet was not created.");
        var cellFormat = (CellFormat)(stylesheet.CellFormats ?? throw new InvalidOperationException("Cell formats were not created."))
            .ElementAt((int)styleIndex);
        var numberFormatId = cellFormat.NumberFormatId?.Value ?? 0U;
        var numberingFormat = (stylesheet.NumberingFormats?.Elements<NumberingFormat>() ?? [])
            .FirstOrDefault(format => format.NumberFormatId?.Value == numberFormatId);
        return numberingFormat?.FormatCode?.Value ?? string.Empty;
    }

    private string ReadCellValue(int sheetIndex, string cellReference)
    {
        using var document = SpreadsheetDocument.Open(workbookPath, false);
        var workbookPart = document.WorkbookPart ?? throw new InvalidOperationException("Workbook part was not created.");
        var value = GetCell(workbookPart, sheetIndex, cellReference).CellValue?.Text ?? string.Empty;
        // Normalize so a stored "2026" and "2026.0" compare on numeric intent, not text formatting.
        return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed.ToString("0.##", CultureInfo.InvariantCulture)
            : value;
    }

    private static Cell GetCell(WorkbookPart workbookPart, int sheetIndex, string cellReference)
    {
        var sheet = (workbookPart.Workbook?.Sheets ?? throw new InvalidOperationException("Workbook sheets were not created."))
            .Elements<Sheet>()
            .ElementAt(sheetIndex);
        var relationshipId = sheet.Id?.Value ?? throw new InvalidOperationException("Worksheet relationship ID was not created.");
        var worksheetPart = (WorksheetPart)workbookPart.GetPartById(relationshipId);
        var worksheet = worksheetPart.Worksheet ?? throw new InvalidOperationException("Worksheet was not created.");
        return (Cell)Assert.Single(worksheet.Descendants<Cell>(), cell => cell.CellReference?.Value == cellReference);
    }

    public void Dispose()
    {
        if (File.Exists(workbookPath))
        {
            File.Delete(workbookPath);
        }
    }
}
