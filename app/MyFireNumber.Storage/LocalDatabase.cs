using MyFireNumber.Core.Calculations;
using MyFireNumber.Core.Profile;
using SQLite;
using System.Text.Json;

namespace MyFireNumber.Storage;

public sealed class LocalDatabase
{
    private readonly SQLiteAsyncConnection connection;
    private readonly SemaphoreSlim initializationLock = new(1, 1);
    private bool isInitialized;

    public LocalDatabase(string databasePath)
    {
        connection = new SQLiteAsyncConnection(databasePath);
    }

    internal SQLiteAsyncConnection Connection => connection;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (isInitialized)
        {
            return;
        }

        await initializationLock.WaitAsync(cancellationToken);
        try
        {
            if (isInitialized)
            {
                return;
            }

            await CreateCurrentSchemaAsync();

            isInitialized = true;
        }
        finally
        {
            initializationLock.Release();
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        await connection.RunInTransactionAsync(database =>
        {
            database.DeleteAll<DraftEntity>();
            database.DeleteAll<PlanEntity>();
            database.DeleteAll<CalculatorPreferenceEntity>();
            database.DeleteAll<RecentActivityEntity>();
            database.DeleteAll<CorruptPayloadEntity>();
            database.DeleteAll<ProfileEntity>();
            database.DeleteAll<ProfileAccountEntity>();
            database.DeleteAll<ProfileIncomeEntity>();
            database.DeleteAll<ProfileExpenseEntity>();
            database.DeleteAll<ProfileDebtEntity>();
            database.DeleteAll<FinancialCheckInEntity>();
        });
    }

    public async Task<LocalDataArchive> ExportAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        var drafts = await connection.Table<DraftEntity>().ToListAsync();
        var plans = await connection.Table<PlanEntity>().ToListAsync();
        var preferences = await connection.Table<CalculatorPreferenceEntity>().ToListAsync();
        var activity = await connection.Table<RecentActivityEntity>().ToListAsync();
        var corruptPayloads = await connection.Table<CorruptPayloadEntity>().ToListAsync();
        var profile = await connection.Table<ProfileEntity>().FirstOrDefaultAsync();
        var profileAccounts = await connection.Table<ProfileAccountEntity>().ToListAsync();
        var profileIncome = await connection.Table<ProfileIncomeEntity>().ToListAsync();
        var profileExpenses = await connection.Table<ProfileExpenseEntity>().ToListAsync();
        var profileDebts = await connection.Table<ProfileDebtEntity>().ToListAsync();
        var financialCheckIns = await connection.Table<FinancialCheckInEntity>().ToListAsync();

