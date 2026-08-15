namespace MyFireNumber.Core.Calculations;

public static class FinancialCalculator
{
    public const double LeanFireThreshold = 40_000;
    public const double FatFireThreshold = 100_000;

    /// <summary>Horizon used by the withdrawal-rate comparison before a plan is treated as open-ended.</summary>
    private const int RateAnalysisMaxYears = 50;

    private const int MaxProjectionYears = 100;

    /// <summary>
    /// Balance in today's dollars after <paramref name="years"/> of flat nominal contributions.
    /// Matches the year-by-year projection exactly at whole years, and interpolates between them.
    /// </summary>
    private static double FlatContributionRealBalance(
        double presentValue,
        double annualContribution,
        double expectedReturn,
        double inflationRate,
        double years)
    {
        var compoundFactor = Math.Pow(1 + expectedReturn, years);
        var nominal = expectedReturn == 0
            ? presentValue + (annualContribution * years)
            : (presentValue * compoundFactor) + (annualContribution * ((compoundFactor - 1) / expectedReturn));
        return nominal / Math.Pow(1 + inflationRate, years);
    }

    public static double FutureValue(double presentValue, double annualContribution, double rate, double years)
    {
        if (rate == 0)
        {
            return presentValue + (annualContribution * years);
        }

        var compoundFactor = Math.Pow(1 + rate, years);
        return (presentValue * compoundFactor) + (annualContribution * ((compoundFactor - 1) / rate));
    }

    public static double PresentValue(double futureValue, double rate, double years)
    {
        return years <= 0 ? futureValue : futureValue / Math.Pow(1 + rate, years);
    }

    public static double YearsToTarget(double presentValue, double annualContribution, double rate, double target)
    {
        if (presentValue >= target)
        {
            return 0;
        }

        if (rate == 0)
        {
            return annualContribution <= 0 ? double.PositiveInfinity : (target - presentValue) / annualContribution;
        }

        var numerator = annualContribution + (target * rate);
        var denominator = annualContribution + (presentValue * rate);
        if (denominator <= 0 || numerator <= denominator)
        {
            var years = 0;
            var current = presentValue;
            while (current < target && years < MaxProjectionYears)
            {
                current = (current * (1 + rate)) + annualContribution;
                years++;
            }

            return years >= MaxProjectionYears ? double.PositiveInfinity : years;
        }

        var result = Math.Log(numerator / denominator) / Math.Log(1 + rate);
        return result is < 0 or > MaxProjectionYears ? double.PositiveInfinity : result;
    }

    /// <summary>
    /// Years until the portfolio reaches a target expressed in today's dollars.
    /// This is the single source of truth for every headline FIRE age, and it is solved against the same
    /// path <see cref="GenerateProjections"/> draws, so the projection crossing always equals the headline.
    /// </summary>
    public static double YearsToFireTarget(
        double presentValue,
        double annualContribution,
        double expectedReturn,
        double inflationRate,
        double target,
        ContributionGrowth contributionGrowth = ContributionGrowth.Inflation)
    {
        if (presentValue >= target)
        {
            return 0;
        }

        if (contributionGrowth == ContributionGrowth.Inflation)
        {
            return YearsToTarget(presentValue, annualContribution, RealReturn(expectedReturn, inflationRate), target);
        }

        double BalanceAt(double years) =>
            FlatContributionRealBalance(presentValue, annualContribution, expectedReturn, inflationRate, years);

        double lower = 0;
        double upper = -1;
        for (var year = 1; year <= MaxProjectionYears; year++)
        {
            if (BalanceAt(year) >= target)
            {
                upper = year;
                lower = year - 1;
                break;
            }
        }

        if (upper < 0)
        {
            return double.PositiveInfinity;
        }

        for (var iteration = 0; iteration < 60; iteration++)
        {
            var midpoint = (lower + upper) / 2;
            if (BalanceAt(midpoint) >= target)
            {
                upper = midpoint;
            }
            else
            {
                lower = midpoint;
            }
        }

        return (lower + upper) / 2;
    }

    /// <summary>
    /// Nominal contribution paid at the end of year <paramref name="year"/> (1-based).
    /// With <see cref="ContributionGrowth.Inflation"/> the contribution keeps a constant purchasing power,
    /// which is what makes the deflated projection identical to the closed-form headline solution.
    /// </summary>
    public static double ContributionForYear(
        double annualContribution,
        double inflationRate,
        int year,
        ContributionGrowth contributionGrowth = ContributionGrowth.Inflation)
    {
        return contributionGrowth == ContributionGrowth.Inflation
            ? annualContribution * Math.Pow(1 + inflationRate, year)
            : annualContribution;
    }

