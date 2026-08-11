namespace MyFireNumber.Core.Calculations;

public static class DeferredCompensationCalculator
{
    public static DeferredCompensationResult Calculate(DeferredCompensationInputs inputs)
    {
        var startAge = Math.Max(0, inputs.CurrentAge);
        var retirementAge = Math.Max(startAge, inputs.SemiRetirementAge);
        var endAge = Math.Max(retirementAge, inputs.PlanThroughAge);
        var currentYear = inputs.CurrentYear == 0 ? DateTime.Now.Year : inputs.CurrentYear;
        var balances = inputs.Accounts.ToDictionary(account => account.Id, account => Math.Max(0, account.Balance));
        var deferredAnnualPayouts = new Dictionary<string, double>();
        var projections = new List<RetirementCashFlowPoint>();

        for (var age = startAge; age <= endAge; age++)
        {
            var yearsFromNow = age - startAge;
            var canWithdraw = !inputs.WithdrawOnlyAfterRetirement || age >= retirementAge;
            var withdrawals = new Dictionary<string, double>();
            var accountBalances = new Dictionary<string, double>();
            var incomeBySource = new Dictionary<string, double>();
            var expensesByItem = new Dictionary<string, double>();

            foreach (var account in inputs.Accounts)
            {
                var balance = balances.GetValueOrDefault(account.Id);
                if (age > startAge)
                {
                    var payoutHasStarted = account.Type == RetirementAccountType.Deferred
                        && age >= account.AvailableAge;
                    if (!payoutHasStarted)
                    {
                        balance *= 1 + Math.Max(-1, account.AnnualReturn);
                    }

                    if (age < retirementAge)
                    {
                        balance += Math.Max(0, account.AnnualContribution);
                    }
                }

                balances[account.Id] = balance;
            }

            var outsideIncome = 0d;
            foreach (var source in inputs.IncomeSources)
            {
                var isActive = age >= source.StartAge && age <= source.EndAge;
                var grossAmount = isActive
                    ? Math.Max(0, source.AnnualAmount) * Math.Pow(1 + Math.Max(-1, source.AnnualGrowth), yearsFromNow)
                    : 0;
                var netAmount = source.IsAfterTax
                    ? grossAmount
                    : grossAmount * (1 - Math.Clamp(source.TaxRate, 0, 1));
                incomeBySource[source.Id] = RoundNonNegative(netAmount);
                outsideIncome += netAmount;
            }

            var inflationMultiplier = Math.Pow(1 + Math.Max(-1, inputs.InflationRate), yearsFromNow);
            var coreExpenses = Math.Max(0, inputs.AnnualExpenses) * inflationMultiplier;
            var additionalExpenseTotal = 0d;
            foreach (var expense in inputs.AdditionalExpenses)
            {
                var amount = age >= expense.StartAge ? Math.Max(0, expense.AnnualAmount) * inflationMultiplier : 0;
                expensesByItem[expense.Id] = RoundNonNegative(amount);
                additionalExpenseTotal += amount;
            }

            var expenses = coreExpenses + additionalExpenseTotal;
            var deferredIncome = 0d;
            foreach (var account in inputs.Accounts.Where(account => account.Type == RetirementAccountType.Deferred))
            {
                var payoutStartAge = account.AvailableAge;
                var payoutEndAge = payoutStartAge + Math.Max(1, account.PayoutYears) - 1;
                if (age < payoutStartAge || age > payoutEndAge)
                {
                    continue;
                }

                var balance = balances.GetValueOrDefault(account.Id);
                if (!deferredAnnualPayouts.TryGetValue(account.Id, out var annualPayout))
                {
                    annualPayout = balance / (payoutEndAge - age + 1);
                    deferredAnnualPayouts[account.Id] = annualPayout;
                }

                var withdrawal = Math.Min(balance, annualPayout);
                balances[account.Id] = balance - withdrawal;
                withdrawals[account.Id] = withdrawal;
                deferredIncome += withdrawal;
            }

            var remainingGap = Math.Max(0, expenses - outsideIncome - deferredIncome);
            var portfolioWithdrawals = 0d;
            if (canWithdraw)
            {
                foreach (var account in inputs.Accounts.Where(account => account.Type != RetirementAccountType.Deferred && age >= account.AvailableAge))
                {
                    var balance = balances.GetValueOrDefault(account.Id);
                    var withdrawal = Math.Min(balance, Math.Min(remainingGap, balance * Math.Clamp(account.WithdrawalRate, 0, 1)));
                    balances[account.Id] = balance - withdrawal;
                    withdrawals[account.Id] = withdrawal;
                    portfolioWithdrawals += withdrawal;
                    remainingGap -= withdrawal;
                }
            }

            var totalIncome = outsideIncome + deferredIncome + portfolioWithdrawals;
            var surplus = totalIncome - expenses;
            if (inputs.ReinvestSurplus && surplus > 0)
            {
                DistributeSurplus(balances, inputs.Accounts, surplus);
            }

            foreach (var account in inputs.Accounts)
            {
                withdrawals[account.Id] = RoundNonNegative(withdrawals.GetValueOrDefault(account.Id));
                accountBalances[account.Id] = RoundNonNegative(balances.GetValueOrDefault(account.Id));
            }

            projections.Add(new RetirementCashFlowPoint(
                age,
                currentYear + yearsFromNow,
                RoundNonNegative(balances.Values.Sum()),
                RoundNonNegative(outsideIncome),
                RoundNonNegative(deferredIncome),
                RoundNonNegative(portfolioWithdrawals),
                RoundNonNegative(totalIncome),
                RoundNonNegative(expenses),
                Round(surplus),
                withdrawals,
                accountBalances,
                incomeBySource,
                RoundNonNegative(coreExpenses),
                RoundNonNegative(additionalExpenseTotal),
                expensesByItem));
        }

        var retirementProjection = projections.FirstOrDefault(point => point.Age == retirementAge) ?? projections[0];
        var retirementProjections = projections.Where(point => point.Age >= retirementAge).ToArray();

        return new DeferredCompensationResult(
            projections,
            RoundNonNegative(inputs.Accounts.Sum(account => Math.Max(0, account.Balance))),
            retirementProjection.TotalBalance,
            retirementProjection.TotalIncome,
            retirementProjection.Surplus,
            projections[^1].TotalBalance,
            retirementProjections.Count(point => point.Surplus >= 0));
    }

    private static void DistributeSurplus(
        Dictionary<string, double> balances,
        IReadOnlyList<RetirementAccount> accounts,
        double surplus)
    {
        if (surplus <= 0 || accounts.Count == 0)
        {
            return;
        }

        var totalBalance = accounts.Sum(account => balances.GetValueOrDefault(account.Id));
        foreach (var account in accounts)
        {
            var weight = totalBalance > 0 ? balances.GetValueOrDefault(account.Id) / totalBalance : 1d / accounts.Count;
            balances[account.Id] = balances.GetValueOrDefault(account.Id) + (surplus * weight);
        }
    }

    private static double RoundNonNegative(double value)
    {
        return Math.Round(Math.Max(0, value), MidpointRounding.AwayFromZero);
    }

    private static double Round(double value)
    {
        return Math.Round(value, MidpointRounding.AwayFromZero);
    }
}