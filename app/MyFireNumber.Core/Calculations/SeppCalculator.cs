namespace MyFireNumber.Core.Calculations;

public sealed record SeppInputs(
    double AccountBalance,
    double ExpectedReturn,
    DateOnly BirthDate,
    DateOnly FirstPaymentDate,
    double InterestRate,
    double MaximumInterestRate,
    double? AnnuityFactor,
    SeppMethod Method);

public sealed record SeppMethodResult(
    SeppMethod Method,
    double? AnnualPayment,
    double? MonthlyPayment,
    IReadOnlyList<SeppProjectionPoint> Projections);

public sealed record SeppProjectionPoint(
    int YearNumber,
    int CalendarYear,
    int Age,
    double StartingBalance,
    double AnnualPayment,
    double EndingBalance);

public sealed record SeppResult(
    int StartingAge,
    double LifeExpectancyFactor,
    DateOnly RequiredEndDate,
    int RequiredYears,
    double MaximumInterestRate,
    SeppMethodResult Rmd,
    SeppMethodResult Amortization,
    SeppMethodResult Annuitization)
{
    public SeppMethodResult For(SeppMethod method) => method switch
    {
        SeppMethod.RequiredMinimumDistribution => Rmd,
        SeppMethod.FixedAmortization => Amortization,
        SeppMethod.FixedAnnuitization => Annuitization,
        _ => throw new ArgumentOutOfRangeException(nameof(method))
    };
}

public static class SeppCalculator
{
    // Treas. Reg. §1.401(a)(9)-9, Table I, effective for 2022 and later.
    // A SEPP must begin before age 59½, but a five-year series can continue into the 60s.
    private static readonly IReadOnlyDictionary<int, double> SingleLifeFactors =
        new Dictionary<int, double>
        {
            [18] = 67.0, [19] = 66.0, [20] = 65.0, [21] = 64.1, [22] = 63.1,
            [23] = 62.1, [24] = 61.1, [25] = 60.2, [26] = 59.2, [27] = 58.2,
            [28] = 57.3, [29] = 56.3, [30] = 55.3, [31] = 54.4, [32] = 53.4,
            [33] = 52.5, [34] = 51.5, [35] = 50.5, [36] = 49.6, [37] = 48.6,
            [38] = 47.7, [39] = 46.7, [40] = 45.7, [41] = 44.8, [42] = 43.8,
            [43] = 42.9, [44] = 41.9, [45] = 41.0, [46] = 40.0, [47] = 39.0,
            [48] = 38.1, [49] = 37.1, [50] = 36.2, [51] = 35.3, [52] = 34.3,
            [53] = 33.4, [54] = 32.5, [55] = 31.6, [56] = 30.6, [57] = 29.8,
            [58] = 28.9, [59] = 28.0, [60] = 27.1, [61] = 26.2, [62] = 25.4,
            [63] = 24.5, [64] = 23.7, [65] = 22.9, [66] = 22.0, [67] = 21.2,
            [68] = 20.4, [69] = 19.6, [70] = 18.8
        };

    public static double MaximumPermittedInterestRate(double federalMidTermRate) =>
        Math.Max(0.05, federalMidTermRate * 1.2);

    public static double SingleLifeFactor(int age) =>
        SingleLifeFactors.TryGetValue(age, out var factor)
            ? factor
            : throw new ArgumentOutOfRangeException(nameof(age), "Age is outside the retained IRS Single Life table range.");