    /// <summary>
    /// Generate projection points over time.
    /// <c>Portfolio</c> is in future (nominal) dollars and <c>InflationAdjusted</c> is the same portfolio in
    /// today's dollars. <paramref name="annualContribution"/> is stated in today's dollars.
    /// </summary>
    public static IReadOnlyList<ProjectionPoint> GenerateProjections(
        double currentAge,
        double currentSavings,
        double annualContribution,
        double expectedReturn,
        double inflationRate,
        int years,
        int startYear = 0,
        ContributionGrowth contributionGrowth = ContributionGrowth.Inflation)
    {
        var projections = new List<ProjectionPoint>();
        var portfolio = currentSavings;
        var totalContributions = currentSavings;
        var projectionStartYear = startYear == 0 ? DateTime.Now.Year : startYear;

        for (var year = 0; year <= years; year++)
        {
            projections.Add(new ProjectionPoint(
                currentAge + year,
                projectionStartYear + year,
                Round(portfolio),
                year == 0
                    ? currentSavings
                    : ContributionForYear(annualContribution, inflationRate, year, contributionGrowth),
                Round(totalContributions),
                Round(portfolio / Math.Pow(1 + inflationRate, year))));

            var nextContribution = ContributionForYear(annualContribution, inflationRate, year + 1, contributionGrowth);
            portfolio = (portfolio * (1 + expectedReturn)) + nextContribution;
            totalContributions += nextContribution;
        }

        return projections;
    }

    public static StandardFireResult CalculateStandardFire(FireInputs inputs)
    {
        var fireNumber = inputs.AnnualExpenses / inputs.WithdrawalRate;
        var realReturn = RealReturn(inputs.ExpectedReturn, inputs.InflationRate);
        var yearsToFire = YearsToFireTarget(inputs.CurrentSavings, inputs.AnnualContribution, inputs.ExpectedReturn, inputs.InflationRate, fireNumber, inputs.ContributionGrowth);
        var yearsToRetirement = Math.Max(0, inputs.RetirementAge - inputs.CurrentAge);
        var projectionYears = double.IsFinite(yearsToFire)
            ? Math.Min((int)Math.Ceiling(yearsToFire) + 10, 50)
            : 50;

        var fireAge = RoundToTenth(inputs.CurrentAge + yearsToFire);

        return new StandardFireResult(
            Round(fireNumber),
            RoundToTenth(yearsToFire),
            fireAge,
            GenerateProjections(inputs.CurrentAge, inputs.CurrentSavings, inputs.AnnualContribution, inputs.ExpectedReturn, inputs.InflationRate, projectionYears, inputs.ProjectionStartYear, inputs.ContributionGrowth),
            inputs.AnnualIncome > 0 ? inputs.AnnualContribution / inputs.AnnualIncome : 0,
            inputs.AnnualContribution / 12,
            Round(PresentValue(fireNumber, realReturn, yearsToRetirement)))
        {
            RetirementGoal = new RetirementGoalAssessment(inputs.RetirementAge, fireAge)
        };
    }

    public static CoastFireResult CalculateCoastFire(FireInputs inputs)
    {
        var fireNumber = inputs.AnnualExpenses / inputs.WithdrawalRate;
        var yearsToRetirement = Math.Max(0, inputs.RetirementAge - inputs.CurrentAge);
        var realReturn = RealReturn(inputs.ExpectedReturn, inputs.InflationRate);
        var coastNumber = PresentValue(fireNumber, realReturn, yearsToRetirement);
        var alreadyCoasting = inputs.CurrentSavings >= coastNumber;
        var yearsToCoast = alreadyCoasting
            ? 0
            : YearsToFireTarget(inputs.CurrentSavings, inputs.AnnualContribution, inputs.ExpectedReturn, inputs.InflationRate, coastNumber, inputs.ContributionGrowth);

        return new CoastFireResult(
            Round(coastNumber),
            RoundToTenth(yearsToCoast),
            alreadyCoasting,
            Round(fireNumber),
            GenerateProjections(inputs.CurrentAge, inputs.CurrentSavings, 0, inputs.ExpectedReturn, inputs.InflationRate, (int)yearsToRetirement + 10, inputs.ProjectionStartYear, inputs.ContributionGrowth),
            GenerateProjections(inputs.CurrentAge, inputs.CurrentSavings, inputs.AnnualContribution, inputs.ExpectedReturn, inputs.InflationRate, (int)yearsToRetirement + 10, inputs.ProjectionStartYear, inputs.ContributionGrowth));
    }

