# My Fire Number .NET MAUI Implementation Plan

## Purpose

This document is the source of truth for building the native My Fire Number app in
`app/MyFireNumber/`. It defines the agreed product scope, architecture, delivery order,
acceptance criteria, and the repeatable validation loop an implementation agent must follow.

The native app should provide calculator feature parity with the web app while using native
navigation, local storage, lifecycle handling, accessibility, sharing, and platform conventions.
Financial data must remain on-device.

**Current status:** Feature implementation and iOS simulator validation are complete. Version 1
release qualification remains blocked on Android runtime automation, minimum-OS simulators/devices,
screen-reader and landscape device passes, hosted CI from a pushed branch, Apple Numbers, production
legal-page deployment, and store-console submission.

## Status Legend

- `[ ]` Not started
- `[~]` In progress
- `[x]` Complete and validated
- `[!]` Blocked; document the reason in the Decision and Blocker Log

A checkbox may only be marked `[x]` when its acceptance criteria have executable or visual
evidence. Compilation alone is not completion for user-facing work.

## Confirmed Product Decisions

- [x] Product name: My Fire Number; home-screen display name: `My Fire #`
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
| Standard FIRE | `web/src/pages/StandardFIRE.tsx` | [~] iOS complete; Android runtime pending |
| Coast FIRE | `web/src/pages/CoastFIRE.tsx` | [~] iOS complete; Android runtime pending |
| Lean FIRE | `web/src/pages/LeanFIRE.tsx` | [~] iOS complete; Android runtime pending |
| Fat FIRE | `web/src/pages/FatFIRE.tsx` | [~] iOS complete; Android runtime pending |
| Barista FIRE | `web/src/pages/BaristaFIRE.tsx` | [~] iOS complete; Android runtime pending |
| Reverse FIRE | `web/src/pages/ReverseFIRE.tsx` | [~] iOS complete; Android runtime pending |
| Withdrawal Rate | `web/src/pages/WithdrawalRate.tsx` | [~] iOS complete; Android runtime pending |
| Savings and Investment Rate | `web/src/pages/SavingsRate.tsx` | [~] iOS complete; Android runtime pending |
| Debt Payoff | `web/src/pages/DebtPayoff.tsx` | [~] iOS complete; Android runtime pending |
| Healthcare Gap | `web/src/pages/HealthcareGap.tsx` | [~] iOS complete; Android runtime pending |
| Retirement Cash Flow | `web/src/pages/DeferredCompensation.tsx` | [~] iOS complete; Android runtime pending |

### Included Supporting Features

- [~] Home dashboard with enabled calculators in user-defined order (implemented; device validation pending)
- [~] All Calculators catalog with search and visibility controls (implemented and validated on iOS; Android pending)
- [x] First-run and repeatable FIRE Quiz
- [~] Automatic per-calculator drafts (implemented and validated on iOS; Android pending)
- [~] Multiple named plans with save, rename, duplicate, load, and delete (implemented and validated on iOS; Android pending)
- [~] Quick presets where supplied by the web app (implemented and validated on iOS; Android pending)
- [~] Live result updates and input validation (implemented and validated on iOS; Android pending)
- [~] LiveCharts2 projection and comparison charts (implemented and validated on iOS; Android pending)
- [~] Accessible non-chart summaries for every chart (implemented and validated on iOS; Android pending)
- [~] Reset actions with confirmation where data would be lost (implemented and validated on iOS; Android pending)
- [~] Excel report generation and native sharing (implemented and validated on iOS; Android pending)
- [~] Light, dark, and system themes (implemented and validated on iOS; Android pending)
- [~] Reduced-motion and high-contrast behavior (implemented and validated on iOS; Android pending)
- [~] Device-region currency and number formatting with overrides (implemented and validated on iOS; Android pending)
- [~] Import/export of all local app data (implemented and validated on iOS; Android pending)
- [~] Delete all local data (implemented and validated on iOS; Android pending)
- [~] Educational explanations and financial-advice disclaimers (implemented and validated on iOS; Android pending)
- [~] Fully offline calculator, plan, quiz, and settings workflows (no runtime backend dependency; offline cold-start matrix pending)

### Active User UX Queue

This queue records direct product-review feedback separately from phase evidence so later
implementation passes cannot lose requested refinements.

- [x] Retirement Cash Flow supports custom accounts, income sources, and expenses.
- [x] Calculator and plan cards open when the card is tapped; redundant Open buttons are removed.
- [~] Plans expose Rename, Duplicate, and Delete as visible accessible actions plus swipe shortcuts; iOS prompts are validated and Android remains.
- [x] Root tabs hide the navigation bar while pushed calculators restore a contextual title and toolbar.
- [x] Settings uses icon controls for calculator order/visibility and an appearance-first segmented selector.
- [x] Plans search and filtering share one compact surface in the collection header.
- [x] Calculator search is theme-aware and calculator cards have distinct icons.
- [x] Home shows the three most recently used calculators and opened plans.
- [x] Home shows onboarding/recommendation guidance only before meaningful calculator or plan activity; afterward, navigation tabs replace the large starter catalog. Include quiz recommendation and retake actions.
- [x] Replace the default .NET app icon and splash screen with branded My Fire Number assets.
- [x] Remove the duplicate large calculator title/summary block when the native navigation bar already identifies the calculator.
- [~] Move Privacy to the bottom of Settings, link Terms and Privacy to the website, and update web Terms to cover the mobile app.
- [x] Keep Retirement Cash Flow's main sections visible and use polished row-level accordions for individual accounts, income sources, and additional expenses. New rows expand for editing; existing rows start collapsed.
- [x] Use context-aware plan actions: fresh drafts say Save to Plans, loaded plans say Update Plan. Keep Duplicate in Plans and remove redundant Save As.
- [x] Group the Calculators catalog into FIRE, Finance, and Cash Flow sections while preserving search and hidden-calculator discovery.
- [x] Add a skippable first-run defaults step before the FIRE Quiz, backed by the existing calculator-defaults service.
- [x] Pair expected return, inflation, and withdrawal-rate defaults with synchronized sliders in Settings.
- [x] Move calculator search into a scrolling CollectionView header with tab-consistent title, icon, and supporting copy.
- [x] Clarify the Settings launch-tab selector with immediate persisted-selection feedback.
- [x] Verify Settings theme icons and saved-plan search colors in light mode.

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

- [x] `CommunityToolkit.Mvvm`
- [x] `sqlite-net-pcl`
- [x] `LiveChartsCore.SkiaSharpView.Maui`
- [x] `DocumentFormat.OpenXml`
- [x] Current repository-approved xUnit packages for the test project
- [x] Optional assertion or mocking package evaluated; xUnit assertions remain clearer for the current tests

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

- [x] Database path uses `FileSystem.AppDataDirectory`
- [x] Connection initialization is asynchronous and idempotent
- [x] WAL and other pragmas are evaluated on both iOS and Android
- [x] All writes use repository methods and transactions where multiple records must agree
- [x] Draft saves are debounced and flushed when the app stops or deactivates
- [x] Corrupt payloads fail safely and remain recoverable/exportable when possible
- [x] Payload migrations are explicit and covered by tests
- [x] Database migrations are monotonic and covered by upgrade tests
- [x] Financial values never appear in application logs
- [~] Delete-all clears SQLite records and relevant preferences (implemented and validated on iOS; Android pending)
- [x] Export-all produces a versioned local archive without network transmission
- [x] Import validates archive version and data before mutating the database

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

