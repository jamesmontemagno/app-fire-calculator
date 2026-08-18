using System.Text.Json;

using MyFireNumber.Core.Calculations;

namespace MyFireNumber.Tests.Calculations;

/// <summary>
/// Drives <see cref="FinancialCalculator"/> from <c>shared/parity/fire-parity-cases.json</c>, the same
/// file the web suite reads.
///
/// <para><b>Why this exists.</b> Both platforms previously asserted against constants that had each
/// been copied from the other at some point in the past. That pins nothing: if one side drifts, its
/// own tests drift with it and stay green. This file and the Vitest suite read one artifact, so a
/// change on either platform that is not matched by the other turns a test red on that platform.</para>
///
/// <para><b>Where the numbers came from.</b> Not from this C# code, and not from running the shipped
/// TypeScript and pasting the output either. Every case carries a <c>derivation</c> field stating the
/// closed form or recurrence it was computed from, and the web suite's <c>fixtureSelfCheck.test.ts</c>
/// re-derives the algebraically checkable ones from independently written oracles. If a fixture value
/// is ever regenerated from an implementation, that self-check is what catches it.</para>
///
/// <para><b>Do not "fix" a failure here by editing the fixture.</b> A red test means the two
/// implementations disagree, or one of them disagrees with the algebra. Find out which before
/// touching a number.</para>
///
/// <para>Argument order deliberately does not travel in the fixture: the web engine takes eight
/// positional arguments for Coast FIRE while this side takes a single <see cref="FireInputs"/>
/// record. Inputs are named semantically and each platform adapts them locally, so a positional
/// mismatch cannot silently produce a plausible-looking wrong answer.</para>
/// </summary>
public class SharedParityFixtureTests
{
    /// <summary>
    /// Rounded currency and count fields are compared exactly; both platforms round them the same way,
    /// so any difference at all is drift.
    /// </summary>
    private const double ExactTolerance = 0.0;

    /// <summary>
    /// Unrounded ratios carry double accumulation from two different runtimes. 1e-9 is far tighter
    /// than any real formula difference and far looser than IEEE noise over these magnitudes.
    /// </summary>
    private const double RatioTolerance = 1e-9;

    /// <summary>
    /// Unrounded currency accumulates over as many as 60 compounding steps on values above $1M, so the
    /// absolute error budget scales with the magnitude rather than being a flat epsilon.
    /// </summary>
    private const double CurrencyTolerance = 1e-6;

    private static readonly IReadOnlyList<ParityCase> Cases = LoadCases();

    public static TheoryData<string> FireCaseIds => IdsOfKind("fire");

    public static TheoryData<string> DebtCaseIds => IdsOfKind("debt");

    public static TheoryData<string> WithdrawalCaseIds => IdsOfKind("withdrawal");

    public static TheoryData<string> InvestmentCaseIds => IdsOfKind("investment");

    public static TheoryData<string> HealthcareCaseIds => IdsOfKind("healthcare");

    public static TheoryData<string> DeferredCaseIds => IdsOfKind("deferred");

    [Fact]
    public void FixtureLoads_AndEveryCaseDocumentsItsDerivation()
    {
        Assert.NotEmpty(Cases);

        foreach (var parityCase in Cases)
        {
            Assert.False(string.IsNullOrWhiteSpace(parityCase.Id));
            Assert.False(string.IsNullOrWhiteSpace(parityCase.Kind));

            // A case without a stated derivation is a screenshot of whatever the code did that day.
            // The review surface for this fixture is that every number can be traced to an argument.
            Assert.False(
                string.IsNullOrWhiteSpace(parityCase.Derivation),
                $"Case '{parityCase.Id}' has no derivation. Every fixture value must state how it was obtained.");
        }

        Assert.Equal(Cases.Select(c => c.Id).Distinct().Count(), Cases.Count);
    }

