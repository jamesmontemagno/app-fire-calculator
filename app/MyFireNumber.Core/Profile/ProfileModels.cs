using MyFireNumber.Core.Calculations;

namespace MyFireNumber.Core.Profile;

public sealed record FinancialProfile(
    string? DisplayName,
    string? HouseholdName,
    int? HouseholdSize,
    DateOnly? BirthDate,
    DateOnly? PhasedRetirementDate,
    DateOnly? TargetRetirementDate,
    double? AnnualIncome,
    double? AnnualExpenses)
{
    public static FinancialProfile Empty { get; } = new(
        DisplayName: null,
        HouseholdName: null,
        HouseholdSize: null,
        BirthDate: null,
        PhasedRetirementDate: null,
        TargetRetirementDate: null,
        AnnualIncome: null,
        AnnualExpenses: null);

    public bool IsComplete =>
        BirthDate is not null &&
        TargetRetirementDate is not null &&
        AnnualIncome is not null &&
        AnnualExpenses is not null;
}

public sealed record ProfileAccount(
    string Id,
    string Name,
    RetirementAccountType Type,
    double Balance,
    double AnnualContribution,
    double AnnualReturn,
    int AvailableAge,
    double WithdrawalRate,
    int PayoutYears,
    double EffectiveWithdrawalTaxRate)
{
    public RetirementAccount CreateScenarioAccount(string name) => new(
        Guid.NewGuid().ToString("N"),
        name,
        Type,
        Balance,
        AnnualContribution,
        AnnualReturn,
        AvailableAge,
        WithdrawalRate,
        PayoutYears,
        EffectiveWithdrawalTaxRate);
}

public static class ProfileAgeCalculator
{
    public static int AgeOn(DateOnly birthDate, DateOnly date)
    {
        if (date < birthDate)
        {
            throw new ArgumentOutOfRangeException(nameof(date), "The date must not be before the birth date.");
        }

        var birthday = BirthdayInYear(birthDate, date.Year);
        return date.Year - birthDate.Year - (date < birthday ? 1 : 0);
    }

    public static DateOnly BirthdayInYear(DateOnly birthDate, int year) =>
        birthDate.Month == 2 && birthDate.Day == 29 && !DateTime.IsLeapYear(year)
            ? new DateOnly(year, 2, 28)
            : new DateOnly(year, birthDate.Month, birthDate.Day);

    public static bool TryValidate(FinancialProfile profile, out string validationMessage)
    {
        validationMessage = string.Empty;
        if (profile.HouseholdSize is <= 0)
        {
            validationMessage = "Household size must be one or more.";
            return false;
        }

        if (profile.AnnualIncome is < 0 || profile.AnnualExpenses is < 0)
        {
            validationMessage = "Income and spending must be zero or more.";
            return false;
        }

        if (profile.BirthDate is not DateOnly birthDate)
        {
            return true;
        }

        if (profile.PhasedRetirementDate is DateOnly phasedDate && phasedDate <= birthDate)
        {
            validationMessage = "Phased retirement must be after the birth date.";
            return false;
        }

        if (profile.TargetRetirementDate is DateOnly targetDate && targetDate <= birthDate)
        {
            validationMessage = "Full retirement must be after the birth date.";
            return false;
        }

        if (profile.PhasedRetirementDate is DateOnly phased && profile.TargetRetirementDate is DateOnly target && target < phased)
        {
            validationMessage = "Full retirement must be on or after phased retirement.";
            return false;
        }

        return true;
    }
}
