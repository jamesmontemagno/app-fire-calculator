# Shared parity fixtures

`fire-parity-cases.json` is the single set of numbers that both front ends are pinned to.

- The web app reads it from `web/src/utils/__tests__/parityFixtures.ts` (Vitest).
- The MAUI app reads it from `app/MyFireNumber.Tests/Calculations/SharedParityFixtureTests.cs` (xUnit).

It lives here, outside both `web/` and `app/`, because neither platform owns it. Before this file
existed, "web and MAUI agree" was enforced by review alone: the MAUI suite asserted
`FinancialCalculator.cs` against constants that had *once* been copied from the web, so a change to
`calculations.ts` that silently diverged would pass CI cleanly. That is issue #54.

## The rule that makes this worth having

**Never regenerate an expected value by running either implementation and pasting the output.**

A test that asserts whatever the code currently prints is a screenshot, not a test. Two of the bugs
the cross-platform audit fixed (#45's double-charged interest, #46's projection that disagreed with
its own headline) shipped precisely because the behaviour looked intentional — nothing independent
contradicted it.

Every case therefore carries a **`derivation`** field stating how its numbers were obtained: the
closed form, the hand arithmetic, or the invariant. A case without a real derivation is not
reviewable and should not be merged.

`web/src/utils/__tests__/fixtureSelfCheck.test.ts` enforces this mechanically. It re-derives the
closed-form-checkable values a third time, in TypeScript, from `oracles.ts` — a module forbidden
from importing the code under test — and fails if a fixture value has drifted toward an
implementation. So the chain is:

```
independent algebra  ===  this fixture  ===  web implementation
                                        ===  MAUI implementation
```

## Case shape

```jsonc
{
  "id": "fire-defaults-inflation",
  "kind": "fire",                  // fire | debt | withdrawal | investment | healthcare | deferred
  "description": "Shipped web defaults with inflation-escalated contributions.",
  "derivation": "rho = 1.07/1.03 - 1 = 0.038834951456...; n = ln((C + T*rho)/(C + PV*rho))/ln(1+rho) = 24.3838964605, so fireAge = 54.4.",
  "inputs":   { "currentAge": 30, "currentSavings": 100000, ... },
  "expected": { "fireNumber": 1200000, "fireAge": 54.4, ... }
}
```

Inputs are named **semantically**, never positionally. The two platforms genuinely disagree on
argument order — web's `calculateCoastFIRE` takes eight positional arguments with `annualExpenses`
*before* `withdrawalRate`, while MAUI takes a single `FireInputs` record — so each side keeps a thin
adapter that maps these names onto its own call shape. Encoding positions here would bake one
platform's ordering into a supposedly neutral artifact.

### Conventions

- Percentages are decimals (`0.07` is 7%), currency is dollars, consistent with the rest of the repo.
- Values the app rounds for display are stored rounded. Both platforms round **away from zero** —
  `Math.round(Math.abs(x))` carrying the sign back on web, `MidpointRounding.AwayFromZero` on MAUI.
  Non-negative fields are clamped at zero first, so the midpoint rule is only observable on `surplus`,
  the one signed field. Negative zero is normalized to positive zero before it reaches a fixture or a
  screen: it formats as `-$0` under both `Intl.NumberFormat` and C# `ToString("C0")`, while still
  satisfying `>= 0`. This paragraph used to describe the convention as JS `Math.round` paired with C#
  `AwayFromZero` — that pairing disagreed at every negative midpoint and was issue #63.
- A rounded field is a **display** value. Nothing may branch on one. The funded/shortfall verdict in
  the deferred cases is taken from the unrounded surplus against an explicit half-dollar tolerance,
  because deriving it from the rounded figure is what let `Math.round(-0.5) === -0` (and `-0 < 0`
  being `false`) report a fifty-cent shortfall as a fully funded plan on web while MAUI reported
  failure at the first retirement age.
- Values the app does not round are stored at full `double` precision and compared with a tolerance.
- Where a case exists to pin a **policy choice** rather than an arithmetic result, at least one
  expected value has to differ under the alternative policy. The `deferred` cap-flex cases prorate
  withdrawals across reachable accounts by balance; `deferred-cap-flex-prorates-and-skips-locked-accounts`
  therefore ends on `178750` specifically because a taxable-first rule would end on `180000`. A
  proration rule that no expectation distinguishes from its alternative is not actually pinned, and
  the next person to touch the ordering would see a green suite.
