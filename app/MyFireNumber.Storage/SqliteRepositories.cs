using System.Globalization;
using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Profile;
using SQLite;

namespace MyFireNumber.Storage;

public sealed class SqliteLocalDataArchiveRepository(LocalDatabase database) : ILocalDataArchiveRepository
{
    public Task<LocalDataArchive> ExportAsync(CancellationToken cancellationToken = default) =>
        database.ExportAsync(cancellationToken);

    public Task ImportAsync(LocalDataArchive archive, CancellationToken cancellationToken = default) =>
        database.ImportAsync(archive, cancellationToken);
}

public sealed class SqliteDraftRepository(LocalDatabase database) : IDraftRepository
{
    public async Task<DraftRecord?> GetAsync(string calculatorId, CancellationToken cancellationToken = default)
    {
        await database.InitializeAsync(cancellationToken);
        var entity = await database.Connection.FindAsync<DraftEntity>(calculatorId);
        return entity is null ? null : ToRecord(entity);
    }

    public async Task SaveAsync(DraftRecord draft, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(draft.CalculatorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(draft.PayloadJson);

        await database.InitializeAsync(cancellationToken);
        await database.Connection.InsertOrReplaceAsync(new DraftEntity
        {
            CalculatorId = draft.CalculatorId,
            PayloadVersion = draft.PayloadVersion,
            PayloadJson = draft.PayloadJson,
            UpdatedAtUtc = draft.UpdatedAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            DataMode = draft.DataMode.ToString(),
            ProfileRevision = draft.ProfileRevision
        });
    }

    public async Task DeleteAsync(string calculatorId, CancellationToken cancellationToken = default)
    {
        await database.InitializeAsync(cancellationToken);
        await database.Connection.DeleteAsync<DraftEntity>(calculatorId);
    }

    private static DraftRecord ToRecord(DraftEntity entity)
    {
        return new DraftRecord(
            entity.CalculatorId,
            entity.PayloadVersion,
            entity.PayloadJson,
            DateTime.Parse(entity.UpdatedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            Enum.Parse<ScenarioDataMode>(entity.DataMode),
            entity.ProfileRevision);
    }
}

public sealed class SqlitePlanRepository(LocalDatabase database) : IPlanRepository
{
    public async Task<IReadOnlyList<PlanRecord>> ListAsync(string? calculatorId = null, CancellationToken cancellationToken = default)
    {
        await database.InitializeAsync(cancellationToken);
        var query = database.Connection.Table<PlanEntity>();
        var entities = string.IsNullOrWhiteSpace(calculatorId)
            ? await query.ToListAsync()
            : await query.Where(plan => plan.CalculatorId == calculatorId).ToListAsync();

        return entities
            .Select(ToRecord)
            .OrderByDescending(plan => plan.UpdatedAtUtc)
            .ToArray();
    }

    public async Task<PlanRecord?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        await database.InitializeAsync(cancellationToken);
        var entity = await database.Connection.FindAsync<PlanEntity>(id);
        return entity is null ? null : ToRecord(entity);
    }

    public async Task SaveAsync(PlanRecord plan, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plan.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(plan.CalculatorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(plan.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(plan.PayloadJson);

        await database.InitializeAsync(cancellationToken);
        await database.Connection.InsertOrReplaceAsync(new PlanEntity
        {
            Id = plan.Id,
            CalculatorId = plan.CalculatorId,
            Name = plan.Name.Trim(),
            PayloadVersion = plan.PayloadVersion,
            PayloadJson = plan.PayloadJson,
            CreatedAtUtc = plan.CreatedAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            UpdatedAtUtc = plan.UpdatedAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            DataMode = plan.DataMode.ToString(),
            ProfileRevision = plan.ProfileRevision
        });
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        await database.InitializeAsync(cancellationToken);
        await database.Connection.DeleteAsync<PlanEntity>(id);
    }

    private static PlanRecord ToRecord(PlanEntity entity)
    {
        return new PlanRecord(
            entity.Id,
            entity.CalculatorId,
            entity.Name,
            entity.PayloadVersion,
            entity.PayloadJson,
            DateTime.Parse(entity.CreatedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            DateTime.Parse(entity.UpdatedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            Enum.Parse<ScenarioDataMode>(entity.DataMode),
            entity.ProfileRevision);
    }
}

public sealed class SqliteCalculatorPreferencesRepository(LocalDatabase database) : ICalculatorPreferencesRepository
{
    public async Task<IReadOnlyList<CalculatorPreferenceRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        await database.InitializeAsync(cancellationToken);
        var preferences = await database.Connection.Table<CalculatorPreferenceEntity>().ToListAsync();
        return preferences
            .Select(preference => new CalculatorPreferenceRecord(preference.CalculatorId, preference.IsVisible, preference.SortOrder))
            .OrderBy(preference => preference.SortOrder)
            .ToArray();
    }

    public async Task SaveAsync(CalculatorPreferenceRecord preference, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(preference.CalculatorId);

        await database.InitializeAsync(cancellationToken);
        await database.Connection.InsertOrReplaceAsync(new CalculatorPreferenceEntity
        {
            CalculatorId = preference.CalculatorId,
            IsVisible = preference.IsVisible,
            SortOrder = preference.SortOrder
        });
    }
}

public sealed class SqliteProfileRepository(LocalDatabase database) : IProfileRepository
{
    private const int ProfileId = 1;

    public async Task<FinancialProfile> GetAsync(CancellationToken cancellationToken = default)
    {
        await database.InitializeAsync(cancellationToken);
        var entity = await database.Connection.FindAsync<ProfileEntity>(ProfileId);
        return entity is null
            ? FinancialProfile.Empty
            : new FinancialProfile(entity.DisplayName, entity.HouseholdName, entity.HouseholdSize, ParseDate(entity.BirthDate), ParseDate(entity.PhasedRetirementDate), ParseDate(entity.TargetRetirementDate), entity.AnnualIncome, entity.AnnualExpenses);
    }

    public async Task SaveAsync(FinancialProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (!ProfileAgeCalculator.TryValidate(profile, out var validationMessage))
        {
            throw new ArgumentException(validationMessage, nameof(profile));
        }

        await database.InitializeAsync(cancellationToken);
        await database.Connection.InsertOrReplaceAsync(new ProfileEntity
        {
            Id = ProfileId,
            DisplayName = NullIfWhiteSpace(profile.DisplayName),
            HouseholdName = NullIfWhiteSpace(profile.HouseholdName),
            HouseholdSize = profile.HouseholdSize,
            BirthDate = FormatDate(profile.BirthDate),
            PhasedRetirementDate = FormatDate(profile.PhasedRetirementDate),
            TargetRetirementDate = FormatDate(profile.TargetRetirementDate),
            AnnualIncome = profile.AnnualIncome,
            AnnualExpenses = profile.AnnualExpenses
        });
    }

    private static DateOnly? ParseDate(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : DateOnly.Parse(value, CultureInfo.InvariantCulture);

    private static string? FormatDate(DateOnly? value) =>
        value?.ToString("O", CultureInfo.InvariantCulture);

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class SqliteProfileAccountRepository(LocalDatabase database) : IProfileAccountRepository
{
    public async Task<IReadOnlyList<ProfileAccount>> ListAsync(CancellationToken cancellationToken = default)
    {
        await database.InitializeAsync(cancellationToken);
        var entities = await database.Connection.Table<ProfileAccountEntity>().OrderBy(account => account.Name).ToListAsync();
        return entities.Select(ToRecord).ToArray();
    }

    public async Task SaveAsync(ProfileAccount account, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(account.Id);
            ArgumentException.ThrowIfNullOrWhiteSpace(account.Name);

            await database.InitializeAsync(cancellationToken);
            await database.Connection.InsertOrReplaceAsync(new ProfileAccountEntity
            {
                Id = account.Id,
                Name = account.Name.Trim(),
                Type = account.Type.ToString(),
                Balance = account.Balance,
                AnnualContribution = account.AnnualContribution,
                AnnualReturn = account.AnnualReturn,
                AvailableAge = account.AvailableAge,
                WithdrawalRate = account.WithdrawalRate,
                PayoutYears = account.PayoutYears,
                EffectiveWithdrawalTaxRate = account.EffectiveWithdrawalTaxRate
            });
        }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            await database.InitializeAsync(cancellationToken);
            await database.Connection.DeleteAsync<ProfileAccountEntity>(id);
        }

    private static ProfileAccount ToRecord(ProfileAccountEntity entity) => new(
            entity.Id,
            entity.Name,
            Enum.Parse<RetirementAccountType>(entity.Type),
            entity.Balance,
            entity.AnnualContribution,
            entity.AnnualReturn,
            entity.AvailableAge,
            entity.WithdrawalRate,
            entity.PayoutYears,
            entity.EffectiveWithdrawalTaxRate);
}

public sealed class SqliteProfileIncomeRepository(LocalDatabase database) : IProfileIncomeRepository
{
    public async Task<IReadOnlyList<ProfileIncome>> ListAsync(CancellationToken cancellationToken = default)
    {
        await database.InitializeAsync(cancellationToken);
        var items = await database.Connection.Table<ProfileIncomeEntity>().OrderBy(item => item.Name).ToListAsync();
        return items.Select(item => new ProfileIncome(item.Id, item.Name, item.Amount, Enum.Parse<MyFireNumber.Core.Presentation.CurrencyPeriod>(item.Period), item.Category)).ToArray();
    }
    public async Task SaveAsync(ProfileIncome item, CancellationToken cancellationToken = default)
    {
        Validate(item.Id, item.Name, item.Amount);
        await database.InitializeAsync(cancellationToken);
        await database.Connection.InsertOrReplaceAsync(new ProfileIncomeEntity { Id = item.Id, Name = item.Name.Trim(), Amount = item.Amount, Period = item.Period.ToString(), Category = item.Category });
    }
    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default) { await database.InitializeAsync(cancellationToken); await database.Connection.DeleteAsync<ProfileIncomeEntity>(id); }
    private static void Validate(string id, string name, double amount) { ArgumentException.ThrowIfNullOrWhiteSpace(id); ArgumentException.ThrowIfNullOrWhiteSpace(name); ArgumentOutOfRangeException.ThrowIfNegative(amount); }
}

public sealed class SqliteProfileExpenseRepository(LocalDatabase database) : IProfileExpenseRepository
{
    public async Task<IReadOnlyList<ProfileExpense>> ListAsync(CancellationToken cancellationToken = default)
    {
        await database.InitializeAsync(cancellationToken);
        var items = await database.Connection.Table<ProfileExpenseEntity>().OrderBy(item => item.Name).ToListAsync();
        return items.Select(item => new ProfileExpense(item.Id, item.Name, item.Amount, Enum.Parse<MyFireNumber.Core.Presentation.CurrencyPeriod>(item.Period), item.Category)).ToArray();
    }
    public async Task SaveAsync(ProfileExpense item, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(item.Id); ArgumentException.ThrowIfNullOrWhiteSpace(item.Name); ArgumentOutOfRangeException.ThrowIfNegative(item.Amount);
        await database.InitializeAsync(cancellationToken);
        await database.Connection.InsertOrReplaceAsync(new ProfileExpenseEntity { Id = item.Id, Name = item.Name.Trim(), Amount = item.Amount, Period = item.Period.ToString(), Category = item.Category });
    }
    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default) { await database.InitializeAsync(cancellationToken); await database.Connection.DeleteAsync<ProfileExpenseEntity>(id); }
}

public sealed class SqliteProfileDebtRepository(LocalDatabase database) : IProfileDebtRepository
{
    public async Task<IReadOnlyList<ProfileDebt>> ListAsync(CancellationToken cancellationToken = default)
    {
        await database.InitializeAsync(cancellationToken);
        var items = await database.Connection.Table<ProfileDebtEntity>().OrderBy(item => item.Name).ToListAsync();
        return items.Select(item => new ProfileDebt(item.Id, item.Name, item.Balance, item.Rate, item.MinimumPayment)).ToArray();
    }
    public async Task SaveAsync(ProfileDebt item, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(item.Id); ArgumentException.ThrowIfNullOrWhiteSpace(item.Name);
        ArgumentOutOfRangeException.ThrowIfNegative(item.Balance);
        ArgumentOutOfRangeException.ThrowIfNegative(item.Rate);
        ArgumentOutOfRangeException.ThrowIfNegative(item.MinimumPayment);
        await database.InitializeAsync(cancellationToken);
        await database.Connection.InsertOrReplaceAsync(new ProfileDebtEntity { Id = item.Id, Name = item.Name.Trim(), Balance = item.Balance, Rate = item.Rate, MinimumPayment = item.MinimumPayment });
    }
    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default) { await database.InitializeAsync(cancellationToken); await database.Connection.DeleteAsync<ProfileDebtEntity>(id); }
}

public sealed class SqliteProfileFinancialSnapshotRepository(
    IProfileRepository profileRepository,
    IProfileAccountRepository accountRepository,
    IProfileIncomeRepository incomeRepository,
    IProfileExpenseRepository expenseRepository,
    IProfileDebtRepository debtRepository) : IProfileFinancialSnapshotRepository
{
    public async Task<ProfileFinancialSnapshot> GetAsync(CancellationToken cancellationToken = default) => new(
        await profileRepository.GetAsync(cancellationToken),
        await accountRepository.ListAsync(cancellationToken),
        await incomeRepository.ListAsync(cancellationToken),
        await expenseRepository.ListAsync(cancellationToken),
        await debtRepository.ListAsync(cancellationToken),
        DateTime.UtcNow.Ticks);
}

public sealed class SqliteRecentActivityRepository(LocalDatabase database) : IRecentActivityRepository
{
    private const int MaximumEntriesPerKind = 20;

    public async Task<IReadOnlyList<RecentActivityRecord>> ListAsync(
        RecentActivityKind kind,
        int limit,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);

        await database.InitializeAsync(cancellationToken);
        var kindValue = kind.ToString();
        var entities = await database.Connection.Table<RecentActivityEntity>()
            .Where(activity => activity.Kind == kindValue)
            .OrderByDescending(activity => activity.LastOpenedAtUtc)
            .Take(limit)
            .ToListAsync();

        return entities.Select(ToRecord).ToArray();
    }

    public async Task TrackAsync(RecentActivityRecord activity, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activity.ItemId);

        await database.InitializeAsync(cancellationToken);
        var kindValue = activity.Kind.ToString();
        await database.Connection.InsertOrReplaceAsync(new RecentActivityEntity
        {
            Key = CreateKey(activity.Kind, activity.ItemId),
            Kind = kindValue,
            ItemId = activity.ItemId,
            LastOpenedAtUtc = activity.LastOpenedAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)
        });

