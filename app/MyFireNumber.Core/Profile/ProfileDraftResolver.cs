using MyFireNumber.Core.Calculations;

namespace MyFireNumber.Core.Profile;

public static class ProfileDraftResolver
{
    public static ProfileResolution<TDraft> Resolve<TDraft>(
        TDraft source,
        ProfileFinancialSnapshot snapshot,
        DateOnly today)
        where TDraft : class
    {
        var errors = Validate(source, snapshot);
        object resolved = source switch
        {
            StandardFireDraft draft => draft with
            {
                CurrentAge = CurrentAge(snapshot, today, draft.CurrentAge),
                RetirementAge = TargetAge(snapshot, draft.RetirementAge),
                CurrentSavings = snapshot.TotalAccountBalance,
                AnnualContribution = snapshot.TotalAnnualContributions,
                AnnualIncome = snapshot.EffectiveAnnualIncome ?? draft.AnnualIncome,
                AnnualExpenses = snapshot.EffectiveAnnualExpenses ?? draft.AnnualExpenses
            },
            LeanFireDraft draft => draft with
            {
                CurrentAge = CurrentAge(snapshot, today, draft.CurrentAge),
                RetirementAge = TargetAge(snapshot, draft.RetirementAge),
                CurrentSavings = snapshot.TotalAccountBalance,
                AnnualContribution = snapshot.TotalAnnualContributions,
                AnnualIncome = snapshot.EffectiveAnnualIncome ?? draft.AnnualIncome,
                AnnualExpenses = snapshot.EffectiveAnnualExpenses ?? draft.AnnualExpenses
            },
            FatFireDraft draft => draft with
            {
                CurrentAge = CurrentAge(snapshot, today, draft.CurrentAge),
                RetirementAge = TargetAge(snapshot, draft.RetirementAge),
                CurrentSavings = snapshot.TotalAccountBalance,
                AnnualContribution = snapshot.TotalAnnualContributions,
                AnnualIncome = snapshot.EffectiveAnnualIncome ?? draft.AnnualIncome,
                AnnualExpenses = snapshot.EffectiveAnnualExpenses ?? draft.AnnualExpenses
            },
            CoastFireDraft draft => draft with
            {
                CurrentAge = CurrentAge(snapshot, today, draft.CurrentAge),
                RetirementAge = TargetAge(snapshot, draft.RetirementAge),
                CurrentSavings = snapshot.TotalAccountBalance,
                AnnualContribution = snapshot.TotalAnnualContributions,
                AnnualExpenses = snapshot.EffectiveAnnualExpenses ?? draft.AnnualExpenses
            },
            BaristaFireDraft draft => draft with
            {
                CurrentAge = CurrentAge(snapshot, today, draft.CurrentAge),
                CurrentSavings = snapshot.TotalAccountBalance,
                AnnualContribution = snapshot.TotalAnnualContributions,
                AnnualExpenses = snapshot.EffectiveAnnualExpenses ?? draft.AnnualExpenses
            },
            ReverseFireDraft draft => draft with
            {
                CurrentAge = CurrentAge(snapshot, today, draft.CurrentAge),
                TargetRetirementAge = TargetAge(snapshot, draft.TargetRetirementAge),
                CurrentSavings = snapshot.TotalAccountBalance,
                AnnualExpenses = snapshot.EffectiveAnnualExpenses ?? draft.AnnualExpenses
            },
            SavingsInvestmentDraft draft => draft with
            {
                StartingAmount = snapshot.TotalAccountBalance,
                ContributionAmount = snapshot.TotalAnnualContributions / 12,
                ContributionFrequency = ContributionFrequency.Monthly,
                AnnualIncome = snapshot.EffectiveAnnualIncome ?? draft.AnnualIncome,
                CurrentAge = CurrentAge(snapshot, today, draft.CurrentAge)
            },
            HealthcareGapDraft draft => draft with
            {
                CurrentAge = CurrentAge(snapshot, today, draft.CurrentAge),
                EarlyRetirementAge = TargetAge(snapshot, draft.EarlyRetirementAge)
            },
            DebtPayoffDraft draft => draft with
            {
                Debts = snapshot.Debts.ToArray(),
                MonthlyBudget = snapshot.Debts.Sum(debt => debt.MinimumPayment + debt.ExtraMonthlyPayment)
            },
            DeferredCompensationDraft draft => draft with
            {
                CurrentAge = CurrentAge(snapshot, today, draft.CurrentAge),
                SemiRetirementAge = PhasedAge(snapshot, draft.SemiRetirementAge),
                AnnualExpenses = 0,
                Accounts = snapshot.Accounts.ToArray(),
                IncomeSources = snapshot.Income.ToArray(),
                AdditionalExpenses = snapshot.Expenses.ToArray()
            },
            SeppDraft draft => ResolveSepp(draft, snapshot),
            RothConversionDraft draft => draft with
            {
                CurrentAge = CurrentAge(snapshot, today, draft.CurrentAge),
                StartYear = today.Year,
                TraditionalBalance = snapshot.Accounts
                    .Where(account => account.Type == RetirementAccountType.Traditional)
                    .Sum(account => account.Balance),
                RothBalance = snapshot.Accounts
                    .Where(account => account.Type == RetirementAccountType.Roth)
                    .Sum(account => account.Balance)
            },
            _ => source
        };

        return new ProfileResolution<TDraft>(
            (TDraft)(object)resolved,
            snapshot.Revision,
            SourcesFor(source),
            errors);
    }

