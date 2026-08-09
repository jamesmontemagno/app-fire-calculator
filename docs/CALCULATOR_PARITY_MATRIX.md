# Calculator Parity Matrix

Percentages are stored as decimals and currency values as dollars. Numeric inputs
use locale-aware text entry and have no implicit step. Invalid text, negative
currency, rates outside their stated ranges, impossible age ordering, empty debt
lists, and budgets below required minimum payments produce an in-page validation
summary instead of a calculation.

| Calculator | Inputs and units | Web-parity defaults | Validation and empty state |
| --- | --- | --- | --- |
| Standard FIRE | Current/retirement age; savings, annual contribution/income/expenses; expected return, inflation, withdrawal rate | 30; 55; $100,000; $24,000; $72,000; 7%; 3%; 4%; $48,000 | Ages 18-100 with retirement later; money nonnegative; return/inflation 0-100%; withdrawal 0-100% exclusive |
| Coast FIRE | Current/retirement age; savings, annual contribution/expenses; expected return, inflation, withdrawal rate | 30; 55; $100,000; $24,000; $48,000; 7%; 3%; 4% | Standard age/rate/money constraints |
| Lean FIRE | Standard FIRE inputs | Standard FIRE defaults; calculation expenses capped at $40,000 | Standard constraints; threshold guidance remains visible when entered expenses exceed $40,000 |
| Fat FIRE | Standard FIRE inputs | Standard FIRE defaults | Standard constraints; guidance identifies plans below the $100,000 annual-expense guideline |
| Barista FIRE | Current age; savings, annual contribution/expenses, part-time annual income; expected return, inflation, withdrawal rate | 30; $100,000; $24,000; $48,000; $20,000; 7%; 3%; 4% | Age/rate/money constraints; part-time income cannot exceed invalid numeric bounds |
| Reverse FIRE | Current/target retirement age; savings, annual expenses; expected return, inflation, withdrawal rate | 30; 55; $100,000; $48,000; 7%; 3%; 4% | Target age later than current; standard rate/money constraints |
| Withdrawal Rate | Portfolio value; withdrawal, expected-return, and inflation rates; retirement years | $1,000,000; 4%; 7%; 3%; 30 years | Positive portfolio/years; rates in valid decimal ranges |
| Savings & Investment Rate | Starting amount; contribution amount and monthly/annual frequency; years; expected return; inflation; annual income; current age | $100,000; $500 monthly; 30 years; 7%; 3%; $75,000; age 30 | Nonnegative money; positive years/income; valid rates and age |
| Healthcare Gap | Current, early-retirement, and Medicare ages; monthly premium; annual deductible/out-of-pocket; inflation | 30; 55; 65; $600; $2,500; $2,000; 3% | Strictly increasing ages; nonnegative costs; valid inflation |
| Debt Payoff | Editable debts (name, balance, APR, minimum payment); monthly budget; extra payment; fixed-budget/target-timeline mode; target months; snowball/avalanche strategy | No debts; $1,000 budget; $0 extra; 36 months; fixed budget; snowball | At least one valid debt; positive balances/minimums; APR 0-100%; budget must cover all minimums; positive target months |
| Retirement Cash Flow | Current/semi-retirement/plan-through ages; annual expenses/inflation; editable accounts, income sources, and additional expenses; withdrawal/reinvestment switches | Ages 45/55/90; $80,000; 3%; $300,000 deferred account; $500,000 401(k); $20,000 part-time income; withdraw-after-retirement and reinvest enabled | Ordered ages; unique typed rows; nonnegative amounts; valid return/tax/withdrawal rates and active age ranges |

## Standard FIRE Presets

| Preset | Current / retirement age | Savings | Contribution | Income | Return / inflation / withdrawal | Expenses |
| --- | --- | --- | --- | --- | --- | --- |
| Conservative | 30 / 65 | $50,000 | $12,000 | $80,000 | 6% / 3% / 4% | $60,000 |
| Moderate | 30 / 55 | $100,000 | $24,000 | $96,000 | 7% / 3% / 4% | $48,000 |
| Aggressive | 30 / 45 | $150,000 | $48,000 | $96,000 | 7% / 3% / 4% | $40,000 |
| Fat FIRE | 35 / 50 | $500,000 | $100,000 | $250,000 | 7% / 3% / 3.5% | $120,000 |

Each calculator has typed versioned drafts, automatic local save/restore, reset to
defaults, named-plan save/update, LiveCharts projection text alternatives,
calculator-specific educational copy, and an Open XML export verified by the
native test suite.