    [Theory]
    [MemberData(nameof(FireCaseIds))]
    public void StandardFire_MatchesSharedFixture(string caseId)
    {
        var parityCase = Case(caseId);
        var inputs = ToFireInputs(parityCase.Inputs);
        var expected = parityCase.Expected;

        var result = FinancialCalculator.CalculateStandardFire(inputs);

        Assert.Equal(Number(expected, "fireNumber"), result.FireNumber, ExactTolerance);
        Assert.Equal(Number(expected, "yearsToFire"), result.YearsToFire, ExactTolerance);
        Assert.Equal(Number(expected, "fireAge"), result.FireAge, ExactTolerance);
        Assert.Equal(Number(expected, "coastFireNumber"), result.CoastFireNumber, ExactTolerance);
        Assert.Equal(Number(expected, "savingsRate"), result.SavingsRate, RatioTolerance);
        Assert.Equal(Number(expected, "monthlyContribution"), result.MonthlyContribution, CurrencyTolerance);
        Assert.Equal((int)Number(expected, "projectionCount"), result.Projections.Count);

        foreach (var sample in expected.GetProperty("projectionSamples").EnumerateArray())
        {
            var index = sample.GetProperty("year").GetInt32();
            var point = result.Projections[index];

            Assert.Equal(sample.GetProperty("age").GetDouble(), point.Age, ExactTolerance);
            Assert.Equal(sample.GetProperty("portfolio").GetDouble(), point.Portfolio, ExactTolerance);
            Assert.Equal(sample.GetProperty("inflationAdjusted").GetDouble(), point.InflationAdjusted, ExactTolerance);
            Assert.Equal(sample.GetProperty("totalContributions").GetDouble(), point.TotalContributions, ExactTolerance);
            Assert.Equal(sample.GetProperty("contributions").GetDouble(), point.Contributions, CurrencyTolerance);
        }
    }

    /// <summary>
    /// Issue #46 was the headline FIRE age and the projection chart disagreeing: the number on the card
    /// said one year and the line crossed the target in another. The two are computed by different code
    /// paths, so this asserts the relationship between them rather than either one's value.
    /// </summary>
    [Theory]
    [MemberData(nameof(FireCaseIds))]
    public void HeadlineFireAge_MatchesWhereTheProjectionCrossesTheTarget(string caseId)
    {
        var parityCase = Case(caseId);
        var result = FinancialCalculator.CalculateStandardFire(ToFireInputs(parityCase.Inputs));

        if (double.IsInfinity(result.YearsToFire))
        {
            // Unreachable is a legitimate answer, and it means no crossing exists to compare against.
            // What must hold is that the projection genuinely never gets there.
            Assert.All(result.Projections, point => Assert.True(point.InflationAdjusted < result.FireNumber));
            return;
        }

        var years = result.YearsToFire;

        if (years <= 0)
        {
            Assert.True(result.Projections[0].InflationAdjusted >= result.FireNumber);
            return;
        }

        var before = (int)Math.Floor(years);
        var after = (int)Math.Ceiling(years);

        if (after >= result.Projections.Count)
        {
            // fire-degenerate-zero-real-return reaches its target in year 60, but the projection series
            // is capped at 50 points, so the crossing is off the end of the chart and cannot be
            // observed here. Documented in that case's derivation.
            return;
        }

        Assert.True(
            result.Projections[before].InflationAdjusted < result.FireNumber,
            $"Case '{caseId}': projection at year {before} already met the target, so the headline age is late.");

        if (before != after)
        {
            Assert.True(
                result.Projections[after].InflationAdjusted >= result.FireNumber,
                $"Case '{caseId}': projection at year {after} had not met the target, so the headline age is early.");
        }
    }

