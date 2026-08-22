namespace MyFireNumber.Core.Calculations;

public enum SeppMethod
{
    RequiredMinimumDistribution,
    FixedAmortization,
    FixedAnnuitization
}

public sealed record SeppAccount(
    string Id,
    string Name,
    RetirementAccountType Type,
    double Balance,
    double ExpectedReturn);

public sealed record SeppDraft(
    IReadOnlyList<SeppAccount> Accounts,
    string SelectedAccountId,
    DateOnly BirthDate,
    DateOnly FirstPaymentDate,
    double InterestRate,
    double MaximumInterestRate,
    double? AnnuityFactor,
    SeppMethod Method)
{
    public const int PayloadVersion = 1;

    public static SeppDraft Default
    {
        get
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var account = new SeppAccount(
                "standalone-sepp-account",
                "Traditional IRA",
                RetirementAccountType.Traditional,
                500_000,
                0.05);
            return new(
                [account],
                account.Id,
                today.AddYears(-50),
                today,
                0.05,
                0.05,
                null,
                SeppMethod.FixedAmortization);
        }
    }

    public SeppAccount SelectedAccount =>
        Accounts.FirstOrDefault(account => account.Id == SelectedAccountId)
        ?? Accounts.FirstOrDefault()
        ?? throw new InvalidOperationException("A 72(t) calculation requires an account.");

    public SeppInputs ToInputs() => new(
        SelectedAccount.Balance,
        SelectedAccount.ExpectedReturn,
        BirthDate,
        FirstPaymentDate,
        InterestRate,
        MaximumInterestRate,
        AnnuityFactor,
        Method);
}
