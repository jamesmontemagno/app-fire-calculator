# My Fire Number .NET MAUI Implementation Plan

## Purpose

This document is the source of truth for building the native My Fire Number app in
`app/MyFireNumber/`. It defines the agreed product scope, architecture, delivery order,
acceptance criteria, and the repeatable validation loop an implementation agent must follow.

The native app should provide calculator feature parity with the web app while using native
navigation, local storage, lifecycle handling, accessibility, sharing, and platform conventions.
Financial data must remain on-device.

## Status Legend

- `[ ]` Not started
- `[~]` In progress
- `[x]` Complete and validated
- `[!]` Blocked; document the reason in the Decision and Blocker Log

A checkbox may only be marked `[x]` when its acceptance criteria have executable or visual
evidence. Compilation alone is not completion for user-facing work.

## Confirmed Product Decisions

- [x] App name: MyFireNumber
- [x] Application ID: `com.refractored.myfirenumber`
- [x] Initial platforms: iOS and Android
- [x] Core development target: standard iPhone 17 simulator
- [x] Framework: .NET 10 MAUI single-project app
- [x] Navigation: Shell with Home, Calculators, Plans, and Settings tabs
- [x] First launch opens the FIRE Quiz
- [x] First-run quiz is skippable and can be retaken later
- [x] Quiz results prefill a draft for the recommended calculator
- [x] Retaking the quiz asks before replacing an existing calculator draft
- [x] Calculators can be shown, hidden, and reordered
- [x] Hidden calculators remain available in All Calculators
- [x] Drafts are restored automatically
- [x] Users can maintain multiple named scenarios per calculator
- [x] Local database: `sqlite-net-pcl`
- [x] Charts: LiveCharts2 using the MAUI SkiaSharp view package
- [x] Currency defaults to the device region with a Settings override
- [x] SQLite data uses normal app sandbox protection and encrypted OS backup
- [x] Excel exports remain available through the native share sheet
- [x] No web/native URL interoperability is required for the first release
- [x] Books and recommended Apps are excluded from the native app
- [x] No analytics, tracking, backend, account system, or custom cloud sync

## Native Feature Scope

### Included Calculators

Each calculator must match the web implementation's defaults, formulas, validation rules,
presets, input semantics, result metrics, projection data, explanatory copy, and disclaimer.

| Calculator | Web reference | Native status |
| --- | --- | --- |
| Standard FIRE | `web/src/pages/StandardFIRE.tsx` | [ ] |
| Coast FIRE | `web/src/pages/CoastFIRE.tsx` | [ ] |
| Lean FIRE | `web/src/pages/LeanFIRE.tsx` | [ ] |
| Fat FIRE | `web/src/pages/FatFIRE.tsx` | [ ] |
| Barista FIRE | `web/src/pages/BaristaFIRE.tsx` | [ ] |
| Reverse FIRE | `web/src/pages/ReverseFIRE.tsx` | [ ] |
| Withdrawal Rate | `web/src/pages/WithdrawalRate.tsx` | [ ] |
| Savings and Investment Rate | `web/src/pages/SavingsRate.tsx` | [ ] |
| Debt Payoff | `web/src/pages/DebtPayoff.tsx` | [ ] |
| Healthcare Gap | `web/src/pages/HealthcareGap.tsx` | [ ] |
| Retirement Cash Flow | `web/src/pages/DeferredCompensation.tsx` | [ ] |

### Included Supporting Features

- [ ] Home dashboard with enabled calculators in user-defined order
- [ ] All Calculators catalog with search and visibility controls
- [ ] First-run and repeatable FIRE Quiz
- [ ] Automatic per-calculator drafts
- [ ] Multiple named plans with save, rename, duplicate, load, and delete
- [ ] Quick presets where supplied by the web app
- [ ] Live result updates and input validation
- [ ] LiveCharts2 projection and comparison charts
- [ ] Accessible non-chart summaries for every chart
- [ ] Reset actions with confirmation where data would be lost
- [ ] Excel report generation and native sharing
- [ ] Light, dark, and system themes
- [ ] Reduced-motion and high-contrast behavior
- [ ] Device-region currency and number formatting with overrides
- [ ] Import/export of all local app data
- [ ] Delete all local data
- [ ] Educational explanations and financial-advice disclaimers
- [ ] Fully offline calculator, plan, quiz, and settings workflows