        return new LocalDataArchive(
            DateTime.UtcNow,
            drafts.Select(ToRecord).ToArray(),
            plans.Select(ToRecord).ToArray(),
            preferences.Select(item => new CalculatorPreferenceRecord(item.CalculatorId, item.IsVisible, item.SortOrder)).ToArray(),
            activity.Select(ToRecord).ToArray(),
            corruptPayloads.Select(ToRecord).ToArray())
        {
            Profile = ToRecord(profile),
            ProfileAccounts = profileAccounts.Select(ToRecord).ToArray(),
            ProfileIncome = profileIncome.Select(ToRecord).ToArray(),
            ProfileExpenses = profileExpenses.Select(ToRecord).ToArray(),
            ProfileDebts = profileDebts.Select(ToRecord).ToArray(),
            FinancialCheckIns = financialCheckIns.Select(ToFinancialCheckIn).ToArray()
        };
    }

    public async Task ImportAsync(LocalDataArchive archive, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(archive);
        ValidateArchive(archive);
        await InitializeAsync(cancellationToken);
        await connection.RunInTransactionAsync(database =>
        {
            database.DeleteAll<DraftEntity>();
            database.DeleteAll<PlanEntity>();
            database.DeleteAll<CalculatorPreferenceEntity>();
            database.DeleteAll<RecentActivityEntity>();
            database.DeleteAll<CorruptPayloadEntity>();
            database.DeleteAll<ProfileEntity>();
            database.DeleteAll<ProfileAccountEntity>();
            database.DeleteAll<ProfileIncomeEntity>();
            database.DeleteAll<ProfileExpenseEntity>();
            database.DeleteAll<ProfileDebtEntity>();
            database.DeleteAll<FinancialCheckInEntity>();

            database.InsertAll(archive.Drafts.Select(ToEntity));
            database.InsertAll(archive.Plans.Select(ToEntity));
            database.InsertAll(archive.CalculatorPreferences.Select(item => new CalculatorPreferenceEntity
            {
                CalculatorId = item.CalculatorId,
                IsVisible = item.IsVisible,
                SortOrder = item.SortOrder
            }));
            database.InsertAll(archive.RecentActivity.Select(ToEntity));
            database.InsertAll(archive.CorruptPayloads.Select(ToEntity));
            database.Insert(ToEntity(archive.Profile));
            database.InsertAll(archive.ProfileAccounts.Select(ToEntity));
            database.InsertAll(archive.ProfileIncome.Select(ToEntity));
            database.InsertAll(archive.ProfileExpenses.Select(ToEntity));
            database.InsertAll(archive.ProfileDebts.Select(ToEntity));
            database.InsertAll(archive.FinancialCheckIns.Select(ToFinancialCheckInEntity));
        });
    }

    private static void ValidateArchive(LocalDataArchive archive)
    {
        if (archive.Drafts is null ||
            archive.Plans is null ||
            archive.CalculatorPreferences is null ||
            archive.RecentActivity is null ||
            archive.CorruptPayloads is null ||
            archive.ProfileAccounts is null ||
            archive.ProfileIncome is null ||
            archive.ProfileExpenses is null ||
            archive.ProfileDebts is null ||
            archive.FinancialCheckIns is null)
        {
            throw new InvalidDataException("The archive is missing required local data.");
        }

        if (archive.Drafts.Any(item => string.IsNullOrWhiteSpace(item.CalculatorId) || string.IsNullOrWhiteSpace(item.PayloadJson)) ||
            archive.Plans.Any(item => string.IsNullOrWhiteSpace(item.Id) || string.IsNullOrWhiteSpace(item.CalculatorId) || string.IsNullOrWhiteSpace(item.Name) || string.IsNullOrWhiteSpace(item.PayloadJson)) ||
            archive.CalculatorPreferences.Any(item => string.IsNullOrWhiteSpace(item.CalculatorId) || item.SortOrder < 0) ||
            archive.RecentActivity.Any(item => string.IsNullOrWhiteSpace(item.ItemId)) ||
            archive.CorruptPayloads.Any(item => string.IsNullOrWhiteSpace(item.Id) || string.IsNullOrWhiteSpace(item.SourceId) || string.IsNullOrWhiteSpace(item.CalculatorId) || string.IsNullOrWhiteSpace(item.PayloadJson)))
        {
            throw new InvalidDataException("The archive contains invalid local data.");
        }

        if (archive.ProfileAccounts.Any(item => string.IsNullOrWhiteSpace(item.Id) || string.IsNullOrWhiteSpace(item.Name) || item.Balance < 0 || item.AnnualContribution < 0) ||
            archive.ProfileIncome.Any(item =>
                string.IsNullOrWhiteSpace(item.Id) ||
                string.IsNullOrWhiteSpace(item.Name) ||
                item.AnnualAmount < 0 ||
                item.StartAge is < 18 or > 100 ||
                item.EndAge < item.StartAge ||
                item.EndAge > 100 ||
                item.AnnualGrowth is < -1 or > 1 ||
                item.TaxRate is < 0 or > 1) ||
            archive.ProfileExpenses.Any(item =>
                string.IsNullOrWhiteSpace(item.Id) ||
                string.IsNullOrWhiteSpace(item.Name) ||
                item.AnnualAmount < 0 ||
                item.StartAge is < 18 or > 100 ||
                item.EndAge < item.StartAge ||
                item.EndAge > 100) ||
            archive.ProfileDebts.Any(item =>
                string.IsNullOrWhiteSpace(item.Id) ||
                string.IsNullOrWhiteSpace(item.Name) ||
                item.Balance < 0 ||
                item.Rate < 0 ||
                item.MinimumPayment < 0 ||
                item.ExtraMonthlyPayment < 0))
        {
            throw new InvalidDataException("The archive contains invalid profile data.");
        }

        if (!ProfileAgeCalculator.TryValidate(archive.Profile, out var profileError))
        {
            throw new InvalidDataException($"The archive contains invalid profile data. {profileError}");
        }

        if (archive.FinancialCheckIns.Any(checkIn =>
                string.IsNullOrWhiteSpace(checkIn.Id) ||
                checkIn.Accounts is null ||
                checkIn.Debts is null ||
                checkIn.AnnualIncome < 0 ||
                checkIn.AnnualExpenses < 0 ||
                checkIn.Accounts.Any(account => string.IsNullOrWhiteSpace(account.AccountId) || string.IsNullOrWhiteSpace(account.Name) || account.Balance < 0) ||
                checkIn.Debts.Any(debt => string.IsNullOrWhiteSpace(debt.DebtId) || string.IsNullOrWhiteSpace(debt.Name) || debt.Balance < 0)))
        {
            throw new InvalidDataException("The archive contains invalid check-in data.");
        }
    }

    private static DraftRecord ToRecord(DraftEntity item) => new(
        item.CalculatorId,
        item.PayloadVersion,
        item.PayloadJson,
        ParseDate(item.UpdatedAtUtc),
        Enum.Parse<MyFireNumber.Core.Profile.ScenarioDataMode>(item.DataMode),
        item.ProfileRevision);

    private static PlanRecord ToRecord(PlanEntity item) => new(
        item.Id,
        item.CalculatorId,
        item.Name,
        item.PayloadVersion,
        item.PayloadJson,
        ParseDate(item.CreatedAtUtc),
        ParseDate(item.UpdatedAtUtc),
        Enum.Parse<MyFireNumber.Core.Profile.ScenarioDataMode>(item.DataMode),
        item.ProfileRevision);

    private static RecentActivityRecord ToRecord(RecentActivityEntity item) => new(
        Enum.Parse<RecentActivityKind>(item.Kind),
        item.ItemId,
        ParseDate(item.LastOpenedAtUtc));

    private static CorruptPayloadRecord ToRecord(CorruptPayloadEntity item) => new(
        item.Id,
        Enum.Parse<CorruptPayloadSourceKind>(item.SourceKind),
        item.SourceId,
        item.CalculatorId,
        item.DisplayName,
        item.PayloadVersion,
        item.PayloadJson,
        item.OriginalCreatedAtUtc is null ? null : ParseDate(item.OriginalCreatedAtUtc),
        ParseDate(item.OriginalUpdatedAtUtc),
        ParseDate(item.QuarantinedAtUtc));

    private static DraftEntity ToEntity(DraftRecord item) => new()
    {
        CalculatorId = item.CalculatorId,
        PayloadVersion = item.PayloadVersion,
        PayloadJson = item.PayloadJson,
        UpdatedAtUtc = FormatDate(item.UpdatedAtUtc),
        DataMode = item.DataMode.ToString(),
        ProfileRevision = item.ProfileRevision
    };

    private static PlanEntity ToEntity(PlanRecord item) => new()
    {
        Id = item.Id,
        CalculatorId = item.CalculatorId,
        Name = item.Name,
        PayloadVersion = item.PayloadVersion,
        PayloadJson = item.PayloadJson,
        CreatedAtUtc = FormatDate(item.CreatedAtUtc),
        UpdatedAtUtc = FormatDate(item.UpdatedAtUtc),
        DataMode = item.DataMode.ToString(),
        ProfileRevision = item.ProfileRevision
    };

    private static RecentActivityEntity ToEntity(RecentActivityRecord item) => new()
    {
        Key = $"{item.Kind}:{item.ItemId}",
        Kind = item.Kind.ToString(),
        ItemId = item.ItemId,
        LastOpenedAtUtc = FormatDate(item.LastOpenedAtUtc)
    };

    private static CorruptPayloadEntity ToEntity(CorruptPayloadRecord item) => new()
    {
        Id = item.Id,
        SourceKind = item.SourceKind.ToString(),
        SourceId = item.SourceId,
        CalculatorId = item.CalculatorId,
        DisplayName = item.DisplayName,
        PayloadVersion = item.PayloadVersion,
        PayloadJson = item.PayloadJson,
        OriginalCreatedAtUtc = item.OriginalCreatedAtUtc is null ? null : FormatDate(item.OriginalCreatedAtUtc.Value),
        OriginalUpdatedAtUtc = FormatDate(item.OriginalUpdatedAtUtc),
        QuarantinedAtUtc = FormatDate(item.QuarantinedAtUtc)
    };

    private static FinancialProfile ToRecord(ProfileEntity? item) => item is null
        ? FinancialProfile.Empty
        : new FinancialProfile(
            item.DisplayName,
            item.HouseholdName,
            item.HouseholdSize,
            ParseDateOnly(item.BirthDate),
            ParseDateOnly(item.PhasedRetirementDate),
            ParseDateOnly(item.TargetRetirementDate),
            item.AnnualIncome,
            item.AnnualExpenses);

    private static RetirementAccount ToRecord(ProfileAccountEntity item) => new(
        item.Id,
        item.Name,
        Enum.Parse<RetirementAccountType>(item.Type),
        item.Balance,
        item.AnnualContribution,
        item.AnnualReturn,
        item.AvailableAge,
        item.WithdrawalRate,
        item.PayoutYears,
        item.EffectiveWithdrawalTaxRate);

    private static RetirementIncomeSource ToRecord(ProfileIncomeEntity item) => new(
        item.Id,
        item.Name,
        item.AnnualAmount,
        item.StartAge,
        item.EndAge,
        item.AnnualGrowth,
        item.IsAfterTax,
        item.TaxRate);

    private static RetirementExpense ToRecord(ProfileExpenseEntity item) =>
        new(item.Id, item.Name, item.AnnualAmount, item.StartAge, item.EndAge);

    private static DebtItem ToRecord(ProfileDebtEntity item) =>
        new(item.Id, item.Name, item.Balance, item.Rate, item.MinimumPayment, item.ExtraMonthlyPayment);

    internal static FinancialCheckIn ToFinancialCheckIn(FinancialCheckInEntity item) => new(
        item.Id,
        ParseDate(item.CompletedAtUtc),
        JsonSerializer.Deserialize<AccountBalanceEntry[]>(item.AccountsJson) ?? [],
        JsonSerializer.Deserialize<DebtBalanceEntry[]>(item.DebtsJson) ?? [],
        item.AnnualIncome,
        item.AnnualExpenses);

    internal static FinancialCheckInEntity ToFinancialCheckInEntity(FinancialCheckIn item) => new()
    {
        Id = item.Id,
        CompletedAtUtc = FormatDate(item.CompletedAtUtc),
        AccountsJson = JsonSerializer.Serialize(item.Accounts),
        DebtsJson = JsonSerializer.Serialize(item.Debts),
        AnnualIncome = item.AnnualIncome,
        AnnualExpenses = item.AnnualExpenses
    };

    private static ProfileEntity ToEntity(FinancialProfile item) => new()
    {
        Id = 1,
        DisplayName = item.DisplayName,
        HouseholdName = item.HouseholdName,
        HouseholdSize = item.HouseholdSize,
        BirthDate = FormatDateOnly(item.BirthDate),
        PhasedRetirementDate = FormatDateOnly(item.PhasedRetirementDate),
        TargetRetirementDate = FormatDateOnly(item.TargetRetirementDate),
        AnnualIncome = item.AnnualIncome,
        AnnualExpenses = item.AnnualExpenses
    };

    private static ProfileAccountEntity ToEntity(RetirementAccount item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        Type = item.Type.ToString(),
        Balance = item.Balance,
        AnnualContribution = item.AnnualContribution,
        AnnualReturn = item.AnnualReturn,
        AvailableAge = item.AvailableAge,
        WithdrawalRate = item.WithdrawalRate,
        PayoutYears = item.PayoutYears,
        EffectiveWithdrawalTaxRate = item.EffectiveWithdrawalTaxRate
    };

    private static ProfileIncomeEntity ToEntity(RetirementIncomeSource item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        AnnualAmount = item.AnnualAmount,
        StartAge = item.StartAge,
        EndAge = item.EndAge,
        AnnualGrowth = item.AnnualGrowth,
        IsAfterTax = item.IsAfterTax,
        TaxRate = item.TaxRate
    };

    private static ProfileExpenseEntity ToEntity(RetirementExpense item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        AnnualAmount = item.AnnualAmount,
        StartAge = item.StartAge,
        EndAge = item.EndAge
    };

    private static ProfileDebtEntity ToEntity(DebtItem item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        Balance = item.Balance,
        Rate = item.Rate,
        MinimumPayment = item.MinimumPayment,
        ExtraMonthlyPayment = item.ExtraMonthlyPayment
    };

    private static DateOnly? ParseDateOnly(string? value) => string.IsNullOrWhiteSpace(value)
        ? null
        : DateOnly.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

    private static string? FormatDateOnly(DateOnly? value) =>
        value?.ToString("O", System.Globalization.CultureInfo.InvariantCulture);

    private static DateTime ParseDate(string value) => DateTime.Parse(
        value,
        System.Globalization.CultureInfo.InvariantCulture,
        System.Globalization.DateTimeStyles.RoundtripKind);

    private static string FormatDate(DateTime value) => value
        .ToUniversalTime()
        .ToString("O", System.Globalization.CultureInfo.InvariantCulture);

    private async Task CreateCurrentSchemaAsync()
    {
        await connection.CreateTableAsync<DraftEntity>();
        await connection.CreateTableAsync<PlanEntity>();
        await connection.CreateTableAsync<CalculatorPreferenceEntity>();
        await connection.CreateTableAsync<RecentActivityEntity>();
        await connection.CreateTableAsync<CorruptPayloadEntity>();
        await connection.CreateTableAsync<ProfileEntity>();
        await connection.CreateTableAsync<ProfileAccountEntity>();
        await connection.CreateTableAsync<ProfileIncomeEntity>();
        await connection.CreateTableAsync<ProfileExpenseEntity>();
        await connection.CreateTableAsync<ProfileDebtEntity>();
        await connection.CreateTableAsync<FinancialCheckInEntity>();
    }

}

