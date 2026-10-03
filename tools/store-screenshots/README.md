# Store Screenshot Runbook

How the App Store and Play Store screenshots in `metadata/` are produced.

The app ships **no demo mode**. Demo data is written straight into a simulator's
SQLite file by `seed_demo_data.py`, so nothing here reaches production code or a
real device. Never run these against a physical device or a personal profile.

## What you need

- macOS with Xcode and the iOS simulators installed
- .NET MAUI workloads (`dotnet workload restore`)
- Python 3 with Pillow (`pip3 install pillow`)

## 1. Build and install

Build once for the simulator. The same arm64 slice installs on both iPhone and
iPad simulators.

```bash
dotnet build app/MyFireNumber/MyFireNumber.csproj \
  -f net10.0-ios -c Debug -p:RuntimeIdentifier=iossimulator-arm64

xcrun simctl boot "<DEVICE_UDID>"
xcrun simctl install "<DEVICE_UDID>" \
  app/MyFireNumber/bin/Debug/net10.0-ios/iossimulator-arm64/MyFireNumber.app
xcrun simctl launch "<DEVICE_UDID>" com.refractored.myfirenumber
```

Launch once before seeding so the app creates its database and schema.

Devices used for the current set:

| Slot | Device | Native size |
| --- | --- | --- |
| iPhone 6.5-inch | iPhone 17 Pro Max | 1320 x 2868 |
| iPad 13-inch | iPad Pro 13-inch (M5) | 2064 x 2752 |

The iPhone capture is downscaled to 1242 x 2688 during framing. The iPad capture
is already the exact required size.

## 2. Seed the demo data

Find the database, then seed it:

```bash
DB=$(find ~/Library/Developer/CoreSimulator/Devices/<DEVICE_UDID> \
  -name "my-fire-number-v4.db3" | head -1)

python3 tools/store-screenshots/seed_demo_data.py "$DB"
xcrun simctl terminate <DEVICE_UDID> com.refractored.myfirenumber
xcrun simctl launch <DEVICE_UDID> com.refractored.myfirenumber
```

The script prints a summary and asserts that itemized income and expenses match
the headline profile figures, so linked calculators can't show two different
numbers on one screen.

### Getting past onboarding

The app rewrites its preferences plist on launch, so setting the
`onboarding-v2-*` keys externally does not reliably stick. The dependable path is
to tap through onboarding once after seeding — every field is already pre-filled
from the seeded profile, so it is three taps:

1. **Get started**
2. **Skip** on each of "About you", "Your timeline", and "Withdrawal rate"
3. **Explore on my own** on "Choose a starting point"

Choosing "Explore on my own" leaves the seeded data untouched.

## 3. Capture

Pin the status bar first so captures are reproducible:

```bash
xcrun simctl status_bar <DEVICE_UDID> override \
  --time "09:41" --batteryState charged --batteryLevel 100 \
  --wifiBars 3 --cellularBars 4 --dataNetwork wifi
```

Capture each screen to a raw directory. Filenames must match the `SLIDES` list in
`frame_screenshots.py`:

```bash
xcrun simctl io <DEVICE_UDID> screenshot raw/01-home.png
```

| File | Screen | How to get there |
| --- | --- | --- |
| `01-home.png` | Home dashboard | Launch state |
| `02-accounts.png` | Accounts overview | Home → **Go to Accounts** |
| `03-history.png` | History & trends | Accounts → **History & trends** |
| `04-calculators.png` | Calculator catalog | Calculators tab |
| `05-coast-fire.png` | Coast FIRE results | Calculators → Coast FIRE → **Linked Profile** |

Pick **Linked Profile** on the Coast FIRE prompt so the slide demonstrates the
profile-linking feature rather than a standalone snapshot.

## 4. Frame

```bash
# iPhone 6.5-inch
python3 tools/store-screenshots/frame_screenshots.py raw metadata/iphone-6.5 1242 2688

# iPad 13-inch
python3 tools/store-screenshots/frame_screenshots.py raw-ipad metadata/ipad-13 2064 2752
```

Each output is a branded gradient with a headline, subhead, accent rule, and the
capture inside a rounded bezel with a drop shadow. All spacing scales from the
canvas width, so new dimensions work without editing the script. Headline and
subhead copy lives in the `SLIDES` list at the top of `frame_screenshots.py`.

Raw captures are scratch output — keep them outside the repo, or delete them once
the framed set is generated. Only the framed results belong in `metadata/`.

## Accepted dimensions

App Store Connect accepts 1242 x 2688 or 1284 x 2778 for the iPhone slot and
2064 x 2752 for the 13-inch iPad slot. Uploading these two master sizes lets it
scale the remaining device classes.

## Mac Catalyst

Build the Mac Catalyst app with an isolated bundle identifier so demo data never
touches a locally installed copy:

