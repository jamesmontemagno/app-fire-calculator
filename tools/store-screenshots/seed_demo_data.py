#!/usr/bin/env python3
"""Seed the My Fire # simulator database with polished demo data for store screenshots.

Writes directly to the app's SQLite file using the same shapes the app's storage
layer reads. No app code is modified and nothing here ships in the product.
"""
import json
import sqlite3
import sys
import uuid
from datetime import datetime, timedelta, timezone

DB = sys.argv[1]

NOW = datetime(2026, 8, 30, 15, 0, 0, tzinfo=timezone.utc)


def iso(dt):
    return dt.strftime("%Y-%m-%dT%H:%M:%S.%f0Z")


# --- Persona -----------------------------------------------------------------
# Alex Rivera, 37, household of 3, targeting full retirement at 55.
BIRTH = "1989-04-12"
PHASED = "2041-04-12"   # age 52
TARGET = "2044-04-12"   # age 55
ANNUAL_INCOME = 182000.0
ANNUAL_EXPENSES = 96000.0

# RetirementAccountType: Deferred=0, Traditional=1, Roth=2, Taxable=3, Savings=4, Hsa=5, Other=6.
# The profile table stores the name; check-in JSON uses the default serializer, so it stores the number.
TYPE_ORDINALS = {"Deferred": 0, "Traditional": 1, "Roth": 2, "Taxable": 3,
                 "Savings": 4, "Hsa": 5, "Other": 6}

ACCOUNTS = [
    # id, name, type, balance, annual contribution, return, avail age, wd rate, payout yrs, tax
    ("acct-401k", "Workplace 401(k)", "Traditional", 412500.0, 23000.0, 0.07, 59, 0.04, 30, 0.25),
    ("acct-roth", "Roth IRA", "Roth", 148200.0, 7000.0, 0.07, 59, 0.04, 30, 0.0),
    ("acct-brokerage", "Taxable Brokerage", "Taxable", 196400.0, 18000.0, 0.065, 37, 0.04, 30, 0.0),
    ("acct-hsa", "Health Savings Account", "Hsa", 38900.0, 4300.0, 0.06, 59, 0.04, 30, 0.0),
    ("acct-cash", "Emergency Savings", "Savings", 42000.0, 2400.0, 0.04, 37, 0.04, 30, 0.0),
]

INCOME = [
    # id, name, annual, start age, end age, growth, is after tax, tax rate
    # Linked calculators sum every entry regardless of start age, so the itemized total is
    # kept equal to ANNUAL_INCOME. Social Security is intentionally left out of the demo
    # rather than inflating today's income with a benefit that starts at 67.
    ("inc-salary", "Primary Salary", 124000.0, 37, 55, 0.03, False, 0.25),
    ("inc-partner", "Partner Income", 40000.0, 37, 55, 0.03, False, 0.22),
    ("inc-rental", "Rental Property", 18000.0, 37, 95, 0.02, False, 0.22),
]

EXPENSES = [
    ("exp-housing", "Housing & Utilities", 34800.0, 37, 95),
    ("exp-food", "Food & Groceries", 15600.0, 37, 95),
    ("exp-transport", "Transportation", 9600.0, 37, 95),
    ("exp-health", "Healthcare", 12000.0, 37, 95),
    ("exp-travel", "Travel & Leisure", 14400.0, 37, 80),
    ("exp-misc", "Everything Else", 9600.0, 37, 95),
]

DEBTS = [
    # id, name, balance, rate, minimum, extra
    ("debt-mortgage", "Mortgage", 268400.0, 0.0425, 1980.0, 250.0),
    ("debt-auto", "Auto Loan", 18250.0, 0.0589, 465.0, 100.0),
    ("debt-card", "Credit Card", 4180.0, 0.1999, 145.0, 300.0),
]

conn = sqlite3.connect(DB)
cur = conn.cursor()

# Linked calculators read the itemized totals, so a mismatch against the headline profile
# figures would show two different incomes on the same screen.
assert sum(i[2] for i in INCOME) == ANNUAL_INCOME, sum(i[2] for i in INCOME)
assert sum(e[2] for e in EXPENSES) == ANNUAL_EXPENSES, sum(e[2] for e in EXPENSES)
for table in (
    "profile", "profile_accounts", "profile_income", "profile_expenses",
    "profile_debts", "financial_check_ins", "plans", "drafts", "recent_activity",
    "calculator_preferences", "corrupt_payloads",
):
    cur.execute(f"DELETE FROM {table}")

cur.execute(
    "INSERT INTO profile (Id, DisplayName, HouseholdName, HouseholdSize, BirthDate,"
    " PhasedRetirementDate, TargetRetirementDate, AnnualIncome, AnnualExpenses)"
    " VALUES (1,?,?,?,?,?,?,?,?)",
    ("Alex Rivera", "The Rivera Household", 3, BIRTH, PHASED, TARGET,
     ANNUAL_INCOME, ANNUAL_EXPENSES),
)

cur.executemany(
    "INSERT INTO profile_accounts (Id, Name, Type, Balance, AnnualContribution,"
    " AnnualReturn, AvailableAge, WithdrawalRate, PayoutYears, EffectiveWithdrawalTaxRate)"
    " VALUES (?,?,?,?,?,?,?,?,?,?)", ACCOUNTS)

cur.executemany(
    "INSERT INTO profile_income (Id, Name, AnnualAmount, StartAge, EndAge, AnnualGrowth,"
    " IsAfterTax, TaxRate) VALUES (?,?,?,?,?,?,?,?)",
    [(i[0], i[1], i[2], i[3], i[4], i[5], 1 if i[6] else 0, i[7]) for i in INCOME])