[Table("drafts")]
internal sealed class DraftEntity
{
    [PrimaryKey]
    public string CalculatorId { get; set; } = string.Empty;

    [NotNull]
    public int PayloadVersion { get; set; }

    [NotNull]
    public string PayloadJson { get; set; } = string.Empty;

    [NotNull]
    public string UpdatedAtUtc { get; set; } = string.Empty;

    [NotNull]
    public string DataMode { get; set; } = "Standalone";

    public long? ProfileRevision { get; set; }
}

[Table("plans")]
internal sealed class PlanEntity
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;

    [NotNull]
    public string CalculatorId { get; set; } = string.Empty;

    [NotNull]
    public string Name { get; set; } = string.Empty;

    [NotNull]
    public int PayloadVersion { get; set; }

    [NotNull]
    public string PayloadJson { get; set; } = string.Empty;

    [NotNull]
    public string CreatedAtUtc { get; set; } = string.Empty;

    [NotNull]
    public string UpdatedAtUtc { get; set; } = string.Empty;

    [NotNull]
    public string DataMode { get; set; } = "Standalone";

    public long? ProfileRevision { get; set; }
}

[Table("calculator_preferences")]
internal sealed class CalculatorPreferenceEntity
{
    [PrimaryKey]
    public string CalculatorId { get; set; } = string.Empty;