```bash
dotnet build app/MyFireNumber/MyFireNumber.csproj \
  -f net10.0-maccatalyst \
  -c Debug \
  -p:RuntimeIdentifier=maccatalyst-arm64 \
  -p:ApplicationId=com.refractored.myfirenumber.screenshots
```

Launch the app once, seed the isolated container's database with
`seed_demo_data.py`, and capture these four screens at native scale:

| File | Screen |
| --- | --- |
| `01-home.png` | Home dashboard |
| `02-accounts.png` | Accounts overview |
| `03-history.png` | History & trends |
| `04-coast-fire.png` | Linked Coast FIRE plan |

Frame the raw captures at the largest accepted Mac App Store size:

```bash
python3 tools/store-screenshots/frame_macos_screenshots.py \
  raw-macos metadata/macos
```

The outputs are 2880 x 1800 pixels with a desktop-specific side-by-side layout,
Mac window chrome, branded gradients, headlines, and supporting copy.

## Windows

You need the .NET MAUI Windows workload, Python 3 with Pillow, and the
[`winapp` CLI](https://github.com/microsoft/winappcli) for UI Automation.

**Build and launch.** Restore the app for the Windows target only, restore the two
libraries on their own (the app's `TargetFramework` override would otherwise leak
into them), then build without restoring again:

```powershell
$tf = 'net10.0-windows10.0.19041.0'
dotnet restore app\MyFireNumber\MyFireNumber.csproj -p:TargetFramework=$tf
dotnet restore app\MyFireNumber.Core\MyFireNumber.Core.csproj
dotnet restore app\MyFireNumber.Storage\MyFireNumber.Storage.csproj
dotnet build app\MyFireNumber\MyFireNumber.csproj -f $tf -c Debug -r win-x64 --no-restore

$app = Start-Process "app\MyFireNumber\bin\Debug\$tf\win-x64\MyFireNumber.exe" -PassThru
```

**Seed.** The unpackaged build keeps its data under
`%LOCALAPPDATA%\User Name\com.refractored.myfirenumber`. That folder is shared by
every unpackaged run on the machine (the Store install is packaged and lives
elsewhere), and passing `-p:ApplicationId` does not move it on an incremental build,
so move an existing folder aside first and delete the seeded one when you are done.
Launch once so the schema exists, close the app, then:

```powershell
$data = "$env:LOCALAPPDATA\User Name\com.refractored.myfirenumber"
python tools\store-screenshots\seed_demo_data.py "$data\Data\my-fire-number-v4.db3"
```

Preferences are a plain JSON file here, so onboarding can be skipped and the theme
pinned without tapping through anything. Write this to
`$data\Settings\preferences.dat` while the app is closed:

```json
{"":{"theme-preference":"Dark","onboarding-v2-welcome-seen":"True","onboarding-v2-completed":"True"}}
```

**Capture.** Relaunch, then let the script drive the app. It takes over the mouse and
the foreground window for about a minute, so leave the machine alone while it runs:

```powershell
tools\store-screenshots\capture_windows.ps1 -AppPid $app.Id -OutDir raw-windows
```

It sizes the window to 1180 x 980 device-independent pixels and saves the same five
screens as the phone sets, choosing **Linked Profile** for Coast FIRE. Capture on a
display scaled to 150% or more: the window is saved at native resolution, and a
100% display produces a capture too small to frame without upscaling.

**Frame.**

```powershell
python tools\store-screenshots\frame_windows_screenshots.py raw-windows metadata\windows
```

The outputs are 2560 x 1440 pixels. Partner Center accepts desktop screenshots from
1366 x 768 up to 3840 x 2160, as PNG files of 50 MB or less. Pass a width and height
to render another 16:9 size.

Partner Center's own guidance prefers screenshots without added marketing copy and
may overlay text on the bottom third. The headline block sits in the upper two-thirds
for that reason. The raw captures are valid Store screenshots as they are if an
unframed set is ever needed.

## Demo persona

Kept internally consistent so no screen contradicts another. Change it in one
place — the constants at the top of `seed_demo_data.py`.

Alex Rivera, 37, "The Rivera Household", household of 3. Full retirement at 55,
phased at 52. Income $182,000, expenses $96,000.

- 5 accounts totaling **$838,000**, contributing $54,700/yr
- 2 property assets totaling **$489,500**: the family home and an SUV, each backing a debt
- 3 debts totaling **$290,830**
- **Net worth $1,036,670**
- 12 monthly check-ins curving from $863,720 up to $1,025,484
- 4 saved plans, all linked to the profile

The newest check-in sits one notch behind the live balances on purpose, so Home
reads "Up $11,186 since your last update" instead of "No change".

Check-ins are dated relative to the day the script runs, with the newest two days
old. A fixed date would drift past the 30-day cadence and put an orange "Overdue"
line on the Home dashboard.

The iOS, Android, and Mac sets in `metadata/` predate the property assets and still
show a $547,170 net worth. They pick up these figures the next time they are
recaptured.

## Serialization notes

If you extend `seed_demo_data.py`, match `app/MyFireNumber.Storage/LocalDatabase.cs`
exactly. There is no `JsonStringEnumConverter` anywhere in the codebase, so the
two conventions below coexist and are easy to mix up:

- **SQL columns** store enum **names** — `profile_accounts.Type` is `"Traditional"`
- **JSON payloads** store enum **numbers** — `AccountsJson` uses `"Type": 1`
- `RetirementAccountType`: Deferred=0, Traditional=1, Roth=2, Taxable=3,
  Savings=4, Hsa=5, Other=6
- `PropertyAssetType`: Home=0, RealEstate=1, Land=2, Vehicle=3, Collectible=4,
  Other=5 — `profile_assets.Type` is the name, `AssetsJson` uses the number
- Plan `PayloadJson` uses default `System.Text.Json` options: PascalCase names,
  numeric enums
- `recent_activity.Key` is `"{Kind}:{ItemId}"` with a colon
- `DateTime` uses round-trip `"O"` UTC; `DateOnly` uses `yyyy-MM-dd`
- `profile` is a singleton row with a fixed `Id = 1`

## Android

Same idea, with two Android-specific gotchas.

**Build with assemblies embedded.** A plain `dotnet build` for Android produces a
Fast Deployment APK whose assemblies are pushed separately by the deploy tooling.
Installing that APK with `adb install` gives you an app that aborts on launch with
`No assemblies found in ... /.__override__/arm64-v8a`, and never creates its
database. Pass `-p:EmbedAssembliesIntoApk=true`:

```bash
dotnet build app/MyFireNumber/MyFireNumber.csproj \
  -f net10.0-android -c Debug -p:EmbedAssembliesIntoApk=true

adb install --no-incremental -r \
  app/MyFireNumber/bin/Debug/net10.0-android/com.refractored.myfirenumber-Signed.apk
adb shell am start -n com.refractored.myfirenumber/crc649d097029e233987d.MainActivity
```

A correct APK is around 110 MB. If yours is a few MB, the assemblies are not in it.

**Seed by round-tripping the file.** `run-as` can read and write the app's data
directory but the seeding script needs a local file, so pull, seed, and push back
with the app stopped:

```bash
DBPATH=/data/data/com.refractored.myfirenumber/files/my-fire-number-v4.db3

adb shell "run-as com.refractored.myfirenumber cat $DBPATH" > /tmp/and.db3
python3 tools/store-screenshots/seed_demo_data.py /tmp/and.db3

adb shell am force-stop com.refractored.myfirenumber
adb push /tmp/and.db3 /data/local/tmp/and.db3
adb shell "run-as com.refractored.myfirenumber cp /data/local/tmp/and.db3 $DBPATH"
adb shell "run-as com.refractored.myfirenumber rm -f $DBPATH-wal $DBPATH-shm"
adb shell am start -n com.refractored.myfirenumber/crc649d097029e233987d.MainActivity
```

Removing the `-wal` and `-shm` files matters — a stale write-ahead log will
otherwise replay over the data you just seeded.

Then tap through onboarding exactly as on iOS.

**Status bar.** Android has its own demo mode:

```bash
adb shell settings put global sysui_demo_allowed 1
adb shell am broadcast -a com.android.systemui.demo -e command enter
adb shell am broadcast -a com.android.systemui.demo -e command clock -e hhmm 0941
adb shell am broadcast -a com.android.systemui.demo -e command battery -e level 100 -e plugged false
adb shell am broadcast -a com.android.systemui.demo -e command network -e wifi show -e level 4
adb shell am broadcast -a com.android.systemui.demo -e command notifications -e visible false
```

Capture with `adb exec-out screencap -p > raw-android/01-home.png`, then frame:

```bash
python3 tools/store-screenshots/frame_screenshots.py raw-android metadata/android-phone 1080 1920
```

Play Store phone screenshots must be between 320 and 3840 px on a side with an
aspect ratio no taller than 2:1, so the Pixel's native 1080 x 2400 (2.22:1) is
rejected. 1080 x 1920 is the safe target.

**Purple status bar on pages with a nav bar.** Any page with
`Shell.NavBarIsVisible="True"` (Coast FIRE, Standard FIRE, History & trends,
Profile, ...) used to show the default .NET MAUI template purple
(`#512BD4`) behind the status bar on Android, while pages with a hidden nav
bar (Home, Accounts, Calculators, Plans) looked correct. Root cause:
`Platforms/Android/Resources/values/colors.xml` still had the unedited
template `colorPrimary`/`colorPrimaryDark`, which Android falls back to for
the status bar whenever the native Toolbar is shown — this is unrelated to
the Shell/NavigationPage `BarBackgroundColor` XAML setters in
`Resources/Styles/Styles.xaml`. Fixed by updating `colors.xml` to the app's
actual light palette and adding a `values-night/colors.xml` override for
dark mode. If you see this again, check those two files first.