- [x] List every input, unit, minimum, maximum, step, and default in `docs/CALCULATOR_PARITY_MATRIX.md`
- [x] List every preset and its complete value set in `docs/CALCULATOR_PARITY_MATRIX.md`
- [x] List every validation and invalid/empty state in `docs/CALCULATOR_PARITY_MATRIX.md`
- [x] Port formulas to pure C# without UI dependencies
- [x] Capture normal, boundary, zero, and invalid web result fixtures
- [x] Compare C# results with web fixtures using documented numeric tolerances
- [~] Verify all result cards and explanatory values (iOS complete; Android pending)
- [~] Verify all chart series, axes, thresholds, and labels (iOS complete; Android pending)
- [~] Verify save/load/reset and automatic draft behavior (iOS complete; Android pending)
- [x] Verify Excel workbook sheets, values, formats, and formulas
- [~] Verify disclaimer and calculator-specific educational copy (iOS complete; Android pending)

Use exact equality for deterministic integer and decimal outputs. For calculations involving
floating-point powers or logarithms, define a tolerance in the test with a brief reason. Never
silently change formulas to make a test pass.

## Shared Native UI

### Reusable Controls

- [x] Currency input with locale parsing and annual/monthly toggle
- [x] Percentage input that stores decimals and displays percentages
- [x] Age input with accessible validation
- [x] Integer and decimal numeric input
- [x] Segmented mode selector
- [x] Labeled switch/checkbox rows
- [x] Result metric view
- [x] Progress-to-FIRE view
- [x] Preset selector
- [x] Save/load/reset/export command bar
- [x] Validation summary
- [x] Financial disclaimer
- [x] LiveCharts2 chart host with loading, empty, and accessible summary states
- [x] Editable debt collection
- [x] Editable income-source collection
- [x] Editable account collection
- [x] Editable additional-expense collection

All pages and `DataTemplate` elements require `x:DataType`. Use `Border`, `Grid`,
`VerticalStackLayout`, and `CollectionView` according to repository guidance. Do not use
`ListView`, `TableView`, renderers, obsolete expand options, or a `CollectionView` nested in a
stack layout.

### Iconography

- Use the bundled Font Awesome Free Solid 6.7.2 font for native UI glyphs.
- Prefer `FontImageSource` for an icon paired with an action label; icon-only commands must have
  a semantic description and a tooltip where the platform supports hover.
- Keep Shell source icons as SVG assets referenced as generated PNGs, as required by MAUI.
- Use icons to reinforce clear actions and landmarks, not as a substitute for visible text.

### Responsive Behavior

- Compact phones: single-column inputs followed by results
- Larger phones and tablets: use additional width for metrics and charts without shrinking text
- Landscape: preserve scrolling and keep controls reachable above the keyboard
- Dynamic text: allow labels to wrap without clipping or overlapping
- Charts: maintain a stable aspect ratio and provide a textual data summary

## Plans

### Plan List

- [~] Group or filter by calculator (implemented; device validation pending)
- [~] Search by plan name (implemented; device validation pending)
- [x] Sort by last modified by default
- [x] Show plan name, calculator, and last modified time
- [x] Open a plan into its calculator
- [~] Rename, duplicate, and delete through native actions (implemented and prompt-validated on iOS; Android pending)
- [~] Show an intentional empty state (iOS validated; Android pending)

### Save Semantics

- Saving a draft as a new plan requests a name.
- Editing a loaded plan changes working state, not the stored plan, until Save is invoked.
- [~] Save updates the loaded plan; Save As creates another plan. Both semantics are implemented and validated on iOS; Android remains pending.
- Duplicate creates a new ID and requests or generates a distinct name.
- Deletion requires confirmation and does not delete the calculator draft.
- Navigating away does not lose edits because the draft is automatic.

## Settings

### Calculator Customization

- [~] Show/hide calculators (implemented; device validation pending)
- [~] Reorder calculators with accessible move controls and native reorder interaction (accessible move controls implemented; device validation pending)
- [~] Reset visibility and order to defaults (implemented; device validation pending)
- [~] Keep hidden calculators discoverable in All Calculators (validated on iOS; Android pending)

### Appearance

- [~] System, light, and dark theme (implemented and persisted; device validation pending)
- [x] Chart palette suitable for the active theme
- [~] Follow system reduced motion by default (chart motion override implemented and validated on iOS; OS preference detection and Android pending)
- [~] Optional high-contrast override (dark high-contrast palette implemented and validated on iOS; Android pending)

### Defaults and Assumptions

- [~] Device-region currency by default (implemented and validated on iOS; Android pending)
- [~] Explicit currency override (implemented and validated on iOS; Android pending)
- [~] Locale-aware number formatting (device separators retained with currency overrides; broader locale matrix pending)
- [~] Expected return default (implemented and validated on iOS; Android pending)
- [~] Inflation default (implemented and validated on iOS; Android pending)
- [~] Withdrawal-rate default (implemented and validated on iOS; Android pending)
- [~] Common age defaults (implemented and validated on iOS; Android pending)
- [x] Explain whether changed defaults affect new drafts only

Changing defaults must not silently rewrite existing drafts or named scenarios.

### App Behavior

- [~] Default launch destination after onboarding (implemented and validated on iOS; Android pending)
- [~] Automatic draft restoration toggle (implemented and validated on iOS; Android pending)
- [~] Destructive-action confirmation toggle where appropriate (plan deletion implemented; iOS validation complete, Android pending)
- [~] Haptics toggle (implemented for plan save/delete; physical-device validation pending)
- [~] Retake FIRE Quiz (implemented and validated on iOS; Android pending)

### Privacy and Data

- [~] Explain on-device storage and OS backup behavior (local-only storage and user-invoked export documented; platform backup disclosure pending)
- [~] Export all local app data (implemented and validated on iOS; Android pending)
- [~] Import a validated app-data archive (implemented, storage-tested, and prompt-validated on iOS; Android pending)
- [~] Delete all local data with explicit confirmation (implemented and validated on iOS; Android pending)
- [x] Link to the privacy policy without transmitting financial values

### Accessibility

- [~] Follow device text scaling (native scaling preserved; large-text matrix pending)
- [~] Reduced-motion setting and system behavior (chart animation override validated on iOS; system detection and Android pending)
- [~] High-contrast setting (dark high-contrast palette validated on iOS; Android pending)
- [x] Chart data alternatives
- [~] Screen-reader-friendly number and percentage descriptions (semantic chart and action descriptions implemented; screen-reader matrix pending)

## Excel Export

Use `DocumentFormat.OpenXml` to generate `.xlsx` files in cache or temporary storage and present
them with MAUI `Share`. Do not retain exports indefinitely.