    [Theory]
    [MemberData(nameof(FireCaseIds))]
    public void ReverseFire_MatchesSharedFixture(string caseId)
    {
        var parityCase = Case(caseId);
        var expected = parityCase.Expected.GetProperty("reverse");

        var result = FinancialCalculator.CalculateReverseFire(ToFireInputs(parityCase.Inputs));

        // ExactTolerance is 0.0 deliberately. A currency tolerance would also hide the raw-vs-rounded
        // gap this pins (issue #75): on the non-default cases 70000/0.035 is 1999999.9999999998, so
        // anything looser than exact equality passes with the rounding removed.
        Assert.Equal(Number(expected, "fireNumber"), result.FireNumber, ExactTolerance);
        // Reverse must solve for the same target the forward calculators report, not its own.
        Assert.Equal(Number(parityCase.Expected, "fireNumber"), result.FireNumber, ExactTolerance);
        Assert.Equal(Number(expected, "requiredAnnualSavings"), result.RequiredAnnualSavings, CurrencyTolerance);
        Assert.Equal(Number(expected, "requiredMonthlySavings"), result.RequiredMonthlySavings, CurrencyTolerance);
        Assert.Equal(Number(expected, "currentWillGrowTo"), result.CurrentWillGrowTo, ExactTolerance);
        Assert.Equal(expected.GetProperty("alreadyAchievable").GetBoolean(), result.AlreadyAchievable);
    }

    [Theory]
    [MemberData(nameof(DebtCaseIds))]
    public void DebtPayoff_MatchesSharedFixture(string caseId)
    {
        var parityCase = Case(caseId);
        var inputs = parityCase.Inputs;
        var expected = parityCase.Expected;

        var debts = inputs.GetProperty("debts").EnumerateArray()
            .Select(d => new DebtItem(
                d.GetProperty("id").GetString()!,
                d.GetProperty("name").GetString()!,
                d.GetProperty("balance").GetDouble(),
                d.GetProperty("rate").GetDouble(),
                d.GetProperty("minPayment").GetDouble()))
            .ToList();

        var monthlyPayment = inputs.GetProperty("monthlyPayment").GetDouble();
        var extraPayment = inputs.GetProperty("extraPayment").GetDouble();
        var strategy = inputs.GetProperty("strategy").GetString();

        var result = strategy == "avalanche"
            ? FinancialCalculator.CalculateAvalanchePayoff(debts, monthlyPayment, extraPayment)
            : FinancialCalculator.CalculateSnowballPayoff(debts, monthlyPayment, extraPayment);

        Assert.Equal((int)Number(expected, "totalMonths"), result.TotalMonths);
        Assert.Equal(Number(expected, "totalInterest"), result.TotalInterest, ExactTolerance);
        Assert.Equal(Number(expected, "totalPrincipal"), result.TotalPrincipal, ExactTolerance);
        Assert.Equal(Number(expected, "monthlyPayment"), result.MonthlyPayment, CurrencyTolerance);

        Assert.Equal(
            expected.GetProperty("payoffOrder").EnumerateArray().Select(e => e.GetString()).ToArray(),
            result.PayoffOrder.ToArray());

        // Month one must be charged interest exactly once. Charging it twice is what made a 25 month
        // payoff report as 34 months before #45, and a rounded total is too coarse to catch that.
        //
        // The fixture stores the exact arithmetic value (10000 * 0.20/12 = 166.666...), while both
        // implementations round the per-month projection field to whole dollars. Comparing through the
        // same rounding keeps this a parity assertion rather than a rounding assertion; the doubled
        // charge it guards against would read 333, which this still catches. The unrounded arithmetic
        // is pinned exactly in FirstMonthInterest_IsChargedExactlyOnce below.
        var expectedFirstMonthInterest = Number(expected, "firstMonthInterest");
        Assert.Equal(Math.Round(expectedFirstMonthInterest), result.Projections[0].InterestPaid, ExactTolerance);

        // Exact invariant: you repay precisely what you borrowed, no more and no less. Independent of
        // strategy, budget and rate, so it holds for every case without a fixture number.
        Assert.Equal(debts.Sum(d => d.Balance), result.TotalPrincipal, ExactTolerance);

        // The per-month projection fields are each rounded to whole dollars, so their sum can differ
        // from the reported total by up to half a dollar per month plus half a dollar for the total's
        // own rounding. That derived bound is the correct tolerance here — a genuine accounting error
        // would be off by far more than a rounding budget.
        var roundingBudget = 0.5 * (result.TotalMonths + 1);
        Assert.Equal(result.TotalInterest, result.Projections.Sum(p => p.InterestPaid), roundingBudget);
        Assert.Equal(result.TotalPrincipal, result.Projections.Sum(p => p.PrincipalPaid), roundingBudget);
    }