    public static LeanFireResult CalculateLeanFire(FireInputs inputs)
    {
        return new LeanFireResult(CalculateStandardFire(inputs), inputs.AnnualExpenses <= LeanFireThreshold, LeanFireThreshold);
    }

    public static FatFireResult CalculateFatFire(FireInputs inputs)
    {
        return new FatFireResult(CalculateStandardFire(inputs), inputs.AnnualExpenses >= FatFireThreshold, FatFireThreshold);
    }

    public static BaristaFireResult CalculateBaristaFire(FireInputs inputs, double partTimeAnnualIncome)
    {
        var fullFireNumber = inputs.AnnualExpenses / inputs.WithdrawalRate;
        var baristaNumber = Math.Max(0, inputs.AnnualExpenses - partTimeAnnualIncome) / inputs.WithdrawalRate;
        var yearsToBaristaFire = YearsToFireTarget(inputs.CurrentSavings, inputs.AnnualContribution, inputs.ExpectedReturn, inputs.InflationRate, baristaNumber, inputs.ContributionGrowth);
        var projectionYears = Math.Min((int)Math.Ceiling(yearsToBaristaFire) + 10, 50);

        return new BaristaFireResult(
            Round(baristaNumber),
            Round(fullFireNumber),
            RoundToTenth(yearsToBaristaFire),
            partTimeAnnualIncome,
            GenerateProjections(inputs.CurrentAge, inputs.CurrentSavings, inputs.AnnualContribution, inputs.ExpectedReturn, inputs.InflationRate, projectionYears, inputs.ProjectionStartYear, inputs.ContributionGrowth),
            Round(fullFireNumber - baristaNumber));
    }

    /// <summary>
    /// Projects a single deterministic drawdown path at a fixed return with inflation-adjusted
    /// withdrawals. This is not a historical or Monte Carlo simulation, so it cannot produce a
    /// probability of success. <see cref="WithdrawalResult.PortfolioLongevity"/> and every
    /// <see cref="WithdrawalRateAnalysis.Years"/> use the same convention: full years funded while
    /// a positive balance remained, so the headline and the comparison table always agree.
    /// </summary>
    public static WithdrawalResult CalculateWithdrawal(double portfolioValue, double withdrawalRate, double expectedReturn, double inflationRate, int retirementYears)
    {
        var annualWithdrawal = portfolioValue * withdrawalRate;
        var balance = portfolioValue;
        var year = 0;
        var adjustedWithdrawal = annualWithdrawal;
        var projections = new List<WithdrawalProjection>();
        while (balance > 0 && year <= retirementYears)
        {
            projections.Add(new WithdrawalProjection(year, Round(balance), Round(adjustedWithdrawal)));
            balance = (balance * (1 + expectedReturn)) - adjustedWithdrawal;
            adjustedWithdrawal *= 1 + inflationRate;
            year++;
        }

        var portfolioLongevity = Math.Max(0, year - 1);
        var rateAnalysis = new[] { 0.03, 0.035, 0.04, 0.045, 0.05 }
            .Select(rate => CalculateRateAnalysis(portfolioValue, rate, expectedReturn, inflationRate))
            .ToArray();

        return new WithdrawalResult(
            portfolioLongevity,
            retirementYears <= 0 || portfolioLongevity >= retirementYears ? 1 : (double)portfolioLongevity / retirementYears,
            Round(annualWithdrawal),
            Round(annualWithdrawal / 12),
            Math.Max(0, projections.LastOrDefault()?.Balance ?? 0),
            projections,
            rateAnalysis);
    }

    public static DebtPayoffResult CalculateSnowballPayoff(IReadOnlyList<DebtItem> debts, double totalMonthlyPayment, double extraPayment = 0)
    {
        return CalculateDebtPayoff(debts.OrderBy(debt => debt.Balance), totalMonthlyPayment, extraPayment);
    }

    public static DebtPayoffResult CalculateAvalanchePayoff(IReadOnlyList<DebtItem> debts, double totalMonthlyPayment, double extraPayment = 0)
    {
        return CalculateDebtPayoff(debts.OrderByDescending(debt => debt.Rate), totalMonthlyPayment, extraPayment);
    }

