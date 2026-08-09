using MyFireNumber.Core.Calculations;
using MyFireNumber.Storage;
using System.Text.Json;

namespace MyFireNumber.Tests.Data;

public sealed class SqlitePlanRepositoryTests : IAsyncLifetime
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"my-fire-number-{Guid.NewGuid():N}.db3");

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }

        return Task.CompletedTask;
    }

    [Fact]
    public async Task ListAsync_ReturnsMatchingPlansNewestFirst()
    {
        var repository = new SqlitePlanRepository(new LocalDatabase(databasePath));
        var createdAt = new DateTime(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc);
        await repository.SaveAsync(new PlanRecord("older", "standard-fire", "Older", 1, "{}", createdAt, createdAt));
        await repository.SaveAsync(new PlanRecord("newer", "standard-fire", "Newer", 1, "{}", createdAt, createdAt.AddMinutes(1)));
        await repository.SaveAsync(new PlanRecord("coast", "coast-fire", "Coast", 1, "{}", createdAt, createdAt.AddMinutes(2)));

        var plans = await repository.ListAsync("standard-fire");

        Assert.Collection(
            plans,
            plan => Assert.Equal("newer", plan.Id),
            plan => Assert.Equal("older", plan.Id));
    }

    [Fact]
    public async Task SaveAsync_PreservesStandardFireDraftPayload()
    {
        var repository = new SqlitePlanRepository(new LocalDatabase(databasePath));
        var createdAt = new DateTime(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc);
        var draft = StandardFireDraft.Default with { CurrentSavings = 250_000 };

        await repository.SaveAsync(new PlanRecord(
            "accelerated-fire",
            "standard-fire",
            "Accelerated FIRE",
            StandardFireDraft.PayloadVersion,
            JsonSerializer.Serialize(draft),
            createdAt,
            createdAt));

        var savedPlan = await repository.GetAsync("accelerated-fire");
        Assert.NotNull(savedPlan);
        var restoredDraft = JsonSerializer.Deserialize<StandardFireDraft>(savedPlan!.PayloadJson);

        Assert.Equal("Accelerated FIRE", savedPlan.Name);
        Assert.Equal(StandardFireDraft.PayloadVersion, savedPlan.PayloadVersion);
        Assert.NotNull(restoredDraft);
        Assert.Equal(250_000, restoredDraft!.CurrentSavings);
    }

    [Fact]
    public async Task SaveAsync_PreservesCoastFireDraftPayload()
    {
        var repository = new SqlitePlanRepository(new LocalDatabase(databasePath));
        var createdAt = new DateTime(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc);
        var draft = CoastFireDraft.Default with { CurrentSavings = 500_000 };

        await repository.SaveAsync(new PlanRecord(
            "coast-complete",
            "coast-fire",
            "Coast Complete",
            CoastFireDraft.PayloadVersion,
            JsonSerializer.Serialize(draft),
            createdAt,
            createdAt));

        var savedPlan = await repository.GetAsync("coast-complete");
        Assert.NotNull(savedPlan);
        var restoredDraft = JsonSerializer.Deserialize<CoastFireDraft>(savedPlan!.PayloadJson);

        Assert.Equal("coast-fire", savedPlan.CalculatorId);
        Assert.Equal("Coast Complete", savedPlan.Name);
        Assert.NotNull(restoredDraft);
        Assert.Equal(500_000, restoredDraft!.CurrentSavings);
    }

    [Fact]
    public async Task SaveAsync_PreservesLeanFireDraftPayload()
    {
        var repository = new SqlitePlanRepository(new LocalDatabase(databasePath));
        var createdAt = new DateTime(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc);
        var draft = LeanFireDraft.Default with { AnnualExpenses = 36_000 };

        await repository.SaveAsync(new PlanRecord(
            "lean-complete",
            "lean-fire",
            "Lean Complete",
            LeanFireDraft.PayloadVersion,
            JsonSerializer.Serialize(draft),
            createdAt,
            createdAt));

        var savedPlan = await repository.GetAsync("lean-complete");
        Assert.NotNull(savedPlan);
        var restoredDraft = JsonSerializer.Deserialize<LeanFireDraft>(savedPlan!.PayloadJson);

        Assert.Equal("lean-fire", savedPlan.CalculatorId);
        Assert.Equal("Lean Complete", savedPlan.Name);
        Assert.NotNull(restoredDraft);
        Assert.Equal(36_000, restoredDraft!.AnnualExpenses);
    }

    [Fact]
    public async Task SaveAsync_PreservesFatFireDraftPayload()
    {
        var repository = new SqlitePlanRepository(new LocalDatabase(databasePath));
        var createdAt = new DateTime(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc);
        var draft = FatFireDraft.Default with { AnnualExpenses = 125_000 };

        await repository.SaveAsync(new PlanRecord(
            "fat-complete",
            "fat-fire",
            "Fat Complete",
            FatFireDraft.PayloadVersion,
            JsonSerializer.Serialize(draft),
            createdAt,
            createdAt));

        var savedPlan = await repository.GetAsync("fat-complete");
        Assert.NotNull(savedPlan);
        var restoredDraft = JsonSerializer.Deserialize<FatFireDraft>(savedPlan!.PayloadJson);

        Assert.Equal("fat-fire", savedPlan.CalculatorId);
        Assert.Equal("Fat Complete", savedPlan.Name);
        Assert.NotNull(restoredDraft);
        Assert.Equal(125_000, restoredDraft!.AnnualExpenses);
    }

    [Fact]
    public async Task SaveAsync_PreservesBaristaFireDraftPayload()
    {
        var repository = new SqlitePlanRepository(new LocalDatabase(databasePath));
        var createdAt = new DateTime(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc);
        var draft = BaristaFireDraft.Default with { PartTimeAnnualIncome = 30_000 };

        await repository.SaveAsync(new PlanRecord(
            "barista-complete",
            "barista-fire",
            "Barista Complete",
            BaristaFireDraft.PayloadVersion,
            JsonSerializer.Serialize(draft),
            createdAt,
            createdAt));

        var savedPlan = await repository.GetAsync("barista-complete");
        Assert.NotNull(savedPlan);
        var restoredDraft = JsonSerializer.Deserialize<BaristaFireDraft>(savedPlan!.PayloadJson);

        Assert.Equal("barista-fire", savedPlan.CalculatorId);
        Assert.Equal("Barista Complete", savedPlan.Name);
        Assert.NotNull(restoredDraft);
        Assert.Equal(30_000, restoredDraft!.PartTimeAnnualIncome);
    }

    [Fact]
    public async Task SaveAsync_PreservesReverseFireDraftPayload()
    {
        var repository = new SqlitePlanRepository(new LocalDatabase(databasePath));
        var createdAt = new DateTime(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc);
        var draft = ReverseFireDraft.Default with { TargetRetirementAge = 50 };

        await repository.SaveAsync(new PlanRecord(
            "reverse-complete",
            "reverse-fire",
            "Reverse Complete",
            ReverseFireDraft.PayloadVersion,
            JsonSerializer.Serialize(draft),
            createdAt,
            createdAt));

        var savedPlan = await repository.GetAsync("reverse-complete");
        Assert.NotNull(savedPlan);
        var restoredDraft = JsonSerializer.Deserialize<ReverseFireDraft>(savedPlan!.PayloadJson);

        Assert.Equal("reverse-fire", savedPlan.CalculatorId);
        Assert.Equal("Reverse Complete", savedPlan.Name);
        Assert.NotNull(restoredDraft);
        Assert.Equal(50, restoredDraft!.TargetRetirementAge);
    }

    [Fact]
    public async Task SaveAsync_PreservesWithdrawalRateDraftPayload()
    {
        var repository = new SqlitePlanRepository(new LocalDatabase(databasePath));
        var createdAt = new DateTime(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc);
        var draft = WithdrawalRateDraft.Default with { WithdrawalRate = 0.035 };

        await repository.SaveAsync(new PlanRecord(
            "withdrawal-complete",
            "withdrawal-rate",
            "Withdrawal Complete",
            WithdrawalRateDraft.PayloadVersion,
            JsonSerializer.Serialize(draft),
            createdAt,
            createdAt));

        var savedPlan = await repository.GetAsync("withdrawal-complete");
        Assert.NotNull(savedPlan);
        var restoredDraft = JsonSerializer.Deserialize<WithdrawalRateDraft>(savedPlan!.PayloadJson);

        Assert.Equal("withdrawal-rate", savedPlan.CalculatorId);
        Assert.Equal("Withdrawal Complete", savedPlan.Name);
        Assert.NotNull(restoredDraft);
        Assert.Equal(0.035, restoredDraft!.WithdrawalRate);
    }

    [Fact]
    public async Task SaveAsync_PreservesSavingsInvestmentDraftPayload()
    {
        var repository = new SqlitePlanRepository(new LocalDatabase(databasePath));
        var createdAt = new DateTime(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc);
        var draft = SavingsInvestmentDraft.Default with { ContributionFrequency = ContributionFrequency.Yearly, ContributionAmount = 12_000 };

        await repository.SaveAsync(new PlanRecord(
            "investment-complete",
            "savings-rate",
            "Investment Complete",
            SavingsInvestmentDraft.PayloadVersion,
            JsonSerializer.Serialize(draft),
            createdAt,
            createdAt));

        var savedPlan = await repository.GetAsync("investment-complete");
        Assert.NotNull(savedPlan);
        var restoredDraft = JsonSerializer.Deserialize<SavingsInvestmentDraft>(savedPlan!.PayloadJson);

        Assert.Equal("savings-rate", savedPlan.CalculatorId);
        Assert.Equal("Investment Complete", savedPlan.Name);
        Assert.NotNull(restoredDraft);
        Assert.Equal(ContributionFrequency.Yearly, restoredDraft!.ContributionFrequency);
        Assert.Equal(12_000, restoredDraft.ContributionAmount);
    }

    [Fact]
    public async Task SaveAsync_PreservesHealthcareGapDraftPayload()
    {
        var repository = new SqlitePlanRepository(new LocalDatabase(databasePath));
        var createdAt = new DateTime(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc);
        var draft = HealthcareGapDraft.Default with { MonthlyPremium = 850, MedicareAge = 67 };

        await repository.SaveAsync(new PlanRecord(
            "healthcare-complete",
            "healthcare-gap",
            "Healthcare Complete",
            HealthcareGapDraft.PayloadVersion,
            JsonSerializer.Serialize(draft),
            createdAt,
            createdAt));

        var savedPlan = await repository.GetAsync("healthcare-complete");
        Assert.NotNull(savedPlan);
        var restoredDraft = JsonSerializer.Deserialize<HealthcareGapDraft>(savedPlan!.PayloadJson);

        Assert.Equal("healthcare-gap", savedPlan.CalculatorId);
        Assert.Equal("Healthcare Complete", savedPlan.Name);
        Assert.NotNull(restoredDraft);
        Assert.Equal(850, restoredDraft!.MonthlyPremium);
        Assert.Equal(67, restoredDraft.MedicareAge);
    }

    [Fact]
    public async Task SaveAsync_PreservesDebtPayoffDraftPayload()
    {
        var repository = new SqlitePlanRepository(new LocalDatabase(databasePath));
        var createdAt = new DateTime(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc);
        var draft = DebtPayoffDraft.Default with
        {
            Debts = [new DebtItem("card", "Credit card", 5_000, 0.1999, 150)],
            MonthlyBudget = 600,
            ExtraPayment = 100,
            Strategy = DebtPayoffStrategy.Avalanche
        };

        await repository.SaveAsync(new PlanRecord(
            "debt-complete",
            "debt-payoff",
            "Debt Complete",
            DebtPayoffDraft.PayloadVersion,
            JsonSerializer.Serialize(draft),
            createdAt,
            createdAt));

        var savedPlan = await repository.GetAsync("debt-complete");
        Assert.NotNull(savedPlan);
        var restoredDraft = JsonSerializer.Deserialize<DebtPayoffDraft>(savedPlan!.PayloadJson);

        Assert.Equal("debt-payoff", savedPlan.CalculatorId);
        Assert.NotNull(restoredDraft);
        var debt = Assert.Single(restoredDraft!.Debts);
        Assert.Equal("Credit card", debt.Name);
        Assert.Equal(DebtPayoffStrategy.Avalanche, restoredDraft.Strategy);
        Assert.Equal(100, restoredDraft.ExtraPayment);
    }

    [Fact]
    public async Task DeleteAsync_RemovesOnlyTheRequestedPlan()
    {
        var repository = new SqlitePlanRepository(new LocalDatabase(databasePath));
        var createdAt = new DateTime(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc);
        await repository.SaveAsync(new PlanRecord("keep", "standard-fire", "Keep", 1, "{}", createdAt, createdAt));
        await repository.SaveAsync(new PlanRecord("remove", "standard-fire", "Remove", 1, "{}", createdAt, createdAt));

        await repository.DeleteAsync("remove");

        var plans = await repository.ListAsync();
        var plan = Assert.Single(plans);
        Assert.Equal("keep", plan.Id);
        Assert.Null(await repository.GetAsync("remove"));
    }

    [Fact]
    public async Task SaveAsync_UpsertsCalculatorPreference()
    {
        var repository = new SqliteCalculatorPreferencesRepository(new LocalDatabase(databasePath));

        await repository.SaveAsync(new CalculatorPreferenceRecord("standard-fire", true, 1));
        await repository.SaveAsync(new CalculatorPreferenceRecord("standard-fire", false, 3));

        var preferences = await repository.ListAsync();

        var preference = Assert.Single(preferences);
        Assert.False(preference.IsVisible);
        Assert.Equal(3, preference.SortOrder);
    }
}