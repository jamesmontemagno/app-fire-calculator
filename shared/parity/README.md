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
