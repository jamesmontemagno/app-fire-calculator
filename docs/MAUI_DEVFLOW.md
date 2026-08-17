# MAUI DevFlow

This repository supports [MAUI DevFlow](https://learn.microsoft.com/dotnet/maui/developer-tools/devflow/)
for inspecting and automating the companion app during Debug development. DevFlow runs an in-app
agent, coordinated by a local broker; it never transmits financial data to a service.

> [!WARNING]
> DevFlow is experimental. Enable it only for Debug builds. It is intentionally opt-in and is not
> part of Release builds.

## Prerequisites

Install the DevFlow CLI and start its broker:

```bash
dotnet tool install -g Microsoft.Maui.Cli --prerelease
maui devflow broker start
maui devflow broker status
```

The project pins the matching `Microsoft.Maui.DevFlow.Agent` preview package. Its
[`.mauidevflow`](../app/MyFireNumber/.mauidevflow) file configures the broker port used for
automatic agent discovery.

## Build with DevFlow

Pass `MauiDevFlowEnabled=true` when building a Debug target:

```bash
dotnet restore app/MyFireNumber/MyFireNumber.csproj -p:MauiDevFlowEnabled=true

dotnet build app/MyFireNumber/MyFireNumber.csproj \
  -f net10.0-maccatalyst \
  -c Debug \
  -p:MauiDevFlowEnabled=true \
  --no-restore
```

The opt-in property adds the agent package and defines `MAUI_DEVFLOW`. The app registers the agent
in [`MauiProgram.cs`](../app/MyFireNumber/MauiProgram.cs) for each configured target: Android, iOS,
Mac Catalyst, and Windows.

## Mac Catalyst

Build, launch, and wait for the agent:

```bash
dotnet build app/MyFireNumber/MyFireNumber.csproj \
  -f net10.0-maccatalyst \
  -c Debug \
  -p:MauiDevFlowEnabled=true

open "app/MyFireNumber/bin/Debug/net10.0-maccatalyst/maccatalyst-arm64/My Fire #.app"

maui devflow wait \
  --project app/MyFireNumber/MyFireNumber.csproj \
  --wait-platform maccatalyst \
  --timeout 120
```

Verify the connection and inspect the UI:

```bash
maui devflow list
maui devflow ui status
maui devflow ui tree --depth 4 --format compact
maui devflow ui screenshot --output artifacts/maccatalyst.png --overwrite
```

## iOS Simulator

Start a simulator, then run the instrumented target against its UDID:

```bash
maui apple simulator list
maui apple simulator start "<simulator-name-or-udid>"

dotnet build app/MyFireNumber/MyFireNumber.csproj \
  -t:Run \
  -f net10.0-ios \
  -c Debug \
  -p:MauiDevFlowEnabled=true \
  -p:_DeviceName=:v2:udid="<simulator-udid>"

maui devflow wait \
  --project app/MyFireNumber/MyFireNumber.csproj \
  --wait-platform ios \
  --timeout 120
```

## Android

Android requires ADB reverse port forwarding each time an emulator or connected device restarts.
Use the serial reported by `adb devices`:

```bash
adb -s "<device-serial>" reverse tcp:19223 tcp:19223

dotnet build app/MyFireNumber/MyFireNumber.csproj \
  -t:Run \
  -f net10.0-android \
  -c Debug \
  -p:MauiDevFlowEnabled=true
```

After the agent registers, forward its assigned port from `maui devflow list`:

```bash
adb -s "<device-serial>" reverse tcp:<agent-port> tcp:<agent-port>
```

## Windows

The agent is registered for the Windows target when the project is built on Windows:

```powershell
dotnet build app/MyFireNumber/MyFireNumber.csproj `
  -f net10.0-windows10.0.19041.0 `
  -c Debug `
  -p:MauiDevFlowEnabled=true
```

## Troubleshooting

Run the built-in checks first:

```bash
maui devflow diagnose --platform maccatalyst
maui devflow list
```

If Mac Catalyst crashes during startup with a `FileNotFoundException` for
`Microsoft.Maui.DevFlow.Agent`, confirm that the agent package reference does **not** use
`PrivateAssets="all"`. That setting prevents the assembly from being bundled with the Mac Catalyst
app.

Current DevFlow runtime support is strongest on Mac Catalyst and iOS Simulator. Android requires
the ADB forwarding above, and Windows support remains in progress.
