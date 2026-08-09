---
applyTo: "app/**,MyFireNumber.slnx,.github/workflows/maui-*.yml"
---

# .NET MAUI App Instructions

## Project and Commands

The mobile companion app is a .NET MAUI single-project application in `app/MyFireNumber/`, referenced by `MyFireNumber.slnx`.

Use the solution or project directly:

```bash
dotnet restore MyFireNumber.slnx
dotnet build app/MyFireNumber/MyFireNumber.csproj
```

Prefer a targeted framework when validating locally if the full multi-target build requires unavailable platform tooling. Do not hardcode SDK, workload, JDK, Android SDK, Xcode, or Windows SDK versions into repository guidance.

## MauiDevFlow from Copilot CLI

This repository supports MauiDevFlow through the `maui` global CLI. Copilot CLI agents must use
`maui devflow` directly when IDE/MCP wrapper tools such as `dotnet_maui_debugProject`,
`maui_tree`, or `maui_screenshot` are not exposed in the current session. Do not report that
DevFlow is unavailable until checking `command -v maui`, `dotnet tool list --global`, and
`maui devflow --help`.

The app already opts into DevFlow when `MauiDevFlowEnabled=true`:

- `MyFireNumber.csproj` conditionally references `Microsoft.Maui.DevFlow.Agent` and defines
  `MAUI_DEVFLOW`.
- `MauiProgram.cs` conditionally calls `AddMauiDevFlowAgent()`.
- `.mauidevflow` supplies the broker port, which the CLI discovers automatically.

Use this CLI flow:

```bash
# Start and inspect the broker.
maui devflow broker start
maui devflow broker status

# Discover and start the requested simulator. Use the returned UDID in later commands.
maui apple simulator list
maui apple simulator start "<simulator-name-or-udid>"

# Build and launch an instrumented Debug app. Run this as a background process because
# the Run target remains attached to the app.
dotnet build app/MyFireNumber/MyFireNumber.csproj \
  -t:Run \
  -f net10.0-ios \
  -p:MauiDevFlowEnabled=true \
  -p:_DeviceName=:v2:udid="<simulator-udid>"

# Wait on the actual agent connection instead of sleeping, then verify the running app.
maui devflow wait \
  --project app/MyFireNumber/MyFireNumber.csproj \
  --wait-platform iOS \
  --timeout 120
maui devflow list
maui devflow ui status
maui devflow ui tree --depth 4 --format compact
```

Drive the app with stable `AutomationId` or text selectors rather than ephemeral tree IDs whenever
possible:

```bash
maui devflow ui tap --automationId SaveOnboardingDefaultsButton
maui devflow ui navigate //calculators
maui devflow ui fill --automationId CalculatorSearch "FIRE"
maui devflow ui property "<element-id>" TextColor
maui devflow ui screenshot --output metadata/screenshot.png --scale native --overwrite
```

For multiple running apps, use `maui devflow list` and pass the selected agent's port with
`--agent-port` to subsequent commands. Repeat the build, launch, wait, interaction, and screenshot
sequence for each simulator; never assume one agent or simulator state applies to another.

If the Run target cannot remain attached, build with `MauiDevFlowEnabled=true`, then use
`maui apple simulator install <udid> <app-bundle-path>` and
`maui apple simulator launch <udid> com.refractored.myfirenumber`. Use
`maui devflow diagnose` for connection failures before changing project integration. The fallback
for a full-device capture is `maui apple simulator screenshot <udid> <output-path>`, but prefer
`maui devflow ui screenshot` for app UI evidence.

After every MAUI UI change, launch the app and verify it with `maui devflow ui tree`. Query actual
runtime foreground, background, and placeholder colors with `maui devflow ui property`, especially
for native input controls, and capture a screenshot when visual confirmation is needed. A successful
build alone is not sufficient UI validation.

## Architecture

- Keep platform-neutral UI, models, view models, services, and calculations in the shared project.
- Put platform-specific implementations under `Platforms/` and isolate them behind interfaces, partial classes, or conditional compilation.
- Register pages, view models, and services in `MauiProgram.cs`.
- Prefer constructor injection over service location.
- Use Shell routes for app navigation and keep route registration centralized.
- Keep financial calculations pure and independent of MAUI controls so formulas can be tested and compared with the web implementation.

## XAML and UI

- Prefer XAML for page layout and reusable controls.
- Use `Grid` for structured layouts and place `ScrollView` inside a `Grid`.
- Prefer `Border` over the legacy `Frame` control.
- Use `CollectionView` for scrollable lists and `BindableLayout` only for small collections.
- Add `x:DataType` to pages and every `DataTemplate` so bindings are compiled.
- Use resources and styles from `Resources/Styles/` rather than duplicating colors or control styling.
- Put images in `Resources/Images/`, fonts in `Resources/Fonts/`, and raw bundled files in `Resources/Raw/`.
- Reference raster UI images as PNG assets.

## MVVM and Binding

- Prefer MVVM with observable properties and commands as the app grows; avoid business logic in page code-behind.
- Use one-time binding for immutable values, default one-way binding for display values, and two-way binding only for editable controls.
- Expose explicit busy, empty, error, and validation states.
- Prevent duplicate async command execution and restore busy state in `finally`.
- Do not silently swallow service or calculation failures; surface errors using the app's established UI and logging patterns.

## Lifecycle and Performance

- Defer nonessential startup work until the relevant page appears.
- Avoid blocking the UI thread; use async APIs for I/O.
- Unsubscribe event handlers when their owner leaves the visual tree or is disposed.
- Keep list item templates lightweight and avoid deeply nested layouts.
- Preserve Release AOT and trimming compatibility; do not add reflection-heavy patterns without verifying them.

## Cross-Platform Requirements

- Verify behavior on each affected target: Android, iOS, Mac Catalyst, and Windows where applicable.
- Do not assume filesystem paths, permissions, device capabilities, or lifecycle behavior are identical across platforms.
- Request only permissions required for an explicit user-facing feature.
- Keep app identifiers, version numbers, platform manifests, entitlements, and signing settings coordinated when packaging behavior changes.

## Privacy and Storage

- Keep financial data on-device and do not add telemetry or network transmission.
- Use Preferences only for non-sensitive settings and SecureStorage for secrets or tokens.
- Do not place sensitive financial data in logs.
- Never commit keystores, provisioning profiles, certificates, or platform signing credentials.

## Generated Files

- Do not edit or commit `bin/`, `obj/`, generated resource output, IDE user settings, or package artifacts.
- Treat `.csproj`, XAML, source files, manifests, entitlements, resources, and the solution file as source-controlled inputs.
