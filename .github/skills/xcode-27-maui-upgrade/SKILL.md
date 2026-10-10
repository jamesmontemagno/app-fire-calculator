---
name: xcode-27-maui-upgrade
description: Upgrade .NET MAUI iOS and Mac Catalyst apps for Xcode 27 and Apple's scene-based lifecycle. Use when migrating Apple-platform builds, investigating NoSceneLifecycleAdoption launch crashes, or aligning .NET workloads with Xcode.
license: MIT
---

# Xcode 27 .NET MAUI upgrade

Use this skill to migrate existing .NET MAUI apps to Apple's scene-based UIKit lifecycle and verify their Xcode/.NET build configuration. If the user asks for an explanation only, diagnose and explain without editing; otherwise, implement the migration.

## Diagnose first

1. Read the repository's applicable instructions and inspect the app's Apple target frameworks, MAUI package/workload versions, `AppDelegate`, entry point, `Info.plist` files, and CI workflows.
2. Inspect the crash log before changing lifecycle code. A launch trap in `___UIApplicationEvaluateRuntimeIssueForNoSceneLifecycleAdoption_block_invoke` or a `NoSceneLifecycleAdoption` message indicates that the app has not adopted scene lifecycle. Do not apply this migration to unrelated crashes.
3. Check whether each iOS and Mac Catalyst app head already has both a registered `SceneDelegate` and a `UIApplicationSceneManifest`. A manifest without its delegate, or a delegate whose registered name differs from the manifest, is incomplete.

## Choose a compatible toolchain

- Check `dotnet --info`, `dotnet workload --version`, `dotnet workload list`, and `xcodebuild -version`. Use `dotnet workload --version` to identify the workload set selected by the installed SDK; `dotnet workload list` shows the installed workloads.
- When evaluating an upgrade, run `dotnet workload search version --format json --take 1` to discover the latest released workload-set candidate for the SDK feature band. This reports candidates, not the active workload set. To inspect a specific candidate's manifest versions, run `dotnet workload search version <workload-version>`. For the active set and each candidate, inspect `data/microsoft.net.workloads.workloadset.json` and find the iOS and Mac Catalyst manifest versions. Read each manifest's `data/WorkloadDependencies.json` and check `xcode.version` and `xcode.recommendedVersion`. The manifests are available in the installed SDK manifest directory or as NuGet packages under `https://api.nuget.org/v3-flatcontainer/`.
- Do not infer compatibility from the .NET SDK major version alone or hardcode a version from old guidance. If the installed and candidate workload sets differ, use the set that CI will restore and report the exact Xcode range it requires.
- Keep the .NET SDK, workload set, and Xcode version compatible. If pinning for reproducibility, pin the complete pair and ensure CI restores the same workload set; do not pin only Xcode or only the SDK.
- Do not treat downgrading Xcode as the lifecycle fix. A downgrade does not add scene configuration and is not a durable way to support newer Apple operating systems.

## Add MAUI scene lifecycle

For every Apple app head that builds with the affected SDK, add a platform-specific `SceneDelegate.cs` under both `Platforms/iOS` and `Platforms/MacCatalyst` as applicable. Use the app's existing namespace:

```csharp
using Foundation;

namespace MyApp;

[Register("SceneDelegate")]
public class SceneDelegate : MauiUISceneDelegate
{
}
```

Add this scene manifest inside the root `<dict>` in each corresponding `Info.plist`:

```xml
<key>UIApplicationSceneManifest</key>
<dict>
    <key>UIApplicationSupportsMultipleScenes</key>
    <false/>
    <key>UISceneConfigurations</key>
    <dict>
        <key>UIWindowSceneSessionRoleApplication</key>
        <array>
            <dict>
                <key>UISceneConfigurationName</key>
                <string>__MAUI_DEFAULT_SCENE_CONFIGURATION__</string>
                <key>UISceneDelegateClassName</key>
                <string>SceneDelegate</string>
            </dict>
        </array>
    </dict>
</dict>
```

Keep `__MAUI_DEFAULT_SCENE_CONFIGURATION__` unchanged; it must match MAUI's framework configuration. Keep the `SceneDelegate` registration name and manifest class name identical. Leave multiple-scene support disabled unless the app intentionally supports multiple windows.

Keep the existing `AppDelegate` and entry point unless the app has a separately justified lifecycle refactor. MAUI's `MauiUISceneDelegate` creates the MAUI window and forwards scene lifecycle events; do not add duplicate window creation or manual lifecycle forwarding. If `MauiUISceneDelegate` is unavailable, update to a MAUI version that supports it rather than replacing it with a bare UIKit delegate.

The scene migration does not require network entitlements. Do not add `com.apple.security.network.server`; handle any entitlement review as a separate capability audit.

## Update and verify CI

Keep workflow runner images and Xcode selectors consistent with the supported workload range. Run `dotnet workload restore` after selecting the SDK, then build each affected target using the repository's existing release and signing settings. Example target builds:

```bash
dotnet build path/to/App.csproj -c Debug -f net10.0-ios \
  -p:RuntimeIdentifier=iossimulator-arm64
dotnet build path/to/App.csproj -c Debug -f net10.0-maccatalyst \
  -p:RuntimeIdentifier=maccatalyst-arm64
```

Validate the source plists with `plutil -lint`. Inspect the generated app bundle's `Info.plist` to confirm the manifest, configuration name, and delegate class are present in the packaged app. Preserve the app's existing linker, AOT, and signing settings when validating Release builds.

When a simulator or device running the affected Apple OS is available, launch the built app and confirm it reaches its first MAUI window. A successful build alone does not verify startup. If that OS is unavailable, state that runtime launch remains unverified rather than claiming the crash is resolved.

## References

- [Apple: Transitioning to the UIKit scene-based life cycle](https://developer.apple.com/documentation/uikit/transitioning-to-the-uikit-scene-based-life-cycle)
- [Apple Technote TN3187: Migrating to the UIKit scene-based life cycle](https://developer.apple.com/documentation/technotes/tn3187-migrating-to-the-uikit-scene-based-life-cycle)
- [dotnet/macios issue #26837](https://github.com/dotnet/macios/issues/26837)
