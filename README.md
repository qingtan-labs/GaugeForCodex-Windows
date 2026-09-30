# Gauge for Codex · Windows

[简体中文](README.zh-Hans.md) · English

> Independent community tool. Not affiliated with or endorsed by OpenAI.

> 1.0.0: an opaque tray flyout, separate frosted desktop widget, DPI-aware tray artwork, and non-repeating dual-window quota details.

<img src="assets/app-icon.png" width="96" alt="Original Gauge for Codex Windows icon">

A native Windows tray companion for Codex quota, with an optional softly frosted desktop card.
This is the Windows edition of the Gauge for Codex product family; [macOS source](https://github.com/qingtan-labs/GaugeForCodex) stays independent.

<img src="assets/screenshots/widget-preview.png" width="292" alt="Frosted quota widget layout with synthetic values">

The preview uses synthetic quota values; system blur/transparency depends on your Windows settings and background.

[Download 1.0.0](https://github.com/qingtan-labs/GaugeForCodex-Windows/releases/tag/v1.0.0) · [Detailed guide (中文)](docs/user-guide.zh-Hans.md) · [Privacy](PRIVACY.md) · [Security](SECURITY.md) · [Support](SUPPORT.md)

## Visual guide

### Tray flyout

<img src="assets/screenshots/tray-panel-preview.png" width="336" alt="Quota summary, other periods, widget toggle, refresh, updates, settings and exit">

When an actual five-hour window is available, it has a dedicated summary; other windows follow below. Otherwise the tightest available window is used. A clear desktop-widget switch and compact action footer keep the panel concise. Dismiss with Escape, the close button, or loss of focus.

The tray flyout is a normal opaque panel, without desktop blur. Only the optional desktop widget uses frosted glass: a 58% default tint, adjustable within 56–70%, and independent opaque text preserve backdrop visibility and readability.

### Frosted desktop widget: collapsed / expanded

<img src="assets/screenshots/widget-preview.png" width="304" alt="Collapsed quota card with visible View details affordance">
<img src="assets/screenshots/widget-expanded-preview.png" width="304" alt="Five-hour main card with only the additional weekly allowance in details">
<img src="assets/screenshots/single-weekly-preview.png" width="304" alt="Weekly-only card without a redundant disclosure button">

**View details ▾** becomes **Less details ▴** when expanded. It shows only other quota windows, never a copy of the main card. Single-window and unavailable data hide disclosure entirely; incoming single-window data also closes an open detail area. Duplicate server fields are deduplicated, and a low/exhausted weekly limit stays visible even when details are collapsed. Tab and Enter remain supported. Update time stays visible. These are WPF layout renders with synthetic data, not desktop screenshots; live background blur is composed by Windows.

### Application icon and light/dark tray

<img src="assets/screenshots/app-icon-sizes.png" width="760" alt="Application icon at 16, 32, 48, 64 and 128 pixels">
<img src="assets/screenshots/tray-icon-preview.png" width="920" alt="Light/dark tray marks at 16, 20, 24 and 32 pixels, actual size above and 2x below">

The app keeps the macOS C + terminal monogram with a restrained quota arc. The tray uses a bolder, pixel-hinted monochrome mark and follows the taskbar light/dark preference. Exact-size frames are retained after DPI changes and update reminders. This is a scale-audit board, not a taskbar screenshot. See the [tray clarity audit](docs/tray-clarity.md).

### Low quota / unavailable data

<img src="assets/screenshots/low-quota-preview.png" width="304" alt="Low quota uses muted red and keeps a numeric value and cache timestamp">
<img src="assets/screenshots/unavailable-preview.png" width="304" alt="Unknown quota uses a dash, not zero, with disabled expansion">

Below 20%, the progress color changes while the numeric value remains. Cached values retain their timestamp; missing values never masquerade as zero. See the [design audit](docs/design-audit.md) for the verification scope and limitations.

## Interaction

Click the tray icon to see remaining quota, reset times and actions. Right-click for the quick menu.
Enable **Show desktop widget** for a draggable card; the card is not always-on-top by default.
The card's × hides it, while **Exit application** closes the quota UI and stops refresh. A minimal guardian can open it again on the next Codex desktop session.
Use the **View details** / **Less details** footer when other quota periods are available. The body is draggable; buttons do not initiate dragging. Tab/Enter operate actions; Escape dismisses the flyout.
If Windows hides the icon in tray overflow, drag it into the visible tray area.

When Plus returns both five-hour and seven-day windows, the card shows five-hour quota and details show weekly quota. If only weekly quota is returned, that is the main card and disclosure is hidden. Missing periods are never inferred from the subscription name.

Theme scope: tray artwork follows the taskbar light/dark setting. Version 1.0.0 panels retain a neutral light palette; a full system/light/dark selector is not implemented. Language is manually selectable; first launch defaults to Chinese on Chinese systems and English otherwise. See the [1.0.0 verification scope](docs/verification-1.0.0.md).

## Features

- Remaining and used percentages; actual five-hour quota first when returned, additional windows in details, with no invented missing quota.
- Reset-day countdown and accurate local date/time, with explicit unknown/cached/manual states.
- Startup and 60-second refresh through local `account/rateLimits/read`; no model invocation.
- Native WPF and system tray; thinner neutral glass tint with native background blur on Windows 11, Acrylic/opaque compatibility fallback. See [glass design and limitations](docs/frost-and-details.md).
- Persistent position, configurable glass tint, optional always-on-top and startup choices.
- Codex-follow start/exit by default; manual exit suppresses relaunch within that desktop session.
- Weekly update checks notify only; manual check, explicit install confirmation, SHA-256 verification and rollback backup.
- English, Simplified Chinese, Japanese and Spanish. No ads, telemetry or developer backend. No pet logic/assets.

## Requirements and installation

Windows 10 22H2 or Windows 11, x64/ARM64, and installed/signed-in Codex Desktop. Live Acrylic requires Windows 11 22H2+ and enabled system transparency effects.
Each ZIP includes the .NET 8 Windows Desktop runtime; the SDK is only needed for building source.
ARM64 packages are cross-compiled/resource-verified; native ARM64 desktop testing is not yet complete.

Verify the ZIP hash against the release's `SHA256SUMS.txt`, extract, and open `GaugeForCodex.exe`.
Optional per-user installation (no administrator needed):

```powershell
# Run inside the extracted application folder.
.\install.ps1
# Optional custom user-writable directory:
.\install.ps1 -InstallDir 'D:\aiTools\ChatGPT\GaugeForCodex'
```

The installer creates Desktop and Start-menu shortcuts plus a sign-in guardian. Settings control whether the guardian follows Codex or starts the card independently at sign-in.
Preferences, normalized quota and rollback backups are stored in `Data/` beside the executable. Do not publish this folder.
The package is **not Authenticode signed**. Do not disable SmartScreen or other Windows protections.

## Build

```powershell
dotnet run --project tests/GaugeForCodex.Tests.csproj -c Release -p:TreatWarningsAsErrors=true
.\scripts\build-release.ps1 -Runtime win-x64
.\scripts\build-release.ps1 -Runtime win-arm64
```

Editable SVG icon masters are included; their PNG/ICO outputs are already committed. Rebuilding icons requires Node.js and `sharp`.
See [contribution guidelines](CONTRIBUTING.md), [release process](docs/release-process.md), [changelog](CHANGELOG.md) and [notices](NOTICE.md).

## License

Source and original project graphics: [MIT](LICENSE). Codex/OpenAI names identify compatibility only.