    public static DebtTimelineResult? CalculateDebtPayoffByTimeline(IReadOnlyList<DebtItem> debts, int targetMonths, bool useSnowball, double extraPayment = 0)
    {
        if (targetMonths <= 0 || debts.Count == 0)
        {
            return null;
        }

        var minimumPayment = debts.Sum(debt => debt.MinimumPayment);
        var maximumPayment = debts.Sum(debt => debt.Balance);
        var requiredPayment = minimumPayment;
        DebtPayoffResult? result = null;

        for (var iteration = 0; iteration < 30; iteration++)
        {
            var testPayment = (minimumPayment + maximumPayment) / 2;
            var testResult = useSnowball
                ? CalculateSnowballPayoff(debts, testPayment, extraPayment)
                : CalculateAvalanchePayoff(debts, testPayment, extraPayment);

            if (testResult.TotalMonths <= targetMonths)
            {
                requiredPayment = testPayment;
                result = testResult;
                maximumPayment = testPayment;
            }
            else
            {
                minimumPayment = testPayment;
            }

            if (Math.Abs(maximumPayment - minimumPayment) < 1)
            {
                break;
            }
        }

        return result is null ? null : new DebtTimelineResult(Round(requiredPayment), result);
    }

    public static ReverseFireResult CalculateReverseFire(FireInputs inputs)
    {
        var yearsToFire = Math.Max(1, inputs.RetirementAge - inputs.CurrentAge);
        var fireNumber = inputs.AnnualExpenses / inputs.WithdrawalRate;
        var realReturn = RealReturn(inputs.ExpectedReturn, inputs.InflationRate);

        // Existing savings deflate the same way in both models, so this is always in today's dollars.
        var futureValueOfCurrent = inputs.CurrentSavings * Math.Pow(1 + realReturn, yearsToFire);

        double requiredAnnualSavings;
        if (futureValueOfCurrent >= fireNumber)
        {
            requiredAnnualSavings = 0;
        }
        else if (inputs.ContributionGrowth == ContributionGrowth.Inflation)
        {
            var compoundFactor = Math.Pow(1 + realReturn, yearsToFire);
            requiredAnnualSavings = realReturn == 0
                ? (fireNumber - inputs.CurrentSavings) / yearsToFire
                : ((fireNumber - futureValueOfCurrent) * realReturn) / (compoundFactor - 1);
        }
        else
        {
            // Flat nominal contributions: solve the deflated flat path for a constant nominal payment.
            var nominalTarget = fireNumber * Math.Pow(1 + inputs.InflationRate, yearsToFire);
            var compoundFactor = Math.Pow(1 + inputs.ExpectedReturn, yearsToFire);
            var nominalValueOfCurrent = inputs.CurrentSavings * compoundFactor;
            requiredAnnualSavings = inputs.ExpectedReturn == 0
                ? (nominalTarget - nominalValueOfCurrent) / yearsToFire
                : ((nominalTarget - nominalValueOfCurrent) * inputs.ExpectedReturn) / (compoundFactor - 1);
        }

        var safeAnnualSavings = Math.Max(0, requiredAnnualSavings);

        return new ReverseFireResult(
            fireNumber,
            yearsToFire,
            safeAnnualSavings,
            safeAnnualSavings / 12,
            GenerateProjections(inputs.CurrentAge, inputs.CurrentSavings, safeAnnualSavings, inputs.ExpectedReturn, inputs.InflationRate, (int)yearsToFire + 10, inputs.ProjectionStartYear, inputs.ContributionGrowth),
            futureValueOfCurrent >= fireNumber,
            Round(futureValueOfCurrent));
    }