    [NotNull]
    public bool IsVisible { get; set; }

    [NotNull]
    public int SortOrder { get; set; }
}

[Table("recent_activity")]
internal sealed class RecentActivityEntity
{
    [PrimaryKey]
    public string Key { get; set; } = string.Empty;

    [NotNull]
    public string Kind { get; set; } = string.Empty;

    [NotNull]
    public string ItemId { get; set; } = string.Empty;

    [NotNull]
    public string LastOpenedAtUtc { get; set; } = string.Empty;
}

[Table("corrupt_payloads")]
internal sealed class CorruptPayloadEntity
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;

    [NotNull]
    public string SourceKind { get; set; } = string.Empty;

    [NotNull]
    public string SourceId { get; set; } = string.Empty;

    [NotNull]
    public string CalculatorId { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    [NotNull]
    public int PayloadVersion { get; set; }

    [NotNull]
    public string PayloadJson { get; set; } = string.Empty;

    public string? OriginalCreatedAtUtc { get; set; }

    [NotNull]
    public string OriginalUpdatedAtUtc { get; set; } = string.Empty;

    [NotNull]
    public string QuarantinedAtUtc { get; set; } = string.Empty;
}

[Table("profile")]
internal sealed class ProfileEntity
{
    [PrimaryKey]
    public int Id { get; set; } = 1;