        var staleEntries = await database.Connection.Table<RecentActivityEntity>()
            .Where(entry => entry.Kind == kindValue)
            .OrderByDescending(entry => entry.LastOpenedAtUtc)
            .Skip(MaximumEntriesPerKind)
            .ToListAsync();
        foreach (var staleEntry in staleEntries)
        {
            await database.Connection.DeleteAsync(staleEntry);
        }
    }

    private static string CreateKey(RecentActivityKind kind, string itemId) => $"{kind}:{itemId}";

    private static RecentActivityRecord ToRecord(RecentActivityEntity entity)
    {
        return new RecentActivityRecord(
            Enum.Parse<RecentActivityKind>(entity.Kind),
            entity.ItemId,
            DateTime.Parse(entity.LastOpenedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
    }
}

public sealed class SqliteCorruptPayloadRepository(LocalDatabase database) : ICorruptPayloadRepository
{
    public async Task<IReadOnlyList<CorruptPayloadRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        await database.InitializeAsync(cancellationToken);
        var entities = await database.Connection.Table<CorruptPayloadEntity>()
            .OrderByDescending(payload => payload.QuarantinedAtUtc)
            .ToListAsync();
        return entities.Select(ToRecord).ToArray();
    }

    public async Task QuarantineDraftAsync(DraftRecord draft, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(draft.CalculatorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(draft.PayloadJson);

        await database.InitializeAsync(cancellationToken);
        var entity = CreateEntity(
            CorruptPayloadSourceKind.Draft,
            draft.CalculatorId,
            draft.CalculatorId,
            null,
            draft.PayloadVersion,
            draft.PayloadJson,
            null,
            draft.UpdatedAtUtc);
        await database.Connection.RunInTransactionAsync(connection =>
        {
            connection.Insert(entity);
            connection.Delete<DraftEntity>(draft.CalculatorId);
        });
    }

    public async Task QuarantinePlanAsync(PlanRecord plan, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plan.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(plan.CalculatorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(plan.PayloadJson);

        await database.InitializeAsync(cancellationToken);
        var entity = CreateEntity(
            CorruptPayloadSourceKind.Plan,
            plan.Id,
            plan.CalculatorId,
            plan.Name,
            plan.PayloadVersion,
            plan.PayloadJson,
            plan.CreatedAtUtc,
            plan.UpdatedAtUtc);
        await database.Connection.RunInTransactionAsync(connection =>
        {
            connection.Insert(entity);
            connection.Delete<PlanEntity>(plan.Id);
        });
    }

    private static CorruptPayloadEntity CreateEntity(
        CorruptPayloadSourceKind sourceKind,
        string sourceId,
        string calculatorId,
        string? displayName,
        int payloadVersion,
        string payloadJson,
        DateTime? originalCreatedAtUtc,
        DateTime originalUpdatedAtUtc)
    {
        return new CorruptPayloadEntity
        {
            Id = Guid.NewGuid().ToString("N"),
            SourceKind = sourceKind.ToString(),
            SourceId = sourceId,
            CalculatorId = calculatorId,
            DisplayName = displayName,
            PayloadVersion = payloadVersion,
            PayloadJson = payloadJson,
            OriginalCreatedAtUtc = originalCreatedAtUtc?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            OriginalUpdatedAtUtc = originalUpdatedAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            QuarantinedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };
    }

    private static CorruptPayloadRecord ToRecord(CorruptPayloadEntity entity)
    {
        return new CorruptPayloadRecord(
            entity.Id,
            Enum.Parse<CorruptPayloadSourceKind>(entity.SourceKind),
            entity.SourceId,
            entity.CalculatorId,
            entity.DisplayName,
            entity.PayloadVersion,
            entity.PayloadJson,
            entity.OriginalCreatedAtUtc is null
                ? null
                : DateTime.Parse(entity.OriginalCreatedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            DateTime.Parse(entity.OriginalUpdatedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            DateTime.Parse(entity.QuarantinedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
    }
}