### Explicitly Excluded From Version 1

- Books and affiliate content
- Recommended third-party apps
- Web SEO and PWA behavior
- Shareable calculator URLs or deep-link interoperability
- User accounts, telemetry, analytics, ads, or tracking
- Custom cloud synchronization
- Mac Catalyst and Windows release validation

## Architecture

### Core Principles

1. Keep calculations pure and independent of MAUI, SQLite, culture, and UI controls.
2. Use MVVM with constructor injection and compiled XAML bindings.
3. Use one central calculator catalog for IDs, metadata, routes, visibility, and ordering.
4. Store percentages as decimals and currency values as decimal dollar amounts.
5. Keep financial data in app-private local storage and out of logs.
6. Treat drafts, named scenarios, and app preferences as different concepts.
7. Use native platform services through injected interfaces.
8. Build reusable controls only where calculators genuinely share behavior.
9. Version persisted payloads from the first schema.
10. Preserve trimming and AOT compatibility.

### Proposed Project Layout

```text
app/MyFireNumber/
  Calculations/       Pure calculation engines and result models
  Controls/           Reusable XAML controls for inputs, results, and charts
  Converters/         Small presentation-only value converters
  Data/               SQLite connection, entities, migrations, repositories
  Models/             Calculator input, plan, catalog, and settings models
  Services/           Navigation, settings, export, draft, theme, and dialog services
  ViewModels/         Page and reusable-control view models
  Views/
    Calculators/      Calculator pages
    Onboarding/       First-run quiz pages
    Plans/            Plan list and plan management pages
    Settings/         Settings pages
  Resources/
    Images/           Tab and calculator artwork
    Styles/           Theme resources and shared styles
```

Add a separate test project to the solution:

```text
tests/MyFireNumber.Tests/
  Calculations/
  Data/
  Services/
  ViewModels/
  Fixtures/           Golden parity vectors derived from the web implementation
```

### NuGet Dependencies

Resolve mutually compatible stable versions when implementation begins. Do not pin versions in
this document because the project targets the evolving .NET 10 toolchain.

- [ ] `CommunityToolkit.Mvvm`
- [ ] `sqlite-net-pcl`
- [ ] `LiveChartsCore.SkiaSharpView.Maui`
- [ ] `DocumentFormat.OpenXml`
- [ ] Current repository-approved xUnit packages for the test project
- [ ] Optional assertion or mocking package only when it materially improves test clarity

Before broad implementation, prove LiveCharts2 and Open XML work in trimmed Release builds on
both target platforms.

### Dependency Injection Lifetimes

- Singleton: database connection owner, repositories, settings, theme, calculator catalog
- Singleton: stateless calculation services, unless implemented as static pure functions
- Transient: pages and page view models
- Transient: export builders and workflow coordinators unless state requires otherwise

Do not retrieve services directly from `IServiceProvider` inside pages or view models.

## Navigation and Startup

### Shell Structure

```text
TabBar
  Home
  Calculators
  Plans
  Settings

Registered detail routes
  quiz
  calculator/{calculatorId}
  plan/{planId}
  plan/save
  settings/calculators
  settings/defaults
  settings/appearance
  settings/privacy
  settings/accessibility
```

Every Shell tab must have a real icon. Author source icons in `Resources/Images/` and reference
their generated `.png` names from XAML.

### First-Run Flow

1. Create the Shell and initialize lightweight preferences.
2. Read `OnboardingCompleted` from `Preferences`.
3. If false, navigate to the FIRE Quiz after the Shell is ready.
4. Allow Skip; Skip sets `OnboardingCompleted` and navigates to Home.
5. Completion records `OnboardingCompleted`, writes the recommended calculator draft, and opens
   that calculator.
6. Retake Quiz remains available from Home and Settings.
7. If a retake would replace a non-default draft, request confirmation first.

The onboarding flag is a non-sensitive preference. Quiz answers become financial data only when
written to a calculator draft and therefore belong in SQLite.