    /// <summary>
    /// Pins the month-one interest to exact arithmetic with no rounding in the way. A $12,000 balance
    /// at 20% APR owes exactly 12,000 * 0.20/12 = $200 in its first month, which is a whole number, so
    /// the projection's rounding cannot hide a discrepancy. Charging interest twice — the #45 bug —
    /// would read $400 here.
    /// </summary>
    [Fact]
    public void FirstMonthInterest_IsChargedExactlyOnce()
    {
        var debts = new[] { new DebtItem("card", "Card", 12_000, 0.20, 500) };

        var result = FinancialCalculator.CalculateSnowballPayoff(debts, 500);

        Assert.Equal(200.0, result.Projections[0].InterestPaid, ExactTolerance);
    }

    /// <summary>
    /// Avalanche targets the highest rate first, so it can never pay more total interest than snowball
    /// on the same debts and budget. The pre-#45 implementation inverted this, so it is asserted as a
    /// relationship rather than as two golden totals that could both drift together.
    /// </summary>
    [Theory]
    [MemberData(nameof(DebtCaseIds))]
    public void Avalanche_NeverCostsMoreInterestThanSnowball(string caseId)
    {
        var inputs = Case(caseId).Inputs;

        var debts = inputs.GetProperty("debts").EnumerateArray()
            .Select(d => new DebtItem(
                d.GetProperty("id").GetString()!,
                d.GetProperty("name").GetString()!,
                d.GetProperty("balance").GetDouble(),
                d.GetProperty("rate").GetDouble(),
                d.GetProperty("minPayment").GetDouble()))
            .ToList();

        var monthlyPayment = inputs.GetProperty("monthlyPayment").GetDouble();
        var extraPayment = inputs.GetProperty("extraPayment").GetDouble();

        var avalanche = FinancialCalculator.CalculateAvalanchePayoff(debts, monthlyPayment, extraPayment);
        var snowball = FinancialCalculator.CalculateSnowballPayoff(debts, monthlyPayment, extraPayment);

        Assert.True(
            avalanche.TotalInterest <= snowball.TotalInterest,
            $"Case '{caseId}': avalanche paid {avalanche.TotalInterest} interest against snowball's {snowball.TotalInterest}.");
        Assert.True(avalanche.TotalMonths <= snowball.TotalMonths);
    }

    [Theory]
    [MemberData(nameof(WithdrawalCaseIds))]
    public void Withdrawal_MatchesSharedFixture(string caseId)
    {
        var parityCase = Case(caseId);
        var inputs = parityCase.Inputs;
        var expected = parityCase.Expected;

        var result = FinancialCalculator.CalculateWithdrawal(
            inputs.GetProperty("portfolioValue").GetDouble(),
            inputs.GetProperty("withdrawalRate").GetDouble(),
            inputs.GetProperty("expectedReturn").GetDouble(),
            inputs.GetProperty("inflationRate").GetDouble(),
            inputs.GetProperty("retirementYears").GetInt32());

        Assert.Equal(Number(expected, "annualWithdrawal"), result.AnnualWithdrawal, CurrencyTolerance);
        Assert.Equal(Number(expected, "monthlyWithdrawal"), result.MonthlyWithdrawal, ExactTolerance);
        Assert.Equal(Number(expected, "portfolioLongevity"), result.PortfolioLongevity, ExactTolerance);
        Assert.Equal(Number(expected, "horizonFundedRatio"), result.HorizonFundedRatio, RatioTolerance);
        Assert.Equal(Number(expected, "endingBalance"), result.EndingBalance, ExactTolerance);

        var expectedRows = expected.GetProperty("rateAnalysis").EnumerateArray().ToList();
        Assert.Equal(expectedRows.Count, result.RateAnalysis.Count);

        for (var i = 0; i < expectedRows.Count; i++)
        {
            Assert.Equal(expectedRows[i].GetProperty("rate").GetDouble(), result.RateAnalysis[i].Rate, RatioTolerance);
            Assert.Equal(expectedRows[i].GetProperty("years").GetDouble(), result.RateAnalysis[i].Years, ExactTolerance);
            Assert.Equal(expectedRows[i].GetProperty("endBalance").GetDouble(), result.RateAnalysis[i].EndBalance, ExactTolerance);
        }

        // Drawing more can never make a portfolio last longer. Holds regardless of the fixture values.
        for (var i = 1; i < result.RateAnalysis.Count; i++)
        {
            Assert.True(result.RateAnalysis[i].Rate > result.RateAnalysis[i - 1].Rate);
            Assert.True(result.RateAnalysis[i].Years <= result.RateAnalysis[i - 1].Years);
        }
    }