- [x] Match the web workbook's calculator title and generated timestamp
- [x] Include Inputs and Results sheets
- [x] Include projection sheets when applicable
- [x] Include calculator-specific collection sheets for debts, accounts, income, and expenses
- [x] Preserve useful formulas where the web export supplies them
- [x] Apply currency, percentage, integer, and decimal formats
- [x] Sanitize worksheet names and user-provided text
- [~] Open generated workbooks successfully in Apple Numbers and Microsoft Excel (Excel verified on macOS; Numbers is not installed on the validation host, and platform share-target checks remain)
- [x] Remove or age out stale temporary exports

## Delivery Phases

### Phase 0: Baseline and Technical Spikes

- [~] Record clean iOS simulator and Android emulator launches
- [x] Wire MauiDevFlow for Debug validation
- [x] Add test project to `MyFireNumber.slnx`
- [x] Add selected packages
- [~] Prove a LiveCharts2 chart renders on iOS and Android
- [~] Prove a SQLite write/read/migration cycle on iOS and Android
- [~] Prove a generated `.xlsx` opens from the native share flow
- [~] Prove trimmed Release builds retain required chart, SQLite, and Open XML behavior

Exit gate: all technical risks have small running proofs on both target platforms.

#### Phase 0 Evidence

| Date | Item | Evidence | Status |
| --- | --- | --- | --- |
| 2026-08-08 | Dependency foundation | iOS simulator build succeeds with CommunityToolkit.Mvvm, sqlite-net-pcl, LiveCharts2, and DocumentFormat.OpenXml. | Complete |
| 2026-08-08 | DevFlow extension | VS Code MAUI extension 1.17.156 failed with `MSB4099`; a documented temporary local scalar-property workaround allows the exact IDE build path to succeed. | Local workaround active |
| 2026-08-08 | iOS baseline | MyFireNumber launches on standard iPhone 17 at 402x874. DevFlow 0.1.0-preview.12.26368.2 connects on port 10223; visible labels and button have non-zero bounds. Tapping the button changes `Click me` to `Clicked 1 time`. | Complete |
| 2026-08-08 | iOS accessibility baseline | Button runtime colors are TextColor `#FFFFFF` on BackgroundColor `#512BD4`; screenshot confirms readable contrast and no overlap. | Complete |
| 2026-08-08 | Test harness | .NET 10 xUnit project is registered in `MyFireNumber.slnx`; one convention test is discovered and passes. | Complete |
| 2026-08-09 | iOS LiveCharts2 proof | Fresh iPhone 17 launch renders a visible `CartesianChart` at 316x340 with the expected line and area fill. The visual tree exposes its portfolio-projection accessibility label; the heading is TextColor `#17352C` on the light canvas. Android remains pending. | iOS complete |
| 2026-08-09 | iOS SQLite proof | Fresh iPhone 17 launch creates an app-private database, writes a v1 row, adds a nullable v2 column, updates it to schema version 2, and reads the expected values back. The visible status label has non-zero bounds and TextColor `#425D54`; Android remains pending. | iOS complete |
| 2026-08-09 | iOS Open XML and share proof | Fresh iPhone 17 launch creates `my-fire-number-proof.xlsx` in app cache. Tapping the visible share action opens the iOS native share sheet, which names the workbook and offers Copy and Save to Files. Runtime button colors are TextColor `#FFFFFF` on BackgroundColor `#512BD4`; Android remains pending. | iOS complete |
| 2026-08-09 | Debug build configuration | Debug builds define `MAUI_DEVFLOW` only when the IDE sets `MauiDevFlowEnabled=true`, matching extension package injection. Standalone iOS simulator and Android builds both succeed without a missing DevFlow assembly; 56 native tests pass. | Complete; IDE runtime inspection remains required |
| 2026-08-09 | DevFlow CLI recovery | The MAUI CLI broker, iPhone 17 simulator, and instrumented iOS build connect successfully. Settings renders at 402x874; selecting Dark makes the native Picker TextColor `#F2F7F3` and TitleColor `#B9D1C2`, and those values persist after a full process relaunch. | iOS complete |
| 2026-08-09 | Calculator preferences | On iPhone 17, Settings renders the Home calculator visibility, ordering, and reset controls. Hiding Standard FIRE changes its action to Show and removes it from Home after a full process relaunch; reset restores the default visible state. | iOS complete; Android pending |
| 2026-08-09 | Plans empty state | On iPhone 17, the Plans tab renders the visible `No saved plans yet.` state with its explanatory guidance when no scenarios exist. | iOS complete; Android pending |
| 2026-08-09 | Standard FIRE plan save | On iPhone 17, entering `iOS Validation Plan` in Standard FIRE and saving it creates a visible plan with that exact name in Plans. | iOS complete; Android pending |

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

- [x] Define typed calculator input and result models
- [x] Create golden web fixtures for all calculators
- [x] Port common financial math
- [x] Port Standard, Coast, Lean, Fat, Barista, and Withdrawal calculations
- [x] Port Reverse, Savings Rate, and Healthcare calculations currently located in pages
- [x] Port Debt Payoff calculations
- [x] Port Retirement Cash Flow calculations
- [x] Pass parity tests for normal and boundary cases

Exit gate: all calculation tests pass without referencing MAUI.

#### Phase 1 Evidence

| Date | Item | Evidence | Status |
| --- | --- | --- | --- |
| 2026-08-09 | Pure calculation core | `MyFireNumber.Core` targets `net10.0` without MAUI dependencies and is referenced by both the app and test project. | Complete |
| 2026-08-09 | Web parity fixtures | 15 xUnit tests cover shared FIRE, Coast, Lean, Fat, Barista, Withdrawal, Debt, Reverse, Savings, Healthcare, and Retirement Cash Flow normal and boundary behavior. | Complete |
| 2026-08-09 | iOS integration | The iOS simulator target compiles successfully against the complete pure calculation core. | Complete |

### Phase 2: App Foundation

- [x] Implement central catalog and Shell routes
- [x] Implement four-tab Shell with icons
- [~] Establish resources, themes, typography, and shared styles
- [x] Register pages, view models, repositories, and services in DI
- [x] Implement SQLite initialization and migrations
- [~] Implement preferences and theme services
- [x] Implement first-run routing
- [x] Add global error presentation that does not expose financial values

Exit gate: fresh install enters Quiz; skip reaches a functional four-tab shell.

#### Phase 2 Evidence