    private static IReadOnlyList<string> Validate<TDraft>(TDraft draft, ProfileFinancialSnapshot snapshot)
    {
        var errors = new List<string>();
        if (draft is DebtPayoffDraft && snapshot.Debts.Count == 0)
        {
            errors.Add("Add at least one debt to Profile to use linked mode.");
        }
        else if (draft is DeferredCompensationDraft && snapshot.Accounts.Count == 0)
        {
            errors.Add("Add at least one account to Profile to use linked mode.");
        }
        else if (draft is SeppDraft && !snapshot.Accounts.Any(IsPotentialSeppAccount))
        {
            errors.Add("Add a Traditional 401(k)/IRA or Roth IRA account to Accounts to use linked mode.");
        }
        else if (draft is RothConversionDraft &&
                 !snapshot.Accounts.Any(account => account.Type == RetirementAccountType.Traditional))
        {
            errors.Add("Add a Traditional 401(k) or IRA account to Accounts to use linked mode.");
        }
        else if (draft is StandardFireDraft or LeanFireDraft or FatFireDraft or CoastFireDraft or
                 BaristaFireDraft or ReverseFireDraft or SavingsInvestmentDraft &&
                 snapshot.Accounts.Count == 0)
        {
            errors.Add("Add at least one account to Profile to use linked mode.");
        }

        // Either source satisfies the requirement now that both feed the same effective value, so a
        // profile that only answered the onboarding income question is still linkable.
        if (draft is StandardFireDraft or LeanFireDraft or FatFireDraft && snapshot.EffectiveAnnualIncome is null)
        {
            errors.Add("Add at least one income source to Profile to use linked mode.");
        }

        if (draft is DeferredCompensationDraft)
        {
            if (snapshot.Profile.BirthDate is null)
            {
                errors.Add("Add your birth date to Profile to use linked mode.");
            }

            if (snapshot.Profile.PhasedRetirementDate is null)
            {
                errors.Add("Add your phased retirement date to Profile to use linked mode.");
            }
        }

        if (draft is SeppDraft && snapshot.Profile.BirthDate is null)
        {
            errors.Add("Add your birth date to Profile to use linked mode.");
        }

        if (draft is RothConversionDraft && snapshot.Profile.BirthDate is null)
        {
            errors.Add("Add your birth date to Profile to use linked mode.");
        }

        if (draft is StandardFireDraft or LeanFireDraft or FatFireDraft or CoastFireDraft or
            BaristaFireDraft or ReverseFireDraft or DeferredCompensationDraft &&
            snapshot.EffectiveAnnualExpenses is null)
        {
            errors.Add("Add at least one expense to Profile to use linked mode.");
        }

        if (snapshot.Accounts.Any(account => account.Balance < 0 || account.AnnualContribution < 0))
        {
            errors.Add("Profile accounts contain invalid negative values.");
        }

        if (snapshot.Income.Any(item =>
                item.AnnualAmount < 0 ||
                item.StartAge is < 18 or > 100 ||
                item.EndAge < item.StartAge ||
                item.EndAge > 100 ||
                item.AnnualGrowth is < -1 or > 1 ||
                item.TaxRate is < 0 or > 1))
        {
            errors.Add("Profile income contains invalid amount, age, growth, or tax values.");
        }

        if (snapshot.Expenses.Any(item =>
                item.AnnualAmount < 0 ||
                item.StartAge is < 18 or > 100 ||
                item.EndAge < item.StartAge ||
                item.EndAge > 100))
        {
            errors.Add("Profile expenses contain an invalid amount or age range.");
        }

        if (snapshot.Debts.Any(debt =>
                debt.Balance < 0 ||
                debt.MinimumPayment < 0 ||
                debt.ExtraMonthlyPayment < 0 ||
                debt.Rate < 0))
        {
            errors.Add("Profile debts contain invalid negative values.");
        }

        return errors;
    }