    public static InvestmentGrowthResult CalculateInvestmentGrowth(InvestmentGrowthInputs inputs)
    {
        var annualContribution = inputs.ContributionFrequency == ContributionFrequency.Monthly
            ? inputs.ContributionAmount * 12
            : inputs.ContributionAmount;
        var savingsRate = inputs.AnnualIncome > 0 ? annualContribution / inputs.AnnualIncome : 0;
        var startYear = inputs.ProjectionStartYear == 0 ? DateTime.Now.Year : inputs.ProjectionStartYear;
        var nominalBalance = inputs.StartingAmount;
        var totalContributions = inputs.StartingAmount;
        var projections = new List<InvestmentProjectionPoint>
        {
            new(inputs.CurrentAge, startYear, 0, Round(nominalBalance), Round(nominalBalance), Round(totalContributions), 0)
        };

        for (var year = 1; year <= inputs.YearsInvesting; year++)
        {
            var contribution = ContributionForYear(annualContribution, inputs.InflationRate, year, inputs.ContributionGrowth);
            nominalBalance = (nominalBalance * (1 + inputs.ExpectedReturn)) + contribution;
            totalContributions += contribution;

            // One plan, two views: the nominal balance deflated to today's dollars.
            var inflationAdjusted = nominalBalance / Math.Pow(1 + inputs.InflationRate, year);
            projections.Add(new InvestmentProjectionPoint(inputs.CurrentAge + year, startYear + year, year, Round(nominalBalance), Round(inflationAdjusted), Round(totalContributions), contribution));
        }

        var finalInflationAdjustedBalance = nominalBalance / Math.Pow(1 + inputs.InflationRate, inputs.YearsInvesting);

        return new InvestmentGrowthResult(
            savingsRate,
            annualContribution,
            annualContribution / 12,
            nominalBalance,
            finalInflationAdjustedBalance,
            totalContributions,
            nominalBalance - totalContributions,
            nominalBalance - finalInflationAdjustedBalance,
            projections,
            SavingsCategory(savingsRate));
    }

    public static HealthcareGapResult CalculateHealthcareGap(HealthcareGapInputs inputs)
    {
        var gapYears = Math.Max(0, inputs.MedicareAge - inputs.EarlyRetirementAge);
        var annualCost = (inputs.MonthlyPremium * 12) + inputs.AnnualDeductible + inputs.AnnualOutOfPocket;
        var startYear = inputs.ProjectionStartYear == 0 ? DateTime.Now.Year : inputs.ProjectionStartYear;
        var totalCost = 0d;
        var yearlyBreakdown = new List<HealthcareYear>();

        for (var year = 0; year < gapYears; year++)
        {
            var multiplier = Math.Pow(1 + inputs.InflationRate, year);
            var cost = annualCost * multiplier;
            totalCost += cost;
            yearlyBreakdown.Add(new HealthcareYear(
                inputs.EarlyRetirementAge + year,
                startYear + (inputs.EarlyRetirementAge - inputs.CurrentAge) + year,
                Round(cost),
                Round(inputs.MonthlyPremium * 12 * multiplier),
                Round(inputs.AnnualDeductible * multiplier),
                Round(inputs.AnnualOutOfPocket * multiplier)));
        }

        return new HealthcareGapResult(
            gapYears,
            annualCost,
            Round(totalCost),
            gapYears > 0 ? Round(totalCost / gapYears) : 0,
            yearlyBreakdown);
    }

    /// <summary>
    /// Core debt payoff calculation logic.
    /// </summary>
    /// <remarks>
    /// Each month interest accrues exactly once per debt before any payment is applied, then the
    /// available budget pays minimums in priority order and any remainder goes to the highest
    /// priority debt as pure principal. Payments never exceed the available budget, so a budget
    /// that cannot cover the minimums results in growing balances instead of silent overpayment.
    /// This mirrors <c>calculateDebtPayoff</c> in the web app's <c>calculations.ts</c>.
    /// </remarks>
    private static DebtPayoffResult CalculateDebtPayoff(IEnumerable<DebtItem> debts, double totalMonthlyPayment, double extraPayment)
    {
        var remainingDebts = debts.Select(debt => new MutableDebt(debt)).ToList();
        var projections = new List<DebtPayoffMonth>();
        var payoffOrder = new List<string>();
        var debtMilestones = new List<DebtMilestone>();
        var totalPrincipal = remainingDebts.Sum(debt => debt.Balance);
        var cumulativePrincipal = 0d;
        var cumulativeInterest = 0d;
        var month = 0;

        while (remainingDebts.Any(debt => debt.CurrentBalance > 0) && month < 600)
        {
            month++;
            var monthlyBudget = totalMonthlyPayment + extraPayment;
            var monthPayments = 0d;
            var monthInterest = 0d;
            var paidOffThisMonth = new List<string>();

            // 1. Accrue interest exactly once per debt, before any payment is applied.
            foreach (var debt in remainingDebts)
            {
                if (debt.CurrentBalance <= 0)
                {
                    continue;
                }

                var interestCharge = debt.CurrentBalance * (debt.Rate / 12);
                debt.CurrentBalance += interestCharge;
                monthInterest += interestCharge;
            }

            // 2. Pay minimums in priority order, never spending more than the available budget.
            foreach (var debt in remainingDebts)
            {
                if (debt.CurrentBalance <= 0 || monthlyBudget <= 0)
                {
                    continue;
                }

                var payment = Math.Min(debt.MinimumPayment, Math.Min(debt.CurrentBalance, monthlyBudget));
                debt.CurrentBalance -= payment;
                monthlyBudget -= payment;
                monthPayments += payment;
                MarkPaidOff(debt, month, paidOffThisMonth, payoffOrder, debtMilestones);
            }

            // 3. Apply any remaining budget to the highest priority debt as pure principal.
            while (monthlyBudget > 0 && remainingDebts.FirstOrDefault(debt => debt.CurrentBalance > 0) is { } targetDebt)
            {
                var payment = Math.Min(monthlyBudget, targetDebt.CurrentBalance);
                targetDebt.CurrentBalance -= payment;
                monthlyBudget -= payment;
                monthPayments += payment;
                MarkPaidOff(targetDebt, month, paidOffThisMonth, payoffOrder, debtMilestones);
            }

            // Balances already include this month's interest, so principal is what is left of the payments.
            var monthPrincipal = monthPayments - monthInterest;

            cumulativePrincipal += monthPrincipal;
            cumulativeInterest += monthInterest;
            var totalBalance = remainingDebts.Sum(debt => debt.CurrentBalance);
            projections.Add(new DebtPayoffMonth(
                month,
                Round(totalBalance),
                Round(monthPrincipal),
                Round(monthInterest),
                Round(cumulativePrincipal),
                Round(cumulativeInterest),
                paidOffThisMonth,
                remainingDebts.Where(debt => debt.CurrentBalance > 0).Select(debt => new DebtBalance(debt.Name, Round(debt.CurrentBalance))).ToArray()));

            if (totalBalance <= 0)
            {
                break;
            }
        }

        return new DebtPayoffResult(month, Round(cumulativeInterest), Round(totalPrincipal), totalMonthlyPayment + extraPayment, projections, payoffOrder, debtMilestones);
    }