| Date | Item | Evidence | Status |
| --- | --- | --- | --- |
| 2026-08-09 | Catalog and Shell | A central typed catalog exposes 11 stable calculator IDs and routes; two focused catalog tests pass. iPhone 17 renders catalog search results and query-bound calculator detail content. | iOS complete |
| 2026-08-09 | Four-tab navigation | Fresh iPhone 17 launch renders Home, Calculators, Plans, and Settings tabs with generated PNG tab assets, visible labels, and non-zero bounds. | iOS complete |
| 2026-08-09 | Onboarding routing | A first-run launch opens the skippable FIRE Quiz. Skip reaches Home and the completion preference survives a subsequent fresh iPhone 17 launch. Settings can reset onboarding and re-open the Quiz. | iOS complete |
| 2026-08-09 | Search accessibility | The iOS SearchBar renders dark entered text (`#17352C`) and a muted placeholder (`#587068`) on the native white field. MAUI reports the outer SearchBar background as transparent, so screenshot evidence confirms the actual native field contrast. | iOS complete |
| 2026-08-09 | Local storage foundation | The platform-neutral SQLite storage project initializes versioned drafts, plans, and calculator preferences. Three focused repository tests validate draft upsert/round trip, plan ordering, and preference upsert; a fresh iPhone 17 launch includes the repository DI registrations without a startup failure. | iOS complete; future migrations pending |
| 2026-08-09 | Icon system | Font Awesome Free Solid 6.7.2 is bundled and registered for landmark glyphs and `FontImageSource` action icons. A fresh iPhone 17 launch renders the glyphs without the prior `IconGlyph` StaticResource XAML exception. | iOS complete |
| 2026-08-09 | Global error presentation | A singleton error presenter marshals generic messages to the UI thread without accepting exception or payload data. Plans load, rename, duplicate, and delete failures use it. The full 57-test suite passes, and a fresh iPhone 17 launch resolves Plans through DI and renders its heading at non-zero bounds. | iOS complete |
| 2026-08-09 | Schema v2 migration | `LocalDatabase` now applies explicit ordered migrations and updates schema metadata. A focused upgrade test creates a version-1 database, initializes the version-2 recent-activity table, persists a record, and verifies metadata advanced to version 2 without replacing existing tables. | Complete |
| 2026-08-09 | Linked iOS Release proof | A clean `net10.0-ios` `iossimulator-arm64` Release rebuild completed with linking/AOT enabled and simulator LLVM disabled, with no warnings or errors. The uninstrumented app launched on iPhone 17 and rendered persisted SQLite calculator and plan recents; the bundle retains LiveCharts2, Open XML, SQLite, SQLitePCLRaw, and SQLite AOT artifacts. Command-line Release DevFlow instrumentation exits before agent registration, while the shipping-style build runs normally, so runtime inspection remains a Debug-only validation path. | iOS simulator complete; device LLVM and Android pending |
| 2026-08-09 | Schema v3 payload recovery | A monotonic v3 migration adds app-private corrupt-payload recovery storage. Malformed drafts and plans are copied with their source metadata and original local JSON, then removed from active storage in the same SQLite transaction so calculators can recover without data loss or repeated launch failures. Three focused tests cover draft and plan quarantine plus v2-to-v3 upgrade; the prior v1 upgrade test now advances through v3. The existing iOS database reports schema `3`, contains the recovery table, and still renders persisted Home recents. | Complete |

### Phase 3: Standard FIRE Vertical Slice

- [x] Build the shared compiled input/result patterns needed by Standard FIRE
- [x] Implement Standard FIRE page and view model
- [x] Implement projection chart and accessible summary
- [x] Implement presets
- [x] Implement draft save/restore/reset
- [x] Implement named plan save/load
- [x] Implement Excel export and share
- [~] Validate light/dark, accessibility, lifecycle restoration, and offline behavior (iOS large text, contrast, lifecycle restoration, and zero-network cold start complete; VoiceOver, disconnected-radio, and Android matrices pending)

Exit gate: Standard FIRE is complete end to end on iOS and Android.

#### Phase 3 Evidence

| Date | Item | Evidence | Status |
| --- | --- | --- | --- |
| 2026-08-09 | Standard FIRE inputs and draft | A typed, versioned Standard FIRE draft matches web defaults in a focused unit test. A fresh iPhone 17 launch renders the input form and the default `$1,200,000` FIRE Number with its textual projection summary. The detail route restores and debounces writes to the local SQLite draft, validates user input, and calculates live metrics through the tested pure Core calculator. | iOS complete |
| 2026-08-09 | Standard FIRE accessibility | On iPhone 17, the native Current Savings Entry reports TextColor `#17352C`, PlaceholderColor `#587068`, and transparent MAUI outer BackgroundColor; the screenshot confirms the native light field has readable contrast. | iOS complete |
| 2026-08-09 | Standard FIRE named plans | Saving `Accelerated FIRE` with Current Savings `$250,000` lists the name, calculator, and modification time in Plans. Opening it restores the name and `$250,000` entry, with the distinct `21% of your FIRE Number is currently funded` result. Three focused SQLite plan repository tests, including the typed StandardFireDraft JSON round trip, pass. | iOS complete; rename, duplicate, delete, and update semantics pending |
| 2026-08-09 | Standard FIRE presets | Four typed Core presets provide complete StandardFireDraft values. On iPhone 17, selecting Aggressive applies age 45, `$150,000` savings, `$48,000` contribution, `$96,000` income, and the computed `50.0%` savings rate. The native Picker reports TextColor `#17352C`, TitleColor `#587068`, and transparent MAUI outer BackgroundColor; screenshot confirms the native light field is readable. | iOS complete |
| 2026-08-09 | Standard FIRE projection chart | A fresh iPhone 17 launch renders a 328x260 LiveCharts CartesianChart with portfolio, inflation-adjusted, and FIRE-target series. The visible chart shows its age axis and target line; its runtime accessibility label gives the age range, starting portfolio, and FIRE target, with the textual projection summary directly beneath it. | iOS complete |
| 2026-08-09 | Standard FIRE draft lifecycle | Editing Annual Contribution to `$36,000` recalculates the savings rate to `37.5%` and survives a fresh iPhone 17 debug relaunch. Reset then restores the default `$24,000` contribution, `$1,200,000` FIRE Number, and 24.4-year projection. | iOS complete |
| 2026-08-09 | Immediate draft flush | On iPhone 17, Annual Contribution was changed to `$43,210` and navigation left the calculator immediately, before the 400 ms debounce elapsed. After process termination and relaunch, Standard FIRE restored `$43,210`, proving the page/window lifecycle flush persisted the pending draft. | iOS complete |
| 2026-08-09 | Standard FIRE Excel export | A focused Open XML test opens the generated workbook and verifies Inputs, Results, and Projections sheets with formula-backed FIRE Number and projection cells. On iPhone 17, Export to Excel opens the native share sheet with `standard-fire-20260809-032846.xlsx`, including Copy and Save to Files actions. | iOS complete |
| 2026-08-09 | Standard FIRE dark mode | iPhone 17 system dark mode renders the form, Export action, and portfolio chart without overlap. Native Entry and Picker text is `#F2F7F3`, with placeholder/title `#B9D1C2`; screenshots confirm readable contrast on the dark native fields and chart canvas. | iOS complete; disconnected-radio and VoiceOver validation pending |

### Phase 4: Shared FIRE Calculators

- [~] Coast FIRE
- [~] Lean FIRE
- [~] Fat FIRE
- [~] Barista FIRE
- [~] Reverse FIRE
- [~] Withdrawal Rate
- [~] Savings and Investment Rate (implemented, tested, and validated on iOS; Android pending)
- [~] Healthcare Gap (implemented, tested, and validated on iOS; Android pending)

Run the complete per-calculator parity checklist for each item before marking it complete.

Exit gate: nine shared-form calculators are complete on both platforms.

#### Phase 4 Evidence