    private static int CurrentAge(ProfileFinancialSnapshot snapshot, DateOnly today, int fallback) =>
        snapshot.Profile.BirthDate is DateOnly birthDate
            ? ProfileAgeCalculator.AgeOn(birthDate, today)
            : fallback;

    private static int TargetAge(ProfileFinancialSnapshot snapshot, int fallback) =>
        snapshot.Profile.BirthDate is DateOnly birthDate && snapshot.Profile.TargetRetirementDate is DateOnly date
            ? ProfileAgeCalculator.AgeOn(birthDate, date)
            : fallback;

    private static int PhasedAge(ProfileFinancialSnapshot snapshot, int fallback) =>
        snapshot.Profile.BirthDate is DateOnly birthDate && snapshot.Profile.PhasedRetirementDate is DateOnly date
            ? ProfileAgeCalculator.AgeOn(birthDate, date)
            : fallback;

    private static string[] SourcesFor<TDraft>(TDraft draft) => draft switch
    {
        DebtPayoffDraft => ["Profile debts"],
        HealthcareGapDraft => ["Profile timeline"],
        SeppDraft => ["Profile timeline", "Profile retirement accounts"],
        RothConversionDraft => ["Profile timeline", "Profile retirement accounts"],
        WithdrawalRateDraft => [],
        DeferredCompensationDraft => ["Profile timeline", "Profile accounts", "Profile income", "Profile expenses"],
        _ => ["Profile timeline", "Profile accounts", "Profile income", "Profile expenses"]
    };

    private static SeppDraft ResolveSepp(SeppDraft draft, ProfileFinancialSnapshot snapshot)
    {
        var accounts = snapshot.Accounts
            .Where(IsPotentialSeppAccount)
            .Select(account => new SeppAccount(
                account.Id,
                account.Name,
                account.Type,
                account.Balance,
                account.AnnualReturn))
            .ToArray();
        var selectedId = accounts.Any(account => account.Id == draft.SelectedAccountId)
            ? draft.SelectedAccountId
            : accounts.FirstOrDefault()?.Id ?? draft.SelectedAccountId;

        return draft with
        {
            Accounts = accounts,
            SelectedAccountId = selectedId,
            BirthDate = snapshot.Profile.BirthDate ?? draft.BirthDate
        };
    }

    private static bool IsPotentialSeppAccount(RetirementAccount account) =>
        account.Type is RetirementAccountType.Traditional or RetirementAccountType.Roth;
}