    private static WithdrawalRateAnalysis CalculateRateAnalysis(double portfolioValue, double rate, double expectedReturn, double inflationRate)
    {
        var balance = portfolioValue;
        var year = 0;
        var withdrawal = portfolioValue * rate;
        while (balance > 0 && year < RateAnalysisMaxYears)
        {
            balance = (balance * (1 + expectedReturn)) - withdrawal;
            withdrawal *= 1 + inflationRate;
            year++;
        }

        // Match PortfolioLongevity: count the full years funded while a positive balance remained.
        var yearsFunded = Math.Max(0, balance > 0 ? year : year - 1);
        return new WithdrawalRateAnalysis(rate, yearsFunded, Math.Max(0, Round(balance)));
    }

    private static void MarkPaidOff(MutableDebt debt, int month, ICollection<string> paidOffThisMonth, ICollection<string> payoffOrder, ICollection<DebtMilestone> debtMilestones)
    {
        if (debt.CurrentBalance > 0)
        {
            return;
        }

        debt.CurrentBalance = 0;
        if (paidOffThisMonth.Contains(debt.Name))
        {
            return;
        }

        paidOffThisMonth.Add(debt.Name);
        payoffOrder.Add(debt.Name);
        debtMilestones.Add(new DebtMilestone(month, debt.Name));
    }

    private static double RealReturn(double expectedReturn, double inflationRate)
    {
        return ((1 + expectedReturn) / (1 + inflationRate)) - 1;
    }

    private static string SavingsCategory(double savingsRate)
    {
        return savingsRate >= 0.5 ? "Extreme Saver"
            : savingsRate >= 0.3 ? "Aggressive Saver"
            : savingsRate >= 0.2 ? "Good Saver"
            : savingsRate >= 0.1 ? "Average Saver"
            : "Below Average";
    }

    private static double Round(double value)
    {
        return Math.Round(value, MidpointRounding.AwayFromZero);
    }

    private static double RoundToTenth(double value)
    {
        return double.IsInfinity(value) ? value : Math.Round(value * 10, MidpointRounding.AwayFromZero) / 10;
    }

    private sealed class MutableDebt
    {
        public MutableDebt(DebtItem debt)
        {
            Name = debt.Name;
            Balance = debt.Balance;
            Rate = debt.Rate;
            MinimumPayment = debt.MinimumPayment;
            CurrentBalance = debt.Balance;
        }

        public string Name { get; }

        public double Balance { get; }

        public double Rate { get; }

        public double MinimumPayment { get; }

        public double CurrentBalance { get; set; }
    }
}