| Date | Item | Evidence | Status |
| --- | --- | --- | --- |
| 2026-08-09 | Coast FIRE initial slice | Typed Coast FIRE defaults map to the shared Core input contract in a focused unit test. A fresh iPhone 17 launch renders the default `$462,932` Coast FIRE Number and `10.7 years to Coast FIRE`; changing savings to `$500,000` switches to the already-Coast status and survives a fresh relaunch. In dark mode, the visible native savings Entry reports TextColor `#F2F7F3` and PlaceholderColor `#B9D1C2` on the dark native field. | iOS initial inputs, results, drafts, chart, plans, and export complete; Android pending |
| 2026-08-09 | Coast FIRE comparison chart | A fresh iPhone 17 launch renders a 328x260 LiveCharts comparison with Coast, continued-contribution, and full-FIRE-target lines. The light-mode screenshot confirms the chart and age axis render without overlap; its runtime accessibility label explains the two paths and target, and the textual summary reports both age-65 outcomes. | iOS complete; Android pending |
| 2026-08-09 | Coast FIRE named plans | A Coast scenario with `$500,000` savings saves as `Coast Complete`, appears in Plans with the `Coast FIRE` calculator title, and opens through its routed detail page with the saved name, savings, and already-Coast result intact. Focused SQLite tests cover the typed Coast draft JSON round trip. | iOS complete; plan update, rename, duplicate, delete, and Android validation pending |
| 2026-08-09 | Coast FIRE Excel export | A focused Open XML test verifies Inputs, Results, Coast Projection, and Contributions Projection sheets, including the full-FIRE formula and both projection formulas. On iPhone 17, Export to Excel opens the native share sheet with `coast-fire-20260809-035225.xlsx`, including Copy and Save to Files actions. | iOS complete; Android validation pending |
| 2026-08-09 | Lean FIRE vertical slice | A typed Lean draft preserves entered expenses while capping the calculation to the web’s `$40,000` Lean guideline. On iPhone 17, the default `$48,000` expense input displays the threshold warning and computes a `$1,000,000` Lean FIRE Number in `21.3 years`; the form, textual progress, and LiveCharts projection use Lean-specific labels. The visible native savings Entry reports TextColor `#17352C` and PlaceholderColor `#587068` on the readable light native field. | iOS inputs, results, chart, and drafts complete; Android pending |
| 2026-08-09 | Lean FIRE named plans and export | `Lean Complete` saves to Plans with the `Lean FIRE` calculator title and restores its name and `$48,000` input through the detail route. Focused SQLite tests cover the typed Lean draft JSON round trip. A focused Open XML test verifies the Lean workbook title and `$40,000` capped calculation input; on iPhone 17, the Lean export action exposes the correct accessibility description and opens the native share sheet with `lean-fire-20260809-054654.xlsx`, including Copy and Save to Files actions. | iOS complete; plan update, rename, duplicate, delete, and Android validation pending |
| 2026-08-09 | Fat FIRE vertical slice | A typed Fat draft preserves entered expenses without applying a cap. On iPhone 17, the `$48,000` default renders its below-threshold guidance and a `$1,200,000` target; changing expenses to `$125,000` switches to the Fat FIRE status and recomputes the target to `$3,125,000` in 43.3 years. The visible native expense Entry reports TextColor `#17352C` and PlaceholderColor `#587068` on the readable light native field. | iOS inputs, results, chart, and drafts complete; Android pending |
| 2026-08-09 | Fat FIRE named plans and export | The `$125,000` scenario saves as `Fat Complete` with the correct Fat plan accessibility name. Focused SQLite tests cover typed Fat draft JSON round trips. A focused Open XML test verifies the Fat workbook title and `$125,000` input; on iPhone 17, the Fat export exposes a Fat-specific accessibility description and opens the native share sheet with `fat-fire-20260809-055122.xlsx`, including Copy and Save to Files actions. | iOS complete; plan update, rename, duplicate, delete, and Android validation pending |

### Phase 5: Complex Calculators

- [x] Debt Payoff editable debt workflow
- [x] Snowball and Avalanche comparison
- [~] Debt balance and breakdown charts (implemented and validated on iOS; Android pending)
- [x] Retirement Cash Flow scenario inputs
- [x] Income, account, and additional-expense editors
- [~] Retirement cash-flow and bucket charts (implemented and validated on iOS; Android pending)
- [~] Expandable annual cash-flow detail (implemented and validated on iOS; Android pending)
- [x] Complex calculator exports and persistence

Exit gate: collection editing, calculations, charts, plans, and exports pass on both platforms.

#### 2026-08-09 Checkpoint

- Debt Payoff now has typed drafts, editable debts, fixed-budget and target-timeline modes, Snowball/Avalanche comparison, a balance projection, named-plan saves, and a local Open XML workbook/share service. Focused `DebtPayoffWorkbookTests` pass.
- Retirement Cash Flow started this checkpoint with a typed web-parity scenario, validated Core inputs, a cash-flow projection, typed draft saves, and a local Open XML workbook/share service. The later collection-editor checkpoint below completes its account, income, expense, plan-load, and collection-export gaps; bucket visualization and annual detail remain.
- Focused `DeferredCompensationDraftTests` pass. The iOS target builds successfully with `MauiDevFlowEnabled=true`.
- DevFlow recovery is in progress. On the iPhone 17 simulator, `dotnet_maui_debugProject` launches successfully but `maui_wait(app: "MyFireNumber", timeout: 30)` times out. Diagnostics report a healthy broker at port `19223`, `agentCount=0`, no version mismatch, and no runtime app output. The manual `Microsoft.Maui.DevFlow.Agent` reference was removed so the VS Code extension can inject its compatible debug agent on the next launch. No visual-tree, screenshot, or runtime color validation is yet available for Phase 5.

#### 2026-08-09 Retirement Collection Editor Checkpoint

- Retirement Cash Flow now supports editable account, income-source, and additional-expense collections. Each collection supports add, field editing, and remove; account type, availability, returns, withdrawals, payout years, income tax/growth/age ranges, and expense start ages flow into the Core calculation contract.
- Typed local drafts and named plans now restore all three Retirement Cash Flow collections. On iPhone 17, a newly added account, income source, and expense survived immediate navigation, process termination, relaunch, and route restoration.
- The Open XML workbook now includes Accounts, Income Sources, and Additional Expenses sheets. Five focused Retirement Cash Flow tests cover web-parity defaults, custom JSON round trips, workbook collection output, and calculation fixtures.
- iPhone 17 renders the collection editors with non-zero bounds. The visible dark native Entry reports TextColor `#F2F7F3`, PlaceholderColor `#B9D1C2`, and transparent MAUI outer background; the account Picker reports the same text/title colors, and screenshot evidence confirms readable native fields.

#### 2026-08-09 Complex Chart Checkpoint

- Debt Payoff now includes a cumulative principal-and-interest stacked-area chart matching the web breakdown semantics. On iPhone 17, the chart renders at 328x260 and its visible text alternative reports `$1,000` principal and `$33` interest for the exercised scenario.
- Retirement Cash Flow now includes one balance series per account across the full age projection, matching the web account-bucket chart. On iPhone 17, the chart renders at 328x260 and its visible text alternative lists every ending account balance.
- The dark native Debt budget Entry and Retirement plan-name Entry report TextColor `#F2F7F3`, PlaceholderColor `#B9D1C2`, and transparent MAUI outer backgrounds; screenshots confirm readable native fields and chart canvases. All 61 tests pass.