    [Theory]
    [MemberData(nameof(InvestmentCaseIds))]
    public void InvestmentGrowth_MatchesSharedFixture(string caseId)
    {
        var parityCase = Case(caseId);
        var inputs = parityCase.Inputs;
        var expected = parityCase.Expected;

        var result = FinancialCalculator.CalculateInvestmentGrowth(new InvestmentGrowthInputs(
            StartingAmount: inputs.GetProperty("startingAmount").GetDouble(),
            ContributionAmount: inputs.GetProperty("contributionAmount").GetDouble(),
            ContributionFrequency: inputs.GetProperty("contributionFrequency").GetString() switch
            {
                "monthly" => ContributionFrequency.Monthly,
                "yearly" => ContributionFrequency.Yearly,
                var other => throw new FormatException($"Unrecognised contributionFrequency '{other}'."),
            },
            YearsInvesting: inputs.GetProperty("yearsInvesting").GetInt32(),
            ExpectedReturn: inputs.GetProperty("expectedReturn").GetDouble(),
            InflationRate: inputs.GetProperty("inflationRate").GetDouble(),
            AnnualIncome: inputs.GetProperty("annualIncome").GetDouble(),
            CurrentAge: inputs.GetProperty("currentAge").GetDouble(),
            ContributionGrowth: ToContributionGrowth(inputs)));

        Assert.Equal(Number(expected, "annualContribution"), result.AnnualContribution, CurrencyTolerance);
        Assert.Equal(Number(expected, "monthlyContribution"), result.MonthlyContribution, CurrencyTolerance);
        Assert.Equal(Number(expected, "savingsRate"), result.SavingsRate, RatioTolerance);
        Assert.Equal(Number(expected, "finalNominalBalance"), result.FinalNominalBalance, 1e-6);
        Assert.Equal(Number(expected, "finalInflationAdjustedBalance"), result.FinalInflationAdjustedBalance, 1e-6);
        Assert.Equal(Number(expected, "totalInvested"), result.TotalInvested, 1e-6);
        Assert.Equal(Number(expected, "totalGrowth"), result.TotalGrowth, 1e-6);
        Assert.Equal(Number(expected, "inflationImpact"), result.InflationImpact, 1e-6);

        // Definitional identities that hold for any inputs at all.
        Assert.Equal(result.FinalNominalBalance - result.TotalInvested, result.TotalGrowth, 1e-6);
        Assert.Equal(
            result.FinalNominalBalance - result.FinalInflationAdjustedBalance,
            result.InflationImpact,
            1e-6);
    }

    [Theory]
    [MemberData(nameof(HealthcareCaseIds))]
    public void HealthcareGap_MatchesSharedFixture(string caseId)
    {
        var parityCase = Case(caseId);
        var inputs = parityCase.Inputs;
        var expected = parityCase.Expected;

        var result = FinancialCalculator.CalculateHealthcareGap(new HealthcareGapInputs(
            CurrentAge: inputs.GetProperty("currentAge").GetInt32(),
            EarlyRetirementAge: inputs.GetProperty("earlyRetirementAge").GetInt32(),
            MedicareAge: 65,
            MonthlyPremium: inputs.GetProperty("monthlyPremium").GetDouble(),
            AnnualDeductible: inputs.GetProperty("annualDeductible").GetDouble(),
            AnnualOutOfPocket: inputs.GetProperty("annualOutOfPocket").GetDouble(),
            InflationRate: inputs.GetProperty("inflationRate").GetDouble()));

        Assert.Equal((int)Number(expected, "gapYears"), result.GapYears);
        Assert.Equal(Number(expected, "annualCost"), result.AnnualCost, CurrencyTolerance);
        Assert.Equal(Number(expected, "totalCost"), result.TotalCost, ExactTolerance);
        Assert.Equal(Number(expected, "avgAnnualCost"), result.AverageAnnualCost, ExactTolerance);

        Assert.Equal(result.GapYears, result.YearlyBreakdown.Count);
    }