    public string? DisplayName { get; set; }

    public string? HouseholdName { get; set; }

    public int? HouseholdSize { get; set; }

    public string? BirthDate { get; set; }

    public string? PhasedRetirementDate { get; set; }

    public string? TargetRetirementDate { get; set; }

    public double? AnnualIncome { get; set; }

    public double? AnnualExpenses { get; set; }
}

[Table("profile_accounts")]
internal sealed class ProfileAccountEntity
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;

    [NotNull]
    public string Name { get; set; } = string.Empty;

    [NotNull]
    public string Type { get; set; } = string.Empty;

    public double Balance { get; set; }

    public double AnnualContribution { get; set; }

    public double AnnualReturn { get; set; }

    public int AvailableAge { get; set; }

    public double WithdrawalRate { get; set; }

    public int PayoutYears { get; set; }

    public double EffectiveWithdrawalTaxRate { get; set; }
}

[Table("profile_income")]
internal sealed class ProfileIncomeEntity
{
    [PrimaryKey] public string Id { get; set; } = string.Empty;
    [NotNull] public string Name { get; set; } = string.Empty;
    public double AnnualAmount { get; set; }
    public int StartAge { get; set; }
    public int EndAge { get; set; }
    public double AnnualGrowth { get; set; }
    public bool IsAfterTax { get; set; }
    public double TaxRate { get; set; }
}

