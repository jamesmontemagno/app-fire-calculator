namespace MyFireNumber.Core.Calculations;

public static class FinancialCalculator
{
    public const double LeanFireThreshold = 40_000;
    public const double FatFireThreshold = 100_000;

    /// <summary>Horizon used by the withdrawal-rate comparison before a plan is treated as open-ended.</summary>
    private const int RateAnalysisMaxYears = 50;

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
            while (current < target && years < 100)
            {
                current = (current * (1 + rate)) + annualContribution;
                years++;
            }

            return years >= 100 ? double.PositiveInfinity : years;
        }

        var result = Math.Log(numerator / denominator) / Math.Log(1 + rate);
        return result is < 0 or > 100 ? double.PositiveInfinity : result;
    }

    public static IReadOnlyList<ProjectionPoint> GenerateProjections(
        double currentAge,
        double currentSavings,
        double annualContribution,
        double expectedReturn,
        double inflationRate,
        int years,
        int startYear = 0)
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
                year == 0 ? currentSavings : annualContribution,
                Round(totalContributions),
                Round(portfolio / Math.Pow(1 + inflationRate, year))));

            portfolio = (portfolio * (1 + expectedReturn)) + annualContribution;
            totalContributions += annualContribution;
        }

        return projections;
    }

    public static StandardFireResult CalculateStandardFire(FireInputs inputs)
    {
        var fireNumber = inputs.AnnualExpenses / inputs.WithdrawalRate;
        var realReturn = RealReturn(inputs.ExpectedReturn, inputs.InflationRate);
        var yearsToFire = YearsToTarget(inputs.CurrentSavings, inputs.AnnualContribution, realReturn, fireNumber);
        var yearsToRetirement = Math.Max(0, inputs.RetirementAge - inputs.CurrentAge);
        var projectionYears = double.IsFinite(yearsToFire)
            ? Math.Min((int)Math.Ceiling(yearsToFire) + 10, 50)
            : 50;

        var fireAge = RoundToTenth(inputs.CurrentAge + yearsToFire);

        return new StandardFireResult(
            Round(fireNumber),
            RoundToTenth(yearsToFire),
            fireAge,
            GenerateProjections(inputs.CurrentAge, inputs.CurrentSavings, inputs.AnnualContribution, inputs.ExpectedReturn, inputs.InflationRate, projectionYears, inputs.ProjectionStartYear),
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
            : YearsToTarget(inputs.CurrentSavings, inputs.AnnualContribution, realReturn, coastNumber);

        return new CoastFireResult(
            Round(coastNumber),
            RoundToTenth(yearsToCoast),
            alreadyCoasting,
            Round(fireNumber),
            GenerateProjections(inputs.CurrentAge, inputs.CurrentSavings, 0, inputs.ExpectedReturn, inputs.InflationRate, (int)yearsToRetirement + 10, inputs.ProjectionStartYear),
            GenerateProjections(inputs.CurrentAge, inputs.CurrentSavings, inputs.AnnualContribution, inputs.ExpectedReturn, inputs.InflationRate, (int)yearsToRetirement + 10, inputs.ProjectionStartYear));
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
        var yearsToBaristaFire = YearsToTarget(inputs.CurrentSavings, inputs.AnnualContribution, RealReturn(inputs.ExpectedReturn, inputs.InflationRate), baristaNumber);
        var projectionYears = Math.Min((int)Math.Ceiling(yearsToBaristaFire) + 10, 50);

        return new BaristaFireResult(
            Round(baristaNumber),
            Round(fullFireNumber),
            RoundToTenth(yearsToBaristaFire),
            partTimeAnnualIncome,
            GenerateProjections(inputs.CurrentAge, inputs.CurrentSavings, inputs.AnnualContribution, inputs.ExpectedReturn, inputs.InflationRate, projectionYears, inputs.ProjectionStartYear),
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
        var compoundFactor = Math.Pow(1 + realReturn, yearsToFire);
        var futureValueOfCurrent = inputs.CurrentSavings * compoundFactor;
        var requiredAnnualSavings = futureValueOfCurrent >= fireNumber
            ? 0
            : realReturn == 0
                ? (fireNumber - inputs.CurrentSavings) / yearsToFire
                : ((fireNumber - futureValueOfCurrent) * realReturn) / (compoundFactor - 1);

        return new ReverseFireResult(
            fireNumber,
            yearsToFire,
            Math.Max(0, requiredAnnualSavings),
            Math.Max(0, requiredAnnualSavings / 12),
            GenerateProjections(inputs.CurrentAge, inputs.CurrentSavings, requiredAnnualSavings, inputs.ExpectedReturn, inputs.InflationRate, (int)yearsToFire + 10, inputs.ProjectionStartYear),
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
        var inflationAdjustedBalance = inputs.StartingAmount;
        var totalContributions = inputs.StartingAmount;
        var realReturn = RealReturn(inputs.ExpectedReturn, inputs.InflationRate);
        var projections = new List<InvestmentProjectionPoint>
        {
            new(inputs.CurrentAge, startYear, 0, Round(nominalBalance), Round(inflationAdjustedBalance), Round(totalContributions), 0)
        };

        for (var year = 1; year <= inputs.YearsInvesting; year++)
        {
            nominalBalance = (nominalBalance * (1 + inputs.ExpectedReturn)) + annualContribution;
            inflationAdjustedBalance = (inflationAdjustedBalance * (1 + realReturn)) + annualContribution;
            totalContributions += annualContribution;
            projections.Add(new InvestmentProjectionPoint(inputs.CurrentAge + year, startYear + year, year, Round(nominalBalance), Round(inflationAdjustedBalance), Round(totalContributions), annualContribution));
        }

        return new InvestmentGrowthResult(
            savingsRate,
            annualContribution,
            annualContribution / 12,
            nominalBalance,
            inflationAdjustedBalance,
            totalContributions,
            nominalBalance - totalContributions,
            nominalBalance - inflationAdjustedBalance,
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
            var monthPrincipal = 0d;
            var monthInterest = 0d;
            var paidOffThisMonth = new List<string>();

            foreach (var debt in remainingDebts.Where(debt => debt.CurrentBalance > 0))
            {
                var interestCharge = debt.CurrentBalance * (debt.Rate / 12);
                var minimumPayment = Math.Min(debt.MinimumPayment, debt.CurrentBalance + interestCharge);
                var principalPayment = Math.Max(0, minimumPayment - interestCharge);
                debt.CurrentBalance -= principalPayment;
                monthlyBudget -= minimumPayment;
                monthPrincipal += principalPayment;
                monthInterest += interestCharge;
                MarkPaidOff(debt, month, paidOffThisMonth, payoffOrder, debtMilestones);
            }

            if (monthlyBudget > 0 && remainingDebts.FirstOrDefault(debt => debt.CurrentBalance > 0) is { } targetDebt)
            {
                var additionalInterest = targetDebt.CurrentBalance * (targetDebt.Rate / 12);
                var actualPayment = Math.Min(monthlyBudget, targetDebt.CurrentBalance + additionalInterest);
                var additionalPrincipal = Math.Max(0, actualPayment - additionalInterest);
                targetDebt.CurrentBalance -= additionalPrincipal;
                monthPrincipal += additionalPrincipal;
                monthInterest += additionalInterest;
                MarkPaidOff(targetDebt, month, paidOffThisMonth, payoffOrder, debtMilestones);
            }

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