## Data and Persistence

### Preferences

Use MAUI `Preferences` only for small, non-sensitive values:

- Onboarding completion
- Theme selection: system, light, dark
- Currency override and formatting preferences
- Reduced motion and high contrast overrides
- Haptics and confirmation preferences
- Default launch destination
- Automatic draft restoration preference

### SQLite Entities

#### ScenarioEntity

| Field | Type | Notes |
| --- | --- | --- |
| `Id` | string | GUID stored as text |
| `CalculatorId` | string | Stable catalog ID, never display text |
| `Name` | string | User-facing and validated |
| `PayloadVersion` | integer | Calculator payload schema version |
| `PayloadJson` | string | Serialized typed calculator inputs |
| `CreatedAtUtc` | string | ISO 8601 UTC |
| `UpdatedAtUtc` | string | ISO 8601 UTC |

#### DraftEntity

| Field | Type | Notes |
| --- | --- | --- |
| `CalculatorId` | string | Primary key; one draft per calculator |
| `PayloadVersion` | integer | Calculator payload schema version |
| `PayloadJson` | string | Current unsaved input state |
| `UpdatedAtUtc` | string | ISO 8601 UTC |

#### CalculatorPreferenceEntity

| Field | Type | Notes |
| --- | --- | --- |
| `CalculatorId` | string | Primary key |
| `IsVisible` | boolean | Controls Home visibility |
| `SortOrder` | integer | Stable user-defined order |

#### SchemaMetadataEntity

| Field | Type | Notes |
| --- | --- | --- |
| `Key` | string | Primary key |
| `Value` | string | Database schema and migration metadata |

### Persistence Rules

- [ ] Database path uses `FileSystem.AppDataDirectory`
- [ ] Connection initialization is asynchronous and idempotent
- [ ] WAL and other pragmas are evaluated on both iOS and Android
- [ ] All writes use repository methods and transactions where multiple records must agree
- [ ] Draft saves are debounced and flushed when the app stops or deactivates
- [ ] Corrupt payloads fail safely and remain recoverable/exportable when possible
- [ ] Payload migrations are explicit and covered by tests
- [ ] Database migrations are monotonic and covered by upgrade tests
- [ ] Financial values never appear in application logs
- [ ] Delete-all clears SQLite records and relevant preferences
- [ ] Export-all produces a versioned local archive without network transmission
- [ ] Import validates archive version and data before mutating the database

## Calculator Contracts and Parity

### Source of Truth

- Shared FIRE and debt formulas: `web/src/utils/calculations.ts`
- Retirement Cash Flow formulas: `web/src/utils/deferredCompensation.ts`
- Shared defaults and parameter semantics: `web/src/hooks/useCalculatorParams.ts`
- Retirement defaults and semantics: `web/src/hooks/useDeferredCompensationParams.ts`
- Per-calculator UI and explanatory behavior: `web/src/pages/*.tsx`
- Excel structure and formats: `web/src/utils/excelExport.ts`

### Parity Method

For each calculator:

- [ ] List every input, unit, minimum, maximum, step, and default
- [ ] List every preset and its complete value set
- [ ] List every validation and invalid/empty state
- [ ] Port formulas to pure C# without UI dependencies
- [ ] Capture normal, boundary, zero, and invalid web result fixtures
- [ ] Compare C# results with web fixtures using documented numeric tolerances
- [ ] Verify all result cards and explanatory values
- [ ] Verify all chart series, axes, thresholds, and labels
- [ ] Verify save/load/reset and automatic draft behavior
- [ ] Verify Excel workbook sheets, values, formats, and formulas
- [ ] Verify disclaimer and calculator-specific educational copy

Use exact equality for deterministic integer and decimal outputs. For calculations involving
floating-point powers or logarithms, define a tolerance in the test with a brief reason. Never
silently change formulas to make a test pass.

## Shared Native UI

### Reusable Controls