    /// <summary>
    /// Deferred-compensation cases exist because of issue #63, where the two platforms rounded a
    /// negative <c>surplus</c> with different midpoint rules and then classified funded/shortfall from
    /// that rounded value. The result was a categorical disagreement — web said "fully funded, never
    /// falls short", this side said "fails at 60" — from identical inputs, and no shared case could
    /// catch it because none produced a negative surplus.
    ///
    /// <para>Each case asserts the surplus of every projected year, not just the headline, so the
    /// displayed figure and the verdict derived from it are both pinned across platforms.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(DeferredCaseIds))]
    public void DeferredCompensation_MatchesSharedFixture(string caseId)
    {
        var parityCase = Case(caseId);
        var expected = parityCase.Expected;

        var result = DeferredCompensationCalculator.Calculate(ToDeferredInputs(parityCase.Inputs));

        Assert.Equal((int)Number(expected, "projectionCount"), result.Projections.Count);
        Assert.Equal(Number(expected, "currentBalance"), result.CurrentBalance, ExactTolerance);
        Assert.Equal(Number(expected, "balanceAtSemiRetirement"), result.BalanceAtSemiRetirement, ExactTolerance);
        Assert.Equal(Number(expected, "endingBalance"), result.EndingBalance, ExactTolerance);
        Assert.Equal(Number(expected, "firstYearIncome"), result.FirstYearIncome, ExactTolerance);
        Assert.Equal(Number(expected, "firstYearSurplus"), result.FirstYearSurplus, ExactTolerance);
        Assert.Equal((int)Number(expected, "retirementYears"), result.RetirementYears);
        Assert.Equal((int)Number(expected, "fundedYears"), result.FundedYears);
        Assert.Equal((int)Number(expected, "yearsFullyCovered"), result.YearsFullyCovered);

        // Null is an answer here — "the plan never falls short" — not a missing value, so it is
        // asserted rather than skipped.
        var expectedShortfallAge = expected.GetProperty("firstShortfallAge");
        Assert.Equal(
            expectedShortfallAge.ValueKind == JsonValueKind.Null ? null : expectedShortfallAge.GetInt32(),
            result.FirstShortfallAge);

        foreach (var sample in expected.GetProperty("annualSamples").EnumerateArray())
        {
            var age = sample.GetProperty("age").GetInt32();
            var point = Assert.Single(result.Projections, p => p.Age == age);

            Assert.Equal(Number(sample, "totalIncome"), point.TotalIncome, ExactTolerance);
            Assert.Equal(Number(sample, "expenses"), point.Expenses, ExactTolerance);
            Assert.Equal(Number(sample, "surplus"), point.Surplus, ExactTolerance);

            // Read with the same hard get as every other field: a sample that omits it throws rather
            // than quietly asserting nothing about how far the plan exceeded its withdrawal policy.
            Assert.Equal(
                Number(sample, "policyExcessWithdrawals"),
                point.PolicyExcessWithdrawals,
                ExactTolerance);

            // Assert.Equal treats -0.0 and 0.0 as equal, so the line above cannot catch a negative
            // zero. It has to be checked outright: negative zero silently satisfying `>= 0` while
            // formatting as "-$0" is the mechanism that made #63 severe.
            Assert.False(double.IsNegative(point.Surplus) && point.Surplus == 0);
        }
    }

    // ---- fixture plumbing ------------------------------------------------------------------