#### 2026-08-09 Retirement Annual Detail Checkpoint

- Retirement Cash Flow now opens a dedicated Shell detail route with a virtualized `CollectionView` for the full projection horizon. Each year is a tappable 44-point-or-larger target that expands in place to show total income, expenses, annual surplus or shortfall, named income and withdrawal sources, expense items, and every account balance.
- On iPhone 17, the list renders at 362x602. Expanding age 45 grows its row from 92 to 406 points and reveals the expected `$0` income, `$80,000` expenses, `-$80,000` shortfall, and `$800,000` combined account balance without clipping or nested scrolling.
- The pushed page restores the native navigation bar, uses the contextual `Annual Cash Flow` title, hides the root tab bar, and returns to the calculator through the standard Shell back action. All 61 tests pass.

### Phase 6: Quiz, Home, Catalog, and Plans

- [x] Port quiz questions and recommendation rules
- [x] Implement Skip, completion, retake, and draft-overwrite confirmation
- [x] Build Home with enabled calculators in selected order
- [~] Build searchable All Calculators catalog
- [~] Build Plans list and plan management workflows
- [~] Verify hidden calculators remain discoverable (validated on iOS; Android pending)

Exit gate: all top-level navigation and discovery workflows are complete.

#### Phase 6 Evidence

| Date | Item | Evidence | Status |
| --- | --- | --- | --- |
| 2026-08-09 | Calculator catalog interaction and theming | Calculator cards are full-width tap targets with distinct Font Awesome icons and no redundant Open buttons. On iPhone 17, tapping the Standard FIRE card opens its detail route. The dark SearchBar reports TextColor `#F2F7F3` and PlaceholderColor `#B9D1C2`; screenshot evidence confirms readable native field contrast. | iOS complete; hidden-calculator discovery and Android pending |
| 2026-08-09 | Plans interaction and discovery | Saved-plan cards are full-width tap targets with no redundant Open buttons. Tapping `iOS Validation Plan` opens the saved Standard FIRE scenario. Rename, Duplicate, and Delete are exposed as native right-side swipe actions, and search plus calculator filtering now share one clearly grouped surface. | iOS tap and layout complete; swipe gesture automation and Android pending |
| 2026-08-09 | Contextual navigation chrome | Root Home, Calculators, Plans, and Settings tabs hide the redundant top navigation bar while preserving the bottom tab bar. Pushed calculator routes restore the native bar with a back action, the bound calculator name, and Save/Export toolbar icons. iPhone 17 screenshots verify both states and the Standard FIRE title at non-zero bounds. | iOS complete; plan-specific toolbar actions and Android pending |
| 2026-08-09 | Home recent activity | A bounded SQLite history stores only calculator IDs, plan IDs, and local timestamps. Home resolves and shows up to three recently used calculators and three recently opened plans without changing plan modification dates. Two focused repository/migration tests pass; iPhone 17 shows both sections at non-zero bounds and retains them after process relaunch. | iOS complete; Android pending |
| 2026-08-09 | Explicit Save As | Calculator details initially exposed Save, Save As, and Export as distinct native toolbar actions. The native prompt received its suggested name as an editable initial value instead of placeholder text, fixing the same issue for Rename and Duplicate. Product review later removed Save As in favor of context-aware Save/Update plus Duplicate in Plans. | Superseded by simplified plan actions |
| 2026-08-09 | Native FIRE Quiz | The eight-question native quiz matches the web recommendation priority and creates calculator-specific typed drafts from local answers. Seven focused rule tests cover every recommendation path and the full suite passes with 71 tests. On iPhone 17, Lean FIRE renders at non-zero bounds, the existing-draft path shows a native Replace/Cancel confirmation, and a clean Reverse FIRE path opens with quiz-prefilled ages 40/50, `$200,000` savings, and `$60,000` expenses. The dark native Entry reports TextColor `#F2F7F3`, PlaceholderColor `#B9D1C2`, and a transparent MAUI outer background; screenshot evidence confirms readable contrast. | iOS complete; Android pending |
| 2026-08-09 | Streamlined plans and calculator chrome | Plans search and calculator filtering now scroll in a compact `CollectionView` header; the list renders at 362x697 on iPhone 17. Calculator pages begin directly with calculator content beneath the contextual native title, without a duplicate 30-point heading. Fresh drafts expose Save to Plans and loaded plans expose Update Plan in both toolbar and page actions; Save As is removed because Plans already provides Duplicate. The dark Plans SearchBar reports TextColor `#F2F7F3` and PlaceholderColor `#B9D1C2`; the exercised calculator Entry reports the same readable colors. All 71 tests pass. | iOS complete; Android pending |
| 2026-08-09 | Grouped calculator catalog | The All Calculators `CollectionView` now presents FIRE, Finance, and Cash Flow groups with distinct accessible headers while retaining full-card navigation and search. On iPhone 17, all three headers render at non-zero bounds; searching `debt` collapses the list to the Finance header and Debt Payoff card. The dark native SearchBar reports TextColor `#F2F7F3`, PlaceholderColor `#B9D1C2`, and a transparent MAUI outer background. All 71 tests pass. | iOS complete; Android pending |
| 2026-08-09 | Activity-aware Home guidance | Home now persists only the non-sensitive recommended calculator ID from the quiz. Before calculator or plan activity, it shows the quiz recommendation with Open, Retake Quiz, and Browse All actions; Retake opens the native quiz and its numeric Entry at non-zero bounds. Once recent calculator activity or any saved plan exists, the starter card is removed and the bounded recent sections remain. iPhone 17 screenshots verify both states; all 71 tests pass. | iOS complete; Android pending |
| 2026-08-09 | Retirement Cash Flow row accordions | Main Scenario, Accounts, Income sources, Additional expenses, and result sections remain visible so the cash-flow flow is scannable. Individual account, income, and expense rows are compact 44-point accordion headers with aligned title, chevron, and remove target; existing rows start collapsed while newly added rows open directly for editing. On iPhone 17, an existing Deferred Compensation row expands from 70 to 389 points, and a newly added account exposes its editable name field. Dark native Entries report TextColor `#F2F7F3`, PlaceholderColor `#B9D1C2`, and a transparent MAUI outer background. All 71 tests pass. | iOS complete; Android pending |

### Phase 7: Full Settings

- [~] Calculator visibility and ordering
- [~] Appearance and theme
- [~] Defaults and assumptions (implemented and validated on iOS; Android pending)
- [~] App behavior (implemented and validated on iOS; Android pending)
- [~] Privacy and data management
  - [x] Confirmed reset deletes drafts, plans, calculator preferences, recent activity, corrupt-payload quarantine, and app preferences before restarting the first-run quiz.
  - [~] Import/export all local app data with corrupt-input handling (implemented, storage-tested, and validated on iOS; Android pending).
- [~] Accessibility preferences (implemented and validated on iOS; Android and assistive-technology matrix pending)

Exit gate: settings persist across relaunch and do not corrupt drafts or plans.

#### Phase 7 Evidence