- [ ] Currency input with locale parsing and annual/monthly toggle
- [ ] Percentage input that stores decimals and displays percentages
- [ ] Age input with accessible validation
- [ ] Integer and decimal numeric input
- [ ] Segmented mode selector
- [ ] Labeled switch/checkbox rows
- [ ] Result metric view
- [ ] Progress-to-FIRE view
- [ ] Preset selector
- [ ] Save/load/reset/export command bar
- [ ] Validation summary
- [ ] Financial disclaimer
- [ ] LiveCharts2 chart host with loading, empty, and accessible summary states
- [ ] Editable debt collection
- [ ] Editable income-source collection
- [ ] Editable account collection
- [ ] Editable additional-expense collection

All pages and `DataTemplate` elements require `x:DataType`. Use `Border`, `Grid`,
`VerticalStackLayout`, and `CollectionView` according to repository guidance. Do not use
`ListView`, `TableView`, renderers, obsolete expand options, or a `CollectionView` nested in a
stack layout.

### Responsive Behavior

- Compact phones: single-column inputs followed by results
- Larger phones and tablets: use additional width for metrics and charts without shrinking text
- Landscape: preserve scrolling and keep controls reachable above the keyboard
- Dynamic text: allow labels to wrap without clipping or overlapping
- Charts: maintain a stable aspect ratio and provide a textual data summary

## Plans

### Plan List

- [ ] Group or filter by calculator
- [ ] Search by plan name
- [ ] Sort by last modified by default
- [ ] Show plan name, calculator, and last modified time
- [ ] Open a plan into its calculator
- [ ] Rename, duplicate, and delete through native actions
- [ ] Show an intentional empty state

### Save Semantics

- Saving a draft as a new plan requests a name.
- Editing a loaded plan changes working state, not the stored plan, until Save is invoked.
- Save updates the loaded plan; Save As creates another plan.
- Duplicate creates a new ID and requests or generates a distinct name.
- Deletion requires confirmation and does not delete the calculator draft.
- Navigating away does not lose edits because the draft is automatic.

## Settings

### Calculator Customization

- [ ] Show/hide calculators
- [ ] Reorder calculators with accessible move controls and native reorder interaction
- [ ] Reset visibility and order to defaults
- [ ] Keep hidden calculators discoverable in All Calculators

### Appearance

- [ ] System, light, and dark theme
- [ ] Chart palette suitable for the active theme
- [ ] Follow system reduced motion by default
- [ ] Optional high-contrast override

### Defaults and Assumptions

- [ ] Device-region currency by default
- [ ] Explicit currency override
- [ ] Locale-aware number formatting
- [ ] Expected return default
- [ ] Inflation default
- [ ] Withdrawal-rate default
- [ ] Common age defaults
- [ ] Explain whether changed defaults affect new drafts only

Changing defaults must not silently rewrite existing drafts or named scenarios.

### App Behavior

- [ ] Default launch destination after onboarding
- [ ] Automatic draft restoration toggle
- [ ] Destructive-action confirmation toggle where appropriate
- [ ] Haptics toggle
- [ ] Retake FIRE Quiz

### Privacy and Data

- [ ] Explain on-device storage and OS backup behavior
- [ ] Export all local app data
- [ ] Import a validated app-data archive
- [ ] Delete all local data with explicit confirmation
- [ ] Link to the privacy policy without transmitting financial values

### Accessibility

- [ ] Follow device text scaling
- [ ] Reduced-motion setting and system behavior
- [ ] High-contrast setting
- [ ] Chart data alternatives
- [ ] Screen-reader-friendly number and percentage descriptions

## Excel Export

Use `DocumentFormat.OpenXml` to generate `.xlsx` files in cache or temporary storage and present
them with MAUI `Share`. Do not retain exports indefinitely.

- [ ] Match the web workbook's calculator title and generated timestamp
- [ ] Include Inputs and Results sheets
- [ ] Include projection sheets when applicable
- [ ] Include calculator-specific collection sheets for debts, accounts, income, and expenses
- [ ] Preserve useful formulas where the web export supplies them
- [ ] Apply currency, percentage, integer, and decimal formats
- [ ] Sanitize worksheet names and user-provided text
- [ ] Open generated workbooks successfully in Apple Numbers and Microsoft Excel
- [ ] Remove or age out stale temporary exports

## Delivery Phases

### Phase 0: Baseline and Technical Spikes

