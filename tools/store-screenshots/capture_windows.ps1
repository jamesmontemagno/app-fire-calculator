<#
.SYNOPSIS
Captures the raw Microsoft Store screenshots from a running, seeded My Fire # window.

.DESCRIPTION
Drives the app through UI Automation with the `winapp` CLI, so it needs an unlocked desktop
and takes over the mouse and foreground window while it runs. Seed the demo data and get past
onboarding first; see README.md. Frame the output with frame_windows_screenshots.py.

.EXAMPLE
.\capture_windows.ps1 -AppPid 1234 -OutDir raw-windows
#>
param(
    [Parameter(Mandatory)][int]$AppPid,
    [Parameter(Mandatory)][string]$OutDir,
    # Device-independent pixels. The height deliberately overshoots a 1080p work area: Windows
    # lets a window hang below the taskbar, and the capture still grabs all of it. The Coast FIRE
    # outlook and its chart only fit on one screen at this height.
    [int]$Width = 1180,
    [int]$Height = 980
)

$ErrorActionPreference = 'Stop'

Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class StoreShotWindow {
    [DllImport("user32.dll")] static extern bool MoveWindow(IntPtr hWnd, int x, int y, int w, int h, bool repaint);
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int cmd);
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern uint GetDpiForWindow(IntPtr hWnd);
    [DllImport("user32.dll")] static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
    [DllImport("user32.dll")] static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);

    // Sizes in physical pixels regardless of whether the calling shell is DPI-aware.
    public static void Place(IntPtr hWnd, int width, int height) {
        IntPtr previous = SetThreadDpiAwarenessContext(new IntPtr(-4));
        double scale = GetDpiForWindow(hWnd) / 96.0;
        ShowWindow(hWnd, 9);
        MoveWindow(hWnd, (int)(40 * scale), 0, (int)(width * scale), (int)(height * scale), true);
        SetThreadDpiAwarenessContext(previous);
    }

    // Windows refuses SetForegroundWindow from a background process, but lifts that lock when
    // Alt is pressed. Without the foreground the title bar is captured in its dimmed inactive
    // state and winapp refuses to click. Alt is released before the switch so the keystroke never
    // reaches the app, which would otherwise draw a keyboard focus ring. Activation lands
    // asynchronously and loses to anything the user is typing elsewhere, hence the retries.
    public static bool Activate(IntPtr hWnd) {
        for (int attempt = 0; attempt < 8; attempt++) {
            if (GetForegroundWindow() == hWnd) return true;
            keybd_event(0x12, 0, 0, UIntPtr.Zero);
            keybd_event(0x12, 0, 2, UIntPtr.Zero);
            SetForegroundWindow(hWnd);
            System.Threading.Thread.Sleep(250);
        }
        return GetForegroundWindow() == hWnd;
    }
}
"@

$window = (Get-Process -Id $AppPid).MainWindowHandle
if ($window -eq [IntPtr]::Zero) { throw "Process $AppPid has no main window." }
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$OutDir = (Resolve-Path $OutDir).Path

function Use-Window {
    if (-not [StoreShotWindow]::Activate($window)) { throw 'Could not bring the app to the foreground.' }
    Start-Sleep -Milliseconds 300
}

function Get-Elements {
    function Expand-Node($node) { $node; foreach ($child in $node.children) { Expand-Node $child } }
    $tree = winapp ui inspect -a $AppPid --depth 24 --json 2>$null | ConvertFrom-Json
    foreach ($element in $tree.windows[0].elements) { Expand-Node $element }
}

function Find-Element([string]$NamePattern, [string]$Type) {
    Get-Elements |
        Where-Object { $_.name -match $NamePattern -and (-not $Type -or $_.type -eq $Type) -and -not $_.isOffscreen } |
        Select-Object -First 1
}

function Invoke-WinApp {
    winapp ui @args -a $AppPid -q | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "winapp ui $args failed." }
}

function Open-Tab([string]$Name, [string]$ReadyId) {
    Invoke-WinApp invoke (Find-Element "^$Name$" 'TabItem').selector
    Invoke-WinApp wait-for $ReadyId -t 8000
    Get-Elements | Where-Object { $_.className -eq 'ScrollViewer' -and $_.height -gt 500 } | ForEach-Object {
        winapp ui scroll $_.selector -a $AppPid --to top -q 2>$null | Out-Null
    }
}

function Save-Capture([string]$Name, [switch]$KeepPointer) {
    Use-Window
    if (-not $KeepPointer) {
        # Park the pointer on the title bar so no button is caught hovered and the scroll
        # indicators have time to fade.
        Invoke-WinApp hover AppTitle
    }
    Start-Sleep -Seconds 3
    Invoke-WinApp screenshot -o (Join-Path $OutDir $Name)
    Write-Host "captured $Name"
}

[StoreShotWindow]::Place($window, $Width, $Height)
Start-Sleep -Seconds 1

Open-Tab 'Home' 'HomeNetWorthLabel'
Save-Capture '01-home.png'

Open-Tab 'Accounts' 'AccountsNetWorthLabel'
Save-Capture '02-accounts.png'

# Opened from Accounts so the Accounts tab stays highlighted behind the charts.
Invoke-WinApp invoke ViewHistoryButton
Invoke-WinApp wait-for HistoryRangeOneYearButton -t 8000
Save-Capture '03-history.png'
Invoke-WinApp invoke NavigationViewBackButton

Open-Tab 'Calculators' 'CalculatorSearch'
Save-Capture '04-calculators.png'

# Catalog rows are tap gestures with no UIA invoke pattern, so they need a real click.
Use-Window
Invoke-WinApp click (Find-Element '^Open Coast FIRE calculator$').selector
Start-Sleep -Seconds 2
$linkedProfile = Find-Element '^Linked Profile$'
if ($linkedProfile) {
    Use-Window
    Invoke-WinApp click $linkedProfile.selector
}
Invoke-WinApp wait-for ScenarioModeBannerText -t 8000
Start-Sleep -Seconds 2

# Bottom-align the chart summary: that leaves the outlook heading, result cards, and chart all on
# screen. A tap pins that age's values under the chart, which grows the page, so align once to
# reach the chart and again after the tap. The closing hover raises the tooltip that doubles as
# the chart's legend. The scroll reports failure when the summary is already in view, which is fine.
function Show-ChartSummary {
    $summary = Get-Elements | Where-Object { $_.name -match '^By age \d+, coasting' } | Select-Object -First 1
    winapp ui scroll-into-view $summary.selector -a $AppPid -q 2>$null | Out-Null
    Start-Sleep -Seconds 1
}

Show-ChartSummary
$chart = (Find-Element '^Coast FIRE comparison').selector
Use-Window
Invoke-WinApp click $chart
Start-Sleep -Seconds 1
Show-ChartSummary
Use-Window
Invoke-WinApp hover $chart
Save-Capture '05-coast-fire.png' -KeepPointer

Invoke-WinApp invoke NavigationViewBackButton
Open-Tab 'Home' 'HomeNetWorthLabel'