- A case that deliberately neutralizes part of the pipeline says so in its `derivation`. The
  cap-flex cases that isolate the flex pass set `withdrawalRate` to `0` on every account, which is
  not the shipped 4% default; a reader who assumes the default derives different figures and
  concludes the fixture is broken.
- `Infinity` is carried as the **string** `"Infinity"`, because JSON has no infinity literal. Both
  consumers decode the sentinel explicitly. An unreachable target is a correct answer — a 2% return
  against 5% inflation converges to a $700,000 fixed point and can never reach $1.25M — so it is
  asserted, not treated as a missing value.
- `year` is deliberately **not** in any expectation. The web derives calendar years from
  `new Date().getFullYear()` while MAUI takes an explicit `ProjectionStartYear`, so calendar
  behaviour is covered per-platform instead of pretending it is shared.

## Adding a case

1. Derive the expected values independently — closed form, hand arithmetic, or an invariant.
2. Write the `derivation` field so a reviewer can check it without running anything.
3. Add oracle coverage in `fixtureSelfCheck.test.ts` if the case is closed-form-checkable.
4. Run `npm test` in `web/` **and** `dotnet test app/MyFireNumber.Tests`. Both must pass.

A case of an existing `kind` needs nothing beyond the JSON. A **new kind** also needs wiring on both
sides, because each platform adapts the fixture's semantic input names onto its own call shape: an
interface plus a `casesOfKind` export and an adapter in `parityFixtures.ts` and a block in
`parity.test.ts` on web, and a `TheoryData` member, a `[Theory]`, and a `JsonElement` adapter in
`SharedParityFixtureTests.cs` on MAUI. Keep both adapters strict — read every field with a hard get
so a missing one throws, rather than defaulting and quietly testing a different scenario than the
`derivation` describes.

Editing this file triggers both CI jobs in `.github/workflows/tests.yml`; the path filters are there
so a fixture change cannot land without re-running both suites.

---

# `periodic-fields.json`

A second artifact in this directory, pinning a **policy** rather than a set of numbers: which currency
inputs are recurring amounts, and which period each one is canonically stored in.

- Web reads it from `web/src/utils/__tests__/periodicFieldInventory.test.ts`.
- MAUI reads it from `app/MyFireNumber.Tests/Presentation/SharedPeriodicFieldInventoryTests.cs`.

## Why a policy belongs here when conversion arithmetic does not

The monthly/annual toggle is display-only: a recurring amount is stored in one canonical period, every
calculation runs on the canonical value, and the other period is produced at the display edge. So
there is no cross-platform *number* to pin — and a case asserting `50000 / 12 = 4166.67` would be the
screenshot test this README forbids, because that identity is the implementation restated rather than
an independent oracle. Conversion behaviour is pinned per platform instead, against a
lossless-round-trip invariant the implementation can actually fail.

What *is* shared, and what review alone was previously holding, is the inventory. Almost every field is
canonically annual, but the healthcare premium is canonically monthly. A mechanism that assumed all
fields were annual would read a $600 premium as $50 a month — wrong by 144x, silently, and identically
plausible on screen. That makes the policy distinguishable from its alternative, which is the bar this
directory sets for being worth pinning.

It also covers a gap neither suite can reach on its own: `MyFireNumber.Tests` cannot reference the MAUI
single-project, so no unit test can prove a XAML `Entry` is bound to a period-aware field. A field made
periodic on one platform and forgotten on the other is invisible to both suites unless something
outside both of them holds the list.

## Shape

Every shipped calculator appears, including those with **no** periodic fields, which declare an empty
list rather than being omitted — absence is indistinguishable from an oversight. `webPage` names the
`.tsx` the web scan reads, since MAUI's three FIRE variants share one page while web gives each its own.

Both consumers additionally assert the inventory is not silently empty, so an artifact that failed to
load cannot make an empty list agree with an empty list and report success.

## Adding a field

1. Add it to the calculator's `fields`, with the period the value is actually stored in.
2. Web: pass `periodic` (and `storedPeriod="monthly"` when it is not annual) to that `CurrencyInput`.
3. MAUI: declare it in `PeriodicFieldCatalog`, route the view model's text property through
   `PeriodicText`/`SetPeriodicText`, and bind `PeriodQualifier`/`PeriodSuffix` on the page.
4. Run `npm test` in `web/` **and** `dotnet test app/MyFireNumber.Tests`. Both must pass.
