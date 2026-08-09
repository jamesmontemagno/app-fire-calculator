using MyFireNumber.Core.Calculations;

namespace MyFireNumber.Services;

public sealed record CalculatorDefaults(
    double ExpectedReturn,
    double InflationRate,
    double WithdrawalRate,
    int CurrentAge,
    int RetirementAge);

public interface ICalculatorDefaultsService
{
    CalculatorDefaults Current { get; }

    void Save(CalculatorDefaults defaults);

    StandardFireDraft StandardFire { get; }
    CoastFireDraft CoastFire { get; }
    LeanFireDraft LeanFire { get; }
    FatFireDraft FatFire { get; }
    BaristaFireDraft BaristaFire { get; }
    ReverseFireDraft ReverseFire { get; }
    WithdrawalRateDraft WithdrawalRate { get; }
    SavingsInvestmentDraft SavingsInvestment { get; }
    HealthcareGapDraft HealthcareGap { get; }
    DeferredCompensationDraft RetirementCashFlow { get; }
}

public sealed class CalculatorDefaultsService : ICalculatorDefaultsService
{
    private const string ExpectedReturnKey = "defaults-expected-return";
    private const string InflationRateKey = "defaults-inflation-rate";
    private const string WithdrawalRateKey = "defaults-withdrawal-rate";
    private const string CurrentAgeKey = "defaults-current-age";
    private const string RetirementAgeKey = "defaults-retirement-age";

    public CalculatorDefaults Current => new(
        Preferences.Default.Get(ExpectedReturnKey, 0.07),
        Preferences.Default.Get(InflationRateKey, 0.03),
        Preferences.Default.Get(WithdrawalRateKey, 0.04),
        Preferences.Default.Get(CurrentAgeKey, 30),
        Preferences.Default.Get(RetirementAgeKey, 55));

    public StandardFireDraft StandardFire => Apply(StandardFireDraft.Default);
    public CoastFireDraft CoastFire => Apply(CoastFireDraft.Default);
    public LeanFireDraft LeanFire => Apply(LeanFireDraft.Default);
    public FatFireDraft FatFire => Apply(FatFireDraft.Default);
    public BaristaFireDraft BaristaFire => Apply(BaristaFireDraft.Default);
    public ReverseFireDraft ReverseFire => Apply(ReverseFireDraft.Default);
    public WithdrawalRateDraft WithdrawalRate => Apply(WithdrawalRateDraft.Default);
    public SavingsInvestmentDraft SavingsInvestment => Apply(SavingsInvestmentDraft.Default);
    public HealthcareGapDraft HealthcareGap => Apply(HealthcareGapDraft.Default);
    public DeferredCompensationDraft RetirementCashFlow => Apply(DeferredCompensationDraft.Default);

    public void Save(CalculatorDefaults defaults)
    {
        Preferences.Default.Set(ExpectedReturnKey, defaults.ExpectedReturn);
        Preferences.Default.Set(InflationRateKey, defaults.InflationRate);
        Preferences.Default.Set(WithdrawalRateKey, defaults.WithdrawalRate);
        Preferences.Default.Set(CurrentAgeKey, defaults.CurrentAge);
        Preferences.Default.Set(RetirementAgeKey, defaults.RetirementAge);
    }

    private StandardFireDraft Apply(StandardFireDraft draft)
    {
        var defaults = Current;
        return draft with
        {
            ExpectedReturn = defaults.ExpectedReturn,
            InflationRate = defaults.InflationRate,
            WithdrawalRate = defaults.WithdrawalRate,
            CurrentAge = defaults.CurrentAge,
            RetirementAge = defaults.RetirementAge
        };
    }

    private CoastFireDraft Apply(CoastFireDraft draft)
    {
        var defaults = Current;
        return draft with
        {
            ExpectedReturn = defaults.ExpectedReturn,
            InflationRate = defaults.InflationRate,
            WithdrawalRate = defaults.WithdrawalRate,
            CurrentAge = defaults.CurrentAge,
            RetirementAge = defaults.RetirementAge
        };
    }

    private LeanFireDraft Apply(LeanFireDraft draft)
    {
        var defaults = Current;
        return draft with
        {
            ExpectedReturn = defaults.ExpectedReturn,
            InflationRate = defaults.InflationRate,
            WithdrawalRate = defaults.WithdrawalRate,
            CurrentAge = defaults.CurrentAge,
            RetirementAge = defaults.RetirementAge
        };
    }

    private FatFireDraft Apply(FatFireDraft draft)
    {
        var defaults = Current;
        return draft with
        {
            ExpectedReturn = defaults.ExpectedReturn,
            InflationRate = defaults.InflationRate,
            WithdrawalRate = defaults.WithdrawalRate,
            CurrentAge = defaults.CurrentAge,
            RetirementAge = defaults.RetirementAge
        };
    }

    private BaristaFireDraft Apply(BaristaFireDraft draft)
    {
        var defaults = Current;
        return draft with
        {
            ExpectedReturn = defaults.ExpectedReturn,
            InflationRate = defaults.InflationRate,
            WithdrawalRate = defaults.WithdrawalRate,
            CurrentAge = defaults.CurrentAge
        };
    }

    private ReverseFireDraft Apply(ReverseFireDraft draft)
    {
        var defaults = Current;
        return draft with
        {
            ExpectedReturn = defaults.ExpectedReturn,
            InflationRate = defaults.InflationRate,
            WithdrawalRate = defaults.WithdrawalRate,
            CurrentAge = defaults.CurrentAge,
            TargetRetirementAge = defaults.RetirementAge
        };
    }

    private WithdrawalRateDraft Apply(WithdrawalRateDraft draft)
    {
        var defaults = Current;
        return draft with
        {
            ExpectedReturn = defaults.ExpectedReturn,
            InflationRate = defaults.InflationRate,
            WithdrawalRate = defaults.WithdrawalRate
        };
    }

    private SavingsInvestmentDraft Apply(SavingsInvestmentDraft draft)
    {
        var defaults = Current;
        return draft with
        {
            ExpectedReturn = defaults.ExpectedReturn,
            InflationRate = defaults.InflationRate,
            CurrentAge = defaults.CurrentAge
        };
    }

    private HealthcareGapDraft Apply(HealthcareGapDraft draft)
    {
        var defaults = Current;
        return draft with
        {
            InflationRate = defaults.InflationRate,
            CurrentAge = defaults.CurrentAge,
            EarlyRetirementAge = defaults.RetirementAge
        };
    }

    private DeferredCompensationDraft Apply(DeferredCompensationDraft draft)
    {
        var defaults = Current;
        return draft with
        {
            InflationRate = defaults.InflationRate,
            CurrentAge = defaults.CurrentAge,
            SemiRetirementAge = defaults.RetirementAge
        };
    }
}