- [~] Record clean iOS simulator and Android emulator launches
- [x] Wire MauiDevFlow for Debug validation
- [x] Add test project to `MyFireNumber.slnx`
- [x] Add selected packages
- [ ] Prove a LiveCharts2 chart renders on iOS and Android
- [ ] Prove a SQLite write/read/migration cycle on iOS and Android
- [ ] Prove a generated `.xlsx` opens from the native share flow
- [ ] Prove trimmed Release builds retain required chart, SQLite, and Open XML behavior

Exit gate: all technical risks have small running proofs on both target platforms.

#### Phase 0 Evidence

| Date | Item | Evidence | Status |
| --- | --- | --- | --- |
| 2026-08-08 | Dependency foundation | iOS simulator build succeeds with CommunityToolkit.Mvvm, sqlite-net-pcl, LiveCharts2, and DocumentFormat.OpenXml. | Complete |
| 2026-08-08 | DevFlow extension | VS Code MAUI extension 1.17.156 failed with `MSB4099`; a documented temporary local scalar-property workaround allows the exact IDE build path to succeed. | Local workaround active |
| 2026-08-08 | iOS baseline | MyFireNumber launches on standard iPhone 17 at 402x874. DevFlow 0.1.0-preview.12.26368.2 connects on port 10223; visible labels and button have non-zero bounds. Tapping the button changes `Click me` to `Clicked 1 time`. | Complete |
| 2026-08-08 | iOS accessibility baseline | Button runtime colors are TextColor `#FFFFFF` on BackgroundColor `#512BD4`; screenshot confirms readable contrast and no overlap. | Complete |
| 2026-08-08 | Test harness | .NET 10 xUnit project is registered in `MyFireNumber.slnx`; one convention test is discovered and passes. | Complete |

#### Temporary DevFlow Extension Workaround

The installed .NET MAUI extension `1.17.156` evaluates an item transform directly in an MSBuild
property condition. MSBuild 18.6 rejects that expression with `MSB4099` before the app project can
compile. Until an extension update includes the correction:

1. Keep `Microsoft.Maui.DevFlow.Agent` explicitly pinned in the app project and set the temporary
  scalar marker `MauiDevFlowAgentExplicitlyReferenced=true`.
2. In the installed extension's `MauiDevFlow.targets`, use that scalar marker to skip package
  injection. MSBuild 18.6 rejects item-list expressions in property conditions, including checks
  against a previously filtered item.
3. Reapply the local extension patch after an extension reinstall only if the upstream target still
  contains the invalid condition.
4. Remove this workaround as soon as the exact VS Code DevFlow build succeeds with an unmodified
  extension.

This patch is local development-machine state and must never be copied into the repository or CI.

### Phase 1: Domain Parity Harness

- [ ] Define typed calculator input and result models
- [ ] Create golden web fixtures for all calculators
- [ ] Port common financial math
- [ ] Port Standard, Coast, Lean, Fat, Barista, and Withdrawal calculations
- [ ] Port Reverse, Savings Rate, and Healthcare calculations currently located in pages
- [ ] Port Debt Payoff calculations
- [ ] Port Retirement Cash Flow calculations
- [ ] Pass parity tests for normal and boundary cases

Exit gate: all calculation tests pass without referencing MAUI.

### Phase 2: App Foundation

- [ ] Implement central catalog and Shell routes
- [ ] Implement four-tab Shell with icons
- [ ] Establish resources, themes, typography, and shared styles
- [ ] Register pages, view models, repositories, and services in DI
- [ ] Implement SQLite initialization and migrations
- [ ] Implement preferences and theme services
- [ ] Implement first-run routing
- [ ] Add global error presentation that does not expose financial values

Exit gate: fresh install enters Quiz; skip reaches a functional four-tab shell.

### Phase 3: Standard FIRE Vertical Slice

- [ ] Build shared input and result controls needed by Standard FIRE
- [ ] Implement Standard FIRE page and view model
- [ ] Implement projection chart and accessible summary
- [ ] Implement presets
- [ ] Implement draft save/restore/reset
- [ ] Implement named plan save/load
- [ ] Implement Excel export and share
- [ ] Validate light/dark, accessibility, lifecycle restoration, and offline behavior