[Table("profile_expenses")]
internal sealed class ProfileExpenseEntity
{
    [PrimaryKey] public string Id { get; set; } = string.Empty;
    [NotNull] public string Name { get; set; } = string.Empty;
    public double AnnualAmount { get; set; }
    public int StartAge { get; set; }
    public int EndAge { get; set; }
}

[Table("profile_debts")]
internal sealed class ProfileDebtEntity
{
    [PrimaryKey] public string Id { get; set; } = string.Empty;
    [NotNull] public string Name { get; set; } = string.Empty;
    public double Balance { get; set; }
    public double Rate { get; set; }
    public double MinimumPayment { get; set; }
    public double ExtraMonthlyPayment { get; set; }
}

/// <summary>
/// A completed monthly check-in. <see cref="AccountsJson"/> and <see cref="DebtsJson"/> hold the
/// serialized <see cref="AccountBalanceEntry"/>/<see cref="DebtBalanceEntry"/> lists captured at
/// check-in time, so a rename or deletion of the live account/debt afterward never rewrites history.
/// </summary>
[Table("financial_check_ins")]
internal sealed class FinancialCheckInEntity
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;

    [NotNull]
    [Indexed]
    public string CompletedAtUtc { get; set; } = string.Empty;

    [NotNull]
    public string AccountsJson { get; set; } = "[]";

    [NotNull]
    public string DebtsJson { get; set; } = "[]";

    public double AnnualIncome { get; set; }

    public double AnnualExpenses { get; set; }
}