    public static SeppResult Calculate(SeppInputs inputs)
    {
        Validate(inputs);

        var startingAge = AgeOn(inputs.BirthDate, inputs.FirstPaymentDate);
        var lifeExpectancyFactor = SingleLifeFactor(startingAge);
        var age59AndHalf = inputs.BirthDate.AddYears(59).AddMonths(6);
        var fiveYearsAfterFirstPayment = inputs.FirstPaymentDate.AddYears(5);
        var requiredEndDate = age59AndHalf > fiveYearsAfterFirstPayment
            ? age59AndHalf
            : fiveYearsAfterFirstPayment;
        var requiredYears = Math.Max(
            1,
            (int)Math.Ceiling(
                (requiredEndDate.DayNumber - inputs.FirstPaymentDate.DayNumber) / 365.2425));

        var rmd = BuildMethodResult(
            SeppMethod.RequiredMinimumDistribution,
            inputs,
            requiredYears,
            (_, balance, age) => balance / SingleLifeFactor(age));
        var amortizationPayment = Payment(inputs.AccountBalance, inputs.InterestRate, lifeExpectancyFactor);
        var amortization = BuildMethodResult(
            SeppMethod.FixedAmortization,
            inputs,
            requiredYears,
            (_, _, _) => amortizationPayment);
        var annuitization = inputs.AnnuityFactor is > 0
            ? BuildMethodResult(
                SeppMethod.FixedAnnuitization,
                inputs,
                requiredYears,
                (_, _, _) => inputs.AccountBalance / inputs.AnnuityFactor.Value)
            : new SeppMethodResult(SeppMethod.FixedAnnuitization, null, null, []);

        return new(
            startingAge,
            lifeExpectancyFactor,
            requiredEndDate,
            requiredYears,
            inputs.MaximumInterestRate,
            rmd,
            amortization,
            annuitization);
    }

    private static SeppMethodResult BuildMethodResult(
        SeppMethod method,
        SeppInputs inputs,
        int requiredYears,
        Func<int, double, int, double> paymentForYear)
    {
        var balance = inputs.AccountBalance;
        var points = new List<SeppProjectionPoint>(requiredYears);
        for (var year = 0; year < requiredYears; year++)
        {
            var paymentDate = inputs.FirstPaymentDate.AddYears(year);
            var age = AgeOn(inputs.BirthDate, paymentDate);
            var payment = Math.Min(balance, paymentForYear(year, balance, age));
            var endingBalance = Math.Max(0, balance * (1 + inputs.ExpectedReturn) - payment);
            points.Add(new(
                year + 1,
                paymentDate.Year,
                age,
                Math.Round(balance),
                Math.Round(payment),
                Math.Round(endingBalance)));
            balance = endingBalance;
        }

        var annualPayment = points.Count == 0 ? 0 : points[0].AnnualPayment;
        return new(method, annualPayment, annualPayment / 12, points);
    }

    private static double Payment(double presentValue, double rate, double years) =>
        rate == 0
            ? presentValue / years
            : rate * presentValue / (1 - Math.Pow(1 + rate, -years));

    private static int AgeOn(DateOnly birthDate, DateOnly date)
    {
        if (date < birthDate)
        {
            throw new ArgumentOutOfRangeException(nameof(date), "First payment date must be after birth date.");
        }

        var birthday = birthDate.Month == 2 && birthDate.Day == 29 && !DateTime.IsLeapYear(date.Year)
            ? new DateOnly(date.Year, 2, 28)
            : new DateOnly(date.Year, birthDate.Month, birthDate.Day);
        return date.Year - birthDate.Year - (date < birthday ? 1 : 0);
    }

    private static void Validate(SeppInputs inputs)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(inputs.AccountBalance);
        ArgumentOutOfRangeException.ThrowIfLessThan(inputs.ExpectedReturn, -1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(inputs.ExpectedReturn, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(inputs.InterestRate);
        ArgumentOutOfRangeException.ThrowIfNegative(inputs.MaximumInterestRate);
        if (inputs.InterestRate > inputs.MaximumInterestRate)
        {
            throw new ArgumentOutOfRangeException(nameof(inputs), "Interest rate exceeds the supplied IRS limit.");
        }

        if (inputs.AnnuityFactor is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(inputs), "Annuity factor must be greater than zero.");
        }

        if (inputs.Method == SeppMethod.FixedAnnuitization && inputs.AnnuityFactor is null)
        {
            throw new ArgumentException("An annuity factor is required for fixed annuitization.", nameof(inputs));
        }

        if (inputs.FirstPaymentDate >= inputs.BirthDate.AddYears(59).AddMonths(6))
        {
            throw new ArgumentOutOfRangeException(
                nameof(inputs),
                "The first SEPP payment must occur before age 59½.");
        }

        var age = AgeOn(inputs.BirthDate, inputs.FirstPaymentDate);
        if (age is < 18 or > 59)
        {
            throw new ArgumentOutOfRangeException(nameof(inputs), "SEPP starting age must be from 18 through 59.");
        }
        _ = SingleLifeFactor(age);
    }
}