cur.executemany(
    "INSERT INTO profile_expenses (Id, Name, AnnualAmount, StartAge, EndAge)"
    " VALUES (?,?,?,?,?)", EXPENSES)

cur.executemany(
    "INSERT INTO profile_debts (Id, Name, Balance, Rate, MinimumPayment, ExtraMonthlyPayment)"
    " VALUES (?,?,?,?,?,?)", DEBTS)

# --- Check-in history: 12 monthly snapshots trending up ----------------------
# Balances grow and debts shrink month over month so History & Trends renders a
# convincing net-worth curve. The newest snapshot matches the live balances above.
MONTHS = 12
checkins = []
for step in range(MONTHS):
    # step 0 = oldest, step MONTHS-1 = newest.
    # The newest snapshot is deliberately one notch behind the live balances so the Home
    # dashboard shows genuine growth since the last check-in rather than "no change".
    back = (MONTHS - step)
    completed = NOW - timedelta(days=30 * (back - 1) + 2)
    # Slight non-linear growth for a natural-looking curve.
    growth = 1.0 - (0.0092 * back) - (0.00035 * back * back)
    accounts = [
        {"AccountId": a[0], "Name": a[1], "Type": TYPE_ORDINALS[a[2]],
         "Balance": round(a[3] * growth, 2)}
        for a in ACCOUNTS
    ]
    debts = [
        {"DebtId": d[0], "Name": d[1], "Balance": round(d[2] + (d[4] + d[5]) * back * 0.62, 2)}
        for d in DEBTS
    ]
    checkins.append((
        str(uuid.uuid4()),
        iso(completed),
        json.dumps(accounts),
        json.dumps(debts),
        ANNUAL_INCOME - (back * 900),
        ANNUAL_EXPENSES - (back * 260),
    ))

cur.executemany(
    "INSERT INTO financial_check_ins (Id, CompletedAtUtc, AccountsJson, DebtsJson,"
    " AnnualIncome, AnnualExpenses) VALUES (?,?,?,?,?,?)", checkins)

# --- Saved plans -------------------------------------------------------------
# Payloads use the default System.Text.Json shape the app writes: PascalCase names
# and numeric enum values. DataMode "LinkedProfile" ties a plan to the Accounts data.
TOTAL_BALANCE = sum(a[3] for a in ACCOUNTS)
TOTAL_CONTRIB = sum(a[4] for a in ACCOUNTS)

standard = {
    "CurrentAge": 37, "RetirementAge": 55, "CurrentSavings": TOTAL_BALANCE,
    "AnnualContribution": TOTAL_CONTRIB, "AnnualIncome": ANNUAL_INCOME,
    "ExpectedReturn": 0.07, "InflationRate": 0.03, "WithdrawalRate": 0.04,
    "AnnualExpenses": ANNUAL_EXPENSES,
}
coast = {
    "CurrentAge": 37, "RetirementAge": 55, "CurrentSavings": TOTAL_BALANCE,
    "AnnualContribution": TOTAL_CONTRIB, "ExpectedReturn": 0.07,
    "InflationRate": 0.03, "WithdrawalRate": 0.04, "AnnualExpenses": ANNUAL_EXPENSES,
}
lean = dict(standard, AnnualExpenses=68000.0, RetirementAge=50)
debt = {
    "Debts": [
        {"Id": d[0], "Name": d[1], "Balance": d[2], "Rate": d[3],
         "MinimumPayment": d[4], "ExtraMonthlyPayment": d[5]} for d in DEBTS
    ],
    "MonthlyBudget": sum(d[4] + d[5] for d in DEBTS),
    "ExtraPayment": 0.0, "TargetMonths": 36,
    "Mode": 0,       # FixedBudget
    "Strategy": 1,   # Avalanche
}

PLANS = [
    # id, calculator id, name, payload, data mode, days ago
    ("plan-standard", "standard-fire", "Retire at 55", standard, "LinkedProfile", 3),
    ("plan-coast", "coast-fire", "Coast from 45", coast, "LinkedProfile", 9),
    ("plan-lean", "lean-fire", "Lean exit at 50", lean, "LinkedProfile", 21),
    ("plan-debt", "debt-payoff", "Debt-free by 2029", debt, "LinkedProfile", 34),
]

cur.executemany(
    "INSERT INTO plans (Id, CalculatorId, Name, PayloadVersion, PayloadJson,"
    " CreatedAtUtc, UpdatedAtUtc, DataMode, ProfileRevision) VALUES (?,?,?,?,?,?,?,?,?)",
    [(p[0], p[1], p[2], 1, json.dumps(p[3]),
      iso(NOW - timedelta(days=p[5] + 45)), iso(NOW - timedelta(days=p[5])), p[4], None)
     for p in PLANS])

# Recent activity drives the Home "pick up where you left off" list.
RECENT = [
    ("Plan:plan-standard", "Plan", "plan-standard", 1),
    ("Calculator:coast-fire", "Calculator", "coast-fire", 2),
    ("Calculator:retirement-cash-flow", "Calculator", "retirement-cash-flow", 4),
    ("Plan:plan-debt", "Plan", "plan-debt", 6),
]
cur.executemany(
    "INSERT INTO recent_activity (Key, Kind, ItemId, LastOpenedAtUtc) VALUES (?,?,?,?)",
    [(r[0], r[1], r[2], iso(NOW - timedelta(days=r[3]))) for r in RECENT])

conn.commit()

net = sum(a[3] for a in ACCOUNTS) - sum(d[2] for d in DEBTS)
print(f"Seeded {len(ACCOUNTS)} accounts, {len(INCOME)} income, {len(EXPENSES)} expenses, "
      f"{len(DEBTS)} debts, {len(checkins)} check-ins.")
print(f"Net worth: ${net:,.0f}")
conn.close()
