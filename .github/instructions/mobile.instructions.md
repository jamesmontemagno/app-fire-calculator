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