Exit gate: Standard FIRE is complete end to end on iOS and Android.

### Phase 4: Shared FIRE Calculators

- [ ] Coast FIRE
- [ ] Lean FIRE
- [ ] Fat FIRE
- [ ] Barista FIRE
- [ ] Reverse FIRE
- [ ] Withdrawal Rate
- [ ] Savings and Investment Rate
- [ ] Healthcare Gap

Run the complete per-calculator parity checklist for each item before marking it complete.

Exit gate: nine shared-form calculators are complete on both platforms.

### Phase 5: Complex Calculators

- [ ] Debt Payoff editable debt workflow
- [ ] Snowball and Avalanche comparison
- [ ] Debt balance and breakdown charts
- [ ] Retirement Cash Flow scenario inputs
- [ ] Income, account, and additional-expense editors
- [ ] Retirement cash-flow and bucket charts
- [ ] Expandable annual cash-flow detail
- [ ] Complex calculator exports and persistence

Exit gate: collection editing, calculations, charts, plans, and exports pass on both platforms.

### Phase 6: Quiz, Home, Catalog, and Plans

- [ ] Port quiz questions and recommendation rules
- [ ] Implement Skip, completion, retake, and draft-overwrite confirmation
- [ ] Build Home with enabled calculators in selected order
- [ ] Build searchable All Calculators catalog
- [ ] Build Plans list and plan management workflows
- [ ] Verify hidden calculators remain discoverable

Exit gate: all top-level navigation and discovery workflows are complete.

### Phase 7: Full Settings

- [ ] Calculator visibility and ordering
- [ ] Appearance and theme
- [ ] Defaults and assumptions
- [ ] App behavior
- [ ] Privacy and data management
- [ ] Accessibility preferences

Exit gate: settings persist across relaunch and do not corrupt drafts or plans.

### Phase 8: Release Hardening

- [ ] Validate Android minimum supported API behavior
- [ ] Validate iOS minimum supported version behavior
- [ ] Validate phone, tablet, portrait, and landscape layouts
- [ ] Validate VoiceOver and TalkBack workflows
- [ ] Validate large text and display scaling
- [ ] Validate light, dark, high contrast, and reduced motion
- [ ] Validate offline cold start and all local workflows
- [ ] Validate background/foreground and process termination restoration
- [ ] Validate database upgrade from every released schema
- [ ] Validate import/export and corrupted input handling
- [ ] Validate Release trimming and AOT
- [ ] Run Android and iOS GitHub Actions workflows
- [ ] Complete privacy disclosures, app metadata, icons, splash screen, and screenshots

Exit gate: release candidate passes the Definition of Done below.

## Ralph-Style Agent Loop

The implementation agent works in small, independently verifiable slices. It repeatedly chooses
the next unchecked item, proves it, records evidence, and only then advances. The loop is designed
to survive interruptions and prevent large batches of unverified code.

### Loop Invariants

1. Work on one acceptance slice at a time.
2. Start from the controlling code path, neighboring test, or current failing behavior.
3. State one falsifiable hypothesis before editing.
4. Make the smallest change that can prove or disprove the hypothesis.
5. Run the cheapest focused executable validation immediately after the first edit.
6. Launch and inspect the app for every user-facing MAUI change.
7. Validate both target platforms before completing a cross-platform phase item.
8. Never mark a checkbox complete without evidence.
9. Never fix unrelated failures as part of the slice.
10. Stop on repeated failure and document the blocker instead of hiding it.

### Loop State

At the start of each cycle, the agent records:

```text
Phase:
Checklist item:
Target platform:
Current behavior:
Expected behavior:
Hypothesis:
Focused validation:
Files expected to change:
```

### Execute One Cycle

#### 1. Select

- Choose the highest-priority unchecked item whose prerequisites are complete.
- Keep the slice small enough to implement and validate in one working session.
- If the item is too large, add indented sub-items before implementation.

#### 2. Inspect

- Read the owning code, corresponding web implementation, and nearest tests.
- Check for uncommitted user changes and preserve them.
- Identify the exact formula, state transition, route, repository, or view model that owns behavior.