| Date | Item | Evidence | Status |
| --- | --- | --- | --- |
| 2026-08-09 | Settings interaction redesign | Appearance is the first Settings section and uses a compact, accessible monitor/sun/moon segmented selector. Home calculator rows use 44x44 icon actions for move earlier, move later, show, and hide, with calculator-specific semantic descriptions. iPhone 17 screenshots verify readable light and dark states and visible selected segments. | iOS interaction complete; remaining Settings groups and Android pending |
| 2026-08-09 | Privacy legal links | Privacy is the final Settings card with clear Terms and Privacy policy actions. On iPhone 17, both controls render as 153x44-point targets, and the dark-state screenshot confirms readable presentation. The links use the canonical `https://myfirenumber.com/legal` route, whose local implementation documents educational-use terms and local-only financial-data handling for the website and My Fire Number. The instrumented iOS build, 71 native tests, TypeScript production build, and design detector pass. | iOS visual verification complete; production `/legal` deployment, successful launcher handoff, and Android pending |
| 2026-08-09 | Branded icon and splash | Replaced the MAUI template's purple/.NET assets with a custom evergreen momentum-flame mark: a bright flame surrounds a rising financial trajectory, framed by subdued growth rings. On iPhone 17, the crisp home-screen icon pairs with the untruncated `My Fire #` label; the launch screen centers the same mark with generous negative space on a deep evergreen field. | iOS complete; Android visual verification pending |
| 2026-08-09 | Confirmed app reset | The final Privacy-first Settings card offers a deliberate `Delete all app data` action. On iPhone 17, it opens a destructive confirmation explaining that drafts, plans, calculator settings, and quiz progress will be deleted before the first-run FIRE Quiz resumes. The storage-layer reset atomically clears every local data table; a focused test and the full 72-test suite pass. | iOS confirmation visual and storage behavior complete; direct confirmation-button automation and Android pending |
| 2026-08-09 | Local backup and restore | Settings now creates a versioned JSON archive containing every draft, plan, calculator preference, recent activity entry, quarantined payload, theme, onboarding state, calculator default, behavior preference, accessibility preference, and currency override. Export opens the iOS native share sheet without automatic upload. Import requires replacement confirmation, rejects unsupported or invalid archives before mutation, and restores data transactionally. Two focused archive tests and all 74 native tests pass; both 153x44-point controls render with readable dark-state contrast on iPhone 17. | iOS complete; Android file-picker/share validation pending |
| 2026-08-09 | Calculator defaults | Settings now persists expected return, inflation, withdrawal rate, current age, and retirement age as decimal-backed assumptions for newly created calculators. Existing drafts and named plans remain untouched. On iPhone 17, all five numeric fields render in a compact two-column card, validation explains invalid ranges, save confirmation appears in-page, and the native expected-return Entry reports TextColor `#F2F7F3` and PlaceholderColor `#8EAA9B` over the visually verified `#15211C` field surface. | iOS complete; Android pending |
| 2026-08-09 | Behavior and accessibility preferences | Settings now persists a post-onboarding launch destination, latest-draft restoration, plan-delete confirmation, haptics, reduced chart motion, and a high-contrast dark palette. Relaunching iPhone 17 after selecting Calculators opens that tab; enabling reduced motion gives the live Standard FIRE chart `AnimationsSpeed=00:00:00`; disabling it restores motion. The behavior card renders all controls with non-zero bounds, and its selected action reports white text while the page's native numeric Entry remains `#F2F7F3` with `#8EAA9B` placeholder text on the verified dark field surface. | iOS simulator complete; physical haptics and Android pending |
| 2026-08-09 | Currency preference | Calculator results use the device region by default and can explicitly display USD, CAD, EUR, GBP, AUD, or JPY while retaining the device's number separators. On iPhone 17, selecting EUR changes the live Standard FIRE result to `€1,200,000`; restoring Device region returns the default. The native 316x44-point Picker reports TextColor `#F2F7F3` and TitleColor `#8EAA9B` over the visually verified dark field surface. | iOS complete; Android and expanded locale matrix pending |
| 2026-08-09 | Hidden calculator discovery | Standard FIRE remained visible in the grouped All Calculators catalog while its Settings action showed that it was hidden from Home. The catalog card retained its full open action, confirming Home personalization never removes calculator access. Standard FIRE was restored to Home after validation. | iOS complete; Android pending |
| 2026-08-09 | Accessible plan management | Plan cards retain left-swipe shortcuts and now also show explicit 104x44-point Rename, Duplicate, and Delete controls. On iPhone 17, each action opens the correct native prompt with the current or generated copy name; Delete explains that the automatic calculator draft remains. SearchBar and Picker runtime colors are `#F2F7F3` text with `#B9D1C2` placeholder/title text, and Rename reports `#C8E7D5`. | iOS complete; destructive acceptance and Android pending |

### Phase 8: Release Hardening

- [!] Validate Android minimum supported API behavior (API 21 runtime/device unavailable; Android DevFlow agent does not register)
- [!] Validate iOS minimum supported version behavior (only iOS 26.5 runtime is installed)
- [~] Validate phone, tablet, portrait, and landscape layouts (iPhone and iPad portrait complete; landscape and Android pending)
- [!] Validate VoiceOver and TalkBack workflows (requires interactive assistive-technology passes on both platforms)
- [~] Validate large text and display scaling (iPad accessibility-extra-extra-large complete; Android pending)
- [~] Validate light, dark, high contrast, and reduced motion (iOS complete; Android pending)
- [~] Validate offline cold start and all local workflows (zero-network iOS cold start and source audit complete; physically disconnected-radio matrix pending)
- [~] Validate background/foreground and process termination restoration (iOS draft flush/relaunch complete; Android pending)
- [x] Validate database upgrade from every released schema
- [~] Validate import/export and corrupted input handling (storage tests and iOS prompts/share complete; Android pending)
- [~] Validate Release trimming and AOT
- [!] Run Android and iOS GitHub Actions workflows (the local branch has no upstream and cannot be dispatched until explicitly pushed)
- [~] Complete privacy disclosures, app metadata, icons, splash screen, and screenshots (metadata/disclosures/assets complete; final store screenshot matrix pending)

Exit gate: release candidate passes the Definition of Done below.

#### Phase 8 Evidence