    private sealed record ParityCase(string Id, string Kind, string Derivation, JsonElement Inputs, JsonElement Expected);

    private static ParityCase Case(string id) => Cases.Single(c => c.Id == id);

    private static TheoryData<string> IdsOfKind(string kind)
    {
        var data = new TheoryData<string>();
        foreach (var parityCase in Cases.Where(c => c.Kind == kind))
        {
            data.Add(parityCase.Id);
        }

        return data;
    }

    private static IReadOnlyList<ParityCase> LoadCases()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "SharedParity", "fire-parity-cases.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Shared parity fixture not found at '{path}'. It is copied from shared/parity by MyFireNumber.Tests.csproj.",
                path);
        }

        // Held as JsonDocument rather than deserialized into records so that the "Infinity" sentinel and
        // the differently-shaped inputs per kind stay readable without a converter per case type.
        var document = JsonDocument.Parse(File.ReadAllText(path));

        return document.RootElement.GetProperty("cases").EnumerateArray()
            .Select(c => new ParityCase(
                c.GetProperty("id").GetString()!,
                c.GetProperty("kind").GetString()!,
                c.GetProperty("derivation").GetString()!,
                c.GetProperty("inputs"),
                c.GetProperty("expected")))
            .ToList();
    }

    /// <summary>
    /// JSON has no literal for infinity, and emitting a bare <c>Infinity</c> token produces a file that
    /// <c>JSON.parse</c> rejects, so unreachable results are stored as the string "Infinity". These are
    /// real, correct answers — the target is never reached — not placeholders for a missing value.
    /// </summary>
    private static double Number(JsonElement parent, string property)
    {
        var element = parent.GetProperty(property);

        return element.ValueKind switch
        {
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.String => element.GetString() switch
            {
                "Infinity" => double.PositiveInfinity,
                "-Infinity" => double.NegativeInfinity,
                "NaN" => double.NaN,
                var other => throw new FormatException($"Unrecognised non-finite sentinel '{other}' for '{property}'."),
            },
            _ => throw new FormatException($"Expected a number for '{property}' but found {element.ValueKind}."),
        };
    }

    private static ContributionGrowth ToContributionGrowth(JsonElement inputs)
    {
        // Deliberately strict. A silent fallback would let a typo in the fixture quietly test the
        // wrong contribution model and still pass.
        if (!inputs.TryGetProperty("contributionGrowth", out var growth))
        {
            throw new FormatException("Case is missing contributionGrowth.");
        }

        return growth.GetString() switch
        {
            "inflation" => ContributionGrowth.Inflation,
            "flat" => ContributionGrowth.Flat,
            var other => throw new FormatException($"Unrecognised contributionGrowth '{other}'."),
        };
    }

    /// <summary>
    /// Maps the fixture's semantic input names onto this platform's call shape. The web engine takes
    /// these same values as positional arguments in a different order; keeping the mapping here is what
    /// makes an argument-order mistake impossible to share between the two suites.
    /// </summary>
    private static FireInputs ToFireInputs(JsonElement inputs) => new(
        CurrentAge: inputs.GetProperty("currentAge").GetDouble(),
        RetirementAge: inputs.GetProperty("retirementAge").GetDouble(),
        CurrentSavings: inputs.GetProperty("currentSavings").GetDouble(),
        AnnualContribution: inputs.GetProperty("annualContribution").GetDouble(),
        AnnualIncome: inputs.GetProperty("annualIncome").GetDouble(),
        ExpectedReturn: inputs.GetProperty("expectedReturn").GetDouble(),
        InflationRate: inputs.GetProperty("inflationRate").GetDouble(),
        WithdrawalRate: inputs.GetProperty("withdrawalRate").GetDouble(),
        AnnualExpenses: inputs.GetProperty("annualExpenses").GetDouble(),
        ContributionGrowth: ToContributionGrowth(inputs));

    /// <summary>
    /// Maps the fixture's semantic input names onto this platform's deferred-compensation call shape.
    ///
    /// <para>Deliberately strict about the collections: they are read with <c>GetProperty</c> so a
    /// case that omits <c>accounts</c> or <c>incomeSources</c> throws rather than quietly testing an
    /// empty plan and passing. An empty array must be written out explicitly.</para>
    /// </summary>
    private static DeferredCompensationInputs ToDeferredInputs(JsonElement inputs) => new(
        CurrentAge: inputs.GetProperty("currentAge").GetInt32(),
        SemiRetirementAge: inputs.GetProperty("semiRetirementAge").GetInt32(),
        PlanThroughAge: inputs.GetProperty("planThroughAge").GetInt32(),
        AnnualExpenses: inputs.GetProperty("annualExpenses").GetDouble(),
        InflationRate: inputs.GetProperty("inflationRate").GetDouble(),
        Accounts: inputs.GetProperty("accounts").EnumerateArray().Select(ToRetirementAccount).ToArray(),
        IncomeSources: inputs.GetProperty("incomeSources").EnumerateArray().Select(ToIncomeSource).ToArray(),
        AdditionalExpenses: inputs.GetProperty("additionalExpenses").EnumerateArray().Select(ToExpense).ToArray(),
        WithdrawOnlyAfterRetirement: inputs.GetProperty("withdrawOnlyAfterRetirement").GetBoolean(),
        ReinvestSurplus: inputs.GetProperty("reinvestSurplus").GetBoolean(),
        // Pinned rather than defaulted to DateTime.Now.Year so the case is reproducible. No
        // expectation reads the calendar year: the platforms derive it differently, so it is covered
        // per-platform instead of pretending it is shared.
        CurrentYear: 2025);

    private static RetirementAccount ToRetirementAccount(JsonElement account) => new(
        Id: account.GetProperty("id").GetString()!,
        Name: account.GetProperty("name").GetString()!,
        Type: account.GetProperty("type").GetString() switch
        {
            "deferred" => RetirementAccountType.Deferred,
            "traditional" => RetirementAccountType.Traditional,
            "roth" => RetirementAccountType.Roth,
            "taxable" => RetirementAccountType.Taxable,
            "savings" => RetirementAccountType.Savings,
            "hsa" => RetirementAccountType.Hsa,
            "other" => RetirementAccountType.Other,
            var other => throw new FormatException($"Unrecognised account type '{other}'."),
        },
        Balance: account.GetProperty("balance").GetDouble(),
        AnnualContribution: account.GetProperty("annualContribution").GetDouble(),
        AnnualReturn: account.GetProperty("annualReturn").GetDouble(),
        AvailableAge: account.GetProperty("availableAge").GetInt32(),
        WithdrawalRate: account.GetProperty("withdrawalRate").GetDouble(),
        PayoutYears: account.GetProperty("payoutYears").GetInt32(),
        WithdrawalTaxRate: account.GetProperty("withdrawalTaxRate").GetDouble());

    /// <summary>
    /// The fixture carries a <c>type</c> on each income source because the web model requires one.
    /// This platform's record has no such field, which is exactly the kind of shape difference the
    /// per-platform adapters exist to absorb.
    /// </summary>
    private static RetirementIncomeSource ToIncomeSource(JsonElement source) => new(
        Id: source.GetProperty("id").GetString()!,
        Name: source.GetProperty("name").GetString()!,
        AnnualAmount: source.GetProperty("annualAmount").GetDouble(),
        StartAge: source.GetProperty("startAge").GetInt32(),
        EndAge: source.GetProperty("endAge").GetInt32(),
        AnnualGrowth: source.GetProperty("annualGrowth").GetDouble(),
        IsAfterTax: source.GetProperty("isAfterTax").GetBoolean(),
        TaxRate: source.GetProperty("taxRate").GetDouble());

    private static RetirementExpense ToExpense(JsonElement expense) => new(
        Id: expense.GetProperty("id").GetString()!,
        Name: expense.GetProperty("name").GetString()!,
        AnnualAmount: expense.GetProperty("annualAmount").GetDouble(),
        StartAge: expense.GetProperty("startAge").GetInt32(),
        EndAge: expense.GetProperty("endAge").GetInt32());
}