#### 3. Predict

- Write one falsifiable hypothesis.
- Name the cheapest check that would disprove it.
- Define concrete acceptance evidence before editing.

#### 4. Implement

- Apply the smallest coherent edit.
- Keep calculations pure and UI state in view models.
- Add or update focused tests with the behavior.
- Do not broaden scope during the first edit.

#### 5. Validate Narrowly

Run the narrowest available check immediately:

```bash
dotnet test tests/MyFireNumber.Tests/MyFireNumber.Tests.csproj --filter "FullyQualifiedName~RelevantTest"
```

If no focused test exists yet, use the narrowest project test or target-framework compilation
that can falsify the change. A build is an intermediate check, not user-facing completion.

#### 6. Launch and Inspect

For MAUI behavior:

1. Launch with the VS Code .NET MAUI debug command on the selected device.
2. Wait for MauiDevFlow to connect.
3. Inspect the visual tree and verify non-zero bounds, visibility, text, and hierarchy.
4. Use runtime property inspection for actual native input colors.
5. Capture a screenshot for visual layout confirmation.
6. Exercise the interaction with DevFlow actions: fill, tap, scroll, navigate, and back.
7. Background and resume when the slice involves persisted or draft state.
8. Repeat on the other target platform before closing a cross-platform item.

Hot Reload is the first choice while a debug session is active. Rebuild and relaunch only when
Hot Reload fails or cannot apply the change.

#### 7. Check Accessibility

For every changed screen:

- Verify semantic labels, hints, headings, and reading order.
- Verify text scales without clipping or overlap.
- Query actual `BackgroundColor`, `TextColor`, and `PlaceholderColor` for native inputs.
- Verify normal text contrast is at least 4.5:1 and large text is at least 3:1.
- Verify controls remain operable with screen-reader navigation.
- Verify charts have equivalent text summaries.

#### 8. Record Evidence

Append a short entry to the Evidence Log in the pull request or active implementation notes:

```text
Item:
Tests:
iOS evidence:
Android evidence:
Accessibility evidence:
Known limitations:
Files changed:
```

Then mark only the proven checklist item `[x]`.

#### 9. Decide

- PASS: select the next eligible unchecked item.
- LOCAL FAILURE: repair the same slice and rerun the same check.
- HYPOTHESIS FALSE: move one code boundary closer to the behavior owner and retry.
- ENVIRONMENT FAILURE: diagnose the environment, record it, and do not alter product code to
  conceal the issue.
- THIRD FAILURE IN THE SAME SLICE: mark `[!]`, record attempts and output, and request direction.

### Loop Pseudocode

```text
while unchecked_items_exist:
    item = highest_priority_unblocked_item()
    define_cycle_state(item)
    inspect_owning_path_and_reference_behavior()
    hypothesis, focused_check = predict()
    implement_smallest_coherent_change()
    result = run(focused_check)

    if result.failed:
        repair_or_revise_hypothesis_without_expanding_scope()
        continue

    if item.affects_maui_behavior:
        for platform in [ios, android]:
            launch(platform)
            inspect_visual_tree()
            exercise_behavior()
            verify_runtime_colors_and_accessibility()
            capture_evidence()

    if all_acceptance_evidence_passes:
        mark_complete(item)
    else:
        keep_unchecked_and_continue_same_slice()
```

## Validation Matrix

Every completed calculator must have evidence in each applicable column.

| Area | Unit | Integration | iOS runtime | Android runtime | Accessibility |
| --- | --- | --- | --- | --- | --- |
| Formula parity | Required | Fixture serialization | Result values | Result values | Spoken formats |
| Inputs | Parsing/validation | Draft round trip | Keyboard/layout | Keyboard/layout | Labels/contrast |
| Plans | Repository behavior | SQLite migration | CRUD workflow | CRUD workflow | Actions announced |
| Charts | Series construction | Theme mapping | Render/interaction | Render/interaction | Text alternative |
| Export | Workbook structure | File generation | Share/open | Share/open | Named action |
| Settings | View model rules | Persistence | Relaunch | Relaunch | Scalable controls |