| Date | Item | Evidence | Status |
| --- | --- | --- | --- |
| 2026-08-09 | iOS linked/AOT simulator launch | A clean linked/AOT Release rebuild completed without warnings. The shipping-style app launched on iPhone 17 and rendered the Home dashboard with persisted SQLite recents in dark mode. Simulator LLVM was disabled after Open XML LLVM optimization remained CPU-bound for more than 36 minutes; final device/store LLVM validation remains required. | iOS simulator complete; device LLVM and Android pending |
| 2026-08-09 | Current release builds | The final 79-test Release suite passes. The current iOS `iossimulator-arm64` Release build succeeds with linking/AOT and simulator LLVM disabled. Android API 35 Debug builds, installs, launches, and keeps a live process; the trimmed `android-arm64` Release build succeeds with Android AOT disabled because the .NET 10 toolchain fails precompilation across framework and app assemblies. | Build validation complete; Android UI automation and Android AOT pending |
| 2026-08-09 | iPad portrait layout | On an iPad Pro 11-inch simulator, the quiz and Home render at 834x1210 points without clipping. Home keeps all 11 calculators, quiz/retake/browse actions, and the four-tab bar reachable. The 748x44-point native quiz Entry reports TextColor `#17352C` and PlaceholderColor `#587068` over the visually verified light field. | iPad portrait complete; landscape pending |
| 2026-08-09 | Toolchain and CI hardening | MAUI Doctor reports all 13 checks healthy with the active .NET 10 SDK, MAUI workload, Microsoft OpenJDK, Android SDK, and Xcode. Both mobile workflows now restore workloads from the project, run the 79-test Release suite, trigger for app/Core/storage/test changes, and use the latest stable Xcode selector. | Local validation complete; hosted workflow dispatch requires a pushed branch |
| 2026-08-09 | Store metadata and disclosures | `docs/MOBILE_STORE_LISTING.md` records store identity, descriptions, legal/support URLs, review notes, no-collection disclosures, OS-backup behavior, and the required screenshot set. The app repeats the OS-backup disclosure in Settings; the branded icon/splash and educational disclaimers are present. | Content complete; final platform screenshot capture and store-console entry pending |
| 2026-08-09 | Temporary export retention | App startup removes generated `.xlsx` reports and versioned JSON backups from the OS cache after 24 hours. Cleanup handles unavailable or locked cache files without exposing filenames or financial values. The final iOS app launches normally after cleanup registration, and iOS/Android Debug builds plus all 79 Release tests pass. | Complete |
| 2026-08-09 | Boundary parity fixtures | Added funded-target, fully covered Barista income, invalid debt timeline, and zero-year healthcare-gap fixtures alongside the existing normal, zero-return, threshold, and unreachable-target cases. The Release suite now passes 79 tests. | Complete |
| 2026-08-09 | SQLite pragma evaluation | The app uses one shared sqlite-net async connection, serializes access, and wraps destructive replacement in transactions. WAL would not add useful reader/writer concurrency but would add sidecar-file recovery and sensitive-data-retention complexity, so the shared iOS/Android storage layer intentionally retains the platform SQLite defaults. iOS runtime inspection confirmed `journal_mode=delete`, `synchronous=FULL`, no foreign-key graph, and no busy timeout; the same repository architecture is compiled for Android. | Complete; re-evaluate if multiple connections or background writers are introduced |
| 2026-08-09 | iOS large text and cold start | On iPad, the quiz relaunches at the accessibility-extra-extra-large content size with the heading, question, helper text, 748x58-point native Entry, and both navigation actions visible without clipping. The Entry reports TextColor `#17352C` and PlaceholderColor `#587068` over the visually verified light field. A separate full process termination/relaunch generated zero captured HTTP requests; source audit found no runtime client/backend dependency, only explicit user-initiated legal links. | iOS large-text and zero-network cold start complete; VoiceOver and physically disconnected-radio checks pending |
| 2026-08-09 | Workbook consumer validation | A fresh Standard FIRE workbook was generated through the native export action. Microsoft Excel opened and retained a live file handle to the `.xlsx`; focused Open XML tests already verify its sheets, formulas, values, and formats. Apple Numbers is not installed on this host. | Excel complete on macOS; Numbers and mobile share-target checks pending |
| 2026-08-09 | Defaults onboarding and tab refinements | First launch now presents a skippable defaults step before the FIRE Quiz with five local-only sliders; saving persists percentages as decimals and continues to the quiz. Settings keeps percentage entries and sliders synchronized, shows where the app will open, and the selected destination survives process relaunch. Calculators now owns its icon, title, supporting copy, and search inside the scrolling CollectionView header. On iPad light mode, theme icons render at 6.25:1 selected and 6.34:1 unselected contrast; Settings native Entry and Picker secondary text was strengthened to `#587068` for 4.71:1 contrast; saved-plan search reports the same readable `#587068` placeholder on `#F5F7F2`. | iOS complete; Android pending |

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
11. Stop the running app before source edits, then rebuild and relaunch for runtime validation.

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

1. Stop the active debug session before editing source files.
2. Make and narrowly validate the source change while the app is stopped.
3. Launch with the VS Code .NET MAUI debug command on the selected device.
4. Wait for MauiDevFlow to connect.
5. Inspect the visual tree and verify non-zero bounds, visibility, text, and hierarchy.
6. Use runtime property inspection for actual native input colors.
7. Capture a screenshot for visual layout confirmation.
8. Exercise the interaction with DevFlow actions: fill, tap, scroll, navigate, and back.
9. Background and resume when the slice involves persisted or draft state.
10. Repeat on the other target platform before closing a cross-platform item.

Do not rely on Hot Reload for validation in this repository. Every runtime validation must use a
fresh build and launch so startup registration, resources, XAML, and C# changes are all exercised.

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

- [x] All 11 calculators pass documented web parity fixtures
- [~] First launch presents the skippable FIRE Quiz (iOS validated; Android pending)
- [~] Quiz recommendation prefills the correct calculator draft (iOS validated; Android pending)
- [~] Home, Calculators, Plans, and Settings navigation works on iOS and Android (iOS complete; Android runtime pending)
- [~] Calculator visibility and ordering persist correctly (iOS validated; Android pending)
- [~] Drafts survive navigation, backgrounding, and process termination (iOS validated; Android pending)
- [~] Named plans support save, rename, duplicate, load, and delete (iOS validated; Android pending)
- [x] Database and payload migrations have upgrade tests
- [~] Every chart renders with LiveCharts2 and has an accessible text equivalent (iOS validated; Android pending)
- [!] Excel reports open successfully from both platforms (Excel verified on macOS; Apple Numbers is not installed and Android share-target validation is blocked)
- [~] Theme, locale, defaults, privacy, behavior, and accessibility settings persist (iOS validated; Android pending)
- [~] Delete-all and data import/export are validated (storage and iOS validated; Android pending)
- [x] No financial data is transmitted or written to logs
- [~] All critical workflows function offline (no runtime backend and zero-network iOS cold start verified; disconnected-radio/platform matrix pending)
- [~] VoiceOver, TalkBack, large text, contrast, and reduced motion are validated (large text, contrast, and reduced motion verified on iOS; screen readers and Android pending)
- [~] Debug and trimmed Release validation pass for iOS and Android (local builds pass; Android AOT and release runtime launch pending)
- [!] Android and iOS CI workflows pass (requires explicit push of the local branch before dispatch)
- [x] App identifier is `com.refractored.myfirenumber` in project and release configuration
- [~] Store assets, privacy disclosures, and educational disclaimers are complete (final screenshot matrix pending)

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
5. For MAUI UI or behavior, stop the app before edits, then rebuild, launch, and inspect it with MauiDevFlow.
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
| 2026-08-09 | Blocker | DevFlow | The tool surface did not expose MAUI DevFlow launch or inspection commands. | Resolved by using the installed MAUI CLI to restart the broker, deploy to iPhone 17, and connect the DevFlow agent on port 10223. |
| 2026-08-09 | Blocker | Privacy legal links | iPhone 17 successfully opens the in-app browser to `myfirenumber.com`, but the production website returns 404 for `/legal` because the new web route exists only on this local branch. | Deploy the web legal-route commit before treating the Terms/Privacy handoff as complete. |
