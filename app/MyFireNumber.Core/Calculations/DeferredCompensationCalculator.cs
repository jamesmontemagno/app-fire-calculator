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
        var projections = new List<RetirementCashFlowPoint>();

        // The verdict below reads these, not the rounded Surplus on the projection points. See #63.
        var exactSurplusByAge = new Dictionary<int, double>();

        for (var age = startAge; age <= endAge; age++)
        {
            var yearsFromNow = age - startAge;
            var inflationMultiplier = Math.Pow(1 + Math.Max(-1, inputs.InflationRate), yearsFromNow);
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
                    balance *= 1 + Math.Max(-1, account.AnnualReturn);

                    // Contributions are entered in today's dollars, so the nominal amount paid in
                    // year k is the entered amount escalated by inflation, matching expenses.
                    if (age < retirementAge)
                    {
                        balance += Math.Max(0, account.AnnualContribution) * inflationMultiplier;
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

            var coreExpenses = Math.Max(0, inputs.AnnualExpenses) * inflationMultiplier;
            var additionalExpenseTotal = 0d;
            foreach (var expense in inputs.AdditionalExpenses)
            {
                var isActive = age >= expense.StartAge && age <= expense.EndAge;
                var amount = isActive ? Math.Max(0, expense.AnnualAmount) * inflationMultiplier : 0;
                expensesByItem[expense.Id] = RoundNonNegative(amount);
                additionalExpenseTotal += amount;
            }

            var expenses = coreExpenses + additionalExpenseTotal;
            var deferredIncome = 0d;
            var withdrawalTaxes = 0d;
            foreach (var account in inputs.Accounts.Where(account => account.Type == RetirementAccountType.Deferred))
            {
                var payoutStartAge = account.AvailableAge;
                var payoutEndAge = payoutStartAge + Math.Max(1, account.PayoutYears) - 1;
                if (age < payoutStartAge || age > payoutEndAge)
                {
                    continue;
                }

                var balance = balances.GetValueOrDefault(account.Id);

                // The undistributed balance keeps earning, so each year distributes the remaining
                // balance over the remaining payout years. That honors the payout period exactly
                // and leaves nothing stranded in an account gap withdrawals can never reach.
                var withdrawal = Math.Min(balance, balance / (payoutEndAge - age + 1));
                var deferredTaxRate = account.EffectiveWithdrawalTaxRate;
                balances[account.Id] = balance - withdrawal;
                withdrawals[account.Id] = withdrawal;
                withdrawalTaxes += withdrawal * deferredTaxRate;
                deferredIncome += withdrawal * (1 - deferredTaxRate);
            }

            var remainingGap = Math.Max(0, expenses - outsideIncome - deferredIncome);
            var portfolioWithdrawals = 0d;
            var policyExcessWithdrawals = 0d;
            if (canWithdraw)
            {
                // Materialized once so both passes see the same accounts in the same order, exactly
                // as the web mirror's `.filter()` does.
                var reachable = inputs.Accounts
                    .Where(account => account.Type != RetirementAccountType.Deferred && age >= account.AvailableAge)
                    .ToArray();

                foreach (var account in reachable)
                {
                    var balance = balances.GetValueOrDefault(account.Id);
                    var taxRate = account.EffectiveWithdrawalTaxRate;
                    var netFactor = 1 - taxRate;

                    // Withdrawals are grossed up so the spendable remainder covers the gap.
                    var grossNeeded = netFactor > 0 ? remainingGap / netFactor : double.PositiveInfinity;
                    var policyLimit = balance * Math.Clamp(account.WithdrawalRate, 0, 1);
                    var withdrawal = Math.Min(Math.Min(balance, grossNeeded), policyLimit);

                    balances[account.Id] = balance - withdrawal;
                    withdrawals[account.Id] = withdrawal;
                    withdrawalTaxes += withdrawal * taxRate;
                    var spendable = withdrawal * netFactor;
                    portfolioWithdrawals += spendable;
                    remainingGap -= spendable;
                }

                // The withdrawal rate is a spending *policy*, not the amount of money that exists, so
                // a year the policy cannot fund is allowed to exceed it rather than report a
                // shortfall next to an untouched balance. That contradiction was issue #56, and the
                // throttle had a worse consequence: while the cap bound, the withdrawal was
                // min(cap, need) = cap, so the whole balance path stopped depending on AnnualExpenses
                // and the spending input silently did nothing.
                //
                // The gate reads the UNFLEXED gap through the same predicate the headline verdict
                // uses, so "would this year have been short" and "is this year short" can never drift
                // apart. It runs as a second pass rather than inline above because an early account
                // must not blow past its cap while a later one still has capped headroom left.
                if (IsShortfall(-remainingGap))
                {
                    // Each account's spendable capacity, which is what the gap is denominated in.
                    var netCapacity = 0d;
                    foreach (var account in reachable)
                    {
                        netCapacity += balances.GetValueOrDefault(account.Id) * (1 - account.EffectiveWithdrawalTaxRate);
                    }

                    if (netCapacity > 0)
                    {
                        // Prorating the net need by net capacity makes the gross withdrawal exactly
                        // proportional to the remaining balance — gross = need * balance / netCapacity
                        // — the same convention DistributeSurplus uses in the other direction: a
                        // surplus prorates in by balance, a shortfall prorates out by balance.
                        //
                        // Taking the scale from Math.Min(need, capacity) bounds it at 1, which is what
                        // guarantees gross <= balance for every account in a single pass: no
                        // iteration, no clamping, and no way to overdraw. When capacity is exhausted
                        // the scale is exactly 1 and every reachable balance lands on exactly 0.
                        var flexScale = Math.Min(remainingGap, netCapacity) / netCapacity;
                        foreach (var account in reachable)
                        {
                            var balance = balances.GetValueOrDefault(account.Id);
                            var taxRate = account.EffectiveWithdrawalTaxRate;
                            var withdrawal = balance * flexScale;
                            balances[account.Id] = balance - withdrawal;
                            withdrawals[account.Id] = withdrawals.GetValueOrDefault(account.Id) + withdrawal;
                            withdrawalTaxes += withdrawal * taxRate;
                            var spendable = withdrawal * (1 - taxRate);
                            portfolioWithdrawals += spendable;
                            remainingGap -= spendable;
                            policyExcessWithdrawals += withdrawal;
                        }
                    }
                }
            }

            var totalIncome = outsideIncome + deferredIncome + portfolioWithdrawals;
            var surplus = totalIncome - expenses;
            exactSurplusByAge[age] = surplus;
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
                RoundSigned(surplus),
                withdrawals,
                accountBalances,
                incomeBySource,
                RoundNonNegative(coreExpenses),
                RoundNonNegative(additionalExpenseTotal),
                expensesByItem,
                RoundNonNegative(withdrawalTaxes),
                RoundNonNegative(policyExcessWithdrawals)));
        }

        var retirementProjection = projections.FirstOrDefault(point => point.Age == retirementAge) ?? projections[0];
        var retirementProjections = projections.Where(point => point.Age >= retirementAge).ToArray();
        var firstShortfallIndex = Array.FindIndex(
            retirementProjections,
            point => IsShortfall(exactSurplusByAge.GetValueOrDefault(point.Age)));

        return new DeferredCompensationResult(
            projections,
            RoundNonNegative(inputs.Accounts.Sum(account => Math.Max(0, account.Balance))),
            retirementProjection.TotalBalance,
            retirementProjection.TotalIncome,
            retirementProjection.Surplus,
            projections[^1].TotalBalance,
            firstShortfallIndex < 0 ? retirementProjections.Length : firstShortfallIndex,
            retirementProjections.Count(point => !IsShortfall(exactSurplusByAge.GetValueOrDefault(point.Age))),
            firstShortfallIndex < 0 ? null : retirementProjections[firstShortfallIndex].Age,
            retirementProjections.Length);
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

    /// <summary>
    /// Rounds a value that is allowed to be negative, for display only.
    ///
    /// <para><c>Surplus</c> is the one field <see cref="RoundNonNegative"/> cannot serve, because
    /// clamping at zero would hide every shortfall. The web mirror previously paired this with bare
    /// <c>Math.round</c>, which rounds half toward +Infinity, so <c>Math.round(-2.5)</c> was <c>-2</c>
    /// against this side's <c>-3</c>. That pairing was issue #63; both platforms now round signed
    /// money away from zero.</para>
    ///
    /// <para>The <c>+ 0d</c> normalizes negative zero to positive zero.
    /// <c>Math.Round(-0.4, MidpointRounding.AwayFromZero)</c> is <c>-0.0</c>, which
    /// <c>ToString("C0")</c> renders as <c>-$0</c> — a negative surplus displayed for a year that is
    /// not short. IEEE 754 gives <c>-0.0 + 0.0 == +0.0</c> while leaving every other value — including
    /// <c>NaN</c> and both infinities — untouched, so the web mirror applies the identical
    /// <c>+ 0</c>.</para>
    /// </summary>
    private static double RoundSigned(double value)
    {
        return Math.Round(value, MidpointRounding.AwayFromZero) + 0d;
    }

    /// <summary>
    /// Half of the whole-dollar unit the surplus is displayed in.
    ///
    /// <para>The funded/shortfall verdict is a tolerance question, not an exact comparison:
    /// <c>surplus</c> is <c>totalIncome - expenses</c>, and both operands accumulate floating-point
    /// error over as many as sixty compounding steps, so a bare <c>surplus &lt; 0</c> would report a
    /// shortfall for a residue of a millionth of a cent. Half a dollar sits roughly thirteen orders of
    /// magnitude above that residue at realistic balances.</para>
    ///
    /// <para>It is exactly half a display unit for a second reason: <c>exact &lt;= -0.5</c> is
    /// equivalent to <c>RoundSigned(exact) &lt; 0</c> for every double, so the figure shown to the user
    /// and the verdict about it can never contradict each other.</para>
    /// </summary>
    private const double ShortfallTolerance = 0.5;

    /// <summary>
    /// Decides whether a year is short, from the UNROUNDED surplus.
    ///
    /// <para>Reading the rounded field instead is what made issue #63 severe. The web mirror's
    /// <c>Math.round(-0.5)</c> is <c>-0</c> and <c>-0 &lt; 0</c> is <c>false</c>, so it reported a
    /// fifty-cent shortfall as a fully funded year while this side — rounding to <c>-1</c> — reported
    /// failure at the first retirement age, from identical inputs. Keeping the verdict on the exact
    /// value means no display rounding rule can move a headline again, and a negative zero can never
    /// enter the comparison.</para>
    /// </summary>
    private static bool IsShortfall(double exactSurplus)
    {
        return exactSurplus <= -ShortfallTolerance;
    }
}