## Standard Validation Commands

Use the narrowest command that applies. Do not run both platform builds reflexively after every
small change when a focused test provides the first answer.

```bash
# All non-UI tests
dotnet test tests/MyFireNumber.Tests/MyFireNumber.Tests.csproj

# Android compilation when needed
dotnet build app/MyFireNumber/MyFireNumber.csproj -f net10.0-android

# iOS simulator compilation when needed
dotnet build app/MyFireNumber/MyFireNumber.csproj \
  -f net10.0-ios \
  -p:RuntimeIdentifier=iossimulator-arm64

# Release checks for trimming/AOT/package-sensitive slices
dotnet publish app/MyFireNumber/MyFireNumber.csproj \
  -c Release \
  -f net10.0-android \
  -p:RuntimeIdentifier=android-arm64
```

Normal UI validation must use the VS Code .NET MAUI launch and MauiDevFlow inspection tools, not
`dotnet run`. CI validation is provided by `.github/workflows/maui-android.yml` and
`.github/workflows/maui-ios.yml`.

## Definition of Done

The native version 1 is complete only when all statements are true:

- [ ] All 11 calculators pass documented web parity fixtures
- [ ] First launch presents the skippable FIRE Quiz
- [ ] Quiz recommendation prefills the correct calculator draft
- [ ] Home, Calculators, Plans, and Settings navigation works on iOS and Android
- [ ] Calculator visibility and ordering persist correctly
- [ ] Drafts survive navigation, backgrounding, and process termination
- [ ] Named plans support save, rename, duplicate, load, and delete
- [ ] Database and payload migrations have upgrade tests
- [ ] Every chart renders with LiveCharts2 and has an accessible text equivalent
- [ ] Excel reports open successfully from both platforms
- [ ] Theme, locale, defaults, privacy, behavior, and accessibility settings persist
- [ ] Delete-all and data import/export are validated
- [ ] No financial data is transmitted or written to logs
- [ ] All critical workflows function offline
- [ ] VoiceOver, TalkBack, large text, contrast, and reduced motion are validated
- [ ] Debug and trimmed Release validation pass for iOS and Android
- [ ] Android and iOS CI workflows pass
- [ ] App identifier is `com.refractored.myfirenumber` in project and release configuration
- [ ] Store assets, privacy disclosures, and educational disclaimers are complete

## Agent Start Prompt

Use this prompt when asking an implementation agent to continue the plan:

```text
Continue the native My Fire Number implementation using
docs/MAUI_IMPLEMENTATION_PLAN.md as the source of truth.

Follow the Ralph-style agent loop exactly:
1. Select the highest-priority unblocked unchecked item.
2. State the cycle state, local hypothesis, and focused validation.
3. Implement only that acceptance slice.
4. Run focused tests immediately after the first edit.
5. For MAUI UI or behavior, launch and inspect it with MauiDevFlow on the target platform.
6. Verify runtime colors, accessibility, and interaction behavior.
7. Repeat cross-platform checks where required.
8. Record evidence and mark only proven checklist items complete.
9. Continue until blocked or the requested phase is complete.

Do not implement Books, recommended Apps, analytics, a backend, custom cloud sync, or URL
interoperability. Never transmit or log financial values.
```

## Decision and Blocker Log

Add entries here only when a decision changes or a blocker affects future work.

| Date | Type | Area | Decision or blocker | Resolution |
| --- | --- | --- | --- | --- |
| 2026-08-08 | Decision | Identity | Use `com.refractored.myfirenumber` | Confirmed |
| 2026-08-08 | Decision | Onboarding | Show a skippable FIRE Quiz on first launch | Confirmed |
| 2026-08-08 | Decision | Charts | Use LiveCharts2 | Confirmed |
| 2026-08-08 | Decision | Storage | Use `sqlite-net-pcl` with OS sandbox and backup | Confirmed |
| 2026-08-08 | Decision | Content | Exclude Books and recommended Apps | Confirmed |
| 2026-08-08 | Blocker | DevFlow | VS Code MAUI extension 1.17.156 target fails with MSB4099 before project compilation | Temporarily resolved with documented local target patch; remove after upstream fix |
